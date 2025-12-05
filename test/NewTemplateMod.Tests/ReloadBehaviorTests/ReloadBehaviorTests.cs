using NUnit.Framework;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.Layout;
using mz.Config.Abstractions.SE;
using mz.Config.Core;
using mz.Config.Core.Converter;
using mz.Config.Core.Layout;
using mz.Config.Core.Storage;
using mz.Config.Domain;

namespace NewTemplateMod.Tests.ReloadBehaviorTests
{
    [TestFixture]
    public class ReloadBehaviorTests
    {
        // wired by SetUp into InternalConfigStorage
        private IConfigXmlSerializer _xml;
        private IXmlConverter _tomlConverter;
        private IConfigFileSystem _fileSystem = new FakeFileSystem();
        private IConfigLayoutMigrator _layoutMigrator;

        [SetUp]
        public void SetUp()
        {
            // Guard so we don't try to re-initialize InternalConfigStorage every test run.
            if (InternalConfigStorage.IsInitialized)
                return;

            _xml = new TestXmlSerializer();          // your existing test serializer
            _tomlConverter = new TomlXmlConverter(); // your TOML<->XML converter
            _layoutMigrator = new ConfigLayoutMigrator();

            ConfigStorage.Debug = new Debug();

            // Force fake filesystem into the core storage
            InternalConfigStorage.Initialize(_fileSystem, _xml, _layoutMigrator, _tomlConverter);
        }

        // --------------------------------------------------------------------
        // SimpleConfig
        // --------------------------------------------------------------------

