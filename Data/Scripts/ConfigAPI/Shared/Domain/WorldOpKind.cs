namespace MarcoZechner.ConfigAPI.Shared.Domain
{
    public enum WorldOpKind
    {
        Unknown = 0,
        Error = 1,
        LoadAndSwitch = 2,
        Reload = 3,
        SaveAndSwitch = 4,
        Save = 5,
        Export = 6,
        WorldUpdate = 7,
    }
}