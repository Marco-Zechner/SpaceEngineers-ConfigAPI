using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Layout;

namespace mz.Config.Core.Layout
{
    /// <summary>
    /// Normalizes a config's XML layout against the current code layout:
    /// - adds missing keys with defaults
    /// - removes keys that no longer exist (signals backup)
    /// - keeps existing values where possible
    /// - upgrades values that are still equal to an old default to the new default
    ///
    /// It operates only on XML strings produced/consumed by the XML serializer,
    /// using LayoutXml to handle full child elements (so xsi:nil, nested blocks etc. survive).
    /// </summary>
    public sealed class ConfigLayoutMigrator : IConfigLayoutMigrator
    {
        public ConfigLayoutResult Normalize(
                IConfigDefinition definition,
                string xmlCurrentFromFile,
                string xmlOldDefaultsFromFile,
                string xmlCurrentDefaults)
            {
                if (definition == null)
                    throw new ArgumentNullException(nameof(definition));

                // Defensive: treat null as empty
                if (xmlCurrentFromFile == null)
                    xmlCurrentFromFile = string.Empty;
                if (xmlOldDefaultsFromFile == null)
                    xmlOldDefaultsFromFile = string.Empty;
                if (xmlCurrentDefaults == null)
                    xmlCurrentDefaults = string.Empty;

                var result = new ConfigLayoutResult();

                try
                {
                    // Parse three layouts into: rootName + map of childName -> full child XML
                    string rootCurrent;
                    var currentChildren = LayoutXml.ParseChildren(xmlCurrentFromFile, out rootCurrent);

                    string rootOldDefaults;
                    var oldDefaultChildren = LayoutXml.ParseChildren(xmlOldDefaultsFromFile, out rootOldDefaults);

                    string rootNewDefaults;
                    var newDefaultChildren = LayoutXml.ParseChildren(xmlCurrentDefaults, out rootNewDefaults);

                    // Use the config's type name as canonical root name (matches serializer)
                    var rootName = definition.TypeName;

                    var normalizedCurrentChildren = new Dictionary<string, string>();
                    var normalizedDefaultChildren = new Dictionary<string, string>();
                    var requiresBackup = false;

                    // Work across keys present in the "current" default layout
                    foreach (var kv in newDefaultChildren)
                    {
                        var key = kv.Key;
                        var newDefaultElement = kv.Value;

                        string currentElement;
                        var hasCurrent = currentChildren.TryGetValue(key, out currentElement);

                        string oldDefaultElement;
                        var hasOldDefault = oldDefaultChildren.TryGetValue(key, out oldDefaultElement);

                        // Detect structural corruption:
                        // If the default element has complex nested children (e.g. Settings.Display.Width/Height/...)
                        // but the current element has those complex children collapsed to scalars or missing, 
                        // we treat it as a corrupted layout that should be reset to defaults and backed up.
                        var structuralCorruption = hasCurrent &&
                                                   HasBrokenComplexChildren(newDefaultElement, currentElement);

                        string finalCurrentElement;

                        if (!hasCurrent)
                        {
                            // Missing key in user's file -> inject new default element
                            finalCurrentElement = newDefaultElement;
                        }
                        else if (structuralCorruption)
                        {
                            // Complex nested object was flattened / corrupted -> restore default subtree and flag backup
                            finalCurrentElement = newDefaultElement;
                            requiresBackup = true;
                        }
                        else if (hasOldDefault &&
                                 currentElement == oldDefaultElement &&
                                 oldDefaultElement != newDefaultElement)
                        {
                            // User value is still exactly the old default element and the default changed:
                            // treat as unchanged by user and upgrade to the new default element.
                            finalCurrentElement = newDefaultElement;
                        }
                        else
                        {
                            // Keep whatever the user currently has (could be nil/number/nested block/etc.)
                            finalCurrentElement = currentElement;
                        }

                        normalizedCurrentChildren[key] = finalCurrentElement;
                        normalizedDefaultChildren[key] = newDefaultElement;
                    }

                    // Detect extra keys in current file that no longer exist in current layout
                    foreach (var kv in currentChildren)
                    {
                        if (!newDefaultChildren.ContainsKey(kv.Key))
                        {
                            requiresBackup = true;
                            break;
                        }
                    }

                    // Rebuild normalized XML using a canonical header + namespaces for all configs
                    var normalizedCurrentXml = LayoutXml.Build(rootName, normalizedCurrentChildren);
                    var normalizedDefaultsXml = LayoutXml.Build(rootName, normalizedDefaultChildren);

                    result.NormalizedXml = normalizedCurrentXml;
                    result.NormalizedDefaultsXml = normalizedDefaultsXml;
                    result.RequiresBackup = requiresBackup;

                    return result;
                }
                catch
                {
                    // On any error, fall back to original "current" XML and no backup recommendation.
                    result.NormalizedXml = xmlCurrentFromFile;
                    result.NormalizedDefaultsXml = xmlCurrentDefaults;
                    result.RequiresBackup = false;
                    return result;
                }
            }

            /// <summary>
            /// Detects structural corruption for a single element:
            /// If in the default element a direct child is a complex object (it has its own children),
            /// but in the current element that same child is missing or has no children (flattened to scalar),
            /// we treat this as a corrupted layout.
            /// 
            /// This is generic and works for things like:
            /// - Settings.Display (complex in defaults) -> turned into scalar text in current
            /// - NamedValues (dictionary) -> turned into scalar text in current
            /// </summary>
            private static bool HasBrokenComplexChildren(string defaultElement, string currentElement)
            {
                if (string.IsNullOrEmpty(defaultElement) || string.IsNullOrEmpty(currentElement))
                    return false;

                string defaultRootName;
                var defaultChildren = LayoutXml.ParseChildren(defaultElement, out defaultRootName);
                if (defaultChildren.Count == 0)
                    return false; // no expectations about nested structure

                string currentRootName;
                var currentChildren = LayoutXml.ParseChildren(currentElement, out currentRootName);

                foreach (var kv in defaultChildren)
                {
                    var childName = kv.Key;
                    var defaultChildBlock = kv.Value;

                    // Look at the default child's children to see if it's complex
                    string dummy;
                    var defaultGrandChildren = LayoutXml.ParseChildren(defaultChildBlock, out dummy);

                    // Only care about children that are complex in defaults
                    if (defaultGrandChildren.Count == 0)
                        continue;

                    // In current: if the complex child is missing or has been flattened, that's corruption
                    string currentChildBlock;
                    if (!currentChildren.TryGetValue(childName, out currentChildBlock))
                    {
                        // Complex child missing entirely
                        return true;
                    }

                    var currentGrandChildren = LayoutXml.ParseChildren(currentChildBlock, out dummy);
                    if (currentGrandChildren.Count == 0)
                    {
                        // Child exists but has no children -> likely scalar or empty, not complex as required
                        return true;
                    }
                }

                return false;
            }
    }
}
