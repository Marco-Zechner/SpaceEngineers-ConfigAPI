using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Layout;
using mz.Config.Abstractions.SE;
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
    /// It knows nothing about TOML; it only deals with XML strings
    /// produced/consumed by the XML serializer.
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

                var normalizedCurrent = new Dictionary<string, string>();
                var normalizedDefaults = new Dictionary<string, string>();
                var requiresBackup = false;

                // 2) For every key in the *current* default layout:
                //    - decide what the user's current value should be
                //    - define what the stored default should be
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

                // 3) Detect extra keys in the file that no longer exist in current layout
                foreach (var kv in currentValues)
                {
                    if (!newDefaultValues.ContainsKey(kv.Key))
                    {
                        // Unknown key -> it will be dropped -> we recommend a backup
                        requiresBackup = true;
                    }
                }

                // 4) Build normalized XML strings
                var typeName = definition.TypeName;

                var normalizedCurrentXml = SimpleXml.BuildSimpleXml(typeName, normalizedCurrent);
                var normalizedDefaultsXml = SimpleXml.BuildSimpleXml(typeName, normalizedDefaults);

                // 5) Populate result
                result.NormalizedXml = normalizedCurrentXml;
                result.NormalizedDefaultsXml = normalizedDefaultsXml;
                result.RequiresBackup = requiresBackup;

                return result;
            }
            catch
            {
                // On any error, fall back to the original "current" XML and no backup.
                result.NormalizedXml = xmlCurrentFromFile;
                result.NormalizedDefaultsXml = xmlCurrentDefaults;
                result.RequiresBackup = false;
                return result;
            }
        }
    }
}
