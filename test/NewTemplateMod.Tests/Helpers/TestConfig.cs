using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    public class TestConfig : ConfigBase
    {
        public int SomeValue { get; set; }

        public override string ConfigVersion => "0.1.0";

        public override string ConfigNameOverride => "TestConfig";
    }
}
