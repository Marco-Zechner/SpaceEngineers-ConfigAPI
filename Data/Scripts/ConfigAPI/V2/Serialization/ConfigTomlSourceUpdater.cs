using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.V2.Domain;
using Mz.Toml;

namespace MarcoZechner.ConfigAPI.V2.Serialization
{
    public static class ConfigTomlSourceUpdater
    {
        private const string VALUE_WRAPPER_KEY = "__configapi_value__";

        public static string SetValue(string source, ConfigValuePath path, ConfigNode value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var parsed = Toml.TryParse(source);

            if (!parsed.IsSuccess || parsed.Syntax == null)
            {
                var message = "Source must be valid TOML.";

                if (parsed.Diagnostics.Count > 0)
                    message += " " + parsed.Diagnostics[0];

                throw new ArgumentException(message, nameof(source));
            }

            var assignment = FindUniqueAssignment(
                ConfigTomlSyntaxIndex.Create(parsed),
                path);

            var editor = parsed.Syntax.CreateEditor();

            if (value is ConfigNullNode)
            {
                if (assignment.IsDisabled)
                    return source;

                editor.DisableAssignment(assignment.Node);
                return editor.Source;
            }

            var valueSource = RenderValueSource(value);

            if (assignment.IsDisabled)
            {
                editor.EnableAssignment(assignment.Node);

                var enabledParse = Toml.TryParse(editor.Source);
                assignment = FindUniqueAssignment(
                    ConfigTomlSyntaxIndex.Create(enabledParse),
                    path);

                editor = enabledParse.Syntax.CreateEditor();
            }

            editor.ReplaceAssignmentValue(assignment.Node, valueSource);
            return editor.Source;
        }

        private static ConfigTomlSyntaxAssignment FindUniqueAssignment(
            ConfigTomlSyntaxIndex index,
            ConfigValuePath path)
        {
            ConfigTomlSyntaxAssignment result = null;

            for (var i = 0; i < index.Assignments.Count; i++)
            {
                var candidate = index.Assignments[i];

                if (!candidate.Path.Equals(path))
                    continue;

                if (result != null)
                {
                    throw new InvalidOperationException(
                        "Multiple TOML assignments map to the same ConfigAPI value path.");
                }

                result = candidate;
            }

            if (result == null)
                throw new KeyNotFoundException("The ConfigAPI value path has no addressable TOML assignment.");

            return result;
        }

        private static string RenderValueSource(ConfigNode value)
        {
            var wrapper = new ConfigDocument(
                new ConfigObjectNode(
                    new ConfigObjectEntry(
                        VALUE_WRAPPER_KEY,
                        new ConfigArrayNode(value))));

            var source = Toml.Write(
                ConfigTomlDocumentCodec.ToTomlDocument(wrapper));

            var parsed = Toml.TryParse(source);

            if (!parsed.IsSuccess || parsed.Syntax == null)
                throw new InvalidOperationException("Generated TOML value wrapper did not parse successfully.");

            TomlSyntaxNode assignment = null;

            for (var i = 0; i < parsed.Syntax.Nodes.Count; i++)
            {
                if (parsed.Syntax.Nodes[i].Kind != TomlSyntaxNodeKind.Assignment)
                    continue;

                assignment = parsed.Syntax.Nodes[i];
                break;
            }

            if (assignment == null || !assignment.ValueSpan.HasValue)
                throw new InvalidOperationException("Generated TOML value wrapper has no assignment value span.");

            var span = assignment.ValueSpan.Value;
            var arraySource = source.Substring(span.Start, span.Length);

            if (arraySource.Length < 2 ||
                arraySource[0] != '[' ||
                arraySource[arraySource.Length - 1] != ']')
            {
                throw new InvalidOperationException("Generated TOML value wrapper is not an array.");
            }

            return arraySource.Substring(1, arraySource.Length - 2);
        }
    }
}
