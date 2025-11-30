using System;
using System.IO;
using System.Xml.Serialization;
using mz.Config.Abstractions;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;

namespace NewTemplateMod.Tests
{
    public class TestXmlSerializer : IConfigXmlSerializer
    {
        public string SerializeToXml(ConfigBase config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var type = config.GetType();
            var serializer = new XmlSerializer(type);

            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, config);
                return sw.ToString();
            }
        }

        public T DeserializeFromXml<T>(Type configType, string xml) where T :
            ConfigBase, new()
        {
            if (configType == null)
                throw new ArgumentNullException(nameof(configType));
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));
            
            var serializer = new XmlSerializer(configType);
            using (var sr = new StringReader(xml))
            {
                var obj = serializer.Deserialize(sr);
                return obj as T;
            }
        }
    }
}