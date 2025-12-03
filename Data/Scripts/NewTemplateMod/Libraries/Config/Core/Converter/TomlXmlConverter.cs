using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Storage;
using mz.Config.Domain;

namespace mz.Config.Core.Converter
{
    /// <summary>
    /// Pure XML &lt;-&gt; TOML converter for a single config instance.
    /// No defaults, no migration, no version logic.
    /// </summary>
    public sealed class TomlXmlConverter : IXmlConverter
    {
        public string GetExtension => ".toml";

        private readonly IConfigXmlSerializer _backupXmlSerializer;

        public TomlXmlConverter(IConfigXmlSerializer backupXmlSerializer)
        {
            _backupXmlSerializer = backupXmlSerializer;
        }

        public TomlXmlConverter()
        {
        }

        private static IConfigXmlSerializer GetXmlSerializer(IConfigXmlSerializer backup)
        {
            var xml = InternalConfigStorage.XmlSerializer ?? backup;
            if (xml == null)
                throw new InvalidOperationException("No XML serializer available for TomlXmlConverter.");
            return xml;
        }

        public string ToExternal(IConfigDefinition definition, string xmlContent)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (xmlContent == null)
                xmlContent = string.Empty;

            // Parse current XML into key/value map
            var values = SimpleXml.ParseSimpleElements(xmlContent);

            // Optional descriptions from default instance
            IReadOnlyDictionary<string, string> descriptions = null;
            var defaultInstance = definition.CreateDefaultInstance();
            if (defaultInstance != null)
            {
                descriptions = defaultInstance.VariableDescriptions;
            }

            var sb = new StringBuilder();

            // Section header: [TypeName]
            sb.Append('[')
              .Append(definition.TypeName)
              .Append(']')
              .AppendLine();

            // One plain key = value per element, with optional doc comments above
            foreach (var kv in values)
            {
                var key = kv.Key;
                var raw = kv.Value;

                // Insert multiline description as comments, if present
                if (descriptions != null)
                {
                    string desc;
                    if (descriptions.TryGetValue(key, out desc) && !string.IsNullOrEmpty(desc))
                    {
                        // Support \n and \r\n
                        var split = desc.Replace("\r\n", "\n").Split('\n');
                        foreach (var line in split)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                sb.Append("# ").Append(line).AppendLine();
                            }
                            else
                            {
                                // empty line -> still emit a bare comment to keep spacing
                                sb.Append("#").AppendLine();
                            }
                        }
                    }
                }

                var literal = ToTomlLiteral(raw);

                sb.Append(key)
                  .Append(" = ")
                  .Append(literal)
                  .AppendLine();
            }

            return sb.ToString();
        }

        public string ToInternal(IConfigDefinition definition, string externalContent)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var xmlSerializer = GetXmlSerializer(_backupXmlSerializer);

            // Empty file => default instance XML
            if (string.IsNullOrEmpty(externalContent))
            {
                var defaultInstance = definition.CreateDefaultInstance();
                return xmlSerializer.SerializeToXml(defaultInstance);
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

                // full-line comment (including description lines)
                if (line[0] == '#')
                    continue;

                // section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                    continue;

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                var key = line.Substring(0, eqIndex).Trim();
                var valuePart = line.Substring(eqIndex + 1).Trim();

                // strip end-of-line comment
                var valueText = valuePart;
                var hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                }

                var rawValue = FromTomlLiteral(valueText);
                values[key] = rawValue;
            }

            // Build simple XML for root = TypeName
            var xml = SimpleXml.BuildSimpleXml(definition.TypeName, values);
            return xml;
        }

        // ----------------- helpers -----------------

        private static string ToTomlLiteral(string raw)
        {
            if (raw == null)
                raw = string.Empty;

            bool b;
            int i;
            double d;

            // bool
            if (bool.TryParse(raw, out b))
                return b ? "true" : "false";

            // int
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
                return i.ToString(CultureInfo.InvariantCulture);

            // double
            if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands,
                               CultureInfo.InvariantCulture, out d))
                return d.ToString("G", CultureInfo.InvariantCulture);

            // fallback: string
            var escaped = raw.Replace("\\", @"\\").Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string FromTomlLiteral(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var s = value.Trim();

            // quoted -> unescape
            if (s.Length < 2 || s[0] != '"' || s[s.Length - 1] != '"') return s;
            
            s = s.Substring(1, s.Length - 2);
            s = s.Replace("\\\"", "\"").Replace(@"\\", "\\");
            return s;

            // bare token (true, 5, 0.5, etc.) => keep as text
        }
    }
}
