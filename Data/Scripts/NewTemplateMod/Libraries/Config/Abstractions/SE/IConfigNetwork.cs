using mz.Config.Domain;

namespace mz.Config.Abstractions.SE
{
    /// <summary>
    /// Handles client-server communication for config operations
    /// in multiplayer scenarios.
    /// </summary>
    public interface IConfigNetwork
    {
        // client -> server requests
        void RequestLoad(ConfigLocationType location, string typeName, string fileName);
        void RequestSave(ConfigLocationType location, string typeName, string fileName);

        // server -> clients pushes (content is external format, not XML)
        void BroadcastConfig(ConfigLocationType location, string typeName, string fileName, string content);
    }
}