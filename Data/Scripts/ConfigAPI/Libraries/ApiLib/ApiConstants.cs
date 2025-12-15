namespace MarcoZechner.ApiLib
{
    public class ApiConstants
    {
        public const string API_LIB_VERSION = "1.0.0";
        public const int PROTOCOL = 1;
        
        public const string HEADER_API_PROVIDER_MOD_ID_KEY = "ApiProviderModId";
        public const string HEADER_PROTOCOL_KEY = "Protocol";
        public const string HEADER_INTENT_KEY = "Intent";
        public const string HEADER_API_VERSION_KEY = "ApiVersion";

        public const string HEADER_FROM_MOD_ID_KEY = "FromModId";
        public const string HEADER_FROM_MOD_NAME_KEY = "FromModName";

        public const string HEADER_TARGET_MOD_ID_KEY = "TargetModId";
        public const string HEADER_TARGET_MOD_NAME_KEY = "TargetModName";

        public const string HEADER_LAYOUT_KEY = "Layout";
        public const string HEADER_TYPES_KEY = "Types";

        public const string INTENT_REQUEST = "API_Request";
        public const string INTENT_ANNOUNCE = "API_Announce";
        

        // setup API convention keys
        public const string SETUP_KEY_CONNECT = "Connect";
        public const string SETUP_KEY_DISCONNECT = "Disconnect";
    }
}