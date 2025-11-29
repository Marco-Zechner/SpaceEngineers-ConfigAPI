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

            // For now we only support ExampleConfig (no reflection, simple and explicit).
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

        // --------- not used by current tests; keep as stubs for now ---------

        public ITomlModel ParseToModel(string tomlContent)
        {
            throw new NotImplementedException();
        }

        public string SerializeModel(ITomlModel model)
        {
            throw new NotImplementedException();
        }

        public ITomlModel BuildDefaultModel(IConfigDefinition definition)
        {
            throw new NotImplementedException();
        }

        // --------- ExampleConfig-specific implementation ---------

        private static string SerializeExampleConfig(ExampleConfig cfg)
        {
            // Defaults (from ExampleConfig ctor)
            const bool defaultRespondToHello = false;
            const string defaultGreetingMessage = "hello";

            StringBuilder sb = new();

            sb.AppendLine("[ExampleConfig]");
            sb.Append("StoredVersion = \"").Append(cfg.ConfigVersion).AppendLine("\"");

            // RespondToHello = <value> # <default>
            sb.Append("RespondToHello = ");
            sb.Append(cfg.RespondToHello ? "true" : "false");
            sb.Append(" # ");
            sb.Append(defaultRespondToHello ? "true" : "false");
            sb.AppendLine();

            // GreetingMessage = "<value>" # "<default>"
            sb.Append("GreetingMessage = ");
            sb.Append(ToTomlString(cfg.GreetingMessage));
            sb.Append(" # ");
            sb.Append(ToTomlString(defaultGreetingMessage));
            sb.AppendLine();

            return sb.ToString();
        }

        private static ConfigBase DeserializeExampleConfig(string content)
        {
            // Start from defaults
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
                    // Section header, e.g. [ExampleConfig]
                    // We can ignore the name for now; tests assume correct section.
                    continue;
                }

                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                string key = line.Substring(0, eqIndex).Trim();
                string valuePart = line.Substring(eqIndex + 1).Trim();

                // Strip default comment: "<value> # <default>"
                string valueText = valuePart;
                int hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                }

                if (string.Equals(key, "RespondToHello", StringComparison.OrdinalIgnoreCase))
                {
                    bool parsedBool;
                    if (bool.TryParse(valueText, out parsedBool))
                        cfg.RespondToHello = parsedBool;
                }
                else if (string.Equals(key, "GreetingMessage", StringComparison.OrdinalIgnoreCase))
                {
                    cfg.GreetingMessage = FromTomlString(valueText);
                }
                else if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    // Ignore for now – version handling will come with migration tests later.
                }
            }

            return cfg;
        }

        private static string ToTomlString(string value)
        {
            if (value == null)
                return "\"\"";

            // Very naive quoting, sufficient for tests:
            // - wrap in double quotes
            // - escape existing double quotes
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

            // Unescape basic \" sequences
            s = s.Replace("\\\"", "\"");
            return s;
        }
    }
}
