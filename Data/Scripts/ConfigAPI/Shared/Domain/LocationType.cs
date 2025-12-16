namespace MarcoZechner.ConfigAPI.Shared.Domain
{
    public enum LocationType
    {
        /// <summary>
        /// Saved in 'SEAppData/Storage/ModFolder'
        /// </summary>
        Local = 0,

        /// <summary>
        /// Saved in 'SEAppData/Storage'
        /// </summary>
        Global = 1,
        /// <summary>
        /// Saved in 'SEAppData/Saves/[steamID]/[WorldName]/Storage/ModFolder'
        /// </summary>
        // World = 2 // world is handled via a separate system.
        // IF you add it back in, make sure the guard against it in ClientConfigService.cs
    }
}