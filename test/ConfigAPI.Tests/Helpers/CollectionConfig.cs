using System.Collections.Generic;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Serialization;

namespace NewTemplateMod.Tests
{
    public class CollectionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.3.0";

        public List<string> Tags { get; set; }
        public SerializableDictionary<string, int> NamedValues { get; set; }
        public SubConfig Nested { get; set; }


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
            Nested = new SubConfig()
            {
                Threshold = 0.85f,
                Allowed = false
            };
        }
        
        
        public class SubConfig
        {
            public float Threshold { get; set; }
            public bool Allowed { get; set; }
        }
    }
}