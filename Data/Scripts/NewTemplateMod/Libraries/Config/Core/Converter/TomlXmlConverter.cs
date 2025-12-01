using System;
using System.Collections.Generic;
using System.Text;
using mz.Config.Abstractions;
using mz.Config.Abstractions.Converter;
using mz.Config.Abstractions.SE;
using mz.Config.Core.Storage;

namespace mz.Config.Core.Converter
{
    public sealed class TomlXmlConverter : IXmlConverter
    {
        public string GetExtension => ".toml";

        private readonly IConfigXmlSerializer _backupXmlSerializer;

        /// <summary>
        /// Creates a new TomlXmlConverter with a backup XML serializer.
        /// This can be used if you know that ConfigStorage won't be initialized yet.
        /// (ConfigStorage.XmlSerializer will always be preferred if available.)
        /// </summary>
        /// <param name="backupXmlSerializer"></param>
        public TomlXmlConverter(IConfigXmlSerializer backupXmlSerializer)
        {
            _backupXmlSerializer = backupXmlSerializer;
        }
        
        public TomlXmlConverter() {}
        
        public string ToExternal(IConfigDefinition definition, string xmlContent)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (xmlContent == null) xmlContent = string.Empty;

            var currentValues = SimpleXml.ParseSimpleElements(xmlContent);

            var defaultInstance = definition.CreateDefaultInstance();
            var defaultXml = (InternalConfigStorage.XmlSerializer ?? _backupXmlSerializer).SerializeToXml(defaultInstance);
            var defaultValues = SimpleXml.ParseSimpleElements(defaultXml);

            var sb = new StringBuilder();

            sb.Append('[')
              .Append(definition.TypeName)
              .Append(']')
              .AppendLine();

            var storedVersion = defaultInstance.ConfigVersion ?? string.Empty;
            sb.Append("StoredVersion = ")
              .Append(ToTomlString(storedVersion))
              .AppendLine();

            foreach (var kv in defaultValues)
            {
                var key = kv.Key;
                var defaultRaw = kv.Value;

                string currentRaw;
                if (!currentValues.TryGetValue(key, out currentRaw))
                    currentRaw = defaultRaw;

                var valueToml = ToTomlString(currentRaw);
                var defaultToml = ToTomlString(defaultRaw);

                sb.Append(key)
                  .Append(" = ")
                  .Append(valueToml)
                  .Append(" # ")
                  .Append(defaultToml)
                  .AppendLine();
            }

            return sb.ToString();
        }

        public string ToInternal(IConfigDefinition definition, string externalContent)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrEmpty(externalContent))
            {
                var defaultInstance = definition.CreateDefaultInstance();
                return (InternalConfigStorage.XmlSerializer ?? _backupXmlSerializer).SerializeToXml(defaultInstance);
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

                if (line.StartsWith("[") && line.EndsWith("]"))
                    continue;

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                var key = line.Substring(0, eqIndex).Trim();
                var valuePart = line.Substring(eqIndex + 1).Trim();

                var valueText = valuePart;
                var hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valueText = valuePart.Substring(0, hashIndex).Trim();
                }

                if (string.Equals(key, "StoredVersion", StringComparison.OrdinalIgnoreCase))
                    continue;

                var rawValue = FromTomlString(valueText);
                values[key] = rawValue;
            }

            var xml = SimpleXml.BuildSimpleXml(definition.TypeName, values);
            return xml;
        }

        private static string ToTomlString(string raw)
        {
            if (raw == null)
                raw = string.Empty;

            var escaped = raw.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
                s = s.Replace("\\\"", "\"").Replace("\\\\", "\\");
                return s;
            }

            return s;
        }
    }
}
