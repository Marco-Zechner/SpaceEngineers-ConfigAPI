using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public interface IMainApi
    {
        // Consumer mods call this once after they obtained the main API.
        // ConfigAPIMod stores these callbacks and uses them for routing.
        void RegisterCallbacks(ulong modId, string modName, Dictionary<string, Delegate> callbacks);

        void Test();
    }
}