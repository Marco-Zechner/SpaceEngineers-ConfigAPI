using mz.Config.Abstractions.Toml;
using mz.Config.Domain;

namespace mz.Config.Abstractions
{
    public interface IConfigSerializer
    {
        string Serialize(ConfigBase config);
        ConfigBase Deserialize(IConfigDefinition definition, string tomlContent);

        // For migration / layout checking:
        ITomlModel ParseToModel(string tomlContent);
        string SerializeModel(ITomlModel model);
        ITomlModel BuildDefaultModel(IConfigDefinition definition);
    }
}