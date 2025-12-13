using mz.Config.Core.Storage;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace mz.Config.SeImpl
{
    public static class ConfigMultiplayerFileSystem
    {
        private static readonly IMyUtilities _utils;

        static ConfigMultiplayerFileSystem()
        {
            _utils = MyAPIGateway.Utilities;
        }

        private static string GetVariableId(string fileName) 
            => $"{InternalConfigStorage.StoragePrefix}.{fileName}";

        public static bool TryReadWorldFile(string fileName, out string content)
        {
            content = null;
            return ModSession.IsServer 
                ? TryReadServerFile(fileName, out content) 
                : TryReadClientFile(fileName, out content);
        }

        private static bool TryReadServerFile(string fileName, out string content)
        {
            content = null;
            if (!_utils.FileExistsInWorldStorage(fileName, typeof(ConfigMultiplayerFileSystem))) return false;
            var reader = _utils.ReadFileInWorldStorage(fileName, typeof(ConfigMultiplayerFileSystem));
            content = reader.ReadToEnd();
            reader.Close();
            
            _utils.SetVariable(GetVariableId(fileName), content);
            //todo send over network
            return true;
        }
        
        private static bool TryReadClientFile(string fileName, out string content) 
            => _utils.GetVariable(GetVariableId(fileName), out content);

        public static void WriteWorldFile(string fileName, string content)
        {
            if (ModSession.IsServer)
                WriteServerFile(fileName, content);
            else
                WriteClientFile(fileName, content);
        }

        private static void WriteServerFile(string fileName, string content)
        { 
            var writer = _utils.WriteFileInWorldStorage(fileName, typeof(ConfigMultiplayerFileSystem));
            writer.Write(content);
            writer.Flush();
            writer.Close();
            _utils.SetVariable(GetVariableId(fileName), content);
            //todo send over network
        }

        private static void WriteClientFile(string fileName, string content)
        {
            //todo check if admin, then send request over network to server to write the file
        }
        
        public static bool ExistsWorldFile(string fileName)
        {
            string _;
            return ModSession.IsServer
                ? _utils.FileExistsInWorldStorage(fileName, typeof(ConfigMultiplayerFileSystem))
                : _utils.GetVariable(GetVariableId(fileName), out _);
        }
    }
}