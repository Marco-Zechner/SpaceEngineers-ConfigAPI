using mz.Config.Abstractions;
using mz.Config.Core;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests
{
    [TestFixture]
    public class TomlRoundtripTests
    {
        [Test]
        public void Toml_Roundtrip_UsesXmlBridge()
        {
            IConfigXmlSerializer xml = new TestXmlSerializer(); // your test-side XML impl
            TomlConfigSerializer ser = new TomlConfigSerializer(xml);

            IConfigDefinition def = new ConfigDefinition<RoundtripConfig>("RoundtripConfig");

            RoundtripConfig original = new RoundtripConfig
            {
                Flag = true,
                Count = 42,
                Message = "hi there"
            };

            string toml = ser.Serialize(original);
            ConfigBase restoredBase = ser.Deserialize(def, toml);
            RoundtripConfig restored = (RoundtripConfig)restoredBase;

            Assert.That(restored.Flag, Is.True);
            Assert.That(restored.Count, Is.EqualTo(42));
            Assert.That(restored.Message, Is.EqualTo("hi there"));
        }
    }
}