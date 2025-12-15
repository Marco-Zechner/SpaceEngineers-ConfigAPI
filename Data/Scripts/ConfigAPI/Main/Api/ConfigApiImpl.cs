using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;
using Sandbox.ModAPI;

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

        public void Test()
        {
            MyAPIGateway.Utilities.ShowMessage("ConfigAPI-host", "Test invoked");
            _configCallbackApi.TestCallback();
        }

        public object GetConfig(string typeKey, int locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public object LoadConfig(string typeKey, int locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public bool SaveConfig(string typeKey, int locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public bool WorldOpen(string typeKey, string defaultFile)
        {
            throw new Exception("Not Implemented");
        }

        public CfgUpdate WorldGetUpdate(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public object WorldGetAuth(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public object WorldGetDraft(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public void WorldResetDraft(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public bool WorldLoadAndSwitch(string typeKey, string file, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public bool WorldSave(string typeKey, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public bool WorldSaveAndSwitch(string typeKey, string file, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public bool WorldExport(string typeKey, string file, ulong baseIteration)
        {
            throw new Exception("Not Implemented");
        }

        public WorldMeta WorldGetMeta(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                { nameof(Test), new Action(Test) },
                { nameof(GetConfig), new Func<string, int, string, object>(GetConfig) },
                { nameof(LoadConfig), new Func<string, int, string, object>(LoadConfig) },
                { nameof(SaveConfig), new Func<string, int, string, bool>(SaveConfig) },
                { nameof(WorldOpen), new Func<string, string, bool>(WorldOpen) },
                { nameof(WorldGetUpdate), new WorldTryDequeueUpdateDelegate(WorldTryDequeueUpdate) },
                { nameof(WorldGetAuth), new Func<string, object>(WorldGetAuth) },
                { nameof(WorldGetDraft), new Func<string, object>(WorldGetDraft) },
                { nameof(WorldResetDraft), new Action<string>(WorldResetDraft) },
                { nameof(WorldLoadAndSwitch), new Func<string, string, ulong, bool>(WorldLoadAndSwitch) },
                { nameof(WorldSave), new Func<string, ulong, bool>(WorldSave) },
                { nameof(WorldSaveAndSwitch), new Func<string, string, ulong, bool>(WorldSaveAndSwitch) },
                { nameof(WorldExport), new Func<string, string, ulong, bool>(WorldExport) },
                { nameof(WorldGetMeta), new WorldTryGetMetaDelegate(WorldTryGetMeta) },
            };
        }
        
        // ===============================================================
        // Internal conversion methods for delegate to custom types
        // ===============================================================
        
        private bool WorldTryDequeueUpdate(
            string typeKey,
            out int worldOpKindEnum,
            out string error,
            out long triggeredBy,
            out ulong serverIteration,
            out string currentFile)
        {
            var cfgUpdate = WorldGetUpdate(typeKey);
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

        private bool WorldTryGetMeta(
            string typeKey,
            out ulong serverIteration,
            out string currentFile,
            out bool requestInFlight)
        {
            var worldMeta = WorldGetMeta(typeKey);
            if (worldMeta == null)
            {
                serverIteration = 0;
                currentFile = null;
                requestInFlight = false;
                return false;
            }

            serverIteration = worldMeta.ServerIteration;
            currentFile = worldMeta.CurrentFile;
            requestInFlight = worldMeta.RequestInFlight;
            return true;
        }
    }
}