namespace MarcoZechner.ApiLib
{
    public abstract class ApiBootstrapConfig
    {
        public abstract string ApiLibVersion { get; }
        public abstract long DiscoveryChannel { get; }
        public abstract string ApiProviderModId { get; }
        public abstract string ApiVersion { get; }
    }
}