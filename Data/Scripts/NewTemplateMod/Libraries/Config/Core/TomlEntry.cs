using mz.Config.Abstractions;

namespace mz.Config.Core
{
    internal class TomlEntry : ITomlEntry
    {
        public string Value { get; set; }
        public string DefaultValue { get; set; }
    }
}