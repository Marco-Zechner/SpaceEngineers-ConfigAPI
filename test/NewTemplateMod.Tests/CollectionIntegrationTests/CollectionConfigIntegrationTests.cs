using System.Collections.Generic;
using System.Linq;
using mz.Config.Core;
using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using mz.NewTemplateMod;
using NUnit.Framework;

namespace NewTemplateMod.Tests.CollectionIntegrationTests
{
    /// <summary>
    /// Integration-style tests for CollectionConfig that go through
    /// the full ConfigStorage + LayoutMigrator + IdentityXmlConverter pipeline.
    ///
    /// These tests are the unit-test equivalent of the in-game commands:
    ///   /ntcfg collection get
    ///   /ntcfg collection addtag <tag>
    ///   /ntcfg collection reload
    /// plus manual remove operations.
    /// </summary>
    [TestFixture]
    public class CollectionConfigIntegrationTests
    {
        private FakeFileSystem _fileSystem;
        private TestXmlSerializer _xml;
        private ConfigLayoutMigrator _migrator;
        private IdentityXmlConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FakeFileSystem();
            _xml = new TestXmlSerializer();
            _migrator = new ConfigLayoutMigrator();
            _converter = new IdentityXmlConverter();

            InternalConfigStorage.Initialize(_fileSystem, _xml, _migrator, _converter);

            // Register CollectionConfig exactly like the mod does
            InternalConfigStorage.Register<CollectionConfig>(ConfigLocationType.Local);
        }

        private CollectionConfig Load()
        {
            return ConfigStorage.Load<CollectionConfig>(ConfigLocationType.Local);
        }

        private void Save()
        {
            ConfigStorage.Save<CollectionConfig>(ConfigLocationType.Local);
        }

        private static List<string> TagList(CollectionConfig cfg)
        {
            return cfg.Tags == null ? new List<string>() : new List<string>(cfg.Tags);
        }

        // --------------------------------------------------------------
        // 1) Initial load: defaults must be correct and stable
        // --------------------------------------------------------------

        [Test]
        public void Initial_Load_Uses_CollectionConfig_Defaults()
        {
            var cfg = Load();

            Assert.That(cfg, Is.Not.Null);
            Assert.Multiple(() =>
            {
                // Tags
                var tags = TagList(cfg);
                Assert.That(tags, Is.EqualTo(new[] { "alpha", "beta" }));

                // NamedValues (SerializableDictionary)
                Assert.That(cfg.NamedValues, Is.Not.Null);
                Assert.That(cfg.NamedValues.Dictionary, Has.Count.EqualTo(2));
                Assert.That(cfg.NamedValues.Dictionary["start"], Is.EqualTo(1));
                Assert.That(cfg.NamedValues.Dictionary["end"], Is.EqualTo(10));

                // Nested
                Assert.That(cfg.Nested, Is.Not.Null);
                Assert.That(cfg.Nested.Threshold, Is.EqualTo(0.75f).Within(1e-6f));
                Assert.That(cfg.Nested.Allowed, Is.True);
            });
        }

        // --------------------------------------------------------------
        // 2) AddTag + Save + Reload: tag must be appended once,
        //    and other fields must remain intact.
        // --------------------------------------------------------------

        [Test]
        public void AddTag_Save_Reload_Appends_Tag_Once_And_Preserves_Other_Fields()
        {
            // Initial load with defaults
            var cfg = Load();
            Assert.That(TagList(cfg), Is.EqualTo(new[] { "alpha", "beta" }));

            // Simulate: /ntcfg collection addtag hi
            cfg.Tags.Add("hi");
            Save();

            // Simulate: /ntcfg collection reload
            var reloaded = Load();

            Assert.Multiple(() =>
            {
                // Tags: should be alpha, beta, hi (no duplicates)
                Assert.That(TagList(reloaded), Is.EqualTo(new[] { "alpha", "beta", "hi" }));

                // NamedValues unchanged
                Assert.That(reloaded.NamedValues.Dictionary, Has.Count.EqualTo(2));
                Assert.That(reloaded.NamedValues.Dictionary["start"], Is.EqualTo(1));
                Assert.That(reloaded.NamedValues.Dictionary["end"], Is.EqualTo(10));

                // Nested unchanged
                Assert.That(reloaded.Nested.Threshold, Is.EqualTo(0.75f).Within(1e-6f));
                Assert.That(reloaded.Nested.Allowed, Is.True);
            });
        }

