using System;
using System.Collections.Generic;
using System.Text;

namespace mz.Config.Core
{
    internal static class SimpleXml
    {
        public static Dictionary<string, string> ParseSimpleElements(string xml)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(xml))
                return result;

            var len = xml.Length;
            var pos = 0;

            string rootName = null;
            var rootStartClose = -1;
            var endRootIndex = -1;

            // Find the real root element, skipping declarations/comments/etc.
            while (pos < len && rootName == null)
            {
                var lt = xml.IndexOf('<', pos);
                if (lt < 0)
                    return result;

                var gt = xml.IndexOf('>', lt + 1);
                if (gt < 0)
                    return result;

                var tagContent = xml.Substring(lt + 1, gt - lt - 1).Trim();
                if (tagContent.Length == 0)
                {
                    pos = gt + 1;
                    continue;
                }

                var first = tagContent[0];
                // Skip xml declarations, comments, closing tags etc.
                if (first == '?' || first == '!' || first == '/')
                {
                    pos = gt + 1;
                    continue;
                }

                var spaceIndex = tagContent.IndexOf(' ');
                rootName = spaceIndex >= 0
                    ? tagContent.Substring(0, spaceIndex)
                    : tagContent;

                rootStartClose = gt;

                var endRootTag = "</" + rootName + ">";
                endRootIndex = xml.LastIndexOf(endRootTag, StringComparison.Ordinal);
                if (endRootIndex < 0)
                {
                    // malformed, give up
                    return result;
                }
            }

            if (rootName == null || rootStartClose < 0 || endRootIndex <= rootStartClose)
                return result;

            // Inner content between <root ...> and </root>
            var inner = xml.Substring(rootStartClose + 1, endRootIndex - (rootStartClose + 1));

            // Parse direct child elements from inner
            var innerLen = inner.Length;
            var innerPos = 0;

            while (true)
            {
                var startTagOpen = inner.IndexOf('<', innerPos);
                if (startTagOpen < 0 || startTagOpen >= innerLen)
                    break;

                var startTagClose = inner.IndexOf('>', startTagOpen + 1);
                if (startTagClose < 0)
                    break;

                var startTagContent = inner.Substring(startTagOpen + 1, startTagClose - startTagOpen - 1).Trim();
                if (startTagContent.Length == 0)
                {
                    innerPos = startTagClose + 1;
                    continue;
                }

                var first = startTagContent[0];
                if (first == '?' || first == '!' || first == '/')
                {
                    innerPos = startTagClose + 1;
                    continue;
                }

                // Extract tag name before first space or slash
                var spaceIndex = startTagContent.IndexOf(' ');
                var slashIndex = startTagContent.IndexOf('/');

                var endIndex = startTagContent.Length;
                if (slashIndex >= 0 && slashIndex < endIndex)
                    endIndex = slashIndex;
                if (spaceIndex >= 0 && spaceIndex < endIndex)
                    endIndex = spaceIndex;

                var tagName = startTagContent.Substring(0, endIndex);

                // Handle self-closing tags (e.g. <OptionalValue xsi:nil="true" />)
                var selfClosing = startTagContent.Length > 0 &&
                                  startTagContent[startTagContent.Length - 1] == '/';

                if (selfClosing)
                {
                    // Represent as empty string value
                    result[tagName] = string.Empty;
                    innerPos = startTagClose + 1;
                    continue;
                }

                // Normal <Tag>...</Tag>
                var endTag = "</" + tagName + ">";
                var endTagIndex = inner.IndexOf(endTag, startTagClose + 1, StringComparison.Ordinal);
                if (endTagIndex < 0)
                {
                    innerPos = startTagClose + 1;
                    continue;
                }

                var innerValueStart = startTagClose + 1;
                var innerText = inner.Substring(innerValueStart, endTagIndex - innerValueStart);

                var value = Unescape(innerText.Trim());
                result[tagName] = value;

                innerPos = endTagIndex + endTag.Length;
            }

            return result;
        }

        public static string BuildSimpleXml(string rootName, IDictionary<string, string> values)
        {
            var sb = new StringBuilder();
            sb.Append('<').Append(rootName).Append('>').AppendLine();

            foreach (var kv in values)
            {
                sb.Append("  <").Append(kv.Key).Append('>');
                sb.Append(Escape(kv.Value));
                sb.Append("</").Append(kv.Key).Append('>').AppendLine();
            }

            sb.Append("</").Append(rootName).Append('>');
            return sb.ToString();
        }

        private static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return s
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'");
        }

        private static string Escape(string s)
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
    }
}
