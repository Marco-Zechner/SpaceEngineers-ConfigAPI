using System;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
    public class TomlConfigSerializer : IConfigSerializer
    {
        private readonly IConfigXmlSerializer _xml; // currently unused, reserved for future

        public TomlConfigSerializer(IConfigXmlSerializer xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            _xml = xml;
        }

        public string Serialize(ConfigBase config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // For now we only support ExampleConfig; others can be added later.
            var example = config as ExampleConfig;
            if (example == null)
                throw new InvalidOperationException(
                    "TomlConfigSerializer currently only supports ExampleConfig.");

            return SerializeExampleConfig(example);
        }

        public ConfigBase Deserialize(IConfigDefinition definition, string content)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (definition.ConfigType == typeof(ExampleConfig))
            {
                return DeserializeExampleConfig(content);
            }

            throw new InvalidOperationException(
                "TomlConfigSerializer currently only supports ExampleConfig.");
        }

        public ITomlModel ParseToModel(string tomlContent)
        {
            if (string.IsNullOrEmpty(tomlContent))
                return null;

            var model = new TomlModel();

            var lines = tomlContent.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var sectionName = line.Substring(1, line.Length - 2).Trim();
                    model.TypeName = sectionName;
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

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

                var entry = new TomlEntry
                {
                    Value = valueText,
                    DefaultValue = defaultText
                };
                model.Entries[key] = entry;
            }

            return model;
        }

        public string SerializeModel(ITomlModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

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
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (definition.ConfigType != typeof(ExampleConfig))
                throw new InvalidOperationException(
                    "BuildDefaultModel currently only supports ExampleConfig.");

            var cfg = new ExampleConfig();

            const string defaultGreetingMessage = "hello";

            var model = new TomlModel
            {
                TypeName = definition.TypeName,
                StoredVersion = cfg.ConfigVersion
            };

            var resp = new TomlEntry
            {
                Value = "false"
            };
            resp.DefaultValue = resp.Value;
            model.Entries["RespondToHello"] = resp;

            var greet = new TomlEntry
            {
                Value = ToTomlString(defaultGreetingMessage)
            };
            greet.DefaultValue = greet.Value;
            model.Entries["GreetingMessage"] = greet;

            return model;
        }

        // ---------------- ExampleConfig-specific implementation ----------------

        private static string SerializeExampleConfig(ExampleConfig cfg)
        {
            const string defaultGreetingMessage = "hello";

            var sb = new StringBuilder();

            sb.AppendLine("[ExampleConfig]");
            sb.Append("StoredVersion = \"").Append(cfg.ConfigVersion).AppendLine("\"");

            sb.Append("RespondToHello = ");
            sb.Append(cfg.RespondToHello ? "true" : "false");
            sb.Append(" # ");
            sb.Append("false");
            sb.AppendLine();

            sb.Append("GreetingMessage = ");
            sb.Append(ToTomlString(cfg.GreetingMessage));
            sb.Append(" # ");
            sb.Append(ToTomlString(defaultGreetingMessage));
            sb.AppendLine();

            return sb.ToString();
        }

        private static ConfigBase DeserializeExampleConfig(string content)
        {
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";

            var cfg = new ExampleConfig();

            if (string.IsNullOrEmpty(content))
                return cfg;

            var lines = content.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

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

                    if (hasValue && defaultText == null)
                    {
                        cfg.RespondToHello = parsedValue;
                        continue;
                    }

                    bool parsedDefault;
                    var hasDefault = bool.TryParse(defaultText, out parsedDefault);

                    if (hasValue)
                    {
                        if (hasDefault &&
                            parsedValue == parsedDefault &&
                            parsedDefault != defaultRespondToHello)
                        {
                            // value == old default but current default changed → upgrade to current default
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

                    if (!string.IsNullOrEmpty(defaultString) &&
                        valueString == defaultString &&
                        defaultString != defaultGreetingMessage)
                    {
                        // value == old default but current default changed → upgrade to current default
                        cfg.GreetingMessage = defaultGreetingMessage;
                    }
                    else
                    {
                        cfg.GreetingMessage = valueString;
                    }
                }
                else if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    // currently unused; version migration can be added later
                }
            }

            return cfg;
        }

        // ---------------- helpers ----------------

        private static string ToTomlString(string value)
        {
            if (value == null)
                return "\"\"";

            var escaped = value.Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string FromTomlString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var s = value.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                s = s.Substring(1, s.Length - 2);
            }

            s = s.Replace("\\\"", "\"");
            return s;
        }

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                return s.Substring(1, s.Length - 2);
            }

            return s;
        }
    }
}