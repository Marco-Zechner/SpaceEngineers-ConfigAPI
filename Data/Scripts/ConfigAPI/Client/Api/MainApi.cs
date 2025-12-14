using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public sealed class MainApi : IMainApi
    {
        private readonly Action<ulong, string, Dictionary<string, Delegate>> _registerCallbacks;
        private readonly Action _test;

        public MainApi(Dictionary<string, Delegate> dict)
        {
            _registerCallbacks = (Action<ulong, string, Dictionary<string, Delegate>>)dict["RegisterCallbacks"];
            _test = (Action)dict["Test"];
        }
        
        /// <summary>
        /// Called automatically by consumer mods after obtaining the main API.
        /// </summary>
        /// <param name="modId"></param>
        /// <param name="modName"></param>
        /// <param name="callbacks"></param>
        public void RegisterCallbacks(ulong modId, string modName, Dictionary<string, Delegate> callbacks) 
            => _registerCallbacks(modId, modName, callbacks);

        public void Test() 
            => _test();
    }
}