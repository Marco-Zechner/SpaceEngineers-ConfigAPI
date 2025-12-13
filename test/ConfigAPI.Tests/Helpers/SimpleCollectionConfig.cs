using mz.Config.Domain;
using mz.SemanticVersioning;

namespace NewTemplateMod.Tests
{
    public class SimpleCollectionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public bool Enabled { get; set; } = true;
        public int[] IntArray { get; set; } = new[] { 1, 2, 3 };
        public string[] Names { get; set; } = new[] { "Alice", "Bob" };
    }
}