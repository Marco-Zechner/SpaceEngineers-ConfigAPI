using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Client.Core
{
    public interface IConfigDefinitionClient
    {
        ConfigBase NewDefault();
        string SerializeToInternalXml(ConfigBase instance);
        ConfigBase DeserializeFromInternalXml(string internalXml);
        IReadOnlyDictionary<string, string> GetVariableDescriptions();
    }
}