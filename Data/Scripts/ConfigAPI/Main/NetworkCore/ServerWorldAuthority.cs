using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Main.NetworkCore
{
    public sealed class ServerWorldAuthority
    {
        private readonly Dictionary<string, WorldRecord> _records
            = new Dictionary<string, WorldRecord>(StringComparer.Ordinal);

        private static string Key(ulong consumerModId, string typeKey)
            => consumerModId + "|" + typeKey;

        private static string VarId(ulong consumerModId, string typeKey)
            => "ConfigAPI.World.AuthXml|" + consumerModId + "|" + typeKey;

        private static string VarIdIter(ulong consumerModId, string typeKey)
            => "ConfigAPI.World.Iter|" + consumerModId + "|" + typeKey;

        private static string VarIdFile(ulong consumerModId, string typeKey)
            => "ConfigAPI.World.File|" + consumerModId + "|" + typeKey;

        public void TryGetAuthSnapshot(
            ulong consumerModId,
            string typeKey,
            string fileHint,
            out string authXml,
            out ulong iteration,
            out string currentFile,
            out string error)
        {
            error = null;

            var k = Key(consumerModId, typeKey);

            WorldRecord rec;
            if (!_records.TryGetValue(k, out rec))
            {
                string storedXml;
                ulong storedIter;
                string storedFile;

                if (TryRead(consumerModId, typeKey, out storedXml, out storedIter, out storedFile))
                {
                    rec = new WorldRecord
                    {
                        ConsumerModId = consumerModId,
                        TypeKey = typeKey,
                        AuthXml = storedXml,
                        Iteration = storedIter,
                        CurrentFile = string.IsNullOrEmpty(storedFile) ? (fileHint ?? (typeKey + ".toml")) : storedFile
                    };
                }
                else
                {
                    rec = new WorldRecord
                    {
                        ConsumerModId = consumerModId,
                        TypeKey = typeKey,
                        AuthXml = null,
                        Iteration = 0UL,
                        CurrentFile = fileHint ?? (typeKey + ".toml")
                    };
                }

                _records[k] = rec;
            }

            authXml = rec.AuthXml;
            iteration = rec.Iteration;
            currentFile = rec.CurrentFile;

            if (string.IsNullOrEmpty(authXml))
                error = "No authoritative snapshot stored yet (server has no saved world config for this type).";
        }

        public void ApplyOp(
            ulong requesterSteamId,
            ulong consumerModId,
            string typeKey,
            WorldOpKind op,
            ulong baseIteration,
            string file,
            bool overwrite,
            string draftXml,
            out string authXml,
            out ulong iteration,
            out string currentFile,
            out string error)
        {
            error = null;

            var k = Key(consumerModId, typeKey);

            WorldRecord rec;
            if (!_records.TryGetValue(k, out rec))
            {
                string storedXml;
                ulong storedIter;
                string storedFile;

                if (TryRead(consumerModId, typeKey, out storedXml, out storedIter, out storedFile))
                {
                    rec = new WorldRecord
                    {
                        ConsumerModId = consumerModId,
                        TypeKey = typeKey,
                        AuthXml = storedXml,
                        Iteration = storedIter,
                        CurrentFile = storedFile
                    };
                }
                else
                {
                    rec = new WorldRecord
                    {
                        ConsumerModId = consumerModId,
                        TypeKey = typeKey,
                        AuthXml = null,
                        Iteration = 0UL,
                        CurrentFile = typeKey + ".toml"
                    };
                }

                _records[k] = rec;
            }

            if (baseIteration != rec.Iteration && op != WorldOpKind.Export)
            {
                authXml = rec.AuthXml;
                iteration = rec.Iteration;
                currentFile = rec.CurrentFile;
                error = "Rejected: baseIteration mismatch (config changed on server).";
                return;
            }

            switch (op)
            {
                case WorldOpKind.Save:
                {
                    if (string.IsNullOrEmpty(draftXml))
                    {
                        error = "Save rejected: missing draftXml.";
                        break;
                    }

                    rec.AuthXml = draftXml;
                    rec.Iteration = rec.Iteration + 1UL;
                    Persist(rec);
                    break;
                }
 
                case WorldOpKind.SaveAndSwitch:
                {
                    if (string.IsNullOrEmpty(draftXml))
                    {
                        error = "SaveAndSwitch rejected: missing draftXml.";
                        break;
                    }

                    if (string.IsNullOrEmpty(file))
                    {
                        error = "SaveAndSwitch rejected: missing file.";
                        break;
                    }

                    rec.AuthXml = draftXml;
                    rec.CurrentFile = file;
                    rec.Iteration = rec.Iteration + 1UL;
                    Persist(rec);
                    break;
                }

                case WorldOpKind.LoadAndSwitch:
                {
                    if (string.IsNullOrEmpty(file))
                    {
                        error = "LoadAndSwitch rejected: missing file.";
                        break;
                    }

                    rec.CurrentFile = file;
                    rec.Iteration = rec.Iteration + 1UL;
                    Persist(rec);
                    break;
                }

                case WorldOpKind.Export:
                {
                    // No-op in SetVariable-only version
                    break;
                }

                case WorldOpKind.Reload:
                {
                    // Treat as "snapshot request" (no mutation)
                    break;
                }

                default:
                    error = "Unknown/unsupported op: " + op;
                    break;
            }

            authXml = rec.AuthXml;
            iteration = rec.Iteration;
            currentFile = rec.CurrentFile;
        }

        private static void Persist(WorldRecord rec)
        {
            MyAPIGateway.Utilities.SetVariable(VarId(rec.ConsumerModId, rec.TypeKey), rec.AuthXml ?? string.Empty);
            MyAPIGateway.Utilities.SetVariable(VarIdIter(rec.ConsumerModId, rec.TypeKey), rec.Iteration);
            MyAPIGateway.Utilities.SetVariable(VarIdFile(rec.ConsumerModId, rec.TypeKey), rec.CurrentFile ?? string.Empty);
        }

        private static bool TryRead(ulong consumerModId, string typeKey, out string xml, out ulong iter, out string file)
        {
            xml = null;
            iter = 0UL;
            file = null;

            string storedXml;
            if (!MyAPIGateway.Utilities.GetVariable(VarId(consumerModId, typeKey), out storedXml))
                return false;

            ulong storedIter;
            MyAPIGateway.Utilities.GetVariable(VarIdIter(consumerModId, typeKey), out storedIter);

            string storedFile;
            MyAPIGateway.Utilities.GetVariable(VarIdFile(consumerModId, typeKey), out storedFile);

            xml = storedXml;
            iter = storedIter;
            file = storedFile;
            return true;
        }

        private sealed class WorldRecord
        {
            public ulong ConsumerModId;
            public string TypeKey;
            public string AuthXml;
            public ulong Iteration;
            public string CurrentFile;
        }
    }
}
