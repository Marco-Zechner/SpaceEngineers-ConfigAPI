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

                var normalizedCurrentChildren  = new Dictionary<string, string>();
                var normalizedDefaultChildren  = new Dictionary<string, string>();
                var requiresBackup             = false;

                // ------------------------------------------------------------
                // Canonical order: ALWAYS follow xmlCurrentDefaults order.
                // newDefaultChildren comes from LayoutXml.ParseChildren(xmlCurrentDefaults),
                // which reflects whatever order XmlSerializer uses for this type.
                //
                // For each key:
                //   - if missing in current file  -> inject new default
                //   - else if value == oldDefault and default changed -> upgrade
                //   - else keep user value
                // ------------------------------------------------------------
                foreach (var kv in newDefaultChildren)
                {
                    var key             = kv.Key;
                    var newDefaultElem  = kv.Value;

                    string currentElem;
                    var hasCurrent      = currentChildren.TryGetValue(key, out currentElem);

                    string oldDefaultElem;
                    var hasOldDefault   = oldDefaultChildren.TryGetValue(key, out oldDefaultElem);

                    string finalCurrentElem;

                    if (!hasCurrent)
                    {
                        // Missing key in user's file -> inject new default element
                        finalCurrentElem = newDefaultElem;
                    }
                    else if (hasOldDefault &&
                             currentElem == oldDefaultElem &&
                             oldDefaultElem != newDefaultElem)
                    {
                        // User value is still exactly the old default element and the default changed:
                        // treat as unchanged-by-user and upgrade to the new default.
                        finalCurrentElem = newDefaultElem;
                    }
                    else
                    {
                        // Keep whatever the user currently has (nil / number / nested block / etc.)
                        finalCurrentElem = currentElem;
                    }

                    normalizedCurrentChildren[key] = finalCurrentElem;
                    normalizedDefaultChildren[key] = newDefaultElem;
                }

                // ------------------------------------------------------------
                // Detect extra keys in current file that no longer exist
                // in the current layout -> require backup.
                // ------------------------------------------------------------
                foreach (var kv in currentChildren)
                {
                    if (!newDefaultChildren.ContainsKey(kv.Key))
                    {
                        requiresBackup = true;
                        break;
                    }
                }

                // Rebuild normalized XML using canonical header + namespaces
                var normalizedCurrentXml  = LayoutXml.Build(rootName, normalizedCurrentChildren);
                var normalizedDefaultsXml = LayoutXml.Build(rootName, normalizedDefaultChildren);

                result.NormalizedXml        = normalizedCurrentXml;
                result.NormalizedDefaultsXml = normalizedDefaultsXml;
                result.RequiresBackup       = requiresBackup;

                return result;
            }
            catch
            {
                // On any error, fall back to original "current" XML and no backup recommendation.
                result.NormalizedXml        = xmlCurrentFromFile;
                result.NormalizedDefaultsXml = xmlCurrentDefaults;
                result.RequiresBackup       = false;
                return result;
            }
        }
    }
}
