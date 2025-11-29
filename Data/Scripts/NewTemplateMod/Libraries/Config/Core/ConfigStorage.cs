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

        // key: id (usually typeof(T).Name or alias) -> definition
        private static readonly Dictionary<string, IConfigDefinition> _definitions =
            new Dictionary<string, IConfigDefinition>();

        // key: (location|id) -> current filename
        private static readonly Dictionary<string, string> _currentFiles =
            new Dictionary<string, string>();

        // key: (location|id) -> in-memory instance
        private static readonly Dictionary<string, ConfigBase> _instances =
            new Dictionary<string, ConfigBase>();

        // optional: track which ids are registered in which locations
        private static readonly Dictionary<string, HashSet<ConfigLocationType>> _registeredLocations =
            new Dictionary<string, HashSet<ConfigLocationType>>();

        private static bool _initialized;

        public static void Initialize(IConfigFileSystem fileSystem, IConfigSerializer serializer)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (serializer == null) throw new ArgumentNullException("serializer");

            _fileSystem = fileSystem;
            _serializer = serializer;

            _definitions.Clear();
            _currentFiles.Clear();
            _instances.Clear();
            _registeredLocations.Clear();

            _initialized = true;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("ConfigStorage.Initialize must be called before use.");
        }

        // ----------------- registration API -----------------

        public static void Register<T>(ConfigLocationType location) where T : ConfigBase, new()
        {
            Register<T>(location, null);
        }

        public static void Register<T>(ConfigLocationType location, string id) where T : ConfigBase, new()
        {
            EnsureInitialized();

            string typeName = typeof(T).Name;
            string keyId = string.IsNullOrEmpty(id) ? typeName : id;

            // Ensure we have a definition for this id
            IConfigDefinition def;
            if (!_definitions.TryGetValue(keyId, out def))
            {
                // section name for TOML – for now just type name or id; can adjust later
                string sectionName = keyId;
                def = new ConfigDefinition<T>(sectionName);
                _definitions[keyId] = def;
            }
            else
            {
                // sanity: make sure the existing definition matches the type
                if (def.ConfigType != typeof(T))
                    throw new InvalidOperationException(
                        "Config id '" + keyId + "' is already registered with a different type.");
            }

            HashSet<ConfigLocationType> locations;
            if (!_registeredLocations.TryGetValue(keyId, out locations))
            {
                locations = new HashSet<ConfigLocationType>();
                _registeredLocations[keyId] = locations;
            }

            locations.Add(location);

            // Lazily create default filename for this (location,id) pair
            string key = MakeKey(location, keyId);
            if (!_currentFiles.ContainsKey(key))
            {
                string defaultFileName = _fileSystem.GetDefaultFileName(def);
                _currentFiles[key] = defaultFileName;
            }
        }

        // For listing / commands:
        public static IConfigDefinition[] GetRegisteredDefinitions()
        {
            EnsureInitialized();
            List<IConfigDefinition> list = new List<IConfigDefinition>(_definitions.Values);
            return list.ToArray();
        }

        public static ConfigLocationType[] GetRegisteredLocations(string id)
        {
            EnsureInitialized();
            HashSet<ConfigLocationType> locations;
            if (_registeredLocations.TryGetValue(id, out locations))
            {
                ConfigLocationType[] arr = new ConfigLocationType[locations.Count];
                locations.CopyTo(arr);
                return arr;
            }
            return new ConfigLocationType[0];
        }

        // ----------------- core API -----------------

        public static T GetOrCreate<T>(ConfigLocationType location) where T : ConfigBase, new()
        {
            EnsureInitialized();

            string typeName = typeof(T).Name;
            IConfigDefinition def = GetDefinitionById(typeName);

            string key = MakeKey(location, typeName);

            ConfigBase instance;
            if (_instances.TryGetValue(key, out instance))
                return (T)instance;

            ConfigBase created = def.CreateDefaultInstance();
            _instances[key] = created;

            if (!_currentFiles.ContainsKey(key))
            {
                string defaultFileName = _fileSystem.GetDefaultFileName(def);
                _currentFiles[key] = defaultFileName;
            }

            return (T)created;
        }

        public static string GetCurrentFileName(ConfigLocationType location, string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            IConfigDefinition def = GetDefinitionById(id);
            string key = MakeKey(location, id);

            string fileName;
            if (_currentFiles.TryGetValue(key, out fileName))
                return fileName;

            string defaultFileName = _fileSystem.GetDefaultFileName(def);
            _currentFiles[key] = defaultFileName;
            return defaultFileName;
        }

        public static void SetCurrentFileName(ConfigLocationType location, string id, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            GetDefinitionById(id); // ensure id exists

            string key = MakeKey(location, id);
            _currentFiles[key] = fileName;
        }

        public static bool Load(ConfigLocationType location, string id, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            IConfigDefinition def = GetDefinitionById(id);

            string content;
            if (!_fileSystem.TryReadFile(location, fileName, out content))
                return false;

            ConfigBase config = _serializer.Deserialize(def, content);
            if (config == null)
                return false;

            string key = MakeKey(location, id);
            _instances[key] = config;
            _currentFiles[key] = fileName;

            return true;
        }

        public static bool Save(ConfigLocationType location, string id, string fileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            IConfigDefinition def = GetDefinitionById(id);
            string key = MakeKey(location, id);

            ConfigBase instance;
            if (!_instances.TryGetValue(key, out instance))
            {
                instance = def.CreateDefaultInstance();
                _instances[key] = instance;
            }

            string content = _serializer.Serialize(instance);
            _fileSystem.WriteFile(location, fileName, content);
            _currentFiles[key] = fileName;

            return true;
        }

        public static string GetConfigAsText(ConfigLocationType location, string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            IConfigDefinition def = GetDefinitionById(id);
            string key = MakeKey(location, id);

            ConfigBase instance;
            if (!_instances.TryGetValue(key, out instance))
            {
                instance = def.CreateDefaultInstance();
                _instances[key] = instance;
            }

            return _serializer.Serialize(instance);
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

        // ----------------- helpers -----------------

        private static IConfigDefinition GetDefinitionById(string id)
        {
            IConfigDefinition def;
            if (!_definitions.TryGetValue(id, out def))
                throw new InvalidOperationException("No config definition registered for id: " + id);

            return def;
        }

        private static string MakeKey(ConfigLocationType location, string id)
        {
            return ((int)location).ToString() + "|" + id;
        }
    }
}
