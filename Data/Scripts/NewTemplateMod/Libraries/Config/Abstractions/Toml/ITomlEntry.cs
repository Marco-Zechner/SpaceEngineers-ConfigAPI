namespace mz.Config.Abstractions.Toml
{
    public interface ITomlEntry
    {
        string Value { get; set; }
        string DefaultValue { get; set; }
    }
}