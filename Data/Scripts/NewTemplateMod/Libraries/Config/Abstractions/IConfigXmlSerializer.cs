using System;
using mz.Config.Domain;

namespace mz.Config.Abstractions
{
    public interface IConfigXmlSerializer
    {
        string SerializeToXml(ConfigBase config);

        T DeserializeFromXml<T>(Type configType, string xml) where T :
            ConfigBase, new();
    }
}