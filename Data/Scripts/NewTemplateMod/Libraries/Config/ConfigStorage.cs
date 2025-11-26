using System;
using System.Collections.Generic;
using System.IO;
using mz.SemanticVersioning;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace mz.Config
{
    /* 
     * NOTE: 
     * This system is complex, so that it can automatically handle all the configs
     * automatically, with minimal user code. If you try to learn how configs work... DO NOT use this as an example.
    */

    /// <summary>
    /// This is a config storage system
    /// that automatically handles loading, saving, and change detection.
    /// Configs are classes derived from ConfigBase.
    /// DO NOT create instances of ConfigBase or derived types directly.
    /// Instead, use ConfigStorage.Register<T>() to get an instance of T.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]  
    public class ConfigStorage : MySessionComponentBase
    {
        private const string Extension = ".xml";

        private class ConfigEntry
        {
            public ConfigBase Instance;
            public Action Save;
        }

        private static readonly Dictionary<Type, Dictionary<ConfigStorageKind, ConfigEntry>> _configs
            = new Dictionary<Type, Dictionary<ConfigStorageKind, ConfigEntry>>();

        /// <summary>
        /// Called after any config value changes (after auto-save).
        /// </summary>
        public static Action OnAnyConfigChanged;

        /// <summary>
        /// Register and load a config of type T.
        /// The returned instance is the loaded (or created default) config.
        /// ConfigStorage will manage saving on changes and on unload
        /// </summary>
        public static T Register<T>(ConfigStorageKind storageKind)
            where T : ConfigBase, new()
        {
            var type = typeof(T);

            Dictionary<ConfigStorageKind, ConfigEntry> byKind;
            if (!_configs.TryGetValue(type, out byKind))
            {
                byKind = new Dictionary<ConfigStorageKind, ConfigEntry>();
                _configs[type] = byKind;
            }

            ConfigEntry existingEntry;
            if (byKind.TryGetValue(storageKind, out existingEntry))
                return (T)existingEntry.Instance;

            var cfg = new T();
            string name = GetSafeConfigName(cfg);

            cfg = Load(storageKind, cfg, name);

            var entry = new ConfigEntry
            {
                Instance = cfg,
                Save = () => Save(storageKind, cfg, name)
            };

            byKind[storageKind] = entry;
            return cfg;
        }

        /// <summary>
        /// Get previously registered config for the given type and storage kind. (after Register has been called)
        /// </summary>
        public static T Get<T>(ConfigStorageKind storageKind) where T : ConfigBase
        {
            var type = typeof(T);

            Dictionary<ConfigStorageKind, ConfigEntry> byKind;
            ConfigEntry entry;
            if (_configs.TryGetValue(type, out byKind) &&
                byKind.TryGetValue(storageKind, out entry))
            {
                return (T)entry.Instance;
            }

            throw new InvalidOperationException($"Config {type.Name} with storage kind {storageKind} not registered.");
        }

        protected override void UnloadData()
        {
            SaveAll();
        }

        /// <summary>
        /// Save all configs that have been registered.
        /// Called automatically on unload.
        /// </summary>
        public static void SaveAll()
        {
            foreach (var byKind in _configs.Values)
            {
                foreach (var entry in byKind.Values)
                    entry.Save();
            }
        }

        /// <summary>
        /// Called by ConfigValue when any config value is changed.
        /// </summary>
        internal static void NotifyChanged()
        {
            SaveAll();
            OnAnyConfigChanged?.Invoke();
        }

        // ------------------ internals ------------------

        private static T Load<T>(ConfigStorageKind storageKind, T cfgTemplate, string name) where T : ConfigBase, new()
        {
            var utils = MyAPIGateway.Utilities;
            if (utils == null)
                throw new Exception("MyAPIGateway.Utilities is null (too early for config load).");

            string file = MakeFileName(storageKind, cfgTemplate, name);

            try
            {
                if (Exists<T>(utils, file, storageKind))
                {
                    using (var reader = OpenRead<T>(utils, file, storageKind))
                    {
                        var xml = reader.ReadToEnd();

                        if (string.IsNullOrWhiteSpace(xml))
                        {
                            InitializeVersionInfo(cfgTemplate, file);
                            Save(storageKind, cfgTemplate, name);
                            return cfgTemplate;
                        }

                        try
                        {
                            var loaded = utils.SerializeFromXML<T>(xml);

                            if (!VersionAndHashOk(loaded, file))
                            {
                                CreateBackupForCorrupted<T>(utils, file, storageKind, xml);
                                InitializeVersionInfo(cfgTemplate, file);
                                TryLog($"Config {file}: tampered or invalid version data. Reset to defaults.");
                                Save(storageKind, cfgTemplate, name);
                                return cfgTemplate;
                            }

                            var storedVer = SemanticVersion.Parse(loaded.StoredVersion ?? "0.0.0");
                            var currentVer = loaded.ConfigVersion;

                            if (storedVer.Major != currentVer.Major)
                            {
                                // Major mismatch: backup and reset
                                CreateBackupForCorrupted<T>(utils, file, storageKind, xml);
                                InitializeVersionInfo(cfgTemplate, file);
                                TryLog($"Config {file}: major version mismatch ({storedVer} -> {currentVer}). Reset to defaults, please review settings.");
                                Save(storageKind, cfgTemplate, name);
                                return cfgTemplate;
                            }

                            bool minorOrPatchChanged =
                                storedVer.Minor != currentVer.Minor ||
                                storedVer.Patch != currentVer.Patch;

                            if (minorOrPatchChanged)
                            {
                                // Minor/patch mismatch: keep values, bump version & hash
                                InitializeVersionInfo(loaded, file);
                                TryLog($"Config {file}: version upgraded ({storedVer} -> {currentVer}), keeping existing values.");
                                Save(storageKind, loaded, name);
                                return loaded;
                            }

                            // Same version: ensure hash up to date (in case algo changed)
                            InitializeVersionInfo(loaded, file);
                            Save(storageKind, loaded, name);
                            return loaded;
                        }
                        catch (Exception e)
                        {
                            CreateBackupForCorrupted<T>(utils, file, storageKind, xml);
                            InitializeVersionInfo(cfgTemplate, file);
                            TryLog($"Config parse error {file}: {e}. Reset to defaults.");
                            Save(storageKind, cfgTemplate, name);
                            return cfgTemplate;
                        }
                    }
                }

                // No file: fresh default
                InitializeVersionInfo(cfgTemplate, file);
                Save(storageKind, cfgTemplate, name);
                return cfgTemplate;
            }
            catch (Exception e)
            {
                TryLog($"Config load error {file}: {e}");
                InitializeVersionInfo(cfgTemplate, file);
                Save(storageKind, cfgTemplate, name);
                return cfgTemplate;
            }
        }

        private static void Save<T>(ConfigStorageKind storageKind, T cfg, string name) where T : ConfigBase, new()
        {
            var utils = MyAPIGateway.Utilities;
            if (utils == null)
                return;

            string file = MakeFileName(storageKind, cfg, name);

            try
            {
                // Make sure version info is consistent before saving
                InitializeVersionInfo(cfg, file);

                using (var writer = OpenWrite<T>(utils, file,storageKind))
                {
                    var xml = utils.SerializeToXML(cfg);
                    writer.Write(xml);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                TryLog($"Config save error {file}: {e}");
            }
        }

        // ---------------- version helpers ----------------

        private static void InitializeVersionInfo(ConfigBase cfg, string fileName)
        {
            var v = cfg.ConfigVersion;
            var versionString = v.ToString();
            cfg.StoredVersion = versionString;
            cfg.StoredVersionHash = ComputeVersionHash(versionString, fileName);
        }

        private static bool VersionAndHashOk(ConfigBase loaded, string fileName)
        {
            if (string.IsNullOrWhiteSpace(loaded.StoredVersion) ||
                string.IsNullOrWhiteSpace(loaded.StoredVersionHash))
                return false;

            string expected = ComputeVersionHash(loaded.StoredVersion, fileName);
            return string.Equals(loaded.StoredVersionHash, expected, StringComparison.Ordinal);
        }

        private static string ComputeVersionHash(string version, string fileName)
        {
            // simple FNV-1a 64-bit hash -> hex
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;
            string s = version + "|" + fileName;

            for (int i = 0; i < s.Length; i++)
            {
                hash ^= (byte)s[i];
                hash *= prime;
            }

            return hash.ToString("X16");
        }

        // ---------------- storage helpers ----------------

        private static string GetSafeConfigName<T>(T cfg) where T : ConfigBase
        {
            var overrideName = cfg.ConfigNameOverride;
            var fallback = typeof(T).Name;

            if (string.IsNullOrWhiteSpace(overrideName))
                return fallback;

            if (!IsValidSimpleFileName(overrideName))
            {
                TryLog($"ConfigNameOverride '{overrideName}' for {typeof(T).Name} is invalid, falling back to '{fallback}'.");
                return fallback;
            }

            return overrideName;
        }

        private static bool IsValidSimpleFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (name.IndexOf(c) >= 0)
                    return false;
            }

            return true;
        }

        private static string MakeFileName(ConfigStorageKind storageKind, ConfigBase cfg, string name)
        {
            if (storageKind == ConfigStorageKind.Global)
                return cfg.GetType().FullName + "." + name + Extension;

            return name + Extension;
        }

        private static bool Exists<T>(IMyUtilities utils, string fileName, ConfigStorageKind kind)
        {
            var t = typeof(T);

            switch (kind)
            {
                case ConfigStorageKind.World:
                    return utils.FileExistsInWorldStorage(fileName, t);

                case ConfigStorageKind.Global:
                    return utils.FileExistsInGlobalStorage(fileName);

                case ConfigStorageKind.Local:
                    return utils.FileExistsInLocalStorage(fileName, t);

                default:
                    return false;
            }
        }

        private static TextReader OpenRead<T>(IMyUtilities utils, string fileName, ConfigStorageKind kind)
        {
            var t = typeof(T);

            switch (kind)
            {
                case ConfigStorageKind.World:
                    return utils.ReadFileInWorldStorage(fileName, t);

                case ConfigStorageKind.Global:
                    return utils.ReadFileInGlobalStorage(fileName);

                case ConfigStorageKind.Local:
                    return utils.ReadFileInLocalStorage(fileName, t);

                default:
                    TryLog($"Config open read: unknown storage kind {kind}");
                    return null;
            }
        }

        private static TextWriter OpenWrite<T>(IMyUtilities utils, string fileName, ConfigStorageKind kind)
        {
            var t = typeof(T);

            switch (kind)
            {
                case ConfigStorageKind.World:
                    return utils.WriteFileInWorldStorage(fileName, t);

                case ConfigStorageKind.Global:
                    return utils.WriteFileInGlobalStorage(fileName);

                case ConfigStorageKind.Local:
                    return utils.WriteFileInLocalStorage(fileName, t);

                default:
                    TryLog($"Config open read: unknown storage kind {kind}");
                    return null;
            }
        }

        private static void CreateBackupForCorrupted<T>(IMyUtilities utils, string fileName, ConfigStorageKind kind, string xml)
        {
            try
            {
                string backupName =
                    Path.GetFileNameWithoutExtension(fileName)
                    + ".backup_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmssfff")
                    + Extension;

                using (var writer = OpenWrite<T>(utils, backupName, kind))
                {
                    writer.Write(xml);
                    writer.Flush();
                }

                TryLog($"Backed up config to {backupName}");
            }
            catch (Exception e)
            {
                TryLog($"Config backup error ({fileName}): {e}");
            }
        }

        private static void TryLog(string msg)
        {
            if (MyAPIGateway.Session?.Player?.Character == null)
            {
                logQueue.Enqueue(msg);
                return;
            } else while (logQueue.Count > 0)
            {
                var queuedMsg = logQueue.Dequeue();
                try
                {
                    MyAPIGateway.Utilities?.ShowMessage("ConfigStorage", queuedMsg);
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                MyAPIGateway.Utilities?.ShowMessage("ConfigStorage", msg);
            }
            catch
            {
                // ignore
            }
        }

        private static readonly Queue<string> logQueue = new Queue<string>();
    }
}
