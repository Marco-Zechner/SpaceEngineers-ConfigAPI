using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Main.Core;
using MarcoZechner.ConfigAPI.Main.Core.Migrator;
using MarcoZechner.ConfigAPI.Main.Core.XmlConverter;
using MarcoZechner.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    /// <summary>
    /// Main API bound to a single consumer mod.
    /// No modId needs to be passed on calls anymore.
    /// </summary>
    public sealed class ConfigServiceImpl : IConfigService, IApiProvider
    {
        private readonly ulong _consumerModId;
        private readonly string _consumerModName;
        private readonly ConfigUserHooks _configUserHooks;
        private ClientConfigService _clientConfigService;

        public ConfigServiceImpl(ulong modId, string modName, ConfigUserHooks configUserHooks)
        {
            _consumerModId = modId;
            _consumerModName = modName;
            _configUserHooks = configUserHooks;
            _clientConfigService = new ClientConfigService(
                _configUserHooks,
                new TomlXmlConverter(),
                new ConfigLayoutMigrator()
            );
        }

        // Client-side APIs
        
        public object ClientConfigGet(string typeKey, LocationType locationType, string filename) 
            => _clientConfigService.ClientConfigGet(typeKey, locationType, filename);

        public object ClientConfigReload(string typeKey, LocationType locationType)
            => _clientConfigService.ClientConfigReload(typeKey, locationType);

        public string ClientConfigGetCurrentFileName(string typeKey, LocationType locationType)
            => _clientConfigService.ClientConfigGetCurrentFileName(typeKey, locationType);

        public object ClientConfigLoadAndSwitch(string typeKey, LocationType locationType, string filename)
            => _clientConfigService.ClientConfigLoadAndSwitch(typeKey, locationType, filename);

        public bool ClientConfigSave(string typeKey, LocationType locationType)
            => _clientConfigService.ClientConfigSave(typeKey, locationType);

        public object ClientConfigSaveAndSwitch(string typeKey, LocationType locationType, string filename)
            => _clientConfigService.ClientConfigSaveAndSwitch(typeKey, locationType, filename);

        public bool ClientConfigExport(string typeKey, LocationType locationType, string filename, bool overwrite)
            => _clientConfigService.ClientConfigExport(typeKey, locationType, filename, overwrite);

        // Server-side APIs
        
        public object ServerConfigInit(string typeKey, string defaultFile)
        {
            throw new Exception("Not Implemented");
        }

        public CfgUpdate ServerConfigGetUpdate(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public object ServerConfigGetAuth(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public object ServerConfigGetDraft(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public void ServerConfigResetDraft(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public bool ServerConfigLoadAndSwitch(string typeKey, string file, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public bool ServerConfigSave(string typeKey, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public bool ServerConfigSaveAndSwitch(string typeKey, string file, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public bool ServerConfigExport(string typeKey, string file, bool overwrite)
        {
            throw new Exception("Not Implemented");
        }

        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                { nameof(ClientConfigGet), new Func<string, int, string, object>(ClientConfigGetInternal) },
                { nameof(ClientConfigReload), new Func<string, int, object>(ClientConfigReloadInternal) },
                { nameof(ClientConfigGetCurrentFileName), new Func<string, int, string>(ClientConfigGetCurrentFileNameInternal) },
                { nameof(ClientConfigLoadAndSwitch), new Func<string, int, string, object>(ClientConfigLoadAndSwitchInternal) },
                { nameof(ClientConfigSave), new Func<string, int, bool>(ClientConfigSaveInternal) },
                { nameof(ClientConfigSaveAndSwitch), new Func<string, int, string, object>(ClientConfigSaveAndSwitchInternal) },
                { nameof(ClientConfigExport), new Func<string, int, string, bool, bool>(ClientConfigExportInternal) },
                { nameof(ServerConfigInit), new Func<string, string, object>(ServerConfigInit) },
                { nameof(ServerConfigGetUpdate), new Func<string, object[]>(ServerConfigGetUpdateInternal) },
                { nameof(ServerConfigGetAuth), new Func<string, object>(ServerConfigGetAuth) },
                { nameof(ServerConfigGetDraft), new Func<string, object>(ServerConfigGetDraft) },
                { nameof(ServerConfigResetDraft), new Action<string>(ServerConfigResetDraft) },
                { nameof(ServerConfigLoadAndSwitch), new Func<string, string, ulong, bool>(ServerConfigLoadAndSwitch) },
                { nameof(ServerConfigSave), new Func<string, ulong, bool>(ServerConfigSave) },
                { nameof(ServerConfigSaveAndSwitch), new Func<string, string, ulong, bool>(ServerConfigSaveAndSwitch) },
                { nameof(ServerConfigExport), new Func<string, string, bool, bool>(ServerConfigExport) },
            };
        }
        
        // ===============================================================
        // Internal conversion methods for delegate to custom types
        // ===============================================================
        
        private object ClientConfigGetInternal(string typeKey, int locationTypeEnum, string filename) 
            => ClientConfigGet(typeKey, (LocationType)locationTypeEnum, filename);      
        private object ClientConfigReloadInternal(string typeKey, int locationTypeEnum) 
            => ClientConfigReload(typeKey, (LocationType)locationTypeEnum);      
        private string ClientConfigGetCurrentFileNameInternal(string typeKey, int locationTypeEnum) 
            => ClientConfigGetCurrentFileName(typeKey, (LocationType)locationTypeEnum);
        private object ClientConfigLoadAndSwitchInternal(string typeKey, int locationTypeEnum, string filename)
            => ClientConfigLoadAndSwitch(typeKey, (LocationType)locationTypeEnum, filename);
        private bool ClientConfigSaveInternal(string typeKey, int locationTypeEnum) 
            => ClientConfigSave(typeKey, (LocationType)locationTypeEnum);
        private object ClientConfigSaveAndSwitchInternal(string typeKey, int locationTypeEnum, string filename) 
            => ClientConfigSaveAndSwitch(typeKey, (LocationType)locationTypeEnum, filename);
        private bool ClientConfigExportInternal(string typeKey, int locationTypeEnum, string filename, bool overwrite) 
            => ClientConfigExport(typeKey, (LocationType)locationTypeEnum, filename, overwrite);
        
        private object[] ServerConfigGetUpdateInternal(string typeKey)
        {
            var results = new object[5];
            var cfgUpdate = ServerConfigGetUpdate(typeKey);
            if (cfgUpdate == null)
            {
                return null;
            }
            
            results[0] = (int)cfgUpdate.WorldOpKind;
            results[1] = cfgUpdate.Error;
            results[2] = cfgUpdate.TriggeredBy;
            results[3] = cfgUpdate.ServerIteration;
            results[4] = cfgUpdate.CurrentFile;
            return results;
        }
    }
}