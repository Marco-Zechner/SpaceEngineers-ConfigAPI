using System;
using System.Collections.Generic;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
    public class ConfigStorage : IConfigStorage
    {
        private readonly IConfigFileSystem _fileSystem;
        private readonly IConfigSerializer _serializer;

        private readonly Dictionary<string, IConfigDefinition> _definitions =
            new Dictionary<string, IConfigDefinition>();

        // key: (location|typeName) -> current filename
        private readonly Dictionary<string, string> _currentFiles =
            new Dictionary<string, string>();

        // key: (location|typeName) -> in-memory instance
        private readonly Dictionary<string, ConfigBase> _instances =
            new Dictionary<string, ConfigBase>();

        public ConfigStorage(IConfigFileSystem fileSystem, IConfigSerializer serializer)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (serializer == null) throw new ArgumentNullException("serializer");

            _fileSystem = fileSystem;
            _serializer = serializer;
        }

        public void RegisterConfig(IConfigDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException("definition");
            if (string.IsNullOrEmpty(definition.TypeName))
                throw new ArgumentException("Config definition must have a TypeName.", "definition");

            _definitions[definition.TypeName] = definition;
        }

        public IConfigDefinition[] GetRegisteredDefinitions()
        {
            var list = new List<IConfigDefinition>(_definitions.Values);
            return list.ToArray();
        }

        public T GetOrCreate<T>(ConfigLocationType location) where T : ConfigBase
        {
            IConfigDefinition def = FindDefinitionByType(typeof(T));
            if (def == null)
                throw new InvalidOperationException("No config definition registered for type: " + typeof(T).FullName);

            string key = MakeKey(location, def.TypeName);

            ConfigBase instance;
            if (_instances.TryGetValue(key, out instance))
                return (T)instance;

            // Create default and store it
            ConfigBase created = def.CreateDefaultInstance();
            _instances[key] = created;

            // Ensure current file name exists (lazy init)
            if (!_currentFiles.ContainsKey(key))
            {
                string defaultFileName = _fileSystem.GetDefaultFileName(def);
                _currentFiles[key] = defaultFileName;
            }

            return (T)created;
        }

        public string GetCurrentFileName(ConfigLocationType location, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");

            IConfigDefinition def = GetDefinitionByTypeName(typeName);
            string key = MakeKey(location, typeName);

            string fileName;
            if (_currentFiles.TryGetValue(key, out fileName))
                return fileName;

            // If not set yet, use default from file system
            string defaultFileName = _fileSystem.GetDefaultFileName(def);
            _currentFiles[key] = defaultFileName;
            return defaultFileName;
        }

        public void SetCurrentFileName(ConfigLocationType location, string typeName, string fileName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            // Ensure type exists
            GetDefinitionByTypeName(typeName);

            string key = MakeKey(location, typeName);
            _currentFiles[key] = fileName;
        }

        public bool Load(ConfigLocationType location, string typeName, string fileName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            IConfigDefinition def = GetDefinitionByTypeName(typeName);

            string content;
            if (!_fileSystem.TryReadFile(location, fileName, out content))
                return false;

            ConfigBase config = _serializer.Deserialize(def, content);
            if (config == null)
                return false;

            string key = MakeKey(location, typeName);
            _instances[key] = config;
            _currentFiles[key] = fileName;

            return true;
        }

        public bool Save(ConfigLocationType location, string typeName, string fileName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            IConfigDefinition def = GetDefinitionByTypeName(typeName);
            string key = MakeKey(location, typeName);

            ConfigBase instance;
            if (!_instances.TryGetValue(key, out instance))
            {
                // No instance yet, create default
                instance = def.CreateDefaultInstance();
                _instances[key] = instance;
            }

            string content = _serializer.Serialize(instance);
            _fileSystem.WriteFile(location, fileName, content);
            _currentFiles[key] = fileName;

            return true;
        }

        public string GetConfigAsText(ConfigLocationType location, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");

            IConfigDefinition def = GetDefinitionByTypeName(typeName);
            string key = MakeKey(location, typeName);

            ConfigBase instance;
            if (!_instances.TryGetValue(key, out instance))
            {
                // If not loaded yet, create default instance but do not persist anywhere
                instance = def.CreateDefaultInstance();
                _instances[key] = instance;
            }

            return _serializer.Serialize(instance);
        }

        public string GetFileAsText(ConfigLocationType location, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            string content;
            if (_fileSystem.TryReadFile(location, fileName, out content))
                return content;

            return null;
        }

        // ----------------- helpers -----------------

        private IConfigDefinition FindDefinitionByType(Type type)
        {
            foreach (var kv in _definitions)
            {
                if (kv.Value.ConfigType == type)
                    return kv.Value;
            }
            return null;
        }

        private IConfigDefinition GetDefinitionByTypeName(string typeName)
        {
            IConfigDefinition def;
            if (!_definitions.TryGetValue(typeName, out def))
                throw new InvalidOperationException("No config definition registered for type name: " + typeName);
            return def;
        }

        private static string MakeKey(ConfigLocationType location, string typeName)
        {
            return ((int)location).ToString() + "|" + typeName;
        }
    }
}
