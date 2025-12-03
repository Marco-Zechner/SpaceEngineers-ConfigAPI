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
    ///
    /// Rules:
    /// - Scalars: &lt;Key&gt;value&lt;/Key&gt;           &lt;=&gt;  Key = value
    /// - Arrays:  &lt;IntArray&gt;&lt;int&gt;5&lt;/int&gt;...&lt;/IntArray&gt;
    ///            &lt;=&gt;  IntArray = [5, 10, 15]
    /// - Strings array: same but element tag "string".
    /// - Nested object:
    ///      &lt;Child&gt;&lt;Age&gt;42&lt;/Age&gt;...&lt;/Child&gt;
    ///      &lt;=&gt; Child.Age = 42, Child.Name = "NestedBob", ...
    ///
    /// ToInternal rebuilds XML in the shapes above so the XML serializer
    /// can reconstruct arrays and nested objects correctly.
    /// </summary>
    public sealed class TomlXmlConverter : IXmlConverter
    {
        public string GetExtension
        {
            get { return ".toml"; }
        }

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
            IConfigXmlSerializer xml;
            if (InternalConfigStorage.IsInitialized)
            {
                xml = InternalConfigStorage.XmlSerializer ?? backup;
            }
            else
            {
                xml = backup;
            }

            if (xml == null)
                throw new InvalidOperationException("No XML serializer available for TomlXmlConverter.");
            return xml;
        }

        // ====================================================================
        // Public API
        // ====================================================================

        public string ToExternal(IConfigDefinition definition, string xmlContent)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");
            if (xmlContent == null)
                xmlContent = string.Empty;

            string rootName;
            var rootChildren = ParseRootChildren(xmlContent, out rootName);

            // Flatten into key -> list of raw values
            var flat = new Dictionary<string, List<string>>();

            foreach (var kv in rootChildren)
            {
                var propName = kv.Key;
                var innerXml = kv.Value;
                var trimmed = innerXml.Trim();

                if (trimmed.Length == 0 || trimmed.IndexOf('<') < 0)
                {
                    // Simple scalar text
                    AddFlatValue(flat, propName, trimmed);
                }
                else
                {
                    // Try to parse first-level children inside this snippet.
                    var children = ParseSnippetChildren(trimmed);

                    if (children.Count == 0)
                    {
                        // Fallback: treat as scalar blob
                        AddFlatValue(flat, propName, trimmed);
                    }
                    else
                    {
                        // Check if this looks like an "array of primitive"
                        bool sameName = true;
                        string firstName = children[0].Name;
                        for (int i = 1; i < children.Count; i++)
                        {
                            if (children[i].Name != firstName)
                            {
                                sameName = false;
                                break;
                            }
                        }

                        if (sameName)
                        {
                            // IntArray / Names style: IntArray = [5, 10, 15]
                            for (int i = 0; i < children.Count; i++)
                            {
                                AddFlatValue(flat, propName, children[i].Value.Trim());
                            }
                        }
                        else
                        {
                            // Nested object: Child.Age, Child.Name, ...
                            for (int i = 0; i < children.Count; i++)
                            {
                                var child = children[i];
                                var key = propName + "." + child.Name;
                                AddFlatValue(flat, key, child.Value.Trim());
                            }
                        }
                    }
                }
            }

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

            foreach (var kv in flat)
            {
                var key = kv.Key;
                var list = kv.Value;
                if (list == null || list.Count == 0)
                    continue;

                // Description comments: match by top-level property name
                if (descriptions != null)
                {
                    string lookupKey = key;
                    int dotIndex = key.IndexOf('.');
                    if (dotIndex > 0)
                        lookupKey = key.Substring(0, dotIndex);

                    string desc;
                    if (descriptions.TryGetValue(lookupKey, out desc) && !string.IsNullOrEmpty(desc))
                    {
                        var split = desc.Replace("\r\n", "\n").Split('\n');
                        for (int i = 0; i < split.Length; i++)
                        {
                            var line = split[i];
                            if (!string.IsNullOrEmpty(line))
                            {
                                sb.Append("# ").Append(line).AppendLine();
                            }
                            else
                            {
                                sb.Append("#").AppendLine();
                            }
                        }
                    }
                }

                if (list.Count == 1)
                {
                    var literal = ToTomlLiteral(list[0]);
                    sb.Append(key)
                      .Append(" = ")
                      .Append(literal)
                      .AppendLine();
                }
                else
                {
                    sb.Append(key)
                      .Append(" = [");

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(", ");

                        var literal = ToTomlLiteral(list[i]);
                        sb.Append(literal);
                    }

                    sb.Append(']')
                      .AppendLine();
                }
            }

            return sb.ToString();
        }

        public string ToInternal(IConfigDefinition definition, string externalContent)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");

            var xmlSerializer = GetXmlSerializer(_backupXmlSerializer);

            // Empty file => default instance XML
            if (string.IsNullOrEmpty(externalContent))
            {
                var defaultInstance = definition.CreateDefaultInstance();
                return xmlSerializer.SerializeToXml(defaultInstance);
            }

            // Parse TOML into key -> list of raw values
            var flat = ParseTomlToFlatMap(externalContent);

            // Split into root scalars/arrays vs nested groups
            var rootMap = new Dictionary<string, List<string>>();
            var nested = new Dictionary<string, Dictionary<string, string>>();

            foreach (var kv in flat)
            {
                var key = kv.Key;
                var list = kv.Value ?? new List<string>();

                var segments = key.Split('.');
                if (segments.Length == 1)
                {
                    rootMap[key] = list;
                }
                else
                {
                    var parent = segments[0];
                    var child = segments[1];

                    Dictionary<string, string> childMap;
                    if (!nested.TryGetValue(parent, out childMap))
                    {
                        childMap = new Dictionary<string, string>();
                        nested[parent] = childMap;
                    }

                    // Nested fields are treated as single-value
                    var val = list.Count > 0 ? list[0] : string.Empty;
                    childMap[child] = val;
                }
            }

            var sb = new StringBuilder();
            sb.Append('<').Append(definition.TypeName).Append('>');

            // Root scalars and arrays
            foreach (var kv in rootMap)
            {
                var name = kv.Key;
                var list = kv.Value;

                if (list == null || list.Count == 0)
                {
                    // Empty scalar
                    sb.Append('<').Append(name).Append("></").Append(name).Append('>');
                    continue;
                }

                if (list.Count == 1)
                {
                    // Scalar
                    sb.Append('<').Append(name).Append('>');
                    sb.Append(XmlEscape(list[0]));
                    sb.Append("</").Append(name).Append('>');
                }
                else
                {
                    // Array of primitive -> wrap in parent + element tags
                    sb.Append('<').Append(name).Append('>');

                    string elementTag = InferArrayElementTag(list);

                    for (int i = 0; i < list.Count; i++)
                    {
                        sb.Append('<').Append(elementTag).Append('>');
                        sb.Append(XmlEscape(list[i]));
                        sb.Append("</").Append(elementTag).Append('>');
                    }

                    sb.Append("</").Append(name).Append('>');
                }
            }

            // Nested objects
            foreach (var kn in nested)
            {
                var parentName = kn.Key;
                var childMap = kn.Value;

                sb.Append('<').Append(parentName).Append('>');
                foreach (var kvChild in childMap)
                {
                    sb.Append('<').Append(kvChild.Key).Append('>');
                    sb.Append(XmlEscape(kvChild.Value));
                    sb.Append("</").Append(kvChild.Key).Append('>');
                }
                sb.Append("</").Append(parentName).Append('>');
            }

            sb.Append("</").Append(definition.TypeName).Append('>');
            return sb.ToString();
        }

        // ====================================================================
        // TOML scalar/array helpers
        // ====================================================================

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

            // fallback: string, escaped
            var escaped = raw
                .Replace("\\", @"\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

            return "\"" + escaped + "\"";
        }

        private static string FromTomlLiteral(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var s = value.Trim();

            // quoted -> unescape
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                s = s.Substring(1, s.Length - 2);

                s = s.Replace("\\\"", "\"");
                s = s.Replace(@"\\", "\\");
                s = s.Replace("\\r", "\r");
                s = s.Replace("\\n", "\n");

                return s;
            }

            // bare token
            return s;
        }

        private static void AddFlatValue(Dictionary<string, List<string>> map, string key, string value)
        {
            List<string> list;
            if (!map.TryGetValue(key, out list))
            {
                list = new List<string>();
                map[key] = list;
            }
            list.Add(value ?? string.Empty);
        }

        private static string InferArrayElementTag(List<string> values)
        {
            if (values == null || values.Count == 0)
                return "string";

            // Check if all look like ints
            bool allInts = true;
            for (int i = 0; i < values.Count; i++)
            {
                int tmp;
                if (!int.TryParse(values[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out tmp))
                {
                    allInts = false;
                    break;
                }
            }
            if (allInts)
                return "int";

            // Could add more heuristics (double, bool) here if needed.
            return "string";
        }

        // ====================================================================
        // Minimal XML parsing for root children and one-level snippets
        // ====================================================================

        private static Dictionary<string, string> ParseRootChildren(string xml, out string rootName)
        {
            var result = new Dictionary<string, string>();
            rootName = string.Empty;

            if (string.IsNullOrEmpty(xml))
                return result;

            int len = xml.Length;
            int pos = 0;

            // Find root
            string foundRoot = null;
            int rootStartClose = -1;
            int endRootIndex = -1;

            while (pos < len && foundRoot == null)
            {
                int lt = xml.IndexOf('<', pos);
                if (lt < 0)
                    break;

                int gt = xml.IndexOf('>', lt + 1);
                if (gt < 0)
                    break;

                string tagContent = xml.Substring(lt + 1, gt - lt - 1).Trim();
                if (tagContent.Length == 0)
                {
                    pos = gt + 1;
                    continue;
                }

                char first = tagContent[0];
                if (first == '?' || first == '!' || first == '/')
                {
                    pos = gt + 1;
                    continue;
                }

                int spaceIndex = tagContent.IndexOf(' ');
                foundRoot = spaceIndex >= 0
                    ? tagContent.Substring(0, spaceIndex)
                    : tagContent;

                rootStartClose = gt;

                string endRootTag = "</" + foundRoot + ">";
                endRootIndex = xml.LastIndexOf(endRootTag, StringComparison.Ordinal);
                if (endRootIndex < 0)
                    return result;
            }

            if (foundRoot == null || rootStartClose < 0 || endRootIndex <= rootStartClose)
                return result;

            rootName = foundRoot;

            string inner = xml.Substring(rootStartClose + 1, endRootIndex - (rootStartClose + 1));
            int innerLen = inner.Length;
            int innerPos = 0;

            while (true)
            {
                int startTagOpen = inner.IndexOf('<', innerPos);
                if (startTagOpen < 0 || startTagOpen >= innerLen)
                    break;

                int startTagClose = inner.IndexOf('>', startTagOpen + 1);
                if (startTagClose < 0)
                    break;

                string startTagContent = inner.Substring(startTagOpen + 1, startTagClose - startTagOpen - 1).Trim();
                if (startTagContent.Length == 0)
                {
                    innerPos = startTagClose + 1;
                    continue;
                }

                char first = startTagContent[0];
                if (first == '?' || first == '!' || first == '/')
                {
                    innerPos = startTagClose + 1;
                    continue;
                }

                int spaceIndex = startTagContent.IndexOf(' ');
                bool selfClosing = startTagContent.EndsWith("/", StringComparison.Ordinal);
                string tagName = spaceIndex >= 0
                    ? startTagContent.Substring(0, spaceIndex)
                    : (selfClosing
                        ? startTagContent.TrimEnd('/')
                        : startTagContent);

                if (selfClosing)
                {
                    result[tagName] = string.Empty;
                    innerPos = startTagClose + 1;
                    continue;
                }

                string endTag = "</" + tagName + ">";
                int endTagIndex = inner.IndexOf(endTag, startTagClose + 1, StringComparison.Ordinal);
                if (endTagIndex < 0)
                {
                    innerPos = startTagClose + 1;
                    continue;
                }

                int valueStart = startTagClose + 1;
                string innerValue = inner.Substring(valueStart, endTagIndex - valueStart);

                result[tagName] = innerValue;

                innerPos = endTagIndex + endTag.Length;
            }

            return result;
        }

        private struct SnippetChild
        {
            public string Name;
            public string Value;
        }

        private static List<SnippetChild> ParseSnippetChildren(string snippet)
        {
            var result = new List<SnippetChild>();

            if (string.IsNullOrEmpty(snippet))
                return result;

            int len = snippet.Length;
            int pos = 0;

            while (true)
            {
                int lt = snippet.IndexOf('<', pos);
                if (lt < 0 || lt >= len)
                    break;

                int gt = snippet.IndexOf('>', lt + 1);
                if (gt < 0)
                    break;

                string tagContent = snippet.Substring(lt + 1, gt - lt - 1).Trim();
                if (tagContent.Length == 0)
                {
                    pos = gt + 1;
                    continue;
                }

                char first = tagContent[0];
                if (first == '?' || first == '!' || first == '/')
                {
                    pos = gt + 1;
                    continue;
                }

                int spaceIndex = tagContent.IndexOf(' ');
                bool selfClosing = tagContent.EndsWith("/", StringComparison.Ordinal);
                string tagName = spaceIndex >= 0
                    ? tagContent.Substring(0, spaceIndex)
                    : (selfClosing
                        ? tagContent.TrimEnd('/')
                        : tagContent);

                if (selfClosing)
                {
                    result.Add(new SnippetChild
                    {
                        Name = tagName,
                        Value = string.Empty
                    });
                    pos = gt + 1;
                    continue;
                }

                string endTag = "</" + tagName + ">";
                int endTagIndex = snippet.IndexOf(endTag, gt + 1, StringComparison.Ordinal);
                if (endTagIndex < 0)
                {
                    pos = gt + 1;
                    continue;
                }

                int valueStart = gt + 1;
                string innerValue = snippet.Substring(valueStart, endTagIndex - valueStart);

                result.Add(new SnippetChild
                {
                    Name = tagName,
                    Value = innerValue
                });

                pos = endTagIndex + endTag.Length;
            }

            return result;
        }

        private static string XmlEscape(string s)
        {
            if (s == null)
                return string.Empty;

            return s
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        // ====================================================================
        // TOML parsing
        // ====================================================================

        private static Dictionary<string, List<string>> ParseTomlToFlatMap(string toml)
        {
            var result = new Dictionary<string, List<string>>();

            if (string.IsNullOrEmpty(toml))
                return result;

            var lines = toml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                if (string.IsNullOrEmpty(raw))
                    continue;

                var line = raw.Trim();
                if (line.Length == 0)
                    continue;

                // full-line comments
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
                var hashIndex = valuePart.IndexOf('#');
                if (hashIndex >= 0)
                {
                    valuePart = valuePart.Substring(0, hashIndex).Trim();
                }

                if (string.IsNullOrEmpty(valuePart))
                {
                    AddFlatValue(result, key, string.Empty);
                    continue;
                }

                // Array: key = [ ... ]
                if (valuePart.Length >= 2 &&
                    valuePart[0] == '[' &&
                    valuePart[valuePart.Length - 1] == ']')
                {
                    var inner = valuePart.Substring(1, valuePart.Length - 2);

                    var items = SplitTomlArray(inner);
                    for (int j = 0; j < items.Count; j++)
                    {
                        var itemText = items[j].Trim();
                        if (itemText.Length == 0)
                            continue;

                        var rawValue = FromTomlLiteral(itemText);
                        AddFlatValue(result, key, rawValue);
                    }
                }
                else
                {
                    var rawValue = FromTomlLiteral(valuePart);
                    AddFlatValue(result, key, rawValue);
                }
            }

            return result;
        }

        private static List<string> SplitTomlArray(string inner)
        {
            var result = new List<string>();
            if (inner == null)
                return result;

            var sb = new StringBuilder();
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < inner.Length; i++)
            {
                var c = inner[i];

                if (escape)
                {
                    sb.Append(c);
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    sb.Append(c);
                    escape = true;
                    continue;
                }

                if (c == '"')
                {
                    sb.Append(c);
                    inString = !inString;
                    continue;
                }

                if (c == ',' && !inString)
                {
                    result.Add(sb.ToString());
                    sb.Length = 0;
                    continue;
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }
    }
}
