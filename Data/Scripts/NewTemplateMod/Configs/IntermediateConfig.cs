// using mz.Config;
// using mz.SemanticVersioning;

// namespace mz.NewTemplateMod
// {
//     public class IntermediateConfig : ConfigBase
//     {
//         public override SemanticVersion ConfigVersion => "0.2.0";

//         public TriggerSave<bool> IsEnabled { get; set; } = true;

//         public TriggerSave<int?> OptionalValue { get; set; } = null;

//         public TriggerSave<Mode> CurrentMode = Mode.Basic;

//         public enum Mode
//         {
//             Basic,
//             Advanced,
//             Expert
//         }
//     }
// }