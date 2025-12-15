namespace MarcoZechner.ConfigAPI.Shared.Domain
{
    public enum WorldOpKind
    {
        Unknown = 0,
        WorldUpdate = 1,
        LoadAndSwitch = 2,
        Save = 3,
        SaveAndSwitch = 4,
        Export = 5,
        Error = 6
    }
}