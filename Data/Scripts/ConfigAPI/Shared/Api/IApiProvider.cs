using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public interface IApiProvider
    {
        Dictionary<string, Delegate> ConvertToDict();
    }
}