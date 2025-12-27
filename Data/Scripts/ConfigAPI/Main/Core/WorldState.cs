using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Main.Domain;
using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Main.Core
{
    public sealed class WorldState
    {
        public readonly string TypeKey;
        public readonly HooksDefinition Definition;

        public string CurrentFile;
        public ulong ServerIteration;

        public string AuthXml;
        public object AuthObj;

        public string DraftXml;
        public object DraftObj;

        public readonly Queue<CfgUpdate> Updates = new Queue<CfgUpdate>();

        public WorldState(string typeKey, HooksDefinition definition)
        {
            TypeKey = typeKey;
            Definition = definition;
        }
    }
}