using System;
using System.Xml.Serialization;

namespace mz.Config
{
    public class CfgVal<T>
    {
        public static implicit operator T(CfgVal<T> c) => c._value;
        public static implicit operator CfgVal<T>(T v) => new CfgVal<T>(v);

        private T _value;

        [XmlText]
        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (Equals(_value, value))
                    return;

                var old = _value;
                _value = value;

                // Local event if you want
                Changed?.Invoke(old, value);

                // triggers config save & a global change event
                ConfigStorage.NotifyChanged();
            }
        }

        /// <summary>
        /// Event triggered when the value changes, providing the old and new values.
        /// </summary>
        public event Action<T, T> Changed;

        /// <summary>
        /// Sets the value without triggering change events or saving.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        public void SetSilent(T value)
        {
            _value = value;
        }

        public CfgVal()
        {
            // for XML serializer
        }

        public CfgVal(T defaultValue)
        {
            _value = defaultValue;
        }

        // -------------------- Overrides & Operators --------------------

        public override string ToString()
        {
            return _value?.ToString();
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is CfgVal<T>)
                return Equals((CfgVal<T>)obj);

            if (obj is T)
                return EqualsValue((T)obj);

            return false;
        }

        public bool Equals(CfgVal<T> other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return EqualsValue(other._value);
        }

        private bool EqualsValue(T otherValue)
        {
            return Equals(_value, otherValue);
        }

        public static bool operator ==(CfgVal<T> a, CfgVal<T> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return Equals(a._value, b._value);
        }

        public static bool operator !=(CfgVal<T> a, CfgVal<T> b)
        {
            return !(a == b);
        }

        // Compare CfgVal<T> with raw T (CfgVal<T> == 42)
        public static bool operator ==(CfgVal<T> a, T b)
        {
            if (ReferenceEquals(a, null))
                return false;

            return Equals(a._value, b);
        }

        public static bool operator !=(CfgVal<T> a, T b)
        {
            return !(a == b);
        }

        public static bool operator ==(T a, CfgVal<T> b)
        {
            return b == a;
        }

        public static bool operator !=(T a, CfgVal<T> b)
        {
            return !(b == a);
        }
    }
}