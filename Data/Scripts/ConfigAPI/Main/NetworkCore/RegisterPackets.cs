using MarcoZechner.ConfigAPI.Main.NetworkCore;
using ProtoBuf;

namespace Digi.NetworkLib
{
    [ProtoInclude(1, typeof(WorldConfigPacket))]
    public abstract partial class PacketBase
    {
    }
}