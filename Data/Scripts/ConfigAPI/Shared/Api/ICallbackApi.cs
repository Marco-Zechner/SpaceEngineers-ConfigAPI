namespace MarcoZechner.ConfigAPI.Shared.Api
{
    // This is the "per user mod" API that ConfigAPIMod calls back into.
    // Keep it as object in AddCallbackApi to avoid type loading issues,
    // but internally you should cast to this interface.
    public interface ICallbackApi
    {
        // Phase 0.2 only needs registration; you can keep this empty for now.
        // Later you add filesystem + serialize/deserialize delegates here.
    }
}