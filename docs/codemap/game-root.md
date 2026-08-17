# game-root

Partition = the 15 `.cs` files directly under `src/ClassicUO.Client/Game/` (maxdepth 1).
All 15 read in full: **7,895 lines**. Branch checked out: `claude/new-chat-session-0zuwmx`
(a `legacy`-line branch; HEAD `fb765aa0f`).

Paths below are relative to `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/`.

---

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `World.cs` | 1021 | Static god-object for the live world: `Items`/`Mobiles` dictionaries, `Player`, `Map`, season, light, per-frame entity sweep + distance cull. |
| `GameActions.cs` | 1315 | Static façade for "player did a thing": opens/closes gumps, sends outgoing packets via `AsyncNetClient.Socket`, feeds `ScriptRecorder`. |
| `Pathfinder.cs` | 1415 | Static A* over map tiles: walkability (`CanWalk`/`CalculateNewZ`), pooled `PathNode`/`PathObject`, auto-walk driver. |
| `UltimaLive.cs` | 1186 | UltimaLive protocol (packets 0x3F/0x40): live map/statics patching via memory-mapped `.mul` files, CRC hash replies, custom `MapLoader`. |
| `GameCursor.cs` | 826 | SDL/soft cursor: graphic-by-state, dragged-item render, tooltip dispatch, custom-house multi preview; owns `ItemHold`. |
| `UoAssist.cs` | 611 | Win32 hidden-window IPC (`UOASSIST-TP-MSG-WND`) for UOAssist-compatible tools. Namespace is `ClassicUO.Utility.Platforms`, not `ClassicUO.Game`. |
| `Weather.cs` | 490 | Rain/snow/storm particle sim + sounds; `WeatherType` enum. |
| `LinkedObject.cs` | 316 | Intrusive doubly-linked list base for entities/containers (`Items`/`Next`/`Previous`) + merge sort. |
| `CircleOfTransparency.cs` | 170 | Generated stencil circle texture used to see through walls. |
| `Constants.cs` | 155 | All tuning constants (FPS, delays, view ranges, GC caps, journal cap...). |
| `ItemHold.cs` | 143 | Snapshot of the item currently on the cursor (drag/drop state). |
| `SelectedObject.cs` | 93 | Static mouse-pick results (`Object`, `HealthbarObject`, `SelectedContainer`, `CorpseObject`) + isometric hit-test. |
| `SerialHelper.cs` | 72 | Serial range predicates: mobile `<0x40000000`, item `0x40000000..0x7FFFFFFF`. |
| `ScanTypeObject.cs` | 42 | Enum: Hostile/Party/Followers/Objects/Mobiles (used by `World.FindNearest`/`FindNext`). |
| `ScanModeObject.cs` | 40 | Enum: Next/Previous/Nearest. |

