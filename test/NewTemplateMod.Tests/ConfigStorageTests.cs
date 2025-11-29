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
        private ConfigStorage _storage;
        private FakeDefinition _testConfigDef;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            _serializer = new FakeSerializer();
            _storage = new ConfigStorage(_fileSystem, _serializer);

            _testConfigDef = new FakeDefinition(
                "TestConfig",
                typeof(TestConfig),
                new TestConfig() // default instance
            );

            _storage.RegisterConfig(_testConfigDef);
        }

        #region Positive tests

        [Test]
        public void GetOrCreate_CreatesDefaultInstance_AndSetsDefaultFileName()
        {
            TestConfig cfg = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg, Is.InstanceOf<TestConfig>());
            Assert.That(cfg.ConfigVersion, Is.EqualTo("0.1.0"));

            string currentFileName = _storage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("TestConfigDefault.toml"));
        }

        [Test]
        public void Save_CreatesFile_WithSerializedContent_AndTracksFileName()
        {
            TestConfig cfg = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 42;

            bool result = _storage.Save(ConfigLocationType.Local, "TestConfig", "myconfig.toml");

            Assert.That(result, Is.True);

            string content;
            bool fileExists = _fileSystem.TryReadFile(ConfigLocationType.Local, "myconfig.toml", out content);

            Assert.That(fileExists, Is.True);
            Assert.That(content, Is.EqualTo(_serializer.LastSerializedContent));

            string currentFileName = _storage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("myconfig.toml"));
        }

        [Test]
        public void Load_ReadsFile_UsesSerializer_AndReplacesInstance()
        {
            // Serializer returns this instance
            var loadedConfig = new TestConfig();
            loadedConfig.SomeValue = 99;
            _serializer.DeserializeResult = loadedConfig;

            // Insert a file
            _fileSystem.WriteFile(ConfigLocationType.Local, "existing.toml", "dummy content");

            bool result = _storage.Load(ConfigLocationType.Local, "TestConfig", "existing.toml");

            Assert.That(result, Is.True);

            TestConfig cfg = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(cfg, Is.SameAs(loadedConfig));

            string currentFileName = _storage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(currentFileName, Is.EqualTo("existing.toml"));

            Assert.That(_serializer.LastDeserializeDefinition.TypeName, Is.EqualTo("TestConfig"));
            Assert.That(_serializer.LastDeserializeContent, Is.EqualTo("dummy content"));
        }

        [Test]
        public void GetConfigAsText_SerializesCurrentInstance()
        {
            TestConfig cfg = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            cfg.SomeValue = 7;

            string text = _storage.GetConfigAsText(ConfigLocationType.Local, "TestConfig");

            Assert.That(text, Is.Not.Null);
            Assert.That(text, Is.EqualTo(_serializer.LastSerializedContent));
            Assert.That(_serializer.LastSerializedInstance, Is.SameAs(cfg));
        }

        [Test]
        public void GetFileAsText_ReturnsFileContent_OrNullIfNotFound()
        {
            _fileSystem.WriteFile(ConfigLocationType.Local, "fileA.toml", "content A");

            string found = _storage.GetFileAsText(ConfigLocationType.Local, "fileA.toml");
            string notFound = _storage.GetFileAsText(ConfigLocationType.Local, "missing.toml");

            Assert.That(found, Is.EqualTo("content A"));
            Assert.That(notFound, Is.Null);
        }

        [Test]
        public void RegisterConfig_OverridesSameTypeName()
        {
            var secondDef = new FakeDefinition("TestConfig", typeof(TestConfig), new TestConfig());
            _storage.RegisterConfig(secondDef);

            // No exception expected; overwrite is allowed.

            // Re-init the storage + register only secondDef, to check creation logic
            _storage = new ConfigStorage(_fileSystem, _serializer);
            _storage.RegisterConfig(secondDef);

            TestConfig cfg = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg, Is.InstanceOf<TestConfig>());
        }

        #endregion

        #region Negative tests

       [Test]
        public void RegisterConfig_NullDefinition_Throws()
        {
            Assert.That(
                () => _storage.RegisterConfig(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void RegisterConfig_EmptyTypeName_Throws()
        {
            var badDef = new FakeDefinition(string.Empty, typeof(TestConfig), new TestConfig());

            Assert.That(
                () => _storage.RegisterConfig(badDef),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void RegisterConfig_NullTypeName_Throws()
        {
            var badDef = new FakeDefinition(null, typeof(TestConfig), new TestConfig());

            Assert.That(
                () => _storage.RegisterConfig(badDef),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void GetOrCreate_NoDefinitionForType_ThrowsInvalidOperationException()
        {

            // New storage with no registrations
            var storage = new ConfigStorage(_fileSystem, _serializer);

            Assert.That(
                () => storage.GetOrCreate<OtherConfig>(ConfigLocationType.Local),
                Throws.TypeOf<InvalidOperationException>());
        }

        // Helper config type used only for the "no definition registered" test.
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
                () => _storage.GetCurrentFileName(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.GetCurrentFileName(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void SetCurrentFileName_NullTypeName_Throws()
        {
            Assert.That(
                () => _storage.SetCurrentFileName(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.SetCurrentFileName(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void SetCurrentFileName_NullFileName_Throws()
        {
            Assert.That(
                () => _storage.SetCurrentFileName(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.SetCurrentFileName(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Load_NullArguments_Throw()
        {
            Assert.That(
                () => _storage.Load(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.Load(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.Load(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.Load(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Save_NullArguments_Throw()
        {
            Assert.That(
                () => _storage.Save(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.Save(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.Save(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.Save(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Load_UnknownTypeName_Throws()
        {
            Assert.That(
                () => _storage.Load(ConfigLocationType.Local, "UnknownType", "file.toml"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Save_UnknownTypeName_Throws()
        {
            Assert.That(
                () => _storage.Save(ConfigLocationType.Local, "UnknownType", "file.toml"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Load_FileMissing_ReturnsFalse_AndDoesNotCreateInstance()
        {
            var result = _storage.Load(ConfigLocationType.Local, "TestConfig", "does_not_exist.toml");

            Assert.That(result, Is.False);

            // No instance should exist yet until GetOrCreate is called
            // We cannot access internals, but we can check that a later GetOrCreate
            // still gives us a default instance (and that nothing crashed).
            var cfg = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(cfg, Is.Not.Null);

            // Current file name should be default, not "does_not_exist.toml"
            var fileName = _storage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(fileName, Is.EqualTo("TestConfigDefault.toml"));
        }

        [Test]
        public void Load_DeserializerReturnsNull_ReturnsFalse_AndDoesNotOverrideExistingInstance()
        {
            // Arrange existing instance
            var original = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            original.SomeValue = 5;

            _serializer.DeserializeResult = null; // simulate deserialize failure
            _fileSystem.WriteFile(ConfigLocationType.Local, "bad.toml", "bad content");

            // Act
            var result = _storage.Load(ConfigLocationType.Local, "TestConfig", "bad.toml");

            // Assert
            Assert.That(result, Is.False);

            var after = _storage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            Assert.That(after, Is.SameAs(original));
            Assert.That(after.SomeValue, Is.EqualTo(5));

            var currentFile = _storage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            // Load failed, so it should not switch to "bad.toml"
            Assert.That(currentFile, Is.EqualTo("TestConfigDefault.toml"));
        }

        [Test]
        public void GetConfigAsText_NullTypeName_Throws()
        {
            Assert.That(
                () => _storage.GetConfigAsText(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.GetConfigAsText(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileAsText_NullFileName_Throws()
        {
            Assert.That(
                () => _storage.GetFileAsText(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => _storage.GetFileAsText(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        // ----------------- fakes -----------------

        private class FakeFileSystem : IConfigFileSystem
        {
            private readonly Dictionary<string, string> _files =
                new();

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

        private class FakeDefinition : IConfigDefinition
        {
            private readonly string _typeName;
            private readonly Type _configType;
            private readonly ConfigBase _defaultInstance;

            public FakeDefinition(string typeName, Type configType, ConfigBase defaultInstance)
            {
                _typeName = typeName;
                _configType = configType;
                _defaultInstance = defaultInstance;
            }

            public string TypeName { get { return _typeName; } }

            public Type ConfigType { get { return _configType; } }

            public string SectionName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ConfigLocationType[] SupportedLocations
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ConfigBase CreateDefaultInstance()
            {
                var test = _defaultInstance as TestConfig;
                if (test != null)
                {
                    return new TestConfig
                    {
                        SomeValue = test.SomeValue
                    };
                }
                return _defaultInstance;
            }
        }

        private class TestConfig : ConfigBase
        {
            public int SomeValue { get; set; }

            public override string ConfigVersion { get { return "0.1.0"; } }

            public override string ConfigNameOverride { get { return "TestConfig"; } }
        }
    }
}
