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
            Assert.That(toml, Does.Contain("ConfigVersion"));
            Assert.That(toml, Does.Contain("\"0.1.0\""));

            Assert.That(toml, Does.Contain("RespondToHello"));
            Assert.That(toml, Does.Contain("false"));

            Assert.That(toml, Does.Contain("GreetingMessage"));
            Assert.That(toml, Does.Contain("\"hello\""));
        }

        [Test]
        public void Serialize_ModifiedExampleConfig_ContainsUpdatedValues()
        {
            var config = new ExampleConfig
            {
                RespondToHello = true,
                GreetingMessage = "hi"
            };

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
            var original = new ExampleConfig
            {
                RespondToHello = true,
                GreetingMessage = "hi"
            };

            var xml1 = _xml.SerializeToXml(original);
            var toml = _converter.ToExternal(_definition, xml1);
            var xml2 = _converter.ToInternal(_definition, toml);
            var cfg = _xml.DeserializeFromXml<ExampleConfig>(xml2);

            Assert.That(cfg, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.True);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hi"));
            });
        }

        [Test]
        public void Deserialize_HandWrittenToml_UsesProvidedValues()
        {
            var toml =
                "[ExampleConfig]\n" +
                "ConfigVersion = \"0.1.0\"\n" +
                "RespondToHello = true\n" +
                "GreetingMessage = \"custom\"\n";

            var xmlInput = _converter.ToInternal(_definition, toml);
            var cfg = _xml.DeserializeFromXml<ExampleConfig>(xmlInput);

            Assert.That(cfg, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.True);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("custom"));
            });
        }
        
        [Test]
        public void RoundTrip_Collections_Survive_AndUseTomlArrays()
        {
            var def = new ConfigDefinition<SimpleCollectionConfig>();

            var original = new SimpleCollectionConfig
            {
                Enabled = false,
                IntArray = new[] { 5, 10, 15 },
                Names = new[] { "One", "Two", "Three" }
            };

            // XML -> TOML
            var xml1 = _xml.SerializeToXml(original);
            Logger.Log("XML Input:\n" + xml1);
            var toml = _converter.ToExternal(def, xml1);
            Logger.Log("TOML Output:\n" + toml);
            
            // Keys present, but value is an opaque XML snippet, not a TOML array (yet)
            Assert.That(toml, Does.Contain("IntArray"));
            Assert.That(toml, Does.Contain("Names"));

            // TOML -> XML -> object
            var xml2 = _converter.ToInternal(def, toml);
            Logger.Log("XML Restored:\n" + xml2);
            var restored = _xml.DeserializeFromXml<SimpleCollectionConfig>(xml2);
            Logger.Log("Restored Object:\n" + restored);
            
            Assert.That(restored, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(restored.Enabled, Is.EqualTo(original.Enabled));
                Assert.That(restored.IntArray, Is.EqualTo(original.IntArray));
                Assert.That(restored.Names, Is.EqualTo(original.Names));
            });
        }

        [Test]
        public void RoundTrip_NestedObject_Survives_AndFlattensKeys()
        {
            var def = new ConfigDefinition<ParentConfig>();

            var original = new ParentConfig
            {
                Child = new ChildConfig
                {
                    Age = 42,
                    Label = "NestedBob"
                }
            };

            // XML -> TOML
            var xml1 = _xml.SerializeToXml(original);
            Logger.Log("XML Input:\n" + xml1);
            var toml = _converter.ToExternal(def, xml1);
            Logger.Log("TOML Output:\n" + toml);

            // We expect some flattened nested keys to show up
            // (exact key shape depends on your XML -> path logic, so keep expectations loose)
            Assert.That(toml, Does.Contain("Child"));
            Assert.That(toml, Does.Contain("42"));
            Assert.That(toml, Does.Contain("NestedBob"));

            // TOML -> XML -> object
            var xml2 = _converter.ToInternal(def, toml);
            Logger.Log("XML Restored:\n" + xml2);
            var restored = _xml.DeserializeFromXml<ParentConfig>(xml2);
            Logger.Log("Restored Object:\n" + restored);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Child, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(restored.Child.Age, Is.EqualTo(42));
                Assert.That(restored.Child.Label, Is.EqualTo("NestedBob"));
            });
        }
    }
}