        [Test]
        public void SimpleConfig_Reload_Behavior_Is_Correct()
        {
            // 1) First load: file missing -> default + file created
            var cfg1 = LoadConfig<SimpleConfig>();
            Assert.That(cfg1, Is.Not.Null);
            Assert.That(cfg1.SomeValue, Is.EqualTo(42));
            Assert.That(cfg1.SomeText, Is.EqualTo("Default text"));

            var fileName = GetFileNameFor<SimpleConfig>();
            Assert.That(_fileSystem.Exists(ConfigLocationType.World, fileName), Is.True,
                "Config file should be created on first load.");

            // 2) Corrupt file in invalid way (it needs to fail at xml deserialization)
            var broken = @"[SimpleConfig]
ConfigVersion = ""0.1.0""
SomeValue = svr # should be an integer
SomeText = ""Edited text""123 # invalid TOML";
            SetFileContent<SimpleConfig>(broken);

            // Reload: expect it to fallback to defaults and recreate a valid file
            var cfg2 = LoadConfig<SimpleConfig>();
            Assert.That(cfg2.SomeValue, Is.EqualTo(42));
            Assert.That(cfg2.SomeText, Is.EqualTo("Default text"));

            Assert.That(
                _fileSystem.Exists(ConfigLocationType.World, fileName.Replace(".toml", ".bak.toml")),
                Is.True,
                "Backup file must be created on invalid content."
            );
            var backupContent = GetFileContent<SimpleConfig>(backup: true);
            Assert.That(backupContent, Is.EqualTo(broken),
                "Invalid content must have been saved to backup file.");

            var afterInvalidContent = GetFileContent<SimpleConfig>();
            Assert.That(afterInvalidContent, Is.Not.EqualTo(broken),
                "Invalid content must have been replaced by a valid default config.");

            // 3) Valid edit: change all fields
            var validEdited = @"[SimpleConfig]
ConfigVersion = ""0.1.0""
SomeValue = 1337
SomeText = ""Edited text""";

            SetFileContent<SimpleConfig>(validEdited);

            var cfg3 = LoadConfig<SimpleConfig>();
            Assert.That(cfg3.SomeValue, Is.EqualTo(1337));
            Assert.That(cfg3.SomeText, Is.EqualTo("Edited text"));

            var finalContent = GetFileContent<SimpleConfig>();
            Assert.That(finalContent.TrimEnd(), Is.EqualTo(validEdited),
                "Valid file must not be overwritten on successful load.");
        }

        // --------------------------------------------------------------------
        // IntermediateConfig
        // --------------------------------------------------------------------

        [Test]
        public void IntermediateConfig_Reload_Behavior_Is_Correct()
        {
            // 1) First load
            var cfg1 = LoadConfig<IntermediateConfig>();
            
            Assert.That(cfg1.IsEnabled, Is.True);
            Assert.That(cfg1.OptionalValue, Is.Null);
            Assert.That(cfg1.CurrentMode, Is.EqualTo(IntermediateConfig.Mode.Basic));

            var fileName = GetFileNameFor<IntermediateConfig>();
            Assert.That(_fileSystem.Exists(ConfigLocationType.World, fileName), Is.True);


            // 2) Corrupt in a way that should be considered invalid
            // - invalid bool ("maybe")
            // - invalid nullable int ("foo")
            // - invalid enum ("INVALID") which should blow up XmlSerializer on deserialize
            var broken = @"[IntermediateConfig]
ConfigVersion = ""0.2.0""
IsEnabled = maybe
OptionalValue = foo
CurrentMode = ""INVALID""";
            SetFileContent<IntermediateConfig>(broken);

            var cfg2 = LoadConfig<IntermediateConfig>();
            Assert.That(cfg2.IsEnabled, Is.True);
            Assert.That(cfg2.OptionalValue, Is.Null);
            Assert.That(cfg2.CurrentMode, Is.EqualTo(IntermediateConfig.Mode.Basic));

            Assert.That(
                _fileSystem.Exists(ConfigLocationType.World, fileName.Replace(".toml", ".bak.toml")),
                Is.True,
                "Backup file must be created on invalid IntermediateConfig content."
            );
            var backupContent = GetFileContent<IntermediateConfig>(backup: true);
            Assert.That(backupContent, Is.EqualTo(broken),
                "Invalid IntermediateConfig content must have been saved to backup file.");

            var afterInvalid = GetFileContent<IntermediateConfig>();
            Assert.That(afterInvalid, Is.Not.EqualTo(broken),
                "Invalid IntermediateConfig content must have been replaced.");

            // 3) Valid edit
            var validEdited = @"[IntermediateConfig]
ConfigVersion = ""0.2.0""
IsEnabled = false
OptionalValue = 123
CurrentMode = ""Expert""";

            SetFileContent<IntermediateConfig>(validEdited);

            var cfg3 = LoadConfig<IntermediateConfig>();
            Assert.That(cfg3.IsEnabled, Is.False);
            Assert.That(cfg3.OptionalValue, Is.EqualTo(123));
            Assert.That(cfg3.CurrentMode, Is.EqualTo(IntermediateConfig.Mode.Expert));

            var finalContent = GetFileContent<IntermediateConfig>();
            Assert.That(finalContent.TrimEnd(), Is.EqualTo(validEdited));
        }

        // --------------------------------------------------------------------
        // CollectionConfig
        // --------------------------------------------------------------------

        [Test]
        public void CollectionConfig_Reload_Behavior_Is_Correct()
        {
            // 1) First load
            var cfg1 = LoadConfig<CollectionConfig>();

            Assert.That(cfg1.Tags, Is.Not.Null);
            Assert.That(cfg1.Tags, Is.EquivalentTo(new[] { "alpha", "beta" }));
            Assert.That(cfg1.NamedValues, Is.Not.Null);
            Assert.That(cfg1.NamedValues.Dictionary["start"], Is.EqualTo(1));
            Assert.That(cfg1.NamedValues.Dictionary["end"], Is.EqualTo(10));
            Assert.That(cfg1.Nested.Threshold, Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(cfg1.Nested.Allowed, Is.True);

            var fileName = GetFileNameFor<CollectionConfig>();
            Assert.That(_fileSystem.Exists(ConfigLocationType.World, fileName), Is.True);

            var firstValid = GetFileContent<CollectionConfig>();

            // 2) Corrupt invalidly
            var broken = "garbage!";
            SetFileContent<CollectionConfig>(broken);

            var cfg2 = LoadConfig<CollectionConfig>();

            Assert.That(cfg2.Tags, Is.EquivalentTo(new[] { "alpha", "beta" }));
            Assert.That(cfg2.NamedValues.Dictionary["start"], Is.EqualTo(1));
            Assert.That(cfg2.NamedValues.Dictionary["end"], Is.EqualTo(10));
            Assert.That(cfg2.Nested.Threshold, Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(cfg2.Nested.Allowed, Is.True);

            Assert.That(
                _fileSystem.Exists(ConfigLocationType.World, fileName.Replace(".toml", ".bak.toml")),
                Is.True,
                "Backup file must be created on invalid CollectionConfig content."
            );
            var backupContent = GetFileContent<CollectionConfig>(backup: true);
            Assert.That(backupContent, Is.EqualTo(broken),
                "Invalid CollectionConfig content must have been saved to backup file.");

            var afterInvalid = GetFileContent<CollectionConfig>();
            Assert.That(afterInvalid, Is.Not.EqualTo(broken));

            // 3) Valid edit: flip everything
            var validEdited = @"[CollectionConfig]
ConfigVersion = ""0.3.0""
Tags.string = [""x"", ""y"", ""z""]

[CollectionConfig.NamedValues-dictionary]
""start"" = 5
""end"" = 99

[CollectionConfig.Nested]
Threshold = 0.9
Allowed = false";

            SetFileContent<CollectionConfig>(validEdited);

            var cfg3 = LoadConfig<CollectionConfig>();

            Assert.That(cfg3.Tags, Is.EquivalentTo(new[] { "x", "y", "z" }));
            Assert.That(cfg3.NamedValues.Dictionary["start"], Is.EqualTo(5));
            Assert.That(cfg3.NamedValues.Dictionary["end"], Is.EqualTo(99));
            Assert.That(cfg3.Nested.Threshold, Is.EqualTo(0.9f).Within(1e-6f));
            Assert.That(cfg3.Nested.Allowed, Is.False);

            var finalContent = GetFileContent<CollectionConfig>();
            Assert.That(finalContent.TrimEnd(), Is.EqualTo(validEdited));
        }

        // --------------------------------------------------------------------
        // AdvancedConfig
        // --------------------------------------------------------------------

        [Test]
        public void AdvancedConfig_Reload_Behavior_Is_Correct()
        {
            // 1) First load
            var cfg1 = LoadConfig<AdvancedConfig>();

            Assert.That(cfg1.Settings, Is.Not.Null);
            Assert.That(cfg1.Settings.Display.Width, Is.EqualTo(1920));
            Assert.That(cfg1.Settings.Display.Height, Is.EqualTo(1080));
            Assert.That(cfg1.Settings.Display.Theme, Is.EqualTo("Dark"));
            Assert.That(cfg1.Settings.Display.Dpi, Is.Null);
            Assert.That(cfg1.Settings.Network, Is.Null);

            var fileName = GetFileNameFor<AdvancedConfig>();
            Assert.That(_fileSystem.Exists(ConfigLocationType.World, fileName), Is.True);

            var firstValid = GetFileContent<AdvancedConfig>();

            // 2) Corrupt
            var broken = @"[AdvancedConfig]
ConfigVersion = ""0.5.0""
Settings.Display = ""<broken>""";
            SetFileContent<AdvancedConfig>(broken);

            var cfg2 = LoadConfig<AdvancedConfig>();

            Assert.That(cfg2.Settings.Display.Width, Is.EqualTo(1920));
            Assert.That(cfg2.Settings.Display.Height, Is.EqualTo(1080));
            Assert.That(cfg2.Settings.Display.Theme, Is.EqualTo("Dark"));
            Assert.That(cfg2.Settings.Display.Dpi, Is.Null);
            Assert.That(cfg2.Settings.Network, Is.Null);

            Assert.That(
                _fileSystem.Exists(ConfigLocationType.World, fileName.Replace(".toml", ".bak.toml")),
                Is.True,
                "Backup file must be created on invalid AdvancedConfig content."
            );
            var backupContent = GetFileContent<AdvancedConfig>(backup: true);
            Assert.That(backupContent, Is.EqualTo(broken),
                "Invalid AdvancedConfig content must have been saved to backup file.");

            var afterInvalid = GetFileContent<AdvancedConfig>();
            Assert.That(afterInvalid, Is.Not.EqualTo(broken));

            // 3) Valid edit: change everything and add Network
            var validEdited = @"[AdvancedConfig]
ConfigVersion = ""0.5.0""

[AdvancedConfig.Settings.Display]
Width = 1280
Height = 720
Theme = ""Light""
Dpi = 96

[AdvancedConfig.Settings.Network]
Host = ""example.com""
Port = 443
UseTls = true";

            SetFileContent<AdvancedConfig>(validEdited);

            var cfg3 = LoadConfig<AdvancedConfig>();

            Assert.That(cfg3.Settings.Display.Width, Is.EqualTo(1280));
            Assert.That(cfg3.Settings.Display.Height, Is.EqualTo(720));
            Assert.That(cfg3.Settings.Display.Theme, Is.EqualTo("Light"));
            Assert.That(cfg3.Settings.Display.Dpi.HasValue, Is.True);
            Assert.That(cfg3.Settings.Display.Dpi.Value, Is.EqualTo(96.0f).Within(1e-6f));

            Assert.That(cfg3.Settings.Network, Is.Not.Null);
            Assert.That(cfg3.Settings.Network.Host, Is.EqualTo("example.com"));
            Assert.That(cfg3.Settings.Network.Port, Is.EqualTo(443));
            Assert.That(cfg3.Settings.Network.UseTls, Is.True);

            var finalContent = GetFileContent<AdvancedConfig>();
            Assert.That(finalContent.TrimEnd(), Is.EqualTo(validEdited));
        }

