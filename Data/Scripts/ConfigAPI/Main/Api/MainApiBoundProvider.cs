using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    /// <summary>
    /// Main API bound to a single consumer mod.
    /// No modId needs to be passed on calls anymore.
    /// </summary>
    public sealed class MainApiBoundProvider : IApiProvider
    {
        private readonly ulong _consumerModId;
        private readonly string _consumerModName;
        private readonly CallbackApi _callbackApi;

        public MainApiBoundProvider(ulong modId, string modName, CallbackApi callbackApi)
        {
            _consumerModId = modId;
            _consumerModName = modName;
            _callbackApi = callbackApi;
        }

        public void Test()
        {
            _callbackApi.TestCallback();
        }

        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                { "Test", new Action(Test) }
            };
        }
    }
}