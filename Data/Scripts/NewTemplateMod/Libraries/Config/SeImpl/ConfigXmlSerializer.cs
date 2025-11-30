using System;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;
using Sandbox.ModAPI;

namespace mz.Config.SeImpl
{
    public class ConfigXmlSerializer : IConfigXmlSerializer
    {
        public string SerializeToXml(ConfigBase config)
        {
            return MyAPIGateway.Utilities.SerializeToXML(config);
        }

        public T DeserializeFromXml<T>(Type configType, string xml) where T :
            ConfigBase, new()
        {
            return MyAPIGateway.Utilities.SerializeFromXML<T>(xml);
        }
    }
}
