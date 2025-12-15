using System;
using System.Collections.Generic;

namespace MarcoZechner.ApiLib
{
    public sealed class SetupApi
    {
        private readonly Func<ulong, string, IApiProvider, IApiProvider> _connect;
        private readonly Action<ulong> _disconnect;

        public SetupApi(Dictionary<string, Delegate> dict, string keyConnect, string keyDisconnect)
        {
            var connectDict =
                (Func<ulong, string, Dictionary<string, Delegate>, Dictionary<string, Delegate>>)dict[keyConnect];

            _disconnect = (Action<ulong>)dict[keyDisconnect];
            
            _connect = (modId, modName, callbacks) =>
            {
                var callbacksDict = callbacks.ConvertToDict() ?? new Dictionary<string, Delegate>();

                var resultDict = connectDict(modId, modName, callbacksDict);

                return new DictApiProvider(resultDict);
            };
        }

        public IApiProvider Connect(ulong modId, string modName, IApiProvider callbacks)
        {
            return _connect(modId, modName, callbacks);
        }

        public void Disconnect(ulong modId)
        {
            _disconnect(modId);
        }
    }
}