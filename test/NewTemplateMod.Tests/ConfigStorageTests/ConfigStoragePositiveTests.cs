using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    [TestFixture]
    public class ConfigStoragePositiveTests : ConfigStorageTestBase
    {
        [Test]
        public void GetOrCreate_CreatesDefaultInstance_AndSetsDefaultFileName()
        {
            var cfg = InternalConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg, Is.InstanceOf<TestConfig>());
            Assert.That(cfg.ConfigVersion, Is.EqualTo("0.1.0"));

            var currentFileName = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("TestConfigDefault.toml"));
        }

        [Test]
        public void Save_CreatesFile_WithSerializedContent_AndTracksFileName()
        {
            var cfg = InternalConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 42;

            var result = InternalConfigStorage.Save(ConfigLocationType.Local, "TestConfig", "myconfig.toml");

            Assert.That(result, Is.True);

            string content;
            var fileExists = FileSystem.TryReadFile(ConfigLocationType.Local, "myconfig.toml", out content);

            Assert.Multiple(() =>
            {
                Assert.That(fileExists, Is.True);
                Assert.That(content, Is.Not.Null);
                Assert.That(content, Does.Contain("[TestConfig]"));
                Assert.That(content, Does.Contain("SomeValue"));
                Assert.That(content, Does.Contain("42"));
            });

            var currentFileName = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("myconfig.toml"));
        }

        [Test]
        public void Load_ReadsFile_AndReplacesInstance()
        {
            // Prepare a TOML file for TestConfig with SomeValue = 99
            var toml =
                "[TestConfig]\n" +
                "ConfigVersion = \"0.1.0\"\n" +
                "SomeValue = 99\n";

            FileSystem.WriteFile(ConfigLocationType.Local, "existing.toml", toml);

            var result = InternalConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "existing.toml");

            Assert.That(result, Is.True);

            var cfg = InternalConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.Multiple(() =>
            {
                Assert.That(cfg, Is.Not.Null);
                Assert.That(cfg.SomeValue, Is.EqualTo(99));
            });

            var currentFileName = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("existing.toml"));
        }

        [Test]
        public void GetConfigAsText_SerializesCurrentInstance()
        {
            var cfg = InternalConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 7;

            var text = InternalConfigStorage.GetConfigAsText(ConfigLocationType.Local, "TestConfig");

            Assert.That(text, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("[TestConfig]"));
                Assert.That(text, Does.Contain("SomeValue"));
                Assert.That(text, Does.Contain("7"));
            });
        }

        [Test]
        public void GetFileAsText_ReturnsFileContent_OrNullIfNotFound()
        {
            FileSystem.WriteFile(ConfigLocationType.Local, "fileA.toml", "content A");

            var found = InternalConfigStorage.GetFileAsText(ConfigLocationType.Local, "fileA.toml");
            var notFound = InternalConfigStorage.GetFileAsText(ConfigLocationType.Local, "missing.toml");

            Assert.Multiple(() =>
            {
                Assert.That(found, Is.EqualTo("content A"));
                Assert.That(notFound, Is.Null);
            });
        }

        [Test]
        public void Register_SameTypeSameLocationTwice_DoesNotThrow_AndDoesNotChangeCurrentFileName()
        {
            var originalFileName = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");

            Assert.That(
                () => InternalConfigStorage.Register<TestConfig>(ConfigLocationType.Local),
                Throws.Nothing);

            var after = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(after, Is.EqualTo(originalFileName));
        }
    }
}
