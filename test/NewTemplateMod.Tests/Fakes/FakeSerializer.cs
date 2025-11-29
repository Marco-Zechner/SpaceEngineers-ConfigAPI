using mz.Config.Abstractions;
using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    public class FakeSerializer : IConfigSerializer
    {
        public string LastSerializedContent;
        public ConfigBase LastSerializedInstance;

        public IConfigDefinition LastDeserializeDefinition;
        public string LastDeserializeContent;
        public ConfigBase DeserializeResult;

        public string Serialize(ConfigBase config)
        {
            LastSerializedInstance = config;
            LastSerializedContent = "SERIALIZED:" + config.GetType().Name + ":" + DateTime.Now.Ticks;
            return LastSerializedContent;
        }

        public ConfigBase Deserialize(IConfigDefinition def, string content)
        {
            LastDeserializeDefinition = def;
            LastDeserializeContent = content;
            return DeserializeResult;
        }

        public ITomlModel ParseToModel(string tomlContent)
        {
            throw new NotImplementedException();
        }

        public string SerializeModel(ITomlModel model)
        {
            throw new NotImplementedException();
        }

        public ITomlModel BuildDefaultModel(IConfigDefinition definition)
        {
            throw new NotImplementedException();
        }
    }
}
