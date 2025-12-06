using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.Layout;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;

namespace mz.Config.Core.Storage
{
    /// <summary>
    /// Static facade for config management:
    /// - registration by type + location
    /// - load / save through XML + converter + layout migrator
    /// - keeps one in-memory instance per (type, location)
    /// </summary>
    public static class InternalConfigStorage
    {
        public static IConfigFileSystem FileSystem
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("ConfigStorage was not initialized");
                return _fileSystem;
            }
        }

        public static IConfigXmlSerializer XmlSerializer
        {   
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("ConfigStorage was not initialized");
                return _xmlSerializer;
            }
        }

        public static IConfigLayoutMigrator LayoutMigrator
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("ConfigStorage was not initialized");
                return _layoutMigrator;
            }
        }

        public static IXmlConverter XmlConverter
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("ConfigStorage was not initialized");
                return _xmlConverter;
            }
        }

        public static string StoragePrefix 
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("ConfigStorage was not initialized");
                return _storagePrefix;
            }
        }
        public static bool IsInitialized => _initialized;
        
        // TypeName -> definition (for metadata + default instance)
        private static readonly Dictionary<string, IConfigDefinition> _definitions =
            new Dictionary<string, IConfigDefinition>();

        // Location -> (TypeName -> Slot)
        private static readonly Dictionary<ConfigLocationType, Dictionary<string, ConfigSlot>> _slots =
            new Dictionary<ConfigLocationType, Dictionary<string, ConfigSlot>>();

        private static bool _initialized;
        private static IConfigFileSystem _fileSystem;
        private static IConfigXmlSerializer _xmlSerializer;
        private static IConfigLayoutMigrator _layoutMigrator;
        private static IXmlConverter _xmlConverter;
        private static string _storagePrefix;

        /// <summary>
        /// Full initialization: caller must provide all dependencies.
        /// For XML-only mode, pass an IdentityXmlConverter instance.
        /// </summary>
        public static void Initialize(
            IConfigFileSystem fileSystem,
            IConfigXmlSerializer xmlSerializer,
            IConfigLayoutMigrator layoutMigrator,
            IXmlConverter xmlConverter,
            string storagePrefix)
        {
            if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
            if (xmlSerializer == null) throw new ArgumentNullException(nameof(xmlSerializer));
            if (layoutMigrator == null) throw new ArgumentNullException(nameof(layoutMigrator));
            if (xmlConverter == null) throw new ArgumentNullException(nameof(xmlConverter));

            _fileSystem = fileSystem;
            _xmlSerializer = xmlSerializer;
            _layoutMigrator = layoutMigrator;
            _xmlConverter = xmlConverter;
            _storagePrefix = storagePrefix;

            _definitions.Clear();
            _slots.Clear();

            _initialized = true;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("ConfigStorage.Initialize must be called before use.");
        }

        // ----------------- registration -----------------

        public static T Register<T>(ConfigLocationType location, string initialFileName = null)
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

            ConfigStorage.Debug?.Log("Registered config type: " + typeName + " at location: " + location, "InternalConfigStorage.Register");
            // Create the slot & remember the filename (or leave it).
            GetOrCreateSlot(location, typeName, initialFileName);

            // Return the current in-memory instance (default if none yet).
            return GetOrCreate<T>(location);
        }


        // ----------------- public API -----------------

        public static T GetOrCreate<T>(ConfigLocationType location)
            where T : ConfigBase, new()
        {
            EnsureInitialized();

            var typeName = typeof(T).Name;
            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, null);

            if (slot.Instance != null) return (T)slot.Instance;
            
            ConfigStorage.Debug?.Log("Creating default instance for type: " + typeName + " at location: " + location, "InternalConfigStorage.GetOrCreate");
            slot.Instance = def.CreateDefaultInstance();

            return (T)slot.Instance;
        }

        public static string GetCurrentFileName(ConfigLocationType location, string typeName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            var slot = GetOrCreateSlot(location, typeName, null);

            return slot.CurrentFileName;
        }

        public static void SetCurrentFileName(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(XmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - XmlConverter.GetExtension.Length);
            
            var slot = GetOrCreateSlot(location, typeName, null);
            slot.CurrentFileName = fileName + XmlConverter.GetExtension;
        }

        private static bool HasConfigVersionElement(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return false;

            string rootName;
            var children = LayoutXml.ParseChildren(xml, out rootName);
            // We don't care about the value, only that the element exists.
            return children != null && children.ContainsKey("ConfigVersion");
        }
        
        public static void Load(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(XmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - XmlConverter.GetExtension.Length);

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, null);

            var fullName = fileName + XmlConverter.GetExtension;
            var defaultsFileName = slot.TypeName + ".defaults";
            var fullDefaultsName = defaultsFileName + XmlConverter.GetExtension;

            // 1) Try to read current external config
            string externalCurrent;
            var hasCurrent = FileSystem.TryReadFile(location, fullName, out externalCurrent);

            if (!hasCurrent)
            {
                // No file yet: create a fresh one based on current defaults
                ConfigStorage.Debug?.Log(
                    "Config file not found for type: " + typeName + " at location: " + location +
                    " with file name: " + fullName + " -> creating new default config.",
                    "InternalConfigStorage.Load");

                var defaultInstance = def.CreateDefaultInstance();
                var defaultXml = XmlSerializer.SerializeToXml(defaultInstance);
                ConfigStorage.Debug?.Log("defaultXML:\n" + defaultXml,
                    "InternalConfigStorage.Load");
                
                // Convert to external format
                var externalDefault = XmlConverter.ToExternal(def, defaultXml);
                ConfigStorage.Debug?.Log("externalDefault:\n" + externalDefault,
                    "InternalConfigStorage.Load");
                
                // Write main + defaults (identical on first creation)
                FileSystem.WriteFile(location, fullName, externalDefault);
                FileSystem.WriteFile(location, fullDefaultsName, externalDefault);

                ConfigStorage.Debug?.Log(
                    "Wrote new config and defaults for type: " + typeName +
                    "\nCurrent/Defaults path: " + fullName + " / " + fullDefaultsName +
                    "\nContent:\n" + externalDefault,
                    "InternalConfigStorage.Load");

                // Deserialize into memory
                var config = def.DeserializeFromXml(XmlSerializer, defaultXml);
                if (config == null)
                {
                    throw new InvalidOperationException(
                        "Failed to deserialize newly created default config for type: " + typeName + "\nHOW?");
                }

                slot.Instance = config;
                slot.CurrentFileName = fullName;
                return;
            }

            ConfigStorage.Debug?.Log(
                "Read current config file for type: " + typeName + " with content:\n" + externalCurrent,
                "InternalConfigStorage.Load");

            // 2) Read old defaults external (sidecar) if present
            string externalOldDefaults;
            var hasOldDefaults = FileSystem.TryReadFile(location, fullDefaultsName, out externalOldDefaults);
            ConfigStorage.Debug?.Log(
                "Defaults file " + (hasOldDefaults ? "found" : "not found") + " for type: " + typeName +
                (hasOldDefaults ? " with content:\n" + externalOldDefaults : string.Empty),
                "InternalConfigStorage.Load");

            // 3) Build current defaults XML from code
            var currentDefaultInstance = def.CreateDefaultInstance();
            var xmlCurrentDefaults = XmlSerializer.SerializeToXml(currentDefaultInstance);
            ConfigStorage.Debug?.Log(
                "Current defaults XML for type: " + typeName + ":\n" + xmlCurrentDefaults,
                "InternalConfigStorage.Load");

            // 4) Convert external -> internal XML
            var xmlCurrentFromFile = XmlConverter.ToInternal(def, externalCurrent);
            var xmlOldDefaultsFromFile = hasOldDefaults
                ? XmlConverter.ToInternal(def, externalOldDefaults)
                : string.Empty;

            ConfigStorage.Debug?.Log(
                "Converted current XML for type: " + typeName + ":\n" + xmlCurrentFromFile,
                "InternalConfigStorage.Load");

            // 4a) Hard validity check: ConfigVersion element must exist.
            // If it's missing, the file is considered garbage (e.g. "garbage!" TOML
            // that parsed into an empty <TypeName/> root). We backup the original
            // external file and regenerate from current defaults.
            if (!HasConfigVersionElement(xmlCurrentFromFile))
            {
                var backupName = fileName + ".bak" + XmlConverter.GetExtension;
                FileSystem.WriteFile(location, backupName, externalCurrent);
                ConfigStorage.Debug?.Log(
                    "Missing <ConfigVersion> element for type: " + typeName +
                    ". Treating file as invalid, backing up to: " + backupName +
                    " and regenerating from defaults.",
                    "InternalConfigStorage.Load");

                var defaultInstance = def.CreateDefaultInstance();
                var defaultXml = XmlSerializer.SerializeToXml(defaultInstance);
                var externalDefault = XmlConverter.ToExternal(def, defaultXml);

                // Overwrite current and defaults
                FileSystem.WriteFile(location, fullName, externalDefault);
                FileSystem.WriteFile(location, fullDefaultsName, externalDefault);

                ConfigStorage.Debug?.Log(
                    "Wrote new config and defaults for type: " + typeName +
                    "\nCurrent/Defaults path: " + fullName + " / " + fullDefaultsName +
                    "\nContent:\n" + externalDefault,
                    "InternalConfigStorage.Load");

                var config = def.DeserializeFromXml(XmlSerializer, defaultXml);
                if (config == null)
                {
                    throw new InvalidOperationException(
                        "Failed to deserialize regenerated default config for type: " + typeName);
                }

                slot.Instance = config;
                slot.CurrentFileName = fullName;
                return;
            }

            // 5) Normalize layout
            var layoutResult = LayoutMigrator.Normalize(
                def,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                xmlCurrentDefaults);

            ConfigStorage.Debug?.Log(
                "Layout normalization result for type: " + typeName +
                "\nNormalized XML:\n" + layoutResult.NormalizedXml +
                "\nNormalized Defaults XML:\n" + layoutResult.NormalizedDefaultsXml +
                "\nRequires Backup: " + layoutResult.RequiresBackup,
                "InternalConfigStorage.Load");

            // 6) Backup if necessary
            if (layoutResult.RequiresBackup)
            {
                var backupName = fileName + ".bak" + XmlConverter.GetExtension;
                FileSystem.WriteFile(location, backupName, externalCurrent);
                ConfigStorage.Debug?.Log(
                    "Backup created for type: " + typeName + " at: " + backupName +
                    " with content:\n" + externalCurrent,
                    "InternalConfigStorage.Load");
            }

            // 7) Convert normalized XML back to external and overwrite both files
            var normalizedExternalCurrent = XmlConverter.ToExternal(def, layoutResult.NormalizedXml);
            FileSystem.WriteFile(location, fullName, normalizedExternalCurrent);
            ConfigStorage.Debug?.Log(
                "Wrote normalized current config for type: " + typeName + " at: " + fullName +
                " with content:\n" + normalizedExternalCurrent,
                "InternalConfigStorage.Load");

            var normalizedExternalDefaults = XmlConverter.ToExternal(def, layoutResult.NormalizedDefaultsXml);
            FileSystem.WriteFile(location, fullDefaultsName, normalizedExternalDefaults);
            ConfigStorage.Debug?.Log(
                "Wrote normalized defaults config for type: " + typeName + " at: " + fullDefaultsName +
                " with content:\n" + normalizedExternalDefaults,
                "InternalConfigStorage.Load");

            // 8) Deserialize config from normalized current XML
            var finalConfig = def.DeserializeFromXml(XmlSerializer, layoutResult.NormalizedXml);
            if (finalConfig == null)
            {
                ConfigStorage.Debug?.Log("Deserialization failed for type: " + typeName +
                                         " after normalization. Using default instance.", "InternalConfigStorage.Load");
                
                var backupName = fileName + ".bak" + XmlConverter.GetExtension;
                FileSystem.WriteFile(location, backupName, externalCurrent);
                ConfigStorage.Debug?.Log(
                    "Backup created for type: " + typeName + " at: " + backupName +
                    " with content:\n" + externalCurrent,
                    "InternalConfigStorage.Load");
                
                finalConfig = def.CreateDefaultInstance();
                var finalXml = XmlSerializer.SerializeToXml(finalConfig);
                var finalExternal = XmlConverter.ToExternal(def, finalXml);
                FileSystem.WriteFile(location, fullName, finalExternal);
                ConfigStorage.Debug?.Log(
                    "Wrote new config with default values for type: " + typeName + " at: " + fullName +
                    " with content:\n" + finalExternal,
                    "InternalConfigStorage.Load");
            }

            slot.Instance = finalConfig;
            slot.CurrentFileName = fullName;
        }


        public static bool Save(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(XmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - XmlConverter.GetExtension.Length);
            
            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, null);

            if (slot.Instance == null)
            {
                ConfigStorage.Debug?.Log("Creating default instance for type: " + typeName + " at location: " + location, "InternalConfigStorage.Save");
                slot.Instance = def.CreateDefaultInstance();
            }

            // Object -> XML (current)
            var xmlCurrent = XmlSerializer.SerializeToXml(slot.Instance);
            ConfigStorage.Debug?.Log("Serialized current instance to XML for type: " + typeName + ":\n" + xmlCurrent, "InternalConfigStorage.Save");
            
            // Current defaults XML from code
            var currentDefaultInstance = def.CreateDefaultInstance();
            var xmlDefaults = XmlSerializer.SerializeToXml(currentDefaultInstance);
            ConfigStorage.Debug?.Log("Serialized current default instance to XML for type: " + typeName + ":\n" + xmlDefaults, "InternalConfigStorage.Save");

            // Convert both XML blobs to external format
            var externalCurrent = XmlConverter.ToExternal(def, xmlCurrent);
            ConfigStorage.Debug?.Log("Converted current XML to external format for type: " + typeName + ":\n" + externalCurrent, "InternalConfigStorage.Save");
            var externalDefaults = XmlConverter.ToExternal(def, xmlDefaults);
            ConfigStorage.Debug?.Log("Converted defaults XML to external format for type: " + typeName + ":\n" + externalDefaults, "InternalConfigStorage.Save");

            FileSystem.WriteFile(location, fileName + XmlConverter.GetExtension, externalCurrent);
            ConfigStorage.Debug?.Log("Wrote current config file for type: " + typeName + " at: " + fileName + XmlConverter.GetExtension + " with content:\n" + externalCurrent, "InternalConfigStorage.Save");
            FileSystem.WriteFile(location, fileName + ".defaults" + XmlConverter.GetExtension, externalDefaults);
            ConfigStorage.Debug?.Log("Wrote defaults config file for type: " + typeName + " at: " + fileName + ".defaults" + XmlConverter.GetExtension + " with content:\n" + externalDefaults, "InternalConfigStorage.Save");

            slot.CurrentFileName = fileName + XmlConverter.GetExtension;

            return true;
        }

        public static string GetConfigAsText(ConfigLocationType location, string typeName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, null);

            if (slot.Instance == null)
            {
                slot.Instance = def.CreateDefaultInstance();
            }

            // Serialize current instance to XML, then convert to external format
            var xml = XmlSerializer.SerializeToXml(slot.Instance);
            var external = XmlConverter.ToExternal(def, xml);
            return external;
        }

        public static string GetFileAsText(ConfigLocationType location, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (fileName.EndsWith(XmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - XmlConverter.GetExtension.Length);
            
            string content;
            if (FileSystem.TryReadFile(location, fileName + XmlConverter.GetExtension, out content))
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
                ConfigStorage.Debug?.Log("Found existing slot for type: " + typeName + " at location: " + location, "InternalConfigStorage.GetOrCreateSlot");
                return slot;
            }

            slot = new ConfigSlot
            {
                TypeName = typeName
            };

            var fileName = !string.IsNullOrEmpty(initialFileNameOverride) ? initialFileNameOverride : typeName;
            if (fileName.EndsWith(XmlConverter.GetExtension))
                fileName = fileName.Remove(fileName.Length - XmlConverter.GetExtension.Length);

            ConfigStorage.Debug?.Log("Creating new slot for type: " + typeName + " at location: " + location +
                                     " with file name: " + fileName + XmlConverter.GetExtension, "InternalConfigStorage.GetOrCreateSlot");
            slot.CurrentFileName = fileName + XmlConverter.GetExtension;

            byType[typeName] = slot;
            return slot;
        }
    }
}
