using System;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;

namespace mz.Config.Core.Storage
{
    public sealed class ConfigDefinition<T> : IConfigDefinition
        where T : ConfigBase, new()
    {
        public ConfigDefinition(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException(nameof(sectionName));
            SectionName = sectionName;
        }

        public string TypeName => typeof(T).Name;

        public string SectionName { get; }

        public Type ConfigType => typeof(T);

        public ConfigBase CreateDefaultInstance() => new T();

        public ConfigBase DeserializeFromXml(IConfigXmlSerializer xmlSerializer, string xml)
        {
            if (xmlSerializer == null)
                throw new ArgumentNullException(nameof(xmlSerializer));
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            var instance = xmlSerializer.DeserializeFromXml<T>(typeof(T), xml);
            return instance;
        }
    }
}
