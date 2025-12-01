namespace mz.Config.Abstractions.SE
{
    /// <summary>
    /// Abstracts storage of small string values in the game session
    /// (e.g. MyAPIGateway.Utilities.SetVariable / GetVariable).
    /// Used by higher-level code for remembering things across loads.
    /// </summary>
    public interface IGameVariableStore
    {
        void SetString(string key, string value);
        bool TryGetString(string key, out string value);
    }
}