# What the ClassicUO forks are carrying

419 forks of `ClassicUO/ClassicUO` scanned on 2026-09-14, by the same method as
`fork-scan.md`: every branch listed, every SHA checked against local history,
and everything unknown fetched and read.

| | |
| --- | --- |
| forks listed | 419 |
| branches found | 3,096 |
| branches already in upstream history | 1,837 |
| branches carrying unknown commits | 1,259, across 263 forks |
| unique commits | 2,578 |
| fix-shaped after filtering | 714 |

Reference remotes now in this repo: `cuo`, `taz`, `mw`, `servuo`, plus every
fetched fork under `refs/forks/` (TazUO) and `refs/cuoforks/` (ClassicUO).

---

## Found in our own code while checking

Neither is this fork's doing - both were inherited - but both are in the
client we ship.

### 43 `Console.WriteLine` calls in shipping client code

Eight of them are in `PacketHandlers.cs`, in packet-handling paths that run
during ordinary play:

```
1805  Console.WriteLine("=== DENY === ADD TO CONTAINER");
1843  Console.WriteLine("=== DENY === ADD TO PAPERDOLL");
1854  Console.WriteLine("=== DENY === SOMETHING WRONG");
1861  Console.WriteLine("=== DENY === ADD TO TERRAIN");
1928  Console.WriteLine("PACKET - ITEM DROP OK!");
6446  Console.WriteLine("ADD ITEM TO CONTAINER -- CLEAR HOLD");
```

`ScriptBrowser.cs` has ten more, `AnchorManager.cs` four. The count is
identical in the 4.5.23 base, so this is upstream debug output that nobody
removed - but it fires on every item drop, and the release build is `WinExe`
with no console attached to receive it.

### `PATHFINDER_MAX_NODES` is 15x ClassicUO's

| | |
| --- | --- |
| ClassicUO | 10,000 |
| iZakaroN's fork, deliberately lowered | 5,000 |
| TazUO 4.5.23, TazUO main, and us | **150,000** |

A pathfind that cannot reach its goal expands up to 150,000 nodes before giving
up. That is a stall, not a slowdown, and it is a TazUO decision rather than
ours - worth knowing about before hunting a stutter somewhere else.

---

## The important structural finding

**ClassicUO's own recent work is an ECS rewrite and does not port.**
andreakarasho's most interesting commits - `+ fix walking over multi`,
`+ reduce the entities to parse while walking`, `+ netclient: span everywhere`,
`+ screenposition component to cache the IsoToWorld values`, `fix(ecs): cull and
occlude dynamic lights like legacy` - all live under `Ecs/Gameplay/...` in a
plugin/system architecture with no counterpart in our tree. They are worth
reading for the *reasoning*, never for the diff.

---

## Candidates worth a proper look

### Core engine

| commit | who | what |
| --- | --- | --- |
| `a34e882284` | iZakaroN | Pathfinding null guards (`?.Value`, `node != null`) plus deliberately lowered node and map limits. The null guards are straight crash fixes; the limits speak to the 150,000 above. |
| `fc8f6d5801` | iZakaroN | Pathfinding rebuilt on a 2D node array and an ordered linked list for cheapest-first. |
| `dcb501b7c1` | Lichtblitz | Multi-threads the world-prep loop that walks chunks before rendering. Large potential win, large risk. |
| `3b80d22e97` | Spasitjel | Race condition handling plugin packets. |
| `a3d9190669` | andreakarasho | Stop allocating for data passed from plugins, and remove plugin concurrency. Pre-ECS, so it may still apply. |
| `243741c1b1` | DarkLotus | Remove `Trace.WriteLine` - was crashing on macOS. Relates to the console output above. |

### Protocol and packets

| commit | who | what |
| --- | --- | --- |
| `f01a7e96d5` | brndd | `0x93` packet UTF-8 handling. |
| `38285f27d3` | brndd | `PUnicodeSpeechRequest` sending too many null bytes. |
| `9198aaf1c4` | brndd | Item desync - restores a `RemoveItemFromContainer` that upstream had commented out. **We already call it**, so this one is closed. |
| `690e7d5513` | Kevin Eady | BuffDebuff packet parsing. |
| `e0b5c30495` | Storm Kiernan | Reversed packets for stun and disarm. |
| `586b0f445a` | Jakub Linhart | Stop appending characters after the `\0` that terminates a string - older Sphere servers. |
| `23dd919a6e` | Jakub Linhart | Client 306m ignores `bodyconv.def` without this change. |
| `a61ef95385` | DarkLotus | Do not send packets back to the assistant that came from it - fixes targeting. |

### Movement

| commit | who | what |
| --- | --- | --- |
| `cce0cceda3` | Storm Kiernan | `Player.Walk` should respect the AlwaysRun flag. |
| `21bdcc09d0` | Storm Kiernan | UOS `walk` should turn to face before moving. |
| `be91488e10` | DarkLotus | Corpses not auto-opening on some server types without movement. |
| `4cecd94104` | Diogo Strube | Horse movement. |

