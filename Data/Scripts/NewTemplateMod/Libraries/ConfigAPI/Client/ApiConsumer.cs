using MarcoZechner.ConfigAPI.Shared;
using MarcoZechner.ConfigAPI.Shared.Abstractions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace MarcoZechner.ConfigAPI.Client
{
    public static class ApiConsumer
    {
        public static bool IsApiAvailable => _apiInstance != null;
        public static IConfigApi Api
        {
            get
            {
                if (_apiInstance == null)
                    throw new System.Exception("ConfigAPI is not available!");
                return _apiInstance;
            }
        }

        private static IConfigApi _apiInstance;
        private const long MOD_ID = 1000; //TODO: find a better way

        public static void Init(string modId, string modName)
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_ID, ReceiveApi);
            MyAPIGateway.Utilities.SendModMessage(MOD_ID, null); // Request API
        }
        
        public static void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_ID, ReceiveApi);
        }

        private static void ReceiveApi(object obj)
        {
            _apiInstance = obj as IConfigApi;
            if (_apiInstance != null)
            {
                MyAPIGateway.Utilities.ShowMessage("Consumer", $"API Loaded, Value: {_apiInstance}");
            }
        }
    }
}