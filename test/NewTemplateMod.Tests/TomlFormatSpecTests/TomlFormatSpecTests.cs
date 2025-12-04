using System;
using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using NUnit.Framework;

namespace NewTemplateMod.Tests.TomlFormatSpecTests
{
    [TestFixture]
    public class TomlFormatSpecTests
    {
        private TestXmlSerializer _xml;
        private TomlXmlConverter _converter;

        private static string NormalizeNewlines(string s)
        {
            if (s == null)
                return string.Empty;

            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void AssertTomlEqual(string actual, string expected, string testName)
        {
            var na = NormalizeNewlines(actual).TrimEnd();
            var ne = NormalizeNewlines(expected).TrimEnd();

            if (!string.Equals(na, ne, StringComparison.Ordinal))
            {
                Logger.Log(
                    "TOML mismatch in " + testName + "\nEXPECTED:\n" + ne + "\n\nACTUAL:\n" + na,
                    "TomlFormatSpecTests");
            }

            Assert.That(na, Is.EqualTo(ne));
        }

        [SetUp]
        public void SetUp()
        {
            _xml = new TestXmlSerializer();
            _converter = new TomlXmlConverter(_xml);

            // Ensure InternalConfigStorage has a serializer so the converter can
            // query it if needed (some code paths use it).
            var fs = new FakeFileSystem();
            var migrator = new ConfigLayoutMigrator();
            var identity = new IdentityXmlConverter();
            InternalConfigStorage.Initialize(fs, _xml, migrator, identity);
        }

        // --------------------------------------------------------------------
        // CONFIG 1: Scalars + nullables  (TOML)
        // --------------------------------------------------------------------

        [Test]
        public void Config1_Scalars_Toml_Format_Is_As_Expected()
        {
            var def = new ConfigDefinition<TomlConfig1Scalars>();
            var cfg = new TomlConfig1Scalars();

            // tweak some values so we see non-default literals clearly
            cfg.IntValue = 42;
            cfg.DoubleValue = 12.5;
            cfg.FloatValue = 0.33f;
            cfg.BoolValue = false;
            cfg.Text = "Custom text";

            cfg.OptionalInt = null;
            cfg.OptionalFloat = null;
            cfg.OptionalText = null;

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config1 XML:\n" + xml, "TomlFormatSpecTests");

            var toml = _converter.ToExternal(def, xml);
            Logger.Log("Config1 TOML:\n" + toml, "TomlFormatSpecTests");

            var expected =
@"[TomlConfig1Scalars]
ConfigVersion = ""1.0.0""
IntValue = 42
DoubleValue = 12.5
FloatValue = 0.33
BoolValue = false
Text = ""Custom text""
OptionalInt = null
OptionalFloat = null
OptionalText = null";

            AssertTomlEqual(toml, expected, "Config1Scalars");
        }

        // --------------------------------------------------------------------
        // CONFIG 1: Scalars + nullables  (XML)
        // --------------------------------------------------------------------

        [Test]
        public void Config1_Scalars_Xml_Contains_All_Scalars_And_Nullables()
        {
            var cfg = new TomlConfig1Scalars();

            cfg.IntValue = 42;
            cfg.DoubleValue = 12.5;
            cfg.FloatValue = 0.33f;
            cfg.BoolValue = false;
            cfg.Text = "Custom text";

            cfg.OptionalInt = null;
            cfg.OptionalFloat = null;
            cfg.OptionalText = null;

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config1 XML (shape check):\n" + xml, "TomlFormatSpecTests");

            var norm = NormalizeNewlines(xml);

            Assert.Multiple(() =>
            {
                Assert.That(norm, Does.Contain("<?xml version=\"1.0\" encoding=\"utf-16\"?>"));
                Assert.That(norm, Does.Contain("<TomlConfig1Scalars "));
                Assert.That(norm, Does.Contain("<ConfigVersion>1.0.0</ConfigVersion>"));
                Assert.That(norm, Does.Contain("<IntValue>42</IntValue>"));
                Assert.That(norm, Does.Contain("<DoubleValue>12.5</DoubleValue>"));
                Assert.That(norm, Does.Contain("<FloatValue>0.33</FloatValue>").Or.Contain("<FloatValue>0.330"));
                Assert.That(norm, Does.Contain("<BoolValue>false</BoolValue>"));
                Assert.That(norm, Does.Contain("<Text>Custom text</Text>"));

                // For Nullable<T>, XmlSerializer normally emits xsi:nil="true"
                Assert.That(norm, Does.Contain("<OptionalInt xsi:nil=\"true\""));
                Assert.That(norm, Does.Contain("<OptionalFloat xsi:nil=\"true\""));

                // OptionalText behavior depends on IsNullable on the string property.
                // We just assert that it does *not* get some random numeric value.
                Assert.That(norm, Does.Not.Contain("<OptionalText>0</OptionalText>"));
            });
        }

        // --------------------------------------------------------------------
        // CONFIG 2: + primitive lists  (TOML)
        // --------------------------------------------------------------------

        [Test]
        public void Config2_Lists_Toml_Format_Includes_Primitive_Lists()
        {
            var def = new ConfigDefinition<TomlConfig2Lists>();
            var cfg = new TomlConfig2Lists();
            cfg.ApplyDefaults();

            cfg.IntValue = 7;
            cfg.Text = "List test";

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config2 XML:\n" + xml, "TomlFormatSpecTests");

            var toml = _converter.ToExternal(def, xml);
            Logger.Log("Config2 TOML:\n" + toml, "TomlFormatSpecTests");

            var expected =
@"[TomlConfig2Lists]
ConfigVersion = ""1.0.0""
IntValue = 7
Text = ""List test""
IntList.int = [1, 2, 3]
StringList.string = [""alpha"", ""beta""]";

            AssertTomlEqual(toml, expected, "Config2Lists");
        }

        // --------------------------------------------------------------------
        // CONFIG 2: + primitive lists  (XML)
        // --------------------------------------------------------------------

        [Test]
        public void Config2_Lists_Xml_Has_IntList_And_StringList()
        {
            var cfg = new TomlConfig2Lists();
            cfg.ApplyDefaults();

            cfg.IntValue = 7;
            cfg.Text = "List test";

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config2 XML (shape check):\n" + xml, "TomlFormatSpecTests");

            var norm = NormalizeNewlines(xml);

            Assert.Multiple(() =>
            {
                Assert.That(norm, Does.Contain("<TomlConfig2Lists "));
                Assert.That(norm, Does.Contain("<ConfigVersion>1.0.0</ConfigVersion>"));
                Assert.That(norm, Does.Contain("<IntValue>7</IntValue>"));
                Assert.That(norm, Does.Contain("<Text>List test</Text>"));

                // IntList
                Assert.That(norm, Does.Contain("<IntList>"));
                Assert.That(norm, Does.Contain("<int>1</int>"));
                Assert.That(norm, Does.Contain("<int>2</int>"));
                Assert.That(norm, Does.Contain("<int>3</int>"));

                // StringList
                Assert.That(norm, Does.Contain("<StringList>"));
                Assert.That(norm, Does.Contain("<string>alpha</string>"));
                Assert.That(norm, Does.Contain("<string>beta</string>"));
            });
        }

        // --------------------------------------------------------------------
        // CONFIG 3: + dictionary block  (TOML)
        // --------------------------------------------------------------------

        [Test]
        public void Config3_Dictionary_Toml_Uses_Dictionary_Block()
        {
            var def = new ConfigDefinition<TomlConfig3Dictionary>();
            var cfg = new TomlConfig3Dictionary();
            cfg.ApplyDefaults();

            cfg.IntValue = 10;
            cfg.Text = "Dict test";

            // adjust dictionary so we can verify numeric values
            cfg.NamedValues.Dictionary["start"] = 5;
            cfg.NamedValues.Dictionary["end"] = 42;

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config3 XML:\n" + xml, "TomlFormatSpecTests");

            var toml = _converter.ToExternal(def, xml);
            Logger.Log("Config3 TOML:\n" + toml, "TomlFormatSpecTests");

            var expected =
@"[TomlConfig3Dictionary]
ConfigVersion = ""1.0.0""
IntValue = 10
Text = ""Dict test""
IntList.int = [1, 2, 3]
StringList.string = [""alpha"", ""beta""]
[TomlConfig3Dictionary.NamedValues-dictionary]
""start"" = 5
""end"" = 42";

            AssertTomlEqual(toml, expected, "Config3Dictionary");
        }

        // --------------------------------------------------------------------
        // CONFIG 3: + dictionary block  (XML)
        // --------------------------------------------------------------------

        [Test]
        public void Config3_Dictionary_Xml_Has_Dictionary_Shape()
        {
            var cfg = new TomlConfig3Dictionary();
            cfg.ApplyDefaults();

            cfg.IntValue = 10;
            cfg.Text = "Dict test";
            cfg.NamedValues.Dictionary["start"] = 5;
            cfg.NamedValues.Dictionary["end"] = 42;

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config3 XML (shape check):\n" + xml, "TomlFormatSpecTests");

            var norm = NormalizeNewlines(xml);

            Assert.Multiple(() =>
            {
                Assert.That(norm, Does.Contain("<TomlConfig3Dictionary "));
                Assert.That(norm, Does.Contain("<ConfigVersion>1.0.0</ConfigVersion>"));
                Assert.That(norm, Does.Contain("<IntValue>10</IntValue>"));
                Assert.That(norm, Does.Contain("<Text>Dict test</Text>"));

                // Lists
                Assert.That(norm, Does.Contain("<IntList>"));
                Assert.That(norm, Does.Contain("<int>1</int>"));
                Assert.That(norm, Does.Contain("<int>2</int>"));
                Assert.That(norm, Does.Contain("<int>3</int>"));

                Assert.That(norm, Does.Contain("<StringList>"));
                Assert.That(norm, Does.Contain("<string>alpha</string>"));
                Assert.That(norm, Does.Contain("<string>beta</string>"));

                // Dictionary shape
                Assert.That(norm, Does.Contain("<NamedValues>"));
                Assert.That(norm, Does.Contain("<dictionary>"));
                Assert.That(norm, Does.Contain("<item>"));
                Assert.That(norm, Does.Contain("<Key>start</Key>"));
                Assert.That(norm, Does.Contain("<Value>5</Value>"));
                Assert.That(norm, Does.Contain("<Key>end</Key>"));
                Assert.That(norm, Does.Contain("<Value>42</Value>"));
            });
        }

        // --------------------------------------------------------------------
        // CONFIG 4: + nested + trailing scalar (TOML)
        // --------------------------------------------------------------------

        [Test]
        public void Config4_Nested_Toml_Uses_Nested_Block_And_End_Marker_With_Trailing_Scalar()
        {
            var def = new ConfigDefinition<TomlConfig4Nested>();
            var cfg = new TomlConfig4Nested();
            cfg.ApplyDefaults();

            cfg.IntValue = 99;
            cfg.Text = "Nested test";
            cfg.FloatValue = 0.5f;

            cfg.Nested.Threshold = 0.9f;
            cfg.Nested.Flag = true;

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config4 XML:\n" + xml, "TomlFormatSpecTests");

            var toml = _converter.ToExternal(def, xml);
            Logger.Log("Config4 TOML:\n" + toml, "TomlFormatSpecTests");

            var expected =
@"[TomlConfig4Nested]
ConfigVersion = ""1.0.0""
IntValue = 99
Text = ""Nested test""
IntList.int = [1, 2, 3]
StringList.string = [""alpha"", ""beta""]
[TomlConfig4Nested.NamedValues-dictionary]
""start"" = 1
""end"" = 99
[TomlConfig4Nested.Nested]
Threshold = 0.9
Flag = true
[TomlConfig4Nested]
FloatValue = 0.5";

            AssertTomlEqual(toml, expected, "Config4Nested");
        }

        // --------------------------------------------------------------------
        // CONFIG 4: + nested + trailing scalar (XML)
        // --------------------------------------------------------------------

        [Test]
        public void Config4_Nested_Xml_Has_Nested_Block_And_Trailing_FloatValue()
        {
            var cfg = new TomlConfig4Nested();
            cfg.ApplyDefaults();

            cfg.IntValue = 99;
            cfg.Text = "Nested test";
            cfg.FloatValue = 0.5f;

            cfg.Nested.Threshold = 0.9f;
            cfg.Nested.Flag = true;

            var xml = _xml.SerializeToXml(cfg);
            Logger.Log("Config4 XML (shape check):\n" + xml, "TomlFormatSpecTests");

            var norm = NormalizeNewlines(xml);

            var nestedIndex = norm.IndexOf("<Nested>", StringComparison.Ordinal);
            var endNestedIndex = norm.IndexOf("</Nested>", StringComparison.Ordinal);
            var floatIndex = norm.IndexOf("<FloatValue>", StringComparison.Ordinal);

            Assert.Multiple(() =>
            {
                Assert.That(norm, Does.Contain("<TomlConfig4Nested "));
                Assert.That(norm, Does.Contain("<ConfigVersion>1.0.0</ConfigVersion>"));
                Assert.That(norm, Does.Contain("<IntValue>99</IntValue>"));
                Assert.That(norm, Does.Contain("<Text>Nested test</Text>"));

                // Lists
                Assert.That(norm, Does.Contain("<IntList>"));
                Assert.That(norm, Does.Contain("<StringList>"));

                // Dictionary
                Assert.That(norm, Does.Contain("<NamedValues>"));
                Assert.That(norm, Does.Contain("<Key>start</Key>"));
                Assert.That(norm, Does.Contain("<Key>end</Key>"));

                // Nested block
                Assert.That(nestedIndex, Is.GreaterThan(0), "Nested block not found");
                Assert.That(endNestedIndex, Is.GreaterThan(nestedIndex), "Closing Nested not after opening");
                Assert.That(norm, Does.Contain("<Threshold>0.9</Threshold>").Or.Contain("<Threshold>0.9"));
                Assert.That(norm, Does.Contain("<Flag>true</Flag>"));

                // FloatValue must come after </Nested>
                Assert.That(floatIndex, Is.GreaterThan(endNestedIndex),
                    "FloatValue element should be after Nested block");
            });
        }
    }
}
