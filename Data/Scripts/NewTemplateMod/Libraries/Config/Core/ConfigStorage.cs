using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
    public static class ConfigStorage
    {
        private static IConfigFileSystem _fileSystem;
        private static IConfigSerializer _serializer;

        // TypeName -> definition (for metadata + default instance)
        private static readonly Dictionary<string, IConfigDefinition> _definitions =
            new Dictionary<string, IConfigDefinition>();

        // Location -> (TypeName -> Slot)
        private static readonly Dictionary<ConfigLocationType, Dictionary<string, ConfigSlot>> _slots =
            new Dictionary<ConfigLocationType, Dictionary<string, ConfigSlot>>();

        private static bool _initialized;

        public static void Initialize(IConfigFileSystem fileSystem, IConfigSerializer serializer)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (serializer == null) throw new ArgumentNullException("serializer");

            _fileSystem = fileSystem;
            _serializer = serializer;

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

        /// <summary>
        /// Register a config type for a given location. File name defaults to
        /// the file system's default file name (usually TypeNameDefault.toml).
        /// </summary>
        public static void Register<T>(ConfigLocationType location)
            where T : ConfigBase, new()
        {
            Register<T>(location, null);
        }

        /// <summary>
        /// Register a config type for a given location and initial file name.
        /// CurrentFileName is set to user-provided name or default; if the file
        /// exists, ConfigStorage will try to load it once at registration time.
        /// </summary>
        public static void Register<T>(ConfigLocationType location, string initialFileName)
            where T : ConfigBase, new()
        {
            EnsureInitialized();

            var typeName = typeof(T).Name;

            IConfigDefinition def;
            if (!_definitions.TryGetValue(typeName, out def))
            {
                // Section name for TOML is also type name for now.
                def = new ConfigDefinition<T>(typeName);
                _definitions[typeName] = def;
            }
            else if (def.ConfigType != typeof(T))
            {
                throw new InvalidOperationException(
                    "Config type name '" + typeName + "' is already registered with a different type.");
            }

            var slot = GetOrCreateSlot(location, typeName, def, initialFileName);

            // If slot.Instance is null, we either failed to load or there was no file;
            // keep it lazy: we will create default on first GetOrCreate/Save/Load success.
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
                throw new ArgumentNullException("typeName");

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            return slot.CurrentFileName;
        }

        public static void SetCurrentFileName(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);
            slot.CurrentFileName = fileName;
        }

        public static bool Load(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            string content;
            if (!_fileSystem.TryReadFile(location, fileName, out content))
                return false;

            content = TryNormalizeConfigFile(location, fileName, def, content);

            var config = _serializer.Deserialize(def, content);
            if (config == null)
                return false;

            slot.Instance = config;
            slot.CurrentFileName = fileName;
            return true;
        }

        public static bool Save(ConfigLocationType location, string typeName, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            if (slot.Instance == null)
            {
                slot.Instance = def.CreateDefaultInstance();
            }

            var content = _serializer.Serialize(slot.Instance);
            _fileSystem.WriteFile(location, fileName, content);
            slot.CurrentFileName = fileName;

            return true;
        }

        public static string GetConfigAsText(ConfigLocationType location, string typeName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");

            var def = GetDefinitionByTypeName(typeName);
            var slot = GetOrCreateSlot(location, typeName, def, null);

            if (slot.Instance == null)
            {
                slot.Instance = def.CreateDefaultInstance();
            }

            return _serializer.Serialize(slot.Instance);
        }

        public static string GetFileAsText(ConfigLocationType location, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            string content;
            if (_fileSystem.TryReadFile(location, fileName, out content))
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
                // Already exists; Register<T> is idempotent. Do not override current filename here.
                return slot;
            }

            slot = new ConfigSlot();
            slot.TypeName = typeName;

            string fileName;
            if (!string.IsNullOrEmpty(initialFileNameOverride))
            {
                fileName = initialFileNameOverride;
            }
            else
            {
                fileName = _fileSystem.GetDefaultFileName(def);
            }

            slot.CurrentFileName = fileName;

            // Attempt to load existing file once
            string content;
            if (_fileSystem.TryReadFile(location, fileName, out content))
            {
                var config = _serializer.Deserialize(def, content);
                if (config != null)
                {
                    slot.Instance = config;
                }
            }

            // If no file or deserialization failed, Instance stays null.
            byType[typeName] = slot;
            return slot;
        }

        private static string TryNormalizeConfigFile(
            ConfigLocationType location,
            string fileName,
            IConfigDefinition def,
            string originalContent)
        {
            // If anything goes wrong, just fall back to original content.
            try
            {
                var fileModel = _serializer.ParseToModel(originalContent);
                var defaultModel = _serializer.BuildDefaultModel(def);

                if (fileModel == null || defaultModel == null)
                    return originalContent;

                if (!string.IsNullOrEmpty(fileModel.TypeName) &&
                    !string.Equals(fileModel.TypeName, def.TypeName, StringComparison.OrdinalIgnoreCase))
                {
                    // Different section/type, do not touch.
                    return originalContent;
                }

                var hasExtraKeys = false;

                // Detect extra keys
                foreach (var kv in fileModel.Entries)
                {
                    if (!defaultModel.Entries.ContainsKey(kv.Key))
                    {
                        hasExtraKeys = true;
                    }
                }

                // Merge known keys from file into default model
                foreach (var kv in defaultModel.Entries)
                {
                    var key = kv.Key;
                    var defaultEntry = kv.Value;

                    ITomlEntry fileEntry;
                    if (fileModel.Entries.TryGetValue(key, out fileEntry))
                    {
                        // If value != its own default in the file, user changed it -> carry over.
                        if (!string.IsNullOrEmpty(fileEntry.DefaultValue) &&
                            fileEntry.Value != fileEntry.DefaultValue)
                        {
                            defaultEntry.Value = fileEntry.Value;
                        }
                        // else: user left it at default; keep defaultEntry.Value (current default).
                    }
                    else
                    {
                        // Missing key: keep defaultEntry.Value (current default).
                    }
                }

                var normalized = _serializer.SerializeModel(defaultModel);

                // Backup only if there were extra keys.
                if (hasExtraKeys)
                {
                    var backupName = fileName + ".bak";
                    _fileSystem.WriteFile(location, backupName, originalContent);
                }

                // Always write normalized file (handles missing keys etc.).
                _fileSystem.WriteFile(location, fileName, normalized);

                return normalized;
            }
            catch
            {
                return originalContent;
            }
        }
    }
}
