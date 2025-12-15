using System;
using System.Collections.Generic;

namespace MarcoZechner.ApiLib
{
    internal sealed class DictApiProvider : IApiProvider
    {
        private readonly Dictionary<string, Delegate> _dict;

        public DictApiProvider(Dictionary<string, Delegate> dict)
        {
            _dict = dict == null
                ? new Dictionary<string, Delegate>()
                : new Dictionary<string, Delegate>(dict);
        }

        public Dictionary<string, Delegate> ConvertToDict()
            => new Dictionary<string, Delegate>(_dict);
    }
}