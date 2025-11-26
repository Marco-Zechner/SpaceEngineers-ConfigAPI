using mz.Config;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class TestConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";
        public override string ConfigNameOverride => "Hello :)";

        public CfgVal<bool> RespondToHello { get; set; } = false;
        public CfgVal<bool> RespondToHello2 = false;
        public CfgVal<string> GreetingMessage { get; set; } = "Hello, world!";

    }
}