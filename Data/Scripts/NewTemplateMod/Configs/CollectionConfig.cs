using System.Collections.Generic;
using mz.Config;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Serialization;

namespace mz.NewTemplateMod
{
    public class CollectionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.3.0";

        public List<string> Tags { get; set; } = new List<string>() { "alpha", "beta" };

        public SerializableDictionary<string, int> NamedValues { get; set; }
            = new SerializableDictionary<string, int>();

        public CollectionConfig()
        {
            NamedValues.Dictionary.Add("start", 1);
            NamedValues.Dictionary.Add("end", 10);
        }

        public SubConfig Nested { get; set; } = new SubConfig();

        public class SubConfig
        {
            public float Threshold { get; set; } = 0.75f;
            public bool Allowed { get; set; } = true;
        }
    }


}