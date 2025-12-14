namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public static class ApiConstant
    {
        public const string API_VERSION = "0.1.0";
        public const string PROVIDER_MOD_NAME = "ConfigAPI";
        public const ulong PROVIDER_STEAM_ID = 1234567890123456;
        public const long DISCOVERY_CH = 23456;
        
        // Header protocol
        public const string MAGIC = "MZ_CFGAPI";
        public const int PROTOCOL = 1;
        
        // Header keys
        public const string H_MAGIC = "Magic";
        public const string H_PROTOCOL = "Protocol";
        public const string H_SCHEMA = "Schema";     // e.g. "MainApiRequest/v1"
        public const string H_INTENT = "Intent";     // request/announce
        public const string H_API_VERSION = "ApiVersion";

        public const string H_FROM_MOD_ID = "FromModId";     // ulong
        public const string H_FROM_MOD_NAME = "FromModName"; // string

        public const string H_TARGET_MOD_ID = "TargetModId";     // ulong or 0 for "any"
        public const string H_TARGET_MOD_NAME = "TargetModName"; // string

        // Layout hints (debuggable convention)
        public const string H_LAYOUT = "Layout"; // string
        public const string H_TYPES = "Types";   // string

        // Intents
        public const string INTENT_REQUEST = "API_Request";
        public const string INTENT_ANNOUNCE = "API_Announce";

        // Schema ids
        public const string SCHEMA_MAIN_REQUEST = "MainApiRequest/v1";
        public const string SCHEMA_MAIN_ANNOUNCE = "MainApiAnnounce/v1";
    }
}