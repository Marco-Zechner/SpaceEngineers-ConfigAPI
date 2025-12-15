using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace MarcoZechner.ApiLib
{
    public sealed class ApiConsumerBridge
    {
        private readonly ApiBootstrapConfig _cfg;
        private readonly ulong _consumerModId;
        private readonly string _consumerModName;
        private readonly IApiProvider _callbackApiInstance;
        private readonly Action<IApiProvider> _onApiLoaded;

        private Func<string, string, ulong, bool> _verify;
        private SetupApi _setupApi;

        public bool ApiLoaded { get; private set; }

        public ApiConsumerBridge(ulong consumerModId,
            string consumerModName,
            ApiBootstrapConfig cfg,
            IApiProvider callbackApiInstance,
            Action<IApiProvider> onApiLoaded)
        {
            _cfg = cfg;
            if (!ApiProviderHost.MajorVersionMatch(_cfg.ApiLibVersion, ApiConstants.API_LIB_VERSION))
            {
                throw new InvalidOperationException($"ApiLib version mismatch: Mod {_cfg.ApiProviderModId} uses {_cfg.ApiLibVersion}, but the imported library is {ApiConstants.API_LIB_VERSION}");
            }
            
            _consumerModId = consumerModId;
            _consumerModName = consumerModName;
            _callbackApiInstance = callbackApiInstance;
            _onApiLoaded = onApiLoaded;
        }

        public void Init()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(_cfg.DiscoveryChannel, OnProviderMessage);
            SendRequest();
        }

        public void Unload()
        {
            ApiLoaded = false;

            _setupApi?.Disconnect(_consumerModId);

            _setupApi = null;

            MyAPIGateway.Utilities.UnregisterMessageHandler(_cfg.DiscoveryChannel, OnProviderMessage);
        }

        private void SendRequest()
        {
            var header = new Dictionary<string, object>
            {
                { ApiConstants.HEADER_API_PROVIDER_MOD_ID_KEY, _cfg.ApiProviderModId },
                { ApiConstants.HEADER_PROTOCOL_KEY, ApiConstants.PROTOCOL },
                { ApiConstants.HEADER_INTENT_KEY, ApiConstants.INTENT_REQUEST },
                { ApiConstants.HEADER_API_VERSION_KEY, _cfg.ApiVersion },

                { ApiConstants.HEADER_FROM_MOD_ID_KEY, _consumerModId },
                { ApiConstants.HEADER_FROM_MOD_NAME_KEY, _consumerModName },

                { ApiConstants.HEADER_TARGET_MOD_ID_KEY, 0UL }, // let provider decide; caller may override externally if desired
                { ApiConstants.HEADER_TARGET_MOD_NAME_KEY, "Any" },

                { ApiConstants.HEADER_LAYOUT_KEY, "Header, Verify, Data" },
                { ApiConstants.HEADER_TYPES_KEY, "Dict<string,object>, null, null" }
            };

            object[] payload = { header, null, null };
            MyAPIGateway.Utilities.SendModMessage(_cfg.DiscoveryChannel, payload);
        }

        private void OnProviderMessage(object obj)
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

            if (!ApiCast.TryGet(header, ApiConstants.HEADER_API_PROVIDER_MOD_ID_KEY, out magic) || magic != _cfg.ApiProviderModId)
                return;

            if (!ApiCast.TryGet(header, ApiConstants.HEADER_PROTOCOL_KEY, out protocol) || protocol != ApiConstants.PROTOCOL)
                return;

            if (!ApiCast.TryGet(header, ApiConstants.HEADER_INTENT_KEY, out intent) || intent != ApiConstants.INTENT_ANNOUNCE)
                return;

            // Target id: 0 means broadcast/any; otherwise must match us
            ulong targetId;
            if (ApiCast.TryGet(header, ApiConstants.HEADER_TARGET_MOD_ID_KEY, out targetId))
            {
                if (targetId != 0UL && targetId != _consumerModId)
                    return;
            }

            Dictionary<string, Delegate> setupDict;
            if (!ApiCast.Try(payload[2], out setupDict))
                return;

            if (!ApiCast.Try(payload[1], out _verify))
                return;

            if (_verify != null && !_verify(_cfg.ApiVersion, _consumerModName, _consumerModId))
                return;

            // Connect
            _setupApi = new SetupApi(setupDict, ApiConstants.SETUP_KEY_CONNECT, ApiConstants.SETUP_KEY_DISCONNECT);

            var boundMainDict = _setupApi.Connect(_consumerModId, _consumerModName, _callbackApiInstance);
            _onApiLoaded?.Invoke(boundMainDict);
            ApiLoaded = boundMainDict != null;
        }
    }
}
