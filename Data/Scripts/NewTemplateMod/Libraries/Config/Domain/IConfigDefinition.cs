namespace mz.Config.Domain
{
    public interface IConfigDefinition
    {
        string TypeName { get; }
        string SectionName { get; } // TOML section, usually ConfigNameOverride
        System.Type ConfigType { get; }
        ConfigLocationType[] SupportedLocations { get; }

        ConfigBase CreateDefaultInstance();
    }
}