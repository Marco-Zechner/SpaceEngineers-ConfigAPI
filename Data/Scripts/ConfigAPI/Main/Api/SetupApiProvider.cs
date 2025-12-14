using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public sealed class SetupApiProvider : ISetupApi, IApiProvider
    {
        public Dictionary<string, Delegate> Connect(
            ulong consumerModId,
            string consumerModName,
            Dictionary<string, Delegate> callbackApi
        )
        {
            if (callbackApi == null)
                throw new ArgumentNullException(nameof(callbackApi));

            var callbackApiInstance = new CallbackApi(callbackApi);
            
            // Store callback API for this mod
            ApiProviderSession.CallbacksByMod[consumerModId] = callbackApiInstance;

            // Create a bound main API instance
            var boundMain = new MainApiBoundProvider(
                consumerModId,
                consumerModName,
                callbackApiInstance
            );

            // Return delegate dict for the bound API
            return boundMain.ConvertToDict();
        }

        public void Disconnect(ulong consumerModId)
        {
            ApiProviderSession.CallbacksByMod.Remove(consumerModId);
        }

        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                {
                    "Connect",
                    new Func<ulong, string, Dictionary<string, Delegate>, Dictionary<string, Delegate>>(Connect)
                },
                {
                    "Disconnect",
                    new Action<ulong>(Disconnect)
                }
            };
        }
    }
}