        // --------------------------------------------------------------------
        // KeybindConfig
        // --------------------------------------------------------------------

        [Test]
        public void KeybindConfig_Reload_Behavior_Is_Correct()
        {
            // 1) First load
            var cfg1 = LoadConfig<KeybindConfig>();

            Assert.That(cfg1.Keybinds.Select, Is.Not.Null);
            Assert.That(cfg1.Keybinds.Select.Modifier, Is.Null);
            Assert.That(cfg1.Keybinds.Select.Action, Is.EqualTo(MyKeys.Control));
            Assert.That(cfg1.Keybinds.Select.Toggle, Is.False);

            Assert.That(cfg1.Keybinds.Throw, Is.Not.Null);
            Assert.That(cfg1.Keybinds.Throw.Modifier, Is.EqualTo(MyKeys.LeftAlt));
            Assert.That(cfg1.Keybinds.Throw.Action, Is.EqualTo(MyKeys.RightButton));
            Assert.That(cfg1.Keybinds.Throw.Toggle, Is.True);

            Assert.That(cfg1.Keybinds.OpenMenu, Is.Null);

            var fileName = GetFileNameFor<KeybindConfig>();
            Assert.That(_fileSystem.Exists(ConfigLocationType.World, fileName), Is.True);

            var firstValid = GetFileContent<KeybindConfig>();

            // 2) Corrupt invalidly
            var broken = "[KeybindConfig]\nTHIS_IS = \"invalid\"";
            SetFileContent<KeybindConfig>(broken);

            var cfg2 = LoadConfig<KeybindConfig>();

            Assert.That(cfg2.Keybinds.Select.Modifier, Is.Null);
            Assert.That(cfg2.Keybinds.Select.Action, Is.EqualTo(MyKeys.Control));
            Assert.That(cfg2.Keybinds.Select.Toggle, Is.False);

            Assert.That(cfg2.Keybinds.Throw.Modifier, Is.EqualTo(MyKeys.LeftAlt));
            Assert.That(cfg2.Keybinds.Throw.Action, Is.EqualTo(MyKeys.RightButton));
            Assert.That(cfg2.Keybinds.Throw.Toggle, Is.True);

            Assert.That(cfg2.Keybinds.OpenMenu, Is.Null);

            Assert.That(
                _fileSystem.Exists(ConfigLocationType.World, fileName.Replace(".toml", ".bak.toml")),
                Is.True,
                "Backup file must be created on invalid KeybindConfig content."
            );
            var backupContent = GetFileContent<KeybindConfig>(backup: true);
            Assert.That(backupContent, Is.EqualTo(broken),
                "Invalid KeybindConfig content must have been saved to backup file.");

            var afterInvalid = GetFileContent<KeybindConfig>();
            Assert.That(afterInvalid, Is.Not.EqualTo(broken));

            // 3) Valid edit: flip all values and make OpenMenu non-null
            var validEdited = @"[KeybindConfig]
ConfigVersion = ""1.0.0""

[KeybindConfig.Keybinds.Select]
Modifier = ""LeftAlt""
Action = ""LeftButton""
Toggle = true

[KeybindConfig.Keybinds.Throw]
Modifier = null
Action = ""RightButton""
Toggle = false

[KeybindConfig.Keybinds.OpenMenu]
Modifier = ""Control""
Action = ""LeftButton""
Toggle = false";

            SetFileContent<KeybindConfig>(validEdited);

            var cfg3 = LoadConfig<KeybindConfig>();

            Assert.That(cfg3.Keybinds.Select.Modifier, Is.EqualTo(MyKeys.LeftAlt));
            Assert.That(cfg3.Keybinds.Select.Action, Is.EqualTo(MyKeys.LeftButton));
            Assert.That(cfg3.Keybinds.Select.Toggle, Is.True);

            Assert.That(cfg3.Keybinds.Throw.Modifier, Is.Null);
            Assert.That(cfg3.Keybinds.Throw.Action, Is.EqualTo(MyKeys.RightButton));
            Assert.That(cfg3.Keybinds.Throw.Toggle, Is.False);

            Assert.That(cfg3.Keybinds.OpenMenu, Is.Not.Null);
            Assert.That(cfg3.Keybinds.OpenMenu.Modifier, Is.EqualTo(MyKeys.Control));
            Assert.That(cfg3.Keybinds.OpenMenu.Action, Is.EqualTo(MyKeys.LeftButton));
            Assert.That(cfg3.Keybinds.OpenMenu.Toggle, Is.False);

            var finalContent = GetFileContent<KeybindConfig>();
            Assert.That(finalContent.TrimEnd(), Is.EqualTo(validEdited));
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private static TConfig LoadConfig<TConfig>()
            where TConfig : ConfigBase, new()
        {
            // World is arbitrary here; adjust if you want Local/Global behavior instead.
            return ConfigStorage.Load<TConfig>(ConfigLocationType.World);
        }

        private static string GetFileNameFor<TConfig>()
            where TConfig : ConfigBase, new()
        {
            var typeName = typeof(TConfig).Name;
            // Uses the exact name that InternalConfigStorage itself considers "current"
            return InternalConfigStorage.GetCurrentFileName(ConfigLocationType.World, typeName);
        }

        private string GetFileContent<TConfig>(bool backup = false)
            where TConfig : ConfigBase, new()
        {
            var fileName = GetFileNameFor<TConfig>();
            if (backup)
            {
                fileName = fileName.Replace(".toml", ".bak.toml");
            }

            string content;
            var found = _fileSystem.TryReadFile(ConfigLocationType.World, fileName, out content);
            Assert.That(found, Is.True, "Expected file to exist: " + fileName);
            return content ?? string.Empty;
        }

        private void SetFileContent<TConfig>(string content)
            where TConfig : ConfigBase, new()
        {
            var fileName = GetFileNameFor<TConfig>();
            _fileSystem.WriteFile(ConfigLocationType.World, fileName, content);
        }
    }
}
