using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace MarcoZechner.ApiLib
{
    public sealed class ApiProviderHost
    {
        private readonly ApiBootstrapConfig _cfg;
        private readonly SetupApiProvider _setupProvider;
        
        private Func<string, string, ulong, bool> _verify;

        public ApiProviderHost(
            ApiBootstrapConfig cfg, 
            Func<ulong, string, IApiProvider, IApiProvider> connect, 
            Action<ulong> disconnect)
        {
            _cfg = cfg;
            if (!MajorVersionMatch(_cfg.ApiLibVersion, ApiConstants.API_LIB_VERSION))
            {
                throw new InvalidOperationException($"ApiLib version mismatch: Mod {_cfg.ApiProviderModId} uses {_cfg.ApiLibVersion}, but the imported library is {ApiConstants.API_LIB_VERSION}");
            }
            
            _setupProvider = new SetupApiProvider(
                connect,
                disconnect
            );
        }

        public void Load()
        {
            _verify = VerifyApi;
            MyAPIGateway.Utilities.RegisterMessageHandler(_cfg.DiscoveryChannel, OnDiscoveryMessage);
            // broadcast announce once
            SendAnnounce(0UL, "Any");
        }

        public void Unload()
        {
            _verify = null;
            MyAPIGateway.Utilities.UnregisterMessageHandler(_cfg.DiscoveryChannel, OnDiscoveryMessage);
        }

        private void OnDiscoveryMessage(object obj)
        {
            object[] payload;
            if (!ApiCast.Try(obj, out payload) || payload.Length != 3)
                return;

            Dictionary<string, object> header;
            if (!ApiCast.Try(payload[0], out header))
                return;

            string apiId;
            int protocol;
            string intent;

            if (!ApiCast.TryGet(header, ApiConstants.HEADER_API_PROVIDER_MOD_ID_KEY, out apiId) || apiId != _cfg.ApiProviderModId)
                return;

            if (!ApiCast.TryGet(header, ApiConstants.HEADER_PROTOCOL_KEY, out protocol) || protocol != ApiConstants.PROTOCOL)
                return;

            if (!ApiCast.TryGet(header, ApiConstants.HEADER_INTENT_KEY, out intent) || intent != ApiConstants.INTENT_REQUEST)
                return;
            
            // respect provider target if present: if target != 0 must match *this provider*
            // since ApiLib is generic, we only enforce "target == 0 OR equals expected provider id if present".
            // The caller can include provider id in header; if you want strict checks, do them outside.
            ulong fromId;
            string fromName;
            ApiCast.TryGet(header, ApiConstants.HEADER_FROM_MOD_ID_KEY, out fromId);
            ApiCast.TryGet(header, ApiConstants.HEADER_FROM_MOD_NAME_KEY, out fromName);

            SendAnnounce(fromId, fromName ?? "Unknown");
        }

        private void SendAnnounce(ulong targetModId, string targetModName)
        {
            var header = new Dictionary<string, object>
            {
                { ApiConstants.HEADER_API_PROVIDER_MOD_ID_KEY, _cfg.ApiProviderModId },
                { ApiConstants.HEADER_PROTOCOL_KEY, ApiConstants.PROTOCOL },
                { ApiConstants.HEADER_INTENT_KEY, ApiConstants.INTENT_ANNOUNCE },
                { ApiConstants.HEADER_API_VERSION_KEY, _cfg.ApiVersion },

                { ApiConstants.HEADER_TARGET_MOD_ID_KEY, targetModId },
                { ApiConstants.HEADER_TARGET_MOD_NAME_KEY, targetModName ?? "Any" },

                { ApiConstants.HEADER_LAYOUT_KEY, "Header, Verify, Data" },
                { ApiConstants.HEADER_TYPES_KEY, "Dict<string,object>, Func<string,string,ulong,bool>, Dict<string,Delegate>" }
            };

            ModMessage.Send(_cfg.DiscoveryChannel, header, _verify, _setupProvider);
        }
        
        private bool VerifyApi(string clientApiVersion, string clientModName, ulong clientModSteamId)
        {
            return MajorVersionMatch(clientApiVersion, _cfg.ApiVersion);
        }
        
        public static bool MajorVersionMatch(string clientApiVersion, string providerApiVersion)
        {
            var client = (clientApiVersion ?? "").Split('.');
            var provider = providerApiVersion.Split('.');

            if (client.Length != 3 || provider.Length != 3)
                return false;

            return client[0] == provider[0];
        }
        
        public static bool MinorVersionMatch(string clientApiVersion, string providerApiVersion)
        {
            var client = (clientApiVersion ?? "").Split('.');
            var provider = providerApiVersion.Split('.');

            if (client.Length != 3 || provider.Length != 3)
                return false;

            return client[0] == provider[0] && client[1] == provider[1];
        }
        
        public static bool PatchVersionMatch(string clientApiVersion, string providerApiVersion)
        {
            var client = (clientApiVersion ?? "").Split('.');
            var provider = providerApiVersion.Split('.');

            if (client.Length != 3 || provider.Length != 3)
                return false;

            return client[0] == provider[0] && client[1] == provider[1] && client[2] == provider[2];
        }
    }
}
