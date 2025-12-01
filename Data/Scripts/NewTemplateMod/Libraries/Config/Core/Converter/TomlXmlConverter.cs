using System;
using System.Collections.Generic;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.SE;

namespace mz.Config.Core.Converter
{
    /// <summary>
    /// XML &lt;-&gt; TOML converter.
    /// ToExternal:
    ///   - parses the given XML for the config instance
    ///   - uses a fresh default instance for the key set and defaults
    ///   - emits TOML with a [TypeName] section and StoredVersion
    /// ToInternal:
    ///   - parses a very simple "key = value [# comment]" TOML
    ///   - builds a minimal XML document that can be deserialized by XmlSerializer
    /// </summary>
    public sealed class TomlXmlConverter : IXmlConverter
    {
        private readonly IConfigXmlSerializer _xmlSerializer;

        public TomlXmlConverter(IConfigXmlSerializer xmlSerializer)
        {
            if (xmlSerializer == null) throw new ArgumentNullException(nameof(xmlSerializer));
            _xmlSerializer = xmlSerializer;
        }

        public string ToExternal(IConfigDefinition definition, string xmlContent)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (xmlContent == null) xmlContent = string.Empty;

            // Current values from xmlContent
            var currentValues = SimpleXml.ParseSimpleElements(xmlContent);

            // Defaults from a freshly created instance (defines the layout)
            var defaultInstance = definition.CreateDefaultInstance();
            var defaultXml = _xmlSerializer.SerializeToXml(defaultInstance);
            var defaultValues = SimpleXml.ParseSimpleElements(defaultXml);

            var sb = new StringBuilder();

            // Section header: use type name
            sb.Append('[')
              .Append(definition.TypeName)
              .Append(']')
              .AppendLine();

            // Stored version from default instance (code version)
            var storedVersion = defaultInstance.ConfigVersion ?? string.Empty;
            sb.Append("StoredVersion = ")
              .Append(ToTomlString(storedVersion))
              .AppendLine();

            // For each key in the current code layout (defaults dict)
            foreach (var kv in defaultValues)
            {
                var key = kv.Key;
                var defaultRaw = kv.Value;

                string currentRaw;
                if (!currentValues.TryGetValue(key, out currentRaw))
                {
                    currentRaw = defaultRaw;
                }

                var valueToml = ToTomlString(currentRaw);
                var defaultToml = ToTomlString(defaultRaw);

                sb.Append(key)
                  .Append(" = ")
                  .Append(valueToml);

                // Comment shows the default (useful for humans and potential future migration)
                sb.Append(" # ")
                  .Append(defaultToml);

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string ToInternal(IConfigDefinition definition, string externalContent)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrEmpty(externalContent))
            {
                // No content -> return XML for default instance
                var defaultInstance = definition.CreateDefaultInstance();
                return _xmlSerializer.SerializeToXml(defaultInstance);
            }

            var lines = externalContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var values = new Dictionary<string, string>();

            foreach (var raw in lines)
            {
                if (string.IsNullOrEmpty(raw))
                    continue;

                var line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line[0] == '#')
                    continue;

                // Section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // Section name is ignored; we rely on definition.TypeName instead.
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                var key = line.Substring(0, eqIndex).Trim();
                var valuePart = line.Substring(eqIndex + 1).Trim();

                // Split off comment if present
                var valueText = valuePart;
                var hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                    // comment part is ignored
                }

                if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                {
                    // We ignore StoredVersion here, version is taken from code.
                    continue;
                }

                var rawValue = FromTomlString(valueText);
                values[key] = rawValue;
            }

            // Build simple XML for the root element == TypeName
            var xml = SimpleXml.BuildSimpleXml(definition.TypeName, values);
            return xml;
        }

        // ----------------------------------------------------
        // Helpers
        // ----------------------------------------------------

        private static string ToTomlString(string raw)
        {
            if (raw == null)
                raw = string.Empty;

            // For simplicity: treat everything as TOML string
            var escaped = raw.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string FromTomlString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var s = value.Trim();

            // Quoted string: unescape
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                s = s.Substring(1, s.Length - 2);
                s = s.Replace("\\\"", "\"").Replace("\\\\", "\\");
                return s;
            }

            // Non-quoted: just return as-is (for numbers, bools, etc.)
            return s;
        }
    }
}
