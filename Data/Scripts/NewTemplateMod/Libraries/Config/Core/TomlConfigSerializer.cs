using System;
using System.Collections.Generic;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
    public class TomlConfigSerializer : IConfigSerializer
    {
        public string Serialize(ConfigBase config)
        {
            if (config == null)
                throw new ArgumentNullException("config");


            ExampleConfig example = config as ExampleConfig;
            if (example == null)
                throw new InvalidOperationException(
                    "TomlConfigSerializer currently only supports ExampleConfig.");

            return SerializeExampleConfig(example);
        }

        public ConfigBase Deserialize(IConfigDefinition definition, string content)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");

            if (definition.ConfigType == typeof(ExampleConfig))
            {
                return DeserializeExampleConfig(content);
            }

            throw new InvalidOperationException(
                "TomlConfigSerializer currently only supports ExampleConfig.");
        }

        // ---------------- TOML model API ----------------

        public ITomlModel ParseToModel(string tomlContent)
        {
            if (string.IsNullOrEmpty(tomlContent))
                return null;

            TomlModel model = new TomlModel();

            string[] lines = tomlContent.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string sectionName = line.Substring(1, line.Length - 2).Trim();
                    model.TypeName = sectionName;
                    continue;
                }

                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                string key = line.Substring(0, eqIndex).Trim();
                string valuePart = line.Substring(eqIndex + 1).Trim();

                string valueText = valuePart;
                string defaultText = null;

                int hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                    defaultText = valuePart.Substring(hashIndex + 1).Trim();
                }

                if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    model.StoredVersion = TrimQuotes(valueText);
                }
                else
                {
                    TomlEntry entry = new TomlEntry();
                    entry.Value = valueText;
                    entry.DefaultValue = defaultText;
                    model.Entries[key] = entry;
                }
            }

            return model;
        }

        public ITomlModel BuildDefaultModel(IConfigDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");

            if (definition.ConfigType != typeof(ExampleConfig))
                throw new InvalidOperationException(
                    "BuildDefaultModel currently only supports ExampleConfig.");

            ExampleConfig cfg = new ExampleConfig();

            TomlModel model = new TomlModel();
            model.TypeName = definition.TypeName;
            model.StoredVersion = cfg.ConfigVersion;

            TomlEntry resp = new TomlEntry();
            resp.Value = cfg.RespondToHello ? "true" : "false";
            resp.DefaultValue = resp.Value;
            model.Entries["RespondToHello"] = resp;

            TomlEntry greet = new TomlEntry();
            greet.Value = ToTomlString(cfg.GreetingMessage);
            greet.DefaultValue = greet.Value;
            model.Entries["GreetingMessage"] = greet;

            return model;
        }

        public string SerializeModel(ITomlModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(model.TypeName))
            {
                sb.Append('[').Append(model.TypeName).Append(']').AppendLine();
            }

            if (!string.IsNullOrEmpty(model.StoredVersion))
            {
                sb.Append("StoredVersion = \"").Append(model.StoredVersion).Append('"').AppendLine();
            }

            foreach (KeyValuePair<string, ITomlEntry> kv in model.Entries)
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

        // --------- ExampleConfig-specific implementation ---------

        private static string SerializeExampleConfig(ExampleConfig cfg)
        {
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[ExampleConfig]");
            sb.Append("StoredVersion = \"").Append(cfg.ConfigVersion).AppendLine("\"");

            sb.Append("RespondToHello = ");
            sb.Append(cfg.RespondToHello ? "true" : "false");
            sb.Append(" # ");
            sb.Append(defaultRespondToHello ? "true" : "false");
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
            // Current defaults
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";

            ExampleConfig cfg = new ExampleConfig();

            if (string.IsNullOrEmpty(content))
                return cfg;

            string[] lines = content.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // Section header, ignore for now
                    continue;
                }

                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                string key = line.Substring(0, eqIndex).Trim();
                string valuePart = line.Substring(eqIndex + 1).Trim();

                string valueText = valuePart;
                string defaultText = null;

                int hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                    defaultText = valuePart.Substring(hashIndex + 1).Trim();
                }

                if (string.Equals(key, "RespondToHello", StringComparison.OrdinalIgnoreCase))
                {
                    bool parsedValue;
                    bool hasValue = bool.TryParse(valueText, out parsedValue);

                    if (hasValue && defaultText == null)
                    {
                        cfg.RespondToHello = parsedValue;
                        continue;
                    }
                    

                    bool parsedDefault;
                    bool hasDefault = bool.TryParse(defaultText, out parsedDefault);

                    if (hasValue)
                    {
                        if (hasDefault &&
                            parsedValue == parsedDefault &&
                            parsedDefault != defaultRespondToHello)
                        {
                            // Value equals old default, but the current default changed:
                            // treat as "user never touched it" -> update to current default.
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
                    string valueString = FromTomlString(valueText);

                    string defaultString = null;
                    if (!string.IsNullOrEmpty(defaultText))
                    {
                        defaultString = FromTomlString(defaultText);
                    }

                    if (!string.IsNullOrEmpty(defaultString) &&
                        valueString == defaultString &&
                        defaultString != defaultGreetingMessage)
                    {
                        // Value equals old default, but current default changed:
                        // update to current default.
                        cfg.GreetingMessage = defaultGreetingMessage;
                    }
                    else
                    {
                        cfg.GreetingMessage = valueString;
                    }
                }
                else if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    // Version handling will come later with more migration logic.
                }
            }

            return cfg;
        }

        private static string ToTomlString(string value)
        {
            if (value == null)
                return "\"\"";

            string escaped = value.Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string FromTomlString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string s = value.Trim();
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
