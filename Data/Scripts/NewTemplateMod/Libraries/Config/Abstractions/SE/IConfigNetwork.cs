using mz.Config.Domain;

namespace mz.Config.Abstractions.SE
{
        public interface IConfigNetwork
    {
        // client -> server requests
        void RequestLoad(ConfigLocationType location, string typeName, string fileName);
        void RequestSave(ConfigLocationType location, string typeName, string fileName);

        // server -> clients pushes
        void BroadcastConfig(ConfigLocationType location, string typeName, string fileName, string tomlContent);
    }
}