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
                ["Test"] = d => _test = (Action)d,
                ["WorldOpen"] = d => _worldOpen = (Func<string, string, bool>)d,
                ["WorldTryDequeueUpdate"] = d => _worldTryDequeueUpdate = (WorldTryDequeueUpdateDelegate)d,
                ["WorldGetAuth"] = d => _worldGetAuth = (Func<string, object>)d,
                ["WorldGetDraft"] = d => _worldGetDraft = (Func<string, object>)d,
                ["WorldResetDraft"] = d => _worldResetDraft = (Action<string>)d,
                ["WorldLoadAndSwitch"] = d => _worldLoadAndSwitch = (Func<string, string, ulong, bool>)d,
                ["WorldSave"] = d => _worldSave = (Func<string, ulong, bool>)d,
                ["WorldSaveAndSwitch"] = d => _worldSaveAndSwitch = (Func<string, string, ulong, bool>)d,
                ["WorldExport"] = d => _worldExport = (Func<string, string, ulong, bool>)d,
                ["WorldTryGetMeta"] = d => _worldTryGetMeta = (WorldTryGetMetaDelegate)d,
            };
            
            foreach (var assignment in assignments)
            {
                var endpointName = assignment.Key;
                var endpointFunc = assignment.Value;
                Delegate del;
                if (source.TryGetValue(endpointName, out del))
                    endpointFunc(del);
            }
        }

        public void Test() 
            => _test?.Invoke();

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