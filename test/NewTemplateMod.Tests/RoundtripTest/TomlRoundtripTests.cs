using mz.Config.Abstractions;
using mz.Config.Core;
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
            var ser = new TomlConfigSerializer(xml);

            IConfigDefinition def = new ConfigDefinition<RoundtripConfig>("RoundtripConfig");

            var original = new RoundtripConfig
            {
                Flag = true,
                Count = 42,
                Message = "hi there"
            };

            var toml = ser.Serialize(original);
            var restoredBase = ser.Deserialize(def, toml);
            var restored = (RoundtripConfig)restoredBase;
            
            var toml2 = ser.Serialize(restored);
            var restoredBase2 = ser.Deserialize(def, toml2);
            var restored2 = (RoundtripConfig)restoredBase2;
            
            Assert.That(restored.Flag, Is.True);
            Assert.That(restored.Count, Is.EqualTo(42));
            Assert.That(restored.Message, Is.EqualTo("hi there"));
            
            Assert.That(restored2.Flag, Is.True);
            Assert.That(restored2.Count, Is.EqualTo(42));
            Assert.That(restored2.Message, Is.EqualTo("hi there"));
        }
    }
}