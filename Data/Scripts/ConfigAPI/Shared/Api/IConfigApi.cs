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