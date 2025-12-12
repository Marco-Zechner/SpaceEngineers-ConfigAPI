using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Shared.Abstractions
{
    public interface IConfigClientBackend
    {
        bool TryLoad(ConfigBase instance, string fileName);
        void Save(ConfigBase instance, string fileName);
        string GetCurrentFileName(ConfigBase instance);
    }
}