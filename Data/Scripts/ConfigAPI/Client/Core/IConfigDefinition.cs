using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Client.Core
{
    internal interface IConfigDefinition
    {
        ConfigBase NewDefault();
        string SerializeToInternalXml(ConfigBase instance);
        ConfigBase DeserializeFromInternalXml(string internalXml);
        IReadOnlyDictionary<string, string> GetVariableDescriptions();
    }
}