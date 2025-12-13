using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using mz.Config.Abstractions.SE;
using mz.Config.Core;
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
        
        private const string INVALID_ENUM_PATTERN = @"'(?<value>[^']+)' is not a valid value for (?<enum>\w+)";

        public T DeserializeFromXml<T>(string xml) where T : ConfigBase, new()
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            var serializer = new XmlSerializer(typeof(T));
            using (var sr = new StringReader(xml))
            {
                try
                {
                    var obj = serializer.Deserialize(sr);
                    return obj as T;
                    
                }
                catch (Exception ex) // catch invalid enum deserialization
                {
                    var message = ex.InnerException?.Message;
                    if (message == null) throw;
                    var match = Regex.Match(message, INVALID_ENUM_PATTERN);
                    ConfigStorage.Debug?.Log(message);
                    if (!match.Success)
                    {
                        ConfigStorage.Debug?.Log(
                            $"Deserialization error in ConfigType {typeof(T).Name}: {message}");
                        return null;
                    }
                
                    var enumName = match.Groups["enum"].Value;
                    var attemptedValue = match.Groups["value"].Value;
                    ConfigStorage.Debug?.Log(
                        $"Deserialization error in ConfigType {typeof(T).Name}: '{attemptedValue}' is not a valid value for enum '{enumName}'.");
                    return null;
                }
            }
        }
    }
}