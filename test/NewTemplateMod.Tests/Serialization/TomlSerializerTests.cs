using mz.Config.Core;
using mz.Config.Domain;
using mz.Config.Abstractions;

namespace NewTemplateMod.Tests.Serialization
{
    [TestFixture]
    public class TomlConfigSerializerTests
    {
        private TomlConfigSerializer _serializer;
        private IConfigDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _serializer = new TomlConfigSerializer();
            _definition = new ConfigDefinition<ExampleConfig>("ExampleConfig");
        }

        [Test]
        public void Serialize_DefaultExampleConfig_ContainsExpectedLines()
        {
            // Arrange
            var config = new ExampleConfig();

            // Act
            var toml = _serializer.Serialize(config);

            // Assert (loose on whitespace / order, strict on content)
            Assert.That(toml, Does.Contain("[ExampleConfig]"));
            Assert.That(toml, Does.Contain("StoredVersion"));
            Assert.That(toml, Does.Contain("\"0.1.0\""));

            Assert.That(toml, Does.Contain("RespondToHello"));
            Assert.That(toml, Does.Contain("false"));
            Assert.That(toml, Does.Contain("# false"));
            
            Assert.That(toml, Does.Contain("GreetingMessage"));
            Assert.That(toml, Does.Contain("\"hello\""));
            Assert.That(toml, Does.Contain("# \"hello\""));
        }

        [Test]
        public void Serialize_ModifiedExampleConfig_ContainsUpdatedValues_AndSameDefaults()
        {
            // Arrange
            var config = new ExampleConfig();
            config.RespondToHello = true;
            config.GreetingMessage = "hi";

            // Act
            var toml = _serializer.Serialize(config);

            // Assert: values reflect current config
            Assert.That(toml, Does.Contain("RespondToHello"));
            Assert.That(toml, Does.Contain("true"));
            Assert.That(toml, Does.Contain("# false")); // default is still false in comment

            Assert.That(toml, Does.Contain("GreetingMessage"));
            Assert.That(toml, Does.Contain("\"hi\""));
            Assert.That(toml, Does.Contain("# \"hello\"")); // default is still "hello"
        }

        [Test]
        public void Deserialize_RoundTrip_ProducesEquivalentConfig()
        {
            // Arrange
            var original = new ExampleConfig();
            original.RespondToHello = true;
            original.GreetingMessage = "hi";

            var toml = _serializer.Serialize(original);

            // Act
            var result = _serializer.Deserialize(_definition, toml);
            var cfg = result as ExampleConfig;

            // Assert
            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg.RespondToHello, Is.True);
            Assert.That(cfg.GreetingMessage, Is.EqualTo("hi"));
        }

        [Test]
        public void Deserialize_HandWrittenToml_UsesProvidedValues()
        {
            // Arrange: simulate a file
            var toml = 
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.1.0\"\n" +
                "RespondToHello = true # false\n" +
                "GreetingMessage = \"custom\" # \"hello\"\n";

            // Act
            var result = _serializer.Deserialize(_definition, toml);
            var cfg = result as ExampleConfig;

            // Assert
            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg.RespondToHello, Is.True);
            Assert.That(cfg.GreetingMessage, Is.EqualTo("custom"));
        }

        [Test]
        public void Deserialize_WhenValueEqualsOldDefaultComment_UsesCurrentDefault()
        {
            // Simulate an old file where the default for GreetingMessage used to be "old",
            // and the user never changed it (value == default comment).
            var toml =
                "[ExampleConfig]\n" +
                "StoredVersion = \"0.0.1\"\n" +
                "RespondToHello = false # false\n" +
                "GreetingMessage = \"old\" # \"old\"\n";

            var result = _serializer.Deserialize(_definition, toml);
            var cfg = result as ExampleConfig;

            Assert.That(cfg, Is.Not.Null);
            // Current default in ExampleConfig ctor is "hello"
            Assert.That(cfg.GreetingMessage, Is.EqualTo("hello"));
        }

    }
}
