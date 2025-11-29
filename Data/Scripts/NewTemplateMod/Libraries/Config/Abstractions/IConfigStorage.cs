using mz.Config.Domain;

namespace mz.Config.Abstractions
{
    public interface IConfigStorage
    {
        void RegisterConfig(IConfigDefinition definition);

        // For listing
        IConfigDefinition[] GetRegisteredDefinitions();

        // Access current in-memory config
        T GetOrCreate<T>(ConfigLocationType location) where T : ConfigBase;

        // File tracking
        string GetCurrentFileName(ConfigLocationType location, string typeName);
        void SetCurrentFileName(ConfigLocationType location, string typeName, string fileName);

        // Load / save APIs
        bool Load(ConfigLocationType location, string typeName, string fileName);
        bool Save(ConfigLocationType location, string typeName, string fileName);

        // Show / showfile
        string GetConfigAsText(ConfigLocationType location, string typeName);
        string GetFileAsText(ConfigLocationType location, string fileName);
    }
}