using System.Linq;
using MarcoZechner.ConfigAPI.Shared.Api;
using mz.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ApiProviderSession : MySessionComponentBase
    {
        // Local discovery channel (NOT the network channel).
        private const long DISCOVERY_CH = ApiConstant.DISCOVERY_CHANNEL_ID;

        private MainApi _api;

        public override void LoadData()
        {
            _api = new MainApi();

            MyAPIGateway.Utilities.RegisterMessageHandler(DISCOVERY_CH, OnDiscoveryMessage);
            MyAPIGateway.Utilities.MessageEnteredSender += DevCommands;

            // Announce once for mods that loaded before us.
            SendAnnounce();
        }

        private void DevCommands(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/cfgapi callbacks")) return;
            
            sendToOthers = false;
            var response = _api.CallbackApis
                .Aggregate("Registered Callback APIs:\n", 
                    (current, entry) => 
                        current + $"- Mod ID: {entry.Key}, API Type: {entry.Value?.GetType().FullName}\n"
                );
            MyAPIGateway.Utilities.ShowMessage("ConfigAPI", response);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(DISCOVERY_CH, OnDiscoveryMessage);
            _api = null;
        }

        private void OnDiscoveryMessage(object obj)
        {
            Chat.TryLine("ApiProviderSession: OnDiscoveryMessage", "Main");
            
            var msg = obj as DiscoveryMessage;
            if (msg == null) return;

            if (msg.ProtocolVersion != DiscoveryMessage.PROTOCOL_VERSION) return;

            if (msg.Kind != DiscoveryKind.PING) return;
            // Only respond to PING (avoid ping-pong storms).
            
            SendAnnounce();
        }

        private void SendAnnounce()
        {
            var announce = new DiscoveryMessage
            {
                ProtocolVersion = DiscoveryMessage.PROTOCOL_VERSION,
                Kind = DiscoveryKind.ANNOUNCE,
                FromModId = ulong.Parse(ModContext.ModId),
                FromModName = ModContext.ModName,
                Api = _api
            };

            MyAPIGateway.Utilities.SendModMessage(DISCOVERY_CH, announce);
        }
    }
}