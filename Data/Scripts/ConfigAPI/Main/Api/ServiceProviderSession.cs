using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared;
using MarcoZechner.ConfigAPI.Shared.Api;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ServiceProviderSession : MySessionComponentBase
    {
        // Stored callback APIs per consumer mod
        private static readonly Dictionary<ulong, ConfigUserHooks> _callbacksByMod
            = new Dictionary<ulong, ConfigUserHooks>();

        private ApiProviderHost _host;

        public override void LoadData()
        {
            _host = new ApiProviderHost(new ConfigApiBootstrap(), Connect, Disconnect);
            _host.Load();
            CfgLog.Info("ConfigAPI loaded");
        }

        protected override void UnloadData()
        {
            if (_host == null) return;
            
            _host.Unload();
            _host = null;
        }

        // Called by ApiLib when a consumer connects
        private static IApiProvider Connect(
            ulong consumerModId,
            string consumerModName,
            IApiProvider configUserHooks
        )
        {
            CfgLog.Info($"{consumerModId}:{consumerModName} connected to ConfigAPI");
            
            // store callbacks for provider -> consumer calls
            _callbacksByMod[consumerModId] = new ConfigUserHooks(configUserHooks);

            // return bound main api dict for this consumer
            return new ConfigServiceImpl(consumerModId, consumerModName, _callbacksByMod[consumerModId]);
        }

        // Called by ApiLib when a consumer disconnects, which means another mod on the same machine is probably unloading.
        // That normally only happens when the world unloads so we don't really need to do anything special here.
        // but it's here if you want to do some cleanup per mod.
        private static void Disconnect(ulong consumerModId)
        {
            CfgLog.Info($"{consumerModId} disconnected from ConfigAPI");
            _callbacksByMod.Remove(consumerModId);
        }
    }
}