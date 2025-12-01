using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    [TestFixture]
    public class ConfigStorageXmlOnlyTests
    {
        private FakeFileSystem _fileSystem;
        private TestXmlSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            _serializer = new TestXmlSerializer();

            var layout = new mz.Config.Core.Layout.ConfigLayoutMigrator();
            var converter = new IdentityXmlConverter();

            ConfigStorage.Initialize(_fileSystem, _serializer, layout, converter);
            ConfigStorage.Register<TestConfig>(ConfigLocationType.Local);
        }

        [Test]
        public void DefaultFileName_UsesXmlExtension_WhenIdentityConverter()
        {
            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(cfg, Is.Not.Null);

            var currentFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("TestConfigDefault.xml"));
        }
    }
}