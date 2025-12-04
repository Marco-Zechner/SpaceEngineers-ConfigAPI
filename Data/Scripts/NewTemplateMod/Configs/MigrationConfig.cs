using mz.Config.Domain;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    //Create config with file, then change it and see if the migration works
    public class MigrationConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.4.0";

        // change variable name
        public double RefreshIntervalSeconds { get; set; } = 4.0;

        // change default Value
        public string DisplayName { get; set; } = "Hello2";

    }
}