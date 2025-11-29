using mz.Config.Domain;

namespace mz.Config.Core
{
    internal class ConfigSlot
    {
        public string TypeName;
        public string CurrentFileName;
        public ConfigBase Instance;
    }
}