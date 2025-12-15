using System;
using System.Collections.Generic;

namespace MarcoZechner.ApiLib
{
    public interface IApiProvider
    {
        Dictionary<string, Delegate> ConvertToDict();
    }
}