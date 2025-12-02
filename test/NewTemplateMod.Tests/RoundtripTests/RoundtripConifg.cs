using mz.Config.Domain;
using mz.SemanticVersioning;

namespace NewTemplateMod.Tests.RoundtripTests
{
    public class RoundtripConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public bool Flag { get; set; } = false;
        public int Count { get; set; } = 0;
        public string Message { get; set; } = "hello";
    }
}