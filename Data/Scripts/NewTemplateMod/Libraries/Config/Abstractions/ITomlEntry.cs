namespace mz.Config.Abstractions
{
    public interface ITomlEntry
    {
        string Value { get; set; }
        string DefaultValue { get; set; }
    }
}