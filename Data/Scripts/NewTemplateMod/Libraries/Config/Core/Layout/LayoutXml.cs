using System;
using System.Collections.Generic;
using System.Text;

namespace mz.Config.Core
{
    /// <summary>
    /// XML layout helper:
    /// - Parse direct children of the root into raw XML fragments.
    /// - Build a canonical document with stable, idempotent indentation.
    ///
    /// No reflection, no XmlReader. Pure string parsing.
    /// </summary>
    public static class LayoutXml
    {
        private const string XML_DECL = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
        private const string XSD_NS = "http://www.w3.org/2001/XMLSchema";
        private const string XSI_NS = "http://www.w3.org/2001/XMLSchema-instance";
        private const string ROOT_INDENT = "  ";

        /// <summary>
        /// Extracts direct child elements of the root as raw XML blocks.
        /// Each value starts with '&lt;ChildName&gt;' (no leading spaces)
        /// and ends at the matching closing tag.
        /// </summary>
        public static Dictionary<string, string> ParseChildren(string xml, out string rootName)
        {
            var result = new Dictionary<string, string>();
            rootName = string.Empty;

            if (string.IsNullOrEmpty(xml))
                return result;

            int len = xml.Length;
            int pos = 0;

            // 1) Find root element (skip xml decl, comments, etc.)
            while (pos < len)
            {
                int lt = xml.IndexOf('<', pos);
                if (lt < 0)
                    return result;

                int gt = xml.IndexOf('>', lt + 1);
                if (gt < 0)
                    return result;

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

                int spaceIdx = tagContent.IndexOf(' ');
                rootName = spaceIdx >= 0
                    ? tagContent.Substring(0, spaceIdx)
                    : tagContent;

                pos = gt + 1;
                break;
            }

            if (string.IsNullOrEmpty(rootName))
                return result;

            string endRootTag = "</" + rootName + ">";
            int endIndex = xml.LastIndexOf(endRootTag, StringComparison.Ordinal);
            if (endIndex < 0 || endIndex <= pos)
                return result;

            // Inner content between <root ...> and </root>
            string inner = xml.Substring(pos, endIndex - pos);
            int innerLen = inner.Length;
            int i = 0;

            // 2) Scan inner for direct children
            while (i < innerLen)
            {
                // Skip whitespace
                while (i < innerLen && char.IsWhiteSpace(inner[i]))
                    i++;

                if (i >= innerLen)
                    break;

                if (inner[i] != '<')
                {
                    i++;
                    continue;
                }

                int startTagOpen = i;
                int startTagClose = inner.IndexOf('>', startTagOpen + 1);
                if (startTagClose < 0)
                    break;

                string startContent = inner.Substring(startTagOpen + 1, startTagClose - startTagOpen - 1).Trim();
                if (startContent.Length == 0)
                {
                    i = startTagClose + 1;
                    continue;
                }

                char c0 = startContent[0];
                if (c0 == '?' || c0 == '!' || c0 == '/')
                {
                    i = startTagClose + 1;
                    continue;
                }

                int spaceIndex = startContent.IndexOf(' ');
                string childName = spaceIndex >= 0
                    ? startContent.Substring(0, spaceIndex)
                    : startContent;

                bool selfClosing = startContent.Length > 0 &&
                                   startContent[startContent.Length - 1] == '/';

                if (selfClosing)
                {
                    int childEnd = startTagClose + 1;
                    string block = inner.Substring(startTagOpen, childEnd - startTagOpen);
                    result[childName] = block;
                    i = childEnd;
                    continue;
                }

                // Non self-closing: find matching </childName> with depth tracking
                int depth = 1;
                int searchPos = startTagClose + 1;

                while (searchPos < innerLen && depth > 0)
                {
                    int nextLt = inner.IndexOf('<', searchPos);
                    if (nextLt < 0)
                        break;

                    int nextGt = inner.IndexOf('>', nextLt + 1);
                    if (nextGt < 0)
                        break;

                    string t = inner.Substring(nextLt + 1, nextGt - nextLt - 1).Trim();
                    if (t.Length == 0)
                    {
                        searchPos = nextGt + 1;
                        continue;
                    }

                    char t0 = t[0];
                    if (t0 == '!' || t0 == '?')
                    {
                        searchPos = nextGt + 1;
                        continue;
                    }

                    bool closing = t0 == '/';
                    string name;

                    if (closing)
                    {
                        name = t.Substring(1);
                        int sp = name.IndexOf(' ');
                        if (sp >= 0)
                            name = name.Substring(0, sp);
                    }
                    else
                    {
                        int sp = t.IndexOf(' ');
                        name = sp >= 0 ? t.Substring(0, sp) : t;
                    }

                    if (!closing && name == childName)
                    {
                        depth++;
                    }
                    else if (closing && name == childName)
                    {
                        depth--;
                    }

                    searchPos = nextGt + 1;

                    if (depth == 0)
                    {
                        int childEnd = nextGt + 1;
                        string block = inner.Substring(startTagOpen, childEnd - startTagOpen);
                        result[childName] = block;
                        i = childEnd;
                        break;
                    }
                }

                if (depth > 0)
                {
                    // malformed; stop scanning to avoid weird slices
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Build a full, canonical XML doc:
        /// - Fixed xml declaration + namespaces.
        /// - Root children indented by 2 spaces.
        /// - For each child block, only the *first* line gets root indent;
        ///   inner lines keep their existing indentation.
        /// This makes Build ∘ ParseChildren idempotent.
        /// </summary>
        public static string Build(string rootName, IDictionary<string, string> children)
        {
            var sb = new StringBuilder();

            sb.Append(XML_DECL).Append("\r\n");
            sb.Append('<').Append(rootName)
              .Append(" xmlns:xsd=\"").Append(XSD_NS).Append('"')
              .Append(" xmlns:xsi=\"").Append(XSI_NS).Append('"')
              .Append('>')
              .Append("\r\n");

            foreach (var kv in children)
            {
                var block = kv.Value ?? string.Empty;

                // Normalize newlines for internal processing
                block = block.Replace("\r\n", "\n");

                var lines = block.Split(new[] { '\n' }, StringSplitOptions.None);
                bool firstLineEmitted = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // Skip completely empty / whitespace-only lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Normalize trailing whitespace, keep leading as-is
                    var trimmedEnd = line.TrimEnd('\r', ' ', '\t');

                    if (!firstLineEmitted)
                    {
                        // First line of the block: add root indent.
                        // Note: by construction from ParseChildren, this line starts at '<'
                        // without leading spaces.
                        sb.Append(ROOT_INDENT);
                        sb.Append(trimmedEnd);
                        sb.Append("\r\n");
                        firstLineEmitted = true;
                    }
                    else
                    {
                        // Inner lines: keep existing indentation exactly.
                        sb.Append(trimmedEnd);
                        sb.Append("\r\n");
                    }
                }
            }

            sb.Append("</").Append(rootName).Append('>');
            return sb.ToString();
        }
    }
}
