using System.Collections.Generic;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Serialization;

namespace mz.NewTemplateMod
{
    public class CollectionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.3.0";

        public List<string> Tags { get; set; }

        public SerializableDictionary<string, int> NamedValues { get; set; }

        public SubConfig Nested { get; set; } = new SubConfig();

        public class SubConfig
        {
            public float Threshold { get; set; } = 0.75f;
            public bool Allowed { get; set; } = true;
        }

        public override void ApplyDefaults()
        {
            Tags = new List<string>() { "alpha", "beta" };
            NamedValues = new SerializableDictionary<string, int>
            {
                Dictionary = new Dictionary<string, int>()
                {
                    { "start", 1 },
                    { "end", 10 }
                }
            };
        }
    }
}