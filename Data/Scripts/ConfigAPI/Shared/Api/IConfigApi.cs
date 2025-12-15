using MarcoZechner.ConfigAPI.Scripts.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Shared.Api
{
    /// <summary>
    /// Main API exposed by ConfigAPIMod. UserMods call this.
    /// Keep this interface stable; add methods carefully.
    /// </summary>
    public interface IConfigApi
    {
        // -------------------------
        // Diagnostics
        // -------------------------
        void Test();

        // -------------------------
        // Local/Global config: (no networking, just local file storage)
        // -------------------------
        
        /// <summary>
        /// Get (create/load/migrate) a Local/Global config instance.
        /// If filename is null/empty, use a default derived from typeKey (e.g. typeKey + ".toml").
        /// Returns an opaque object; UserMod casts to its config type.
        /// </summary>
        object GetConfig(string typeKey, int locationType, string filename);

        /// <summary>
        /// Force load config from disk and replace the in-memory instance.
        /// Returns null if load failed; otherwise returns the new instance.
        /// </summary>
        object LoadConfig(string typeKey, int locationType, string filename);

        /// <summary>
        /// Save the current in-memory instance to disk.
        /// If filename is null/empty, save to the instance's CurrentFile (or default).
        /// Returns false if instance doesn't exist yet or save failed.
        /// </summary>
        bool SaveConfig(string typeKey, int locationType, string filename);
        
        // -------------------------
        // World config: client-side sync surface
        //
        // Key: (modId, typeKey)
        // typeKey = typeof(T).FullName from UserMod
        // -------------------------

        /// <summary>
        /// Ensure a world config sync state exists on the client, using the given default file name.
        /// Returns false if API not initialized properly on provider side.
        /// </summary>
        bool WorldOpen(string typeKey, string defaultFile);

        /// <summary>
        /// Pump the world sync once (should be called once per update loop).
        /// Applies the latest pending server update to Auth internally.
        /// Returns:
        /// - null if no update occurred since last poll
        /// - a CfgUpdate object if something changed or an error happened
        /// </summary>
        CfgUpdate WorldGetUpdate(string typeKey);
        
        /// <summary>
        /// Get current authoritative snapshot (opaque object).
        /// UserMod casts this to its config type.
        /// </summary>
        object WorldGetAuth(string typeKey);

        /// <summary>
        /// Get current draft snapshot (opaque object).
        /// UserMod casts this to its config type and edits it locally.
        /// </summary>
        object WorldGetDraft(string typeKey);

        /// <summary>
        /// Replace Draft with a deep copy of Auth (provider uses callback serialize/deserialize).
        /// </summary>
        void WorldResetDraft(string typeKey);

        // -------------------------
        // World operations (requests)
        // Return false if a request is already in flight for this (typeKey).
        // BaseIteration should be whatever the UserMod last observed from WorldGetUpdate/Auth.
        // -------------------------

        bool WorldLoadAndSwitch(string typeKey, string file, ulong baseIteration);

        bool WorldSave(string typeKey, ulong baseIteration);

        bool WorldSaveAndSwitch(string typeKey, string file, ulong baseIteration);

        bool WorldExport(string typeKey, string file, ulong baseIteration);

        WorldMeta WorldGetMeta(string typeKey);
    }
}