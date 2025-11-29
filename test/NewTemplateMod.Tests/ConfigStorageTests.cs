using mz.Config.Core;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    [TestFixture]
    public class ConfigStorageTests
    {
        private FakeFileSystem _fileSystem;
        private FakeSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            _serializer = new FakeSerializer();

            ConfigStorage.Initialize(_fileSystem, _serializer);
            ConfigStorage.Register<TestConfig>(ConfigLocationType.Local);
        }

        #region Positive tests

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
            var fileExists = _fileSystem.TryReadFile(ConfigLocationType.Local, "myconfig.toml", out content);

            Assert.That(fileExists, Is.True);
            Assert.That(content, Is.EqualTo(_serializer.LastSerializedContent));

            var currentFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("myconfig.toml"));
        }

        [Test]
        public void Load_ReadsFile_UsesSerializer_AndReplacesInstance()
        {
            var loadedConfig = new TestConfig();
            loadedConfig.SomeValue = 99;
            _serializer.DeserializeResult = loadedConfig;

            _fileSystem.WriteFile(ConfigLocationType.Local, "existing.toml", "dummy content");

            var result = ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "existing.toml");

            Assert.That(result, Is.True);

            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(cfg, Is.SameAs(loadedConfig));

            var currentFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("existing.toml"));

            Assert.That(_serializer.LastDeserializeDefinition.TypeName, Is.EqualTo("TestConfig"));
            Assert.That(_serializer.LastDeserializeContent, Is.EqualTo("dummy content"));
        }

        [Test]
        public void GetConfigAsText_SerializesCurrentInstance()
        {
            var cfg = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 7;

            var text = ConfigStorage.GetConfigAsText(ConfigLocationType.Local, "TestConfig");

            Assert.That(text, Is.Not.Null);
            Assert.That(text, Is.EqualTo(_serializer.LastSerializedContent));
            Assert.That(_serializer.LastSerializedInstance, Is.SameAs(cfg));
        }

        [Test]
        public void GetFileAsText_ReturnsFileContent_OrNullIfNotFound()
        {
            _fileSystem.WriteFile(ConfigLocationType.Local, "fileA.toml", "content A");

            var found = ConfigStorage.GetFileAsText(ConfigLocationType.Local, "fileA.toml");
            var notFound = ConfigStorage.GetFileAsText(ConfigLocationType.Local, "missing.toml");

            Assert.That(found, Is.EqualTo("content A"));
            Assert.That(notFound, Is.Null);
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

        #endregion

        #region Negative tests

        [Test]
        public void Initialize_NullFileSystem_Throws()
        {
            Assert.That(
                () => ConfigStorage.Initialize(null, _serializer),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Initialize_NullSerializer_Throws()
        {
            Assert.That(
                () => ConfigStorage.Initialize(_fileSystem, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetOrCreate_NoDefinitionForType_ThrowsInvalidOperationException()
        {
            ConfigStorage.Initialize(_fileSystem, _serializer);
            // do NOT register OtherConfig

            Assert.That(
                () => ConfigStorage.GetOrCreate<OtherConfig>(ConfigLocationType.Local),
                Throws.TypeOf<InvalidOperationException>());
        }

        private class OtherConfig : ConfigBase
        {
            public override string ConfigVersion
            {
                get { return "1.0.0"; }
            }
        }

        [Test]
        public void GetCurrentFileName_NullTypeName_Throws()
        {
            Assert.That(
                () => ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void SetCurrentFileName_NullTypeName_Throws()
        {
            Assert.That(
                () => ConfigStorage.SetCurrentFileName(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.SetCurrentFileName(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void SetCurrentFileName_NullFileName_Throws()
        {
            Assert.That(
                () => ConfigStorage.SetCurrentFileName(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.SetCurrentFileName(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Load_NullArguments_Throw()
        {
            Assert.That(
                () => ConfigStorage.Load(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.Load(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Save_NullArguments_Throw()
        {
            Assert.That(
                () => ConfigStorage.Save(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.Save(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.Save(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.Save(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Load_UnknownTypeName_Throws()
        {
            Assert.That(
                () => ConfigStorage.Load(ConfigLocationType.Local, "UnknownType", "file.toml"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Save_UnknownTypeName_Throws()
        {
            Assert.That(
                () => ConfigStorage.Save(ConfigLocationType.Local, "UnknownType", "file.toml"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Load_FileMissing_ReturnsFalse_AndDoesNotChangeCurrentFileName()
        {
            var originalFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");

            var result = ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "does_not_exist.toml");

            Assert.That(result, Is.False);

            var fileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(fileName, Is.EqualTo(originalFileName));
        }

        [Test]
        public void Load_DeserializerReturnsNull_ReturnsFalse_AndDoesNotOverrideExistingInstance()
        {
            TestConfig original = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            original.SomeValue = 5;

            _serializer.DeserializeResult = null;
            _fileSystem.WriteFile(ConfigLocationType.Local, "bad.toml", "bad content");

            bool result = ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "bad.toml");

            Assert.That(result, Is.False);

            TestConfig after = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(after, Is.SameAs(original));
            Assert.That(after.SomeValue, Is.EqualTo(5));
        }

        [Test]
        public void GetConfigAsText_NullTypeName_Throws()
        {
            Assert.That(
                () => ConfigStorage.GetConfigAsText(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.GetConfigAsText(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileAsText_NullFileName_Throws()
        {
            Assert.That(
                () => ConfigStorage.GetFileAsText(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => ConfigStorage.GetFileAsText(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        // ----------------- fakes -----------------

        private class FakeFileSystem : IConfigFileSystem
        {
            private readonly Dictionary<string, string> _files = [];

            public bool TryReadFile(ConfigLocationType location, string fileName, out string content)
            {
                string key = MakeKey(location, fileName);
                if (_files.TryGetValue(key, out content))
                    return true;

                content = null;
                return false;
            }

            public void WriteFile(ConfigLocationType location, string fileName, string content)
            {
                string key = MakeKey(location, fileName);
                _files[key] = content;
            }

            public string GetDefaultFileName(IConfigDefinition definition)
            {
                return definition.TypeName + "Default.toml";
            }

            private static string MakeKey(ConfigLocationType location, string fileName)
            {
                return ((int)location).ToString() + "|" + fileName;
            }
        }

        private class FakeSerializer : IConfigSerializer
        {
            public string LastSerializedContent;
            public ConfigBase LastSerializedInstance;

            public IConfigDefinition LastDeserializeDefinition;
            public string LastDeserializeContent;
            public ConfigBase DeserializeResult;

            public string Serialize(ConfigBase config)
            {
                LastSerializedInstance = config;
                LastSerializedContent = "SERIALIZED:" + config.GetType().Name + ":" + DateTime.Now.Ticks;
                return LastSerializedContent;
            }

            public ConfigBase Deserialize(IConfigDefinition def, string content)
            {
                LastDeserializeDefinition = def;
                LastDeserializeContent = content;
                return DeserializeResult;
            }

            public ITomlModel ParseToModel(string tomlContent)
            {
                throw new NotImplementedException();
            }

            public string SerializeModel(ITomlModel model)
            {
                throw new NotImplementedException();
            }

            public ITomlModel BuildDefaultModel(IConfigDefinition definition)
            {
                throw new NotImplementedException();
            }
        }

        private class TestConfig : ConfigBase
        {
            public int SomeValue { get; set; }

            public override string ConfigVersion
            {
                get { return "0.1.0"; }
            }

            public override string ConfigNameOverride
            {
                get { return "TestConfig"; }
            }
        }
    }
}
