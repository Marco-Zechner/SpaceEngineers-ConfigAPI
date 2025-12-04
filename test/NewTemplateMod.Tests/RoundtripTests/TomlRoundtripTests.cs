using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using NUnit.Framework;

namespace NewTemplateMod.Tests.RoundtripTests
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
        
        [Test]
        [Timeout(10000)]
        public void Toml_Roundtrip_Supports_Collections()
        {
            IConfigXmlSerializer xml = new TestXmlSerializer();
            var converter = new TomlXmlConverter(xml);

            IConfigDefinition def = new ConfigDefinition<SimpleCollectionConfig>();

            var original = new SimpleCollectionConfig
            {
                IntArray = new[] { 5, 10, 15 },
                Names = new[] { "One", "Two", "Three" }
            };

            var xml1 = xml.SerializeToXml(original);
            Logger.Log("XML1:\n" + xml1);
            var toml = converter.ToExternal(def, xml1);
            Logger.Log("TOML:\n" + toml);
            var xml2 = converter.ToInternal(def, toml);
            Logger.Log("XML2:\n" + xml2);
            var restored = xml.DeserializeFromXml<SimpleCollectionConfig>(xml2);
            Logger.Log("Restored:\n" + restored);

            Assert.Multiple(() =>
            {
                // object roundtrip
                Assert.That(restored.IntArray, Is.EqualTo(new[] { 5, 10, 15 }));
                Assert.That(restored.Names, Is.EqualTo(new[] { "One", "Two", "Three" }));

                // TOML contains the keys (value is an opaque XML blob)
                Assert.That(toml, Does.Contain("IntArray"));
                Assert.That(toml, Does.Contain("Names"));
            });
        }

        [Test]
        [Timeout(10000)]
        public void Toml_Roundtrip_Supports_NestedObjects()
        {
            IConfigXmlSerializer xml = new TestXmlSerializer();
            var converter = new TomlXmlConverter(xml);

            IConfigDefinition def = new ConfigDefinition<ParentConfig>();

            var original = new ParentConfig
            {
                Child = new ChildConfig
                {
                    Age = 42,
                    Name = "NestedBob"
                }
            };

            var xml1 = xml.SerializeToXml(original);
            Logger.Log("XML1:\n" + xml1);
            var toml = converter.ToExternal(def, xml1);
            Logger.Log("TOML:\n" + toml);
            var xml2 = converter.ToInternal(def, toml);
            Logger.Log("XML2:\n" + xml2);
            var restored = xml.DeserializeFromXml<ParentConfig>(xml2);
            Logger.Log("Restored:\n" + restored);
            
            Assert.Multiple(() =>
            {
                Assert.That(restored.Child, Is.Not.Null);
                Assert.That(restored.Child.Age, Is.EqualTo(42));
                Assert.That(restored.Child.Name, Is.EqualTo("NestedBob"));

                // We at least see the nested blob
                Assert.That(toml, Does.Contain("Child"));
                Assert.That(toml, Does.Contain("42"));
                Assert.That(toml, Does.Contain("NestedBob"));
            });
        }
    }
}