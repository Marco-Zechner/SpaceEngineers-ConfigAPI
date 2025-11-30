using mz.Config.Core;
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
            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg, Is.InstanceOf<TestConfig>());
            Assert.That(cfg.ConfigVersion, Is.EqualTo("0.1.0"));

            var currentFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("TestConfigDefault.toml"));
        }

        [Test]
        public void Save_CreatesFile_WithSerializedContent_AndTracksFileName()
        {
            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 42;

            var result = ConfigStorage.Save(ConfigLocationType.Local, "TestConfig", "myconfig.toml");

            Assert.That(result, Is.True);

            string content;
            var fileExists = FileSystem.TryReadFile(ConfigLocationType.Local, "myconfig.toml", out content);

            Assert.Multiple(() =>
            {
                Assert.That(fileExists, Is.True);
                Assert.That(content, Is.EqualTo(Serializer.LastSerializedContent));
            });

            var currentFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("myconfig.toml"));
        }

        [Test]
        public void Load_ReadsFile_UsesSerializer_AndReplacesInstance()
        {
            var loadedConfig = new TestConfig()
            {
                SomeValue = 99
            };
            Serializer.DeserializeResult = loadedConfig;

            FileSystem.WriteFile(ConfigLocationType.Local, "existing.toml", "dummy content");

            var result = ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "existing.toml");

            Assert.That(result, Is.True);

            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(cfg, Is.SameAs(loadedConfig));

            var currentFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.Multiple(() =>
            {
                Assert.That(currentFileName, Is.EqualTo("existing.toml"));

                Assert.That(Serializer.LastDeserializeDefinition.TypeName, Is.EqualTo("TestConfig"));
                Assert.That(Serializer.LastDeserializeContent, Is.EqualTo("dummy content"));
            });
        }

        [Test]
        public void GetConfigAsText_SerializesCurrentInstance()
        {
            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 7;

            var text = ConfigStorage.GetConfigAsText(ConfigLocationType.Local, "TestConfig");

            Assert.That(text, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(text, Is.EqualTo(Serializer.LastSerializedContent));
                Assert.That(Serializer.LastSerializedInstance, Is.SameAs(cfg));
            });
        }

        [Test]
        public void GetFileAsText_ReturnsFileContent_OrNullIfNotFound()
        {
            FileSystem.WriteFile(ConfigLocationType.Local, "fileA.toml", "content A");

            var found = ConfigStorage.GetFileAsText(ConfigLocationType.Local, "fileA.toml");
            var notFound = ConfigStorage.GetFileAsText(ConfigLocationType.Local, "missing.toml");

            Assert.Multiple(() =>
            {
                Assert.That(found, Is.EqualTo("content A"));
                Assert.That(notFound, Is.Null);
            });
        }

        [Test]
        public void Register_SameTypeSameLocationTwice_DoesNotThrow_AndDoesNotChangeCurrentFileName()
        {
            var originalFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");

            Assert.That(
                () => ConfigStorage.Register<TestConfig>(ConfigLocationType.Local),
                Throws.Nothing);

            var after = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(after, Is.EqualTo(originalFileName));
        }
    }
}
