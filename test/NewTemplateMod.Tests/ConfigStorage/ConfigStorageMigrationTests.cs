using mz.Config.Core;
using mz.Config.Core.Storage;
using mz.Config.Core.Toml;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    [TestFixture]
    public class ConfigStorageMigrationTests
    {
        private FakeFileSystem _fileSystem;
        private TomlConfigSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            var xml = new TestXmlSerializer();
            _serializer = new TomlConfigSerializer(xml);

            ConfigStorage.Initialize(_fileSystem, _serializer);
            ConfigStorage.Register<ExampleConfig>(ConfigLocationType.Local);
        }

        [Test]
        public void Load_WithExtraKey_BackupsOriginal_AndRemovesUnknownKeys()
        {
            // File has an extra setting that does not exist in ExampleConfig.
            var oldToml =
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.1.0\"\n" +
                "RespondToHello = false # false\n" +
                "GreetingMessage = \"hello\" # \"hello\"\n" +
                "ExtraSetting = 5 # 5\n";

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

            // Loaded config should still have correct values
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
            // File is missing GreetingMessage entirely.
            var oldToml =
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.1.0\"\n" +
                "RespondToHello = true # false\n";

            _fileSystem.WriteFile(ConfigLocationType.Local, "missing.toml", oldToml);

            var result = ConfigStorage.Load(ConfigLocationType.Local, "ExampleConfig", "missing.toml");

            Assert.That(result, Is.True);

            // No backup, because there was no extra key, only a missing one.
            string backupContent;
            var backupExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "missing.toml.bak", out backupContent);

            Assert.That(backupExists, Is.False);

            // Normalized file must contain GreetingMessage with default "hello".
            string normalizedContent;
            var normalizedExists = _fileSystem.TryReadFile(
                ConfigLocationType.Local, "missing.toml", out normalizedContent);

            Assert.Multiple(() =>
            {
                Assert.That(normalizedExists, Is.True);
                Assert.That(normalizedContent, Does.Contain("GreetingMessage"));
                Assert.That(normalizedContent, Does.Contain("\"hello\""));
            });

            // Loaded config should use user value for RespondToHello and default for GreetingMessage
            var cfg = ConfigStorage.GetOrCreate<ExampleConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg.RespondToHello, Is.True);
                Assert.That(cfg.GreetingMessage, Is.EqualTo("hello"));
            });
        }
    }
}
