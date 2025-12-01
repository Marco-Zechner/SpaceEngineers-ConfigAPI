using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using NUnit.Framework;

namespace NewTemplateMod.Tests
{
    [TestFixture]
    public class TomlRoundtripTests
    {
        [Test]
        public void Toml_Roundtrip_UsesXmlBridge()
        {
            IConfigXmlSerializer xml = new TestXmlSerializer();
            var converter = new TomlXmlConverter(xml);

            IConfigDefinition def = new ConfigDefinition<RoundtripConfig>();

            var original = new RoundtripConfig
            {
                Flag = true,
                Count = 42,
                Message = "hi there"
            };

            // XML -> TOML -> XML -> object
            var xml1 = xml.SerializeToXml(original);
            var toml = converter.ToExternal(def, xml1);
            var xml2 = converter.ToInternal(def, toml);
            var restored = xml.DeserializeFromXml<RoundtripConfig>(xml2);

            // Do a second roundtrip to ensure stability
            var xml3 = xml.SerializeToXml(restored);
            var toml2 = converter.ToExternal(def, xml3);
            var xml4 = converter.ToInternal(def, toml2);
            var restored2 = xml.DeserializeFromXml<RoundtripConfig>(xml4);

            Assert.Multiple(() =>
            {
                Assert.That(restored.Flag, Is.True);
                Assert.That(restored.Count, Is.EqualTo(42));
                Assert.That(restored.Message, Is.EqualTo("hi there"));

                Assert.That(restored2.Flag, Is.True);
                Assert.That(restored2.Count, Is.EqualTo(42));
                Assert.That(restored2.Message, Is.EqualTo("hi there"));
            });
        }
    }
}