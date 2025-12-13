using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Client.Api;
using MarcoZechner.ConfigAPI.Shared.Abstractions;
using MarcoZechner.ConfigAPI.Shared.Domain;
using VRage.Game.ModAPI;

namespace MarcoZechner.ConfigAPI.Client.Storage
{
    public class ConfigStorage
    {
        private static IConfigApi _api;

        // (Location, TypeName) -> current instance
        private static readonly Dictionary<ConfigKey, ConfigBase> _currentInstances =
            new Dictionary<ConfigKey, ConfigBase>();
        
        // instance -> (Location, TypeName)
        private static readonly Dictionary<ConfigBase, ConfigKey> _instanceToKey =
            new Dictionary<ConfigBase, ConfigKey>();

        private struct ConfigKey
        {
            public FileLocation Location;
            public string TypeName;
            public string CurrentFileName;
        }

        public static void Init(IMyModContext modContext) 
            => CustomInit(modContext.ModId, modContext.ModName);

        public static void CustomInit(string modId, string modName)
        {
            ApiBridge.Init(ulong.Parse(modId), modName);

            // Wire instance-level Save/TryLoad into ConfigBase
            ConfigBase.ClientBackend = new ClientBackend(_api, _currentInstances, _instanceToKey);
        }

        public static void Unload() 
            => ApiBridge.Unload();

        public static T Get<T>(FileLocation location)
            where T : ConfigBase, new()
        {
            var typeName = typeof(T).FullName;
            var key = new ConfigKey
            {
                Location = location, 
                TypeName = typeName
            };

            ConfigBase instance;
            if (!_currentInstances.TryGetValue(key, out instance))
            {
                // No instance yet -> create local default
                var created = new T();
                created.ApplyDefaults();
                key.CurrentFileName = null;

                instance = created;
                _currentInstances[key] = instance;
                _instanceToKey[instance] = key;
            }

            if (_api == null) return (T)instance;
            
            // If the provider API is ready, always sync to its canonical instance
            var apiInstance = _api.Get(typeName, location);
            if (apiInstance != null && !ReferenceEquals(apiInstance, instance))
            {
                // Update reverse mapping: old instance may become obsolete
                ConfigKey oldKey;
                if (_instanceToKey.TryGetValue(instance, out oldKey))
                {
                    _instanceToKey.Remove(instance);
                }

                instance = apiInstance;
                _currentInstances[key] = instance;
                _instanceToKey[instance] = key;
            }

            // Keep CurrentFile in sync as well
            var currentFile = _api.GetCurrentFileName(typeName, location);
            key.CurrentFileName = currentFile;

            return (T)instance;
        }

        private sealed class ClientBackend : IConfigClientBackend
        {
            private readonly IConfigApi _api;
            private readonly Dictionary<ConfigKey, ConfigBase> _currentInstances;
            private readonly Dictionary<ConfigBase, ConfigKey> _instanceToKey;

            public ClientBackend(
                IConfigApi api,
                Dictionary<ConfigKey, ConfigBase> currentInstances,
                Dictionary<ConfigBase, ConfigKey> instanceToKey)
            {
                _api = api;
                _currentInstances = currentInstances;
                _instanceToKey = instanceToKey;
            }

            public bool TryLoad(ConfigBase instance, string fileName)
            {
                if (instance == null || _api == null) return false;

                ConfigKey key;
                if (!_instanceToKey.TryGetValue(instance, out key))
                    return false;

                var ok = _api.TryLoad(key.TypeName, key.Location, fileName);

                // After loading, get canonical instance from provider and swap mapping if necessary
                var apiInstance = _api.Get(key.TypeName, key.Location);
                if (apiInstance == null || ReferenceEquals(apiInstance, instance)) return ok;
                
                _instanceToKey.Remove(instance);
                _currentInstances[key] = apiInstance;
                _instanceToKey[apiInstance] = key;
                instance = apiInstance;

                return ok;
            }

            public void Save(ConfigBase instance, string fileName)
            {
                if (instance == null || _api == null) return;

                ConfigKey key;
                if (!_instanceToKey.TryGetValue(instance, out key))
                    return;

                _api.Save(key.TypeName, key.Location, fileName);
            }

            public string GetCurrentFileName(ConfigBase instance)
            {
                if (instance == null || _api == null) return null;

                ConfigKey key;
                return _instanceToKey.TryGetValue(instance, out key) 
                    ? _api.GetCurrentFileName(key.TypeName, key.Location) 
                    : null;
            }
        }
    }
}
