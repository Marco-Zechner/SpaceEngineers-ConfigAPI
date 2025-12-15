namespace MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain
{
    /// <summary>
    /// Update info returned to user mods.
    /// Keep it small; no config instance here (Auth is retrieved separately).
    /// </summary>
    public sealed class CfgUpdate
    {
        public WorldOpKind WorldOpKind;       // what happened
        public string Error;           // null if ok
        public long TriggeredBy;       // playerId that triggered the applied op (if known)
        public ulong ServerIteration;  // authoritative iteration after the change
        public string CurrentFile;     // server's current file after the change (if provided)
    }
}