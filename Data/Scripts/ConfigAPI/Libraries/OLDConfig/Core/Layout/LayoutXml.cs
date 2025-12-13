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

            var len = xml.Length;
            var pos = 0;

            // 1) Find root element (skip xml decl, comments, etc.)
            while (pos < len)
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
                if (first == '?' || first == '!' || first == '/')
                {
                    pos = gt + 1;
                    continue;
                }

                var spaceIdx = tagContent.IndexOf(' ');
                rootName = spaceIdx >= 0
                    ? tagContent.Substring(0, spaceIdx)
                    : tagContent;

                pos = gt + 1;
                break;
            }

            if (string.IsNullOrEmpty(rootName))
                return result;

            var endRootTag = "</" + rootName + ">";
            var endIndex = xml.LastIndexOf(endRootTag, StringComparison.Ordinal);
            if (endIndex < 0 || endIndex <= pos)
                return result;

            // Inner content between <root ...> and </root>
            var inner = xml.Substring(pos, endIndex - pos);
            var innerLen = inner.Length;
            var i = 0;

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

                var startTagOpen = i;
                var startTagClose = inner.IndexOf('>', startTagOpen + 1);
                if (startTagClose < 0)
                    break;

                var startContent = inner.Substring(startTagOpen + 1, startTagClose - startTagOpen - 1).Trim();
                if (startContent.Length == 0)
                {
                    i = startTagClose + 1;
                    continue;
                }

                var c0 = startContent[0];
                if (c0 == '?' || c0 == '!' || c0 == '/')
                {
                    i = startTagClose + 1;
                    continue;
                }

                var spaceIndex = startContent.IndexOf(' ');
                var childName = spaceIndex >= 0
                    ? startContent.Substring(0, spaceIndex)
                    : startContent;

                var selfClosing = startContent.Length > 0 &&
                                  startContent[startContent.Length - 1] == '/';

                if (selfClosing)
                {
                    var childEnd = startTagClose + 1;
                    var block = inner.Substring(startTagOpen, childEnd - startTagOpen);
                    result[childName] = block;
                    i = childEnd;
                    continue;
                }

                // Non self-closing: find matching </childName> with depth tracking
                var depth = 1;
                var searchPos = startTagClose + 1;

                while (searchPos < innerLen && depth > 0)
                {
                    var nextLt = inner.IndexOf('<', searchPos);
                    if (nextLt < 0)
                        break;

                    var nextGt = inner.IndexOf('>', nextLt + 1);
                    if (nextGt < 0)
                        break;

                    var t = inner.Substring(nextLt + 1, nextGt - nextLt - 1).Trim();
                    if (t.Length == 0)
                    {
                        searchPos = nextGt + 1;
                        continue;
                    }

                    var t0 = t[0];
                    if (t0 == '!' || t0 == '?')
                    {
                        searchPos = nextGt + 1;
                        continue;
                    }

                    var closing = t0 == '/';
                    string name;

                    if (closing)
                    {
                        name = t.Substring(1);
                        var sp = name.IndexOf(' ');
                        if (sp >= 0)
                            name = name.Substring(0, sp);
                    }
                    else
                    {
                        var sp = t.IndexOf(' ');
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
                        var childEnd = nextGt + 1;
                        var block = inner.Substring(startTagOpen, childEnd - startTagOpen);
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
                var firstLineEmitted = false;

                for (var i = 0; i < lines.Length; i++)
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
