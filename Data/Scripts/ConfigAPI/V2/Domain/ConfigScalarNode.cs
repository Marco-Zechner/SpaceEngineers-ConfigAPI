using System;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public enum ConfigScalarKind
    {
        Boolean = 0,
        Integer = 1,
        Float = 2,
        String = 3,
        OffsetDateTime = 4,
        LocalDateTime = 5,
        LocalDate = 6,
        LocalTime = 7
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

        public static ConfigScalarNode OffsetDateTime(ConfigOffsetDateTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigScalarNode(ConfigScalarKind.OffsetDateTime, value);
        }

        public static ConfigScalarNode LocalDateTime(ConfigLocalDateTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigScalarNode(ConfigScalarKind.LocalDateTime, value);
        }

        public static ConfigScalarNode LocalDate(ConfigLocalDate value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigScalarNode(ConfigScalarKind.LocalDate, value);
        }

        public static ConfigScalarNode LocalTime(ConfigLocalTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigScalarNode(ConfigScalarKind.LocalTime, value);
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
                case ConfigScalarKind.OffsetDateTime:
                    return ((ConfigOffsetDateTime)_value).Equals((ConfigOffsetDateTime)scalar._value);
                case ConfigScalarKind.LocalDateTime:
                    return ((ConfigLocalDateTime)_value).Equals((ConfigLocalDateTime)scalar._value);
                case ConfigScalarKind.LocalDate:
                    return ((ConfigLocalDate)_value).Equals((ConfigLocalDate)scalar._value);
                case ConfigScalarKind.LocalTime:
                    return ((ConfigLocalTime)_value).Equals((ConfigLocalTime)scalar._value);
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
                    case ConfigScalarKind.OffsetDateTime:
                        return (hash * 397) ^ ((ConfigOffsetDateTime)_value).GetHashCode();
                    case ConfigScalarKind.LocalDateTime:
                        return (hash * 397) ^ ((ConfigLocalDateTime)_value).GetHashCode();
                    case ConfigScalarKind.LocalDate:
                        return (hash * 397) ^ ((ConfigLocalDate)_value).GetHashCode();
                    case ConfigScalarKind.LocalTime:
                        return (hash * 397) ^ ((ConfigLocalTime)_value).GetHashCode();
                    default:
                        throw new InvalidOperationException("Unknown config scalar kind: " + Kind);
                }
            }
        }
    }
}