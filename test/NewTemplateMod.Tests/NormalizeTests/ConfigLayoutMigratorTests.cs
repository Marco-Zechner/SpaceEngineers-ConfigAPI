using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.NormalizeTests
{
    [TestFixture]
    public class ConfigLayoutMigratorTests
    {
        private ConfigDefinition<ExampleConfig> _definition;
        private ConfigLayoutMigrator _migrator;

        [SetUp]
        public void SetUp()
        {
            _definition = new ConfigDefinition<ExampleConfig>();
            _migrator = new ConfigLayoutMigrator();
        }

        private static string CurrentDefaultsXml()
        {
            // Current defaults in ExampleConfig:
            // RespondToHello = false
            // GreetingMessage = "hello"
            return
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>hello</GreetingMessage>" +
                "</ExampleConfig>";
        }

        // -------------------- basic behaviour --------------------

        [Test]
        public void Normalize_NoChanges_KeepsValues_AndDefaults_NoBackup()
        {
            // file has exactly current defaults
            var xmlCurrentFromFile = CurrentDefaultsXml();

            // old defaults also match (no version change)
            var xmlOldDefaultsFromFile = CurrentDefaultsXml();

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                CurrentDefaultsXml());

            // values stay the same
            Assert.That(result.NormalizedXml, Does.Contain("<RespondToHello>false</RespondToHello>"));
            Assert.That(result.NormalizedXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // defaults stay the same
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<RespondToHello>false</RespondToHello>"));
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // nothing destructive -> no backup
            Assert.That(result.RequiresBackup, Is.False);
        }

        [Test]
        public void Normalize_MissingKey_AddsWithCurrentDefault_NoBackup()
        {
            // file is missing GreetingMessage
            var xmlCurrentFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>true</RespondToHello>" +
                "</ExampleConfig>";

            // old defaults: only RespondToHello existed (older version)
            var xmlOldDefaultsFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "</ExampleConfig>";

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                CurrentDefaultsXml());

            // user value for RespondToHello is kept
            Assert.That(result.NormalizedXml, Does.Contain("<RespondToHello>true</RespondToHello>"));

            // missing GreetingMessage is added with current default "hello"
            Assert.That(result.NormalizedXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // defaults reflect current layout (include GreetingMessage)
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // adding keys is not destructive for user data -> no backup
            Assert.That(result.RequiresBackup, Is.False);
        }

        [Test]
        public void Normalize_ExtraKey_Removed_RequiresBackup()
        {
            // file has an extra setting not present in current layout
            var xmlCurrentFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>hello</GreetingMessage>" +
                "<ExtraSetting>5</ExtraSetting>" +
                "</ExampleConfig>";

            var xmlOldDefaultsFromFile = CurrentDefaultsXml();

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                CurrentDefaultsXml());

            // known keys remain
            Assert.That(result.NormalizedXml, Does.Contain("<RespondToHello>false</RespondToHello>"));
            Assert.That(result.NormalizedXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // unknown key is removed
            Assert.That(result.NormalizedXml, Does.Not.Contain("ExtraSetting"));

            // defaults are current layout
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<RespondToHello>false</RespondToHello>"));
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // destructive (dropped data) -> backup required
            Assert.That(result.RequiresBackup, Is.True);
        }

        // -------------------- default migration behaviour --------------------

        [Test]
        public void Normalize_ValueEqualsOldDefault_AndDefaultChanged_UpgradesToNewDefault()
        {
            // Old defaults: GreetingMessage used to be "old"
            var xmlOldDefaultsFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>old</GreetingMessage>" +
                "</ExampleConfig>";

            // File still has "old" => user never changed it
            var xmlCurrentFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>old</GreetingMessage>" +
                "</ExampleConfig>";

            // Current defaults: GreetingMessage = "hello"
            var xmlCurrentDefaults = CurrentDefaultsXml();

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                xmlCurrentDefaults);

            // Value should be upgraded to new default "hello"
            Assert.That(result.NormalizedXml, Does.Not.Contain("<GreetingMessage>old</GreetingMessage>"));
            Assert.That(result.NormalizedXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // Defaults must also be "hello"
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // No unknown keys removed -> no backup
            Assert.That(result.RequiresBackup, Is.False);
        }

        [Test]
        public void Normalize_UserOverride_IsPreserved_WhenDefaultChanges()
        {
            // Old defaults: "old"
            var xmlOldDefaultsFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>old</GreetingMessage>" +
                "</ExampleConfig>";

            // User explicitly changed GreetingMessage to "custom"
            var xmlCurrentFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>custom</GreetingMessage>" +
                "</ExampleConfig>";

            // Current default: "hello"
            var xmlCurrentDefaults = CurrentDefaultsXml();

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                xmlCurrentDefaults);

            // User override should be kept, not replaced by "hello"
            Assert.That(result.NormalizedXml, Does.Contain("<GreetingMessage>custom</GreetingMessage>"));
            Assert.That(result.NormalizedXml, Does.Not.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // Defaults describe code defaults, not user values
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            Assert.That(result.RequiresBackup, Is.False);
        }

        [Test]
        public void Normalize_NoOldDefaults_TreatsFileValuesAsUserValues()
        {
            // Simulate first run: no old defaults file present
            string xmlOldDefaultsFromFile = null;

            // File content with GreetingMessage = "old"
            var xmlCurrentFromFile =
                "<ExampleConfig>" +
                "<RespondToHello>false</RespondToHello>" +
                "<GreetingMessage>old</GreetingMessage>" +
                "</ExampleConfig>";

            // Current defaults: "hello"
            var xmlCurrentDefaults = CurrentDefaultsXml();

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaultsFromFile,
                xmlCurrentDefaults);

            // Without previous defaults, we cannot know "old" was a default,
            // so we MUST treat it as a user choice and keep it.
            Assert.That(result.NormalizedXml, Does.Contain("<GreetingMessage>old</GreetingMessage>"));
            Assert.That(result.NormalizedXml, Does.Not.Contain("<GreetingMessage>hello</GreetingMessage>"));

            // Defaults file still records "hello" as the current default
            Assert.That(result.NormalizedDefaultsXml, Does.Contain("<GreetingMessage>hello</GreetingMessage>"));

            Assert.That(result.RequiresBackup, Is.False);
        }
    }
}
