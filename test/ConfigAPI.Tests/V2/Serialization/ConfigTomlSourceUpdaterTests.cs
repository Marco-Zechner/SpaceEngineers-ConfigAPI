using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.V2.Domain;
using MarcoZechner.ConfigAPI.V2.Serialization;
using Mz.Toml;
using NUnit.Framework;

namespace MarcoZechner.ConfigAPI.Tests.V2.Serialization
{
    [TestFixture]
    public sealed class ConfigTomlSourceUpdaterTests
    {
        [Test]
        public void SetValue_Replaces_Only_Value_Source()
        {
            var source =
                "# user comment\r\n" +
                "answer   = 1   # keep this\r\n" +
                "other = \"unchanged\"\r\n";

            var edited = ConfigTomlSourceUpdater.SetValue(
                source,
                new ConfigValuePath("answer"),
                ConfigScalarNode.Integer(42));

            Assert.That(
                edited,
                Is.EqualTo(
                    "# user comment\r\n" +
                    "answer   = 42   # keep this\r\n" +
                    "other = \"unchanged\"\r\n"));
        }

        [Test]
        public void SetValue_Null_Disables_Assignment_Without_Losing_Previous_Value()
        {
            var source = "optional = \"custom\" # retained\n";

            var edited = ConfigTomlSourceUpdater.SetValue(
                source,
                new ConfigValuePath("optional"),
                ConfigNullNode.Instance);

            Assert.That(
                edited,
                Is.EqualTo("#!optional = \"custom\" # retained\n"));
        }

        [Test]
        public void SetValue_Null_Leaves_Already_Disabled_Assignment_Unchanged()
        {
            var source = "#!optional = \"old\" # retained\n";

            var edited = ConfigTomlSourceUpdater.SetValue(
                source,
                new ConfigValuePath("optional"),
                ConfigNullNode.Instance);

            Assert.That(edited, Is.EqualTo(source));
        }

        [Test]
        public void SetValue_Concrete_Value_Enables_Disabled_Assignment_And_Replaces_Value()
        {
            var source = "#!optional = \"old\" # retained\n";

            var edited = ConfigTomlSourceUpdater.SetValue(
                source,
                new ConfigValuePath("optional"),
                ConfigScalarNode.String("new"));

            Assert.That(
                edited,
                Is.EqualTo("optional = \"new\" # retained\n"));
        }

        [Test]
        public void SetValue_Resolves_Dotted_And_Quoted_Config_Path()
        {
            var source = "\"section.name\".child = 1\n";

            var edited = ConfigTomlSourceUpdater.SetValue(
                source,
                new ConfigValuePath("section.name", "child"),
                ConfigScalarNode.Integer(5));

            Assert.That(
                edited,
                Is.EqualTo("\"section.name\".child = 5\n"));
        }

        [Test]
        public void SetValue_Renders_Object_And_Array_As_One_Canonical_Toml_Value()
        {
            var source = "settings = { old = 1 } # retained\n";

            var edited = ConfigTomlSourceUpdater.SetValue(
                source,
                new ConfigValuePath("settings"),
                new ConfigObjectNode(
                    new ConfigObjectEntry(
                        "Name",
                        ConfigScalarNode.String("Flight")),
                    new ConfigObjectEntry(
                        "Flags",
                        new ConfigArrayNode(
                            ConfigScalarNode.Boolean(true),
                            ConfigScalarNode.Boolean(false)))));

            var parsed = Toml.Parse(edited);
            var config = ConfigTomlDocumentCodec.FromTomlDocument(parsed);

            ConfigNode actual;

            Assert.Multiple(() =>
            {
                Assert.That(
                    edited,
                    Is.EqualTo(
                        "settings = {Name = \"Flight\", Flags = [true, false]} # retained\n"));

                Assert.That(
                    config.TryGet(new ConfigValuePath("settings"), out actual),
                    Is.True);

                Assert.That(
                    actual,
                    Is.EqualTo(
                        new ConfigObjectNode(
                            new ConfigObjectEntry(
                                "Name",
                                ConfigScalarNode.String("Flight")),
                            new ConfigObjectEntry(
                                "Flags",
                                new ConfigArrayNode(
                                    ConfigScalarNode.Boolean(true),
                                    ConfigScalarNode.Boolean(false))))));
            });
        }

        [Test]
        public void SetValue_Rejects_Missing_Or_Ambiguous_Assignment()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<KeyNotFoundException>(() =>
                    ConfigTomlSourceUpdater.SetValue(
                        "value = 1\n",
                        new ConfigValuePath("missing"),
                        ConfigScalarNode.Integer(2)));

                Assert.Throws<InvalidOperationException>(() =>
                    ConfigTomlSourceUpdater.SetValue(
                        "#!value = 1\n#!value = 2\n",
                        new ConfigValuePath("value"),
                        ConfigScalarNode.Integer(3)));
            });
        }

        [Test]
        public void SetValue_Rejects_Invalid_Source_And_Null_Arguments()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() =>
                    ConfigTomlSourceUpdater.SetValue(
                        null,
                        new ConfigValuePath("value"),
                        ConfigScalarNode.Integer(1)));

                Assert.Throws<ArgumentNullException>(() =>
                    ConfigTomlSourceUpdater.SetValue(
                        "value = 1\n",
                        null,
                        ConfigScalarNode.Integer(1)));

                Assert.Throws<ArgumentNullException>(() =>
                    ConfigTomlSourceUpdater.SetValue(
                        "value = 1\n",
                        new ConfigValuePath("value"),
                        null));

                Assert.Throws<ArgumentException>(() =>
                    ConfigTomlSourceUpdater.SetValue(
                        "value =",
                        new ConfigValuePath("value"),
                        ConfigScalarNode.Integer(1)));
            });
        }
    }
}
