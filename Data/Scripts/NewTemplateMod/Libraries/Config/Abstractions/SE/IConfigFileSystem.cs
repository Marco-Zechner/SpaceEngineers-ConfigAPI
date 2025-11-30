using mz.Config.Domain;

namespace mz.Config.Abstractions.SE
{
       public interface IConfigFileSystem
    {
        bool TryReadFile(ConfigLocationType location, string fileName, out string content);
        void WriteFile(ConfigLocationType location, string fileName, string content);
        string GetDefaultFileName(IConfigDefinition definition);
    }
}