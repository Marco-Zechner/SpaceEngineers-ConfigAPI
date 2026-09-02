using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mz.ConfigApi
{
    internal static class ConfigClrMapper
    {
        public static ConfigDocument ToDocument<T>(T value) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var type = typeof(T);
            if (value.GetType() != type)
                throw new NotSupportedException("Polymorphic config roots are not supported.");

            EnsureRootObjectType(type);

            var active = new HashSet<object>(ReferenceComparer.Instance);
            return new ConfigDocument(EncodeObjectEntries(value, type, active));
        }

        public static T FromDocument<T>(ConfigDocument document) where T : class
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var type = typeof(T);
            EnsureRootObjectType(type);

            return (T)DecodeObject(type, document.Entries);
        }

        private static ConfigValue EncodeValue(object value, Type type, HashSet<object> active)
        {
            var nullableType = Nullable.GetUnderlyingType(type);

            if (value == null)
                return ConfigValue.Null;

            if (nullableType != null)
                return EncodeValue(value, nullableType, active);

            if (type == typeof(bool))
                return ConfigValue.Boolean((bool)value);

            if (IsIntegerType(type))
                return ConfigValue.Integer(ToInt64(value, type));

            if (type == typeof(float))
                return ConfigValue.Float((float)value);

            if (type == typeof(double))
                return ConfigValue.Float((double)value);

            if (type == typeof(string))
                return ConfigValue.String((string)value);

            if (type.IsEnum)
                return EncodeEnum(value, type);

            if (type == typeof(ConfigOffsetDateTime))
                return ConfigValue.OffsetDateTime((ConfigOffsetDateTime)value);

            if (type == typeof(ConfigLocalDateTime))
                return ConfigValue.LocalDateTime((ConfigLocalDateTime)value);

            if (type == typeof(ConfigDate))
                return ConfigValue.LocalDate((ConfigDate)value);

            if (type == typeof(ConfigTime))
                return ConfigValue.LocalTime((ConfigTime)value);

            if (type.IsArray)
                return EncodeArray((Array)value, type, active);

            Type elementType;
            if (TryGetListElementType(type, out elementType))
                return EncodeList((IList)value, elementType, active);

            Type dictionaryValueType;
            if (TryGetDictionaryValueType(type, out dictionaryValueType))
                return EncodeDictionary((IDictionary)value, dictionaryValueType, active);

            return ConfigValue.Object(EncodeObjectEntries(value, type, active));
        }

        private static object DecodeValue(ConfigValue value, Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);

            if (value.Kind == ConfigValueKind.Null)
            {
                if (!type.IsValueType || nullableType != null)
                    return null;

                throw new ArgumentException("Semantic null cannot be assigned to " + type.FullName + ".");
            }

            if (nullableType != null)
                return DecodeValue(value, nullableType);

            if (type == typeof(bool))
                return RequireScalar<bool>(value, ConfigValueKind.Boolean, type);

            if (IsIntegerType(type))
            {
                var integer = RequireScalar<long>(value, ConfigValueKind.Integer, type);
                return FromInt64(integer, type);
            }

            if (type == typeof(float))
                return ToSingle(RequireScalar<double>(value, ConfigValueKind.Float, type));

            if (type == typeof(double))
                return RequireScalar<double>(value, ConfigValueKind.Float, type);

            if (type == typeof(string))
                return RequireScalar<string>(value, ConfigValueKind.String, type);

            if (type.IsEnum)
                return DecodeEnum(value, type);

            if (type == typeof(ConfigOffsetDateTime))
                return RequireScalar<ConfigOffsetDateTime>(value, ConfigValueKind.OffsetDateTime, type);

            if (type == typeof(ConfigLocalDateTime))
                return RequireScalar<ConfigLocalDateTime>(value, ConfigValueKind.LocalDateTime, type);

            if (type == typeof(ConfigDate))
                return RequireScalar<ConfigDate>(value, ConfigValueKind.LocalDate, type);

            if (type == typeof(ConfigTime))
                return RequireScalar<ConfigTime>(value, ConfigValueKind.LocalTime, type);

            if (type.IsArray)
                return DecodeArray(value, type);

            Type elementType;
            if (TryGetListElementType(type, out elementType))
                return DecodeList(value, type, elementType);

            Type dictionaryValueType;
            if (TryGetDictionaryValueType(type, out dictionaryValueType))
                return DecodeDictionary(value, type, dictionaryValueType);

            RequireKind(value, ConfigValueKind.Object, type);
            return DecodeObject(type, value.Entries);
        }

        private static ConfigValue EncodeEnum(object value, Type type)
        {
            var name = Enum.GetName(type, value);
            if (name == null)
                throw new NotSupportedException("Enum value has no declared name: " + type.FullName + ".");

            return ConfigValue.String(name);
        }

        private static object DecodeEnum(ConfigValue value, Type type)
        {
            var text = RequireScalar<string>(value, ConfigValueKind.String, type);

            try
            {
                var parsed = Enum.Parse(type, text, false);
                if (!Enum.IsDefined(type, parsed))
                    throw new ArgumentException();

                return parsed;
            }
            catch (Exception exception)
            {
                if (exception is ArgumentException || exception is OverflowException)
                {
                    throw new ArgumentException(
                        "Config value '" + text + "' is not a declared value of enum " + type.FullName + ".",
                        exception);
                }

                throw;
            }
        }

        private static ConfigValue EncodeArray(Array array, Type type, HashSet<object> active)
        {
            if (type.GetArrayRank() != 1)
                throw new NotSupportedException("Only one-dimensional config arrays are supported.");

            EnterReference(active, array);

            try
            {
                var elementType = type.GetElementType();
                var items = new ConfigValue[array.Length];

                for (var i = 0; i < items.Length; i++)
                    items[i] = EncodeValue(array.GetValue(i), elementType, active);

                return ConfigValue.Array(items);
            }
            finally
            {
                active.Remove(array);
            }
        }

        private static object DecodeArray(ConfigValue value, Type type)
        {
            RequireKind(value, ConfigValueKind.Array, type);

            if (type.GetArrayRank() != 1)
                throw new NotSupportedException("Only one-dimensional config arrays are supported.");

            var elementType = type.GetElementType();
            var array = Array.CreateInstance(elementType, value.Items.Count);

            for (var i = 0; i < value.Items.Count; i++)
                array.SetValue(DecodeValue(value.Items[i], elementType), i);

            return array;
        }

        private static ConfigValue EncodeList(IList list, Type elementType, HashSet<object> active)
        {
            EnterReference(active, list);

            try
            {
                var items = new ConfigValue[list.Count];

                for (var i = 0; i < list.Count; i++)
                    items[i] = EncodeValue(list[i], elementType, active);

                return ConfigValue.Array(items);
            }
            finally
            {
                active.Remove(list);
            }
        }

        private static object DecodeList(ConfigValue value, Type type, Type elementType)
        {
            RequireKind(value, ConfigValueKind.Array, type);

            var list = (IList)CreateInstance(type);

            for (var i = 0; i < value.Items.Count; i++)
                list.Add(DecodeValue(value.Items[i], elementType));

            return list;
        }

        private static ConfigValue EncodeDictionary(
            IDictionary dictionary,
            Type valueType,
            HashSet<object> active)
        {
            EnterReference(active, dictionary);

            try
            {
                var keys = new List<string>();

                foreach (DictionaryEntry pair in dictionary)
                {
                    var key = pair.Key as string;
                    if (key == null)
                        throw new NotSupportedException("Config dictionary keys must be strings.");

                    keys.Add(key);
                }

                keys.Sort(StringComparer.Ordinal);

                var entries = new ConfigEntry[keys.Count];

                for (var i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    entries[i] = new ConfigEntry(
                        key,
                        EncodeValue(dictionary[key], valueType, active));
                }

                return ConfigValue.Object(entries);
            }
            finally
            {
                active.Remove(dictionary);
            }
        }

        private static object DecodeDictionary(ConfigValue value, Type type, Type valueType)
        {
            RequireKind(value, ConfigValueKind.Object, type);

            var dictionary = (IDictionary)CreateInstance(type);

            for (var i = 0; i < value.Entries.Count; i++)
            {
                var entry = value.Entries[i];
                dictionary.Add(entry.Name, DecodeValue(entry.Value, valueType));
            }

            return dictionary;
        }

        private static ConfigEntry[] EncodeObjectEntries(
            object instance,
            Type type,
            HashSet<object> active)
        {
            var trackReference = !type.IsValueType;

            if (trackReference)
                EnterReference(active, instance);

            try
            {
                var members = GetMappedMembers(type);
                var entries = new ConfigEntry[members.Length];

                for (var i = 0; i < members.Length; i++)
                {
                    var member = members[i];
                    entries[i] = new ConfigEntry(
                        member.Name,
                        EncodeValue(member.GetValue(instance), member.ValueType, active));
                }

                return entries;
            }
            finally
            {
                if (trackReference)
                    active.Remove(instance);
            }
        }

        private static object DecodeObject(Type type, IReadOnlyList<ConfigEntry> entries)
        {
            var instance = CreateInstance(type);
            var members = GetMappedMembers(type);

            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i];
                ConfigValue value;

                if (!TryGetEntry(entries, member.Name, out value))
                {
                    throw new ArgumentException(
                        "Config document is missing required member '" +
                        member.Name +
                        "' for " +
                        type.FullName +
                        ".");
                }

                member.SetValue(instance, DecodeValue(value, member.ValueType));
            }

            return instance;
        }

        private static MappedMember[] GetMappedMembers(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            if (fields.Length > 0 && properties.Length > 0)
            {
                throw new NotSupportedException(
                    "Config type " +
                    type.FullName +
                    " mixes public fields and public properties. Use one style consistently.");
            }

            if (fields.Length > 0)
            {
                Array.Sort(
                    fields,
                    delegate(FieldInfo left, FieldInfo right)
                    {
                        return left.MetadataToken.CompareTo(right.MetadataToken);
                    });

                var members = new MappedMember[fields.Length];

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];

                    if (field.IsInitOnly || field.IsLiteral)
                    {
                        throw new NotSupportedException(
                            "Config field must be writable: " +
                            type.FullName +
                            "." +
                            field.Name);
                    }

                    members[i] = new MappedMember(field);
                }

                return members;
            }

            Array.Sort(
                properties,
                delegate(PropertyInfo left, PropertyInfo right)
                {
                    return left.MetadataToken.CompareTo(right.MetadataToken);
                });

            var propertyMembers = new MappedMember[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                if (property.GetIndexParameters().Length != 0 ||
                    property.GetGetMethod(false) == null ||
                    property.GetSetMethod(false) == null)
                {
                    throw new NotSupportedException(
                        "Config property must have public getter and setter: " +
                        type.FullName +
                        "." +
                        property.Name);
                }

                propertyMembers[i] = new MappedMember(property);
            }

            return propertyMembers;
        }

        private static object CreateInstance(Type type)
        {
            try
            {
                return Activator.CreateInstance(type, true);
            }
            catch (Exception exception)
            {
                throw new NotSupportedException(
                    "Config type requires a parameterless constructor: " +
                    type.FullName +
                    ".",
                    exception);
            }
        }

        private static void EnsureRootObjectType(Type type)
        {
            Type ignored;

            if (type.IsValueType ||
                type == typeof(string) ||
                type.IsArray ||
                type.IsEnum ||
                IsIntegerType(type) ||
                type == typeof(bool) ||
                type == typeof(float) ||
                type == typeof(double) ||
                TryGetListElementType(type, out ignored) ||
                TryGetDictionaryValueType(type, out ignored))
            {
                throw new NotSupportedException(
                    "Config root must be a class with public fields or public read/write properties.");
            }
        }

        private static void EnterReference(HashSet<object> active, object value)
        {
            if (!active.Add(value))
                throw new NotSupportedException("Cyclic config object graphs are not supported.");
        }

        private static bool TryGetEntry(
            IReadOnlyList<ConfigEntry> entries,
            string name,
            out ConfigValue value)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (!string.Equals(entries[i].Name, name, StringComparison.Ordinal))
                    continue;

                value = entries[i].Value;
                return true;
            }

            value = null;
            return false;
        }

        private static T RequireScalar<T>(
            ConfigValue value,
            ConfigValueKind kind,
            Type targetType)
        {
            RequireKind(value, kind, targetType);

            if (!(value.ScalarValue is T))
            {
                throw new ArgumentException(
                    "Config value has the wrong scalar representation for " +
                    targetType.FullName +
                    ".");
            }

            return (T)value.ScalarValue;
        }

        private static void RequireKind(ConfigValue value, ConfigValueKind kind, Type targetType)
        {
            if (value.Kind != kind)
            {
                throw new ArgumentException(
                    "Config value kind " +
                    value.Kind +
                    " cannot be assigned to " +
                    targetType.FullName +
                    ".");
            }
        }

        private static bool TryGetListElementType(Type type, out Type elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool TryGetDictionaryValueType(Type type, out Type valueType)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var arguments = type.GetGenericArguments();

                if (arguments[0] != typeof(string))
                {
                    throw new NotSupportedException(
                        "Config dictionaries must use string keys: " +
                        type.FullName +
                        ".");
                }

                valueType = arguments[1];
                return true;
            }

            valueType = null;
            return false;
        }

        private static bool IsIntegerType(Type type)
        {
            return type == typeof(sbyte) ||
                type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong);
        }

        private static long ToInt64(object value, Type type)
        {
            checked
            {
                if (type == typeof(sbyte))
                    return (sbyte)value;

                if (type == typeof(byte))
                    return (byte)value;

                if (type == typeof(short))
                    return (short)value;

                if (type == typeof(ushort))
                    return (ushort)value;

                if (type == typeof(int))
                    return (int)value;

                if (type == typeof(uint))
                    return (uint)value;

                if (type == typeof(long))
                    return (long)value;

                if (type == typeof(ulong))
                {
                    var unsigned = (ulong)value;

                    if (unsigned > long.MaxValue)
                    {
                        throw new NotSupportedException(
                            "Unsigned config integer exceeds Int64.MaxValue.");
                    }

                    return (long)unsigned;
                }
            }

            throw new NotSupportedException("Unsupported config integer type: " + type.FullName + ".");
        }

        private static object FromInt64(long value, Type type)
        {
            try
            {
                checked
                {
                    if (type == typeof(sbyte))
                        return (sbyte)value;

                    if (type == typeof(byte))
                        return (byte)value;

                    if (type == typeof(short))
                        return (short)value;

                    if (type == typeof(ushort))
                        return (ushort)value;

                    if (type == typeof(int))
                        return (int)value;

                    if (type == typeof(uint))
                        return (uint)value;

                    if (type == typeof(long))
                        return value;

                    if (type == typeof(ulong))
                        return (ulong)value;
                }
            }
            catch (OverflowException exception)
            {
                throw new ArgumentException(
                    "Config integer " +
                    value +
                    " is outside the range of " +
                    type.FullName +
                    ".",
                    exception);
            }

            throw new NotSupportedException("Unsupported config integer type: " + type.FullName + ".");
        }

        private static float ToSingle(double value)
        {
            if (!double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                (value > float.MaxValue || value < -float.MaxValue))
            {
                throw new ArgumentException("Config float is outside the range of System.Single.");
            }

            return (float)value;
        }

        private sealed class MappedMember
        {
            private readonly FieldInfo _field;
            private readonly PropertyInfo _property;

            public string Name { get; private set; }
            public Type ValueType { get; private set; }

            public MappedMember(FieldInfo field)
            {
                _field = field;
                Name = field.Name;
                ValueType = field.FieldType;
            }

            public MappedMember(PropertyInfo property)
            {
                _property = property;
                Name = property.Name;
                ValueType = property.PropertyType;
            }

            public object GetValue(object instance)
            {
                return _field != null
                    ? _field.GetValue(instance)
                    : _property.GetValue(instance, null);
            }

            public void SetValue(object instance, object value)
            {
                if (_field != null)
                    _field.SetValue(instance, value);
                else
                    _property.SetValue(instance, value, null);
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            bool IEqualityComparer<object>.Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            int IEqualityComparer<object>.GetHashCode(object value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}