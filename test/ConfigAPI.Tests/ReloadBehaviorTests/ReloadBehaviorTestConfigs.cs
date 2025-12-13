using System.Collections.Generic;
using System.Xml.Serialization;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Serialization;

namespace NewTemplateMod.Tests.ReloadBehaviorTests
{
    // NOTE:
    // These are copies of your configs for test-specific scenarios.
    // If you already have them in another assembly, you can remove these
    // and just reference the real types instead.

    public class SimpleConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";

        public int SomeValue = 42;
        public string SomeText { get; set; } = "Default text";
    }

    public class IntermediateConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.2.0";

        public bool IsEnabled { get; set; } = true;

        public int? OptionalValue { get; set; } = null;

        public Mode CurrentMode { get; set; } = Mode.Basic;

        public enum Mode
        {
            Basic,
            Advanced,
            Expert
        }
    }

    // public class SerializableDictionary<TKey, TValue>
    // {
    //     // Minimal shape to make tests compile if your real type is elsewhere.
    //     [XmlElement("dictionary")]
    //     public Dictionary<TKey, TValue> Dictionary { get; set; } = new Dictionary<TKey, TValue>();
    // }

    public class CollectionConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.3.0";

        public List<string> Tags { get; set; }

        public SerializableDictionary<string, int> NamedValues { get; set; }

        public SubConfig Nested { get; set; } = new SubConfig();

        public class SubConfig
        {
            public float Threshold { get; set; } = 0.75f;
            public bool Allowed { get; set; } = true;
        }

        public override void ApplyDefaults()
        {
            Tags = new List<string> { "alpha", "beta" };
            NamedValues = new SerializableDictionary<string, int>
            {
                Dictionary = new Dictionary<string, int>
                {
                    { "start", 1 },
                    { "end", 10 }
                }
            };
        }
    }

    public class AdvancedConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.5.0";

        public SettingsRoot Settings { get; set; } = new SettingsRoot();

        public class SettingsRoot
        {
            public DisplayConfig Display { get; set; } = new DisplayConfig();

            [XmlElement(IsNullable = true)]
            public NetworkConfig Network { get; set; } = null;
        }

        public class DisplayConfig
        {
            public int Width { get; set; } = 1920;
            public int Height { get; set; } = 1080;
            public string Theme { get; set; } = "Dark";
            public float? Dpi { get; set; } = null; // stress-test nullables
        }

        public class NetworkConfig
        {
            public string Host { get; set; } = "localhost";
            public int Port { get; set; } = 8080;

            public bool UseTls { get; set; } = true;
        }
    }

    public enum MyKeys
    {
        LeftButton,
        RightButton,
        Control,
        LeftAlt,
        // add your real keys here
    }

    public class MyKeybind
    {
        public MyKeys? Modifier { get; set; } = null;
        public MyKeys Action { get; set; } = MyKeys.LeftButton;
        public bool Toggle { get; set; } = false;
    }

    public class KeybindConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public KeybindsConfig Keybinds { get; set; } = new KeybindsConfig();

        public class KeybindsConfig
        {
            public MyKeybind Select { get; set; } = new MyKeybind
            {
                Modifier = null,
                Action = MyKeys.Control,
                Toggle = false
            };

            public MyKeybind Throw { get; set; } = new MyKeybind
            {
                Modifier = MyKeys.LeftAlt,
                Action = MyKeys.RightButton,
                Toggle = true
            };

            [XmlElement(IsNullable = true)]
            public MyKeybind OpenMenu { get; set; } = null;
        }
    }
}