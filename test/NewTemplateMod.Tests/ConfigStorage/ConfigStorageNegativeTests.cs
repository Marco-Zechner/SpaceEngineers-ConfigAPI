using System;
using mz.Config.Core;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    [TestFixture]
    public class ConfigStorageNegativeTests : ConfigStorageTestBase
    {
        [Test]
        public void Initialize_NullFileSystem_Throws()
        {
            Assert.That(
                () => ConfigStorage.Initialize(null, Serializer),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Initialize_NullSerializer_Throws()
        {
            Assert.That(
                () => ConfigStorage.Initialize(FileSystem, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetOrCreate_NoDefinitionForType_ThrowsInvalidOperationException()
        {
            ConfigStorage.Initialize(FileSystem, Serializer);
            // do NOT register OtherConfig

            Assert.That(
                () => ConfigStorage.GetOrCreate<OtherConfig>(ConfigLocationType.Local),
                Throws.TypeOf<InvalidOperationException>());
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
            string originalFileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");

            bool result = ConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "does_not_exist.toml");

            Assert.That(result, Is.False);

            string fileName = ConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(fileName, Is.EqualTo(originalFileName));
        }

        [Test]
        public void Load_DeserializerReturnsNull_ReturnsFalse_AndDoesNotOverrideExistingInstance()
        {
            TestConfig original = ConfigStorage.GetOrCreate<TestConfig>(ConfigLocationType.Local);
            original.SomeValue = 5;

            Serializer.DeserializeResult = null;
            FileSystem.WriteFile(ConfigLocationType.Local, "bad.toml", "bad content");

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
    }
}
