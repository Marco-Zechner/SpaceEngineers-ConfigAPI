using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using mz.SemanticVersioning;
using NUnit.Framework;

namespace NewTemplateMod.Tests.VersionTests
{
    public class ExampleVersionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.2.3";

        public string Name { get; set; }

        public override string ConfigNameOverride => "ExampleVersionConfig";
    }
    
    [TestFixture]
    public class ConfigVersionTests
    {
        [Test]
        public void ConfigVersion_IsAlwaysTakenFromCode_NotFromFile()
        {
            // Arrange
            IConfigXmlSerializer xml = new TestXmlSerializer();
            var converter = new TomlXmlConverter(xml);
            IConfigDefinition def = new ConfigDefinition<ExampleVersionConfig>();

            var cfg = new ExampleVersionConfig
            {
                Name = "Test"
            };

            // First save: create a TOML string
            var xmlContent = xml.SerializeToXml(cfg);
            var toml = converter.ToExternal(def, xmlContent);

            // User "maliciously" edits the version:
            var hackedToml = toml.Replace("1.2.3", "999.999.999");

            // Load again
            var xmlFromHacked = converter.ToInternal(def, hackedToml);
            var reloaded = (ExampleVersionConfig)def.DeserializeFromXml(xml, xmlFromHacked);

            // Save again
            var xmlReserialized = xml.SerializeToXml(reloaded);
            var tomlResaved = converter.ToExternal(def, xmlReserialized);

            // Assert: ConfigVersion in final TOML must be 1.2.3, not 999.999.999
            Assert.That(tomlResaved, Does.Contain("ConfigVersion = \"1.2.3\""));
            Assert.That(tomlResaved, Does.Not.Contain("999.999.999"));
        }
    }
}