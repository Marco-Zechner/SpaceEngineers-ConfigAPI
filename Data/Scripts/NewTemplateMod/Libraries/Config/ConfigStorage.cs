using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private const string EXTENSION = ".xml";

        private class ConfigEntry
        {
            public ConfigBase Instance;
            public Action Save;
            public Func<ConfigBase> Reload;

            public ConfigStorageKind StorageKind;
            public string Name;      // base name
            public string FileName;  // MakeFileName(kind, cfg, name)
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
                StorageKind = storageKind,
                Name = name,
                FileName = MakeFileName(storageKind, cfg, name),
            };

            entry.Save = () => Save(storageKind, (T)entry.Instance, name);
            entry.Reload = () => Load(storageKind, new T(), name);

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
            SaveAllConfigs();
        }

        /// <summary>
        /// Save all configs that have been registered.
        /// Called automatically on unload.
        /// </summary>
        public static void SaveAllConfigs()
        {
            foreach (var byKind in _configs.Values)
            {
                foreach (var entry in byKind.Values)
                    entry.Save();
            }
        }

        /// <summary>
        /// Save configs with optional filter.
        /// Filter receives (kind, virtualPath). If filter is null, all configs are saved.
        /// Virtual path looks like: SpaceEngineers/WorldStorage/MyConfig.xml
        /// Returns true if at least one registered config matched and was saved.
        /// </summary>
        public static bool SaveConfigs(Func<ConfigStorageKind, string, bool> filter = null)
        {
            bool anySaved = false;

            foreach (var byKind in _configs.Values)
            {
                foreach (var entry in byKind.Values)
                {
                    string vpath = MakeVirtualPath(entry.StorageKind, entry.FileName);
                    if (filter != null && !filter(entry.StorageKind, vpath))
                        continue;

                    entry.Save();
                    anySaved = true;
                }
            }

            return anySaved;
        }

        /// <summary>
        /// Reload configs with optional filter.
        /// Filter receives (kind, virtualPath). If filter is null, all configs are reloaded.
        /// Returns true if at least one registered config matched and was reloaded.
        /// </summary>
        public static bool ReloadConfigs(Func<ConfigStorageKind, string, bool> filter = null)
        {
            bool anyReloaded = false;
            foreach (var byKind in _configs.Values)
            {
                foreach (var entry in byKind.Values)
                {
                    string vpath = MakeVirtualPath(entry.StorageKind, entry.FileName);
                    if (filter != null && !filter(entry.StorageKind, vpath))
                        continue;

                    if (entry.Reload == null)
                        continue;

                    var newInstance = entry.Reload();
                    entry.Instance = newInstance;
                    anyReloaded = true;
                }
            }
            return anyReloaded;
        }

        /// <summary>
        /// Lists all registered configs as virtual paths, grouped by storage kind.
        /// Virtual paths look like: SpaceEngineers/WorldStorage/MyConfig.xml
        /// </summary>
        public static Dictionary<ConfigStorageKind, List<string>> ListConfigs()
        {
            var result = new Dictionary<ConfigStorageKind, List<string>>();

            foreach (var byKind in _configs.Values)
            {
                foreach (var entry in byKind.Values)
                {
                    var kind = entry.StorageKind;
                    List<string> list;
                    if (!result.TryGetValue(kind, out list))
                    {
                        list = new List<string>();
                        result[kind] = list;
                    }

                    string vpath = MakeVirtualPath(kind, entry.FileName);
                    list.Add(vpath);
                }
            }

            return result;
        }

        /// <summary>
        /// Called by CfgVal when any config value is changed.
        /// </summary>
        internal static void NotifyChanged()
        {
            if (_isLoading)
                return; // Suppress auto-save during load

            SaveAllConfigs();
            OnAnyConfigChanged?.Invoke();
        }

        // ------------------ Command handler ------------------

        /// <summary>
        /// Handles config management commands.
        /// /{prefix} list
        /// /{prefix} save &lt;virtualPath&gt;
        /// /{prefix} save &lt;local|global|world&gt; &lt;fileName[.xml]&gt;
        /// /{prefix} reload ... (same as save)
        ///
        /// virtualPath looks like: SpaceEngineers/WorldStorage/MyConfig.xml
        /// </summary>
        public static void HandleConfigCommands(string commandPrefix, ulong sender, string command, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(commandPrefix) || string.IsNullOrWhiteSpace(command))
                return;

            var text = command.Trim();
            var prefixWithSlash = "/" + commandPrefix;

            if (!text.StartsWith(prefixWithSlash + " ", StringComparison.OrdinalIgnoreCase) &&
                !text.Equals(prefixWithSlash, StringComparison.OrdinalIgnoreCase))
                return;

            // this is our command; don't send it to chat
            sendToOthers = false;

            var rest = text.Substring(prefixWithSlash.Length).TrimStart();
            if (string.IsNullOrEmpty(rest))
            {
                TryLog($"Config command usage: /{commandPrefix} list | save ... | reload ...");
                return;
            }

            var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();

            switch (cmd)
            {
                case "list":
                    HandleListCommand();
                    break;

                case "save":
                    HandleSaveOrReloadCommand(true, parts);
                    break;

                case "reload":
                    HandleSaveOrReloadCommand(false, parts);
                    break;

                default:
                    TryLog($"Unknown config command '{cmd}'. Use: /{commandPrefix} list | save | reload");
                    break;
            }
        }

        private static void HandleListCommand()
        {
            var dict = ListConfigs();
            if (dict.Count == 0)
            {
                TryLog("No configs registered.");
                return;
            }

            foreach (var kv in dict)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{kv.Key}:");
                foreach (var path in kv.Value)
                    sb.AppendLine(path);
                TryLog(sb.ToString());
            }
        }

        private static void HandleSaveOrReloadCommand(bool isSave, string[] parts)
        {
            if (parts.Length < 2)
            {
                TryLog($"Usage: ... {(isSave ? "save" : "reload")} <SpaceEngineers/...> OR <local|global|world> <fileName[.xml]>");
                return;
            }

            // detect "path mode" vs "kind+name mode"
            // path mode: second arg contains "SpaceEngineers" or a '/'
            var second = parts[1];

            if (second.IndexOf("SpaceEngineers", StringComparison.OrdinalIgnoreCase) >= 0 ||
                second.Contains("/") || second.Contains("\\"))
            {
                // path mode: everything after the subcommand is the path
                var pathArg = string.Join(" ", parts, 1, parts.Length - 1).Trim();

                ConfigStorageKind kind;
                string fileName;
                if (!TryParseVirtualPath(pathArg, out kind, out fileName))
                {
                    TryLog($"Could not parse path '{pathArg}'. Expected something like SpaceEngineers/WorldStorage/MyConfig.xml");
                    return;
                }

                Func<ConfigStorageKind, string, bool> filter = (k, vpath) =>
                {
                    if (k != kind) return false;
                    var fn = GetFileNameFromVirtualPath(vpath);
                    return fn.Equals(fileName, StringComparison.OrdinalIgnoreCase);
                };

                if (isSave)
                {
                    if (SaveConfigs(filter))
                        TryLog($"Saved config: {kind} {fileName}");
                    else
                        TryLog($"No matching config found to save for: {kind} {fileName}");
                }
                else
                {
                    if (ReloadConfigs(filter))
                        TryLog($"Reloaded config: {kind} {fileName}");
                    else
                        TryLog($"No matching config found to reload for: {kind} {fileName}");
                }
            }
            else
            {
                // kind + name mode: save local MyConfig[.xml]
                if (parts.Length < 3)
                {
                    TryLog($"Usage: ... {(isSave ? "save" : "reload")} <local|global|world> <fileName[.xml]>");
                    return;
                }

                ConfigStorageKind kind;
                if (!TryParseKind(parts[1], out kind))
                {
                    TryLog($"Unknown storage kind '{parts[1]}'. Use local, global or world.");
                    return;
                }

                var fileName = parts[2];
                if (!fileName.EndsWith(EXTENSION, StringComparison.OrdinalIgnoreCase))
                    fileName += EXTENSION;

                Func<ConfigStorageKind, string, bool> filter = (k, vpath) =>
                {
                    if (k != kind) return false;
                    var fn = GetFileNameFromVirtualPath(vpath);
                    return fn.Equals(fileName, StringComparison.OrdinalIgnoreCase);
                };

                if (isSave)
                {
                    if (SaveConfigs(filter))
                        TryLog($"Saved config: {kind} {fileName}");
                    else
                        TryLog($"No matching config found to save for: {kind} {fileName}");
                }
                else
                {
                    if (ReloadConfigs(filter))
                        TryLog($"Reloaded config: {kind} {fileName}");
                    else
                        TryLog($"No matching config found to reload for: {kind} {fileName}");
                }
            }
        }

        private static bool TryParseKind(string text, out ConfigStorageKind kind)
        {
            kind = ConfigStorageKind.Local;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim().ToLowerInvariant();

            switch (text)
            {
                case "local":
                    kind = ConfigStorageKind.Local;
                    return true;
                case "global":
                    kind = ConfigStorageKind.Global;
                    return true;
                case "world":
                    kind = ConfigStorageKind.World;
                    return true;
                default:
                    return false;
            }
        }

        private static string MakeVirtualPath(ConfigStorageKind kind, string fileName)
        {
            switch (kind)
            {
                case ConfigStorageKind.Global:
                    return "SpaceEngineers/GlobalStorage/" + fileName;
                case ConfigStorageKind.Local:
                    return "SpaceEngineers/LocalStorage/" + fileName;
                case ConfigStorageKind.World:
                    return "SpaceEngineers/WorldStorage/" + fileName;
                default:
                    return "SpaceEngineers/Unknown/" + fileName;
            }
        }

        private static bool TryParseVirtualPath(string virtualPath, out ConfigStorageKind kind, out string fileName)
        {
            kind = ConfigStorageKind.Local;
            fileName = null;

            if (string.IsNullOrWhiteSpace(virtualPath))
                return false;

            var trimmed = virtualPath.Trim().Replace('\\', '/');

            var idx = trimmed.IndexOf("SpaceEngineers/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                trimmed = trimmed.Substring(idx + "SpaceEngineers/".Length);

            if (trimmed.StartsWith("GlobalStorage/", StringComparison.OrdinalIgnoreCase))
            {
                kind = ConfigStorageKind.Global;
                fileName = trimmed.Substring("GlobalStorage/".Length);
                return !string.IsNullOrWhiteSpace(fileName);
            }

            if (trimmed.StartsWith("LocalStorage/", StringComparison.OrdinalIgnoreCase))
            {
                kind = ConfigStorageKind.Local;
                fileName = trimmed.Substring("LocalStorage/".Length);
                return !string.IsNullOrWhiteSpace(fileName);
            }

            if (trimmed.StartsWith("WorldStorage/", StringComparison.OrdinalIgnoreCase))
            {
                kind = ConfigStorageKind.World;
                fileName = trimmed.Substring("WorldStorage/".Length);
                return !string.IsNullOrWhiteSpace(fileName);
            }

            return false;
        }

        private static string GetFileNameFromVirtualPath(string vpath)
        {
            if (string.IsNullOrWhiteSpace(vpath))
                return vpath;

            var s = vpath.Replace('\\', '/');
            var idx = s.LastIndexOf('/');
            if (idx < 0) return s;
            return s.Substring(idx + 1);
        }

        // ------------------ internals ------------------

        private static bool _isLoading = false;

        private static T Load<T>(ConfigStorageKind storageKind, T cfgTemplate, string name)
            where T : ConfigBase, new()
        {
            var utils = MyAPIGateway.Utilities;
            if (utils == null)
                throw new Exception("MyAPIGateway.Utilities is null (too early for config load).");

            string file = MakeFileName(storageKind, cfgTemplate, name);
            string xml = null;

            if (Exists<T>(utils, file, storageKind))
            {
                using (var reader = OpenRead<T>(utils, file, storageKind))
                {
                    xml = reader.ReadToEnd();
                }
            }

            _isLoading = true;
            try
            {
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
            catch (Exception e)
            {
                TryLog($"Config load error {file}: {e}");
                InitializeVersionInfo(cfgTemplate, file);
                Save(storageKind, cfgTemplate, name);
                return cfgTemplate;
            }
            finally
            {
                _isLoading = false;
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

                using (var writer = OpenWrite<T>(utils, file, storageKind))
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
                return cfg.GetType().FullName + "." + name + EXTENSION;

            return name + EXTENSION;
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
                    TryLog($"Config open write: unknown storage kind {kind}");
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
                    + EXTENSION;

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

        public static void TryLog(string msg, string sender = "ConfigStorage")
        {
            if (MyAPIGateway.Session?.Player?.Character == null)
            {
                logQueue.Enqueue(msg);
                return;
            }
            else while (logQueue.Count > 0)
            {
                var queuedMsg = logQueue.Dequeue();
                try
                {
                    MyAPIGateway.Utilities?.ShowMessage(sender, queuedMsg);
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                MyAPIGateway.Utilities?.ShowMessage(sender, msg);
            }
            catch
            {
                // ignore
            }
        }

        public static readonly Queue<string> logQueue = new Queue<string>();
    }
}
