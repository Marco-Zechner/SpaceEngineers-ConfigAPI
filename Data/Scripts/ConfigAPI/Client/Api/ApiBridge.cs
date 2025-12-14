using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;
using MarcoZechner.ConfigAPI.Shared.Logging;
using MarcoZechner.Logging;
using Sandbox.ModAPI;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public static class ApiBridge
    {
        public static Logger<ConfigApiTopics> Log => CfgLog.Logger;
        
        public static bool ApiLoaded { get; private set; }

        private static Func<string, string, ulong, bool> _verify;
        private static Dictionary<string, Delegate> _providerDict;

        public static MainApi Api
        {
            get
            {
                if (ApiLoaded) return _api;
                
                Log.Error(ConfigApiTopics.Api, "API not yet loaded.");
                return null;
            }
        }

        private static MainApi _api;
        private static CallbackApiProvider _callback;

        private static ulong _consumerModId;
        private static string _consumerModName;

        public static void Init(ulong modId, string modName)
        {
            Log.Trace("ApiBridge.Init", $"{nameof(modId)} {modId}, {nameof(modName)} {modName}");
            _consumerModId = modId;
            _consumerModName = modName;

            _callback = new CallbackApiProvider();

            MyAPIGateway.Utilities.RegisterMessageHandler(ApiConstant.DISCOVERY_CH, OnProviderMessage);

            Log.Info(ConfigApiTopics.Api, 1, "ApiBridge.Init -> sending API request");

            SendRequest();
        }

        private static void SendRequest()
        {
            var header = new Dictionary<string, object>
            {
                { ApiConstant.H_MAGIC, ApiConstant.MAGIC },
                { ApiConstant.H_PROTOCOL, ApiConstant.PROTOCOL },
                { ApiConstant.H_SCHEMA, ApiConstant.SCHEMA_MAIN_REQUEST },
                { ApiConstant.H_INTENT, ApiConstant.INTENT_REQUEST },
                { ApiConstant.H_API_VERSION, ApiConstant.API_VERSION },

                { ApiConstant.H_FROM_MOD_ID, _consumerModId },
                { ApiConstant.H_FROM_MOD_NAME, _consumerModName },

                { ApiConstant.H_TARGET_MOD_ID, ApiConstant.PROVIDER_STEAM_ID },
                { ApiConstant.H_TARGET_MOD_NAME, ApiConstant.PROVIDER_MOD_NAME },

                { ApiConstant.H_LAYOUT, "Header, Verify, Data" },
                { ApiConstant.H_TYPES,  "Dict<string,object>, null, null" }
            };

            object[] payload = { header, null, null };
            MyAPIGateway.Utilities.SendModMessage(ApiConstant.DISCOVERY_CH, payload);
        }

        public static void Unload()
        {
            Log.Trace("ApiBridge.Unload");
            
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiConstant.DISCOVERY_CH, OnProviderMessage);
            ApiLoaded = false;
            _api = null;
            _callback = null;
        }

        private static void OnProviderMessage(object obj)
        {
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

            if (!ApiCast.TryGet(header, ApiConstant.H_INTENT, out intent) || intent != ApiConstant.INTENT_ANNOUNCE)
                return;

            if (!ApiCast.TryGet(header, ApiConstant.H_SCHEMA, out schema) || schema != ApiConstant.SCHEMA_MAIN_ANNOUNCE)
                return;

            // Respect target id: 0 means broadcast/any; otherwise must match us.
            ulong targetId;
            if (ApiCast.TryGet(header, ApiConstant.H_TARGET_MOD_ID, out targetId))
            {
                if (targetId != 0UL && targetId != _consumerModId)
                    return;
            }

            if (!ApiCast.Try(payload[1], out _verify))
                return;

            if (!ApiCast.Try(payload[2], out _providerDict))
                return;

            if (!_verify(ApiConstant.API_VERSION, _consumerModName, _consumerModId))
            {
                Log.Warning(ConfigApiTopics.Api, "Verify failed, ignoring provider API");
                return;
            }

            try
            {
                _api = new MainApi(_providerDict);
                _api.RegisterCallbacks(_consumerModId, _consumerModName, _callback.ConvertToDict());
                ApiLoaded = true;

                Log.Info(ConfigApiTopics.Api, 0, "API loaded + callbacks registered");
            }
            catch (Exception e)
            {
                Log.Error(ConfigApiTopics.Api, "API load error: " + e.Message);
            }
        }
    }
}
