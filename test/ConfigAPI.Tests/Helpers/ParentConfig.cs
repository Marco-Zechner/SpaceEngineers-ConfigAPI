using mz.Config.Domain;
using mz.SemanticVersioning;

namespace NewTemplateMod.Tests
{
    public class ParentConfig : ConfigBase
    {
        public override SemanticVersion ConfigVersion => "1.0.0";

        public ChildConfig Child { get; set; } = new ChildConfig();
    }
}