using Sandbox.ModAPI;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Shared.Logging
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public sealed class CfgLogFlushSession : MySessionComponentBase
    {
        private bool _usedUtilities = false;
        public override void UpdateAfterSimulation()
        {
            if (_usedUtilities) return;
            if (MyAPIGateway.Utilities == null) return;
            
            // Replace with your real readiness flag if you have it:
            // CfgLog.LOG.FlushChatIfReady(ModSession.IsClientLoaded);
            CfgLog.Logger.FlushChatIfReady(true);
            _usedUtilities = true;
        }
    }
}