using MarcoZechner.LoggingLite;
using VRage.Game.Components;

namespace MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared
{
    public sealed class CfgLog : LogBase<CfgLog>
    {
        protected override void ChangeConfig(LogConfig defaultConfig)
        {
            defaultConfig.ChatName = "ConfigAPI";
            defaultConfig.WarningInChat = true;
            defaultConfig.ErrorInChat = true;
            defaultConfig.InfoInChat = false;

            defaultConfig.DebugEnabled = false;
            defaultConfig.DebugInChat = false;

            defaultConfig.MaxLineChars = 400;
        }

        protected override string FileName => "Main.ConfigAPI.log";
        
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