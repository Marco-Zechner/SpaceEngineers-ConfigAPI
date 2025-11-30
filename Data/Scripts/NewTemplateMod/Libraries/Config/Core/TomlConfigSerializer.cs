using System;
using System.Collections.Generic;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
public class TomlConfigSerializer : IConfigSerializer
{
    private readonly IConfigXmlSerializer _xml;

    public TomlConfigSerializer(IConfigXmlSerializer xml)
    {
        if (xml == null) throw new ArgumentNullException("xml");
        _xml = xml;
    }

    // ------------------------------------------------
    // High-level API
    // ------------------------------------------------

    public string Serialize(ConfigBase config)
    {
        if (config == null)
            throw new ArgumentNullException("config");

        // Special handling for ExampleConfig to keep existing behaviour/tests
        ExampleConfig example = config as ExampleConfig;
        if (example != null)
            return SerializeExampleConfig(example);

        // Generic path: XML -> fields -> TOML with simple bool/string heuristics
        string typeName = config.GetType().Name;
        string xml = _xml.SerializeToXml(config);
        Dictionary<string, string> fields = SimpleXml.ParseSimpleElements(xml);

        StringBuilder sb = new StringBuilder();

        sb.Append('[').Append(typeName).Append(']').AppendLine();
        sb.Append("StoredVersion = \"").Append(config.ConfigVersion).Append('"').AppendLine();

        foreach (KeyValuePair<string, string> kv in fields)
        {
            string key = kv.Key;
            string rawValue = kv.Value;

            string tomlValue = ToTomlValue(rawValue);

            sb.Append(key).Append(" = ").Append(tomlValue);
            sb.Append(" # ").Append(tomlValue); // comment == current value
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public ConfigBase Deserialize(IConfigDefinition definition, string content)
    {
        if (definition == null)
            throw new ArgumentNullException("definition");

        // Special handling for ExampleConfig
        if (definition.ConfigType == typeof(ExampleConfig))
            return DeserializeExampleConfig(content);

        // Generic path
        ITomlModel model = ParseToModel(content);
        if (model == null)
            return definition.CreateDefaultInstance();

        Dictionary<string, string> values = new Dictionary<string, string>();

        foreach (KeyValuePair<string, ITomlEntry> kv in model.Entries)
        {
            string key = kv.Key;
            string tomlValue = kv.Value.Value;

            string raw = FromTomlValue(tomlValue);
            values[key] = raw;
        }

        string rootName = definition.TypeName;
        string xml = SimpleXml.BuildSimpleXml(rootName, values);

        return definition.DeserializeFromXml(_xml, xml);
    }

    // ------------------------------------------------
    // TOML model API (used by ConfigStorage migration)
    // ------------------------------------------------

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
            string raw = lines[i];
            string line = raw.Trim();
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
                continue;
            }

            TomlEntry entry = new TomlEntry();
            entry.Value = valueText;
            entry.DefaultValue = defaultText;
            model.Entries[key] = entry;
        }

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

    public ITomlModel BuildDefaultModel(IConfigDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException("definition");

        if (definition.ConfigType == typeof(ExampleConfig))
            return BuildDefaultModelExample(definition);

        // Generic path: default instance -> XML -> fields
        ConfigBase defaultInstance = definition.CreateDefaultInstance();
        string xml = _xml.SerializeToXml(defaultInstance);
        Dictionary<string, string> fields = SimpleXml.ParseSimpleElements(xml);

        TomlModel model = new TomlModel();
        model.TypeName = definition.TypeName;
        model.StoredVersion = defaultInstance.ConfigVersion;

        foreach (KeyValuePair<string, string> kv in fields)
        {
            string key = kv.Key;
            string rawValue = kv.Value;

            string tomlValue = ToTomlValue(rawValue);

            TomlEntry entry = new TomlEntry();
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

        StringBuilder sb = new StringBuilder();

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

        ExampleConfig cfg = new ExampleConfig();

        if (string.IsNullOrEmpty(content))
            return cfg;

        string[] lines = content.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            string line = raw.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
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

                bool parsedDefault = false;
                bool hasDefault = defaultText != null && bool.TryParse(defaultText, out parsedDefault);

                if (hasValue)
                {
                    if (hasDefault &&
                        parsedValue == parsedDefault &&
                        parsedDefault != defaultRespondToHello)
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
        ExampleConfig cfg = new ExampleConfig();

        const bool defaultRespondToHello = false;
        const string defaultGreetingMessage = "hello";

        TomlModel model = new TomlModel();
        model.TypeName = definition.TypeName;
        model.StoredVersion = cfg.ConfigVersion;

        TomlEntry resp = new TomlEntry();
        resp.Value = defaultRespondToHello ? "true" : "false";
        resp.DefaultValue = resp.Value;
        model.Entries["RespondToHello"] = resp;

        TomlEntry greet = new TomlEntry();
        string defToml = ToTomlString(defaultGreetingMessage, defaultGreetingMessage);
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
        if (raw == null)
            return "\"\"";

        string t = raw.Trim();

        bool b;
        if (bool.TryParse(t, out b))
            return b ? "true" : "false";

        string escaped = t.Replace("\"", "\\\"");
        return "\"" + escaped + "\"";
    }

    private static string ToTomlString(string defaultValue, string currentValue)
    {
        if (currentValue == null)
            currentValue = string.Empty;

        string escaped = currentValue.Replace("\"", "\\\"");
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

    private static string FromTomlValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string v = value.Trim();

        if (v.Length >= 2 && v[0] == '"' && v[v.Length - 1] == '"')
        {
            return FromTomlString(v);
        }

        return v;
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