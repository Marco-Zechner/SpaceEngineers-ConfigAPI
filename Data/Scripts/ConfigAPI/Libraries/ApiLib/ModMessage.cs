using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace MarcoZechner.ApiLib
{
    public static class ModMessage
    {
        public static void Send(
            long discoveryChannel,
            Dictionary<string, object> header,
            Func<string, string, ulong, bool> versionVerifyFunc,
            IApiProvider apiProvider
        )
        {
            object[] payload = { header, versionVerifyFunc, apiProvider.ConvertToDict() };
            MyAPIGateway.Utilities.SendModMessage(discoveryChannel, payload);
        }
    }
}