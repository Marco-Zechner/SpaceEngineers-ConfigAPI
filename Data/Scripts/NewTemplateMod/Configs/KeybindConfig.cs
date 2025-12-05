using System.Xml.Serialization;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Input;

namespace mz.NewTemplateMod
{
    public class KeybindConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";
        
        public KeybindsConfig Keybinds { get; set; } = new KeybindsConfig();

        public class KeybindsConfig
        {
            public MyKeybind Select { get; set; } = new MyKeybind
            {
                Modifier = null,
                Action   = MyKeys.Control,
                Toggle   = false
            };

            public MyKeybind Throw { get; set; } = new MyKeybind
            {
                Modifier = MyKeys.LeftAlt,
                Action   = MyKeys.RightButton,
                Toggle   = true
            };
                
            [XmlElement(IsNullable = true)]
            public MyKeybind OpenMenu { get; set; } = null;
            // etc...
        }
    }

    public class MyKeybind
    {
        public MyKeys? Modifier { get; set; } = null;
        public MyKeys  Action   { get; set; } = MyKeys.LeftButton;
        public bool    Toggle   { get; set; } = false;
    }
}