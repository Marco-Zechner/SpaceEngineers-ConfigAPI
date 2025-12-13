using mz.Config.Domain;

namespace mz.Config.Core.Storage
{
    internal class ConfigSlot
    {
        public string TypeName;
        public string CurrentFileName;
        public ConfigBase Instance;
    }
}