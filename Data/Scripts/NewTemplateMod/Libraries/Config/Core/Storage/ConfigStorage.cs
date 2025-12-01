using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.Layout;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Domain;

namespace mz.Config.Core.Storage
{
    /// <summary>
    /// Static facade for config management:
    /// - registration by type + location
    /// - load / save through XML + converter + layout migrator
    /// - keeps one in-memory instance per (type, location)
    /// </summary>
    public static class ConfigStorage
    {
        private static IConfigFileSystem _fileSystem;
        private static IConfigXmlSerializer _xmlSerializer;
        private static IConfigLayoutMigrator _layoutMigrator;
        private static IXmlConverter _xmlConverter;

        // TypeName -> definition (for metadata + default instance)
        private static readonly Dictionary<string, IConfigDefinition> _definitions =
            new Dictionary<string, IConfigDefinition>();

        // Location -> (TypeName -> Slot)
        private static readonly Dictionary<ConfigLocationType, Dictionary<string, ConfigSlot>> _slots =
            new Dictionary<ConfigLocationType, Dictionary<string, ConfigSlot>>();

        private static bool _initialized;

        /// <summary>
        /// Full initialization: caller must provide all dependencies.
        /// For XML-only mode, pass an IdentityXmlConverter instance.
        /// </summary>
        public static void Initialize(
            IConfigFileSystem fileSystem,
            IConfigXmlSerializer xmlSerializer,
            IConfigLayoutMigrator layoutMigrator,
            IXmlConverter xmlConverter)
        {
            if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
            if (xmlSerializer == null) throw new ArgumentNullException(nameof(xmlSerializer));
            if (layoutMigrator == null) throw new ArgumentNullException(nameof(layoutMigrator));
            if (xmlConverter == null) throw new ArgumentNullException(nameof(xmlConverter));

            _fileSystem = fileSystem;
            _xmlSerializer = xmlSerializer;
            _layoutMigrator = layoutMigrator;
            _xmlConverter = xmlConverter;

            _definitions.Clear();
            _slots.Clear();

            _initialized = true;
        }

        /// <summary>
        /// Convenience initialization for XML-only configs. Uses IdentityXmlConverter.
        /// </summary>
        public static void InitializeXmlOnly(IConfigFileSystem fileSystem, IConfigXmlSerializer xmlSerializer)
        {
            var layoutMigrator = new ConfigLayoutMigrator();
            var converter = new IdentityXmlConverter();
            Initialize(fileSystem, xmlSerializer, layoutMigrator, converter);
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("ConfigStorage.Initialize must be called before use.");
        }

        // ----------------- registration -----------------

        public static void Register<T>(ConfigLocationType location)
            where T : ConfigBase, new()
        {
            Register<T>(location, null);
        }

        public static void Register<T>(ConfigLocationType location, string initialFileName)
            where T : ConfigBase, new()
        {
            EnsureInitialized();

            var typeName = typeof(T).Name;

            IConfigDefinition def;
            if (!_definitions.TryGetValue(typeName, out def))
            {
                def = new ConfigDefinition<T>();
                _definitions[typeName] = def;
            }
            else if (def.ConfigType != typeof(T))
            {
                throw new InvalidOperationException(
                    "Config type name '" + typeName + "' is already registered with a different type.");
            }

            // Just create the slot & remember the filename.
            // We no longer auto-load here; Load is explicit / lazy.
            GetOrCreateSlot(location, typeName, def, initialFileName);
        }

        // ----------------- public API -----------------

        public static T GetOrCreate<T>(ConfigLocationType location)
            where T : ConfigBase, new()
        {
            EnsureInitialized();

            var typeName = typeof(T).Name;
            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            if (slot.Instance == null)
            {
                slot.Instance = def.CreateDefaultInstance();
            }

            return (T)slot.Instance;
        }

        public static string GetCurrentFileName(ConfigLocationType location, string typeName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            return slot.CurrentFileName;
        }

        public static void SetCurrentFileName(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(_xmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - _xmlConverter.GetExtension.Length);
            
            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);
            slot.CurrentFileName = fileName + _xmlConverter.GetExtension;
        }

