using System.Collections.Generic;
using mz.Config.Abstractions;

namespace mz.Config.Core
{
    internal class TomlModel : ITomlModel
    {
        public string TypeName { get; set; }
        public string StoredVersion { get; set; }

        public Dictionary<string, ITomlEntry> Entries { get; } = new Dictionary<string, ITomlEntry>();
    }
}