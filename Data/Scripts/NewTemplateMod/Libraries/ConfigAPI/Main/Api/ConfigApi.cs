using System;
using MarcoZechner.ConfigAPI.Shared.Abstractions;
using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public class ConfigApi : IConfigApi
    {
        public ConfigBase Get(string typeName, FileLocation location)
        {
            throw new NotImplementedException();
        }

        public bool TryLoad(string typeName, FileLocation location, string presetName)
        {
            throw new NotImplementedException();
        }

        public void Save(string typeName, FileLocation location, string presetName)
        {
            throw new NotImplementedException();
        }

        public string GetCurrentFileName(string typeName, FileLocation location)
        {
            throw new NotImplementedException();
        }
    }
}