        // --------------------------------------------------------------
        // 3) Multiple add / reload cycles: no duplicate tags,
        //    sequence must be stable.
        // --------------------------------------------------------------

        [Test]
        public void Multiple_AddTag_And_Reload_Cycles_Do_Not_Create_Duplicates()
        {
            // Start: [alpha, beta]
            var cfg = Load();
            Assert.That(TagList(cfg), Is.EqualTo(new[] { "alpha", "beta" }));

            // /ntcfg collection addtag hi
            cfg.Tags.Add("hi");
            Save();

            // /ntcfg collection reload
            cfg = Load();
            Assert.That(TagList(cfg), Is.EqualTo(new[] { "alpha", "beta", "hi" }));

            // /ntcfg collection addtag bye
            cfg.Tags.Add("bye");
            Save();

            // /ntcfg collection reload
            cfg = Load();
            var tags = TagList(cfg);

            // Expect alpha, beta, hi, bye exactly once
            Assert.Multiple(() =>
            {
                Assert.That(tags, Is.EqualTo(new[] { "alpha", "beta", "hi", "bye" }));
                Assert.That(tags.Distinct().Count(), Is.EqualTo(tags.Count));
            });

            // Another reload must not change anything
            var cfg2 = Load();
            Assert.That(TagList(cfg2), Is.EqualTo(tags));
        }

        // --------------------------------------------------------------
        // 4) Remove + Save + Reload: removal must persist.
        // --------------------------------------------------------------

        [Test]
        public void RemoveTag_Save_Reload_Removal_Persists()
        {
            var cfg = Load();
            var tags = TagList(cfg);
            Assert.That(tags, Does.Contain("alpha"));
            Assert.That(tags, Does.Contain("beta"));

            // Remove "alpha" and save
            cfg.Tags.Remove("alpha");
            Save();

            // Reload
            var reloaded = Load();
            var tags2 = TagList(reloaded);

            Assert.Multiple(() =>
            {
                Assert.That(tags2, Does.Not.Contain("alpha"));
                Assert.That(tags2, Does.Contain("beta"));
                Assert.That(tags2, Has.Count.EqualTo(1));
            });
        }

        // --------------------------------------------------------------
        // 5) Mixed add/remove/reload sequence:
        //    - ensures that state transitions are robust and
        //      Nested + NamedValues stay intact the whole time.
        // --------------------------------------------------------------

        [Test]
        public void Mixed_Add_Remove_Reload_Sequence_Keeps_State_Consistent()
        {
            // Initial
            var cfg = Load();
            Assert.That(TagList(cfg), Is.EqualTo(new[] { "alpha", "beta" }));

            // Step 1: remove alpha, add gamma, save
            cfg.Tags.Remove("alpha");
            cfg.Tags.Add("gamma");
            Save();

            // reload
            cfg = Load();
            Assert.That(TagList(cfg), Is.EqualTo(new[] { "beta", "gamma" }));

            // Step 2: add alpha back, save again
            cfg.Tags.Add("alpha");
            Save();

            // reload
            cfg = Load();
            var tags = TagList(cfg);

            var expected = new HashSet<string>(new[] { "beta", "gamma", "alpha" });

            Assert.Multiple(() =>
            {
                Assert.That(tags, Has.Count.EqualTo(3));
                Assert.That(new HashSet<string>(tags), Is.EqualTo(expected));

                // NamedValues and Nested still intact
                Assert.That(cfg.NamedValues.Dictionary["start"], Is.EqualTo(1));
                Assert.That(cfg.NamedValues.Dictionary["end"], Is.EqualTo(10));
                Assert.That(cfg.Nested.Threshold, Is.EqualTo(0.75f).Within(1e-6f));
                Assert.That(cfg.Nested.Allowed, Is.True);
            });
        }
    }
}
