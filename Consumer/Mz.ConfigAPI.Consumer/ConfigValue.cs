using System;
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
        LocalTime = 10
    }

    public sealed class ConfigDate : IEquatable<ConfigDate>
    {
        public int Year { get; }
        public int Month { get; }
        public int Day { get; }

        public ConfigDate(int year, int month, int day)
        {
            if (!IsValidDate(year, month, day))
                throw new ArgumentException("The supplied components do not form a valid local date.");

            Year = year;
            Month = month;
            Day = day;
        }

        public bool Equals(ConfigDate other)
        {
            return other != null &&
                Year == other.Year &&
                Month == other.Month &&
                Day == other.Day;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigDate);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Year;
                hash = (hash * 397) ^ Month;
                hash = (hash * 397) ^ Day;
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
        public int Hour { get; }
        public int Minute { get; }
        public int Second { get; }
        public string FractionalSeconds { get; }

        public ConfigTime(int hour, int minute, int second)
            : this(hour, minute, second, string.Empty)
        {
        }

        public ConfigTime(
            int hour,
            int minute,
            int second,
            string fractionalSeconds)
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 60)
                throw new ArgumentException("The supplied components do not form a valid local time.");

            if (fractionalSeconds == null)
                throw new ArgumentNullException(nameof(fractionalSeconds));

            for (var i = 0; i < fractionalSeconds.Length; i++)
            {
                var c = fractionalSeconds[i];

                if (c < '0' || c > '9')
                {
                    throw new ArgumentException(
                        "Fractional seconds must contain only digits.",
                        nameof(fractionalSeconds));
                }
            }

            Hour = hour;
            Minute = minute;
            Second = second;
            FractionalSeconds = fractionalSeconds;
        }

        public bool Equals(ConfigTime other)
        {
            return other != null &&
                Hour == other.Hour &&
                Minute == other.Minute &&
                Second == other.Second &&
                string.Equals(
                    FractionalSeconds,
                    other.FractionalSeconds,
                    StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigTime);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Hour;
                hash = (hash * 397) ^ Minute;
                hash = (hash * 397) ^ Second;
                hash =
                    (hash * 397) ^
                    StringComparer.Ordinal.GetHashCode(
                        FractionalSeconds);

                return hash;
            }
        }
    }

    public sealed class ConfigOffsetDateTime :
        IEquatable<ConfigOffsetDateTime>
    {
        public ConfigDate Date { get; }
        public ConfigTime Time { get; }
        public int OffsetMinutes { get; }
        public bool IsUnknownLocalOffset { get; }

        public ConfigOffsetDateTime(
            ConfigDate date,
            ConfigTime time,
            int offsetMinutes,
            bool isUnknownLocalOffset = false)
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

        public bool Equals(ConfigOffsetDateTime other)
        {
            return other != null &&
                Date.Equals(other.Date) &&
                Time.Equals(other.Time) &&
                OffsetMinutes == other.OffsetMinutes &&
                IsUnknownLocalOffset ==
                    other.IsUnknownLocalOffset;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigOffsetDateTime);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Date.GetHashCode();
                hash = (hash * 397) ^ Time.GetHashCode();
                hash = (hash * 397) ^ OffsetMinutes;
                hash =
                    (hash * 397) ^
                    IsUnknownLocalOffset.GetHashCode();

                return hash;
            }
        }
    }

    public sealed class ConfigLocalDateTime :
        IEquatable<ConfigLocalDateTime>
    {
        public ConfigDate Date { get; }
        public ConfigTime Time { get; }

        public ConfigLocalDateTime(
            ConfigDate date,
            ConfigTime time)
        {
            if (date == null)
                throw new ArgumentNullException(nameof(date));

            if (time == null)
                throw new ArgumentNullException(nameof(time));

            Date = date;
            Time = time;
        }

        public bool Equals(ConfigLocalDateTime other)
        {
            return other != null &&
                Date.Equals(other.Date) &&
                Time.Equals(other.Time);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigLocalDateTime);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (Date.GetHashCode() * 397) ^
                    Time.GetHashCode();
            }
        }
    }

    public sealed class ConfigEntry : IEquatable<ConfigEntry>
    {
        public string Name { get; }
        public ConfigValue Value { get; }

        public ConfigEntry(
            string name,
            ConfigValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Config entry name must not be empty.",
                    nameof(name));
            }

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Name = name;
            Value = value;
        }

        public bool Equals(ConfigEntry other)
        {
            return other != null &&
                string.Equals(
                    Name,
                    other.Name,
                    StringComparison.Ordinal) &&
                Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigEntry);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (StringComparer.Ordinal.GetHashCode(Name) * 397) ^
                    Value.GetHashCode();
            }
        }
    }

    internal sealed class MzReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly T[] _items;

        public int Count
        {
            get { return _items.Length; }
        }

        public T this[int index]
        {
            get { return _items[index]; }
        }

        public MzReadOnlyList(T[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _items = new T[items.Length];
            Array.Copy(items, _items, items.Length);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
    public sealed class ConfigValue : IEquatable<ConfigValue>
    {
        private static readonly ConfigValue NullInstance =
            new ConfigValue(
                ConfigValueKind.Null,
                null,
                null,
                null);

        private readonly object _scalarValue;
        private readonly MzReadOnlyList<ConfigEntry> _entries;
        private readonly MzReadOnlyList<ConfigValue> _items;

        public ConfigValueKind Kind { get; }

        public object ScalarValue
        {
            get
            {
                return _scalarValue;
            }
        }

        public IReadOnlyList<ConfigEntry> Entries
        {
            get
            {
                return _entries;
            }
        }

        public IReadOnlyList<ConfigValue> Items
        {
            get
            {
                return _items;
            }
        }

        public static ConfigValue Null
        {
            get
            {
                return NullInstance;
            }
        }

        private ConfigValue(
            ConfigValueKind kind,
            object scalarValue,
            ConfigEntry[] entries,
            ConfigValue[] items)
        {
            Kind = kind;
            _scalarValue = scalarValue;

            _entries =
                entries == null
                    ? null
                    : new MzReadOnlyList<ConfigEntry>(entries);

            _items =
                items == null
                    ? null
                    : new MzReadOnlyList<ConfigValue>(items);
        }

        public static ConfigValue Boolean(bool value)
        {
            return new ConfigValue(
                ConfigValueKind.Boolean,
                value,
                null,
                null);
        }

        public static ConfigValue Integer(long value)
        {
            return new ConfigValue(
                ConfigValueKind.Integer,
                value,
                null,
                null);
        }

        public static ConfigValue Float(double value)
        {
            return new ConfigValue(
                ConfigValueKind.Float,
                value,
                null,
                null);
        }

        public static ConfigValue String(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(
                ConfigValueKind.String,
                value,
                null,
                null);
        }

        public static ConfigValue Object(
            params ConfigEntry[] entries)
        {
            ConfigEntry[] copy =
                CopyEntries(entries);

            ValidateUniqueEntryNames(copy);

            return new ConfigValue(
                ConfigValueKind.Object,
                null,
                copy,
                null);
        }

        public static ConfigValue Array(
            params ConfigValue[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var copy =
                new ConfigValue[items.Length];

            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    throw new ArgumentNullException(nameof(items));

                copy[i] = items[i];
            }

            return new ConfigValue(
                ConfigValueKind.Array,
                null,
                null,
                copy);
        }

        public static ConfigValue OffsetDateTime(
            ConfigOffsetDateTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(
                ConfigValueKind.OffsetDateTime,
                value,
                null,
                null);
        }

        public static ConfigValue LocalDateTime(
            ConfigLocalDateTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(
                ConfigValueKind.LocalDateTime,
                value,
                null,
                null);
        }

        public static ConfigValue LocalDate(
            ConfigDate value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(
                ConfigValueKind.LocalDate,
                value,
                null,
                null);
        }

        public static ConfigValue LocalTime(
            ConfigTime value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new ConfigValue(
                ConfigValueKind.LocalTime,
                value,
                null,
                null);
        }

        public bool Equals(ConfigValue other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Kind != other.Kind)
                return false;

            if (Kind == ConfigValueKind.Object)
                return ObjectEquals(other);

            if (Kind == ConfigValueKind.Array)
                return ArrayEquals(other);

            return Equals(
                _scalarValue,
                other._scalarValue);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigValue);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash =
                    (int)Kind;

                if (Kind == ConfigValueKind.Object)
                {
                    for (var i = 0; i < _entries.Count; i++)
                    {
                        hash ^=
                            _entries[i].GetHashCode();
                    }

                    return hash;
                }

                if (Kind == ConfigValueKind.Array)
                {
                    for (var i = 0; i < _items.Count; i++)
                    {
                        hash =
                            (hash * 397) ^
                            _items[i].GetHashCode();
                    }

                    return hash;
                }

                return
                    (hash * 397) ^
                    (_scalarValue == null
                        ? 0
                        : _scalarValue.GetHashCode());
            }
        }

        private bool ObjectEquals(
            ConfigValue other)
        {
            if (_entries.Count != other._entries.Count)
                return false;

            for (var i = 0; i < _entries.Count; i++)
            {
                ConfigValue otherValue;

                if (!TryGetEntry(
                    other._entries,
                    _entries[i].Name,
                    out otherValue))
                {
                    return false;
                }

                if (!_entries[i].Value.Equals(otherValue))
                    return false;
            }

            return true;
        }

        private bool ArrayEquals(
            ConfigValue other)
        {
            if (_items.Count != other._items.Count)
                return false;

            for (var i = 0; i < _items.Count; i++)
            {
                if (!_items[i].Equals(other._items[i]))
                    return false;
            }

            return true;
        }

        internal static ConfigEntry[] CopyEntries(
            ConfigEntry[] entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var copy =
                new ConfigEntry[entries.Length];

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null)
                    throw new ArgumentNullException(nameof(entries));

                copy[i] = entries[i];
            }

            return copy;
        }

        internal static void ValidateUniqueEntryNames(
            ConfigEntry[] entries)
        {
            var names =
                new HashSet<string>(
                    StringComparer.Ordinal);

            for (var i = 0; i < entries.Length; i++)
            {
                if (!names.Add(entries[i].Name))
                {
                    throw new ArgumentException(
                        "Config object contains duplicate entry name: " +
                        entries[i].Name,
                        nameof(entries));
                }
            }
        }

        private static bool TryGetEntry(
            IReadOnlyList<ConfigEntry> entries,
            string name,
            out ConfigValue value)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (!string.Equals(
                    entries[i].Name,
                    name,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                value = entries[i].Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    public sealed class ConfigDocument :
        IEquatable<ConfigDocument>
    {
        private readonly ConfigValue _root;

        public IReadOnlyList<ConfigEntry> Entries
        {
            get
            {
                return _root.Entries;
            }
        }

        internal ConfigValue Root
        {
            get
            {
                return _root;
            }
        }

        public ConfigDocument(
            params ConfigEntry[] entries)
        {
            _root =
                ConfigValue.Object(entries);
        }

        public bool TryGet(
            string name,
            out ConfigValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Config entry name must not be empty.",
                    nameof(name));

            for (var i = 0; i < Entries.Count; i++)
            {
                if (!string.Equals(
                    Entries[i].Name,
                    name,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                value = Entries[i].Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool Equals(ConfigDocument other)
        {
            return other != null &&
                _root.Equals(other._root);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigDocument);
        }

        public override int GetHashCode()
        {
            return _root.GetHashCode();
        }
    }
}
