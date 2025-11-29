using System;

namespace mz.Config.Domain
{
    public interface IConfigDefinition
    {
        string TypeName { get; }
        string SectionName { get; } // TOML section, usually ConfigNameOverride
        Type ConfigType { get; }
        ConfigLocationType[] SupportedLocations { get; }

        ConfigBase CreateDefaultInstance();
    }
}