namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public interface IMainApi
    {
        // Consumer mods call this once after they obtained the main API.
        // ConfigAPIMod stores these callbacks and uses them for routing.
        void AddCallbackApi(ulong modId, string modName, object callbackApi);

        // Optional: for debugging
        bool IsCallbackRegistered(ulong modId);
    }
}