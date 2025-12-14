using System;
using System.Collections.Generic;

namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public interface IApi
    {
        Dictionary<string, Delegate> ConvertToDict();
    }
}