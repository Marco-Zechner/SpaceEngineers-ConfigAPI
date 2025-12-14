using Sandbox.ModAPI;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Shared.Logging
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class CfgLogFlushSession : MySessionComponentBase
    {
        private bool _flushed = false;
        public override void BeforeStart()
        {
            if (_flushed) return;
            if (MyAPIGateway.Utilities == null) return;
            
            _flushed = true;
            CfgLog.Logger.FlushChatIfReady(true);
            MyAPIGateway.Utilities.ShowMessage(CfgLog.Logger.Source, "ExampleLogFlushSession: Flushed log to chat.");
        }
    }
}