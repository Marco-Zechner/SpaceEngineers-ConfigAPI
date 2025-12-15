using System;
using MarcoZechner.ApiLib;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public class CallbackApi : ICallbackApi
    {
        private readonly Action _testCallback = null;
        
        public CallbackApi(IApiProvider callbackApi)
        {
            var dict = callbackApi.ConvertToDict();
            Delegate d;
            if (dict != null && dict.TryGetValue("TestCallback", out d))
                _testCallback = (Action)d;
        }

        public void TestCallback() => _testCallback?.Invoke();
    }
}