---

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `static class World` | World.cs:53 | Owns all live entity state; `Update()` is the world tick. |
| `static class GameActions` | GameActions.cs:51 | Player-intent → packet + UI. |
| `static class Pathfinder` | Pathfinder.cs:48 | A* + walkability + auto-walk. |
| `Pathfinder.PathObject` | Pathfinder.cs:1112 | Pooled per-tile solid record (flags/z/avgZ/height), `IComparable`. |
| `Pathfinder.PathNode` | Pathfinder.cs:1182 | Pooled A* node with `Parent` chain. |
| `Pathfinder.PriorityQueue` | Pathfinder.cs:1232 | Binary heap + `(x,y,z)` lookup, lazy duplicate removal, returns nodes to pool. |
| `Pathfinder.PATH_STEP_STATE` / `PATH_OBJECT_FLAGS` | Pathfinder.cs:1095 / :1103 | Normal/Dead-or-GM/SeaHorse/Flying; impassable/surface/bridge/no-diagonal. |
| `sealed class GameCursor` | GameCursor.cs:51 | Cursor state machine + drag render + tooltip. |
| `sealed class ItemHold` | ItemHold.cs:40 | Held-item snapshot (`Set` copies item fields by value at pickup). |
| `abstract class LinkedObject` | LinkedObject.cs:38 | Intrusive list node/head; base of `GameObject`/`Entity`. |
| `class UltimaLive` | UltimaLive.cs:51 | Singleton-ish (`static UltimaLive _UL`) UL session; `Enable()` registers packet handlers. |
| `UltimaLive.ULFileMul` | UltimaLive.cs:655 | `UOFileMul` over a named MemoryMappedFile, writable. |
| `UltimaLive.ULMapLoader` | UltimaLive.cs:775 | Replacement `MapLoader` pointing at the per-shard `.mul` copies. |
| `ULMapLoader.AsyncWriterTasked` | UltimaLive.cs:1149 | Background task draining `ConcurrentQueue<(int,long,byte[])>` to disk. |
| `internal class Weather` | Weather.cs:57 | Particle array `WeatherEffect[70]` + wind timers. |
| `Weather.WeatherEffect` | Weather.cs:485 | struct: pos/speed/angle/magnitude/id. |
| `enum WeatherType` | Weather.cs:46 | RAIN/STORM_APPROACH/SNOW/STORM_BREWING, 0xFE/0xFF = invalid. |
| `static class CircleOfTransparency` | CircleOfTransparency.cs:41 | Stencil texture + draw. |
| `static class SelectedObject` | SelectedObject.cs:40 | Mouse-pick globals + `_InternalArea[44,44]` diamond mask (built in static ctor, :51). |
| `static class SerialHelper` | SerialHelper.cs:38 | Serial classification. |
| `static class UoAssist` | UoAssist.cs:47 | Public signal API. |
| `UoAssist.CustomWindow` | UoAssist.cs:93 | Registers a Win32 class, owns `WndProc`, handles `WM_USER+200..314`. |
| `internal static class Constants` | Constants.cs:35 | Constants. |

---

## State

Everything below is process-global (`static`) unless noted; there is no locking anywhere in this partition.

**World.cs**
- `_effectManager` :55, `_toRemove` (`List<uint>`, reused by both sweeps *and* by `InternalMapChangeClear`) :56, `_timeToDelete` :57
- `RangeSize` :59, `Player` :61, `CustomHouseManager` :63, `WMapManager` :65, `ActiveSpellIcons` :67
- `LastObject`, `ObjectToRemove` :69 — single-slot deferred-removal serial, consumed in `Update` :294
- `OPL` :71, `DurabilityManager` :72, `CorpseManager` :74, `Party` :76, `HouseManager` :78
- `Items` (`Dictionary<uint,Item>`) :80, `Mobiles` (`Dictionary<uint,Mobile>`) :82, `Map` :84
- `ClientViewRange` (server-granted, default `Constants.MAX_VIEW_RANGE`=40) :87, `SkillsRequested` :89
- `Season`/`OldSeason` :91-92, `OldMusicIndex` (-1 sentinel) :97
- `WorldTextManager` :99, `Journal` :101, `CoolDownBarManager` :103, `Light` :165, `ClientLockedFeatures` :173, `ClientFeatures` :175, `ServerName` :177

**Pathfinder.cs** — `_goalNode` :51, `_pathfindDistance` :52, `_openSet` :53, `_closedSet` :54, `_path` :55, `_pointIndex` :56, `_run` :57, `_startPoint/_endPoint` :70, `_endPointZ` :72, `_reusableList` (shared scratch `List<PathObject>`) :73, `AutoWalking` :79, `PathFindingCanBeCancelled` :81, `BlockMoving` :83, `FastRotation` :85, `_listPool` :130, `PathObject._pool` :1114, `PathNode._pool` :1184.

