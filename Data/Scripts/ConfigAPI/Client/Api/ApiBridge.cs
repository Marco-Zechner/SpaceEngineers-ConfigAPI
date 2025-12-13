using MarcoZechner.ConfigAPI.Shared.Api;
using mz.Logging;
using Sandbox.ModAPI;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public static class ApiBridge
    {
        private const long DISCOVERY_CH = ApiConstant.DISCOVERY_CHANNEL_ID;

        private static IMainApi _mainApi;

        // Store these for retry if provider announces before we know our identity.
        private static ulong _modId;
        private static string _modName;
        private static ICallbackApi _callbackApi;

        public static bool IsReady => _mainApi != null;

        public static void Init(ulong modId, string modName)
        {
            _modId = modId;
            _modName = modName;
            _callbackApi = new CallbackApi();

            MyAPIGateway.Utilities.RegisterMessageHandler(DISCOVERY_CH, OnDiscoveryMessage);

            // Ping once (provider will reply with announce).
            var ping = new DiscoveryMessage
            {
                ProtocolVersion = DiscoveryMessage.PROTOCOL_VERSION,
                Kind = DiscoveryKind.PING,
                FromModId = modId,
                FromModName = modName,
                Api = null
            };
            MyAPIGateway.Utilities.SendModMessage(DISCOVERY_CH, ping);
        }

        public static void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(DISCOVERY_CH, OnDiscoveryMessage);
            _mainApi = null;
            _callbackApi = null;
        }

        private static void OnDiscoveryMessage(object obj)
        {
            Chat.TryLine("ApiBridge: OnDiscoveryMessage", "Client");
            
            var msg = obj as DiscoveryMessage;
            if (msg == null) return;

            if (msg.ProtocolVersion != DiscoveryMessage.PROTOCOL_VERSION) return;
            if (msg.Kind != DiscoveryKind.ANNOUNCE) return;

            var api = msg.Api as IMainApi;
            if (api == null) return;

            _mainApi = api;

            // Phase B: register callback API (one-way, idempotent).
            _mainApi.AddCallbackApi(_modId, _modName, _callbackApi);
        }

        public static IMainApi MainApi
        {
            get
            {
                if (_mainApi == null) throw new System.Exception("ConfigAPI Main API not available yet.");
                return _mainApi;
            }
        }
        
        public static ICallbackApi CallbackApi => _callbackApi;
    }
}