        public static bool Load(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(_xmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - _xmlConverter.GetExtension.Length);
                
            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            // 1) Read current external config
            string externalCurrent;
            if (!_fileSystem.TryReadFile(location, fileName + _xmlConverter.GetExtension, out externalCurrent))
                return false;

            // 2) Read old defaults external (sidecar) if present
            var defaultsFileName = fileName + ".defaults";
            string externalOldDefaults;
            var hasOldDefaults = _fileSystem.TryReadFile(location, defaultsFileName + _xmlConverter.GetExtension, out externalOldDefaults);

            // 3) Build current defaults XML from code
            var currentDefaultInstance = def.CreateDefaultInstance();
            var xmlCurrentDefaults = _xmlSerializer.SerializeToXml(currentDefaultInstance);

            // 4) Convert external -> internal XML
            var xmlCurrentFromFile = _xmlConverter.ToInternal(def, externalCurrent);
            var xmlOldDefaultsFromFile = hasOldDefaults
                ? _xmlConverter.ToInternal(def, externalOldDefaults)
                : string.Empty;

            // 5) Normalize layout
            var layoutResult = _layoutMigrator.Normalize(
                def,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                xmlCurrentDefaults);

            // 6) Backup if necessary
            if (layoutResult.RequiresBackup)
            {
                var backupName = fileName + ".bak";
                _fileSystem.WriteFile(location, backupName + _xmlConverter.GetExtension, externalCurrent);
            }

            // 7) Convert normalized XML back to external and overwrite both files
            var normalizedExternalCurrent = _xmlConverter.ToExternal(def, layoutResult.NormalizedXml);
            _fileSystem.WriteFile(location, fileName + _xmlConverter.GetExtension, normalizedExternalCurrent);

            var normalizedExternalDefaults = _xmlConverter.ToExternal(def, layoutResult.NormalizedDefaultsXml);
            _fileSystem.WriteFile(location, defaultsFileName + _xmlConverter.GetExtension, normalizedExternalDefaults);

            // 8) Deserialize config from normalized current XML
            var config = def.DeserializeFromXml(_xmlSerializer, layoutResult.NormalizedXml);
            if (config == null)
                return false;

            slot.Instance = config;
            slot.CurrentFileName = fileName + _xmlConverter.GetExtension;
            return true;
        }

        public static bool Save(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(_xmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - _xmlConverter.GetExtension.Length);
            
            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            if (slot.Instance == null)
            {
                slot.Instance = def.CreateDefaultInstance();
            }

            // Object -> XML (current)
            var xmlCurrent = _xmlSerializer.SerializeToXml(slot.Instance);

            // Current defaults XML from code
            var currentDefaultInstance = def.CreateDefaultInstance();
            var xmlDefaults = _xmlSerializer.SerializeToXml(currentDefaultInstance);

            // Convert both XML blobs to external format
            var externalCurrent = _xmlConverter.ToExternal(def, xmlCurrent);
            var externalDefaults = _xmlConverter.ToExternal(def, xmlDefaults);

            _fileSystem.WriteFile(location, fileName + _xmlConverter.GetExtension, externalCurrent);
            _fileSystem.WriteFile(location, fileName + ".defaults" + _xmlConverter.GetExtension, externalDefaults);

            slot.CurrentFileName = fileName + _xmlConverter.GetExtension;

            return true;
        }

        public static string GetConfigAsText(ConfigLocationType location, string typeName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            if (slot.Instance == null)
            {
                slot.Instance = def.CreateDefaultInstance();
            }

            // Serialize current instance to XML, then convert to external format
            var xml = _xmlSerializer.SerializeToXml(slot.Instance);
            var external = _xmlConverter.ToExternal(def, xml);
            return external;
        }

        public static string GetFileAsText(ConfigLocationType location, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(_xmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - _xmlConverter.GetExtension.Length);
            
            string content;
            if (_fileSystem.TryReadFile(location, fileName + _xmlConverter.GetExtension, out content))
                return content;

            return null;
        }

        // ----------------- internal helpers -----------------

        private static IConfigDefinition GetDefinitionByTypeName(string typeName)
        {
            IConfigDefinition def;
            if (!_definitions.TryGetValue(typeName, out def))
            {
                throw new InvalidOperationException(
                    "No config definition registered for type name: " + typeName);
            }
            return def;
        }

        private static ConfigSlot GetOrCreateSlot(
            ConfigLocationType location,
            string typeName,
            IConfigDefinition def,
            string initialFileNameOverride)
        {
            Dictionary<string, ConfigSlot> byType;
            if (!_slots.TryGetValue(location, out byType))
            {
                byType = new Dictionary<string, ConfigSlot>();
                _slots[location] = byType;
            }

            ConfigSlot slot;
            if (byType.TryGetValue(typeName, out slot))
            {
                return slot;
            }

            slot = new ConfigSlot();
            slot.TypeName = typeName;

            string fileName;
            if (!string.IsNullOrEmpty(initialFileNameOverride))
            {
                fileName = initialFileNameOverride;
                if (fileName.EndsWith(_xmlConverter.GetExtension))
                    fileName = fileName.Remove(fileName.Length - _xmlConverter.GetExtension.Length);
            }
            else
            {
                fileName = typeName + "Default";
            }

            slot.CurrentFileName = fileName + _xmlConverter.GetExtension;

            byType[typeName] = slot;
            return slot;
        }
    }
}
