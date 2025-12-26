using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.Main.Api;
using MarcoZechner.ConfigAPI.Main.Domain;
using MarcoZechner.ConfigAPI.Main.NetworkCore;
using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Main.Core
{
    /// <summary>
    /// World config service (runs on every machine), bound to ONE consumer mod (hooks instance).
    ///
    /// Local model per (consumerModId,typeKey):
    /// - AuthXml/AuthObj: last authoritative snapshot received from server.
    /// - DraftXml/DraftObj: editable copy (never auto-updated).
    ///
    /// Networking:
    /// - Requests are sent via IWorldConfigNetwork (NetworkCore stamps/uses ConsumerModId).
    /// - Server replies/broadcasts come back as WorldOpKind.WorldUpdate (or Error).
    ///
    /// Payload:
    /// - XmlData is internal canonical XML for now.
    /// </summary>
    public sealed class ServerConfigService : IWorldConfigClientSink
    {
        private const int MAX_UPDATES_PER_TYPE = 64;

        private readonly ulong _consumerModId;
        private readonly ConfigUserHooks _hooks;
        private readonly IConfigLayoutMigrator _migrator;
        private readonly IWorldConfigNetwork _net;

        // Key by consumerModId + "|" + typeKey to avoid collisions across consumer mods.
        private readonly Dictionary<string, WorldState> _states
            = new Dictionary<string, WorldState>(StringComparer.Ordinal);

        private static string Key(ulong consumerModId, string typeKey)
            => consumerModId.ToString() + "|" + typeKey;

        public ServerConfigService(
            ulong consumerModId,
            ConfigUserHooks hooks,
            IConfigLayoutMigrator migrator,
            IWorldConfigNetwork net)
        {
            if (hooks == null) throw new ArgumentNullException(nameof(hooks));
            if (migrator == null) throw new ArgumentNullException(nameof(migrator));
            if (net == null) throw new ArgumentNullException(nameof(net));

            _consumerModId = consumerModId;
            _hooks = hooks;
            _migrator = migrator;
            _net = net;
        }

        // ===============================================================
        // API surface called by ConfigServiceImpl
        // ===============================================================

        public object ServerConfigInit(string typeKey, string defaultFile)
        {
            if (string.IsNullOrEmpty(typeKey))
                throw new ArgumentNullException(nameof(typeKey));

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (_states.TryGetValue(k, out st))
                return st.AuthObj;

            var def = new HooksDefinition(_hooks, typeKey);

            // Local fallback defaults until server snapshot arrives
            var defObj = _hooks.NewDefault(typeKey);
            if (defObj == null)
                throw new Exception("ServerConfigInit: NewDefault returned null for " + typeKey);

            var defXml = _hooks.SerializeToInternalXml(typeKey, defObj) ?? string.Empty;

            st = new WorldState(typeKey, def)
            {
                // if caller passed null, derive something deterministic
                CurrentFile = !string.IsNullOrEmpty(defaultFile) ? defaultFile : (typeKey + ".toml"),

                AuthObj = defObj,
                AuthXml = defXml,

                DraftObj = DeserializeOrFallback(typeKey, defXml, defObj),
                DraftXml = defXml,

                ServerIteration = 0UL,
                InitRequested = false
            };

            _states[k] = st;

            // Ask server for snapshot once
            if (!st.InitRequested)
            {
                st.InitRequested = true;

                // Reload is "please send snapshot"; file goes in File
                _net.SendRequest(new WorldNetRequest
                {
                    ConsumerModId = _consumerModId,
                    TypeKey = typeKey,
                    Op = WorldOpKind.Reload,
                    BaseIteration = st.ServerIteration,
                    FileName = st.CurrentFile,
                    Overwrite = false,
                    XmlData = null
                });
            }

            return st.AuthObj;
        }

        public CfgUpdate ServerConfigGetUpdate(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (!_states.TryGetValue(k, out st))
                return null;

            if (st.Updates.Count == 0)
                return null;

            return st.Updates.Dequeue();
        }

        public object ServerConfigGetAuth(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            return _states.TryGetValue(k, out st) ? st.AuthObj : null;
        }

        public object ServerConfigGetDraft(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            return _states.TryGetValue(k, out st) ? st.DraftObj : null;
        }

        public void ServerConfigResetDraft(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (!_states.TryGetValue(k, out st))
                return;

            // Draft <- Auth via XML roundtrip
            st.DraftXml = st.AuthXml;
            st.DraftObj = DeserializeOrFallback(typeKey, st.DraftXml, st.DraftObj);

            Enqueue(st, WorldOpKind.WorldUpdate, error: null, triggeredBy: 0);
        }

        public bool ServerConfigLoadAndSwitch(string typeKey, string file, ulong baseIteration)
        {
            if (string.IsNullOrEmpty(typeKey) || string.IsNullOrEmpty(file))
                return false;

            return _net.SendRequest(new WorldNetRequest
            {
                ConsumerModId = _consumerModId,
                TypeKey = typeKey,
                Op = WorldOpKind.LoadAndSwitch,
                BaseIteration = baseIteration,
                FileName = file,
                Overwrite = false,
                XmlData = null
            });
        }

        public bool ServerConfigSave(string typeKey, ulong baseIteration)
        {
            if (string.IsNullOrEmpty(typeKey))
                return false;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (!_states.TryGetValue(k, out st))
                return false;

            st.DraftXml = SafeSerializeDraft(typeKey, st.DraftObj, st.DraftXml);

            return _net.SendRequest(new WorldNetRequest
            {
                ConsumerModId = _consumerModId,
                TypeKey = typeKey,
                Op = WorldOpKind.Save,
                BaseIteration = baseIteration,
                FileName = null,
                Overwrite = false,
                XmlData = st.DraftXml
            });
        }

        public bool ServerConfigSaveAndSwitch(string typeKey, string file, ulong baseIteration)
        {
            if (string.IsNullOrEmpty(typeKey) || string.IsNullOrEmpty(file))
                return false;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (!_states.TryGetValue(k, out st))
                return false;

            st.DraftXml = SafeSerializeDraft(typeKey, st.DraftObj, st.DraftXml);

            return _net.SendRequest(new WorldNetRequest
            {
                ConsumerModId = _consumerModId,
                TypeKey = typeKey,
                Op = WorldOpKind.SaveAndSwitch,
                BaseIteration = baseIteration,
                FileName = file,
                Overwrite = false,
                XmlData = st.DraftXml
            });
        }

        public bool ServerConfigExport(string typeKey, string file, bool overwrite)
        {
            if (string.IsNullOrEmpty(typeKey) || string.IsNullOrEmpty(file))
                return false;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (!_states.TryGetValue(k, out st))
                return false;

            st.DraftXml = SafeSerializeDraft(typeKey, st.DraftObj, st.DraftXml);

            return _net.SendRequest(new WorldNetRequest
            {
                ConsumerModId = _consumerModId,
                TypeKey = typeKey,
                Op = WorldOpKind.Export,
                BaseIteration = st.ServerIteration,
                FileName = file,
                Overwrite = overwrite,
                XmlData = st.DraftXml
            });
        }

        // ===============================================================
        // Incoming from NetworkCore (applied locally)
        // ===============================================================

        internal void OnNetworkPacket(
            string typeKey,
            WorldOpKind op,
            ulong serverIteration,
            string currentFile,
            string xmlData,
            string error,
            ulong triggeredBy)
        {
            if (string.IsNullOrEmpty(typeKey))
                return;

            var k = Key(_consumerModId, typeKey);

            WorldState st;
            if (!_states.TryGetValue(k, out st))
            {
                // Race-safe: allow snapshot before Init()
                var def = new HooksDefinition(_hooks, typeKey);

                var defObj = _hooks.NewDefault(typeKey);
                if (defObj == null)
                    return;

                var defXml = _hooks.SerializeToInternalXml(typeKey, defObj) ?? string.Empty;

                st = new WorldState(typeKey, def)
                {
                    CurrentFile = !string.IsNullOrEmpty(currentFile) ? currentFile : (typeKey + ".toml"),
                    AuthObj = defObj,
                    AuthXml = defXml,
                    DraftObj = DeserializeOrFallback(typeKey, defXml, defObj),
                    DraftXml = defXml,
                    ServerIteration = 0UL,
                    InitRequested = true
                };

                _states[k] = st;
            }

            // Update metadata first
            st.ServerIteration = serverIteration;
            if (!string.IsNullOrEmpty(currentFile))
                st.CurrentFile = currentFile;

            if (!string.IsNullOrEmpty(error))
            {
                Enqueue(st, WorldOpKind.Error, error, triggeredBy);
                return;
            }

            if (op == WorldOpKind.WorldUpdate)
            {
                if (string.IsNullOrEmpty(xmlData))
                {
                    Enqueue(st, WorldOpKind.Error, "WorldUpdate missing XmlData", triggeredBy);
                    return;
                }

                var normalized = NormalizeAgainstDefaults(st.Definition, xmlData);

                st.AuthXml = normalized;
                st.AuthObj = DeserializeOrFallback(typeKey, st.AuthXml, st.AuthObj);

                // Draft is NOT auto-updated by design.
                Enqueue(st, WorldOpKind.WorldUpdate, null, triggeredBy);
                return;
            }

            // Op-results (Save/Load/etc) can be surfaced as info-only updates.
            Enqueue(st, op, null, triggeredBy);
        }

        // ===============================================================
        // Internals
        // ===============================================================

        private string SafeSerializeDraft(string typeKey, object draftObj, string fallbackXml)
        {
            try
            {
                var xml = _hooks.SerializeToInternalXml(typeKey, draftObj);
                return xml ?? fallbackXml ?? string.Empty;
            }
            catch
            {
                return fallbackXml ?? string.Empty;
            }
        }

        private object DeserializeOrFallback(string typeKey, string xml, object fallback)
        {
            try
            {
                var obj = _hooks.DeserializeFromInternalXml(typeKey, xml);
                return obj ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private string NormalizeAgainstDefaults(HooksDefinition def, string xml)
        {
            var xmlDefaults = def.GetCurrentDefaultsInternalXml();
            var res = _migrator.Normalize(def, xml, xmlDefaults, xmlDefaults);
            return res.NormalizedXml ?? xml;
        }

        private void Enqueue(WorldState st, WorldOpKind kind, string error, ulong triggeredBy)
        {
            if (st.Updates.Count >= MAX_UPDATES_PER_TYPE)
                st.Updates.Dequeue();

            st.Updates.Enqueue(new CfgUpdate
            {
                WorldOpKind = kind,
                Error = error,
                TriggeredBy = triggeredBy,
                ServerIteration = st.ServerIteration,
                CurrentFile = st.CurrentFile
            });
        }

        private sealed class WorldState
        {
            public readonly string TypeKey;
            public readonly HooksDefinition Definition;

            public bool InitRequested;

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

        public void OnServerWorldUpdate(WorldConfigPacket packet)
        {
            if (packet == null)
                return;

            if (packet.ConsumerModId != _consumerModId)
                return;

            OnNetworkPacket(
                packet.TypeKey,
                packet.Op,
                packet.ServerIteration,
                packet.FileName,
                packet.XmlData,
                packet.Error,
                packet.TriggeredBy
            );
        }
    }
}