**GameCursor.cs** — `_cursorData[3,16]` :53 (static), instance `_aura` :111, `_componentsList[10]` :112, `_cursorOffset[2,16]` :113, `_cursors_ptr[3,16]` (SDL cursor handles) :114, `_graphic` :115, `_needGraphicUpdate` :116, `_temp` (`List<Multi>` preview objects) :117, `_tooltip` :118, `ItemHold` :168. Single instance owned by `GameController` (`GameController.cs:222`).

**UltimaLive.cs** — `static _UL` :57 (whole-session singleton, replaced wholesale on packet 0x02 :465), `_EOF` :60, `_filesIdxStatics/_filesMap/_filesStatics` :61-63, `_SentWarning` :64, `_ULMap` :65, `_ValidMaps` :66, `_writequeue` :67, `MapCRCs[127][]` :68/:386, `MapSizeWrapSize[127,4]` :73.

**UoAssist.cs** — `_customWindow` :49; per-window `_wndRegs` :100, `_cmdID` :98, `m_hwnd` :103, `m_wnd_proc_delegate` :106.

**Others** — `CircleOfTransparency._stencil/_texture/_width/_height/_radius` :43-65; `SelectedObject.TranslatedMousePositionByViewport/Object/LastLeftDownObject/HealthbarObject/SelectedContainer/CorpseObject` :42-47 and `_InternalArea` :49; `GameActions.LastSpellIndex/LastSkillIndex` :53-54; `Weather` instance fields `_effects[70]` :62, `_timer/_windTimer/_lastTick` :63, `rainImage` :72.

---

## Timing

- **Per frame (GameScene.Update)** — `World.Update()` (`Game/Scenes/GameScene.cs:913`), immediately followed by `HouseDiagnostics.LogHouseContents()` (:914) and `Pathfinder.ProcessAutoWalk()` (:917).
- **Per frame (GameController)** — `GameCursor.Update()` (`GameController.cs:541`), `GameCursor.Draw()` (`GameController.cs:584`); `Weather.Draw()` from `GameScene.cs:1362`.
- **20 Hz inside `World.Update`** — the distance cull is gated by `_timeToDelete = Time.Ticks + 50` (World.cs:334-338). Entity `Update()` and party/ally world-map upkeep run every frame regardless (World.cs:354, :428).
- **`World.Update` order within one frame**: deferred `ObjectToRemove` (:294) → mobile sweep + `_toRemove` drain (:350-412) → item sweep + `_toRemove` drain (:424-477) → `_effectManager.Update()` → `WorldTextManager.Update()` → `WMapManager.RemoveUnupdatedWEntity()` (:479-481).
- **Per packet** — `UltimaLive.OnUltimaLivePacket` (0x3F) / `OnUpdateTerrainPacket` (0x40), registered in `UltimaLive.Enable()` (:81-82), called from `Client.cs:191`. Packet 0x01 does a synchronous `mapLoader.Load().Wait()` (:419).
- **On input** — everything in `GameActions`; `Pathfinder.WalkTo` (:1004) runs a full A* synchronously up to `PATHFINDER_MAX_NODES = 150000` (:50).
- **On demand / rate-limited** — UL "map not present" warning throttled to 100 s (`_SentWarning = Time.Ticks + 100000`, :111 and :229).
- **Background thread** — `ULMapLoader.AsyncWriterTasked.Loop` polls its queue with a 10 ms `AutoResetEvent.WaitOne` (:1172), started as `Task.Run` at :802.
- **Win32 message pump** — `CustomWindow.CustomWndProc` (UoAssist.cs:239) fires whenever the hidden window is pumped, and reads `World.Items` / `World.Player` from there (:267, :282).
- **Weather cadence** — a weather burst lives `Constants.WEATHER_TIMER` = 6·60·1000 ms (Constants.cs:138, set at Weather.cs:104); wind re-rolls every 13–19 s (Weather.cs:269); frame delta clamped to 25 if >7000 ms (:254-258); positions integrate against `SIMULATION_TIME = 37.0f` (:60, :403).
- **Relevant constants** — `MAX_STEP_COUNT 5`, `TURN_DELAY 100`, `TURN_DELAY_FAST 45`, `WALKING_DELAY/PLAYER_WALKING_DELAY 150`, `CHARACTER_ANIMATION_DELAY 80`, `ITEM_EFFECT_ANIMATION_DELAY 50`, `MIN/MAX_VIEW_RANGE 5/40`, `DRAG_ITEMS_DISTANCE 3`, `WAIT_FOR_TARGET_DELAY 5000`, `DEATH_SCREEN_TIMER 1500`, `MAX_JOURNAL_HISTORY_COUNT 5000` (Constants.cs:43-141).

