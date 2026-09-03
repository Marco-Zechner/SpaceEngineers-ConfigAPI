using System;
using System.Collections;
using System.Collections.Generic;

namespace Mz.ConfigApi
{
    public enum ConfigValueKind
    {
        Null = 0,
        Boolean = 1,
        Integer = 2,
        Float = 3,
        String = 4,
        Object = 5,
        Array = 6,
        OffsetDateTime = 7,
        LocalDateTime = 8,
        LocalDate = 9,
        LocalTime = 10,
    }

    public sealed class ConfigDate : IEquatable<ConfigDate>
    {
        public ConfigDate(int year, int month, int day)
        {
            if (!IsValidDate(year, month, day))
                throw new ArgumentException("The supplied components do not form a valid local date.");

            Year = year;
            Month = month;
            Day = day;
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }

        public bool Equals(ConfigDate other) 
            => other != null && Year == other.Year && Month == other.Month && Day == other.Day;

        public override bool Equals(object obj) => Equals(obj as ConfigDate);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Year;
                hash = ( hash * 397 ) ^ Month;
                hash = ( hash * 397 ) ^ Day;
                return hash;
            }
        }

        private static bool IsValidDate(int year, int month, int day)
        {
            if (year < 0 || year > 9999 || month < 1 || month > 12 || day < 1)
                return false;

            return day <= DaysInMonth(year, month);
        }

        private static int DaysInMonth(int year, int month)
        {
            switch (month)
            {
                case 2:
                    return IsLeapYear(year) ? 29 : 28;
                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;
                default:
                    return 31;
            }
        }

        private static bool IsLeapYear(int year)
        {
            if (year % 400 == 0)
                return true;

            if (year % 100 == 0)
                return false;

            return year % 4 == 0;
        }
    }

    public sealed class ConfigTime : IEquatable<ConfigTime>
    {
        public ConfigTime(int hour, int minute, int second) : this(hour, minute, second, string.Empty) { }

        public ConfigTime(int hour, int minute, int second, string fractionalSeconds)
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 60)
                throw new ArgumentException("The supplied components do not form a valid local time.");

            if (fractionalSeconds == null)
                throw new ArgumentNullException(nameof(fractionalSeconds));

            foreach (char c in fractionalSeconds)
            {
                if (c < '0' || c > '9')
                    throw new ArgumentException("Fractional seconds must contain only digits.", nameof(fractionalSeconds));
            }

            Hour = hour;
            Minute = minute;
            Second = second;
            FractionalSeconds = fractionalSeconds;
        }

        public int Hour { get; }
        public int Minute { get; }
        public int Second { get; }
        public string FractionalSeconds { get; }

        public bool Equals(ConfigTime other) 
            => other != null && Hour == other.Hour && Minute == other.Minute && Second == other.Second &&
               string.Equals(FractionalSeconds, other.FractionalSeconds, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as ConfigTime);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Hour;
                hash = ( hash * 397 ) ^ Minute;
                hash = ( hash * 397 ) ^ Second;
                hash = ( hash * 397 ) ^ StringComparer.Ordinal.GetHashCode(FractionalSeconds);

                return hash;
            }
        }
    }

    public sealed class ConfigOffsetDateTime : IEquatable<ConfigOffsetDateTime>
    {
        public ConfigOffsetDateTime(ConfigDate date, ConfigTime time, int offsetMinutes, bool isUnknownLocalOffset = false)
        {
            if (date == null)
                throw new ArgumentNullException(nameof(date));

            if (time == null)
                throw new ArgumentNullException(nameof(time));

            if (offsetMinutes < -1439 || offsetMinutes > 1439)
                throw new ArgumentException("UTC offset must be between -23:59 and +23:59.", nameof(offsetMinutes));

            if (isUnknownLocalOffset && offsetMinutes != 0)
                throw new ArgumentException("Unknown local offset is only valid with offset 00:00.", nameof(isUnknownLocalOffset));

            Date = date;
            Time = time;
            OffsetMinutes = offsetMinutes;
            IsUnknownLocalOffset = isUnknownLocalOffset;
        }

        public ConfigDate Date { get; }
        public ConfigTime Time { get; }
        public int OffsetMinutes { get; }
        public bool IsUnknownLocalOffset { get; }

        public bool Equals(ConfigOffsetDateTime other) 
            => other != null && Date.Equals(other.Date) && Time.Equals(other.Time) &&
               OffsetMinutes == other.OffsetMinutes && IsUnknownLocalOffset == other.IsUnknownLocalOffset;

        public override bool Equals(object obj) => Equals(obj as ConfigOffsetDateTime);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Date.GetHashCode();
                hash = ( hash * 397 ) ^ Time.GetHashCode();
                hash = ( hash * 397 ) ^ OffsetMinutes;
                hash = ( hash * 397 ) ^ IsUnknownLocalOffset.GetHashCode();

                return hash;
            }
        }
    }

    public sealed class ConfigLocalDateTime : IEquatable<ConfigLocalDateTime>
    {
        public ConfigLocalDateTime(ConfigDate date, ConfigTime time)
        {
            if (date == null)
                throw new ArgumentNullException(nameof(date));

            if (time == null)
                throw new ArgumentNullException(nameof(time));

            Date = date;
            Time = time;
        }

        public ConfigDate Date { get; }
        public ConfigTime Time { get; }

        public bool Equals(ConfigLocalDateTime other) 
            => other != null && Date.Equals(other.Date) && Time.Equals(other.Time);

        public override bool Equals(object obj) => Equals(obj as ConfigLocalDateTime);

        public override int GetHashCode()
        {
            unchecked
            {
                return ( Date.GetHashCode() * 397 ) ^ Time.GetHashCode();
            }
        }
    }

    public sealed class ConfigEntry : IEquatable<ConfigEntry>
    {
        public ConfigEntry(string name, ConfigValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Config entry name must not be empty.", nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Name = name;
            Value = value;
        }

        public string Name { get; }
        public ConfigValue Value { get; }

        public bool Equals(ConfigEntry other) 
            => other != null && string.Equals(Name, other.Name, StringComparison.Ordinal) && Value.Equals(other.Value);

        public override bool Equals(object obj) => Equals(obj as ConfigEntry);

        public override int GetHashCode()
        {
            unchecked
            {
                return ( StringComparer.Ordinal.GetHashCode(Name) * 397 ) ^ Value.GetHashCode();
            }
        }
    }

    internal sealed class MzReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly T[] _items;

        public MzReadOnlyList(T[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _items = new T[items.Length];
            Array.Copy(items, _items, items.Length);
        }

        public int Count => _items.Length;

        public T this[int index] => _items[index];

        public IEnumerator<T> GetEnumerator() => ( (IEnumerable<T>)_items ).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    public sealed class ConfigValue : IEquatable<ConfigValue>
    {
        private readonly MzReadOnlyList<ConfigEntry> _entries;
        private readonly MzReadOnlyList<ConfigValue> _items;

        private ConfigValue(ConfigValueKind kind, object scalarValue, ConfigEntry[] entries, ConfigValue[] items)
        {
            Kind = kind;
            ScalarValue = scalarValue;

            _entries = entries == null ? null : new MzReadOnlyList<ConfigEntry>(entries);

            _items = items == null ? null : new MzReadOnlyList<ConfigValue>(items);
        }

        public ConfigValueKind Kind { get; }

        public object ScalarValue { get; }

        public IReadOnlyList<ConfigEntry> Entries => _entries;

        public IReadOnlyList<ConfigValue> Items => _items;

        public static ConfigValue Null { get; } = new ConfigValue(ConfigValueKind.Null, null, null, null);

        public bool Equals(ConfigValue other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Kind != other.Kind)
                return false;

            switch (Kind)
            {
                case ConfigValueKind.Object: return ObjectEquals(other);
                case ConfigValueKind.Array:  return ArrayEquals(other);
                default:                     return Equals(ScalarValue, other.ScalarValue);
            }
        }

        public static ConfigValue Boolean(bool value) => new ConfigValue(ConfigValueKind.Boolean, value, null, null);

        public static ConfigValue Integer(long value) => new ConfigValue(ConfigValueKind.Integer, value, null, null);

        public static ConfigValue Float(double value) => new ConfigValue(ConfigValueKind.Float, value, null, null);

        public static ConfigValue String(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(ConfigValueKind.String, value, null, null);
        }

        public static ConfigValue Object(params ConfigEntry[] entries)
        {
            var copy = CopyEntries(entries);

            ValidateUniqueEntryNames(copy);

            return new ConfigValue(ConfigValueKind.Object, null, copy, null);
        }

        public static ConfigValue Array(params ConfigValue[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var copy = new ConfigValue[items.Length];

            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    throw new ArgumentNullException(nameof(items));

                copy[i] = items[i];
            }

            return new ConfigValue(ConfigValueKind.Array, null, null, copy);
        }

        public static ConfigValue OffsetDateTime(ConfigOffsetDateTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(ConfigValueKind.OffsetDateTime, value, null, null);
        }

        public static ConfigValue LocalDateTime(ConfigLocalDateTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(ConfigValueKind.LocalDateTime, value, null, null);
        }

        public static ConfigValue LocalDate(ConfigDate value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(ConfigValueKind.LocalDate, value, null, null);
        }

        public static ConfigValue LocalTime(ConfigTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(ConfigValueKind.LocalTime, value, null, null);
        }

        public override bool Equals(object obj) => Equals(obj as ConfigValue);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;

                switch (Kind)
                {
                    case ConfigValueKind.Object:
                    {
                        foreach (ConfigEntry entry in _entries)
                            hash ^= entry.GetHashCode();

                        return hash;
                    }
                    case ConfigValueKind.Array:
                    {
                        foreach (ConfigValue entry in _items)
                            hash = ( hash * 397 ) ^ entry.GetHashCode();

                        return hash;
                    }
                    default:
                        return ( hash * 397 ) ^ ( ScalarValue == null ? 0 : ScalarValue.GetHashCode() );
                }
            }
        }

        private bool ObjectEquals(ConfigValue other)
        {
            if (_entries.Count != other._entries.Count)
                return false;

            foreach (ConfigEntry entry in _entries)
            {
                ConfigValue otherValue;

                if (!TryGetEntry(other._entries, entry.Name, out otherValue))
                    return false;

                if (!entry.Value.Equals(otherValue))
                    return false;
            }

            return true;
        }

        private bool ArrayEquals(ConfigValue other)
        {
            if (_items.Count != other._items.Count)
                return false;

            for (var i = 0; i < _items.Count; i++)
                if (!_items[i].Equals(other._items[i]))
                    return false;

            return true;
        }

        internal static ConfigEntry[] CopyEntries(ConfigEntry[] entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var copy = new ConfigEntry[entries.Length];

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null)
                    throw new ArgumentNullException(nameof(entries));

                copy[i] = entries[i];
            }

            return copy;
        }

        internal static void ValidateUniqueEntryNames(ConfigEntry[] entries)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (ConfigEntry entry in entries)
                if (!names.Add(entry.Name))
                    throw new ArgumentException($"Config object contains duplicate entry name: {entry.Name}", nameof(entries));
        }

        private static bool TryGetEntry(IReadOnlyList<ConfigEntry> entries, string name, out ConfigValue value)
        {
            foreach (ConfigEntry entry in entries)
            {
                if (!string.Equals(entry.Name, name, StringComparison.Ordinal))
                    continue;

                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    public sealed class ConfigDocument : IEquatable<ConfigDocument>
    {
        public ConfigDocument(params ConfigEntry[] entries)
        {
            Root = ConfigValue.Object(entries);
        }

        public IReadOnlyList<ConfigEntry> Entries => Root.Entries;

        internal ConfigValue Root { get; }

        public bool Equals(ConfigDocument other) => other != null && Root.Equals(other.Root);

        public bool TryGet(string name, out ConfigValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Config entry name must not be empty.", nameof(name));

            foreach (ConfigEntry entry in Entries)
            {
                if (!string.Equals(entry.Name, name, StringComparison.Ordinal))
                    continue;

                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }

        public override bool Equals(object obj) => Equals(obj as ConfigDocument);

        public override int GetHashCode() => Root.GetHashCode();
    }
}
