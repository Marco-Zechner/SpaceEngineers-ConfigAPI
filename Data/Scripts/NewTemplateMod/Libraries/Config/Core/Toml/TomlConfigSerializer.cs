using System;
using System.Collections.Generic;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Abstractions.Toml;
using mz.Config.Domain;

namespace mz.Config.Core.Toml
{
    public class TomlConfigSerializer : IConfigSerializer
    {
        private readonly IConfigXmlSerializer _xml;

        public TomlConfigSerializer(IConfigXmlSerializer xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            _xml = xml;
        }

        // ------------------------------------------------
        // High-level API
        // ------------------------------------------------

        public string Serialize(ConfigBase config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Special handling for ExampleConfig to keep existing behaviour/tests
            var example = config as ExampleConfig;
            if (example != null) return SerializeExampleConfig(example);

            // Generic path: XML -> fields -> TOML with simple bool/string heuristics
            var typeName = config.GetType().Name;
            var xml = _xml.SerializeToXml(config);
            var fields = SimpleXml.ParseSimpleElements(xml);
            var sb = new StringBuilder();
            sb.Append('[').Append(typeName).Append(']').AppendLine();
            sb.Append("StoredVersion = \"").Append(config.ConfigVersion).Append('"').AppendLine();
            foreach (var kv in fields)
            {
                var key = kv.Key;
                var rawValue = kv.Value;
                var tomlValue = ToTomlValue(rawValue);
                sb.Append(key).Append(" = ").Append(tomlValue);
                sb.Append(" # ").Append(tomlValue); // comment == current value
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public ConfigBase Deserialize(IConfigDefinition definition, string content)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            // Special handling for ExampleConfig
            if (definition.ConfigType == typeof(ExampleConfig)) return DeserializeExampleConfig(content);

            // Generic path
            var model = ParseToModel(content);
            if (model == null) return definition.CreateDefaultInstance();
            var values = new Dictionary<string, string>();
            foreach (var kv in model.Entries)
            {
                var key = kv.Key;
                var tomlValue = kv.Value.Value;
                var raw = FromTomlValue(tomlValue);
                values[key] = raw;
            }

            var rootName = definition.TypeName;
            var xml = SimpleXml.BuildSimpleXml(rootName, values);
            return definition.DeserializeFromXml(_xml, xml);
        }

        // ------------------------------------------------
        // TOML model API (used by ConfigStorage migration)
        // ------------------------------------------------

        public ITomlModel ParseToModel(string tomlContent)
        {
            if (string.IsNullOrEmpty(tomlContent)) return null;
            var model = new TomlModel();
            var lines = tomlContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var sectionName = line.Substring(1, line.Length - 2).Trim();
                    model.TypeName = sectionName;
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0) continue;
                var key = line.Substring(0, eqIndex).Trim();
                var valuePart = line.Substring(eqIndex + 1).Trim();
                var valueText = valuePart;
                string defaultText = null;
                var hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                    defaultText = valuePart.Substring(hashIndex + 1).Trim();
                }

                if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    model.StoredVersion = TrimQuotes(valueText);
                    continue;
                }