---

## Inbound

- `Game/Scenes/GameScene.cs:913,917,1362` → `World.Update()`, `Pathfinder.ProcessAutoWalk()`, `Weather.Draw()`.
- `GameController.cs:222,541,584,627-629,775-778` → constructs `GameCursor`, ticks/draws it, toggles `AllowDrawSDLCursor` and pokes `Graphic = 0xFFFF`.
- `Client.cs:85` → `UoAssist.Start()`; `Client.cs:191` → `UltimaLive.Enable()`.
- `Network/PacketHandlers*` → `World.GetOrCreateItem/GetOrCreateMobile/RemoveItem/RemoveMobile/ChangeSeason/MapIndex/SpawnEffect`, and the UL handlers registered through `PacketHandlers.Handler.Add`.
- UI gumps, macros, `LegionScripting` (Legion + IronPython API) → `GameActions.*`, `World.FindNearest/FindNext`, `Pathfinder.WalkTo/GetPathTo`.
- Renderer/`GameScene` mouse picking writes `SelectedObject.*`; `GameCursor.Draw` and tooltips read it.
- `World.MapIndex` setter → `UoAssist.SignalMapChanged` (World.cs:158).

## Outbound

- `AsyncNetClient.Socket.Send_*` — the bulk of `GameActions`; `NetClient.Socket.Send_UOLive_HashResponse` (UltimaLive.cs:198).
- `UIManager.*` — `GetGump<T>/Add/AttemptDragControl/MouseOverControl/IsMouseOverWorld` from `GameActions`, `World.Update`/`RemoveItemFromContainer`, `GameCursor`, `UltimaLive` (`MiniMapGump.RequestUpdateContents`, :339, :560).
- Managers: `HouseManager` (`IsInsideKnownHouse`, `TryToRemove`, `Remove`, `Clear`), `WorldMapEntityManager`, `PartyManager`, `ObjectPropertiesListManager`, `CorpseManager`, `JournalManager`, `WorldTextManager`, `EffectManager`, `TargetManager`, `MessageManager`, `SpellVisualRangeManager`, `ChatManager`, `CommandManager`, `DelayedObjectClickManager`, `HouseDiagnostics`, `MusicDiagnostics`.
- `Client.Game.Audio` (`PlayMusic`, `StopWarMusic`, `CanSeasonMusicTakeOver`, `PlaySoundWithDistance`), `Client.Game.Arts` (`GetArt`, `CreateCursorSurfacePtr`), `Client.Game.GraphicsDevice`.
- Assets/IO: `MapLoader.Instance.LoadMap`, `TileDataLoader.Instance.StaticData`, `UOFileManager`, `ExternalImageLoader`.
- `ClassicUO.LegionScripting.ScriptRecorder.Instance.Record*` and `ScriptingInfoGump.AddOrUpdateInfo` from nearly every `GameActions` entry point.
- `EventSink.InvokeOnPlayerCreated` (World.cs:196), `EventSink.InvokeOnPathFinding` (Pathfinder.cs:1011).
- SDL: `SDL_CreateColorCursor`/`SDL_SetCursor` (GameCursor.cs:145, :244), `SDL_GetWindowWMInfo` (UoAssist.cs:112, :388). user32/kernel32 P/Invoke (UoAssist.cs:180-219).

