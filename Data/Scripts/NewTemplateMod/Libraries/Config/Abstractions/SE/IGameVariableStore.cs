namespace mz.Config.Abstractions.SE
{
    public interface IGameVariableStore
    {
        void SetString(string key, string value);
        bool TryGetString(string key, out string value);
    }
}