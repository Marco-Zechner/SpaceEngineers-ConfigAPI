using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    public class RoundtripConfig : ConfigBase
    {
        public override string ConfigVersion => "1.0.0";

        public bool Flag { get; set; } = false;
        public int Count { get; set; } = 0;
        public string Message { get; set; } = "hello";
    }
}