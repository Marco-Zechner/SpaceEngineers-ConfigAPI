using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Shared.Api
{
    /// <summary>
    /// Bootstrap API announced via ModMessage.
    /// Used once per consumer mod to register callbacks
    /// and obtain a bound Main API instance.
    /// </summary>
    public interface ISetupApi
    {
        Dictionary<string, Delegate> Connect(
            ulong consumerModId,
            string consumerModName,
            Dictionary<string, Delegate> callbackApi
        );

        void Disconnect(ulong consumerModId);
    }
}