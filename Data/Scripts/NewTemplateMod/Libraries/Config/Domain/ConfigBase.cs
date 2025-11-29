namespace mz.Config.Domain
{
    public abstract class ConfigBase
    {
        public abstract string ConfigVersion { get; }

        public virtual string ConfigNameOverride
        {
            get { return GetType().Name; }
        }
    }
}