---

## Hazards

*(factual observations, no causal claims)*

**Identity / pooling**
- World.cs:341-349 and :414-423 — in-source comments state that a destroyed entity is returned to a pool and can be re-handed-out under a different serial before the sweep runs, so the dictionary key and `entity.Serial` can disagree. Both drains therefore re-verify by key (`:402`, `:467`) before `Mobiles.Remove`/`Items.Remove` + `ReturnToPool`.
- World.cs:551-554 — `Get()` returns `null` for a destroyed entity, but that entity stays in `Items`/`Mobiles` until the next sweep; `Items.Get`/`Contains` elsewhere still see it.
- World.cs:565-570, :589-593 — `GetOrCreateItem`/`GetOrCreateMobile` remove and immediately `ReturnToPool` a destroyed entry, then create a new one under the same serial in the same call.
- Pathfinder.cs:1402-1411 — `PriorityQueue.RemoveAt` returns an invalidated node to the pool; nodes already enqueued may still hold it via `PathNode.Parent` (set at :792), and `ReconstructPath` (:941) walks that `Parent` chain.
- Pathfinder.cs:1287 — `Enqueue` returns the caller's node to the pool when an existing entry is cheaper.
- Pathfinder.cs:1084-1091 — `CleanupPathfinding` returns every `_closedSet` node to the pool before `_path.Clear()`; `_path` holds the same node references (`ReconstructPath` :972) until that line.
- Pathfinder.cs:136-150 — `GetAllObjectsAt` hands out a list from `_listPool`; nothing in this file returns it.
- ItemHold.cs:90-116 — `Set` copies graphic/hue/amount/layer/flags by value at pickup; later server updates to that item do not refresh the held copy.

**Mutation during iteration / shared scratch**
- World.cs:350-394 and :424-459 — the sweeps call `mob.Update()` / `item.Update()` and `RemoveMobile`/`RemoveItem` (non-force) while enumerating `Mobiles`/`Items`; removal from the dictionaries is deferred to the `_toRemove` drains.
- World.cs:56 vs :990/:1010 — `_toRemove` is a single static list shared by `Update`'s two sweeps and by `InternalMapChangeClear` (which is reached from the `MapIndex` setter, :113).
- Pathfinder.cs:73 — `_reusableList` is static and used by both `CalculateMinMaxZ` (:412-419) and `CalculateNewZ` (:515-536); `CalculateNewZ` calls `CalculateMinMaxZ`, which clears and refills the same list mid-call.
- Weather.cs:314-331 — the loop bound is `CurrentCount` and the body decrements `CurrentCount` (:324) while iterating.
- UoAssist.cs:467-474 — `SignalAddMulti` enumerates `_wndRegs` and calls `PostMessage`; `PostMessage(uint,…)` (:476-500) enumerates the same dictionary and removes entries after the loop.
- UltimaLive.cs:306-335 and :528-556 — packet handlers walk a chunk's tile lists while calling `RemoveFromTile()` on the current object, then `mapChunk.Clear()`, `Load()` and re-add; the render thread walks the same chunk lists.

**Loops / blocking on the frame thread**
- GameActions.cs:303-317 — `CloseSpellBook`: `g` is fetched once before the `while`, and is never reassigned or disposed when `g.SpellBookType != type`; the loop has no exit in that case.
- GameActions.cs:289-293 — `CloseAllJournals` loops on `GetGump<ResizableJournal>()` and relies on `Dispose()` making the next lookup return null.
- GameActions.cs:1094 — `Socket.Disconnect().Wait()` blocks the calling (frame) thread.
- Pathfinder.cs:889-939 — `FindPath` runs up to 150000 closed nodes synchronously; `WalkTo` (:1025) calls it from input handling.
- UltimaLive.cs:419 — `mapLoader.Load().Wait()` inside the 0x3F/0x01 packet handler.

