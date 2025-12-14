using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Shared.Api;
using MarcoZechner.ConfigAPI.Shared.Logging;

namespace MarcoZechner.ConfigAPI.Client.Api
{
    internal sealed class CallbackApiProvider : ICallbackApi, IApiProvider
    {
        public void TestCallback()
        {
            ApiBridge.Log.Info(ConfigApiTopics.Callbacks, 0, "TestCallback invoked");
        }
        // Phase 0.2: empty
        // Later phases:
        // - LoadFile
        // - SaveFile
        // - BackupFile
        // - Serialize
        // - Deserialize
        // - NewDefault
        
        public Dictionary<string, Delegate> ConvertToDict()
        {
            return new Dictionary<string, Delegate>
            {
                { "TestCallback", new Action(TestCallback) },
            };
        }
    }
}