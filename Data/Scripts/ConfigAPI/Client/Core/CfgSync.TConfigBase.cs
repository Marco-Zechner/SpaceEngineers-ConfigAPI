using MarcoZechner.ConfigAPI.Client.Api;
using MarcoZechner.ConfigAPI.Shared.Domain;
using MarcoZechner.ConfigAPI.Shared.Api;

namespace MarcoZechner.ConfigAPI.Client.Core
{
    /// <summary>
    /// User-facing world config handle.
    /// - Auth: authoritative snapshot (updated by ConfigAPIMod)
    /// - Draft: editable snapshot (never auto-updated)
    /// 
    /// IMPORTANT:
    /// Call GetUpdate() once per update loop to stay in sync.
    /// Handling the returned CfgUpdate is optional.
    /// </summary>
    public sealed class CfgSync<T> where T : ConfigBase, new()
    {
        private readonly string _typeKey;
        private readonly string _defaultFile;

        private bool _initQueued;
        private ulong _serverIteration;
        private string _currentFile;

        private T _auth;
        private T _draft;

        internal CfgSync(string defaultFile)
        {
            _typeKey = typeof(T).FullName;
            _defaultFile = string.IsNullOrEmpty(defaultFile) ? (_typeKey + ".toml") : defaultFile;

            // local fallback defaults until API becomes available
            _auth = new T();
            _auth.ApplyDefaults();

            _draft = new T();
            _draft.ApplyDefaults();

            _serverIteration = 0;
            _currentFile = _defaultFile;
        }

        /// <summary>
        /// Authoritative snapshot. Read-only by convention.
        /// </summary>
        public T Auth => _auth;

        /// <summary>
        /// Editable working copy.
        /// </summary>
        public T Draft => _draft;

        /// <summary>
        /// Last known server iteration (used as baseIteration for requests).
        /// </summary>
        public ulong ServerIteration => _serverIteration;

        /// <summary>
        /// Last known current file on the server (from updates).
        /// </summary>
        public string CurrentFile => _currentFile;

        /// <summary>
        /// Pump once per update loop.
        /// Applies the latest pending server update internally (inside ConfigAPIMod).
        /// This method then refreshes local Auth/Draft references.
        /// Returns null if no update happened.
        /// </summary>
        public CfgUpdate GetUpdate()
        {
            var service = ServiceLoader.Service;
            if (service == null)
            {
                // API not ready yet; keep returning defaults.
                // We intentionally do not throw here.
                return null; //TODO: maybe return a "no-op" or "no-api" update instead?
            }

            // Ensure init once API is ready.
            EnsureInit(service);

            // Pump the provider once. Provider applies update to its internal Auth.
            var update = service.ServerConfigGetUpdate(_typeKey);

            // Regardless of whether update is null, we can refresh Auth/Draft cheaply
            // to make sure local references always track provider state.
            RefreshAuthDraft(service, update != null);

            if (update == null) return null; //TODO: maybe return a "no-op" update instead?
            
            // Track metadata so requests can use correct baseIteration.
            _serverIteration = update.ServerIteration;
            if (!string.IsNullOrEmpty(update.CurrentFile))
                _currentFile = update.CurrentFile;

            return update;
        }

        /// <summary>
        /// Draft &lt;- deep copy(Auth) (done by provider via serialize/deserialize callbacks).
        /// </summary>
        public void ResetDraft()
        {
            var service = ServiceLoader.Service;
            if (service == null)
                return;

            EnsureInit(service);

            service.ServerConfigResetDraft(_typeKey);
            // Pull new draft reference
            RefreshAuthDraft(service, refreshDraft: true);
        }

        // -------------------------
        // World operations
        // -------------------------

        public bool LoadAndSwitch(string file)
        {
            var service = ServiceLoader.Service;
            if (service == null)
                return false;

            EnsureInit(service);

            if (string.IsNullOrEmpty(file))
                return false;

            return service.ServerConfigLoadAndSwitch(_typeKey, file, _serverIteration);
        }

        public bool Save()
        {
            var service = ServiceLoader.Service;
            if (service == null)
                return false;

            EnsureInit(service);

            return service.ServerConfigSave(_typeKey, _serverIteration);
        }

        public bool SaveAndSwitch(string file)
        {
            var service = ServiceLoader.Service;
            if (service == null)
                return false;

            EnsureInit(service);

            if (string.IsNullOrEmpty(file))
                return false;

            return service.ServerConfigSaveAndSwitch(_typeKey, file, _serverIteration);
        }

        public bool Export(string file, bool overwrite = false)
        {
            var service = ServiceLoader.Service;
            if (service == null)
                return false;

            EnsureInit(service);

            if (string.IsNullOrEmpty(file))
                return false;

            return service.ServerConfigExport(_typeKey, file, overwrite);
        }

        // ===============================================================
        // Internals
        // ===============================================================

        private void EnsureInit(IConfigService service)
        {
            if (_initQueued)
                return;

            // Provider contract: returns current internal Auth (or default created there)
            // and triggers server open/request to send authoritative snapshot.
            var obj = service.ServerConfigInit(_typeKey, _defaultFile);

            _initQueued = true;

            // If provider returned something useful immediately, use it.
            var cast = obj as T;
            if (cast != null)
                _auth = cast;

            // Also pull draft immediately if available.
            RefreshAuthDraft(service, refreshDraft: true);
        }

        private void RefreshAuthDraft(IConfigService service, bool refreshDraft)
        {
            // Auth
            var authObj = service.ServerConfigGetAuth(_typeKey);
            var authCast = authObj as T;
            if (authCast != null)
                _auth = authCast;

            if (!refreshDraft) return;
            
            // Draft (optional)
            var draftObj = service.ServerConfigGetDraft(_typeKey);
            var draftCast = draftObj as T;
            if (draftCast != null)
                _draft = draftCast;
        }
    }
}