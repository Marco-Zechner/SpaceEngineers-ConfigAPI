using MarcoZechner.Logging;

namespace MarcoZechner.ConfigAPI.Shared.Logging
{
    public static class CfgLog
    {
        public static readonly Logger<ConfigApiTopics> Logger =
            MarcoZechner.Logging.Logger.Get<ConfigApiTopics>(
                "Main.ConfigAPI",
                "Main.ConfigAPI.log",
                initConfig: cfg =>
                {
                    // policy
                    cfg.ErrorOutput = LogOutput.FileAndChat;
                    cfg.WarningOutput = LogOutput.FileAndChat;

                    // default: quiet unless enabled
                    cfg.DefaultRule.Enabled = false;

                    // enable useful baseline logs
                    cfg.SetRule(ConfigApiTopics.Api, enabled: true, maxDetail: 1, output: LogOutput.FileAndChat);
                    cfg.SetRule(ConfigApiTopics.Discovery, enabled: true, maxDetail: 0, output: LogOutput.FileAndChat);

                    // callbacks can be noisy; keep off unless debugging
                    cfg.SetRule(ConfigApiTopics.Callbacks, enabled: true, maxDetail: 0, output: LogOutput.FileAndChat);
                });
    }
}