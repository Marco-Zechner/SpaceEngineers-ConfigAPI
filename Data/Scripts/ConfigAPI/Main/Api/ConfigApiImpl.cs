using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    /// <summary>
    /// Main API bound to a single consumer mod.
    /// No modId needs to be passed on calls anymore.
    /// </summary>
    public sealed class ConfigApiImpl : IConfigApi, IApiProvider
    {
        private readonly ulong _consumerModId;
        private readonly string _consumerModName;
        private readonly ConfigCallbackApi _configCallbackApi;

        public ConfigApiImpl(ulong modId, string modName, ConfigCallbackApi configCallbackApi)
        {
            _consumerModId = modId;
            _consumerModName = modName;
            _configCallbackApi = configCallbackApi;
        }

        // Client-side APIs
        
        public object ClientConfigGet(string typeKey, LocationType locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public object ClientConfigLoadAndSwitch(string typeKey, LocationType locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public bool ClientConfigSave(string typeKey, LocationType locationType)
        {
            throw new Exception("Not Implemented");
        }

        public object ClientConfigSaveAndSwitch(string typeKey, LocationType locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public bool ClientConfigExport(string typeKey, LocationType locationType, string filename, bool overwrite)
        {
            throw new Exception("Not Implemented");
        }

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
                { nameof(ClientConfigLoadAndSwitch), new Func<string, int, string, object>(ClientConfigLoadAndSwitchInternal) },
                { nameof(ClientConfigSave), new Func<string, int, bool>(ClientConfigSaveInternal) },
                { nameof(ClientConfigSaveAndSwitch), new Func<string, int, string, object>(ClientConfigSaveAndSwitchInternal) },
                { nameof(ClientConfigExport), new Func<string, int, string, bool, bool>(ClientConfigExportInternal) },
                { nameof(ServerConfigInit), new Func<string, string, object>(ServerConfigInit) },
                { nameof(ServerConfigGetUpdate), new WorldTryDequeueUpdateDelegate(WorldTryDequeueUpdate) },
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
        private object ClientConfigLoadAndSwitchInternal(string typeKey, int locationTypeEnum, string filename)
            => ClientConfigLoadAndSwitch(typeKey, (LocationType)locationTypeEnum, filename);
        private bool ClientConfigSaveInternal(string typeKey, int locationTypeEnum) 
            => ClientConfigSave(typeKey, (LocationType)locationTypeEnum);
        private object ClientConfigSaveAndSwitchInternal(string typeKey, int locationTypeEnum, string filename) 
            => ClientConfigSaveAndSwitch(typeKey, (LocationType)locationTypeEnum, filename);
        private bool ClientConfigExportInternal(string typeKey, int locationTypeEnum, string filename, bool overwrite) 
            => ClientConfigExport(typeKey, (LocationType)locationTypeEnum, filename, overwrite);
        
        private bool WorldTryDequeueUpdate(
            string typeKey,
            out int worldOpKindEnum,
            out string error,
            out long triggeredBy,
            out ulong serverIteration,
            out string currentFile)
        {
            var cfgUpdate = ServerConfigGetUpdate(typeKey);
            if (cfgUpdate == null)
            {
                worldOpKindEnum = 0;
                error = "No update available";
                triggeredBy = 0;
                serverIteration = 0;
                currentFile = null;
                return false;
            }
            
            worldOpKindEnum = (int)cfgUpdate.WorldOpKind;
            error = cfgUpdate.Error;
            triggeredBy = cfgUpdate.TriggeredBy;
            serverIteration = cfgUpdate.ServerIteration;
            currentFile = cfgUpdate.CurrentFile;
            return true;
        }
    }
}