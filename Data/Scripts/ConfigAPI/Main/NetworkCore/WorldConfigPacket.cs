using Digi.NetworkLib;
using ProtoBuf;
using MarcoZechner.ConfigAPI.Shared.Domain;

namespace MarcoZechner.ConfigAPI.Main.NetworkCore
{
    [ProtoContract]
    public sealed class WorldConfigPacket : PacketBase
    {
        // Which consumer mod this packet is for (routing on the receiving machine)
        [ProtoMember(2)] public ulong ConsumerModId;

        [ProtoMember(3)] public string TypeKey;

        // Operation info
        [ProtoMember(4)] public WorldOpKind Op;
        [ProtoMember(5)] public ulong BaseIteration;

        // Export overwrite
        [ProtoMember(6)] public bool Overwrite;

        // Draft payload (client -> server) or snapshot (server -> client)
        [ProtoMember(7)] public string XmlData;

        // Metadata (server -> client)
        [ProtoMember(8)] public ulong ServerIteration;
        [ProtoMember(9)] public string FileName;

        // Error (server -> client)
        [ProtoMember(10)] public string Error;

        // Who triggered (server -> client)
        [ProtoMember(11)] public ulong TriggeredBy;

        public override void Received(ref PacketInfo packetInfo, ulong senderSteamId)
        {
            // Let the network core decide relay behavior and apply.
            var core = WorldConfigNetworkCore.Instance;

            core?.OnPacketReceived(this, ref packetInfo, senderSteamId);
        }
    }
}