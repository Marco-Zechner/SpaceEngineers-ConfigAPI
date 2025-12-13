using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public sealed class MainApi : IMainApi
    {
        public readonly Dictionary<ulong, object> CallbackApis
            = new Dictionary<ulong, object>();

        public void AddCallbackApi(ulong modId, string modName, object callbackApi)
        {
            // Store it even if null? I’d treat null as unregister.
            if (callbackApi == null)
            {
                CallbackApis.Remove(modId);
                return;
            }

            CallbackApis[modId] = callbackApi;
        }

        public bool IsCallbackRegistered(ulong modId)
        {
            return CallbackApis.ContainsKey(modId);
        }
    }
}