
# Config Availability

| Config | Singleplayer | MpClient | MpHost | MpDedicated |
|--------|:------------:|:--------:|:------:|:-----------:|
| World  |      X       |          |   X    |      X      |
| Local  |      X       |    X     |   X    |      ~      |
| Global |      X       |    X     |   X    |      ~      |

~ ... It exists, but would be a global config for the whole server across worlds, so might not be very useful.

World ... Config stored in each `SEappdata/Saves/<userID>/<worldName>/Storage/<modID>/`

Local ... Config stored in a `SEappdata/Storage/<modID>/`

Global ... Config stored in a `SEappdata/Storage/`  **Be Carful to not conflict with other mods**

---

## Usage example
```csharp
public class CollectionConfig : ConfigBase
{
    public override string ConfigVersion => "0.3.0";

    public List<string> Tags { get; set; }
    public Dictionary<string, int> NamedValues { get; set; }
    public SubConfig Nested { get; set; }

    public override void ApplyDefaults()
    {
        Tags = new List<string>() { "alpha", "beta" };
        NamedValues = new Dictionary<string, int>()
        {
            { "start", 1 },
            { "end", 10 }
        };
        Nested = new SubConfig()
        {
            Threshold = 0.85f,
            Allowed = false
        };
    }

    public class SubConfig
    {
        public float Threshold { get; set; }
        public bool Allowed { get; set; }
    }
}

public class IntermediateConfig : ConfigBase
{
    public override string ConfigVersion => "0.2.0";

    public bool IsEnabled { get; set; } = true;
    public int? OptionalValue { get; set; } = null;
    public Mode CurrentMode { get; set; } = Mode.Basic;

    public enum Mode { Basic, Advanced, Expert }

    private static readonly IReadOnlyDictionary<string, string> _descriptions =
        new Dictionary<string, string>
        {
            {
                nameof(CurrentMode),
                "Select the operating mode.\nValid values: Basic, Advanced, Expert."
            },
            {
                nameof(OptionalValue),
                "Optional integer value.\nLeave empty to use no explicit value (null)."
            },
            {
                nameof(IsEnabled),
                "Master switch for this feature.\ntrue = enabled, false = disabled."
            }
        };

    public override IReadOnlyDictionary<string, string> VariableDescriptions => _descriptions;
}

// ---------------------------------------------------------
// MINIMAL MOD SESSION CODE (UserMod)
// ---------------------------------------------------------
public class MySession // (in SE this would be MySessionComponentBase)
{
    // World configs (networked)
    private CfgSync<CollectionConfig> _worldCollections;
    private CfgSync<IntermediateConfig> _worldIntermediate;

    // Local/Global configs are accessed via properties so they can be replaced by the framework.
    private CollectionConfig LocalCollections => ConfigStorage.Get<CollectionConfig>(LocationType.Local, "optionalName");
    private IntermediateConfig GlobalIntermediate => ConfigStorage.Get<IntermediateConfig>(LocationType.Global, "optionalName");

    public void LoadData()
    {
        ConfigStorage.Init(ModContext);

        // World handles (defaults immediately, real values arrive via sync)
        _worldCollections = ConfigStorage.World<CollectionConfig>("optionalName");
        _worldIntermediate = ConfigStorage.World<IntermediateConfig>("optionalName");

        // Optional: start Draft from current Auth so admin UI is synced initially
        _worldCollections.ResetDraft();
        _worldIntermediate.ResetDraft();
    }
    
    public void UnloadData() 
    {
        ConfigStorage.Unload();
    }

    public void UpdateLoop()
    {
        // 1) Start of tick: optionally observe updates/errors (you can ignore this entirely)
        ConsumeUpdate(_worldCollections);
        ConsumeUpdate(_worldIntermediate);

        // 2) Use local config (client-only)
        if (LocalCollections.Nested.Allowed)
        {
            // ...
        }

        // 3) Use global config (client-only)
        if (GlobalIntermediate.IsEnabled)
        {
            // ...
        }
        
        GlobalIntermediate.Save();
        GlobalIntermediate.SaveAndSwitch("Name");
        GlobalIntermediate.Load("Name");
        GlobalIntermediate.Export("Name");
        
        var cfg = GlobalIntermediate;
        cfg = cfg.SaveAndSwitch("Name"); // this works too if you don't use the global Get<> but is not recommended

        // 4) Use world authoritative config (server state)
        var worldA = _worldCollections.Auth;
        var worldB = _worldIntermediate.Auth;

        // Use them for gameplay:
        if (worldB.CurrentMode == IntermediateConfig.Mode.Expert)
        {
            // ...
        }

        // 5) Admin UI edits Draft, then calls world ops
        if (IsAdmin() && AdminWantsToEdit())
        {
            // edit Draft (never edit Auth)
            var draftA = _worldCollections.Draft;
            draftA.Nested.Allowed = true;
            draftA.NamedValues["end"] = 99;

            var draftB = _worldIntermediate.Draft;
            draftB.CurrentMode = IntermediateConfig.Mode.Advanced;
            draftB.OptionalValue = 123;

            // choose an operation:
            if (AdminPressedSaveCurrent())
                _worldCollections.Save(); // saves to CurrentFile

            if (AdminPressedSaveAndSwitch())
                _worldIntermediate.SaveAndSwitch("preset1.toml");

            if (AdminPressedExport())
                _worldIntermediate.Export("backup-2025-12-13.toml");

            if (AdminPressedLoadPreset())
                _worldCollections.LoadAndSwitch("dev.toml");

            if (AdminPressedResetDraft())
                _worldCollections.ResetDraft();
        }
    }

    private void ConsumeUpdate<T>(CfgSync<T> sync) where T : ConfigBase, new()
    {
        var upd = sync.GetUpdate();
        if (upd == null) return;

        if (upd.Error != null)
        {
            Logger(name + " error: " + upd.Error);
            return;
        }

        // Optional informational log:
        if (upd.TriggeredBy == _ctx.MyPlayerId)
            Logger(sync.Auth.TypeName + " request applied. Iteration=" + upd.ServerIteration);
        else
            Logger(sync.Auth.TypeName + " changed by another admin. Iteration=" + upd.ServerIteration);
    }

    // ---------------------------------------------------------
    // Stubs for whatever UI/input you use
    // ---------------------------------------------------------
    private bool IsAdmin() { return true; }
    private bool AdminWantsToEdit() { return false; }
    private bool AdminPressedSaveCurrent() { return false; }
    private bool AdminPressedSaveAndSwitch() { return false; }
    private bool AdminPressedExport() { return false; }
    private bool AdminPressedLoadPreset() { return false; }
    private bool AdminPressedResetDraft() { return false; }
    private void Logger(string msg) { /* MyLog.Default.WriteLineAndConsole(msg); */ }
}
```

