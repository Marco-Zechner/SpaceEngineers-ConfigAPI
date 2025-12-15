using System;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    public sealed class MainApi : IMainApi
    {
        private readonly Action _test;

        public MainApi(IApiProvider mainApi)
        {
            var dict = mainApi.ConvertToDict();
            _test = (Action)dict["Test"];
        }

        public void Test() 
            => _test();
    }
}