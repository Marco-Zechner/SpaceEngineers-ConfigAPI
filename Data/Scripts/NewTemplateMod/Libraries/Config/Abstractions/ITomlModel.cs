using System.Collections.Generic;

namespace mz.Config.Abstractions
{
    public interface ITomlModel
    {
        string TypeName { get; set; }
        string StoredVersion { get; set; }
        // Key -> entry
        Dictionary<string, ITomlEntry> Entries { get; }
    }
}