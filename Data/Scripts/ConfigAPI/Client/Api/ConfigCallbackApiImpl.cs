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
                { "TestCallback", new Action(TestCallback) },
                { "NewDefault", new Func<string, object>(NewDefault) },
                { "SerializeToInternalXml", new Func<string, object, bool, string>(SerializeToInternalXml) },
                { "DeserializeFromInternalXml", new Func<string, string, object>(DeserializeFromInternalXml) },
                { "GetVariableDescriptions", new Func<string, IReadOnlyDictionary<string, string>>(GetVariableDescriptions) },
                { "LoadFile", new Func<int, string, string>((i, f) => LoadFile((LocationType)i, f)) },
                { "SaveFile", new Action<int, string, string>((i, f, c) => SaveFile((LocationType)i, f, c)) },
                { "BackupFile", new Action<int, string>((i, f) => BackupFile((LocationType)i, f)) },
            };
        }
    }
}