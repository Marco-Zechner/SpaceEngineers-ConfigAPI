using System;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Converter;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;
using System.Collections.Generic;
using mz.Config.Abstractions.Layout;
using mz.Config.Core.Layout;
using mz.SemanticVersioning;

namespace NewTemplateMod.Tests.TomlTests
{
    [TestFixture]
    public class TomlCommentTests
    {
        private IConfigFileSystem _fileSystem;
        private IConfigXmlSerializer _xml;
        private IConfigLayoutMigrator _migrator;
        private TomlXmlConverter _converter;
        private IConfigDefinition _definition;

        /// <summary>
        /// Config with descriptions, including multiline doc for one field.
        /// </summary>
        public class CommentConfig : ConfigBase
        {
            public override SemanticVersion ConfigVersion => "1.0.0";

            public override string ConfigNameOverride => "CommentConfig";

            public bool Flag { get; set; } = true;
            public int Count { get; set; } = 42;
            public string Note { get; set; } = "hello";

            private static readonly IReadOnlyDictionary<string, string> _descriptions =
                new Dictionary<string, string>
                {
                    { nameof(Flag), "Whether the feature is enabled." },
                    { nameof(Count), "Number of retries.\nMust be >= 0.\nUsed by subsystem X." },
                    { nameof(Note), "Arbitrary note for debugging." }
                };

            public override IReadOnlyDictionary<string, string> VariableDescriptions => _descriptions;
        }

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            _xml = new TestXmlSerializer();
            _migrator = new ConfigLayoutMigrator();
            _converter = new TomlXmlConverter(_xml);
            _definition = new ConfigDefinition<CommentConfig>();
            InternalConfigStorage.Initialize(_fileSystem, _xml, _migrator, _converter);
        }

        [Test]
        public void ToExternal_IncludesDescriptionCommentsAboveKeys()
        {
            // Arrange
            var cfg = new CommentConfig
            {
                Flag = false,
                Count = 99,
                Note = "note-text"
            };

            var xmlContent = _xml.SerializeToXml(cfg);

            // Act
            var toml = _converter.ToExternal(_definition, xmlContent);

            // Assert: comments for each key exist
            Assert.That(toml, Does.Contain("[CommentConfig]"));

            // Single-line comment for Flag
            Assert.That(toml, Does.Contain("# Whether the feature is enabled."));
            Assert.That(toml, Does.Contain("Flag = false"));

            // Multiline comment for Count
            Assert.That(toml, Does.Contain("# Number of retries."));
            Assert.That(toml, Does.Contain("# Must be >= 0."));
            Assert.That(toml, Does.Contain("# Used by subsystem X."));
            Assert.That(toml, Does.Contain("Count = 99"));

            // Single-line comment for Note
            Assert.That(toml, Does.Contain("# Arbitrary note for debugging."));
            Assert.That(toml, Does.Contain("Note = \"note-text\""));

            // Optional: check ordering (comment directly before the key)
            var idxCommentFlag = toml.IndexOf("# Whether the feature is enabled.", StringComparison.Ordinal);
            var idxKeyFlag = toml.IndexOf("Flag = false", StringComparison.Ordinal);
            Assert.That(idxCommentFlag, Is.GreaterThan(-1));
            Assert.That(idxKeyFlag, Is.GreaterThan(idxCommentFlag));
        }

        [Test]
        public void Roundtrip_WithDescriptionComments_PreservesValues()
        {
            // Arrange
            var original = new CommentConfig
            {
                Flag = true,
                Count = 7,
                Note = "roundtrip"
            };

            var xml1 = _xml.SerializeToXml(original);
            var tomlWithComments = _converter.ToExternal(_definition, xml1);

            // Act
            var xml2 = _converter.ToInternal(_definition, tomlWithComments);
            var restored = (CommentConfig)_definition.DeserializeFromXml(_xml, xml2);

            // Assert: comments must not affect loaded values
            Assert.Multiple(() =>
            {
                Assert.That(restored.Flag, Is.EqualTo(true));
                Assert.That(restored.Count, Is.EqualTo(7));
                Assert.That(restored.Note, Is.EqualTo("roundtrip"));
            });
        }

        [Test]
        public void ToInternal_IgnoresFullLineAndInlineComments()
        {
            // Arrange: handcrafted TOML with various comment styles
            var toml =
                "[CommentConfig]\n" +
                "# Top-level doc line that should be ignored\n" +
                "# Another doc line\n" +
                "Flag = false # user changed this and added an inline comment\n" +
                "\n" +
                "# Multiline doc for Count\n" +
                "# line 1\n" +
                "# line 2\n" +
                "Count = 123 # trailing garbage\n" +
                "\n" +
                "# Comment above Note\n" +
                "Note = \"custom\" # another inline comment\n";

            // Act
            var xml = _converter.ToInternal(_definition, toml);
            var cfg = (CommentConfig)_definition.DeserializeFromXml(_xml, xml);

            // Assert: values parsed correctly, comments ignored
            Assert.Multiple(() =>
            {
                Assert.That(cfg.Flag, Is.EqualTo(false));
                Assert.That(cfg.Count, Is.EqualTo(123));
                Assert.That(cfg.Note, Is.EqualTo("custom"));
            });
        }
    }
}
