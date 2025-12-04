using System;
using System.Collections.Generic;
using System.Text;

namespace mz.Config.Core
{
    internal static class LayoutXml
    {
        /// <summary>
        /// Parse an XML document into:
        /// - rootName   : the root element name (e.g. "IntermediateConfig")
        /// - children   : map from immediate child element name to the full element XML
        ///                (e.g. "OptionalValue" -> "&lt;OptionalValue xsi:nil=\"true\" /&gt;").
        /// 
        /// Only the first occurrence of a child name is kept. This matches the
        /// "simple property per element" layout we have for configs.
        /// </summary>
        public static Dictionary<string, string> ParseChildren(
            string xml,
            out string rootName)
        {
            rootName = string.Empty;
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(xml))
                return result;

            int len = xml.Length;
            int pos = 0;

            // Skip BOM/whitespace
            while (pos < len && char.IsWhiteSpace(xml[pos]))
                pos++;

            // Skip XML declaration if present: <?xml ... ?>
            if (pos + 5 < len && xml[pos] == '<' && xml[pos + 1] == '?')
            {
                int declEnd = xml.IndexOf("?>", pos, StringComparison.Ordinal);
                if (declEnd >= 0)
                    pos = declEnd + 2;
            }

            // Skip whitespace after declaration
            while (pos < len && char.IsWhiteSpace(xml[pos]))
                pos++;

            // Find root start tag
            int rootStart = xml.IndexOf('<', pos);
            if (rootStart < 0)
                return result;

            int rootTagEnd = xml.IndexOf('>', rootStart + 1);
            if (rootTagEnd < 0)
                return result;

            var rootTagContent = xml.Substring(rootStart + 1, rootTagEnd - rootStart - 1).Trim();
            if (rootTagContent.Length == 0)
                return result;

            // Root name is up to first whitespace or '/'
            int spaceIdx = rootTagContent.IndexOf(' ');
            int slashIdx = rootTagContent.IndexOf('/');
            int endIdx = rootTagContent.Length;

            if (spaceIdx >= 0 && spaceIdx < endIdx)
                endIdx = spaceIdx;
            if (slashIdx >= 0 && slashIdx < endIdx)
                endIdx = slashIdx;

            rootName = rootTagContent.Substring(0, endIdx);

            // Find closing root tag
            string closeRootTag = "</" + rootName + ">";
            int rootCloseIndex = xml.LastIndexOf(closeRootTag, StringComparison.Ordinal);
            if (rootCloseIndex < 0)
                return result;

            int innerStart = rootTagEnd + 1;
            int innerLen = rootCloseIndex - innerStart;
            if (innerLen <= 0)
                return result;

            string inner = xml.Substring(innerStart, innerLen);
            int innerPos = 0;
            int innerTotal = inner.Length;

            while (innerPos < innerTotal)
            {
                // Skip whitespace
                while (innerPos < innerTotal && char.IsWhiteSpace(inner[innerPos]))
                    innerPos++;

                if (innerPos >= innerTotal)
                    break;

                int lt = inner.IndexOf('<', innerPos);
                if (lt < 0)
                    break;

                // Skip any stray text before '<'
                if (lt > innerPos)
                    innerPos = lt;

                if (innerPos >= innerTotal)
                    break;

                int tagEnd = inner.IndexOf('>', innerPos + 1);
                if (tagEnd < 0)
                    break;

                string tagContent = inner.Substring(innerPos + 1, tagEnd - innerPos - 1).Trim();
                if (tagContent.Length == 0)
                {
                    innerPos = tagEnd + 1;
                    continue;
                }

                char first = tagContent[0];
                // Skip comments, declarations, closing tags inside root
                if (first == '?' || first == '!' || first == '/')
                {
                    innerPos = tagEnd + 1;
                    continue;
                }

                // Child tag name
                int childSpace = tagContent.IndexOf(' ');
                int childSlash = tagContent.IndexOf('/');
                int childEnd = tagContent.Length;

                if (childSpace >= 0 && childSpace < childEnd)
                    childEnd = childSpace;
                if (childSlash >= 0 && childSlash < childEnd)
                    childEnd = childSlash;

                string childName = tagContent.Substring(0, childEnd);

                bool selfClosing = tagContent.EndsWith("/", StringComparison.Ordinal);

                if (selfClosing)
                {
                    // Entire child is just this tag
                    int childEndIndex = tagEnd;
                    string childXml = inner.Substring(innerPos, childEndIndex - innerPos + 1);

                    if (!result.ContainsKey(childName))
                        result.Add(childName, childXml);

                    innerPos = childEndIndex + 1;
                    continue;
                }
                else
                {
                    // Need to find matching closing tag </childName>
                    string closeTag = "</" + childName + ">";
                    int closeIndex = inner.IndexOf(closeTag, tagEnd + 1, StringComparison.Ordinal);
                    if (closeIndex < 0)
                    {
                        // malformed; stop parsing further
                        break;
                    }

                    int childEndIndex = closeIndex + closeTag.Length - 1;
                    string childXml = inner.Substring(innerPos, childEndIndex - innerPos + 1);

                    if (!result.ContainsKey(childName))
                        result.Add(childName, childXml);

                    innerPos = childEndIndex + 1;
                    continue;
                }
            }

            return result;
        }

        /// <summary>
        /// Build a nicely formatted XML document:
        /// - Adds XML declaration
        /// - Adds xmlns:xsd and xmlns:xsi on the root
        /// - Indents each child element by two spaces
        /// 
        /// Children are given as full element XML snippets (&lt;Key&gt;...&lt;/Key&gt;,
        /// &lt;OptionalValue xsi:nil="true" /&gt;, &lt;Tags&gt;...&lt;/Tags&gt;, etc.).
        /// </summary>
        public static string Build(string rootName, IDictionary<string, string> children)
        {
            if (rootName == null)
                rootName = string.Empty;

            var nl = Environment.NewLine;
            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"utf-16\"?>").Append(nl);
            sb.Append('<').Append(rootName)
              .Append(" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"")
              .Append(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">")
              .Append(nl);

            if (children != null)
            {
                foreach (var kv in children)
                {
                    var childXml = kv.Value ?? string.Empty;

                    // Split existing child XML into lines and indent each line by two spaces.
                    var lines = childXml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (line.Length == 0)
                        {
                            sb.Append("  ").Append(nl);
                        }
                        else
                        {
                            sb.Append("  ").Append(line).Append(nl);
                        }
                    }
                }
            }

            sb.Append("</").Append(rootName).Append('>');
            return sb.ToString();
        }
    }
}
