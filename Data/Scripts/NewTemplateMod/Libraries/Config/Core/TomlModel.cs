using System.Collections.Generic;
using mz.Config.Abstractions;

namespace mz.Config.Core
{
    internal class TomlModel : ITomlModel
    {
        private readonly Dictionary<string, ITomlEntry> _entries =
            new Dictionary<string, ITomlEntry>();

        public string TypeName { get; set; }
        public string StoredVersion { get; set; }

        public Dictionary<string, ITomlEntry> Entries
        {
            get { return _entries; }
        }
    }
}