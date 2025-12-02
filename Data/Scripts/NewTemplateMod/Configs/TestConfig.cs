using mz.Config.Domain;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class TestConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";
        public override string ConfigNameOverride => "ClientConfig";

        public bool RespondToHello = false;
        public string GreetingMessage {get; set; }= "Hello, world!";
    }
}