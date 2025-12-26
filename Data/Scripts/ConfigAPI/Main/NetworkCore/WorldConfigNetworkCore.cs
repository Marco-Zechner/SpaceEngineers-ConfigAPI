using System;
using System.Collections.Generic;
using Digi.NetworkLib;
using Sandbox.ModAPI;
using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Main.NetworkCore
{
    public sealed class WorldConfigNetworkCore : IWorldConfigNetworkCore
    {
        public static WorldConfigNetworkCore Instance { get; private set; }

        private readonly Network _net;
        private readonly ServerWorldAuthority _authority;

        private readonly Dictionary<ulong, IWorldConfigClientSink> _sinks
            = new Dictionary<ulong, IWorldConfigClientSink>();

        public WorldConfigNetworkCore(Network net, ServerWorldAuthority authority)
        {
            if (net == null) throw new ArgumentNullException(nameof(net));
            if (authority == null) throw new ArgumentNullException(nameof(authority));

            _net = net;
            _authority = authority;

            Instance = this;
        }

        public void Unload()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;

            _sinks.Clear();
        }

        public void RegisterConsumer(ulong consumerModId, IWorldConfigClientSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            _sinks[consumerModId] = sink;
        }

        public void UnregisterConsumer(ulong consumerModId)
        {
            _sinks.Remove(consumerModId);
        }

        public IWorldConfigNetwork CreateConsumerFacade(ulong consumerModId)
        {
            return new ConsumerFacade(this, consumerModId);
        }

        // Called by WorldConfigPacket.Received(...)
        public void OnPacketReceived(WorldConfigPacket p, ref PacketInfo packetInfo, ulong senderSteamId)
        {
            if (p == null)
                return;

            if (MyAPIGateway.Multiplayer == null)
                return;

            if (MyAPIGateway.Multiplayer.IsServer)
                HandleOnServer(p, ref packetInfo, senderSteamId);
            else
                HandleOnClient(p);
        }

        private void HandleOnServer(WorldConfigPacket req, ref PacketInfo packetInfo, ulong senderSteamId)
        {
            string authXml, err, curFile;
            ulong iter;

            if (req.Op == WorldOpKind.Reload)
            {
                _authority.TryGetAuthSnapshot(
                    req.ConsumerModId,
                    req.TypeKey,
                    req.FileName,
                    out authXml,
                    out iter,
                    out curFile,
                    out err);
            }
            else
            {
                _authority.ApplyOp(
                    senderSteamId,
                    req.ConsumerModId,
                    req.TypeKey,
                    req.Op,
                    req.BaseIteration,
                    req.FileName,
                    req.Overwrite,
                    req.XmlData,
                    out authXml,
                    out iter,
                    out curFile,
                    out err);
            }

            // Turn request object into response/broadcast.
            req.Op = WorldOpKind.WorldUpdate;
            req.ServerIteration = iter;
            req.FileName = curFile;
            req.XmlData = authXml;
            req.Error = err;
            req.TriggeredBy = senderSteamId;

            // Broadcast authoritative snapshot to everyone (including sender)
            packetInfo.Relay = RelayMode.ToEveryone;
            packetInfo.Reserialize = true;
        }

        private void HandleOnClient(WorldConfigPacket msg)
        {
            IWorldConfigClientSink sink;
            if (!_sinks.TryGetValue(msg.ConsumerModId, out sink))
                return;

            // IMPORTANT: sink wants the packet object (your interface)
            sink.OnServerWorldUpdate(msg);
        }

        // ===============================================================
        // Per-consumer facade (stamps ConsumerModId)
        // ===============================================================

        private sealed class ConsumerFacade : IWorldConfigNetwork
        {
            private readonly WorldConfigNetworkCore _core;
            private readonly ulong _consumerModId;

            public ConsumerFacade(WorldConfigNetworkCore core, ulong consumerModId)
            {
                if (core == null) throw new ArgumentNullException(nameof(core));
                _core = core;
                _consumerModId = consumerModId;
            }

            public bool SendRequest(WorldNetRequest req)
            {
                var p = new WorldConfigPacket
                {
                    ConsumerModId = _consumerModId,
                    TypeKey = req.TypeKey,
                    Op = req.Op,
                    BaseIteration = req.BaseIteration,
                    Overwrite = req.Overwrite,
                    XmlData = req.XmlData,
                    FileName = req.FileName
                };

                _core._net.SendToServer(p);
                return true;
            }
        }
    }
}