## CodeFlow 
```csharp
// ============================================================================
// SYSTEM CONTROL FLOW (pseudocode, step-by-step, no skipped hops)
// Components:
//   - UserMod (consumer mod, owns real config types + serializer + filesystem)
//   - ConfigAPIMod (provider mod, owns routing + migrator + network + world state)
//
// Communication:
//   - Same machine: UserMod <-> ConfigAPIMod via ModAPI exchange (message channel)
//   - Network: ConfigAPIMod(Client) <-> ConfigAPIMod(Server) via SendMessageToServer/Broadcast
//
// Note: Server also runs UserMod and ConfigAPIMod. For World ops, server will
//       still go through the same network handler path (loopback) to keep code unified.
// ============================================================================


// ============================================================================
// PART 0: API SETUP (2-way), robust to load order
// Recommended approach: 2-phase
//   Phase A: UserMods discover ConfigAPIMod "Main API" (User -> Config)
//   Phase B: UserMods register their Callback API into ConfigAPIMod using that main API
//
// This avoids ConfigAPIMod needing to discover every user mod preemptively.
// ============================================================================

/*------------------------------*
 * (A) DISCOVERY: Main API link *
 *------------------------------*/

// Shared channel for API discovery on same machine, not the network channel.
const ushort API_DISCOVERY_CH = 42000;

>>> UserMod (LoadData)
RegisterListener(API_DISCOVERY_CH, OnApiDiscoveryMessage);

SendApiDiscoveryPing(); // "I am UserMod <modId>, do you have ConfigAPIMod API?"

    >>> UserMod (SendApiDiscoveryPing)
    SendMessageToLocal(API_DISCOVERY_CH, {
        kind: "PING",
        fromModId: MyModId,
        fromModName: MyModName
    });
    <<< UserMod (SendApiDiscoveryPing)

<<< UserMod (LoadData)


>>> ConfigAPIMod (LoadData)
RegisterListener(API_DISCOVERY_CH, OnApiDiscoveryMessage);

// ConfigAPIMod announces itself (optional but helps late joiners)
SendApiDiscoveryAnnounce(); // "I am ConfigAPIMod, here is my Main API"

    >>> ConfigAPIMod (SendApiDiscoveryAnnounce)
    SendMessageToLocal(API_DISCOVERY_CH, {
        kind: "ANNOUNCE",
        fromModId: ConfigModId,
        api: ConfigMainApiDelegates,   // dictionary<string, Delegate>
        ready: true
    });
    <<< ConfigAPIMod (SendApiDiscoveryAnnounce)

<<< ConfigAPIMod (LoadData)


>>> UserMod (OnApiDiscoveryMessage)
if msg.kind == "ANNOUNCE" and msg contains ConfigMainApiDelegates:
    Store ConfigMainApi handle
    // Immediately register callbacks (Phase B)
    ConfigMainApi.AddCallbackApi(MyModId, MyCallbackApiDelegates)
<<< UserMod (OnApiDiscoveryMessage)


>>> ConfigAPIMod (OnApiDiscoveryMessage)
if msg.kind == "PING":
    Reply with ANNOUNCE containing ConfigMainApiDelegates
<<< ConfigAPIMod (OnApiDiscoveryMessage)


/*----------------------------------------------*
 * (B) REGISTRATION: Callback API (User -> Config)
 *----------------------------------------------*/

// This happens after UserMod has ConfigMainApi.
// No need for ConfigAPIMod to discover UserMods by itself.

>>> UserMod (after receiving ConfigMainApi)
ConfigMainApi.AddCallbackApi(MyModId, MyCallbackApiDelegates)
    // MyCallbackApiDelegates includes:
    // - LoadFile(location, file)
    // - SaveFile(location, file, content)
    // - BackupFile(location, file)
    // - NewDefault(typeKey)
    // - Serialize(typeKey, ConfigBase instance, includeCommentsBool)
    // - Deserialize(typeKey, xmlOrTomlString)
<<< UserMod


>>> ConfigAPIMod (AddCallbackApi)
callbackApis[modId] = delegates;
MarkUserModReady(modId);
<<< ConfigAPIMod (AddCallbackApi)


// ============================================================================
// PART 1: LOCAL / GLOBAL CONFIG (no network)
// The filesystem + (de)serialize lives in UserMod, but the storage orchestration
// can still be in ConfigAPIMod (same machine) if you want.
// If you keep local/global fully inside UserMod, remove the ConfigAPIMod hop.
// Below shows the "unified path" variant that still uses ConfigAPIMod callbacks.
// ============================================================================

>>> UserMod (UpdateLoop)
cfgLocal  = ConfigStorage.Get<CollectionConfig>(Location.Local);
cfgGlobal = ConfigStorage.Get<IntermediateConfig>(Location.Global);
<<< UserMod


>>> UserMod (ConfigStorage.Get<T>)
if ConfigMainApi not ready:
    // fallback to defaults
    def = NewDefault<T>(); def.ApplyDefaults(); return def

return ConfigMainApi.GetLocalConfig(modId, typeKey, location, currentFileOrDefault)
    >>> ConfigAPIMod (GetLocalConfig)
    // local/global instance cache per (modId,typeKey,location)
    if instanceDict contains key and filename matches:
        return instance

    // load file must happen in UserMod folder => callback
    cb = callbackApis[modId]
    fileContent = cb.LoadFile(location, filename)      // may be null if missing
    defaultFileContent = cb.LoadFile(location, filename+".default") // optional

        >>> UserMod (Callback.LoadFile)
        path = BuildPathFrom(modId, location, filename)
        if FileExists(path) return FileReadAllText(path)
        return null
        <<< UserMod (Callback.LoadFile)

    // Convert + migrate inside ConfigAPIMod (no user input)
    // (internal canonical XML used for migrator; file might be TOML or XML)
    xmlContent  = Converter.ToInternal(fileContent)
    xmlDefault  = Converter.ToInternal(defaultFileContent)

    // Need type-default xml (from actual type) => callback
    cfgTypeDefault = cb.NewDefault(typeKey); cfgTypeDefault.ApplyDefaults()
    xmlTypeDefault = cb.Serialize(typeKey, cfgTypeDefault, includeComments:false)

        >>> UserMod (Callback.NewDefault)
        def = typeDict[typeKey].NewDefault(); def.ApplyDefaults(); return def
        <<< UserMod (Callback.NewDefault)

        >>> UserMod (Callback.Serialize)
        // serialize real type to XML internal form (no comments for traffic)
        return XmlSerialize(instance)   // your existing serializer
        <<< UserMod (Callback.Serialize)

    if xmlContent is null:
        // missing or invalid: write defaults + return defaults
        if fileContent != null: cb.BackupFile(location, filename)

            >>> UserMod (Callback.BackupFile)
            Copy file -> file.bak
            <<< UserMod (Callback.BackupFile)

        // write external file (TOML with comments) using defaults
        // comments only for disk, not for network
        fileExternal = Converter.ToExternal(xmlTypeDefault, cfgTypeDefault.VariableDescriptions /*comments*/)
        cb.SaveFile(location, filename, fileExternal)

            >>> UserMod (Callback.SaveFile)
            path = BuildPathFrom(modId, location, filename)
            FileWriteAllText(path, content)
            <<< UserMod (Callback.SaveFile)

        instanceDict[key] = cfgTypeDefault (FileName=filename)
        return cfgTypeDefault

    // validate/migrate existing file
    (normXml, normDefaultsXml, requiresBackup, errMsg) =
        Migrator.Verify(typeKey, xmlContent, xmlDefault, xmlTypeDefault)

    if errMsg != null:
        // type mismatch etc.
        return null (or defaults + error reporting)

    if requiresBackup: cb.BackupFile(location, filename)

    if normXml != xmlContent or normDefaultsXml != xmlDefault:
        fileExternalNorm = Converter.ToExternal(normXml, commentsFromTypeDefault)
        cb.SaveFile(location, filename, fileExternalNorm)

    // deserialize final instance => callback
    instance = cb.Deserialize(typeKey, normXml)

        >>> UserMod (Callback.Deserialize)
        return XmlDeserialize<T>(xmlString)
        <<< UserMod (Callback.Deserialize)

    instanceDict[key] = instance (FileName=filename)
    return instance
    <<< ConfigAPIMod (GetLocalConfig)
<<< UserMod (ConfigStorage.Get<T>)


// ============================================================================
// PART 2: WORLD CONFIG (networked)
// Client keeps CfgSync<T> with Auth/Draft.
// Each tick, user MUST call GetUpdate() once to pump updates.
// World ops go: UserMod -> ConfigAPIMod(client) -> network -> ConfigAPIMod(server)
//             -> callbacks into UserMod(server) for IO + (de)serialize -> apply -> broadcast
// ============================================================================

/*------------------------*
 * (2.1) World handle init *
 *------------------------*/

>>> UserMod (LoadData)
_worldCfg = ConfigStorage.World<CollectionConfig>(); // returns CfgSync<T> defaulted
// Draft starts default, then optionally ResetDraft() to copy Auth when first update arrives
<<< UserMod


>>> UserMod (ConfigStorage.World<T>)
if ConfigMainApi not ready:
    // Return a local placeholder sync object (defaults)
    sync = new CfgSync<T>(defaults); return sync

return ConfigMainApi.OpenWorldCfg(modId, typeKey, defaultFile="typename.toml")

    >>> ConfigAPIMod(Client) (OpenWorldCfg)
    // Create/return client-side sync object keyed by (modId,typeKey)
    if not exists: create CfgSync<T> with:
        Auth = NewDefault via callback; ApplyDefaults()
        Draft = DeepCopy(Auth)
        ClientKnownIteration = 0
        CurrentFile = defaultFile
        PendingUpdate = none

    // Start initial load/sync request to server (optional but recommended)
    EnqueueWorldRequest( Op=LoadAndSwitch, File=CurrentFile, BaseIteration=ClientKnownIteration )

        >>> ConfigAPIMod(Client) (SendMessageToServer)
        msg = { modId, typeKey, op, file, baseIter, playerId, payloadTomlOrNull }
        SendMessageToServer(NET_CHANNEL, Protobuf(msg))
        <<< ConfigAPIMod(Client) (SendMessageToServer)

    return sync
    <<< ConfigAPIMod(Client) (OpenWorldCfg)

<<< UserMod (ConfigStorage.World<T>)


/*---------------------------------------------*
 * (2.2) Per-tick pump: GetUpdate() on the client *
 *---------------------------------------------*/

>>> UserMod (UpdateLoop)
upd = _worldCfg.GetUpdate(); // MUST be called once per frame for sync
// handling upd is optional (log errors, ack, etc.)
worldAuth = _worldCfg.Auth;
worldDraft = _worldCfg.Draft;
<<< UserMod


>>> UserMod (CfgSync<T>.GetUpdate)
return ConfigMainApi.PollWorldUpdate(modId, typeKey)
    >>> ConfigAPIMod(Client) (PollWorldUpdate)
    // drain network inbox already received for this (modId,typeKey)
    // apply latest update to sync.Auth and sync.ClientKnownIteration
    if pendingUpdate exists:
        if pendingUpdate.error != null:
            store update meta; clear pendingUpdate; return updateMeta
        else:
            sync.Auth = pendingUpdate.authInstance
            sync.ClientKnownIteration = pendingUpdate.serverIteration
            sync.CurrentFile = pendingUpdate.currentFile
            clear pendingUpdate
            return updateMeta
    return null
    <<< ConfigAPIMod(Client) (PollWorldUpdate)
<<< UserMod (CfgSync<T>.GetUpdate)


/*----------------------------------------*
 * (2.3) Client requests (Save/Load/Switch/Export)
 *----------------------------------------*/

>>> UserMod (Admin action)
_worldCfg.Save(); // or SaveAndSwitch("preset1.toml"), Export("x.toml"), LoadAndSwitch("y.toml")
<<< UserMod


>>> UserMod (CfgSync<T>.SaveAndSwitch)
return ConfigMainApi.WorldSaveAndSwitch(modId, typeKey, file, baseIter=_knownIter, draft=_draft)
    >>> ConfigAPIMod(Client) (WorldSaveAndSwitch)
    if requestInFlight[(modId,typeKey)] == true:
        return false

    cb = callbackApis[modId]
    // For network: serialize WITHOUT comments to reduce size
    payloadXml = cb.Serialize(typeKey, draft, includeComments:false)
    payloadToml = Converter.InternalXmlToNetworkToml(payloadXml) // compact

        >>> UserMod (Callback.Serialize)
        return XmlSerialize(draftInstance)
        <<< UserMod (Callback.Serialize)

    msg = { modId, typeKey, op=SaveAndSwitch, file, baseIter, playerId, payloadToml }
    SendMessageToServer(NET_CHANNEL, Protobuf(msg))
    requestInFlight[(modId,typeKey)] = true
    return true
    <<< ConfigAPIMod(Client) (WorldSaveAndSwitch)
<<< UserMod (CfgSync<T>.SaveAndSwitch)


/*----------------------------------------------------*
 * (2.4) Server receives world request and processes it *
 *----------------------------------------------------*/

>>> ConfigAPIMod(Server) (OnNetworkMessageReceived)
Parse Protobuf(msg)
Route by (msg.modId, msg.typeKey)
DispatchWorldOp(msg)
<<< ConfigAPIMod(Server)


>>> ConfigAPIMod(Server) (DispatchWorldOp)
state = worldState[(modId,typeKey)] // holds Auth, CurrentFile, ServerIteration

// enforce permissions (admin check)
if !IsAdmin(msg.playerId):
    SendErrorToPlayer(msg.playerId, "Not allowed"); return

// enforce optimistic concurrency:
if msg.baseIter != state.ServerIteration:
    SendRejectStale(msg.playerId, state) // include current Auth/iter/currentFile
    return

cb = callbackApis[modId] // must exist on server too

switch msg.op:

  case LoadAndSwitch:
      // file IO must happen in UserMod folder => callback
      fileContent = cb.LoadFile(World, msg.file)

          >>> UserMod(Server) (Callback.LoadFile)
          path = BuildPathFrom(modId, World, msg.file)
          return FileReadAllTextOrNull(path)
          <<< UserMod(Server) (Callback.LoadFile)

      xmlContent = Converter.ToInternal(fileContent)
      // create type default (needed for migration/comments)
      cfgTypeDefault = cb.NewDefault(typeKey); cfgTypeDefault.ApplyDefaults()
      xmlTypeDefault = cb.Serialize(typeKey, cfgTypeDefault, includeComments:false)

          >>> UserMod(Server) (Callback.NewDefault) <<<  // as above
          >>> UserMod(Server) (Callback.Serialize)  <<<

      (normXml, normDefaultsXml, backup, err) = Migrator.Verify(typeKey, xmlContent, xmlDefaultFromFile, xmlTypeDefault)
      if err != null: SendErrorToPlayer(msg.playerId, err); return

      if backup: cb.BackupFile(World, msg.file)
      if needed: cb.SaveFile(World, msg.file, Converter.ToExternal(normXml, commentsFromDefault))

      // deserialize to real type => callback
      newAuth = cb.Deserialize(typeKey, normXml)

          >>> UserMod(Server) (Callback.Deserialize)
          return XmlDeserialize<T>(normXml)
          <<< UserMod(Server) (Callback.Deserialize)

      state.Auth = newAuth
      state.CurrentFile = msg.file
      state.ServerIteration++

      BroadcastWorldUpdateToAll(state)

  case Save:
  case SaveAndSwitch:
      // payloadToml -> internal xml -> validate/migrate -> deserialize
      xmlPayload = Converter.NetworkTomlToInternalXml(msg.payloadToml)

      cfgTypeDefault = cb.NewDefault(typeKey); cfgTypeDefault.ApplyDefaults()
      xmlTypeDefault = cb.Serialize(typeKey, cfgTypeDefault, includeComments:false)

      (normXml, _, backup, err) = Migrator.Verify(typeKey, xmlPayload, xmlDefaultFromType, xmlTypeDefault)
      if err != null: SendErrorToPlayer(msg.playerId, err); return

      newAuth = cb.Deserialize(typeKey, normXml)

      // save to file: file target depends on op
      targetFile = (op==Save) ? state.CurrentFile : msg.file

      // write with comments on disk: need default instance descriptions
      fileExternal = Converter.ToExternal(normXml, cfgTypeDefault.VariableDescriptions)
      cb.SaveFile(World, targetFile, fileExternal)

          >>> UserMod(Server) (Callback.SaveFile)
          FileWriteAllText(modFolder/world/targetFile, content)
          <<< UserMod(Server) (Callback.SaveFile)

      state.Auth = newAuth
      if op==SaveAndSwitch: state.CurrentFile = msg.file
      state.ServerIteration++

      BroadcastWorldUpdateToAll(state)

  case Export:
      // Export: save-only, no switch, no broadcast, no iteration bump
      xmlPayload = Converter.NetworkTomlToInternalXml(msg.payloadToml)
      cfgTypeDefault = cb.NewDefault(typeKey); cfgTypeDefault.ApplyDefaults()
      xmlTypeDefault = cb.Serialize(typeKey, cfgTypeDefault, includeComments:false)
      (normXml, _, _, err) = Migrator.Verify(...)
      if err != null: SendErrorToPlayer(msg.playerId, err); return

      fileExternal = Converter.ToExternal(normXml, cfgTypeDefault.VariableDescriptions)
      cb.SaveFile(World, msg.file, fileExternal)
      SendAckToPlayer(msg.playerId, "Export ok") // optional
      // state unchanged, no broadcast

<<< ConfigAPIMod(Server) (DispatchWorldOp)


/*----------------------------------------------*
 * (2.5) Server broadcasts world update to clients *
 *----------------------------------------------*/

>>> ConfigAPIMod(Server) (BroadcastWorldUpdateToAll)
payloadXml = cb.Serialize(typeKey, state.Auth, includeComments:false)
payloadToml = Converter.InternalXmlToNetworkToml(payloadXml)

msgUpdate = {
   modId, typeKey,
   op="WorldUpdate",
   serverIteration=state.ServerIteration,
   currentFile=state.CurrentFile,
   triggeredBy=originalRequester,
   payloadToml
}
Broadcast(NET_CHANNEL, Protobuf(msgUpdate))
<<< ConfigAPIMod(Server)


/*---------------------------------------------------*
 * (2.6) Client receives broadcast and stores pending update *
 *---------------------------------------------------*/

>>> ConfigAPIMod(Client) (OnNetworkMessageReceived)
if msg.op == "WorldUpdate":
    cb = callbackApis[msg.modId] // on client side too
    xmlPayload = Converter.NetworkTomlToInternalXml(msg.payloadToml)
    authInstance = cb.Deserialize(typeKey, xmlPayload)

        >>> UserMod(Client) (Callback.Deserialize)
        return XmlDeserialize<T>(xmlPayload)
        <<< UserMod(Client) (Callback.Deserialize)

    // store as "latest pending update" (replace older pending update)
    sync = worldSync[(modId,typeKey)]
    sync.pendingUpdate = { authInstance, msg.serverIteration, msg.currentFile, msg.triggeredBy, error=null }

if msg.op == "Error":
    sync.pendingUpdate = { serverConfig=null, error=msg.error, ... }

<<< ConfigAPIMod(Client) (OnNetworkMessageReceived)


// ============================================================================
// IMPORTANT USER RULE (documented):
// In each UpdateLoop:
//   call _worldCfg.GetUpdate() once near the start,
//   then use _worldCfg.Auth safely for the remainder of the tick.
// Draft edits are local until a successful Save/Load op is applied by the server.
// ============================================================================

```