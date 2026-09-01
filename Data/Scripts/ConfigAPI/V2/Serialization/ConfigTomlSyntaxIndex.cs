using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.V2.Domain;
using Mz.Toml;

namespace MarcoZechner.ConfigAPI.V2.Serialization
{
    public sealed class ConfigTomlSyntaxAssignment
    {
        public ConfigValuePath Path { get; }
        public TomlSyntaxNode Node { get; }

        public bool IsDisabled => Node.Kind == TomlSyntaxNodeKind.DisabledAssignment;

        internal ConfigTomlSyntaxAssignment(ConfigValuePath path, TomlSyntaxNode node)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Path = path;
            Node = node;
        }
    }

    public sealed class ConfigTomlSyntaxIndex
    {
        private const string PROBE_KEY = "__configapi_path_probe__";
        private const long PROBE_VALUE = 584321;

        private readonly ConfigTomlSyntaxAssignment[] _assignments;
        private readonly IReadOnlyList<ConfigTomlSyntaxAssignment> _readOnlyAssignments;
        private readonly TomlSyntaxNode[] _unaddressableAssignments;
        private readonly IReadOnlyList<TomlSyntaxNode> _readOnlyUnaddressableAssignments;

        public IReadOnlyList<ConfigTomlSyntaxAssignment> Assignments => _readOnlyAssignments;
        public IReadOnlyList<TomlSyntaxNode> UnaddressableAssignments => _readOnlyUnaddressableAssignments;

        private ConfigTomlSyntaxIndex(
            IList<ConfigTomlSyntaxAssignment> assignments,
            IList<TomlSyntaxNode> unaddressableAssignments)
        {
            _assignments = new ConfigTomlSyntaxAssignment[assignments.Count];
            for (var i = 0; i < assignments.Count; i++)
                _assignments[i] = assignments[i];

            _readOnlyAssignments = Array.AsReadOnly(_assignments);

            _unaddressableAssignments = new TomlSyntaxNode[unaddressableAssignments.Count];
            for (var i = 0; i < unaddressableAssignments.Count; i++)
                _unaddressableAssignments[i] = unaddressableAssignments[i];

            _readOnlyUnaddressableAssignments = Array.AsReadOnly(_unaddressableAssignments);
        }

        public static ConfigTomlSyntaxIndex Create(TomlParseResult parseResult)
        {
            if (parseResult == null)
                throw new ArgumentNullException(nameof(parseResult));

            if (!parseResult.IsSuccess || parseResult.Syntax == null)
            {
                throw new ArgumentException(
                    "A successful TOML parse with source syntax is required.",
                    nameof(parseResult));
            }

            var assignments = new List<ConfigTomlSyntaxAssignment>();
            var unaddressableAssignments = new List<TomlSyntaxNode>();
            var arrayTablePaths = new List<string[]>();

            var currentTablePath = new string[0];
            var currentTableTraversesArray = false;
            var syntax = parseResult.Syntax;

            for (var i = 0; i < syntax.Nodes.Count; i++)
            {
                var node = syntax.Nodes[i];

                switch (node.Kind)
                {
                    case TomlSyntaxNodeKind.TableHeader:
                        currentTablePath = ReadHeaderPath(syntax, node);
                        currentTableTraversesArray = TraversesArrayTable(currentTablePath, arrayTablePaths);
                        break;

                    case TomlSyntaxNodeKind.ArrayTableHeader:
                        currentTablePath = ReadHeaderPath(syntax, node);
                        arrayTablePaths.Add(Copy(currentTablePath));
                        currentTableTraversesArray = true;
                        break;

                    case TomlSyntaxNodeKind.Assignment:
                    case TomlSyntaxNodeKind.DisabledAssignment:
                        if (currentTableTraversesArray)
                        {
                            unaddressableAssignments.Add(node);
                            break;
                        }

                        var localPath = ReadAssignmentPath(syntax, node);
                        var completePath = Combine(currentTablePath, localPath);

                        assignments.Add(
                            new ConfigTomlSyntaxAssignment(
                                CreateConfigValuePath(completePath),
                                node));
                        break;
                }
            }

            return new ConfigTomlSyntaxIndex(assignments, unaddressableAssignments);
        }

        private static ConfigValuePath CreateConfigValuePath(string[] segments)
        {
            try
            {
                return new ConfigValuePath(segments);
            }
            catch (ArgumentException exception)
            {
                throw new NotSupportedException(
                    "The TOML assignment path contains a segment that ConfigValuePath cannot represent.",
                    exception);
            }
        }

        private static string[] ReadAssignmentPath(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        {
            if (!node.ValueSpan.HasValue)
                throw new InvalidOperationException("Assignment syntax node has no value span.");

            var statement = syntax.Source.Substring(node.Span.Start, node.Span.Length);
            var valueSpan = node.ValueSpan.Value;
            var relativeValueStart = valueSpan.Start - node.Span.Start;

            statement = statement
                .Remove(relativeValueStart, valueSpan.Length)
                .Insert(relativeValueStart, "0");

            if (node.Kind == TomlSyntaxNodeKind.DisabledAssignment)
            {
                if (!statement.StartsWith("#!", StringComparison.Ordinal))
                    throw new InvalidOperationException("Disabled assignment is missing the expected '#!' marker.");

                statement = statement.Remove(0, 2);
            }

            return ReadSingleAssignmentPath(Toml.Parse(statement).Root);
        }

        private static string[] ReadSingleAssignmentPath(TomlTable root)
        {
            var path = new List<string>();
            var table = root;

            while (true)
            {
                if (table.Count != 1)
                    throw new InvalidOperationException("Synthetic TOML assignment did not produce exactly one path.");

                var key = table.Keys[0];
                var node = table[key];

                path.Add(key);

                if (node.Kind == TomlNodeKind.Value)
                    return path.ToArray();

                if (node.Kind != TomlNodeKind.Table)
                    throw new InvalidOperationException("Synthetic TOML assignment produced an unexpected node kind.");

                table = (TomlTable)node;
            }
        }

        private static string[] ReadHeaderPath(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        {
            var statement = syntax.Source.Substring(node.Span.Start, node.Span.Length);
            var synthetic = statement + "\n" + PROBE_KEY + " = " + PROBE_VALUE + "\n";
            var parsed = Toml.Parse(synthetic);

            string[] path;
            if (!TryFindProbePath(parsed.Root, new List<string>(), out path))
                throw new InvalidOperationException("Synthetic TOML header did not resolve the probe path.");

            return path;
        }

        private static bool TryFindProbePath(TomlTable table, List<string> path, out string[] result)
        {
            foreach (var pair in table)
            {
                var value = pair.Value as TomlValue;

                if (string.Equals(pair.Key, PROBE_KEY, StringComparison.Ordinal) &&
                    value != null &&
                    value.ValueKind == TomlValueKind.Integer &&
                    value.AsInteger() == PROBE_VALUE)
                {
                    result = path.ToArray();
                    return true;
                }

                if (pair.Value.Kind == TomlNodeKind.Table)
                {
                    path.Add(pair.Key);

                    if (TryFindProbePath((TomlTable)pair.Value, path, out result))
                        return true;

                    path.RemoveAt(path.Count - 1);
                    continue;
                }

                if (pair.Value.Kind != TomlNodeKind.Array)
                    continue;

                path.Add(pair.Key);

                var array = (TomlArray)pair.Value;
                for (var i = 0; i < array.Count; i++)
                {
                    var element = array[i] as TomlTable;
                    if (element != null && TryFindProbePath(element, path, out result))
                        return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            result = null;
            return false;
        }

        private static bool TraversesArrayTable(string[] path, IList<string[]> arrayTablePaths)
        {
            for (var i = 0; i < arrayTablePaths.Count; i++)
            {
                if (IsPrefix(arrayTablePaths[i], path))
                    return true;
            }

            return false;
        }

        private static bool IsPrefix(string[] prefix, string[] value)
        {
            if (prefix.Length > value.Length)
                return false;

            for (var i = 0; i < prefix.Length; i++)
            {
                if (!string.Equals(prefix[i], value[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static string[] Combine(string[] first, string[] second)
        {
            var result = new string[first.Length + second.Length];

            Array.Copy(first, 0, result, 0, first.Length);
            Array.Copy(second, 0, result, first.Length, second.Length);

            return result;
        }

        private static string[] Copy(string[] source)
        {
            var result = new string[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
        }
    }
}
