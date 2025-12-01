using mz.Config.Abstractions;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.SerializationTests
{
    [TestFixture]
    public class TomlXmlConverterTests
    {
        private TestXmlSerializer _xml;
        private TomlXmlConverter _converter;
        private IConfigDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _xml = new TestXmlSerializer();
            _converter = new TomlXmlConverter(_xml);
            _definition = new ConfigDefinition<ExampleConfig>();
        }

        [Test]
        public void Serialize_DefaultExampleConfig_ContainsExpectedLines()
        {
            var config = new ExampleConfig();

            var xmlContent = _xml.SerializeToXml(config);
            var toml = _converter.ToExternal(_definition, xmlContent);

            Assert.That(toml, Does.Contain("[ExampleConfig]"));
            Assert.That(toml, Does.Contain("StoredVersion"));
            Assert.That(toml, Does.Contain("\"0.1.0\""));

            Assert.That(toml, Does.Contain("RespondToHello"));
            Assert.That(toml, Does.Contain("false"));

            Assert.That(toml, Does.Contain("GreetingMessage"));
            Assert.That(toml, Does.Contain("\"hello\""));
        }

        [Test]
        public void Serialize_ModifiedExampleConfig_ContainsUpdatedValues()
        {
            var config = new ExampleConfig();
            config.RespondToHello = true;
            config.GreetingMessage = "hi";

            var xmlContent = _xml.SerializeToXml(config);
            var toml = _converter.ToExternal(_definition, xmlContent);

            Assert.That(toml, Does.Contain("RespondToHello"));
            Assert.That(toml, Does.Contain("true"));

            Assert.That(toml, Does.Contain("GreetingMessage"));
            Assert.That(toml, Does.Contain("\"hi\""));
        }

        [Test]
        public void Deserialize_RoundTrip_ProducesEquivalentConfig()
        {
            var original = new ExampleConfig();
            original.RespondToHello = true;
            original.GreetingMessage = "hi";

            var xml1 = _xml.SerializeToXml(original);
            var toml = _converter.ToExternal(_definition, xml1);
            var xml2 = _converter.ToInternal(_definition, toml);
            var cfg = _xml.DeserializeFromXml<ExampleConfig>(xml2);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg.RespondToHello, Is.True);
            Assert.That(cfg.GreetingMessage, Is.EqualTo("hi"));
        }

        [Test]
        public void Deserialize_HandWrittenToml_UsesProvidedValues()
        {
            var toml =
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.1.0\"\n" +
                "RespondToHello = true\n" +
                "GreetingMessage = \"custom\"\n";

            var xmlInput = _converter.ToInternal(_definition, toml);
            var cfg = _xml.DeserializeFromXml<ExampleConfig>(xmlInput);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg.RespondToHello, Is.True);
            Assert.That(cfg.GreetingMessage, Is.EqualTo("custom"));
        }
    }
}
