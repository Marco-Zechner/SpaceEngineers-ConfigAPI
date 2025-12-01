using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    public abstract class ConfigStorageTestBase
    {
        protected FakeFileSystem FileSystem;
        protected TestXmlSerializer XmlSerializer;
        protected ConfigLayoutMigrator LayoutMigrator;
        protected TomlXmlConverter Converter;

        [SetUp]
        public virtual void SetUp()
        {
            FileSystem = new FakeFileSystem();
            XmlSerializer = new TestXmlSerializer();
            LayoutMigrator = new ConfigLayoutMigrator();
            Converter = new TomlXmlConverter(XmlSerializer);

            ConfigStorage.Initialize(FileSystem, XmlSerializer, LayoutMigrator, Converter);
            ConfigStorage.Register<TestConfig>(ConfigLocationType.Local);
        }
    }
}