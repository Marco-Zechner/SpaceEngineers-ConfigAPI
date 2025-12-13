using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Shared.Abstractions
{
    public interface IConfigApi
    {
        //TODO add version checking
        
        // Provider must:
        // - ensure definition exists
        // - ensure slot exists for (typeName, location)
        // - load/create+save on first use
        // - return the current in-memory instance for that (typeName, location)
        ConfigBase Get(string typeName, FileLocation location);

        bool TryLoad(string typeName, FileLocation location, string presetName);
        void Save(string typeName, FileLocation location, string presetName);

        string GetCurrentFileName(string typeName, FileLocation location);
    }
}