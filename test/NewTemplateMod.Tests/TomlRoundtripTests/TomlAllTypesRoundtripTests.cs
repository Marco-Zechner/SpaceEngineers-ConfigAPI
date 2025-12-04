using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using NUnit.Framework;

namespace NewTemplateMod.Tests.TomlRoundtripTests
{
    [TestFixture]
    public class TomlAllTypesRoundtripTests
    {
        [Test]
        public void Xml_Serialize_Default_TomlAllTypesConfig_Contains_All_Declared_Fields()
        {
            IConfigXmlSerializer xml = new TestXmlSerializer();
            var cfg = new TomlAllTypesConfig();

            // Make sure all complex members get initialized as the game would.
            cfg.ApplyDefaults();

            var xmlText = xml.SerializeToXml(cfg);
            TestContext.Out.WriteLine("Default TomlAllTypesConfig XML:\n" + xmlText);

            Assert.Multiple(() =>
            {
                // primitives
                Assert.That(xmlText, Does.Contain("<ConfigVersion>1.0.0</ConfigVersion>"));
                Assert.That(xmlText, Does.Contain("<IntValue>123</IntValue>"));
                Assert.That(xmlText, Does.Contain("<DoubleValue>4.5</DoubleValue>"));
                Assert.That(xmlText, Does.Contain("<FloatValue>0.75</FloatValue>"));
                Assert.That(xmlText, Does.Contain("<BoolValue>true</BoolValue>"));
                Assert.That(xmlText, Does.Contain("<Text>Hello</Text>"));

                // nullable value types
                // we expect XmlSerializer to emit an element with xsi:nil="true"
                Assert.That(xmlText, Does.Contain("<OptionalInt"));
                Assert.That(xmlText, Does.Contain("OptionalInt xsi:nil=\"true\""));

                Assert.That(xmlText, Does.Contain("<OptionalFloat"));
                Assert.That(xmlText, Does.Contain("OptionalFloat xsi:nil=\"true\""));
                
                Assert.That(xmlText, Does.Contain("<OptionalText"));
                Assert.That(xmlText, Does.Contain("OptionalText xsi:nil=\"true\""));
                
                // collections
                Assert.That(xmlText, Does.Contain("<IntList>"));
                Assert.That(xmlText, Does.Contain("<string>")); // for StringList items
                Assert.That(xmlText, Does.Contain("<StringList>"));

                // dictionary wrapper + nested structure
                Assert.That(xmlText, Does.Contain("<NamedValues>"));
                Assert.That(xmlText, Does.Contain("<dictionary>"));
                Assert.That(xmlText, Does.Contain("<Key>start</Key>"));
                Assert.That(xmlText, Does.Contain("<Value>1</Value>"));
                Assert.That(xmlText, Does.Contain("<Key>end</Key>"));
                Assert.That(xmlText, Does.Contain("<Value>10</Value>"));

                // nested object
                Assert.That(xmlText, Does.Contain("<Nested>"));
                Assert.That(xmlText, Does.Contain("<Threshold>0.25</Threshold>"));
                Assert.That(xmlText, Does.Contain("<Flag>false</Flag>"));
            });
        }

                
        [Test]
        [Timeout(10000)]
        public void Toml_Roundtrip_Preserves_All_Types_And_Nulls()
        {
            // Arrange
            IConfigXmlSerializer xml = new TestXmlSerializer();
            var converter = new TomlXmlConverter(xml);
            IConfigDefinition def = new ConfigDefinition<TomlAllTypesConfig>();

            // Start from an instance with explicit values,
            // including some nulls and some non-defaults.
            var original = new TomlAllTypesConfig
            {
                IntValue = 999,
                DoubleValue = 12.5,
                FloatValue = 0.33f,
                BoolValue = false,
                Text = "Custom text",

                OptionalInt = null,
                OptionalFloat = null,
                OptionalText = null,
            };

            // Ensure defaults for collections & dict are applied as the game would.
            original.ApplyDefaults();

            // Tweak some defaults so we can see they survive.
            original.IntList.Add(42);               // [1,2,3,42]
            original.StringList.Add("extra");       // ["a","b","extra"]
            original.NamedValues.Dictionary["end"] = 99; // {start=1, end=99}
            original.Nested.Threshold = 0.9f;
            original.Nested.Flag = true;

            // Act: object -> XML -> TOML -> XML -> object
            var xml1 = xml.SerializeToXml(original);
            Logger.Log("xml1 Intermediate:\n" + xml1);
            var toml = converter.ToExternal(def, xml1);
            Logger.Log("TOML Output:\n" + toml);
            var xml2 = converter.ToInternal(def, toml);
            Logger.Log("xml2 Restored:\n" + xml2);
            var restored = xml.DeserializeFromXml<TomlAllTypesConfig>(xml2);

            // Assert: values round-trip correctly
            Assert.That(restored, Is.Not.Null);

            Assert.Multiple(() =>
            {
                // primitives
                Assert.That(restored.IntValue, Is.EqualTo(original.IntValue));
                Assert.That(restored.DoubleValue, Is.EqualTo(original.DoubleValue));
                Assert.That(restored.FloatValue, Is.EqualTo(original.FloatValue));
                Assert.That(restored.BoolValue, Is.EqualTo(original.BoolValue));
                Assert.That(restored.Text, Is.EqualTo(original.Text));

                // nullables: must stay null
                Assert.That(restored.OptionalInt.HasValue, Is.False);
                Assert.That(restored.OptionalFloat.HasValue, Is.False);
                Assert.That(restored.OptionalText, Is.Null);

                // lists
                Assert.That(
                    restored.IntList,
                    Is.EqualTo(original.IntList).AsCollection,
                    "IntList did not round-trip correctly");

                Assert.That(
                    restored.StringList,
                    Is.EqualTo(original.StringList).AsCollection,
                    "StringList did not round-trip correctly");

                // dictionary
                Assert.That(restored.NamedValues, Is.Not.Null);
                Assert.That(restored.NamedValues.Dictionary.Count,
                    Is.EqualTo(original.NamedValues.Dictionary.Count));
                Assert.That(restored.NamedValues.Dictionary["start"],
                    Is.EqualTo(original.NamedValues.Dictionary["start"]));
                Assert.That(restored.NamedValues.Dictionary["end"],
                    Is.EqualTo(original.NamedValues.Dictionary["end"]));

                // nested
                Assert.That(restored.Nested, Is.Not.Null);
                Assert.That(restored.Nested.Threshold,
                    Is.EqualTo(original.Nested.Threshold).Within(1e-6f));
                Assert.That(restored.Nested.Flag,
                    Is.EqualTo(original.Nested.Flag));
            });

            // Assert: TOML renders nulls as "Variable = null"
            Assert.Multiple(() =>
            {
                Assert.That(toml, Does.Contain("OptionalInt"));
                Assert.That(toml, Does.Contain("OptionalInt = null"));

                Assert.That(toml, Does.Contain("OptionalFloat"));
                Assert.That(toml, Does.Contain("OptionalFloat = null"));

                Assert.That(toml, Does.Contain("OptionalText"));
                Assert.That(toml, Does.Contain("OptionalText = null"));
            });

            // Optional (if you later add "# type" comments via VariableDescriptions):
            // Assert.That(toml, Does.Contain("# nullable int").IgnoreCase);
            // Assert.That(toml, Does.Contain("# nullable float").IgnoreCase);
            // Assert.That(toml, Does.Contain("# nullable string").IgnoreCase);
        }
    }
}
