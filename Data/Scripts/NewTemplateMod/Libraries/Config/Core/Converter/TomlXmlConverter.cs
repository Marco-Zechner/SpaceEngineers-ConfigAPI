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
    /// Pure XML <-> TOML converter for a single config instance.
    /// No defaults, no migration, no version logic.
    ///
    /// Rules:
    /// - Scalars: <Key>value</Key>                  <=>  Key = value
    /// - Arrays:  <IntList><int>1</int>...</IntList> <=> IntList = [1, 2, 3]
    /// - Nested:  <Nested><Threshold>..</Threshold>  <=> Nested.Threshold = ...
    /// - Dictionary<string,int> (SerializableDictionary):
    ///       <NamedValues><dictionary><item>...</item>...</dictionary></NamedValues>
    ///       <=>
    ///       NamedValues.start = 1
    ///       NamedValues.end   = 99
    ///
    /// Nullable values:
    ///   <OptionalInt xsi:nil="true" />  <=> OptionalInt = null
    /// </summary>
    public sealed class TomlXmlConverter : IXmlConverter
    {
        private const string NULL_SENTINEL = "null";

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
                throw new ArgumentNullException(nameof(definition));
            if (xmlContent == null)
                xmlContent = string.Empty;

            string rootName;
            var rootChildren = ParseRootChildren(xmlContent, out rootName);

            // Default instance for descriptions & dictionary detection
            ConfigBase defaultInstance = definition.CreateDefaultInstance();
            IReadOnlyDictionary<string, string> descriptions = null;
            HashSet<string> dictionaryParents = null;

            if (defaultInstance != null)
            {
                descriptions = defaultInstance.VariableDescriptions;
                dictionaryParents = DetectDictionaryParents(descriptions);
            }

            // Flatten into key -> list of raw string values
            var flat = new Dictionary<string, List<string>>();

            foreach (var kv in rootChildren)
            {
                var propName = kv.Key;
                var innerXml = kv.Value;
                var trimmed = innerXml.Trim();

                if (trimmed.Length == 0 || trimmed.IndexOf('<') < 0)
                {
                    // Simple scalar text (including NullSentinel from self-closing elements)
                    AddFlatValue(flat, propName, trimmed);
                    continue;
                }

                // Try to parse first-level children inside this snippet.
                var children = ParseSnippetChildren(trimmed);

                if (children.Count == 0)
                {
                    // Fallback: treat as scalar blob
                    AddFlatValue(flat, propName, trimmed);
                    continue;
                }

                // Special-case SerializableDictionary pattern:
                //   <PropName>
                //     <dictionary>
                //       <item><Key>k</Key><Value>v</Value></item>...
                //     </dictionary>
                //   </PropName>
                bool isDictionaryParent = dictionaryParents != null && dictionaryParents.Contains(propName);
                if (isDictionaryParent &&
                    children.Count == 1 &&
                    string.Equals(children[0].Name, "dictionary", StringComparison.OrdinalIgnoreCase))
                {
                    var dictItems = ParseSnippetChildren(children[0].Value);
                    for (int i = 0; i < dictItems.Count; i++)
                    {
                        var item = dictItems[i];
                        if (!string.Equals(item.Name, "item", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var kvChildren = ParseSnippetChildren(item.Value);
                        string keyText = null;
                        string valueText = string.Empty;

                        for (int j = 0; j < kvChildren.Count; j++)
                        {
                            var child = kvChildren[j];
                            if (string.Equals(child.Name, "Key", StringComparison.OrdinalIgnoreCase))
                            {
                                keyText = child.Value.Trim();
                            }
                            else if (string.Equals(child.Name, "Value", StringComparison.OrdinalIgnoreCase))
                            {
                                valueText = child.Value.Trim();
                            }
                        }

                        if (!string.IsNullOrEmpty(keyText))
                        {
                            var flatKey = propName + "." + keyText;
                            AddFlatValue(flat, flatKey, valueText);
                        }
                    }

                    continue;
                }

                // Array-of-primitive or nested object
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

                // Array-of-primitive: IntList.int, StringList.string, etc.
                if (sameName)
                {
                    var elementTag = firstName;                  // "int", "string", "float", ...
                    var flatKey = propName + "." + elementTag;   // e.g. "IntList.int"

                    for (int i = 0; i < children.Count; i++)
                    {
                        AddFlatValue(flat, flatKey, children[i].Value.Trim());
                    }
                }
                else
                {
                    // Nested object: Nested.Threshold, Nested.Flag, ...
                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        var key = propName + "." + child.Name;
                        AddFlatValue(flat, key, child.Value.Trim());
                    }
                }
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
        
        // helper near the top of the class (private static)
        private static bool IsPrimitiveElementTag(string tag)
        {
            if (tag == null)
                return false;

            tag = tag.ToLowerInvariant();
            return tag == "int" || tag == "string" || tag == "float" || tag == "double" || tag == "bool";
        }

        public string ToInternal(IConfigDefinition definition, string externalContent)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var xmlSerializer = GetXmlSerializer(_backupXmlSerializer);

            // Empty file => default instance XML
            if (string.IsNullOrEmpty(externalContent))
            {
                var defaultInstanceEmpty = definition.CreateDefaultInstance();
                return xmlSerializer.SerializeToXml(defaultInstanceEmpty);
            }

            // Dictionary detection based on VariableDescriptions
            ConfigBase defaultInstance = definition.CreateDefaultInstance();
            IReadOnlyDictionary<string, string> descriptions = null;
            HashSet<string> dictionaryParents = null;

            if (defaultInstance != null)
            {
                descriptions = defaultInstance.VariableDescriptions;
                dictionaryParents = DetectDictionaryParents(descriptions);
            }
            else
            {
                dictionaryParents = new HashSet<string>();
            }

            // Parse TOML into key -> list of raw values
            var flat = ParseTomlToFlatMap(externalContent);

            // Split into:
            // - root scalars / arrays: key
            // - nested objects: Parent.Child
            // - dictionary entries: DictParent.DictKey
            var rootMap = new Dictionary<string, List<string>>();
            var nested = new Dictionary<string, Dictionary<string, string>>();
            var dictionaries = new Dictionary<string, Dictionary<string, string>>();

            foreach (var kv in flat)
            {
                var key = kv.Key;
                var list = kv.Value ?? new List<string>();

                var segments = key.Split('.');
                if (segments.Length == 2 &&
                    !dictionaryParents.Contains(segments[0]) &&
                    IsPrimitiveElementTag(segments[1]))
                {
                    // Case: IntArray.int, Names.string, etc.
                    // Treat as an array for the parent property (IntArray / Names).
                    var parent = segments[0];

                    List<string> arr;
                    if (!rootMap.TryGetValue(parent, out arr))
                    {
                        arr = new List<string>();
                        rootMap[parent] = arr;
                    }

                    // Append all values from TOML array to this parent list
                    arr.AddRange(list);
                    continue;
                }

                if (segments.Length == 1)
                {
                    // Root scalar or array (simple key)
                    rootMap[key] = list;
                }
                else
                {
                    var parent = segments[0];
                    var child = segments[1];
                    var val = list.Count > 0 ? list[0] : string.Empty;

                    if (dictionaryParents.Contains(parent))
                    {
                        // Dictionary<string,int>: NamedValues.start, NamedValues.end, ...
                        Dictionary<string, string> dict;
                        if (!dictionaries.TryGetValue(parent, out dict))
                        {
                            dict = new Dictionary<string, string>();
                            dictionaries[parent] = dict;
                        }
                        dict[child] = val;
                    }
                    else
                    {
                        // Normal nested object: Nested.Threshold, Nested.Flag, ...
                        Dictionary<string, string> childMap;
                        if (!nested.TryGetValue(parent, out childMap))
                        {
                            childMap = new Dictionary<string, string>();
                            nested[parent] = childMap;
                        }
                        childMap[child] = val;
                    }
                }
            }

            var sb = new StringBuilder();

            // XML declaration + namespaces so xsi:nil works correctly
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n");
            sb.Append('<').Append(definition.TypeName)
              .Append(" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"")
              .Append(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"")
              .Append('>');

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
                    var val = list[0];

                    // Null sentinel -> xsi:nil="true"
                    if (string.Equals(val, NULL_SENTINEL, StringComparison.Ordinal))
                    {
                        sb.Append('<').Append(name).Append(" xsi:nil=\"true\" />");
                    }
                    else
                    {
                        // Scalar
                        sb.Append('<').Append(name).Append('>');
                        sb.Append(XmlEscape(val));
                        sb.Append("</").Append(name).Append('>');
                    }
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

            // Dictionary parents -> SerializableDictionary XML shape
            foreach (var kd in dictionaries)
            {
                var parentName = kd.Key;
                var dict = kd.Value;

                sb.Append('<').Append(parentName).Append('>');
                sb.Append("<dictionary>");

                foreach (var entry in dict)
                {
                    sb.Append("<item>");
                    sb.Append("<Key>").Append(XmlEscape(entry.Key)).Append("</Key>");

                    // Dictionary values are not nullable in our current use-case,
                    // treat NullSentinel as normal text if ever present.
                    sb.Append("<Value>").Append(XmlEscape(entry.Value)).Append("</Value>");
                    sb.Append("</item>");
                }

                sb.Append("</dictionary>");
                sb.Append("</").Append(parentName).Append('>');
            }

            // Nested objects
            foreach (var kn in nested)
            {
                var parentName = kn.Key;
                var childMap = kn.Value;

                sb.Append('<').Append(parentName).Append('>');
                foreach (var kvChild in childMap)
                {
                    var val = kvChild.Value;
                    if (string.Equals(val, NULL_SENTINEL, StringComparison.Ordinal))
                    {
                        sb.Append('<').Append(kvChild.Key).Append(" xsi:nil=\"true\" />");
                    }
                    else
                    {
                        sb.Append('<').Append(kvChild.Key).Append('>');
                        sb.Append(XmlEscape(val));
                        sb.Append("</").Append(kvChild.Key).Append('>');
                    }
                }
                sb.Append("</").Append(parentName).Append('>');
            }

            sb.Append("</").Append(definition.TypeName).Append('>');
            return sb.ToString();
        }

        // ====================================================================
        // Helpers: dictionary detection via VariableDescriptions
        // ====================================================================

        private static HashSet<string> DetectDictionaryParents(IReadOnlyDictionary<string, string> descriptions)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);

            if (descriptions == null)
                return set;

            foreach (var kv in descriptions)
            {
                var name = kv.Key;
                var desc = kv.Value;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(desc))
                    continue;

                // Only consider top-level properties (no dots)
                if (name.IndexOf('.') >= 0)
                    continue;

                var lower = desc.ToLowerInvariant();
                if (lower.Contains("dictionary<") || lower.StartsWith("dictionary") || lower.Contains("dictionary"))
                {
                    set.Add(name);
                }
            }

            return set;
        }

        // ====================================================================
        // TOML scalar/array helpers
        // ====================================================================

        private static string ToTomlLiteral(string raw)
        {
            if (raw == null)
                raw = string.Empty;

            // Null sentinel -> TOML null (bare token)
            if (string.Equals(raw, NULL_SENTINEL, StringComparison.Ordinal))
                return "null";

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

            // TOML null -> our NullSentinel
            if (string.Equals(s, "null", StringComparison.OrdinalIgnoreCase))
                return NULL_SENTINEL;

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
                    bool hasNil = startTagContent.IndexOf("xsi:nil=\"true\"", StringComparison.Ordinal) >= 0;
                    result[tagName] = hasNil ? NULL_SENTINEL : string.Empty;
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
                    bool hasNil = tagContent.IndexOf("xsi:nil=\"true\"", StringComparison.Ordinal) >= 0;
                    result.Add(new SnippetChild
                    {
                        Name = tagName,
                        Value = hasNil ? NULL_SENTINEL : string.Empty
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
