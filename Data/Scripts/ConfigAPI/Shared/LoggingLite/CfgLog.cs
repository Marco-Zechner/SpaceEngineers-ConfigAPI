using MarcoZechner.LoggingLite;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared
{
    public sealed class CfgLog : LogBase<CfgLog>
    {
        protected override string FileName => "Main.ConfigAPI.log";

        public CfgLog()
        {
            Config.ChatName = "ConfigAPI";
            Config.WarningInChat = true;
            Config.ErrorInChat = true;
            Config.InfoInChat = false;

            Config.DebugEnabled = false;
            Config.DebugInChat = false;

            Config.MaxLineChars = 400;
        }
        
        [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
        public sealed class CfgLogFlushSession : MySessionComponentBase
        {
            public override void BeforeStart()
            {
                TryFlushChat();
            }
            
            protected override void UnloadData()
            {
                base.UnloadData();
                Close();
            }
        }
    }
}