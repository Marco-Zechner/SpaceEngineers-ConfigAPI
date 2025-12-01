using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Core.Converter;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    [TestFixture]
    public class ConfigStorageMigrationTests
    {
        private FakeFileSystem _fileSystem;
        private TestXmlSerializer _xmlSerializer;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            _xmlSerializer = new TestXmlSerializer();
            var layout = new ConfigLayoutMigrator();
            var converter = new TomlXmlConverter(_xmlSerializer);

            ConfigStorage.Initialize(_fileSystem, _xmlSerializer, layout, converter);
            ConfigStorage.Register<ExampleConfig>(ConfigLocationType.Local);
        }

        [Test]
        public void Load_WithExtraKey_BackupsOriginal_AndRemovesUnknownKeys()
        {
            var oldToml =
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.1.0\"\n" +
                "RespondToHello = false\n" +
                "GreetingMessage = \"hello\"\n" +
                "ExtraSetting = 5\n";

            _fileSystem.WriteFile(ConfigLocationType.Local, "cfg.toml", oldToml);

            var result = ConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "cfg.toml");

            Assert.That(result, Is.True);

            // Backup must exist and contain original content
            string backupContent;
            var backupExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "cfg.toml.bak", out backupContent);

            Assert.Multiple(() =>
            {
                Assert.That(backupExists, Is.True);
                Assert.That(backupContent, Is.EqualTo(oldToml));
            });

            // Normalized file must not contain ExtraSetting
            string normalizedContent;
            var normalizedExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "cfg.toml", out normalizedContent);

            Assert.Multiple(() =>
            {
                Assert.That(normalizedExists, Is.True);
                Assert.That(normalizedContent, Does.Not.Contain("ExtraSetting"));
                Assert.That(normalizedContent, Does.Contain("RespondToHello"));
                Assert.That(normalizedContent, Does.Contain("GreetingMessage"));
            });

            var cfg = ConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.False);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hello"));
            });
        }

        [Test]
        public void Load_WithMissingKey_AddsItWithoutBackup()
        {
            var oldToml =
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.1.0\"\n" +
                "RespondToHello = true\n";

            _fileSystem.WriteFile(ConfigLocationType.Local, "missing.toml", oldToml);

            var result = ConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "missing.toml");

            Assert.That(result, Is.True);

            // No backup, because there was no extra key, only a missing one.
            string backupContent;
            var backupExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "missing.toml.bak", out backupContent);

            Assert.That(backupExists, Is.False);

            string normalizedContent;
            var normalizedExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "missing.toml", out normalizedContent);

            Assert.Multiple(() =>
            {
                Assert.That(normalizedExists, Is.True);
                Assert.That(normalizedContent, Does.Contain("GreetingMessage"));
                Assert.That(normalizedContent, Does.Contain("\"hello\""));
            });

            var cfg = ConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.True);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hello"));
            });
        }
    }
}
