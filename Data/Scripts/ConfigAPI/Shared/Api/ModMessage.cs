using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public static class ModMessage
    {
        public static void Send(
            Dictionary<string, object> header, 
            Func<string,string,ulong,bool> versionVerifyFunc,
            IApiProvider apiProvider
        )
        {
            object[] payload = { header, versionVerifyFunc, apiProvider.ConvertToDict() };
            MyAPIGateway.Utilities.SendModMessage(ApiConstant.DISCOVERY_CH, payload);
        }
    }
}