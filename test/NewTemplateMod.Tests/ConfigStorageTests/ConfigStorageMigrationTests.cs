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
            var converter = new TomlXmlConverter();

            InternalConfigStorage.Initialize(_fileSystem, _xmlSerializer, layout, converter, null);
            InternalConfigStorage.Register<ExampleConfig>(ConfigLocationType.Local);
        }

        [Test]
        public void Load_WithExtraKey_BackupsOriginal_AndRemovesUnknownKeys()
        {
            var oldToml =
                "[ExampleConfig]\n" +
                "ConfigVersion = \"0.1.0\"\n" +
                "RespondToHello = false\n" +
                "GreetingMessage = \"hello\"\n" +
                "ExtraSetting = 5\n";

            _fileSystem.WriteFile(ConfigLocationType.Local, "cfg.toml", oldToml);

            InternalConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "cfg.toml");

            // Backup must exist and contain original content
            string backupContent;
            var backupExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "cfg.bak.toml", out backupContent);

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

            var cfg = InternalConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
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
                "ConfigVersion = \"0.1.0\"\n" +
                "RespondToHello = true\n";

            _fileSystem.WriteFile(ConfigLocationType.Local, "missing.toml", oldToml);

            InternalConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "missing.toml");

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

            var cfg = InternalConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.True);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hello"));
            });
        }
        
        [Test]
        public void Load_WhenDefaultsFileMissing_CreatesItAndLoadsConfig()
        {
            // Arrange: write only main config file, no .defaults file
            var toml =
                "[ExampleConfig]\n" +
                "ConfigVersion = \"0.1.0\"\n" +
                "RespondToHello = true\n" +
                "GreetingMessage = \"hi\"\n";

            _fileSystem.WriteFile(ConfigLocationType.Local, "example.toml", toml);

            // Act
            InternalConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "example.toml");


            // Defaults file got created
            string defaultsContent;
            var defaultsExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "ExampleConfig.defaults.toml", out defaultsContent);

            Assert.Multiple(() =>
            {
                Assert.That(defaultsExists, Is.True);
                Assert.That(defaultsContent, Does.Contain("[ExampleConfig]"));
            });
            Assert.That(defaultsContent, Does.Contain("ConfigVersion"));

            // In-memory config matches user values
            var cfg = InternalConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.True);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hi"));
            });
        }
        
        [Test]
        public void Load_WithEmptyFile_UsesCurrentDefaults_NoBackup()
        {
            _fileSystem.WriteFile(ConfigLocationType.Local, "empty.toml", string.Empty);

            InternalConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "empty.toml");

            // No backup, nothing destructive happened
            string backup;
            var hasBackup = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "empty.bak.toml", out backup);
            Assert.That(hasBackup, Is.False);

            var cfg = InternalConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.False);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hello"));
            });
        }
    }
}
