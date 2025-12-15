using System;
using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    internal sealed class ConfigCallbackApiImpl : IConfigCallbackApi, IApiProvider
    {
        public object NewDefault(string typeKey)
        {
            throw new Exception("Not implemented");
        }

        public bool IsInstanceOf(string typeKey, object instance)
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
                { nameof(NewDefault), new Func<string, object>(NewDefault) },
                { nameof(IsInstanceOf), new Func<string, object, bool>(IsInstanceOf) },
                { nameof(SerializeToInternalXml), new Func<string, object, bool, string>(SerializeToInternalXml) },
                { nameof(DeserializeFromInternalXml), new Func<string, string, object>(DeserializeFromInternalXml) },
                { nameof(GetVariableDescriptions), new Func<string, IReadOnlyDictionary<string, string>>(GetVariableDescriptions) },
                { nameof(LoadFile), new Func<int, string, string>(LoadFileInternal) },
                { nameof(SaveFile), new Action<int, string, string>(SaveFileInternal) },
                { nameof(BackupFile), new Action<int, string>(BackupFileInternal) },
            };
        }
        
        // ===============================================================
        // Internal conversion methods for delegate to custom types
        // ===============================================================
        
        private string LoadFileInternal(int locationTypeEnum, string filename) 
            => LoadFile((LocationType)locationTypeEnum, filename);
        
        private void SaveFileInternal(int locationTypeEnum, string filename, string content) 
            => SaveFile((LocationType)locationTypeEnum, filename, content);
        
        private void BackupFileInternal(int locationTypeEnum, string filename) 
            => BackupFile((LocationType)locationTypeEnum, filename);
        
    }
}