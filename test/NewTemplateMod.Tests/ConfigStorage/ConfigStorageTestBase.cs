using mz.Config.Core;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.ConfigStorageTests
{
    public abstract class ConfigStorageTestBase
    {
        protected FakeFileSystem FileSystem;
        protected FakeSerializer Serializer;

        [SetUp]
        public void SetUp()
        {
            FileSystem = new FakeFileSystem();
            Serializer = new FakeSerializer();

            ConfigStorage.Initialize(FileSystem, Serializer);
            ConfigStorage.Register<TestConfig>(ConfigLocationType.Local);
        }
    }
}
