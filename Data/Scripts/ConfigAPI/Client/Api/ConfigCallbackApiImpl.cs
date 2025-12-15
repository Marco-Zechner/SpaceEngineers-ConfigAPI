using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    internal sealed class ConfigCallbackApiImpl : IConfigCallbackApi, IApiProvider
    {
        public void TestCallback()
        {
            // ApiBridge.Log.Info(ConfigApiTopics.Callbacks, 0, "TestCallback invoked");
        }

        public object NewDefault(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public string SerializeToInternalXml(string typeKey, object instance, bool includeComments)
        {
            throw new Exception("Not Implemented");
        }

        public object DeserializeFromInternalXml(string typeKey, string internalXml)
        {
            throw new Exception("Not Implemented");
        }

        public IReadOnlyDictionary<string, string> GetVariableDescriptions(string typeKey)
        {
            throw new Exception("Not Implemented");
        }

        public string LoadFile(LocationType locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }

        public void SaveFile(LocationType locationType, string filename, string content)
        {
            throw new Exception("Not Implemented");
        }

        public void BackupFile(LocationType locationType, string filename)
        {
            throw new Exception("Not Implemented");
        }
        
        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                { nameof(TestCallback), new Action(TestCallback) },
                { nameof(NewDefault), new Func<string, object>(NewDefault) },
                { nameof(SerializeToInternalXml), new Func<string, object, bool, string>(SerializeToInternalXml) },
                { nameof(DeserializeFromInternalXml), new Func<string, string, object>(DeserializeFromInternalXml) },
                { nameof(GetVariableDescriptions), new Func<string, IReadOnlyDictionary<string, string>>(GetVariableDescriptions) },
                { nameof(LoadFile), new Func<int, string, string>(LoadFile) },
                { nameof(SaveFile), new Action<int, string, string>(SaveFile) },
                { nameof(BackupFile), new Action<int, string>(BackupFile) },
            };
        }
        
        // ===============================================================
        // Internal conversion methods for delegate to custom types
        // ===============================================================
        
        private string LoadFile(int locationTypeEnum, string filename) 
            => LoadFile((LocationType)locationTypeEnum, filename);
        
        private void SaveFile(int locationTypeEnum, string filename, string content) 
            => SaveFile((LocationType)locationTypeEnum, filename, content);
        
        private void BackupFile(int locationTypeEnum, string filename) 
            => BackupFile((LocationType)locationTypeEnum, filename);
    }
}