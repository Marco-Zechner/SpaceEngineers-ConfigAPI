using System.Collections.Generic;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.NormalizeTests
{
    [TestFixture]
    public class ConfigLayoutMigratorComplexTests
    {
        private ConfigLayoutMigrator _migrator;
        private IConfigDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _migrator = new ConfigLayoutMigrator();
            _definition = new ConfigDefinition<ComplexConfig>();
        }

        // Helper: build a very simple XML document matching what SimpleXml expects.
        private static string BuildXml(string rootName, IDictionary<string, string> values)
        {
            var sb = new StringBuilder();
            sb.Append('<').Append(rootName).Append('>');
            foreach (var kv in values)
            {
                sb.Append('<').Append(kv.Key).Append('>');
                sb.Append(kv.Value);
                sb.Append("</").Append(kv.Key).Append('>');
            }
            sb.Append("</").Append(rootName).Append('>');
            return sb.ToString();
        }

        // -------------------------
        // TEST 1: extra key -> backup
        // -------------------------

        [Test]
        public void Normalize_WithExtraKey_RemovesIt_AndRequiresBackup()
        {
            var defaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "NewName" },
                { "Description", "NewDesc" }
            };

            var xmlCurrentDefaults = BuildXml(_definition.TypeName, defaults);
            var xmlOldDefaults = xmlCurrentDefaults;

            var fileValues = new Dictionary<string, string>(defaults)
            {
                ["ExtraKey"] = "XYZ"
            };
            var xmlCurrentFromFile = BuildXml(_definition.TypeName, fileValues);

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaults,
                xmlCurrentDefaults);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBackup, Is.True);
                Assert.That(result.NormalizedXml, Does.Contain("<IntA>1</IntA>"));
                Assert.That(result.NormalizedXml, Does.Contain("<IntB>2</IntB>"));
                Assert.That(result.NormalizedXml, Does.Contain("<Name>NewName</Name>"));
                Assert.That(result.NormalizedXml, Does.Contain("<Description>NewDesc</Description>"));
                Assert.That(result.NormalizedXml, Does.Not.Contain("ExtraKey"));
            });

            Assert.That(result.NormalizedDefaultsXml, Is.EqualTo(xmlCurrentDefaults));
        }

        // -------------------------
        // TEST 2: missing key -> add with default, no backup
        // -------------------------

        [Test]
        public void Normalize_WithMissingKey_AddsDefault_NoBackup()
        {
            var defaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "NewName" },
                { "Description", "NewDesc" }
            };

            var xmlCurrentDefaults = BuildXml(_definition.TypeName, defaults);
            var xmlOldDefaults = xmlCurrentDefaults;

            var fileValues = new Dictionary<string, string>
            {
                { "IntA", "10" },            // user-changed
                { "Name", "CustomName" }     // user-changed
                // IntB, Description missing
            };
            var xmlCurrentFromFile = BuildXml(_definition.TypeName, fileValues);

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaults,
                xmlCurrentDefaults);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBackup, Is.False);

                // user values preserved
                Assert.That(result.NormalizedXml, Does.Contain("<IntA>10</IntA>"));
                Assert.That(result.NormalizedXml, Does.Contain("<Name>CustomName</Name>"));

                // missing keys filled with defaults
                Assert.That(result.NormalizedXml, Does.Contain("<IntB>2</IntB>"));
                Assert.That(result.NormalizedXml, Does.Contain("<Description>NewDesc</Description>"));
            });

            Assert.That(result.NormalizedDefaultsXml, Is.EqualTo(xmlCurrentDefaults));
        }

        // -------------------------
        // TEST 3: defaults changed, user never touched -> auto-upgrade
        // -------------------------

        [Test]
        public void Normalize_WithChangedDefaultsAndUnchangedUserValues_UpgradesToNewDefaults()
        {
            var oldDefaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "OldName" },
                { "Description", "OldDesc" }
            };

            var newDefaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "NewName" },
                { "Description", "NewDesc" }
            };

            var xmlOldDefaults = BuildXml(_definition.TypeName, oldDefaults);
            var xmlCurrentDefaults = BuildXml(_definition.TypeName, newDefaults);

            // File still has old defaults => user never changed Name/Description
            var fileValues = new Dictionary<string, string>(oldDefaults);
            var xmlCurrentFromFile = BuildXml(_definition.TypeName, fileValues);

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaults,
                xmlCurrentDefaults);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBackup, Is.False);

                // Int values unchanged
                Assert.That(result.NormalizedXml, Does.Contain("<IntA>1</IntA>"));
                Assert.That(result.NormalizedXml, Does.Contain("<IntB>2</IntB>"));

                // Defaults changed and user didn't modify -> upgraded to new defaults
                Assert.That(result.NormalizedXml, Does.Contain("<Name>NewName</Name>"));
                Assert.That(result.NormalizedXml, Does.Contain("<Description>NewDesc</Description>"));
            });

            // Defaults file must reflect new defaults
            Assert.That(result.NormalizedDefaultsXml, Is.EqualTo(xmlCurrentDefaults));
        }

        // -------------------------
        // TEST 4: defaults changed, user modified one field -> only untouched one upgraded
        // -------------------------

        [Test]
        public void Normalize_WithChangedDefaultsAndMixedUserChanges_OnlyUnchangedFieldsUpgrade()
        {
            var oldDefaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "OldName" },
                { "Description", "OldDesc" }
            };

            var newDefaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "NewName" },
                { "Description", "NewDesc" }
            };

            var xmlOldDefaults = BuildXml(_definition.TypeName, oldDefaults);
            var xmlCurrentDefaults = BuildXml(_definition.TypeName, newDefaults);

            // User changed Name but left Description at old default.
            var fileValues = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "CustomName" },   // user-changed
                { "Description", "OldDesc" } // still old default
            };
            var xmlCurrentFromFile = BuildXml(_definition.TypeName, fileValues);

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaults,
                xmlCurrentDefaults);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBackup, Is.False);

                // Ints unchanged
                Assert.That(result.NormalizedXml, Does.Contain("<IntA>1</IntA>"));
                Assert.That(result.NormalizedXml, Does.Contain("<IntB>2</IntB>"));

                // Name was changed by user -> keep user value
                Assert.That(result.NormalizedXml, Does.Contain("<Name>CustomName</Name>"));

                // Description stayed at old default -> should be upgraded to new default
                Assert.That(result.NormalizedXml, Does.Contain("<Description>NewDesc</Description>"));
            });

            Assert.That(result.NormalizedDefaultsXml, Is.EqualTo(xmlCurrentDefaults));
        }

        // -------------------------
        // TEST 5: no old defaults file -> no auto-upgrade, user values preserved
        // -------------------------

        [Test]
        public void Normalize_WithoutOldDefaults_DoesNotUpgradeToNewDefaults()
        {
            var newDefaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "NewName" },
                { "Description", "NewDesc" }
            };

            var xmlCurrentDefaults = BuildXml(_definition.TypeName, newDefaults);
            string xmlOldDefaults = null; // simulate "no history" case

            var fileValues = new Dictionary<string, string>
            {
                // these look like "old" defaults from some earlier version,
                // but migrator has no knowledge of that because xmlOldDefaults is null.
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "OldName" },
                { "Description", "OldDesc" }
            };
            var xmlCurrentFromFile = BuildXml(_definition.TypeName, fileValues);

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaults,
                xmlCurrentDefaults);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBackup, Is.False);

                // Because we have no old-defaults history, migrator must treat
                // these as user choices and NOT auto-upgrade.
                Assert.That(result.NormalizedXml, Does.Contain("<Name>OldName</Name>"));
                Assert.That(result.NormalizedXml, Does.Contain("<Description>OldDesc</Description>"));
            });

            Assert.That(result.NormalizedDefaultsXml, Is.EqualTo(xmlCurrentDefaults));
        }

        // -------------------------
        // TEST 6: empty file -> falls back to current defaults
        // -------------------------

        [Test]
        public void Normalize_WithEmptyFile_UsesCurrentDefaults_NoBackup()
        {
            var newDefaults = new Dictionary<string, string>
            {
                { "IntA", "1" },
                { "IntB", "2" },
                { "Name", "NewName" },
                { "Description", "NewDesc" }
            };

            var xmlCurrentDefaults = BuildXml(_definition.TypeName, newDefaults);
            var xmlOldDefaults = xmlCurrentDefaults;

            string xmlCurrentFromFile = null; // simulate empty / missing config content

            var result = _migrator.Normalize(
                _definition,
                xmlCurrentFromFile,
                xmlOldDefaults,
                xmlCurrentDefaults);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBackup, Is.False);
                Assert.That(result.NormalizedXml, Is.EqualTo(xmlCurrentDefaults));
                Assert.That(result.NormalizedDefaultsXml, Is.EqualTo(xmlCurrentDefaults));
            });
        }

        // -------------------------
        // Test-only complex config
        // -------------------------

        private class ComplexConfig : ConfigBase
        {
            public override string ConfigVersion => "2.0.0";

            public int IntA { get; set; } = 1;
            public int IntB { get; set; } = 2;
            public string Name { get; set; } = "NewName";
            public string Description { get; set; } = "NewDesc";
        }
    }
}
