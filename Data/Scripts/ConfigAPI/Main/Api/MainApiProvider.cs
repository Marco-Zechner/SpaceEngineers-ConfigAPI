using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;
using MarcoZechner.ConfigAPI.Shared.Logging;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public class MainApiProvider : IMainApi, IApiProvider
    {
        public void RegisterCallbacks(ulong modId, string modName, Dictionary<string, Delegate> callbacks)
        {
            ApiProviderSession.Log.Trace($"{nameof(ApiProviderSession)}.{nameof(RegisterCallbacks)}", $"{nameof(modId)}={modId}, {nameof(modName)}={modName}, {nameof(callbacks)}={callbacks}");
            if (callbacks == null) return;
            ApiProviderSession.CallbacksByMod[modId] = new CallbackApi(callbacks);

            ApiProviderSession.Log.Info(ConfigApiTopics.Callbacks, 0, $"Registered callbacks: {modName} ({modId})");
        }

        public void Test()
        {
            ApiProviderSession.Log.Trace($"{nameof(ApiProviderSession)}.{nameof(Test)}", $"{nameof(Test)}");
        }

        public Dictionary<string, Delegate> ConvertToDict()
        {
            ApiProviderSession.Log.Trace($"{nameof(ApiProviderSession)}.{nameof(ConvertToDict)}");
            return new Dictionary<string, Delegate>
            {
                { "RegisterCallbacks", new Action<ulong, string, Dictionary<string, Delegate>>(RegisterCallbacks) },
                { "Test", new Action(Test) },
            };
        }
    }
}