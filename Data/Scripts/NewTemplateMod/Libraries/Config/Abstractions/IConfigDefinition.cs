using System;
using mz.Config.Domain;

namespace mz.Config.Abstractions
{
    public interface IConfigDefinition
    {
        string TypeName { get; }         // usually typeof(T).Name
        string SectionName { get; }      // TOML section name, e.g. "ExampleConfig"
        Type ConfigType { get; }

        ConfigBase CreateDefaultInstance();
    }
}