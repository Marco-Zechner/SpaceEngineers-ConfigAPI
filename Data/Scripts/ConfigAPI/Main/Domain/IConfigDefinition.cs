using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Main.Domain
{
    public interface IConfigDefinition
    {
        string TypeName { get; }
        string GetCurrentDefaultsInternalXml();
        IReadOnlyDictionary<string,string> GetVariableDescriptions();
    }
}