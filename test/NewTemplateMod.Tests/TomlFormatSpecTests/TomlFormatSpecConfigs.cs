using System.Collections.Generic;
using mz.Config.Domain;
using mz.SemanticVersioning;
using VRage.Serialization;

namespace NewTemplateMod.Tests.TomlFormatSpecTests
{
    /// <summary>
    /// Config #1: only scalars + nullable scalars.
    /// Baseline for root [TypeName] table with simple "Key = value" lines.
    /// </summary>
    public class TomlConfig1Scalars : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public int IntValue { get; set; } = 123;
        public double DoubleValue { get; set; } = 4.5;
        public float FloatValue { get; set; } = 0.75f;
        public bool BoolValue { get; set; } = true;
        public string Text { get; set; } = "Hello";

        public int? OptionalInt { get; set; } = null;
        public float? OptionalFloat { get; set; } = null;
        public string OptionalText { get; set; } = null;

        public override IReadOnlyDictionary<string, string> VariableDescriptions
        {
            get
            {
                var map = new Dictionary<string, string>
                {
                    { "IntValue", "int" },
                    { "DoubleValue", "double" },
                    { "FloatValue", "float" },
                    { "BoolValue", "bool" },
                    { "Text", "string" },
                    { "OptionalInt", "nullable int" },
                    { "OptionalFloat", "nullable float" },
                    { "OptionalText", "nullable string" }
                };
                return map;
            }
        }
    }

    /// <summary>
    /// Config #2: scalars + primitive lists.
    /// Builds on Config1 behavior and adds:
    ///   IntList.int       = [...]
    ///   StringList.string = [...]
    /// </summary>
    public class TomlConfig2Lists : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public int IntValue { get; set; } = 123;
        public string Text { get; set; } = "Hello";

        public List<int> IntList { get; set; }
        public List<string> StringList { get; set; }

        public override void ApplyDefaults()
        {
            IntList = new List<int> { 1, 2, 3 };
            StringList = new List<string> { "alpha", "beta" };
        }

        public override IReadOnlyDictionary<string, string> VariableDescriptions
        {
            get
            {
                var map = new Dictionary<string, string>
                {
                    { "IntValue", "int" },
                    { "Text", "string" },
                    { "IntList", "List<int>" },
                    { "StringList", "List<string>" }
                };
                return map;
            }
        }
    }

    /// <summary>
    /// Config #3: scalars + lists + dictionary.
    /// Builds on Config2 behavior and adds:
    ///   [TomlConfig3_Dictionary.NamedValues-dictionary]
    ///   "key" = value
    /// </summary>
    public class TomlConfig3Dictionary : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public int IntValue { get; set; } = 123;
        public string Text { get; set; } = "Hello";

        public List<int> IntList { get; set; }
        public List<string> StringList { get; set; }

        public SerializableDictionary<string, int> NamedValues { get; set; }

        public override void ApplyDefaults()
        {
            IntList = new List<int> { 1, 2, 3 };
            StringList = new List<string> { "alpha", "beta" };

            NamedValues = new SerializableDictionary<string, int>
            {
                Dictionary = new Dictionary<string, int>
                {
                    { "start", 1 },
                    { "end", 99 }
                }
            };
        }

        public override IReadOnlyDictionary<string, string> VariableDescriptions
        {
            get
            {
                var map = new Dictionary<string, string>
                {
                    { "IntValue", "int" },
                    { "Text", "string" },
                    { "IntList", "List<int>" },
                    { "StringList", "List<string>" },
                    { "NamedValues", "Dictionary<string,int>" }
                };
                return map;
            }
        }
    }

    /// <summary>
    /// Config #4: scalars + lists + dictionary + nested + trailing scalar.
    /// Builds on Config3 and adds:
    ///
    ///   [TypeName.Nested]
    ///   Threshold = ...
    ///   Flag      = ...
    ///   [TypeName.Nested-end]
    ///   FloatValue = ...
    ///
    /// to exercise the nested-block + end-marker pattern and a trailing scalar.
    /// </summary>
    public class TomlConfig4Nested : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        // Order is important: FloatValue comes AFTER Nested so we can test that it
        // is written after [TypeName.Nested] ... [TypeName.Nested-end].
        public int IntValue { get; set; } = 123;
        public string Text { get; set; } = "Hello";

        public List<int> IntList { get; set; }
        public List<string> StringList { get; set; }

        public SerializableDictionary<string, int> NamedValues { get; set; }

        public NestedConfig Nested { get; set; }

        public float FloatValue { get; set; } = 0.75f;

        public class NestedConfig
        {
            public float Threshold { get; set; } = 0.9f;
            public bool Flag { get; set; } = true;
        }

        public override void ApplyDefaults()
        {
            IntList = new List<int> { 1, 2, 3 };
            StringList = new List<string> { "alpha", "beta" };

            NamedValues = new SerializableDictionary<string, int>
            {
                Dictionary = new Dictionary<string, int>
                {
                    { "start", 1 },
                    { "end", 99 }
                }
            };

            Nested = new NestedConfig
            {
                Threshold = 0.9f,
                Flag = true
            };
        }

        public override IReadOnlyDictionary<string, string> VariableDescriptions
        {
            get
            {
                var map = new Dictionary<string, string>
                {
                    { "IntValue", "int" },
                    { "Text", "string" },
                    { "IntList", "List<int>" },
                    { "StringList", "List<string>" },
                    { "NamedValues", "Dictionary<string,int>" },
                    { "Nested.Threshold", "float (0..1)" },
                    { "Nested.Flag", "bool" },
                    { "FloatValue", "float" }
                };
                return map;
            }
        }
    }
}
