using System.IO;
using mz.Config.Abstractions.SE;
using mz.Config.Domain;
using Sandbox.ModAPI;

namespace mz.Config.SeImpl
{
    public class ConfigFileSystem : IConfigFileSystem
    {
        public bool TryReadFile(ConfigLocationType location, string fileName, out string content)
        {
            var utils = MyAPIGateway.Utilities;
            TextReader reader;
            content = null;
            switch (location)
            {
                default:
                case ConfigLocationType.World:
                    if (!utils.FileExistsInWorldStorage(fileName, typeof(ConfigFileSystem))) return false;
                    reader = utils.ReadFileInWorldStorage(fileName, typeof(ConfigFileSystem));
                    break;
                case ConfigLocationType.Global:
                    if (!utils.FileExistsInGlobalStorage(fileName)) return false;
                    reader = utils.ReadFileInGlobalStorage(fileName);
                    break;
                case ConfigLocationType.Local:
                    if (!utils.FileExistsInLocalStorage(fileName, typeof(ConfigFileSystem))) return false;
                    reader = utils.ReadFileInLocalStorage(fileName, typeof(ConfigFileSystem));
                    break;
            }
            content = reader.ReadToEnd();
            return true;
        }

        public void WriteFile(ConfigLocationType location, string fileName, string content)
        {
            var utils = MyAPIGateway.Utilities;
            
            switch (location)
            {
                default:
                case ConfigLocationType.World:
                    utils.WriteFileInWorldStorage(fileName, typeof(ConfigFileSystem));
                    break;
                case ConfigLocationType.Global:
                    utils.WriteFileInGlobalStorage(fileName);
                    break;
                case ConfigLocationType.Local:
                    utils.WriteFileInLocalStorage(fileName, typeof(ConfigFileSystem));
                    break;
            }
        }
    }
}