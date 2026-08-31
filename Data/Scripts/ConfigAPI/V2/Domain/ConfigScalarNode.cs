using System;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public enum ConfigScalarKind
    {
        Boolean = 0,
        Integer = 1,
        Float = 2,
        String = 3
    }

    public sealed class ConfigScalarNode : ConfigNode
    {
        private readonly object _value;

        public ConfigScalarKind Kind { get; }
        public object Value => _value;

        private ConfigScalarNode(ConfigScalarKind kind, object value)
        {
            Kind = kind;
            _value = value;
        }

        public static ConfigScalarNode Boolean(bool value)
        {
            return new ConfigScalarNode(ConfigScalarKind.Boolean, value);
        }

        public static ConfigScalarNode Integer(long value)
        {
            return new ConfigScalarNode(ConfigScalarKind.Integer, value);
        }

        public static ConfigScalarNode Float(double value)
        {
            return new ConfigScalarNode(ConfigScalarKind.Float, value);
        }

        public static ConfigScalarNode String(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigScalarNode(ConfigScalarKind.String, value);
        }

        protected override bool EqualsNode(ConfigNode other)
        {
            var scalar = other as ConfigScalarNode;
            if (scalar == null || Kind != scalar.Kind)
                return false;

            switch (Kind)
            {
                case ConfigScalarKind.Boolean:
                    return (bool)_value == (bool)scalar._value;
                case ConfigScalarKind.Integer:
                    return (long)_value == (long)scalar._value;
                case ConfigScalarKind.Float:
                    return ((double)_value).Equals((double)scalar._value);
                case ConfigScalarKind.String:
                    return string.Equals((string)_value, (string)scalar._value, StringComparison.Ordinal);
                default:
                    throw new InvalidOperationException("Unknown config scalar kind: " + Kind);
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;

                switch (Kind)
                {
                    case ConfigScalarKind.Boolean:
                        return (hash * 397) ^ ((bool)_value).GetHashCode();
                    case ConfigScalarKind.Integer:
                        return (hash * 397) ^ ((long)_value).GetHashCode();
                    case ConfigScalarKind.Float:
                        return (hash * 397) ^ ((double)_value).GetHashCode();
                    case ConfigScalarKind.String:
                        return (hash * 397) ^ StringComparer.Ordinal.GetHashCode((string)_value);
                    default:
                        throw new InvalidOperationException("Unknown config scalar kind: " + Kind);
                }
            }
        }
    }
}
