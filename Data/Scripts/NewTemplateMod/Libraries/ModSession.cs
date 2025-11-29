using Sandbox.ModAPI;
using VRage.Game;

public static class ModSession
{
    /// <summary>
    /// Returns true if multiplayer is active (either client or server).
    /// <list type="bullet">
    /// <item> Offline: false </item>
    /// <item> Private: <b>true</b> </item>
    /// <item> Friends: <b>true</b> </item>
    /// <item> Online:  <b>true</b> </item>
    /// </list>
    /// </summary>
    public static bool MultiplayerActive => MyAPIGateway.Multiplayer.MultiplayerActive == true;

    /// <summary>
    /// Returns the current online mode of the session.
    /// If no session is active, returns OFFLINE.
    /// </summary>
    public static MyOnlineModeEnum OnlineMode => MyAPIGateway.Session?.OnlineMode ?? MyOnlineModeEnum.OFFLINE;

    /// <summary>
    /// Returns true if the current session is a player.
    /// A Machine that has a player.
    /// <list type="bullet">
    /// <item> SinglePlayer: <b>true</b> </item>
    /// <item> Client in MP: <b>true</b> </item>
    /// <item> Host in MP: <b>true</b> </item>
    /// <item> Dedicated Server: false </item>
    /// </list>
    /// </summary>
    public static bool IsPlayer => !MyAPIGateway.Utilities.IsDedicated;

    /// <summary>
    /// Returns true if the current session has a loaded player character.
    /// Loaded means the player character is not in a loading screen and can receive chat messages.
    /// <list type="bullet">
    /// <item> SinglePlayer: <b>true</b> </item>
    /// <item> Client in MP: <b>true</b> </item>
    /// <item> Host in MP: <b>true</b> </item>
    /// <item> Dedicated Server: false </item>
    /// </list>
    /// </summary>
    public static bool IsClientLoaded => MyAPIGateway.Session?.Player?.Character != null;

    /// <summary>
    /// Returns true if the current session is running as Server (either dedicated or host).
    /// <list type="bullet">
    /// <item> SinglePlayer: <b>true</b> </item>
    /// <item> Client in MP: false </item>
    /// <item> Host in MP: <b>true</b> </item>
    /// <item> Dedicated Server: <b>true</b> </item>
    /// </list>
    /// </summary>
    public static bool IsServer => MyAPIGateway.Session?.IsServer == true;

    /// <summary>
    /// Returns true if the current session is a server in a multiplayer game.
    /// <list type="bullet">
    /// <item> SinglePlayer: false </item>
    /// <item> Client in MP: false </item>
    /// <item> Host in MP: <b>true</b> </item>
    /// <item> Dedicated Server: <b>true</b> </item>
    /// </list>
    /// </summary>
    public static bool IsServerInMp => IsServer && MultiplayerActive;

    /// <summary>
    /// Returns true if the current session is in single-player mode.
    /// <list type="bullet">
    /// <item> SinglePlayer: <b>true</b> </item>
    /// <item> Client in MP: false </item>
    /// <item> Host in MP: false </item>
    /// <item> Dedicated Server: false </item>
    /// </list>
    /// </summary>
    public static bool IsSinglePlayer => !MultiplayerActive;

    /// <summary>
    /// Returns true if the current session is a client in a multiplayer game.
    /// <list type="bullet">
    /// <item> SinglePlayer: false </item>
    /// <item> Client in MP: <b>true</b> </item>
    /// <item> Host in MP: false </item>
    /// <item> Dedicated Server: false </item>
    /// </list>
    /// </summary>
    public static bool IsClientInMp => !IsServer && MultiplayerActive;

    /// <summary>
    /// Returns true if the current session is the host in a multiplayer game.
    /// <list type="bullet">
    /// <item> SinglePlayer: false </item>
    /// <item> Client in MP: false </item>
    /// <item> Host in MP: <b>true</b> </item>
    /// <item> Dedicated Server: false </item>
    /// </list>
    /// </summary>
    public static bool IsHostInMp => IsServer && MultiplayerActive && !MyAPIGateway.Utilities.IsDedicated;

    /// <summary>
    /// Returns true if the current session is a dedicated server (i.e. no player character on this machine).
    /// <list type="bullet">
    /// <item> SinglePlayer: false </item>
    /// <item> Client in MP: false </item>
    /// <item> Host in MP: false </item>
    /// <item> Dedicated Server: <b>true</b> </item>
    /// </list>
    /// </summary>
    public static bool IsDedicated => MyAPIGateway.Utilities.IsDedicated;
}
