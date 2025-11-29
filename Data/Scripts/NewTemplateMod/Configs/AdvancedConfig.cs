using System.Collections.Generic;
using mz.Config;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
public class AdvancedConfig : ConfigBase
{
        public override SemanticVersion ConfigVersion => "0.5.0";

        public TriggerSave<SettingsRoot> Settings { get; set; } = new();

        public class SettingsRoot
        {
            public TriggerSave<List<IPluginConfig>> Plugins { get; set; } = new ();
            public TriggerSave<DisplayConfig> Display { get; set; } = new();
            public TriggerSave<NetworkConfig?> Network { get; set; } = null;
        }

        public interface IPluginConfig
        {
            string Id { get; }
        }

        public class MathPluginConfig : IPluginConfig
        {
            public string Id => "MathPlugin";
            public TriggerSave<int> Precision { get; set; } = 6;
        }

        public class GraphicsPluginConfig : IPluginConfig
        {
            public string Id => "GraphicsPlugin";
            public TriggerSave<bool> UseGPU { get; set; } = true;
        }

        public class DisplayConfig
        {
            public TriggerSave<int> Width { get; set; } = 1920;
            public TriggerSave<int> Height { get; set; } = 1080;
            public TriggerSave<string> Theme { get; set; } = "Dark";
            public TriggerSave<float?> DPI { get; set; } = null; // stress-test nullables
        }

        public class NetworkConfig
        {
            public TriggerSave<string> Host { get; set; } = "localhost";
            public TriggerSave<int> Port { get; set; } = 8080;

            // Previously optional; now mandatory
            public TriggerSave<bool> UseTLS { get; set; } = true;
        }
    }


}