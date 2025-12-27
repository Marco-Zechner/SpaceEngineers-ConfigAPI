using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Digi.NetworkLib
{
    public class Network : IDisposable
    {
        public readonly ushort ChannelId;

        /// <summary>
        /// Callback for errors in the receive packet event.<br />
        /// If null (default), it will write to SE log, DS console or client HUD (whichever is applicable).<br />
        /// NOTE: Another mod using the same channelId will cause exceptions that are not either of your faults, not recommended to crash nor to ignore the error, just let players find collisions and work it out with the other author(s).
        /// </summary>
        public Action<Exception> ExceptionHandler;

        /// <summary>
        /// Additional callback when exceptions occurs on <see cref="ReceivedPacket(ushort, byte[], ulong, bool)"/> specifically.<br />
        /// Note that the <see cref="ExceptionHandler"/> is also invoked before this.
        /// If null (default), it will write to SE log the sender name and steamid, as well as a byte dump of the packet.
        /// </summary>
        public Action<ulong, IMyPlayer, byte[]> ReceiveExceptionHandler;

        /// <summary>
        /// Callback for custom text errors.<br />
        /// If null (default), it will write to SE log, DS console or client HUD (whichever is applicable).
        /// </summary>
        public Action<string> ErrorHandler;

        /// <summary>
        /// To test serialization&deserialization along with messaging API while in singleplayer, set this to true.
        /// </summary>
        public bool SerializeTest = false;

        private readonly string _modName;
        private readonly List<IMyPlayer> _tempPlayers;

        private static bool _alreadyInstanced = false;

        /// <summary>
        /// Create only one instance of this in session component's LoadData() for example (not in fields) and also call <see cref="Dispose"/> in UnloadData().
        /// </summary>
        /// <param name="channelId">must be unique from all other mods that also use network packets.</param>
        /// <param name="modName">an identifier for errors/warnings.</param>
        /// <param name="registerListener">you can turn off message listening if you don't want this machine to receive them.</param>
        public Network(ushort channelId, string modName, bool registerListener = true)
        {
            ChannelId = channelId;
            _modName = modName;

            if(MyAPIGateway.Session == null)
            {
                CrashAfterLoad($"{_modName}: The {nameof(Network)} constructor was called too early, earliest valid spot is in LoadData().");
                return;
            }

            if(_alreadyInstanced)
            {
                CrashAfterLoad($"{_modName}: The {nameof(Network)} was instanced more than once, if you're doing this in gamelogic then don't, do it in session component.");
                return;
            }

            _alreadyInstanced = true;

            if(registerListener)
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ChannelId, ReceivedPacket);

            _tempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
        }

        /// <summary>
        /// This must be called by you on world unload.
        /// </summary>
        public void Dispose()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ChannelId, ReceivedPacket);

            _tempPlayers.Clear();
        }

        /// <summary>
        /// Send a packet to the server.<br />
        /// Works from clients and server.<br />
        /// <para><paramref name="serialized"/> = input pre-serialized data if you have it (optimization) or leave null otherwise.</para>
        /// </summary>
        public void SendToServer(PacketBase packet, byte[] serialized = null)
        {
            if(!SerializeTest && MyAPIGateway.Multiplayer.IsServer) // short-circuit local call to avoid unnecessary serialization
            {
                HandlePacket(packet, MyAPIGateway.Multiplayer.MyId, serialized);
                return;
            }

            if(serialized == null)
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(ChannelId, serialized);
        }

        /// <summary>
        /// Send a packet to a specific player.<br />
        /// Only works server side.<br />
        /// <para><paramref name="serialized"/> = input pre-serialized data if you have it (optimization) or leave null otherwise.</para>
        /// </summary>
        public void SendToPlayer(PacketBase packet, ulong steamId, byte[] serialized = null)
        {
            if(!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception($"{_modName}: Clients can't send packets to other clients directly!");

            if(serialized == null)
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, serialized, steamId);
        }

        /// <summary>
        /// Sends packet (or supplied bytes) to all players except server player.<br />
        /// NOTE: This does not ignore packet's sender! Only use this if you've not done the action on the sender themselves.<br />
        /// Only works server side.<br />
        /// <para><paramref name="serialized"/> = input pre-serialized data if you have it (optimization) or leave null otherwise.</para>
        /// </summary>
        public void SendToEveryone(PacketBase packet, byte[] serialized = null)
        {
            RelayToClients(packet, 0, serialized);
        }

        private void RelayToClients(PacketBase packet, ulong senderSteamId = 0, byte[] serialized = null)
        {
            if(!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception($"{_modName}: Clients can't relay packets!");

            _tempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(_tempPlayers);

            foreach(var p in _tempPlayers)
            {
                // skip sending to self (server player) or back to sender
                if(p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || p.SteamUserId == senderSteamId)
                    continue;

                if(serialized == null) // only serialize if necessary, and only once.
                    serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, serialized, p.SteamUserId);
            }

            _tempPlayers.Clear();
        }
        
        private void RelayToSenderOnly(PacketBase packet, ulong senderSteamId, byte[] serialized = null)
        {
            if(!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception($"{_modName}: Clients can't relay packets!");

            if(serialized == null) // only serialize if necessary
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, serialized, senderSteamId);
        }

        /// <summary>
        /// Executed when a packet is received on this machine
        /// </summary>
        private void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(serialized);
                HandlePacket(packet, senderSteamId, serialized);
            }
            catch(Exception e)
            {
                if(ExceptionHandler != null)
                {
                    ExceptionHandler.Invoke(e);
                }
                else
                {
                    DefaultExceptionHandler(e);
                }

                _tempPlayers.Clear();
                MyAPIGateway.Players.GetPlayers(_tempPlayers, (p) => p.SteamUserId == senderSteamId); // callback not ideal for speed but this is an error so whatever
                var sender = _tempPlayers.FirstOrDefault();
                _tempPlayers.Clear();

                if(ReceiveExceptionHandler != null)
                {
                    ReceiveExceptionHandler.Invoke(senderSteamId, sender, serialized);
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole($"{_modName} ReceivedPacket error additional info: sender={sender?.DisplayName ?? "<unknown>"} ({senderSteamId}); bytes={string.Join(",", serialized)}");
                }
            }
        }

        private void HandlePacket(PacketBase packet, ulong senderSteamId, byte[] serialized = null)
        {
            // Server-side OriginalSenderSteamId validation
            if(MyAPIGateway.Multiplayer.IsServer)
            {
                if(senderSteamId != packet.OriginalSenderSteamId)
                {
                    var text = $"WARNING: packet {packet.GetType().Name} from {senderSteamId.ToString()} has altered OriginalSenderSteamId to {packet.OriginalSenderSteamId.ToString()}. Replaced it with proper id, but if this triggers for everyone then it's a bug somewhere.";
                    if(ErrorHandler != null)
                        ErrorHandler.Invoke(text);
                    else
                        DefaultErrorHandler(text);

                    packet.OriginalSenderSteamId = senderSteamId;
                    serialized = null; // force reserialize
                }
            }

            var packetInfo = new PacketInfo()
            {
                Relay = RelayMode.None,
                Reserialize = false,
            };

            packet.Received(ref packetInfo, senderSteamId);

            if (!MyAPIGateway.Multiplayer.IsServer) return;
            
            if(packetInfo.Reserialize)
            {
                serialized = null;
            }

            switch(packetInfo.Relay)
            {
                case RelayMode.None: break;
                case RelayMode.ToOthers: RelayToClients(packet, senderSteamId, serialized); break;
                case RelayMode.ToEveryone: RelayToClients(packet, 0, serialized); break;
                case RelayMode.ReturnToSender: RelayToSenderOnly(packet, senderSteamId, serialized); break;
                default: throw new Exception($"{_modName}: Unknown relay mode: {packetInfo.Relay.ToString()}");
            }
        }

        private void DefaultExceptionHandler(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{_modName} ERROR: {e}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ERROR: {_modName}: {e.Message} | Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
        }

        private void DefaultErrorHandler(string error)
        {
            MyLog.Default.WriteLineAndConsole($"{_modName} ERROR: {error}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ERROR: {_modName}: {error} | Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
        }

        /// <summary>
        /// Calling this will schedule a crash with the given text.<para />
        /// If you throw an exception during loading it will instead kick player to main menu,<br />
        ///   that causes all sorts of problems because mods were not being told that world unloaded,<br />
        ///   leaving them with events hooked which will still work in future world loads even if no mods are present.
        /// </summary>
        public static void CrashAfterLoad(string text)
        {
            MyAPIGateway.Utilities = MyAPIUtilities.Static; // ensure this is assigned
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                throw new Exception(text);
            });
        }
    }

    public delegate void ReceiveDelegate<T>(T packet, ref PacketInfo packetInfo, ulong senderSteamId);

    public struct PacketInfo
    {
        /// <summary>
        /// Set whether this packet should be sent to other clients or not, when it reaches server.<br />
        /// Defaults to not relaying.<br />
        /// Has no effect when set on client receivers so it does not need any server checks.
        /// </summary>
        public RelayMode Relay;

        /// <summary>
        /// Set to true if you modified the packet and it needs re-serialization before relaying to clients.<br />
        /// Has no effect when set on client receivers so it does not need any server checks.<br />
        /// NOTE: this being false does not guarantee it won't be re-serialized, this is merely a flag that you should set if you modify the packet.
        /// </summary>
        public bool Reserialize;
    }

    public enum RelayMode
    {
        /// <summary>
        /// No automatic sending to other clients.
        /// </summary>
        None = 0,

        /// <summary>
        /// Automatically sends this packet to other clients except sender.<br />
        /// If server is also a player this does not send to them twice.
        /// </summary>
        ToOthers,

        /// <summary>
        /// Automatically sends this packet to all MP clients including sender.<br />
        /// If server is also a player this does not send to them twice.
        /// </summary>
        ToEveryone,
        
        /// <summary>
        /// Automatically sends this packet only back to the sender client.<br />
        /// If server is also a player this does not send to them twice.
        /// </summary>
        ReturnToSender
    }
}