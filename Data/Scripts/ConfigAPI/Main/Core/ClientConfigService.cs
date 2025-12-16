using System;
using MarcoZechner.ConfigAPI.Main.Api;
using MarcoZechner.ConfigAPI.Main.Core.Migrator;
using MarcoZechner.ConfigAPI.Main.Domain;
using MarcoZechner.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Logging;
using MarcoZechner.Logging;

namespace MarcoZechner.ConfigAPI.Main.Core
{
    /// <summary>
    /// Local/Global config handling (no networking).
    /// Uses:
    /// - ConfigUserHooks for file IO + (de)serialize
    /// - IXmlConverter for TOML &lt;-> internal XML
    /// - IConfigLayoutMigrator for default/layout migration
    /// </summary>
    public sealed class ClientConfigService
    {
        public static Logger<ConfigApiTopics> Log = CfgLog.Logger;
        
        private readonly ConfigUserHooks _configUserHooks;
        private readonly IXmlConverter _converter;
        private readonly IConfigLayoutMigrator _migrator;

        private readonly ClientConfigStore _clientStore = new ClientConfigStore();

        public ClientConfigService(
            ConfigUserHooks configUserHooks,
            IXmlConverter converter,
            IConfigLayoutMigrator migrator)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ClientConfigService)}", $"\n\t{nameof(configUserHooks)} is not null: {configUserHooks != null}, \n\t{nameof(converter)} is not null: {converter != null}, \n\t{nameof(migrator)} is not null: {migrator != null}\n");
            if (configUserHooks == null) throw new ArgumentNullException(nameof(configUserHooks));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (migrator == null) throw new ArgumentNullException(nameof(migrator));

            _configUserHooks = configUserHooks;
            _converter = converter;
            _migrator = migrator;
        }
        
        private static string DefaultFile(string typeKey, string filename)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(DefaultFile)}", $"{nameof(typeKey)}={typeKey}, {nameof(filename)}={filename}");
            if (!string.IsNullOrEmpty(filename))
                return filename;

            return typeKey + ".toml";
        }
        
        private static string DefaultSidecar(string filename)
            => filename + ".default.toml";
        
        public object ClientConfigGet(string typeKey, LocationType locationType, string filename)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ClientConfigGet)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}, {nameof(filename)}={filename}");
            filename = DefaultFile(typeKey, filename);

            object existing;
            if (_clientStore.TryGet(typeKey, locationType, out existing))
                return existing;

            var loaded = TryLoadClient(typeKey, locationType, filename);
            if (loaded != null)
                return loaded;

            var def = _configUserHooks.NewDefault(typeKey);
            if (def == null)
                throw new Exception("ClientConfigGet: NewDefault returned null for " + typeKey);

            SaveClientToFile(typeKey, locationType, filename, def);

            _clientStore.Set(typeKey, locationType, def, filename);
            return def;
        }

        public object ClientConfigLoadAndSwitch(string typeKey, LocationType locationType, string filename)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ClientConfigLoadAndSwitch)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}, {nameof(filename)}={filename}");
            filename = DefaultFile(typeKey, filename);
            return TryLoadClient(typeKey, locationType, filename);
        }

        public bool ClientConfigSave(string typeKey, LocationType locationType)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ClientConfigSave)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}");
            object instance;
            if (!_clientStore.TryGet(typeKey, locationType, out instance))
                return false;

            string file;
            if (!_clientStore.TryGetCurrentFile(typeKey, locationType, out file))
                return false;

            SaveClientToFile(typeKey, locationType, file, instance);
            return true;
        }

        public object ClientConfigSaveAndSwitch(string typeKey, LocationType locationType, string filename)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ClientConfigSaveAndSwitch)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}, {nameof(filename)}={filename}");
            filename = DefaultFile(typeKey, filename);

            object instance;
            if (!_clientStore.TryGet(typeKey, locationType, out instance))
                return null;

            SaveClientToFile(typeKey, locationType, filename, instance);

            // Keep same instance reference; you can change this later if you want “reload after save”.
            _clientStore.Set(typeKey, locationType, instance, filename);
            return instance;
        }

        public bool ClientConfigExport(string typeKey, LocationType locationType, string filename, bool overwrite)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ClientConfigExport)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}, {nameof(filename)}={filename}, {nameof(overwrite)}={overwrite}");
            
            if (string.IsNullOrEmpty(filename))
                return false;

            object instance;
            if (!_clientStore.TryGet(typeKey, locationType, out instance))
                return false;

            string current;
            if (_clientStore.TryGetCurrentFile(typeKey, locationType, out current))
            {
                if (string.Equals(current, filename, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // If file exists, only overwrite if allowed AND type matches.
            var existingExternal = _configUserHooks.LoadFile(locationType, filename);
            if (existingExternal != null)
            {
                if (!overwrite)
                    return false;

                if (!ExternalFileMatchesType(typeKey, existingExternal))
                    return false;
            }

            SaveClientToFile(typeKey, locationType, filename, instance);
            return true;
        }
        
        // -------------------------
        // Helpers
        // -------------------------

        private object TryLoadClient(string typeKey, LocationType locationType, string filename)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(TryLoadClient)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}, {nameof(filename)}={filename}");
            
            var def = new HooksDefinition(_configUserHooks, typeKey);

            // External TOML content
            var externalCurrent = _configUserHooks.LoadFile(locationType, filename);
            var externalDefaults = _configUserHooks.LoadFile(locationType, DefaultSidecar(filename));

            // If missing or unreadable: create defaults, write both files, return default instance.
            if (string.IsNullOrEmpty(externalCurrent))
            {
                if (externalCurrent != null)
                    _configUserHooks.BackupFile(locationType, filename);

                var xmlCurrentDefaults = def.GetCurrentDefaultsInternalXml();

                var external = _converter.ToExternal(def, xmlCurrentDefaults, true);
                _configUserHooks.SaveFile(locationType, filename, external);
                _configUserHooks.SaveFile(locationType, DefaultSidecar(filename), external);

                var instDefault = _configUserHooks.DeserializeFromInternalXml(typeKey, xmlCurrentDefaults);
                _clientStore.Set(typeKey, locationType, instDefault, filename);
                return instDefault;
            }

            // Convert: external -> internal
            string xmlCurrentFromFile;
            try
            {
                xmlCurrentFromFile = _converter.ToInternal(def, externalCurrent);
            }
            catch
            {
                // invalid external format: backup + recreate from defaults
                _configUserHooks.BackupFile(locationType, filename);

                var xmlCurrentDefaults = def.GetCurrentDefaultsInternalXml();
                var external = _converter.ToExternal(def, xmlCurrentDefaults, true);

                _configUserHooks.SaveFile(locationType, filename, external);
                _configUserHooks.SaveFile(locationType, DefaultSidecar(filename), external);

                var instDefault = _configUserHooks.DeserializeFromInternalXml(typeKey, xmlCurrentDefaults);
                _clientStore.Set(typeKey, locationType, instDefault, filename);
                return instDefault;
            }

            // Defaults from file may be missing/invalid -> fall back to current defaults.
            string xmlOldDefaultsFromFile = null;
            if (!string.IsNullOrEmpty(externalDefaults))
            {
                try { xmlOldDefaultsFromFile = _converter.ToInternal(def, externalDefaults); }
                catch { xmlOldDefaultsFromFile = null; }
            }

            var xmlCurrentDefaultsFromCode = def.GetCurrentDefaultsInternalXml();
            if (string.IsNullOrEmpty(xmlOldDefaultsFromFile))
                xmlOldDefaultsFromFile = xmlCurrentDefaultsFromCode;

            // Migrate/normalize layout
            var layout = _migrator.Normalize(
                def,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                xmlCurrentDefaultsFromCode);

            // If migration was destructive, backup original file.
            if (layout.RequiresBackup)
                _configUserHooks.BackupFile(locationType, filename);

            // If normalization changed anything, rewrite disk.
            // (We compare internal XML first, then write external TOML.)
            var changedCurrent = !string.Equals(layout.NormalizedXml, xmlCurrentFromFile, StringComparison.Ordinal);
            var changedDefaults = !string.Equals(layout.NormalizedDefaultsXml, xmlOldDefaultsFromFile, StringComparison.Ordinal);

            if (changedCurrent || changedDefaults)
            {
                var newExternalCurrent = _converter.ToExternal(def, layout.NormalizedXml, true);
                var newExternalDefaults = _converter.ToExternal(def, layout.NormalizedDefaultsXml, true);

                _configUserHooks.SaveFile(locationType, filename, newExternalCurrent);
                _configUserHooks.SaveFile(locationType, DefaultSidecar(filename), newExternalDefaults);
            }

            // Deserialize normalized config
            var inst = _configUserHooks.DeserializeFromInternalXml(typeKey, layout.NormalizedXml);
            if (inst == null)
                return null;

            _clientStore.Set(typeKey, locationType, inst, filename);
            return inst;
        }

        private void SaveClientToFile(string typeKey, LocationType locationType, string filename, object instance)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(SaveClientToFile)}", $"{nameof(typeKey)}={typeKey}, {nameof(locationType)}={locationType}, {nameof(instance)} is not null: {instance != null}");
            
            
            if (!_configUserHooks.IsInstanceOf(typeKey, instance))
                throw new Exception("SaveClientToFile: instance/typeKey mismatch: " + typeKey);

            var def = new HooksDefinition(_configUserHooks, typeKey);

            // instance -> internal xml
            var internalXml = _configUserHooks.SerializeToInternalXml(typeKey, instance);

            // current defaults XML (code)
            var xmlCurrentDefaults = def.GetCurrentDefaultsInternalXml();

            // externalize both the config and the defaults sidecar
            var external = _converter.ToExternal(def, internalXml, true);
            var externalDefaults = _converter.ToExternal(def, xmlCurrentDefaults, true);

            _configUserHooks.SaveFile(locationType, filename, external);
            _configUserHooks.SaveFile(locationType, DefaultSidecar(filename), externalDefaults);
        }

        private bool ExternalFileMatchesType(string typeKey, string existingExternal)
        {
            Log.Trace($"{nameof(ClientConfigService)}.{nameof(ExternalFileMatchesType)}", $"{nameof(typeKey)}={typeKey}, {nameof(existingExternal)}.Length: {existingExternal.Length}");
            
            // The robust way is: ToInternal + check root name == definition.TypeName (or typeKey)
            // We keep this cheap and deterministic.
            var def = new HooksDefinition(_configUserHooks, typeKey);
            try
            {
                var xml = _converter.ToInternal(def, existingExternal);
                // Ensure internal xml root matches type name (not perfect, but good enough)
                string rootName;
                LayoutXml.ParseChildren(xml, out rootName);
                return string.Equals(rootName, def.TypeName, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }
}