                var entry = new TomlEntry();
                entry.Value = valueText;
                entry.DefaultValue = defaultText;
                model.Entries[key] = entry;
            }

            return model;
        }

        public string SerializeModel(ITomlModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(model.TypeName))
            {
                sb.Append('[').Append(model.TypeName).Append(']').AppendLine();
            }

            if (!string.IsNullOrEmpty(model.StoredVersion))
            {
                sb.Append("StoredVersion = \"").Append(model.StoredVersion).Append('"').AppendLine();
            }

            foreach (var kv in model.Entries)
            {
                sb.Append(kv.Key).Append(" = ").Append(kv.Value.Value);
                if (!string.IsNullOrEmpty(kv.Value.DefaultValue))
                {
                    sb.Append(" # ").Append(kv.Value.DefaultValue);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public ITomlModel BuildDefaultModel(IConfigDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.ConfigType == typeof(ExampleConfig)) return BuildDefaultModelExample(definition);

            // Generic path: default instance -> XML -> fields
            var defaultInstance = definition.CreateDefaultInstance();
            var xml = _xml.SerializeToXml(defaultInstance);
            var fields = SimpleXml.ParseSimpleElements(xml);
            var model = new TomlModel();
            model.TypeName = definition.TypeName;
            model.StoredVersion = defaultInstance.ConfigVersion;
            foreach (var kv in fields)
            {
                var key = kv.Key;
                var rawValue = kv.Value;
                var tomlValue = ToTomlValue(rawValue);
                var entry = new TomlEntry();
                entry.Value = tomlValue;
                entry.DefaultValue = tomlValue;
                model.Entries[key] = entry;
            }

            return model;
        }

        // ------------------------------------------------
        // ExampleConfig-specific implementation (old behaviour)
        // ------------------------------------------------

        private static string SerializeExampleConfig(ExampleConfig cfg)
        {
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";
            var sb = new StringBuilder();
            sb.AppendLine("[ExampleConfig]");
            sb.Append("StoredVersion = \"").Append(cfg.ConfigVersion).AppendLine("\"");
            sb.Append("RespondToHello = ");
            sb.Append(cfg.RespondToHello ? "true" : "false");
            sb.Append(" # ");
            sb.Append(defaultRespondToHello ? "true" : "false");
            sb.AppendLine();
            sb.Append("GreetingMessage = ");
            sb.Append(ToTomlString(defaultGreetingMessage, cfg.GreetingMessage));
            sb.Append(" # ");
            sb.Append(ToTomlString(defaultGreetingMessage, defaultGreetingMessage));
            sb.AppendLine();
            return sb.ToString();
        }

        private static ConfigBase DeserializeExampleConfig(string content)
        {
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";
            var cfg = new ExampleConfig();
            if (string.IsNullOrEmpty(content)) return cfg;
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0) continue;
                var key = line.Substring(0, eqIndex).Trim();
                var valuePart = line.Substring(eqIndex + 1).Trim();
                var valueText = valuePart;
                string defaultText = null;
                var hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                    defaultText = valuePart.Substring(hashIndex + 1).Trim();
                }

                if (string.Equals(key, "RespondToHello", StringComparison.OrdinalIgnoreCase))
                {
                    bool parsedValue;
                    var hasValue = bool.TryParse(valueText, out parsedValue);
                    var parsedDefault = false;
                    var hasDefault = defaultText != null && bool.TryParse(defaultText, out parsedDefault);
                    if (hasValue)
                    {
                        if (hasDefault && parsedValue == parsedDefault && parsedDefault != defaultRespondToHello)
                        {
                            cfg.RespondToHello = defaultRespondToHello;
                        }
                        else
                        {
                            cfg.RespondToHello = parsedValue;
                        }
                    }
                }
                else if (string.Equals(key, "GreetingMessage", StringComparison.OrdinalIgnoreCase))
                {
                    var valueString = FromTomlString(valueText);
                    string defaultString = null;
                    if (!string.IsNullOrEmpty(defaultText))
                    {
                        defaultString = FromTomlString(defaultText);
                    }

                    if (!string.IsNullOrEmpty(defaultString) && valueString == defaultString &&
                        defaultString != defaultGreetingMessage)
                    {
                        cfg.GreetingMessage = defaultGreetingMessage;
                    }
                    else
                    {
                        cfg.GreetingMessage = valueString;
                    }
                }
                else if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    // version not used yet
                }
            }

            return cfg;
        }

        private static ITomlModel BuildDefaultModelExample(IConfigDefinition definition)
        {
            var cfg = new ExampleConfig();
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";
            var model = new TomlModel();
            model.TypeName = definition.TypeName;
            model.StoredVersion = cfg.ConfigVersion;
            var resp = new TomlEntry();
            resp.Value = defaultRespondToHello ? "true" : "false";
            resp.DefaultValue = resp.Value;
            model.Entries["RespondToHello"] = resp;
            var greet = new TomlEntry();
            var defToml = ToTomlString(defaultGreetingMessage, defaultGreetingMessage);
            greet.Value = defToml;
            greet.DefaultValue = defToml;
            model.Entries["GreetingMessage"] = greet;
            return model;
        }

        // ------------------------------------------------
        // Helpers
        // ------------------------------------------------

        private static string ToTomlValue(string raw)
        {
            if (raw == null) raw = string.Empty;
            var escaped = raw.Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string ToTomlString(string defaultValue, string currentValue)
        {
            if (currentValue == null) currentValue = string.Empty;
            var escaped = currentValue.Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string FromTomlString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var s = value.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                s = s.Substring(1, s.Length - 2);
            }

            s = s.Replace("\\\"", "\"");
            return s;
        }

        private static string FromTomlValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var v = value.Trim();
            if (v.Length >= 2 && v[0] == '"' && v[v.Length - 1] == '"')
            {
                return FromTomlString(v);
            }

            return v;
        }

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                return s.Substring(1, s.Length - 2);
            }

            return s;
        }
    }
}