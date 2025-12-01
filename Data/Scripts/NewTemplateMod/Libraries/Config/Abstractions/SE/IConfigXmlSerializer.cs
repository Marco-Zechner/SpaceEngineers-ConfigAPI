using mz.Config.Domain;

namespace mz.Config.Abstractions.SE
{
    /// <summary>
    /// Thin wrapper around the Space Engineers XML serializer.
    /// This is the only place in the library that talks to MyAPIGateway.Utilities
    /// for SerializeToXML / SerializeFromXML.
    /// </summary>
    public interface IConfigXmlSerializer
    {
        /// <summary>
        /// Serialize a config instance to SE's XML string.
        /// </summary>
        string SerializeToXml(ConfigBase config);

        /// <summary>
        /// Deserialize a SE XML string into a config instance of type T.
        /// </summary>
        T DeserializeFromXml<T>(string xml) where T : ConfigBase, new();
    }
}