namespace MarcoZechner.ConfigAPI.Shared.Domain
{
    public enum FileLocation
    {
        /// <summary>
        /// Saved in 'SEAppData/Saves/[steamID]/[WorldName]/Storage/ModFolder'
        /// </summary>
        World,
        /// <summary>
        /// Saved in 'SEAppData/Storage'
        /// </summary>
        Global,
        /// <summary>
        /// Saved in 'SEAppData/Storage/ModFolder'
        /// </summary>
        Local
    }
}