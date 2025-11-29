// using mz.Config;
// using mz.SemanticVersioning;

// namespace mz.NewTemplateMod
// {
//     public class TestConfig : ConfigBase
//     {
//         public override SemanticVersion ConfigVersion => "0.1.0";
//         public override string ConfigNameOverride => "ClientConfig";

//         public TriggerSave<bool> RespondToHello = false;
//         public TriggerSave<string> GreetingMessage {get; set; }= "Hello, world!";
//     }
// }