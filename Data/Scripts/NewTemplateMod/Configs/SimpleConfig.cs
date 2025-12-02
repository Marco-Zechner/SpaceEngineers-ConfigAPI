using mz.Config.Domain;
using mz.SemanticVersioning;

namespace mz.NewTemplateMod
{
    public class SimpleConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "0.1.0";

        public int SomeValue = 42;
        public string SomeText { get; set; } = "Default text";
    }

}