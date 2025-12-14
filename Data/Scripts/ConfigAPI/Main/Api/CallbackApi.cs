using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Main.Api
{
    public class CallbackApi : ICallbackApi
    {
        private readonly Action _testCallback;
        public CallbackApi(Dictionary<string, Delegate> dict)
        {
            _testCallback = (Action)dict["TestCallback"];
        }

        public void TestCallback()
            => _testCallback();
    }
}