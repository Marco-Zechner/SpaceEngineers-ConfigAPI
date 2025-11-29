using System;
using mz.Config;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class MigrationConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.4.0";

        // New preferred field
        public TriggerSave<double> RefreshIntervalSeconds { get; set; } = 1.0;

        // Deprecated (should not break loading)
        [Obsolete("Use RefreshIntervalSeconds instead")]
        public TriggerSave<int> RefreshIntervalMs_SHOULD_NO_BE_IN_FILE { get; set; } = 1000;

        // Field renamed compared to older versions
        public TriggerSave<string> DisplayName { get; set; } = "Unnamed";

        // Previously was "UserName" in old configs
        public string LegacyUserName;
    }


}