namespace MarcoZechner.ConfigAPI.Shared.Domain
{
    public enum LocationType
    {
        Local = 0,
        Global = 1,
        // World = 2 // world is handled via a separate system.
        // IF you add it back in, make sure the guard against it in ClientConfigService.cs
    }
}