**Null / range**
- World.cs:210 — `ChangeSeason` calls `Map.GetUsedChunks()` with no null check on `Map` (the inner `chunk?.` at :216 is guarded).
- World.cs:932 — `Clear()` dereferences `Player.Serial` before the `Player?.Destroy()` on :938.
- UltimaLive.cs:506/:509 — `OnUpdateTerrainPacket` dereferences `_UL._filesMap` / `_UL.MapCRCs` without the `_UL == null` guard the 0x3F cases use (:95, :205, :352).
- UltimaLive.cs:1107 — bounds check is `shifted < Entries.Length` but the index used is `Entries[map][shifted]`.
- GameCursor.cs:224-231, :527-536, :689 — `Graphic` is used as an array index after subtracting `0x2053`/`0x206A`; `Graphic` is set to the sentinel `0xFFFF` at World.cs:155 and GameController.cs:778, and is only overwritten by `AssignGraphicByState()` being called first (GameCursor.cs:214, :523).
- Weather.cs:231 — `PlaySound` reads `World.Player.X/Y` with no null check; reachable from `Draw` (:354, :381, :385).

**Client-side state the server owns**
- World.cs:87 — `ClientViewRange` is the server's answer but is initialised to `Constants.MAX_VIEW_RANGE` (40) while the code comments (World.cs:272-273) describe 24 as the protocol maximum.
- World.cs:275-288 `KeptByItsHouse` + :436-453 — items inside a client-held house are exempted from the distance cull entirely, so the client keeps contents the server has stopped refreshing until the house itself is released.
- World.cs:69/:294 + GameActions.cs:879 — `ObjectToRemove` is a single slot written by `PickUp` and consumed once per `World.Update`; a second pickup in the same frame overwrites the first.
- GameActions.cs:1210-1251 — `UsePrimaryAbility`/`UseSecondaryAbility` mutate `World.Player.Abilities[]` locally (mask `0x7F`, then XOR `0x80` at :1228/:1251) around the send.
- GameActions.cs:609 — `TargetManager.LastAttack` set client-side at request time, before any server confirmation.
- GameCursor.cs:249-281 / ItemHold — the dragged item is removed from its tile at GameActions.cs:874 and its `TextContainer` cleared (:877) before the server has acknowledged the pickup.

**Other**
- GameCursor.cs:124-148 — 48 SDL cursors created in the constructor; there is no `SDL_FreeCursor` anywhere in the file.
- GameCursor.cs:311-327, :361-366 — up to 10 `Multi` objects are `Multi.Create`d and `Destroy()`d on every `Draw` while in `CursorTarget.MultiPlacement`.
- Weather.cs:72 — `rainImage` is loaded from disk in a field initializer (runs on `Weather` construction).
- Weather.cs:434 — the y-branch tests `oldY <= -MAX_OFFSET_XY` where the x-branch tests `ofsx`.
- Weather.cs:79 — `SinOscillate` returns `Math.Sign(ToRadians(anglef)) * range`, i.e. a sign step, not a sinusoid.
- Weather.cs:83-90 — `Reset()` zeroes the counters but leaves the `_effects` array contents.
- UltimaLive.cs:458-469 — packet 0x02 replaces `_UL` with a new instance without disposing the previous loader, mmapped files, or writer task.
- UltimaLive.cs:747-750 — `ULFileMul.Dispose()` calls `MapLoader.Instance.Dispose()` instead of disposing itself.
- UltimaLive.cs:410 — `MapLoader.MAPS_COUNT` is raised to `sbyte.MaxValue` (127) globally.
- UltimaLive.cs:277-279 — the same `staticsData` buffer is written through the mmap accessor and then queued for the background writer.
- UoAssist.cs:256-263 — `REGISTER` with an already-known `wParam` removes the entry and returns 2 instead of updating it.
- UoAssist.cs:442-456 — `SignalStaminaUpdate` and `SignalManaUpdate` both post `HitsMax`/`Hits`.
- UoAssist.cs:174-237 — `Dispose` exists but `_customWindow` (:49) is never disposed.
- LinkedObject.cs:123-149 — `Insert` does not `Unlink` the item first (unlike `PushToBack`/`MoveToFront`/`MoveToBack`).
- LinkedObject.cs:304 — `tail.Next = null` after the merge pass, unguarded.
- Pathfinder.cs:874-880 — `FindCheapestNode` logs `"Node in both open and closed set. This shouldn't happen."` rather than treating it as an error.
- Pathfinder.cs:808-812/:928-933 — `_goalNode` is latched inside `AddNodeToList` and only acted on at the top of the *next* `FindPath` iteration.

