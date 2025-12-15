namespace MarcoZechner.ConfigAPI.Shared.Api
{
    public delegate bool WorldTryDequeueUpdateDelegate(
        string typeKey,
        out int worldOpKindEnum,
        out string error,
        out long triggeredBy,
        out ulong serverIteration,
        out string currentFile
    );

    public delegate bool WorldTryGetMetaDelegate(
        string typeKey,
        out ulong serverIteration,
        out string currentFile,
        out bool requestInFlight
    );
}