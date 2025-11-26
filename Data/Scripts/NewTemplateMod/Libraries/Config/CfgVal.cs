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
    }
}