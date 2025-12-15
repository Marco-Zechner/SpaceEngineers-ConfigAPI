using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public sealed class ConfigApi : IConfigApi
    {
        private Action _test;
        private Func<string, int, string, object> _getConfig;
        private Func<string, int, string, object> _loadConfig;
        private Func<string, int, string, bool> _saveConfig;
        private Func<string, string, bool> _worldOpen;
        private WorldTryDequeueUpdateDelegate _worldTryDequeueUpdate;
        private Func<string, object> _worldGetAuth;
        private Func<string, object> _worldGetDraft;
        private Action<string> _worldResetDraft;
        private Func<string, string, ulong, bool> _worldLoadAndSwitch;
        private Func<string, ulong, bool> _worldSave;
        private Func<string, string, ulong, bool> _worldSaveAndSwitch;
        private Func<string, string, ulong, bool> _worldExport;
        private WorldTryGetMetaDelegate _worldTryGetMeta;
        
        public ConfigApi(IApiProvider mainApi)
        {
            var source = mainApi.ConvertToDict();
            if (source == null)
                return;

            var assignments = new Dictionary<string, Action<Delegate>>
            {
                [nameof(Test)] = d => _test = (Action)d,
                [nameof(GetConfig)] = d => _getConfig = (Func<string, int, string, object>)d,
                [nameof(LoadConfig)] = d => _loadConfig = (Func<string, int, string, object>)d,
                [nameof(SaveConfig)] = d => _saveConfig = (Func<string, int, string, bool>)d,
                [nameof(WorldOpen)] = d => _worldOpen = (Func<string, string, bool>)d,
                [nameof(WorldGetUpdate)] = d => _worldTryDequeueUpdate = (WorldTryDequeueUpdateDelegate)d,
                [nameof(WorldGetAuth)] = d => _worldGetAuth = (Func<string, object>)d,
                [nameof(WorldGetDraft)] = d => _worldGetDraft = (Func<string, object>)d,
                [nameof(WorldResetDraft)] = d => _worldResetDraft = (Action<string>)d,
                [nameof(WorldLoadAndSwitch)] = d => _worldLoadAndSwitch = (Func<string, string, ulong, bool>)d,
                [nameof(WorldSave)] = d => _worldSave = (Func<string, ulong, bool>)d,
                [nameof(WorldSaveAndSwitch)] = d => _worldSaveAndSwitch = (Func<string, string, ulong, bool>)d,
                [nameof(WorldExport)] = d => _worldExport = (Func<string, string, ulong, bool>)d,
                [nameof(WorldGetMeta)] = d => _worldTryGetMeta = (WorldTryGetMetaDelegate)d,
            };
            
            foreach (var assignment in assignments)
            {
                var endpointName = assignment.Key;
                var endpointFunc = assignment.Value;
                Delegate del;
                if (source.TryGetValue(endpointName, out del))
                    endpointFunc(del);

                if (del == null)
                    throw new Exception($"ConfigApi: Missing implementation for '{endpointName}'");
            }
        }

        public void Test() 
            => _test?.Invoke();

        public object GetConfig(string typeKey, int locationType, string filename)
            => _getConfig?.Invoke(typeKey, locationType, filename);

        public object LoadConfig(string typeKey, int locationType, string filename)
            => _loadConfig?.Invoke(typeKey, locationType, filename);

        public bool SaveConfig(string typeKey, int locationType, string filename)
            => _saveConfig?.Invoke(typeKey, locationType, filename) ?? false;

        public bool WorldOpen(string typeKey, string defaultFile)
            => _worldOpen?.Invoke(typeKey, defaultFile) ?? false;

        public CfgUpdate WorldGetUpdate(string typeKey)
        {
            var del = _worldTryDequeueUpdate;
            if (del == null)
                return null;

            int kindInt;
            string error;
            long triggeredBy;
            ulong serverIteration;
            string currentFile;

            var has = del(
                typeKey,
                out kindInt,
                out error,
                out triggeredBy,
                out serverIteration,
                out currentFile
            );

            if (!has)
                return null;

            return new CfgUpdate
            {
                WorldOpKind = (WorldOpKind)kindInt,
                Error = error,
                TriggeredBy = triggeredBy,
                ServerIteration = serverIteration,
                CurrentFile = currentFile
            };
        }

        public object WorldGetAuth(string typeKey)
            => _worldGetAuth?.Invoke(typeKey);

        public object WorldGetDraft(string typeKey)
            => _worldGetDraft?.Invoke(typeKey);

        public void WorldResetDraft(string typeKey)
            => _worldResetDraft?.Invoke(typeKey);

        public bool WorldLoadAndSwitch(string typeKey, string file, ulong baseIteration)
            => _worldLoadAndSwitch?.Invoke(typeKey, file, baseIteration) ?? false;

        public bool WorldSave(string typeKey, ulong baseIteration)
            => _worldSave?.Invoke(typeKey, baseIteration) ?? false;

        public bool WorldSaveAndSwitch(string typeKey, string file, ulong baseIteration)
            => _worldSaveAndSwitch?.Invoke(typeKey, file, baseIteration) ?? false;

        public bool WorldExport(string typeKey, string file, ulong baseIteration)
            => _worldExport?.Invoke(typeKey, file, baseIteration) ?? false;

        public WorldMeta WorldGetMeta(string typeKey)
        {
            var del = _worldTryGetMeta;
            if (del == null)
                return null;

            ulong serverIteration;
            string currentFile;
            bool requestInFlight;

            var has = del(typeKey, out serverIteration, out currentFile, out requestInFlight);
            if (!has)
                return null;

            return new WorldMeta
            {
                ServerIteration = serverIteration,
                CurrentFile = currentFile,
                RequestInFlight = requestInFlight
            };
        }
    }
}