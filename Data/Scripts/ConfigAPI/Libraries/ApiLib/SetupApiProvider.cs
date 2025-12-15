using System;
using System.Collections.Generic;

namespace MarcoZechner.ApiLib
{
    public sealed class SetupApiProvider : IApiProvider
    {
        private readonly Func<ulong, string, IApiProvider, IApiProvider> _connect;
        private readonly Action<ulong> _disconnect;

        public SetupApiProvider(
            Func<ulong, string, IApiProvider, IApiProvider> connect,
            Action<ulong> disconnect
        )
        {
            _connect = connect;
            _disconnect = disconnect;
        }

        public Dictionary<string, Delegate> ConvertToDict()
        {
            Func<ulong, string, Dictionary<string, Delegate>, Dictionary<string, Delegate>> connectAdapter =
                (clientId, token, dict) =>
                {
                    var inputProvider = new DictApiProvider(dict);

                    var resultProvider = _connect(clientId, token, inputProvider);

                    return resultProvider.ConvertToDict();
                };
            
            return new Dictionary<string, Delegate>
            {
                { ApiConstants.SETUP_KEY_CONNECT, new Func<ulong, string, Dictionary<string, Delegate>, Dictionary<string, Delegate>>(connectAdapter) },
                { ApiConstants.SETUP_KEY_DISCONNECT, new Action<ulong>(_disconnect) }
            };
        }
    }
}