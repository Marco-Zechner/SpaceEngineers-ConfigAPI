using System.Collections.Generic;
using System.Linq;
using mz.Config.Core;
using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;
using NUnit.Framework;

namespace NewTemplateMod.Tests.CollectionIntegrationTests
{
    /// <summary>
    /// Integration-style tests for the NamedValues dictionary inside CollectionConfig.
    ///
    /// These mirror the "add/remove + reload" patterns we test for Tags, but now focused
    /// on the SerializableDictionary:
    ///   - initial defaults
    ///   - add / update / remove entries
    ///   - multiple reload cycles
    ///   - mixed changes with Tags to ensure they don't interfere.
    /// </summary>
    [TestFixture]
    public class CollectionConfigDictionaryIntegrationTests
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

            InternalConfigStorage.Initialize(_fileSystem, _xml, _migrator, _converter, null);

            // Register CollectionConfig exactly as in the mod
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

        private static Dictionary<string, int> DictSnapshot(CollectionConfig cfg)
        {
            if (cfg == null || cfg.NamedValues == null || cfg.NamedValues.Dictionary == null)
                return new Dictionary<string, int>();

            // Take a snapshot so later changes on cfg don’t affect assertions
            return new Dictionary<string, int>(cfg.NamedValues.Dictionary);
        }

        // --------------------------------------------------------------
        // 1) Initial load: NamedValues defaults must be correct and stable
        // --------------------------------------------------------------

        [Test]
        public void Initial_Load_Uses_NamedValues_Defaults()
        {
            var cfg = Load();

            Assert.That(cfg, Is.Not.Null);

            var dict = DictSnapshot(cfg);

            Assert.Multiple(() =>
            {
                Assert.That(dict, Has.Count.EqualTo(2));
                Assert.That(dict.ContainsKey("start"), Is.True);
                Assert.That(dict.ContainsKey("end"), Is.True);
                Assert.That(dict["start"], Is.EqualTo(1));
                Assert.That(dict["end"], Is.EqualTo(10));
            });
        }

        // --------------------------------------------------------------
        // 2) Add entry + Save + Reload: new pair must be present once,
        //    and original defaults must be preserved.
        // --------------------------------------------------------------

        [Test]
        public void AddEntry_Save_Reload_Preserves_Defaults_And_Adds_New_Entry()
        {
            var cfg = Load();
            var dict0 = DictSnapshot(cfg);

            Assert.Multiple(() =>
            {
                Assert.That(dict0.ContainsKey("start"), Is.True);
                Assert.That(dict0.ContainsKey("end"), Is.True);
                Assert.That(dict0, Has.Count.EqualTo(2));
            });

            // Simulate: add a new key "mid" = 5
            cfg.NamedValues.Dictionary["mid"] = 5;
            Save();

            var reloaded = Load();
            var dict1 = DictSnapshot(reloaded);

            Assert.Multiple(() =>
            {
                Assert.That(dict1, Has.Count.EqualTo(3));
                Assert.That(dict1["start"], Is.EqualTo(1));
                Assert.That(dict1["end"], Is.EqualTo(10));
                Assert.That(dict1["mid"], Is.EqualTo(5));
            });
        }

        // --------------------------------------------------------------
        // 3) Update an existing entry + Save + Reload:
        //    value must be updated, no extra entries created.
        // --------------------------------------------------------------

        [Test]
        public void UpdateEntry_Save_Reload_Updates_Value_Without_Duplicates()
        {
            var cfg = Load();

            // Update defaults: start -> 2, end -> 20
            cfg.NamedValues.Dictionary["start"] = 2;
            cfg.NamedValues.Dictionary["end"] = 20;

            Save();

            var reloaded = Load();
            var dict = DictSnapshot(reloaded);

            Assert.Multiple(() =>
            {
                Assert.That(dict, Has.Count.EqualTo(2), "No new keys should appear.");
                Assert.That(dict["start"], Is.EqualTo(2));
                Assert.That(dict["end"], Is.EqualTo(20));
            });

            // Another reload should be stable (idempotent)
            var reloaded2 = Load();
            var dict2 = DictSnapshot(reloaded2);
            Assert.That(dict2, Is.EqualTo(dict));
        }

        // --------------------------------------------------------------
        // 4) Remove entry + Save + Reload:
        //    removed pair must stay removed.
        // --------------------------------------------------------------

        [Test]
        public void RemoveEntry_Save_Reload_Removal_Persists()
        {
            var cfg = Load();
            var dict0 = DictSnapshot(cfg);

            Assert.Multiple(() =>
            {
                Assert.That(dict0.ContainsKey("start"), Is.True);
                Assert.That(dict0.ContainsKey("end"), Is.True);
            });

            // Remove "start"
            cfg.NamedValues.Dictionary.Remove("start");
            Save();

            var reloaded = Load();
            var dict1 = DictSnapshot(reloaded);

            Assert.Multiple(() =>
            {
                Assert.That(dict1.ContainsKey("start"), Is.False);
                Assert.That(dict1.ContainsKey("end"), Is.True);
                Assert.That(dict1["end"], Is.EqualTo(10));
                Assert.That(dict1, Has.Count.EqualTo(1));
            });
        }

