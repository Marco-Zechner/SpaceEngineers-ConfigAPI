using mz.Config.Abstractions.Toml;

namespace mz.Config.Core.Toml
{
    internal class TomlEntry : ITomlEntry
    {
        public string Value { get; set; }
        public string DefaultValue { get; set; }
    }
}