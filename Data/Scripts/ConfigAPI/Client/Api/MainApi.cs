using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public sealed class MainApi : IMainApi
    {
        private readonly Action<ulong, string, Dictionary<string, Delegate>> _registerCallbacks;

        public MainApi(Dictionary<string, Delegate> dict)
        {
            _registerCallbacks = (Action<ulong, string, Dictionary<string, Delegate>>)dict["RegisterCallbacks"];
        }

        public void RegisterCallbacks(ulong modId, string modName, Dictionary<string, Delegate> callbacks)
        {
            _registerCallbacks(modId, modName, callbacks);
        }
    }
}