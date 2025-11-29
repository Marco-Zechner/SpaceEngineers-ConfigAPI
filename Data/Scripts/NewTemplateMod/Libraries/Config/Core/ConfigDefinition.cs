using System;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
    public class ConfigDefinition<T> : IConfigDefinition
        where T : ConfigBase, new()
    {
        private readonly string _typeName;
        private readonly string _sectionName;

        public ConfigDefinition(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException("sectionName");

            _typeName = typeof(T).Name;
            _sectionName = sectionName;
        }

        public string TypeName
        {
            get { return _typeName; }
        }

        public string SectionName
        {
            get { return _sectionName; }
        }

        public Type ConfigType
        {
            get { return typeof(T); }
        }

        public ConfigBase CreateDefaultInstance()
        {
            return new T();
        }
    }
}
