using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public sealed class ConfigValuePath : IEquatable<ConfigValuePath>
    {
        private readonly string[] _segments;
        private readonly IReadOnlyList<string> _readOnlySegments;

        public IReadOnlyList<string> Segments => _readOnlySegments;

        public ConfigValuePath(params string[] segments)
        {
            if (segments == null)
                throw new ArgumentNullException(nameof(segments));

            _segments = new string[segments.Length];

            for (var i = 0; i < segments.Length; i++)
            {
                ValidateSegment(segments[i], nameof(segments));
                _segments[i] = segments[i];
            }

            _readOnlySegments = Array.AsReadOnly(_segments);
        }

        public ConfigValuePath Append(string segment)
        {
            ValidateSegment(segment, nameof(segment));

            var segments = new string[_segments.Length + 1];
            Array.Copy(_segments, segments, _segments.Length);
            segments[segments.Length - 1] = segment;
            return new ConfigValuePath(segments);
        }

        public bool Equals(ConfigValuePath other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (_segments.Length != other._segments.Length)
                return false;

            for (var i = 0; i < _segments.Length; i++)
            {
                if (!string.Equals(_segments[i], other._segments[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigValuePath);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;

                for (var i = 0; i < _segments.Length; i++)
                    hash = (hash * 31) ^ StringComparer.Ordinal.GetHashCode(_segments[i]);

                return hash;
            }
        }

        private static void ValidateSegment(string segment, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(segment))
                throw new ArgumentException("Path segments must not be empty.", parameterName);
        }
    }
}
