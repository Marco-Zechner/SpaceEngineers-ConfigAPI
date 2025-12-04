using System.Collections.Generic;
using mz.Config.Domain;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
public class AdvancedConfig : ConfigBase
{
        public override SemanticVersion ConfigVersion => "0.5.0";

        public SettingsRoot Settings { get; set; } = new SettingsRoot();

        public class SettingsRoot
        {
            public DisplayConfig Display { get; set; } = new DisplayConfig();
            public NetworkConfig Network { get; set; } = null;
        }

        public class DisplayConfig
        {
            public int Width { get; set; } = 1920;
            public int Height { get; set; } = 1080;
            public string Theme { get; set; } = "Dark";
            public float? Dpi { get; set; } = null; // stress-test nullables
        }

        public class NetworkConfig
        {
            public string Host { get; set; } = "localhost";
            public int Port { get; set; } = 8080;

            public bool UseTls { get; set; } = true;
        }
    }


}