        // --------------------------------------------------------------
        // 5) Multiple add/update/remove cycles + reload:
        //    - final state must match exactly what we expect
        //    - no hidden duplicates
        // --------------------------------------------------------------

        [Test]
        public void Mixed_Add_Update_Remove_Reload_Produces_Expected_Final_Dictionary()
        {
            var cfg = Load();

            // Start from defaults: { start=1, end=10 }

            // Step 1: add mid=5
            cfg.NamedValues.Dictionary["mid"] = 5;
            Save();

            cfg = Load();
            var step1 = DictSnapshot(cfg);
            Assert.That(step1.Keys.OrderBy(k => k),
                Is.EqualTo(new[] { "end", "mid", "start" }));

            // Step 2: change start=2, remove end
            cfg.NamedValues.Dictionary["start"] = 2;
            cfg.NamedValues.Dictionary.Remove("end");
            Save();

            cfg = Load();
            var step2 = DictSnapshot(cfg);
            Assert.That(step2.Keys.OrderBy(k => k),
                Is.EqualTo(new[] { "mid", "start" }));

            // Step 3: add end back (now 99), keep others as-is
            cfg.NamedValues.Dictionary["end"] = 99;
            Save();

            cfg = Load();
            var finalDict = DictSnapshot(cfg);

            var expected = new Dictionary<string, int>
            {
                { "start", 2 },
                { "mid", 5 },
                { "end", 99 }
            };

            Assert.Multiple(() =>
            {
                Assert.That(finalDict, Has.Count.EqualTo(3));
                Assert.That(finalDict.Keys.OrderBy(k => k),
                    Is.EqualTo(expected.Keys.OrderBy(k => k)));
                foreach (var kv in expected)
                {
                    Assert.That(finalDict[kv.Key], Is.EqualTo(kv.Value));
                }
            });

            // Extra safety: another reload must not change anything
            var cfg2 = Load();
            var finalDict2 = DictSnapshot(cfg2);
            Assert.That(finalDict2, Is.EqualTo(finalDict));
        }

        // --------------------------------------------------------------
        // 6) Mixed changes on Tags + NamedValues:
        //    ensure both structures can be modified independently and
        //    reloaded without cross-talk or duplication.
        // --------------------------------------------------------------

        [Test]
        public void Mixed_Tags_And_NamedValues_Changes_Remain_Consistent_After_Reload()
        {
            var cfg = Load();

            // Initial sanity
            var tags0 = cfg.Tags == null ? new List<string>() : new List<string>(cfg.Tags);
            var dict0 = DictSnapshot(cfg);

            Assert.Multiple(() =>
            {
                Assert.That(tags0, Is.EqualTo(new[] { "alpha", "beta" }));
                Assert.That(dict0["start"], Is.EqualTo(1));
                Assert.That(dict0["end"], Is.EqualTo(10));
            });

            // Modify both:
            // Tags: remove "alpha", add "gamma"
            Assert.That(cfg.Tags, Is.Not.Null);
            cfg.Tags.Remove("alpha");
            cfg.Tags.Add("gamma");

            // NamedValues: change start to 3, add mid=7
            cfg.NamedValues.Dictionary["start"] = 3;
            cfg.NamedValues.Dictionary["mid"] = 7;

            Save();

            // Reload
            var reloaded = Load();
            var tags = reloaded.Tags == null ? new List<string>() : new List<string>(reloaded.Tags);
            var dict = DictSnapshot(reloaded);

            Assert.Multiple(() =>
            {
                // Tags should now be [beta, gamma] in some order, but no alpha
                Assert.That(tags, Does.Not.Contain("alpha"));
                Assert.That(tags, Does.Contain("beta"));
                Assert.That(tags, Does.Contain("gamma"));
                Assert.That(tags.Distinct().Count(), Is.EqualTo(tags.Count));

                // NamedValues should reflect the edits
                Assert.That(dict["start"], Is.EqualTo(3));
                Assert.That(dict["mid"], Is.EqualTo(7));
                Assert.That(dict["end"], Is.EqualTo(10));
            });

            // Another reload must not alter anything
            var reloaded2 = Load();
            var tags2 = reloaded2.Tags == null ? new List<string>() : new List<string>(reloaded2.Tags);
            var dict2 = DictSnapshot(reloaded2);

            Assert.Multiple(() =>
            {
                Assert.That(new HashSet<string>(tags2), Is.EqualTo(new HashSet<string>(tags)));
                Assert.That(dict2, Is.EqualTo(dict));
            });
        }
    }
}
