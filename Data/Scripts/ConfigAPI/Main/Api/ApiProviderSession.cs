using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;
using MarcoZechner.ConfigAPI.Shared.Logging;
using MarcoZechner.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ApiProviderSession : MySessionComponentBase
    {
        public static Logger<ConfigApiTopics> Log => CfgLog.Logger;
        
        private Func<string, string, ulong, bool> _verify;

        private MainApi _mainApi;
        public static readonly Dictionary<ulong, CallbackApi> CallbacksByMod
            = new Dictionary<ulong, CallbackApi>();

        public override void LoadData()
        {
            Log.Trace($"{nameof(ApiProviderSession)}.{nameof(LoadData)}");
            _verify = VerifyApi;
            _mainApi = new MainApi();

            MyAPIGateway.Utilities.RegisterMessageHandler(ApiConstant.DISCOVERY_CH, OnDiscoveryMessage);

            Log.Info(ConfigApiTopics.Api, 0, "Provider loaded, announcing API");
            SendAnnounce(0UL, "Any"); // broadcast
        }

        protected override void UnloadData()
        {
            Log.Trace($"{nameof(ApiProviderSession)}.{nameof(UnloadData)}");
            
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiConstant.DISCOVERY_CH, OnDiscoveryMessage);
            _mainApi = null;
            _verify = null;

            Log.CloseWriter();
        }

        private void OnDiscoveryMessage(object obj)
        {
            Log.Trace($"{nameof(ApiProviderSession)}.{nameof(OnDiscoveryMessage)}", $"{nameof(obj)}={obj}");
            object[] payload;
            if (!ApiCast.Try(obj, out payload) || payload.Length != 3)
                return;

            Dictionary<string, object> header;
            if (!ApiCast.Try(payload[0], out header))
                return;

            string magic;
            int protocol;
            string intent;
            string schema;

            if (!ApiCast.TryGet(header, ApiConstant.H_MAGIC, out magic) || magic != ApiConstant.MAGIC)
                return;

            if (!ApiCast.TryGet(header, ApiConstant.H_PROTOCOL, out protocol) || protocol != ApiConstant.PROTOCOL)
                return;

            if (!ApiCast.TryGet(header, ApiConstant.H_INTENT, out intent) || intent != ApiConstant.INTENT_REQUEST)
                return;

            if (!ApiCast.TryGet(header, ApiConstant.H_SCHEMA, out schema) || schema != ApiConstant.SCHEMA_MAIN_REQUEST)
                return;

            ulong targetId;
            if (ApiCast.TryGet(header, ApiConstant.H_TARGET_MOD_ID, out targetId))
            {
                if (targetId != 0UL && targetId != ApiConstant.PROVIDER_STEAM_ID)
                    return;
            }

            ulong fromId;
            string fromName;
            ApiCast.TryGet(header, ApiConstant.H_FROM_MOD_ID, out fromId);
            ApiCast.TryGet(header, ApiConstant.H_FROM_MOD_NAME, out fromName);

            Log.Debug(ConfigApiTopics.Discovery, 0, $"Received API request from {fromName ?? "?"} ({fromId})");
            SendAnnounce(fromId, fromName ?? "Unknown");
        }

        private void SendAnnounce(ulong targetModId, string targetModName)
        {
            Log.Trace($"{nameof(ApiProviderSession)}.{nameof(SendAnnounce)}", $"{nameof(targetModId)}={targetModId}, {nameof(targetModName)}={targetModName}");
            var header = new Dictionary<string, object>
            {
                { ApiConstant.H_MAGIC, ApiConstant.MAGIC },
                { ApiConstant.H_PROTOCOL, ApiConstant.PROTOCOL },
                { ApiConstant.H_SCHEMA, ApiConstant.SCHEMA_MAIN_ANNOUNCE },
                { ApiConstant.H_INTENT, ApiConstant.INTENT_ANNOUNCE },
                { ApiConstant.H_API_VERSION, ApiConstant.API_VERSION },

                { ApiConstant.H_FROM_MOD_ID, ApiConstant.PROVIDER_STEAM_ID },
                { ApiConstant.H_FROM_MOD_NAME, ApiConstant.PROVIDER_MOD_NAME },

                { ApiConstant.H_TARGET_MOD_ID, targetModId }, // 0 = broadcast
                { ApiConstant.H_TARGET_MOD_NAME, targetModName ?? "Any" },

                { ApiConstant.H_LAYOUT, "Header, Verify, Data" },
                { ApiConstant.H_TYPES,  "Dict<string,object>, Func<string,string,ulong,bool>, Dict<string,Delegate>" }
            };

            object[] payload = { header, _verify, _mainApi.ConvertToDict() };
            MyAPIGateway.Utilities.SendModMessage(ApiConstant.DISCOVERY_CH, payload);
        }

        private static bool VerifyApi(string clientApiVersion, string clientModName, ulong clientModSteamId)
        {
            Log.Trace($"{nameof(ApiProviderSession)}.{nameof(VerifyApi)}", $"\n\t{nameof(clientApiVersion)} = {clientApiVersion},\n\t{nameof(clientModName)} = {clientModName},\n\t{nameof(clientModSteamId)} = {clientModSteamId}\n");
            var client = (clientApiVersion ?? "").Split('.');
            var provider = ApiConstant.API_VERSION.Split('.');

            if (client.Length != 3 || provider.Length != 3)
                return false;

            return client[0] == provider[0];
        }
    }
}