---

## Fork deltas (TazUO / Holiday vs stock ClassicUO)

Holiday-Edition-looking (long explanatory comments in the fork's voice, plus new managers):
- `World.KeptByItsHouse` (World.cs:275-288) and the `Settings.GlobalSettings.KeepHouseContentsLoaded` gate; the multi-aware cull radius `ClientViewRange + item.MultiDistanceBonus` (World.cs:436) and `HouseDiagnostics.LogItemCulled` (World.cs:440) / `HouseDiagnostics.LogHouseContents` (GameScene.cs:914).
- The by-key sweeps and `TryGetValue`-before-`Remove` + `ReturnToPool` discipline in `World.Update` (World.cs:396-412, :461-477) with the pooling-identity comments.
- `World.OldMusicIndex` (World.cs:97) and `Constants.MUSIC_STOP_INDEX = 0xFFFF` (Constants.cs:98-101); `MusicDiagnostics.WarMode` (GameActions.cs:64); `ChangeSeason(season, music = -1)` semantics (World.cs:200-234).
- `Constants.MAX_JOURNAL_HISTORY_COUNT` comment tying it to the MaxJournalEntries slider (Constants.cs:127-130).

TazUO-looking (upstream-fork features, no Holiday commentary):
- `ScriptRecorder` / `ScriptingInfoGump` calls threaded through ~20 `GameActions` methods; `LegionScripting` gump helpers (GameActions.cs:103-122).
- Modern UI: `GridContainer`, `NearbyLootGump`, `GridLootGump`, `ModernPaperdoll`, `ModernOptionsGump`, `ResizableJournal`, `DurabilitysGump` — all referenced from `GameActions` and `World.Update`/`RemoveItemFromContainer`.
- `World.DurabilityManager`, `World.ActiveSpellIcons`, `World.CoolDownBarManager` (World.cs:67-103); `SpellVisualRangeManager.Instance.ClearCasting()` (GameActions.cs:1028, :1038); `GameActions.CastSpellByName`, `BandageSelf`, `RequestEquippedOPL`.
- `ScanTypeObject` made `public` and `World.FindNearest`/`FindNext` with `reverse` (World.cs:765, :831) for scripting.
- Global scaling in `GameCursor` (`ProfileManager.CurrentProfile.GlobalScaling/GlobalScale`, GameCursor.cs:416-419, :469-475, :573-577), `AuraOnMouse`, `ShowTargetRangeIndicator` (:382, :422), `ForceTooltipsOnOldClients` (:579).
- `Weather.rainImage` via `ExternalImageLoader` + `ExternalImages/rain.png` (Weather.cs:72, :439-443) replacing the stock line-drawn rain.
- Pathfinder rewrite: turn penalty (:755-763), `PriorityQueue` with lazy dedup (:1232), object pools for `PathNode`/`PathObject` (:1114, :1184), `GetPathTo` (:976), `ObjectBlocksLOS`/`GetAllObjectsAt` (:87, :136), `EventSink.InvokeOnPathFinding` (:1011).
- `EventSink.InvokeOnPlayerCreated` (World.cs:196); `AsyncNetClient` (`using static ClassicUO.Network.AsyncNetClient`, GameActions.cs:47) in place of stock `NetClient`.
