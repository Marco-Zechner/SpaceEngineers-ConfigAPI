using mz.Config;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class UselessConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";
        public override string ConfigNameOverride => "this is intentionally :)";
    }
}