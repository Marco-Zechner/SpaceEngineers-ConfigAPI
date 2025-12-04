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
                Logger.Log("TOML mismatch in " + testName + "\nEXPECTED:\n" + ne + "\n\nACTUAL:\n" + na,
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
        // CONFIG 1: Scalars + nullables
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
@"[TomlConfig1_Scalars]
ConfigVersion = ""1.0.0""
IntValue = 42
DoubleValue = 12.5
FloatValue = 0.33
BoolValue = false
Text = ""Custom text""
OptionalInt = null
OptionalFloat = null
OptionalText = null";

            AssertTomlEqual(toml, expected, "Config1_Scalars");
        }

        // --------------------------------------------------------------------
        // CONFIG 2: + primitive lists
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
@"[TomlConfig2_Lists]
ConfigVersion = ""1.0.0""
IntValue = 7
Text = ""List test""

IntList.int = [1, 2, 3]
StringList.string = [""alpha"", ""beta""]";

            AssertTomlEqual(toml, expected, "Config2_Lists");
        }

        // --------------------------------------------------------------------
        // CONFIG 3: + dictionary block
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

            // We expect:
            // - root scalars
            // - IntList.int / StringList.string
            // - [TomlConfig3_Dictionary.NamedValues-dictionary] with "start"/"end"
            var expected =
@"[TomlConfig3_Dictionary]
ConfigVersion = ""1.0.0""
IntValue = 10
Text = ""Dict test""

IntList.int = [1, 2, 3]
StringList.string = [""alpha"", ""beta""]

[TomlConfig3_Dictionary.NamedValues-dictionary]
""start"" = 5
""end"" = 42";

            AssertTomlEqual(toml, expected, "Config3_Dictionary");
        }

        // --------------------------------------------------------------------
        // CONFIG 4: + nested object + trailing scalar with Nested-end marker
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
@"[TomlConfig4_Nested]
ConfigVersion = ""1.0.0""
IntValue = 99
Text = ""Nested test""

IntList.int = [1, 2, 3]
StringList.string = [""alpha"", ""beta""]

[TomlConfig4_Nested.NamedValues-dictionary]
""start"" = 1
""end"" = 99

[TomlConfig4_Nested.Nested]
Threshold = 0.9
Flag = true
[TomlConfig4_Nested.Nested-end]
FloatValue = 0.5";

            AssertTomlEqual(toml, expected, "Config4_Nested");
        }
    }
}
