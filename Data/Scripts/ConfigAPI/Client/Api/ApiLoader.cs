using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public static class ApiLoader
    {
        public static bool ApiLoaded => _bridge != null && _bridge.ApiLoaded;

        private static ApiConsumerBridge _bridge;
        private static ConfigApi _api;

        public static ConfigApi Api => ApiLoaded ? _api : null;

        public static void Init(ulong modId, string modName)
        {
            _bridge = new ApiConsumerBridge(
                modId,
                modName,
                new ConfigApiBootstrap(),
                new ConfigCallbackApiImpl(), 
                SetMainApi
            );
            
            _bridge.Init();
        }

        public static void Unload()
        {
            if (_bridge != null)
            {
                _bridge.Unload();
                _bridge = null;
            }

            _api = null;
        }
        
        private static void SetMainApi(IApiProvider mainApi) => _api = new ConfigApi(mainApi);
    }
}