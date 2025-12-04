using System.Collections.Generic;
using mz.Config;
using mz.Config.Domain;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class IntermediateConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.2.0";

        public bool IsEnabled { get; set; } = true;

        public int? OptionalValue { get; set; } = null;

        public Mode CurrentMode = Mode.Basic;

        // hmmm should all variables that use enums automatically have their valid values as a comment above them?
        public enum Mode
        {
            Basic,
            Advanced,
            Expert
        }
        
        private static readonly IReadOnlyDictionary<string, string> _descriptions =
            new Dictionary<string, string>
            {
                {
                    nameof(CurrentMode),
                    "Select the operating mode.\n" +
                    "Valid values: Basic, Advanced, Expert."
                },
                {
                    nameof(OptionalValue),
                    "Optional integer value.\n" +
                    "Leave empty to use no explicit value (null)."
                },
                {
                    nameof(IsEnabled),
                    "Master switch for this feature.\n" +
                    "true = enabled, false = disabled."
                }
            };

        public override IReadOnlyDictionary<string, string> VariableDescriptions => _descriptions;
    }
}