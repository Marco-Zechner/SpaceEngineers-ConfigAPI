using MarcoZechner.LoggingLite;

namespace MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared
{
    public sealed class CfgLog : LogBase<CfgLog>
    {
        protected override string FileName => "Client.ConfigAPI.log";

        protected override void Configure(LogConfig c)
        {
            c.ChatName = "ConfigAPI";
            c.WarningInChat = true;
            c.ErrorInChat = true;
            c.InfoInChat = false;

            c.DebugEnabled = false;
            c.DebugInChat = false;

            c.MaxLineChars = 400;
        }
    }
}