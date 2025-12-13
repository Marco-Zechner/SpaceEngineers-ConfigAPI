using System.Text;
using VRage.Game.ModAPI;

namespace MarcoZechner.ConfigAPI.Shared
{
    //TODO: check if this can be used or not.
    public static class ModMessage
    {
        // doesn't work, since the consumer mod would need to know the provider mods modID and modName. idk if that's dynamically possible...
        public static long GetId(string modId, string modName)
        {
            var workshopId = ulong.Parse(modId);
            var modMessageId = unchecked((long)workshopId);
            if (workshopId != 0) // Steam Workshop mod
                return modMessageId;
            
            // Local mod; generate a stable hash from the mod name

            // FNV-1a 64-bit parameters
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            var hash = offsetBasis;
            var data = Encoding.UTF8.GetBytes(modName);

            unchecked
            {
                foreach (var b in data)
                {
                    hash ^= b;
                    hash *= prime;
                }
            }

            // Interpret the 64 bits as a signed long; the sign does not matter
            return unchecked((long)hash);
        }
    }
}