using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public sealed class MainApi : IMainApi
    {
        private readonly Action _test;

        public MainApi(Dictionary<string, Delegate> dict)
        {
            _test = (Action)dict["Test"];
        }

        public void Test() 
            => _test();
    }
}