using System.Collections.Generic;
using mz.Config;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class CollectionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.3.0";

        public TriggerSave<List<string>> Tags { get; set; } = new List<string>() { "alpha", "beta" };

        public TriggerSave<Dictionary<string, int>> NamedValues { get; set; }
            = new Dictionary<string, int>() { { "start", 1 }, { "end", 10 } };

        public TriggerSave<SubConfig> Nested { get; set; } = new SubConfig();

        public class SubConfig
        {
            public TriggerSave<float> Threshold { get; set; } = 0.75f;
            public TriggerSave<bool> Allowed { get; set; } = true;
        }
    }


}