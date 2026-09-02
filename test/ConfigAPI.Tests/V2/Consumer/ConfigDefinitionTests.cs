using System;
using Mz.ConfigApi;
using NUnit.Framework;

namespace MarcoZechner.ConfigAPI.Tests.V2.Consumer
{
    [TestFixture]
    public sealed class ConfigDefinitionTests
    {
        [Test]
        public void Definition_Owns_Stable_Identity_And_Creates_Defaults_On_Demand()
        {
            var createCount = 0;

            var definition =
                new ConfigDefinition<ExampleConfig>(
                    " Settings ",
                    "settings.toml",
                    delegate
                    {
                        createCount++;

                        return new ExampleConfig
                        {
                            Value = createCount
                        };
                    });

            ExampleConfig first =
                definition.CreateDefaults();

            ExampleConfig second =
                definition.CreateDefaults();

            Assert.Multiple(() =>
            {
                Assert.That(definition.ConfigKey, Is.EqualTo("Settings"));
                Assert.That(definition.DefaultFile, Is.EqualTo("settings.toml"));
                Assert.That(first.Value, Is.EqualTo(1));
                Assert.That(second.Value, Is.EqualTo(2));
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(createCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void Definition_Rejects_Invalid_Identity_And_Null_Defaults()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(
                    () => new ConfigDefinition<ExampleConfig>(
                        " ",
                        "settings.toml",
                        () => new ExampleConfig()));

                Assert.Throws<ArgumentException>(
                    () => new ConfigDefinition<ExampleConfig>(
                        "Settings",
                        " ",
                        () => new ExampleConfig()));

                Assert.Throws<ArgumentNullException>(
                    () => new ConfigDefinition<ExampleConfig>(
                        "Settings",
                        "settings.toml",
                        null));

                var nullDefaults =
                    new ConfigDefinition<ExampleConfig>(
                        "Settings",
                        "settings.toml",
                        () => null);

                Assert.Throws<InvalidOperationException>(
                    () => nullDefaults.CreateDefaults());
            });
        }

        private sealed class ExampleConfig
        {
            public int Value { get; set; }
        }
    }
}