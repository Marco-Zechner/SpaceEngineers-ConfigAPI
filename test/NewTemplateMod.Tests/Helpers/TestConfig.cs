using mz.Config.Domain;
using mz.SemanticVersioning;

namespace NewTemplateMod.Tests
{
    public class TestConfig : ConfigBase
    {
        public int SomeValue { get; set; }

        public override SemanticVersion ConfigVersion => "0.1.0";

        public override string ConfigNameOverride => "TestConfig";
    }
}
