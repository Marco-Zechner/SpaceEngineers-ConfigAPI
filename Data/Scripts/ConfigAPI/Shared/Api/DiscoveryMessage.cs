namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public sealed class DiscoveryMessage
    {
        public const int PROTOCOL_VERSION = ApiConstant.PROTOCOL_VERSION;

        public int ProtocolVersion;
        public DiscoveryKind Kind;

        // Useful for debugging only.
        public ulong FromModId;
        public string FromModName;

        // Only filled for ANNOUNCE
        public object Api; // will be IMainApi
    }

    public enum DiscoveryKind
    {
        PING = 1,
        ANNOUNCE = 2
    }
}