# Roadmap: ConfigAPIMod + UserMod API (World + Local/Global)

## Guiding constraints (lock these in)
- One shared **network channel** (ushort) used only by ConfigAPIMod.
- Same-machine APIs: UserMod ⇄ ConfigAPIMod via delegate dictionary exchange.
- World configs: key = (modId, typeFullName). Presets are `CurrentFile`, not identity.
- Client must call `CfgSync.GetUpdate()` once per tick to stay in sync.
- Network payload: TOML string (no comments), hard size cap (~4 KB message cap).
- Concurrency: `BaseIteration == ServerIteration` required for mutating world ops.

---

## Phase 0: Skeleton + smoke tests (1 evening)
### 0.1 Create repo layout
ConfigAPI/
- **Main/**  
  Provider implementation (runs when ConfigAPI mod is loaded)
    - Same-machine API discovery + registration (UserMods → ConfigAPI)
    - Routing tables (modId, typeKey)
    - Network send/receive (client + server side, since it’s one mod)
    - Server world state + request handling (permissions, iteration)
    - Migrator/Converter integration glue
    - Diagnostics / logging

- **Client/**  
  Consumer-side API that other mods include (copied into their mods)
    - Public API surface: `ConfigManager`, `ConfigStorage`, `CfgSync<T>`, `CfgUpdate`
    - Type registration: `ConfigDefinition<T>` (NewDefault, Serialize, Deserialize)
    - Callback API implementation (filesystem + serializer delegates)
    - The “bridge” that talks to Main via the same-machine delegate API

  (This folder is “passive” inside ConfigAPI itself. If you don’t call it, it does nothing.
  You copy **Shared + Client** into ConfigAPIExample to test as a consumer.)

- **Shared/**  
  Code shared by both Main and Client (copied into consumer mods too)
    - Keys: `(modId, typeKey)` structs
    - Message envelope structs (protobuf models)
    - Enums: OpCode, LocationType
    - Small utilities: size checks, string keys, safe helpers


### 0.2 Add “hello link” API discovery
- Local channel for discovery (not the network channel).
- ConfigAPIMod announces Main API.
- UserMod receives it and calls `AddCallbackApi(modId, callbacks)`.

**Exit criteria**
- UserMod can detect ConfigAPIMod loaded.
- ConfigAPIMod stores callback API for that mod.
- No crashes if either side loads first.

---

## Phase 1: Shared protocol + routing (core plumbing)
### 1.1 Define message envelope (protobuf)
Minimum fields:
- `ulong ModId`
- `string TypeKey` (typeof(T).FullName)
- `OpCode Op` (LoadAndSwitch, Save, SaveAndSwitch, Export, WorldUpdate, Error/Ack)
- `ulong BaseIteration`
- `ulong ServerIteration`
- `long TriggeredBy`
- `string File` (optional)
- `string PayloadToml` (optional)
- `string Error` (optional)

### 1.2 Build router tables (client + server)
- `Dictionary<(modId,typeKey), CfgSyncState>` on client
- `Dictionary<(modId,typeKey), WorldState>` on server
- Ensure “latest pending update replaces older pending update”.

**Exit criteria**
- Send/receive roundtrip works for a dummy message.
- Routing is correct for multiple mods (simulate with two local user mods).

---

## Phase 2: Callback API contract (UserMod side)
### 2.1 Define callback delegates (must be stable)
- `string LoadFile(LocationType loc, string filename)`
- `void SaveFile(LocationType loc, string filename, string content)`
- `void BackupFile(LocationType loc, string filename)`
- `ConfigBase NewDefault(string typeKey)` (must call ApplyDefaults inside user mod)
- `string Serialize(string typeKey, ConfigBase instance, bool includeComments)`
- `ConfigBase Deserialize(string typeKey, string internalXml)`

### 2.2 Implement UserMod “API folder” helpers
- `ConfigDefinition<T>` holds:
    - `NewDefault()` (calls ApplyDefaults)
    - `Serialize(T, includeComments)`
    - `Deserialize(string)`
- `ConfigStorage` public surface:
    - `CfgSync<T> World<T>()`
    - `T Get<T>(LocationType.Local/Global)`

**Exit criteria**
- UserMod can register type definitions.
- UserMod can serialize/deserialize both of your example configs.
- No reflection used.

---

## Phase 3: Converter/Migrator integration policy
### 3.1 Decide your internal canonical format
- Internal = your canonical XML format (already used by migrator).
- Disk = TOML with comments.
- Network = TOML without comments (or compact internal XML if you change your mind).

### 3.2 Implement conversions in ConfigAPIMod
- `Disk -> InternalXml` (ToInternal)
- `InternalXml -> DiskToml(with comments)` (ToExternal with descriptions)
- `InternalXml -> NetworkToml(no comments)` (compact)
- `NetworkToml -> InternalXml`

**Exit criteria**
- You can roundtrip: config instance -> internal xml -> network toml -> internal xml -> instance.
- Size check done on final protobuf bytes (reject when too big).

---

## Phase 4: Client-side world sync (CfgSync<T>) end-to-end
### 4.1 Client CfgSync state
For each (modId,typeKey):
- `Auth` (instance)
- `Draft` (instance)
- `CurrentFile`
- `ClientKnownIteration`
- `PendingUpdate` (latest only)
- `RequestInFlight` (bool)

### 4.2 Implement API calls
- `OpenWorldCfg` (create sync object + send initial LoadAndSwitch(CurrentFile))
- `PollWorldUpdate` (implements GetUpdate pump; applies PendingUpdate to Auth + iteration)
- `ResetDraft` (deep copy via serialize->deserialize through callback + your converter)
- Request methods:
    - `Save()`
    - `SaveAndSwitch(file)`
    - `Export(file)`
    - `LoadAndSwitch(file)`
      All should return `bool` (busy check).

**Exit criteria**
- Client can:
    - start with defaults
    - receive a broadcast and update Auth
    - edit Draft and send SaveAndSwitch

---

## Phase 5: Server-side world state + operations
### 5.1 Server world state
For each (modId,typeKey):
- `Auth` instance
- `CurrentFile`
- `ServerIteration`

### 5.2 Implement ops with iteration enforcement
- Reject if not admin.
- Reject if `BaseIteration != ServerIteration`.
- On accept:
    - apply op
    - bump `ServerIteration++` (except Export)
    - broadcast update to all clients

### 5.3 File operations (server) via callback
- All IO uses UserMod(server) callbacks so files go into correct mod folder.

**Exit criteria**
- Two clients connected:
    - one loads preset, both receive new Auth
    - one saves, both receive new Auth
    - stale save gets rejected with error to requester only

---

## Phase 6: Local/Global storage (non-network)
### 6.1 Decide where local/global orchestration lives
Option A (simplest): entirely in UserMod API folder (no ConfigAPIMod involvement)
- Pros: fewer hops, fewer moving parts
- Cons: migrator code duplicated unless shared folder

Option B (unified): local/global load goes through ConfigAPIMod using callbacks (your earlier pseudocode)
- Pros: single migrator/normalizer implementation
- Cons: extra same-machine API hop

Pick one and implement.

**Exit criteria**
- Local/Global config files created with defaults + comments.
- Reload after layout change triggers migrator normalization.

---

## Phase 7: Diagnostics + guardrails (cheap, high ROI)
- Log categories: API handshake, routing, ops, iteration conflicts.
- Rate-limit repeated errors.
- Provide clear error strings:
    - “Payload too large”
    - “Stale: baseIter X, serverIter Y”
    - “Not admin”
    - “Type mismatch file contains <OtherConfig>”
- Add a debug command in ConfigAPIMod:
    - list registered mods, types, iterations, current files

**Exit criteria**
- When something fails, you can tell why in 30 seconds.

---

## Phase 8: Minimal sample mod + integration test checklist
Create a tiny UserMod sample that:
- registers 2 configs (your examples)
- uses Local + Global
- uses World for both
- has admin buttons:
    - ResetDraft
    - Save
    - SaveAndSwitch("preset1")
    - Export("backup")
    - LoadAndSwitch("dev")

Test matrix:
- single player (server = client loopback)
- dedicated server + one client
- dedicated server + two clients (conflict rejection)
- late-joining client receives latest world state

**Exit criteria**
- All tests pass without manual file edits.

---

## What to build first (critical path)
1) Phase 1 (protocol + routing)
2) Phase 2 (callback API registration)
3) Phase 4 + 5 (world sync end-to-end)
4) Then local/global (Phase 6) once world is stable
