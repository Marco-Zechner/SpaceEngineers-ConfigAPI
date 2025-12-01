using System;
using mz.Config.Core.Storage;
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
                () => InternalConfigStorage.Initialize(null, XmlSerializer, LayoutMigrator, Converter),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Initialize_NullXmlSerializer_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.Initialize(FileSystem, null, LayoutMigrator, Converter),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Initialize_NullLayoutMigrator_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.Initialize(FileSystem, XmlSerializer, null, Converter),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Initialize_NullConverter_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.Initialize(FileSystem, XmlSerializer, LayoutMigrator, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetOrCreate_NoDefinitionForType_ThrowsInvalidOperationException()
        {
            // Re-init without registering OtherConfig
            InternalConfigStorage.Initialize(FileSystem, XmlSerializer, LayoutMigrator, Converter);

            Assert.That(
                () => InternalConfigStorage.GetOrCreate<OtherConfig>(ConfigLocationType.Local),
                Throws.TypeOf<InvalidOperationException>());
        }

        private class OtherConfig : ConfigBase
        {
            public override string ConfigVersion => "1.0.0";
        }

        [Test]
        public void GetCurrentFileName_NullTypeName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void SetCurrentFileName_NullTypeName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.SetCurrentFileName(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.SetCurrentFileName(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void SetCurrentFileName_NullFileName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.SetCurrentFileName(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.SetCurrentFileName(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Load_NullArguments_Throw()
        {
            Assert.That(
                () => InternalConfigStorage.Load(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.Load(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.Load(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.Load(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Save_NullArguments_Throw()
        {
            Assert.That(
                () => InternalConfigStorage.Save(ConfigLocationType.Local, null, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.Save(ConfigLocationType.Local, string.Empty, "file.toml"),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.Save(ConfigLocationType.Local, "TestConfig", null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.Save(ConfigLocationType.Local, "TestConfig", string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Load_UnknownTypeName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.Load(ConfigLocationType.Local, "UnknownType", "file.toml"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Save_UnknownTypeName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.Save(ConfigLocationType.Local, "UnknownType", "file.toml"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Load_FileMissing_ReturnsFalse_AndDoesNotChangeCurrentFileName()
        {
            var originalFileName = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");

            var result = InternalConfigStorage.Load(ConfigLocationType.Local, "TestConfig", "does_not_exist.toml");

            Assert.That(result, Is.False);

            var fileName = InternalConfigStorage.GetCurrentFileName(ConfigLocationType.Local, "TestConfig");
            Assert.That(fileName, Is.EqualTo(originalFileName));
        }

        [Test]
        public void GetConfigAsText_NullTypeName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.GetConfigAsText(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.GetConfigAsText(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GetFileAsText_NullFileName_Throws()
        {
            Assert.That(
                () => InternalConfigStorage.GetFileAsText(ConfigLocationType.Local, null),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                () => InternalConfigStorage.GetFileAsText(ConfigLocationType.Local, string.Empty),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}
