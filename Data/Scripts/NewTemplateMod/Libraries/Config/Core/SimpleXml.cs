using System;
using System.Collections.Generic;
using System.Text;

namespace mz.Config.Core
{
    internal static class SimpleXml
    {
        public static Dictionary<string, string> ParseSimpleElements(string xml)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(xml))
                return result;

            // Find root tag
            int lt = xml.IndexOf('<');
            if (lt < 0)
                return result;

            int gt = xml.IndexOf('>', lt + 1);
            if (gt < 0)
                return result;

            string rootTagContent = xml.Substring(lt + 1, gt - lt - 1).Trim();
            if (rootTagContent.Length == 0)
                return result;

            int spaceIndex = rootTagContent.IndexOf(' ');
            string rootName = (spaceIndex >= 0)
                ? rootTagContent.Substring(0, spaceIndex)
                : rootTagContent;

            string endRootTag = "</" + rootName + ">";
            int endRootIndex = xml.LastIndexOf(endRootTag, StringComparison.Ordinal);
            if (endRootIndex < 0)
                return result;

            int innerStart = gt + 1;
            string inner = xml.Substring(innerStart, endRootIndex - innerStart);

            // Now parse only child elements inside the root
            int pos = 0;
            int len = inner.Length;

            while (true)
            {
                int startTagOpen = inner.IndexOf('<', pos);
                if (startTagOpen < 0 || startTagOpen >= len)
                    break;

                int startTagClose = inner.IndexOf('>', startTagOpen + 1);
                if (startTagClose < 0)
                    break;

                string startTagContent = inner.Substring(startTagOpen + 1, startTagClose - startTagOpen - 1).Trim();

                if (startTagContent.Length == 0 ||
                    startTagContent[0] == '?' ||
                    startTagContent[0] == '!' ||
                    startTagContent[0] == '/')
                {
                    pos = startTagClose + 1;
                    continue;
                }

                spaceIndex = startTagContent.IndexOf(' ');
                string tagName = (spaceIndex >= 0)
                    ? startTagContent.Substring(0, spaceIndex)
                    : startTagContent;

                string endTag = "</" + tagName + ">";
                int endTagIndex = inner.IndexOf(endTag, startTagClose + 1, StringComparison.Ordinal);
                if (endTagIndex < 0)
                {
                    pos = startTagClose + 1;
                    continue;
                }

                int innerValueStart = startTagClose + 1;
                string innerText = inner.Substring(innerValueStart, endTagIndex - innerValueStart);

                string value = Unescape(innerText.Trim());
                result[tagName] = value;

                pos = endTagIndex + endTag.Length;
            }

            return result;
        }

        public static string BuildSimpleXml(string rootName, IDictionary<string, string> values)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('<').Append(rootName).Append('>');

            foreach (KeyValuePair<string, string> kv in values)
            {
                sb.Append('<').Append(kv.Key).Append('>');
                sb.Append(Escape(kv.Value));
                sb.Append("</").Append(kv.Key).Append('>');
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
