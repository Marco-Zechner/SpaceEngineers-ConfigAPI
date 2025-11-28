using mz.Config;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class TestConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";

        public CfgVal<bool> RespondToHello { get; set; } = false;
        public CfgVal<string> GreetingMessage { get; set; } = "Hello, world!";
    }
}