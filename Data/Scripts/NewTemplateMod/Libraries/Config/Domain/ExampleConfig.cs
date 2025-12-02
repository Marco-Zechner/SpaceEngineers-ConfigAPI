using mz.SemanticVersioning;

namespace mz.Config.Domain
{
    public class ExampleConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";
        public override string ConfigNameOverride => "ExampleConfig";
        // Example settings
        public bool RespondToHello { get; set; } = false;
        public string GreetingMessage { get; set; } = "hello";
    }
}
