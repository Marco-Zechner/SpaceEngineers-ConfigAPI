using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Layout;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Storage;
using mz.Config.Domain;

namespace mz.Config.Core.Layout
{
    /// <summary>
    /// Normalizes a config's XML layout against the current code layout:
    /// - adds missing keys with defaults
    /// - removes keys that no longer exist (signals backup)
    /// - keeps existing values where possible
    /// - upgrades values that are still equal to an old default to the new default
    ///
    /// Implementation is intentionally "dumb":
    /// - Works on flattened XML (SimpleXml) for simple configs (no nested '<' / '>' in values).
    /// - For complex configs (nested objects, collections, dictionaries, xsi:nil deep inside),
    ///   it leaves the XML unchanged and only uses SimpleXml to decide RequiresBackup.
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
                // 1) Parse all three XML blobs into key/value maps
                var currentValues = SimpleXml.ParseSimpleElements(xmlCurrentFromFile);
                var oldDefaultValues = SimpleXml.ParseSimpleElements(xmlOldDefaultsFromFile);
                var newDefaultValues = SimpleXml.ParseSimpleElements(xmlCurrentDefaults);

                // 2) Decide if layout is "simple" (no nested XML in values)
                var isSimpleLayout = IsSimpleLayout(newDefaultValues);

                if (!isSimpleLayout)
                {
                    // Complex layout: do not rebuild XML at all.
                    // Only detect removed keys for backup recommendation.
                    var requiresBackup = DetectRemovedKeys(currentValues, newDefaultValues);

                    result.NormalizedXml = xmlCurrentFromFile;
                    result.NormalizedDefaultsXml = xmlCurrentDefaults;
                    result.RequiresBackup = requiresBackup;
                    return result;
                }

                // Simple layout: use original string-based migration algorithm.

                var normalizedCurrent = new Dictionary<string, string>();
                var normalizedDefaults = new Dictionary<string, string>();
                var backupNeeded = false;

                // 3) For every key in the *current* default layout:
                foreach (var kv in newDefaultValues)
                {
                    var key = kv.Key;
                    var newDefault = kv.Value;

                    string currentValue;
                    var hasCurrent = currentValues.TryGetValue(key, out currentValue);

                    string oldDefault;
                    var hasOldDefault = oldDefaultValues.TryGetValue(key, out oldDefault);

                    string finalCurrent;

                    if (!hasCurrent)
                    {
                        // Missing key -> use new default
                        finalCurrent = newDefault;
                    }
                    else if (hasOldDefault &&
                             currentValue == oldDefault &&
                             oldDefault != newDefault)
                    {
                        // Value is still equal to an old default that changed:
                        // treat as unchanged by user and upgrade to new default.
                        finalCurrent = newDefault;
                    }
                    else
                    {
                        // Keep the user's current value
                        finalCurrent = currentValue;
                    }

                    normalizedCurrent[key] = finalCurrent;
                    normalizedDefaults[key] = newDefault;
                }

                // 4) Detect extra keys in the file that no longer exist in current layout
                backupNeeded = DetectRemovedKeys(currentValues, newDefaultValues);

                // 5) Build normalized XML strings
                var typeName = definition.TypeName;

                var normalizedCurrentXml = SimpleXml.BuildSimpleXml(typeName, normalizedCurrent);
                var normalizedDefaultsXml = SimpleXml.BuildSimpleXml(typeName, normalizedDefaults);

                // 6) Populate result
                result.NormalizedXml = normalizedCurrentXml;
                result.NormalizedDefaultsXml = normalizedDefaultsXml;
                result.RequiresBackup = backupNeeded;

                return result;
            }
            catch
            {
                // On any error, fall back to the original "current" XML and defaults XML.
                result.NormalizedXml = xmlCurrentFromFile;
                result.NormalizedDefaultsXml = xmlCurrentDefaults;
                result.RequiresBackup = false;
                return result;
            }
        }

        // -------- helper methods --------

        private static bool IsSimpleLayout(Dictionary<string, string> newDefaultValues)
        {
            if (newDefaultValues == null || newDefaultValues.Count == 0)
                return false;

            foreach (var kv in newDefaultValues)
            {
                var v = kv.Value;
                if (string.IsNullOrEmpty(v))
                    continue;

                // If any default value contains '<' or '>', we treat the layout as complex
                // (nested elements, dictionaries, collections, etc.).
                if (v.IndexOf('<') >= 0 || v.IndexOf('>') >= 0)
                    return false;
            }

            return true;
        }

        private static bool DetectRemovedKeys(
            Dictionary<string, string> currentValues,
            Dictionary<string, string> newDefaultValues)
        {
            if (currentValues == null || currentValues.Count == 0)
                return false;
            if (newDefaultValues == null || newDefaultValues.Count == 0)
                return false;

            foreach (var kv in currentValues)
            {
                if (!newDefaultValues.ContainsKey(kv.Key))
                {
                    // Unknown key in current file that does not exist in new defaults
                    // -> it will be dropped in a "pure" layout normalization
                    // -> recommend backup.
                    return true;
                }
            }

            return false;
        }
    }
}
