using System;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;

namespace mz.Config.Core.Storage
{
    /// <summary>
    /// Default config definition: ties a ConfigBase-derived type T to a type name,
    /// section name, and XML deserialization.
    /// </summary>
    public sealed class ConfigDefinition<T> : IConfigDefinition
        where T : ConfigBase, new()
    {
        /// <summary>
        /// Uses typeof(T).Name as section name.
        /// </summary>
        public ConfigDefinition() : this(typeof(T).Name)
        {
        }

        /// <summary>
        /// Explicit section name. This is currently not used by the core,
        /// but kept for future flexibility.
        /// </summary>
        public ConfigDefinition(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException(nameof(sectionName));
            SectionName = sectionName;
        }

        public string TypeName => typeof(T).Name;

        public string SectionName { get; private set; }

        public Type ConfigType => typeof(T);

        public ConfigBase CreateDefaultInstance()
        {
            return new T();
        }

        public ConfigBase DeserializeFromXml(IConfigXmlSerializer xmlSerializer, string xml)
        {
            if (xmlSerializer == null)
                throw new ArgumentNullException(nameof(xmlSerializer));
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            var instance = xmlSerializer.DeserializeFromXml<T>(xml);
            return instance;
        }
    }
}