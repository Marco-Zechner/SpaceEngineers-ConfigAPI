using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public class ConfigCallbackApi : IConfigCallbackApi
    {
        private Action _testCallback;
        private Func<string, object> _newDefault;
        private Func<string, object, bool, string> _serializeToInternalXml;
        private Func<string, string, object> _deserializeFromInternalXml;
        private Func<string, IReadOnlyDictionary<string, string>> _getVariableDescriptions;
        private Func<int, string, string> _loadFile;
        private Action<int, string, string> _saveFile;
        private Action<int, string> _backupFile;
        
        public ConfigCallbackApi(IApiProvider callbackApi)
        {
            var source = callbackApi.ConvertToDict();
            if (source == null)
                return;

            var assignments = new Dictionary<string, Action<Delegate>>
            {
                ["TestCallback"] = d => _testCallback = (Action)d,
                ["NewDefault"] = d => _newDefault = (Func<string, object>)d,
                ["SerializeToInternalXml"] = d => _serializeToInternalXml = (Func<string, object, bool, string>)d,
                ["DeserializeFromInternalXml"] = d => _deserializeFromInternalXml = (Func<string, string, object>)d,
                ["GetVariableDescriptions"] = d => _getVariableDescriptions = (Func<string, IReadOnlyDictionary<string, string>>)d,
                ["LoadFile"] = d => _loadFile = (Func<int, string, string>)d,
                ["SaveFile"] = d => _saveFile = (Action<int, string, string>)d,
                ["BackupFile"] = d => _backupFile = (Action<int, string>)d,
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

        public void TestCallback() 
            => _testCallback?.Invoke();
        
        public object NewDefault(string typeKey) 
            => _newDefault?.Invoke(typeKey);

        public string SerializeToInternalXml(string typeKey, object instance, bool includeComments)
            => _serializeToInternalXml?.Invoke(typeKey, instance, includeComments);

        public object DeserializeFromInternalXml(string typeKey, string internalXml)
            => _deserializeFromInternalXml?.Invoke(typeKey, internalXml);

        public IReadOnlyDictionary<string, string> GetVariableDescriptions(string typeKey)
            => _getVariableDescriptions?.Invoke(typeKey);

        public string LoadFile(LocationType locationType, string filename)
            => _loadFile?.Invoke((int)locationType, filename);

        public void SaveFile(LocationType locationType, string filename, string content)
            => _saveFile?.Invoke((int)locationType, filename, content);

        public void BackupFile(LocationType locationType, string filename)
            => _backupFile?.Invoke((int)locationType, filename);
    }
}