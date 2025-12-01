using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.TomlTests
{
    [TestFixture]
    public class TomlScalarValueTests
    {
        private TomlXmlConverter _converter;
        private IConfigDefinition _def;
        private IConfigXmlSerializer _xml;

        public class ScalarConfig : ConfigBase
        {
            public override string ConfigVersion => "1.0.0";
            public override string ConfigNameOverride => "ScalarConfig";

            public bool Flag { get; set; }
            public int Count { get; set; }
            public double Ratio { get; set; }
            public string Name { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            _xml = new TestXmlSerializer();
            _converter = new TomlXmlConverter(_xml);
            _def = new ConfigDefinition<ScalarConfig>();
        }

        [Test]
        public void ToExternal_EmitsReasonableTomlLiterals()
        {
            var cfg = new ScalarConfig
            {
                Flag = true,
                Count = 5,
                Ratio = 0.5,
                Name = "hello"
            };

            var xmlContent = _xml.SerializeToXml(cfg);
            var toml = _converter.ToExternal(_def, xmlContent);

            // You can relax these expectations depending on how fancy you want to be
            Assert.That(toml, Does.Contain("Flag"));
            Assert.That(toml, Does.Contain("true")); // bool literal
            Assert.That(toml, Does.Contain("Count = 5")); // int literal
            Assert.That(toml, Does.Contain("Ratio")); // maybe "0.5" literal
            Assert.That(toml, Does.Contain("Name = \"hello\"")); // string quoted
        }

        [Test]
        public void Roundtrip_ScalarValuesSurvive()
        {
            var original = new ScalarConfig
            {
                Flag = true,
                Count = 123,
                Ratio = 1.25,
                Name = "abc"
            };

            var xml1 = _xml.SerializeToXml(original);
            var toml = _converter.ToExternal(_def, xml1);
            var xml2 = _converter.ToInternal(_def, toml);
            var restored = (ScalarConfig)_def.DeserializeFromXml(_xml, xml2);

            Assert.Multiple(() =>
            {
                Assert.That(restored.Flag, Is.EqualTo(original.Flag));
                Assert.That(restored.Count, Is.EqualTo(original.Count));
            });
            Assert.Multiple(() =>
            {
                Assert.That(restored.Ratio, Is.EqualTo(original.Ratio).Within(1e-6));
                Assert.That(restored.Name, Is.EqualTo(original.Name));
            });
        }
    }
}