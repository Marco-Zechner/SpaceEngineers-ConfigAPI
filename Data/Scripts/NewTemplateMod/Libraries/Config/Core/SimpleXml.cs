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

            int pos = 0;
            int len = xml.Length;

            while (true)
            {
                int startTagOpen = xml.IndexOf('<', pos);
                if (startTagOpen < 0 || startTagOpen >= len)
                    break;

                int startTagClose = xml.IndexOf('>', startTagOpen + 1);
                if (startTagClose < 0)
                    break;

                string startTagContent = xml.Substring(startTagOpen + 1, startTagClose - startTagOpen - 1).Trim();

                if (startTagContent.Length == 0 ||
                    startTagContent[0] == '?' ||
                    startTagContent[0] == '!' ||
                    startTagContent[0] == '/')
                {
                    pos = startTagClose + 1;
                    continue;
                }

                int spaceIndex = startTagContent.IndexOf(' ');
                string tagName = (spaceIndex >= 0)
                    ? startTagContent.Substring(0, spaceIndex)
                    : startTagContent;

                string endTag = "</" + tagName + ">";
                int endTagIndex = xml.IndexOf(endTag, startTagClose + 1, StringComparison.Ordinal);
                if (endTagIndex < 0)
                {
                    pos = startTagClose + 1;
                    continue;
                }

                int innerStart = startTagClose + 1;
                string innerText = xml.Substring(innerStart, endTagIndex - innerStart);

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