---

## Notable forks

| fork | what it is |
| --- | --- |
| `anethus`, `roxya`, `brndd` | The three with the most independent branches. brndd's are real protocol fixes. |
| `dust765` | Long-running, widely used fork; `gaechti/TazUO-x-Dust765` is a merge of it with TazUO. |
| `kamronbatman` | ModernUO's author. 17 branches. |
| `iZakaroN` | Pathfinding work, the most focused engine contribution found. |
| `Lichtblitz` | Multi-threaded render preparation. |
| `RazorEnhanced`, `Reetus`, `markdwags`, `jaedan` | Assistant and tooling integration. |
| `JakubLinhart` | Sphere-server and old-client compatibility. |
| `Storm Kiernan` (in several forks) | Packet and movement correctness. |
| `UOOutlands`, `Vita-Nex`, `Voxpire`, `ServUOX` | Shard and server-project forks. |
| `Tolokio/ClassicUOExtended`, `SneauxROSE/ClassicUORenaissance`, `filipehb/EpicUO`, `2dchaos/ZanUO` | Named derivative clients. |

---

## The ECS rewrite — what it actually is

Earlier in this document the ECS work is dismissed as "not portable". That is
true, but it is worth being precise about what it is, because it determines
where ClassicUO's fixes will and will not come from in future.

### Two branches, not one

| branch | commits ahead of `cuo/main` | last commit | state |
| --- | --- | --- | --- |
| `cuo/imp/ecs-source-gen` | 202 | **2025-06-11** | dormant, 15 months untouched |
| `cuo/impl/ecs` | **593** | **2026-06-28** | **active** |

The dormant one is the easy one to find and the wrong one to read. The live
work is on `impl/ecs`, started 2024-04-20 and still moving.

### What it is

A ground-up re-architecture of the client as an **entity-component-system** on
`TinyEcs`, with a **Bevy-style** plugin and scheduling API - Bevy being the Rust
game engine whose design it copies. The shape is visible in `Boot.cs`:

```csharp
internal readonly struct CuoPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddState(GameState.Loading);
        app.AddResource(new GameContext() { Map = -1, MaxObjectsDistance = 32 });
        app.AddResource(Settings.GlobalSettings);

        app.AddSystem(Stage.Startup, (ResMut<GameContext> gameCtx, Res<Settings> settings) => { ... });

        app.AddPlugin<PersistencePlugin>();
        app.AddPlugin(new FnaPlugin() { WindowResizable = true, MouseVisible = false, VSync = true });
```

Instead of objects with methods - `Mobile.ProcessSteps`, `World.Update`,
`GameScene.Draw` - there are **components** (plain data on entities) and
**systems** (free functions that query for the components they need, declared
as `Res<T>` / `ResMut<T>` parameters and wired by the scheduler). Everything is
assembled from plugins: `PlayerMovementPlugin`, `TerrainPlugin`,
`ContainersPlugin`, `NetworkPlugin`, `WorldRenderingPlugin`.

### The project layout it implies

```
src/ClassicUO.Ecs/            the new client - ~30 plugins
src/ClassicUO.Client/         the legacy client, still present, still builds
src/ClassicUO.Bootstrap/      apphost
src/ClassicUO.Agent.Host/     AGENT_BUILD only - a remote-control surface
src/ClassicUO.Agent.Contracts/
src/ClassicUO.Schema/
src/Mods/                     WASM mods, in Rust and C#, via wasmtime-dotnet
```

`net10.0`, `LangVersion` Preview. The legacy client is kept alongside rather
than deleted, so the two coexist in one tree.

Two things there are not in any other fork: an **agent host** for driving the
client programmatically, and a **WASM mod system** - `src/Mods/ecs-blocktest`
is a Rust crate with a `world.wit` interface definition, loaded through
`wasmtime-dotnet`.

### Why none of it ports here

- **Architecture.** There is no `Mobile.ProcessSteps` to patch. A fix reads as
  a change to a system's query or to component data.
- **Framework.** `net10.0` and preview C#, against our `net472`.
- **Dependencies.** TinyEcs, TinyEcs.Bevy, wasmtime - none available to us.

### What it means in practice

The commits that sound most relevant to this fork's open problems - **`+ fix
walking over multi`**, **`+ reduce the entities to parse while walking`**,
**`+ netclient: span everywhere`**, **`fix(ecs): cull and occlude dynamic lights
like legacy`**, **`perf(ecs): bump TinyEcs — zero-alloc parallel system
scheduler`** - are all on `impl/ecs`. Read them for the reasoning and re-derive
by hand; never attempt the diff.

It also means **ClassicUO's mainline is comparatively quiet**, and the fixes
this fork can actually take are coming from TazUO main and from the fork
network, not from ClassicUO itself.
