using System.Collections.Generic;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Shared.Api;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ServiceProviderSession : MySessionComponentBase
    {
        // Stored callback APIs per consumer mod
        public static readonly Dictionary<ulong, ConfigUserHooks> CallbacksByMod
            = new Dictionary<ulong, ConfigUserHooks>();

        private ApiProviderHost _host;

        public override void LoadData()
        {
            _host = new ApiProviderHost(new ConfigApiBootstrap(), Connect, Disconnect);
            _host.Load();
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
            IApiProvider yourCallbackApi
        )
        {
            // store callbacks for provider -> consumer calls
            CallbacksByMod[consumerModId] = new ConfigUserHooks(yourCallbackApi);

            // return bound main api dict for this consumer
            return new ConfigServiceImpl(consumerModId, consumerModName, CallbacksByMod[consumerModId]);
        }

        // Called by ApiLib when a consumer disconnects, which means another mod on the same machine is probably unloading.
        // That normally only happens when the world unloads so we don't really need to do anything special here.
        // but it's here if you want to do some cleanup per mod.
        private static void Disconnect(ulong consumerModId)
        {
            CallbacksByMod.Remove(consumerModId);
        }
    }
}