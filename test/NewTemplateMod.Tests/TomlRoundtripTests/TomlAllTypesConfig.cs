using System.Collections.Generic;
using System.Xml.Serialization;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Serialization;

namespace NewTemplateMod.Tests.TomlRoundtripTests
{
    /// <summary>
    /// Config used to test TOML roundtrip for:
    /// - primitives (int, double, float, bool, string)
    /// - nullable types
    /// - lists
    /// - SerializableDictionary
    /// - nested object
    /// </summary>
    public class TomlAllTypesConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        // --- primitives ---
        public int IntValue { get; set; } = 123;
        public double DoubleValue { get; set; } = 4.5;
        public float FloatValue { get; set; } = 0.75f;
        public bool BoolValue { get; set; } = true;
        public string Text { get; set; } = "Hello";

        // --- nullable ---
        public int? OptionalInt { get; set; } = null;
        public float? OptionalFloat { get; set; } = null;
        [XmlElement(IsNullable = true)]
        public string OptionalText { get; set; } = null;

        // --- collections ---
        public List<int> IntList { get; set; }
        public List<string> StringList { get; set; }

        // --- dictionary ---
        public SerializableDictionary<string, int> NamedValues { get; set; }

        // --- nested object ---
        public NestedConfig Nested { get; set; } = new NestedConfig();

        public class NestedConfig
        {
            public float Threshold { get; set; } = 0.25f;
            public bool Flag { get; set; } = false;
        }

        /// <summary>
        /// Use ApplyDefaults to safely initialize complex members
        /// so XmlSerializer + migrator + TOML converter all see
        /// stable default values.
        /// </summary>
        public override void ApplyDefaults()
        {
            IntList = new List<int> { 1, 2, 3 };
            StringList = new List<string> { "a", "b" };
            NamedValues = new SerializableDictionary<string, int>
            {
                Dictionary = new Dictionary<string, int>
                {
                    ["start"] = 1,
                    ["end"] = 10
                }
            };
            Nested = new NestedConfig();
        }

        /// <summary>
        /// Optional: describe variables so TOML converter can emit comments.
        /// This is where you can encode "# type" info.
        /// </summary>
        public override IReadOnlyDictionary<string, string> VariableDescriptions
        {
            get
            {
                var map = new Dictionary<string, string>
                {
                    ["IntValue"] = "int",
                    ["DoubleValue"] = "double",
                    ["FloatValue"] = "float",
                    ["BoolValue"] = "bool",
                    ["Text"] = "string",
                    ["OptionalInt"] = "nullable int",
                    ["OptionalFloat"] = "nullable float",
                    ["OptionalText"] = "nullable string",
                    ["IntList"] = "List<int>",
                    ["StringList"] = "List<string>",
                    ["NamedValues"] = "Dictionary<string,int>",
                    ["Nested.Threshold"] = "float (0..1)",
                    ["Nested.Flag"] = "bool"
                };
                return map;
            }
        }
    }
}
