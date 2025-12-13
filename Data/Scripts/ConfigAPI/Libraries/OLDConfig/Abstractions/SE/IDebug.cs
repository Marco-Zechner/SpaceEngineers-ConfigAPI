namespace mz.Config.Abstractions
{
    public interface IDebug
    {
        void Log(string message, string source = "cfg");
    }
}