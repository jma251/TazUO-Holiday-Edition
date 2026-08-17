# managers-b

Partition = every 2nd file (even index) of `src/ClassicUO.Client/Game/Managers/*.cs` sorted.
34 files, 12,548 lines, all read in full.

All paths below are relative to `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/Managers/`.

## Files

| path | lines | purpose |
| --- | --- | --- |
| AnchorManager.cs | 542 | Gump-to-gump snapping. Keeps a `Dictionary<AnchorableGump, AnchorGroup>` and a 2-D `AnchorableGump[,]` matrix per group; moving one member drags all. |
| AnonMetrics.cs | 64 | Fire-and-forget `WebClient.UploadString` POST of server name + versions to `http://metrics.tazuo.org:5000` on first login. Global namespace, not `ClassicUO.Game.Managers`. |
| AuraManager.cs | 152 | `Aura` builds a 30-radius circle `Texture2D` once (static readonly, built at type init); `AuraManager.Draw` blits it under feet each frame with a custom `BlendState`. |
| BandageManager.cs | 145 | Auto-bandage agent. Subscribes to `EventSink.OnPlayerStatChange` / `OnBuffAdded` / `OnBuffRemoved`; heals on HP-drop events, gated by `nextBandageTime` and the Healing buff. |
| BuySellAgent.cs | 319 | Auto buy/sell at vendors. Per-vendor `VendorSellInfo` accumulator keyed by vendor serial; builds buy/sell lists and sends `Send_BuyRequest` / `Send_SellRequest`, then disposes the shop gump. |
| ChatManager.cs | 127 | Static dictionary of chat channels + a fixed 41-entry localized system-message table indexed by the server. |
| CommandManager.cs | 423 | `-command` registry: `Dictionary<string, Action<string[]>>`. Registers ~20 built-ins in `Initialize()`, including the fork's `housedump` and `nearby` diagnostics. |
| CoolDownBarManager.cs | 78 | Listens to `EventSink.MessageReceived`, spawns `CoolDownBar` gumps from profile-configured trigger strings. Fixed 15-slot static array. |
| DelayedObjectClickManager.cs | 105 | Single-slot deferred single-click / popup-menu. Polled from `GameScene.Update`. |
| DressAgentManager.cs | 460 | Named dress/undress sets persisted to `dress_configs.json`; also scans every other character's profile dir. Equips via `MoveItemQueue` or KR equip/unequip packets. |
| EffectManager.cs | 260 | `LinkedObject` list of `GameEffect`s. `CreateEffect` builds Moving/Drag/Lightning/Fixed effects from the 0x70/0xC0/0xC7 packets; `Update()` ticks and culls by `World.ClientViewRange`. |
| ForcedTooltipManager.cs | 61 | Pre-tooltip-era name synthesis: sends single-clicks and folds the replies into `World.OPL`. |
| GlobalActionCooldown.cs | 18 | One static `nextActionTime`; cooldown length read live from `Profile.MoveMultiObjectDelay`. |
| GridContainerSaveData.cs | 373 | Grid-container layout persistence (`grid_containers.json` + 3 rotating backups), 120-day inactive pruning, one-time XML→JSON migration. |
| HideHudManager.cs | 72 | One static `isVisible` bool; walks `UIManager.Gumps` and flips `IsVisible` on gump types selected by a 64-bit flag mask. |
| HouseCustomizationManager.cs | 2160 | House-design mode. Static parsed tables from `walls.txt`/`floors.txt`/etc; per-house instance validates and previews placement, sends `Send_CustomHouseAdd*`/`Delete*`. |
| HouseManager.cs | 304 | `Dictionary<uint, House>` of loaded houses plus the fork's `_footprints` table of bounds remembered for the whole session. |
| InfoBarManager.cs | 251 | List of `InfoBarItem` (label + var enum + hue) persisted to `infobar.xml`. Two var enums, one for Outlands. |
| JournalManager.cs | 169 | Static `Deque<JournalEntry>` capped at `MAX_JOURNAL_HISTORY_COUNT`, recycling the evicted entry object. Optional per-session journal file, keeps newest 100 logs. |
| MacroManager.cs | 3099 | Macro storage (`LinkedObject` list of `Macro`, each a `LinkedObject` list of `MacroObject`), `macros.xml` load/save, and the giant `Process()` interpreter. |
| MessageEventArgs.cs | 90 | Immutable payload for `EventSink.MessageReceived`. `Affix` is declared but never assigned. |
| MobileStatusRequestQueue.cs | 41 | `ConcurrentQueue<uint>` drained by a background `Task` that sends a status request every 1000 ms. |
| MusicDiagnostics.cs | 353 | Fork diagnostic. Appends a tab-separated line per music event to `Data/musiclog.txt` under a lock; tracks distance/time from the last server music packet. |
| NameOverHeadManager.cs | 524 | Name-overhead filter options (flags enum), `nameoverhead.xml` persistence, hotkey hold-to-show state. |
| OrganizerAgent.cs | 439 | Named item-moving configs (`OrganizerConfig.json`), registers `-organize`/`-organizer`/`-organizerlist`, enqueues moves on `MoveItemQueue`. |
| Season.cs | 42 | `enum Season { Spring, Summer, Fall, Winter, Desolation }`. |
| SimpleAccountManager.cs | 26 | Lists account folder names under `Data/Profiles`. Hardcoded to `CUOEnviroment.ExecutablePath`, ignores `Settings.ProfilesPath`. |
| SpellBarManager.cs | 331 | Static spell-bar rows + hotkey/controller bindings, `SpellBar.json` + `SpellBarSettings.json`, preset import/export. |
| Stitchin.cs | 146 | Parses `stitchin.def` into command tokens and then discards every result. Effectively dead. |
| TextHistoryManager.cs | 81 | Static 200-entry command/text history with prefix autocomplete. |
| TileMarkerManager.cs | 170 | Persistent per-(x,y,map) tile hue markers (`TileMarkers.json`), migrates a legacy `BinaryFormatter` .bin, writes hue straight onto live `Land`/`Static` objects. |
| ToolTipOverrideManager.cs | 484 | `ToolTipOverrideData` — profile-backed parallel-list storage of tooltip rewrite rules, plus the whole tooltip text-building pipeline. |
| UseItemQueue.cs | 101 | Deque of serials double-clicked one per `GlobalActionCooldown` window. |
| WorldMapEntityManager.cs | 282 | Party/guild pins for the world map: `Dictionary<uint, WMapEntity>` with 1000 ms staleness expiry, polls the server every 250 ms. |

## Types

| name | file:line | responsibility |
| --- | --- | --- |
| `AnchorManager` | AnchorManager.cs:43 | Owns `reverseMap`; drop/detach/dispose of anchored gumps. |
| `AnchorManager.AnchorGroup` | AnchorManager.cs:293 | One snapped cluster; `AnchorableGump[,] controlMatrix`, resize/save/move. |
| `AnonMetrics` | AnonMetrics.cs:8 | Static one-shot login telemetry. Global namespace. |
| `Aura` | AuraManager.cs:42 | Owns the circle `Texture2D` and a lazy `BlendState`. |
| `AuraManager` | AuraManager.cs:106 | Static singleton `_aura`, `IsEnabled` from `Profile.AuraUnderFeetType`, `ToggleVisibility`. |
| `BandageManager` | BandageManager.cs:11 | Singleton auto-heal agent; all config read live off `ProfileManager.CurrentProfile`. |
| `BuySellAgent` | BuySellAgent.cs:13 | Static `Instance` created in `Load()`, nulled in `Unload()`. |
| `BuySellItemConfig` | BuySellAgent.cs:279 | graphic/hue/max/restock rule; `Hue == ushort.MaxValue` = any. |
| `VendorSellInfo` / `VendorSellItemData` | BuySellAgent.cs:293 / :302 | Per-vendor accumulation of the multi-packet sell list. |
| `ChatManager` | ChatManager.cs:38 | Static channel dict + message table. |
| `CommandManager` | CommandManager.cs:51 | Static command registry. |
| `CoolDownBarManager` | CoolDownBarManager.cs:8 | Instance ctor hooks `EventSink.MessageReceived`; static 15-slot bar array. |
| `DelayedObjectClickManager` | DelayedObjectClickManager.cs:38 | Static single-slot timer. |
| `DressAgentManager` | DressAgentManager.cs:16 | Singleton; `CurrentPlayerConfigs` + `OtherCharacterConfigs`. |
| `DressConfig` / `DressItem` | DressAgentManager.cs:435 / :446 | Serialized dress set; items stored **by serial**. |
| `DressAgentJsonContext` | DressAgentManager.cs:457 | Source-generated serializer context. |
| `EffectManager` | EffectManager.cs:40 | `LinkedObject` head of all live effects. |
| `ForcedTooltipManager` | ForcedTooltipManager.cs:6 | Static; `_requestedSingleClick` serial→expiry map. |
| `GlobalActionCooldown` | GlobalActionCooldown.cs:5 | Static gate shared by `UseItemQueue` and others. |
| `GridContainerSaveData` | GridContainerSaveData.cs:15 | Lazy singleton with `Reset()`; `_entries` serial→entry. |
| `GridContainerEntry` / `GridContainerSlotEntry` | GridContainerSaveData.cs:288 / :352 | Persisted container geometry + per-item slot/lock. |
| `HideHudManager` | HideHudManager.cs:10 | Static toggle. |
| `CustomBuildObject` | HouseCustomizationManager.cs:46 | struct: graphic + x/y/z offset in a 10-slot placement array. |
| `HouseCustomizationManager` | HouseCustomizationManager.cs:58 | Per-house design session; static parsed data tables. |
| `CUSTOM_HOUSE_*` enums | HouseCustomizationManager.cs:2100-2159 | Gump state, floor vision state, build type, multi-object flags, validation flags. |
| `HouseManager` | HouseManager.cs:40 | `_houses` + fork `_footprints`. |
| `InfoBarManager` / `InfoBarItem` | InfoBarManager.cs:44 / :216 | Info-bar list and one entry. |
| `InfoBarVars` / `InfoBarVarsOutlands` | InfoBarManager.cs:158 / :187 | Two parallel var enums selected by `CUOEnviroment.IsOutlands`. |
| `JournalManager` / `JournalEntry` | JournalManager.cs:43 / :154 | Journal ring buffer and entry (mutable, reused). |
| `MacroManager` | MacroManager.cs:55 | Macro store + interpreter. |
| `Macro` | MacroManager.cs:2210 | One named macro: key/mouse/wheel/controller binding + `LinkedObject` action list. |
| `MacroObject` / `MacroObjectString` | MacroManager.cs:2613 / :2684 | One action; the string variant carries text. |
| `MacroType` / `MacroSubType` | MacroManager.cs:2699 / :2803 | ~100 macro codes and ~350 subcodes; subcode ranges are positional. |
| `MessageEventArgs` | MessageEventArgs.cs:39 | Message payload. |
| `MobileStatusRequestQueue` | MobileStatusRequestQueue.cs:10 | Lazy singleton with a background drain task. |
| `MusicDiagnostics` | MusicDiagnostics.cs:22 | Static file logger. |
| `NameOverheadOptions` | NameOverHeadManager.cs:50 | 22-bit `[Flags]` filter enum. |
| `NameOverHeadManager` | NameOverHeadManager.cs:91 | Static; options list, active flags, hotkey state, gump handle. |
| `NameOverheadOption` | NameOverHeadManager.cs:458 | One named preset + hotkey. |
| `OrganizerAgent` | OrganizerAgent.cs:16 | Static `Instance`; config list + command registration. |
| `OrganizerConfig` / `OrganizerItemConfig` | OrganizerAgent.cs:403 / :427 | Source/dest container serials and per-graphic amounts. |
| `Season` | Season.cs:35 | Enum. |
| `SimpleAccountManager` | SimpleAccountManager.cs:6 | Static folder listing. |
| `SpellBarManager` | SpellBarManager.cs:15 | All-static spell bar state. |
| `SpellBarRow` / `SpellBarSettings` | SpellBarManager.cs:275 / :312 | 10 slots per row; hotkeys/mods/controller buttons. |
| `Stitchin` | Stitchin.cs:11 | Parser with empty command bodies. |
| `TextHistoryManager` | TextHistoryManager.cs:7 | Static history list. |
| `TileLocation` / `TileMarkerEntry` | TileMarkerManager.cs:14 / :29 | Record-struct key and serialized pair. |
| `TileMarkerManager` | TileMarkerManager.cs:41 | Singleton (ctor loads immediately). |
| `ToolTipOverrideData` | ToolTipOverrideManager.cs:18 | Rule record **and** the static tooltip pipeline (`ProcessTooltipText`). |
| `UseItemQueue` | UseItemQueue.cs:38 | Deque of serials; `Instance` set by the ctor, not lazily. |
| `WMapEntity` | WorldMapEntityManager.cs:44 | One map pin; static `_mobileNameCache` never cleared. |
| `WorldMapEntityManager` | WorldMapEntityManager.cs:78 | Pin table + server polling. |

## State

Mutable / static / global state owned in this partition:

- AnchorManager.cs:69 — `readonly Dictionary<AnchorableGump, AnchorGroup> reverseMap` (instance, one per `UIManager.AnchorManager`).
- AnchorManager.cs:45,53,61 — static readonly `_anchorTriangles` / `_anchorDirectionMatrix` / `_anchorMultiplierMatrix` (`_anchorTriangles`, `_anchorMultiplierMatrix`, and `IsPointInPolygon` are unused).
- AnchorManager.cs:295-296 — `AnchorableGump[,] controlMatrix`, `int updateCount` (re-entrancy guard on `UpdateLocation`).
- AnonMetrics.cs:14,16 — `static bool MetricsEnabled`, `static bool _metricsSent`.
- AuraManager.cs:44 — `static Lazy<BlendState> _blend`; AuraManager.cs:108 `static readonly Aura _aura = new Aura(30)` (allocates a GPU texture during static init); AuraManager.cs:110 `_saveAuraUnderFeetType`.
- BandageManager.cs:13 — `static BandageManager Instance` eagerly constructed at type init (ctor subscribes to three `EventSink` events).
- BandageManager.cs:15,16,30 — `nextBandageTime`, `bandagingBuffSetTime`, `HasBandagingBuff`.
- BuySellAgent.cs:15,20,21,23 — `static Instance`, `sellItems`, `buyItems`, `Dictionary<uint, VendorSellInfo> sellPackets` keyed by vendor serial.
- ChatManager.cs:40-42 — `static readonly Dictionary<string, ChatChannel> Channels`, `ChatStatus ChatIsEnabled`, `string CurrentChannelName`.
- CommandManager.cs:53 — `static readonly Dictionary<string, Action<string[]>> _commands`.
- CoolDownBarManager.cs:11 — `static CoolDownBar[] coolDownBars` (15 slots, never shrunk).
- DelayedObjectClickManager.cs:40-46 — static `Serial`, `IsEnabled`, `Timer`, `X`, `Y`, `LastMouseX`, `LastMouseY`.
- DressAgentManager.cs:18,20,21,22 — `static Instance`, `CurrentPlayerConfigs`, `OtherCharacterConfigs`, `IsLoaded`.
- EffectManager.cs — inherits `LinkedObject.Items`, the head of the effect list.
- ForcedTooltipManager.cs:10 — `static Dictionary<uint, long> _requestedSingleClick`; :11 `DELAY = 500` ms; :12 `UPDATE_DELAY = 1500` ms.
- GlobalActionCooldown.cs:7 — `static long nextActionTime`.
- GridContainerSaveData.cs:17,28 — `static _instance`, `Dictionary<uint, GridContainerEntry> _entries`; :26 `INACTIVE_CUTOFF = 120 days`.
- HideHudManager.cs:12 — `static bool isVisible` (single global for all flag groups).
- HouseCustomizationManager.cs:60-67 — eight `static readonly List<...>` tables (`Walls`, `Floors`, `Doors`, `Miscs`, `Stairs`, `Teleports`, `Roofs`, `ObjectsInfo`) filled by the static ctor from UO .txt files.
- HouseCustomizationManager.cs:95-106 — instance state: `Category`, `MaxPage`, `CurrentFloor`, `FloorCount`, `RoofZ`, `MinHouseZ`, `Components`, `Fixtures`, `MaxComponets`, `MaxFixtures`, `Erasing`, `SeekTile`, `ShowWindow`, `CombinedStair`, `FloorVisionState[4]`, `SelectedGraphic`, `StartPos`/`EndPos`, `State`.
- HouseManager.cs:42 — `Dictionary<uint, House> _houses`; :68 `Dictionary<uint, Rectangle> _footprints` (fork; grows for the whole session, never pruned).
- InfoBarManager.cs:46 — `readonly List<InfoBarItem> infoBarItems`.
- JournalManager.cs:48 — `static Deque<JournalEntry> Entries` (**static**, survives character switch — `Clear()` at :147 deliberately does not clear it); :45 `_fileWriter`, :46 `_writerHasException`.
- MacroManager.cs:57 — `static readonly string[] MacroNames`; :58 `uint[] _itemsInHand` (2 slots); :59 `_lastMacro`; :60 `_nextTimer`; :82 `WaitForTargetTimer`; :84 `WaitingBandageTarget`.
- MobileStatusRequestQueue.cs:12,13,14 — `ConcurrentQueue<uint> requestedSerials`, `static instance`, `Task queueProccessor`.
- MusicDiagnostics.cs:28 — `static readonly object _writeLock`; :34-41 `_anchorX`, `_anchorY`, `_anchorMap`, `_anchorTime`, `_lastZoneBand`, `_lastMapIndex`, `_bannerWritten`; :24 `ZONE_DISTANCE = 20`.
- NameOverHeadManager.cs:93-95 — `static _gump`, `_lastKeySym`, `_lastKeyMod`; :103 `ActiveOverheadOptions`; :111 `IsTemporarilyShowing`; :114 `static List<NameOverheadOption> Options`; :116 `Search`.
- OrganizerAgent.cs:18,20 — `static Instance`, `List<OrganizerConfig> OrganizerConfigs`.
- SimpleAccountManager.cs:8 — `static string accountPath` computed at type init.
- SpellBarManager.cs:17,18,20-25 — `static List<SpellBarRow> SpellBarRows`, `static int CurrentRow`, `enabled`, `charPath`, `fullSavePath`, `presetPath`, `spellBarSettings`. All static; not per-character except by `Load()` overwrite.
- TextHistoryManager.cs:10 — `static readonly List<string> commandHistory`, cap 200 (:9).
- TileMarkerManager.cs:43,45 — `static Instance` (ctor calls `Load()` at type init), `Dictionary<TileLocation, ushort> markedTiles`.
- ToolTipOverrideManager.cs — no own field state; every read/write goes through the seven parallel `ProfileManager.CurrentProfile.ToolTipOverride_*` lists (lines 53-79, 95-121, 130-149).
- UseItemQueue.cs:40,43,44 — `static Instance`, `_isEmpty`, `Deque<uint> _actions`.
- WorldMapEntityManager.cs:46 — `static Dictionary<uint, string> _mobileNameCache` (static, never cleared, not reset by `Clear()`); :80-83 `_ackReceived`, `_lastUpdate`, `_lastPacketSend`, `_lastPacketRecv`, `_toRemove`, `_corpse`; :96 `Dictionary<uint, WMapEntity> Entities`.

## Timing

Per frame (from `GameScene.Update`, `Game/Scenes/GameScene.cs`):
- `DelayedObjectClickManager.Update()` — GameScene.cs:918. Fires when `Time.Ticks >= Timer`.
- `_useItemQueue.ClearCorpses()` — GameScene.cs:927, only when `CorpseOpenOptions` 1/2/3 conditions hold.
- `UseItemQueue.Update()` — GameScene.cs:930. Pops **one** serial per frame and only when `GlobalActionCooldown.IsOnCooldown == false`; cooldown length = `Profile.MoveMultiObjectDelay` ms.
- `Macros.Update()` — GameScene.cs:977. Drains `_lastMacro` chain until `Process()` returns 1 (break, retry next frame) or 2 (stop). `_nextTimer` gates the whole chain.
- Profile autosave — GameScene.cs:983: every 3,600,000 ms (1 hour).

Per frame (from `World.Update`, `Game/World.cs`):
- `_effectManager.Update()` — World.cs:479. Walks the effect linked list, destroys any effect with `Distance > World.ClientViewRange`.
- `WMapManager.RemoveUnupdatedWEntity()` — World.cs:481. Self-throttled to once per 1000 ms (`_lastUpdate = Time.Ticks + 1000`); drops pins whose `LastUpdate < Time.Ticks - 1000`; drops `_corpse` after 1000 ms.

Per frame (render): `AuraManager.Draw` — called from `Game/GameObjects/Views/MobileView.cs:86`.

Per frame (map chunk build): `TileMarkerManager.Instance.IsTileMarked` — `Game/Map/Chunk.cs:110` and `:140`, once per land/static as chunks are created.

On input:
- `Macros.Update()` also fires directly from key/mouse handlers — `GameSceneInputHandler.cs:1724`, `:1815`, and `MacroButtonGump.cs:214`.
- `SpellBarManager.KeyPress` — `GameSceneInputHandler.cs:1523`; `SpellBarManager.ControllerInput` — `:1788`.
- `NameOverHeadManager.RegisterKeyDown/RegisterKeyUp` — hold-to-show; `_lastKeySym` de-dupes key repeat.
- `HouseCustomizationManager.OnTargetWorld` — `GameScene.cs:1081`, `GameSceneInputHandler.cs:529`. Each call ends with a full `GenerateFloorPlace()` + `gump.Update()`.

Per packet:
- `EffectManager.CreateEffect` — from the graphic-effect packets.
- `BuySellAgent.HandleBuyPacket` / `HandleSellPacket` / `HandleSellPacketFinished` — vendor packets; the sell path is multi-packet, one item per `HandleSellPacket` call, committed on `HandleSellPacketFinished`.
- `WorldMapEntityManager.AddOrUpdate(..., from_packet: true)` sets `_lastPacketRecv = Time.Ticks + 10000`; non-packet callers are ignored unless a packet arrived in the last 10 s.
- `WMapManager.RemoveUnupdatedWEntity()` also called from `Network/PacketHandlers.cs:6053`.
- `JournalManager.Add` — every message.
- `CoolDownBarManager.MessageManager_MessageReceived` — every message; **the body runs on a `Task.Factory.StartNew`, off the frame thread**.
- `BandageManager.OnPlayerStatChanged` — every stat-change packet; `OnBuffAdded`/`OnBuffRemoved` on buff packets.

On timer / background thread:
- `MobileStatusRequestQueue` — background `Task` sends one status request per 1000 ms (`Task.Delay(1000).Wait()`, MobileStatusRequestQueue.cs:35).
- `WorldMapEntityManager.RequestServerPartyGuildInfo` — self-throttled to one send per 250 ms (WorldMapEntityManager.cs:246).
- `ForcedTooltipManager.RequestName` — 500 ms per-serial re-request window; OPL entries written with a 1500 ms revision horizon.
- `BandageManager` — `healDelayMs` default 3000 ms between heals; `MAX_BANDAGE_BUFF_AGE_MS = 15000` safety expiry on the Healing buff.
- `AnonMetrics.TrackLoginFireAndForget` — one blocking `WebClient.UploadString` on a thread-pool task, once per process.

On load / unload (`GameScene.Load` / `Unload`):
- `NameOverHeadManager.Load()` — GameScene.cs:222; `DressAgentManager.Instance.Load()` — :251; `BandageManager.Instance` touched to force construction — :253; `BuySellAgent.Load()` — :262; `OrganizerAgent.Load()` — :263; `SpellBarManager.Load()` — :265; `GridContainerSaveData.Instance.Load()` — :210.
- `SpellBarManager.Unload()` — :424; `BuySellAgent.Unload()` — :428; `OrganizerAgent.Unload()` — :429; `GridContainerSaveData.Save()` + `Reset()` — :420-421; `TileMarkerManager.Instance.Save()` — :468; `_useItemQueue.Clear()` then set null — :497-498.
- `HouseCustomizationManager` ctor — `UI/Gumps/HouseCustomizationGump.cs:63`, once per design session. Its **static** ctor parses seven UO text files on first touch of the type.

## Inbound

- `Game/Scenes/GameScene.cs` — per-frame ticks (`DelayedObjectClickManager`, `UseItemQueue`, `Macros`) and load/unload for `NameOverHeadManager`, `DressAgentManager`, `BandageManager`, `BuySellAgent`, `OrganizerAgent`, `SpellBarManager`, `GridContainerSaveData`, `TileMarkerManager`.
- `Game/World.cs:55,103,479,481` — owns `EffectManager`, `CoolDownBarManager`, `HouseManager` (`World.HouseManager`), `WMapManager`, and ticks the first and last.
- `Game/Scenes/GameSceneInputHandler.cs` — `SpellBarManager.KeyPress/ControllerInput`, `World.CustomHouseManager.OnTargetWorld`, macro dispatch.
- `Game/GameCursor.cs:291-342` — calls `HouseCustomizationManager.CanBuildHere` every frame while a design graphic is selected, to tint the cursor.
- `Game/Map/Chunk.cs:110,140` — `TileMarkerManager.IsTileMarked` during chunk construction.
- `Game/GameObjects/Views/MobileView.cs:86` — `AuraManager.Draw`.
- `Network/PacketHandlers.cs` — `EffectManager.CreateEffect`, `BuySellAgent` handlers, `WMapManager.AddOrUpdate` / `RemoveUnupdatedWEntity` (:6053), `JournalManager.Add`.
- `Game/UI/Gumps/*` — `GridContainer.cs:144` reads `GridContainerSaveData.Instance.GetContainer`; `HouseCustomizationGump.cs:63` constructs the customization manager; `ModernOptionsGump.cs:4419` calls `CoolDownBarManager.AddCoolDownBar`; `MacroButtonGump.cs:214` runs macros.
- `LegionScripting/API.cs` — `CoolDownBarManager.AddCoolDownBar` (:1091), `UseItemQueue.Instance.IsEmpty` (:3410), `TileMarkerManager.AddTile/RemoveTile` (:3499, :3513).
- `LegionScripting/Commands.cs:53` — `CoolDownBarManager.AddCoolDownBar`.
- `EventSink` — pushes into `BandageManager` (3 events), `CoolDownBarManager` (`MessageReceived`), `ToolTipOverrideData` (`PreProcessTooltip`/`PostProcessTooltip`).
- `UIManager.AnchorManager` — the single `AnchorManager` instance; `AnchorableGump` drag handlers call `DropControl`/`DetachControl`/`GetCandidateDropLocation`.
- `MacroManager.TryGetMacroManager()` (MacroManager.cs:86) is the fan-in point used by `DressAgentManager.CreateDressMacro` and `OrganizerAgent.CreateOrganizerMacroButton`.

## Outbound

- `NetClient.Socket` / `AsyncNetClient.Socket` — `Send_BuyRequest`/`Send_SellRequest` (BuySellAgent.cs:161,273), `Send_EquipMacroKR`/`Send_UnequipMacroKR` (DressAgentManager.cs:302,364), `Send_CustomHouseAddItem`/`AddRoof`/`AddStair`/`DeleteItem`/`DeleteRoof` (HouseCustomizationManager.cs:668,672,726,800,804), `Send_QueryGuildPosition`/`Send_QueryPartyPosition` (WorldMapEntityManager.cs:253,265), and ~20 sends from `MacroManager.Process` (`Send_OpenSpellBook`, `Send_InvokeVirtueRequest`, `Send_ToggleGargoyleFlying`, `Send_EquipLastWeapon`, `Send_DisarmRequest`, `Send_StunRequest`, `Send_Resync`, `Send_TargetSelectedObject`).
- `GameActions.*` — `Print`, `SingleClick`, `DoubleClick`, `DoubleClickQueued`, `OpenPopupMenu`, `Say`, `CastSpell`, `UseSkill`, `Attack`, `PickUp`/`DropItem`/`Equip`, `BandageSelf`, `RequestMobileStatus`, the whole Open*/Close* gump family.
- `TargetManager` — `SetTargeting`, `SetAutoTarget`, `SetTargetingMulti`, `Target`, `TargetLast`, `CancelTarget`, `LastTargetInfo`, `SelectedTarget`, `LastAttack`.
- `MoveItemQueue.Instance` — `Enqueue`, `EnqueueEquipSingle` (DressAgentManager.cs:335,395,406; OrganizerAgent.cs:344).
- `UIManager` — `Add`, `GetGump<T>`, `Gumps` enumeration, `MakeTopMostGump`, `AnchorManager`, `SystemChat`.
- `World` — `Items`, `Mobiles`, `Player`, `Get`, `Map`, `MapIndex`, `HouseManager`, `OPL`, `Party`, `ClientFeatures`, `ClientLockedFeatures`, `ClientViewRange`, `RangeSize`, `FindNext`/`FindNearest`.
- `ProfileManager.CurrentProfile` / `ProfileManager.ProfilePath` — read live on nearly every call in `BandageManager`, `GlobalActionCooldown`, `ToolTipOverrideData`, `NameOverHeadManager`, `AuraManager`, `BuySellAgent`, `SpellBarManager`.
- `HouseDiagnostics.DumpAll` / `LogHouseContents` / `LogHouseLetGo` — CommandManager.cs:170, HouseManager.cs:154.
- `MusicMapManager.Load()` / `.AreaCount`, `SoundsLoader.Instance.TryGetMusicData` — MusicDiagnostics.cs:163,180,298.
- `UOFileManager.GetUOFilePath` — HouseCustomizationManager.cs:71-83, Stitchin.cs:17.
- `EventSink.InvokeJournalEntryAdded` (JournalManager.cs:83), `EventSink.PreProcessTooltip` / `PostProcessTooltip` (ToolTipOverrideManager.cs:269,352).
- `LegionScripting.ScriptRecorder.Instance.RecordMount/RecordDismount` (MacroManager.cs:1134,1145), `LegionScripting.DownloadAPIPy` (CommandManager.cs:63).
- `FriendsListManager.Instance.AddFriend/RemoveFriend`, `TargetHelper.TargetObject` (MacroManager.cs:1168-1221).
- `SpellBar.Instance.ChangeRow/SetRow` (MacroManager.cs:1107-1121).
- `File`/`Directory` I/O on the frame thread: macros.xml, infobar.xml, nameoverhead.xml, grid_containers.json (+3 backups), dress_configs.json, SellAgentConfig.json, BuyAgentConfig.json, OrganizerConfig.json, SpellBar.json, SpellBarSettings.json, TileMarkers.json, musiclog.txt, journal logs.

## Hazards

- AnchorManager.cs:82-89 — the indexer's setter calls `reverseMap.Add` whenever the key is absent **or** the value is non-null; assigning a new group to a control that already has one throws `ArgumentException` instead of replacing.
- AnchorManager.cs:165,189 — `DetachControl`/`DisposeAllControls` re-evaluate `this[control]` inside the loop after having just set `this[ctrl] = null`, so `o.Value == this[control]` compares against `null` for later iterations.
- AnchorManager.cs:192 — `ctrl.Dispose()` is called while iterating a materialised list built from `reverseMap`, and `Dispose` on an `AnchorableGump` can re-enter the manager.
- AnchorManager.cs:245 — `ClosestOverlappingControl` walks `UIManager.Gumps` (a `LinkedList`) with no snapshot; a gump disposed during the walk mutates that list.
- AnchorManager.cs:419-433 — `AnchorControlAt` resizes the matrix based on `targetX/targetY` computed before the resize, then recomputes `hostPosition`; if `GetControlCoordinates` returns null after the resize the control is silently dropped, and `AddControlToMatrix` at :315 indexes without bounds checks.
- AnonMetrics.cs:24 — `async void` with a blocking synchronous `WebClient.UploadString` inside; an unobserved throw outside the inner try is unhandleable.
- AnonMetrics.cs:42-46 — JSON built by string concatenation with no escaping of `serverName`.
- AuraManager.cs:108 — `static readonly Aura _aura = new Aura(30)` creates a `Texture2D` from `Client.Game.GraphicsDevice` during static initialisation; the manager is never disposed, so the texture leaks across device resets.
- AuraManager.cs:135-145 — `ToggleVisibility` dereferences `currentProfile` without a null check even though `IsEnabled` (:116) explicitly guards for null; and toggling twice while `AuraUnderFeetType == 3` overwrites `_saveAuraUnderFeetType` with 3.
- BandageManager.cs:13 — eager static singleton whose ctor subscribes to `EventSink` events; there is no unsubscribe, so handlers persist across character switch/disconnect.
- BandageManager.cs:87 — `newHp` comes from the event payload, not from `player.Hits`; if the stat packet and `HitsMax` update out of order the percentage is computed against a stale max.
- BandageManager.cs:130 — `TargetManager.SetAutoTarget` is armed before the double-click; if the server sends an unrelated target cursor first, the auto-target consumes it.
- BuySellAgent.cs:172-180 — `GetBackpackItemCount` walks only the backpack's direct children; nested containers are not counted, so restock thresholds are computed against a partial total.
- BuySellAgent.cs:188-191 — `sellPackets` entries are only removed in `HandleSellPacketFinished`; if the finish packet never arrives (gump closed, vendor out of range) the entry leaks, and the same vendor serial reopened re-accumulates onto the stale list.
- BuySellAgent.cs:230 — `sellPackets[vendorSerial]` is indexed without a `ContainsKey` guard after the `sellItems == null` early return; a "finished" packet with no preceding item packets throws `KeyNotFoundException`.
- BuySellAgent.cs:97,186 — `ProfileManager.CurrentProfile` dereferenced without null check inside packet handlers.
- BuySellAgent.cs:33,39,82,88 — `JsonSerializer.Deserialize<T>` / `Serialize` with no generated serializer context, against the repo convention in CLAUDE.md.
- CommandManager.cs:140 — `Initialize()` reads `World.Player.Skills` at registration time; if `Initialize` ever runs before the player exists this NREs. The resulting `sortSkills` list is never used.
- CommandManager.cs:198,215 — the `nearby` command enumerates `World.Items.Values` / `World.Mobiles.Values` live with no snapshot.
- CommandManager.cs:420 — `OnHueTarget` dereferences `entity.Graphic` after an `if (entity != null)` block that does not return; a null entity NREs one line later.
- CommandManager.cs:346-354 — `setinscreen` walks `UIManager.Gumps` backwards and calls `SetInScreen()`, which can reposition/dispose gumps mid-walk.
- CoolDownBarManager.cs:20 — the whole message handler body runs on a thread-pool task and then calls `AddCoolDownBar`, which does `UIManager.Add` (UI mutation off the frame thread).
- CoolDownBarManager.cs:31,35 — `World.Player.Serial` read on that background task with no null check.
- CoolDownBarManager.cs:62,71 — the bar's Y position is derived from its slot index `i`, so a bar reusing a freed slot appears at that slot's fixed offset regardless of what is above it.
- DelayedObjectClickManager.cs:88 vs :97 — `Clear()` sets `Serial = 0xFFFFFFFF` while `Clear(uint)` sets `Serial = 0`; a later `Clear(0)` will therefore match and reset unrelated state.
- DressAgentManager.cs:208,214 — dress configs store item **serials**. Serials are reassigned by the server for new items, so a saved config can equip whatever now holds that serial.
- DressAgentManager.cs:85-125 — `LoadOtherCharacterConfigs` does a three-level recursive directory walk plus a file read per character, synchronously, during `GameScene.Load`.
- DressAgentManager.cs:194,215,225,231,237 — every mutation calls `Save()`, so `AddCurrentlyEquippedItems` (:250) writes the JSON file once per equipped layer.
- DressAgentManager.cs:135 — `RegisterCommands` guards on the key existing but never unregisters, so the closure captures the first `DressAgentManager` view of configs for the process lifetime.
- EffectManager.cs:44-56 — `Update` caches `f.Next` before `f.Update()`, but `f.Update()` can destroy *other* effects and unlink them; the cached `next` can then be an already-unlinked node.
- EffectManager.cs:245-259 — `Clear()` hides the base `LinkedObject.Clear` (`public new`), calls `Destroy()` on each node and then nulls `Items`; a caller holding the base type gets the base behaviour instead.
- ForcedTooltipManager.cs:10 — `_requestedSingleClick` only removes entries in `IsObjectTextRequested` (:35) when a matching text arrives after expiry; serials that are never spoken about stay in the dictionary forever.
- ForcedTooltipManager.cs:21 — compares an OPL *revision* against `Time.Ticks`; the two are only comparable because this class writes `Time.Ticks + UPDATE_DELAY` as the revision at :46/:51/:56.
- GridContainerSaveData.cs:75,99 — `Path.GetTempFileName()` creates the temp file in the system temp dir and `File.Move` across volumes throws; the catch swallows it and the main save file has already been moved to backup1 by that point (:96), so a cross-volume failure loses the current file.
- GridContainerSaveData.cs:133-138 — on a partially-corrupt file `_entries` is replaced only after a successful deserialize, but `entries` is not null-checked before the `foreach`.
- GridContainerSaveData.cs:239,246 — `ConvertOldXMLSave` returning `true` skips `Load()` entirely, so an existing JSON save is ignored the one time an old XML is present.
- GridContainerSaveData.cs:284 — `GetContainer` returns a **new, unregistered** `GridContainerEntry` on a miss; callers mutating it silently write to a throwaway object.
- HideHudManager.cs:12,19 — one global `isVisible` flipped for every call regardless of which flags were passed; calling `ToggleHidden` with two different flag masks desynchronises the two groups.
- HideHudManager.cs:23 — enumerates `UIManager.Gumps` live while setting `IsVisible`.
- HouseCustomizationManager.cs:69-84 — the static ctor does eight synchronous file parses and reads `World.ClientLockedFeatures` (:1996, :2029); the tables are static, so feature flags from the *first* server connection are baked in for the process lifetime.
- HouseCustomizationManager.cs:620,701,840 — `gump` from `UIManager.GetGump<HouseCustomizationGump>(Serial)` is used at :701 and :840 without a null check (only the `CombinedStair` branch is guarded by `gump.Page` bounds).
- HouseCustomizationManager.cs:675-683 — iterates `multi.ToList()` but calls `house.Components.Remove` inside; `GetMultiAt` returns a view over the same structures being mutated.
- HouseCustomizationManager.cs:750-793 — `foreach (Multi multiObject in multi)` calls `multiObject.Destroy()` inside the loop over a live `GetMultiAt` enumerable.
- HouseCustomizationManager.cs:689,691 — `CustomBuildObject[10]` is always allocated and `list.Length != 0` is always true; slots the branch did not fill stay zero-graphic and the `foreach` at :818 relies on `break` at graphic 0.
- HouseCustomizationManager.cs:2092 — `SeekGraphicInCustomHouseObjectList` returns `(i, graphic)` — the second element is the graphic, not an index, yet callers treat both as indices (e.g. `Stairs[res1]` at :924 is guarded by `res1 >= Stairs.Count` at :911 but `ObjectsInfo[infoCheck1]` at :1575/:1695/:1866 is not).
- HouseCustomizationManager.cs:1858 — `validatedFloors` is allocated **inside** the `foreach`, so the list passed to `ValidateItemPlace` at :1900 is empty on every iteration and the accumulated points are discarded.
- HouseManager.cs:68,102 — `_footprints` never has entries removed; every house visited in a session is retained, and `IsInsideKnownHouse` (:122) is a linear scan of it.
- HouseManager.cs:129-137 — `IsInsideKnownHouse` ignores the map index, so a footprint on Trammel spares objects at the same x/y on Felucca.
- HouseManager.cs:196 — `IsHouseInRange` returns `true` when the multi item is gone (:189), so `TryToRemove` will not remove a house whose item has already been destroyed.
- InfoBarManager.cs:234 — `InfoBarItem(XmlElement)` casts the saved int straight to `InfoBarVars`; the same file loaded under Outlands is reinterpreted against `InfoBarVarsOutlands`, which has different members at the same ordinals (index 8+ diverge).
- InfoBarManager.cs:234-235 — `int.Parse`/`ushort.Parse` with no try, inside a load path whose only try covers `doc.Load`.
- JournalManager.cs:48,55 — `Entries` is static and `JournalEntry` objects are **recycled**: at capacity the front entry is removed and its fields overwritten, so any gump or script holding a reference to an old entry sees it mutate into a new message. `Clear()` (:147) deliberately does not clear `Entries`, so the journal carries across character switches.
- JournalManager.cs:100 — `_fileWriter.WriteLine` with `AutoFlush = true` is a synchronous disk write on the message path (which runs on the frame/packet thread).
- MacroManager.cs:411-430 — `Update()` loops until `Process()` returns 1 or 2; a macro chain of only result-0 actions with a `Next` cycle would spin the frame thread.
- MacroManager.cs:425 — `_lastMacro = (MacroObject)_lastMacro?.Next` reads `Next` after `Process` may have run arbitrary code (including macro edits from `ClientCommand`).
- MacroManager.cs:1412-1447 — `ArmDisarm` stores item serials in `_itemsInHand`; the entry is cleared only on the re-equip path, so a serial that has been destroyed or reassigned is picked up blindly at :1414.
- MacroManager.cs:1513 — `CloseGump` materialises with `.ToList()` then disposes; but MacroManager.cs:2050, :2063, :2082, :2091 dispose gumps while iterating the lazy `UIManager.Gumps.OfType<...>()` / `.Where(...)` sequences with no `ToList()`.
- MacroManager.cs:1183,1212 — inside the `else` branch reached when `targeted` is null, `targeted.Serial` is dereferenced.
- MacroManager.cs:139 — `Path.Combine(Path.GetTempPath(), Path.GetTempFileName())` combines a temp dir with an already-absolute temp path; the result is the absolute path, and the file `GetTempFileName` created is left behind on every save.
- MacroManager.cs:170-172 — save deletes the real `macros.xml` before moving the temp file in; a failure between the two loses all macros.
- MacroManager.cs:2367 — `ushort.TryParse(xml.GetAttribute("graphic"), ...)` on the string written at :2313, which writes `Graphic.ToString()` for a `ushort?` — i.e. the nullable's `ToString`, so the round-trip depends on that formatting.
- MacroManager.cs:2357-2360,2393-2394,2427 — `int.Parse`/`bool.Parse` on macro XML attributes with no try; one hand-edited macros.xml aborts the whole load (the outer catch is only around `doc.Load`).
- MacroManager.cs:2572-2574 — `countInitial`/`countFinal` computed then `count` hardcoded to `countInitial + 33 + 43`; `countFinal` is unused.
- MessageEventArgs.cs:85 — `Affix` has no setter and is never assigned; always null. `AffixType` is hardcoded to `None` at :61.
- MobileStatusRequestQueue.cs:28 — the "is a processor already running" test uses `queueProccessor.Status.Equals(TaskStatus.Running)`, which is false for a task in `WaitingForActivation`/`WaitingToRun`; two or more drain tasks can race on the same queue.
- MobileStatusRequestQueue.cs:33-35 — `GameActions.RequestMobileStatus` and `GameActions.Print` are called from a background thread; `Print` mutates the journal and UI.
- MobileStatusRequestQueue.cs:34 — a debug `Print($"Processing {serial}")` is left in the shipping path.
- MusicDiagnostics.cs:280,324 — every logged event opens the file, and `File.AppendAllText` is called two to three times per event; on the frame/packet thread when `LogMusicIndices` is on.
- MusicDiagnostics.cs:163 — `WriteSessionBanner` calls `MusicMapManager.Load()` from inside the write lock, on whatever thread raised the first event (possibly FNA's audio callback).
- NameOverHeadManager.cs:99,107 — `LastActiveNameOverheadOption` and `IsPermaToggled` dereference `ProfileManager.CurrentProfile` with no null check.
- NameOverHeadManager.cs:368 — `Save()` rethrows after cleanup; the caller at profile-save time gets the exception.
- NameOverHeadManager.cs:346-350 — delete-then-move, not atomic despite the comment at :345.
- NameOverHeadManager.cs:432-439 — `RegisterKeyUp` only compares `key.sym`, not the modifier; releasing a modifier first leaves `IsTemporarilyShowing` true until the base key is released.
- NameOverHeadManager.cs:518-522 — `int.Parse`/`bool.Parse` on XML with no try.
- OrganizerAgent.cs:34-36 — `oldPath` is the **directory** `<exe>/Data`; `File.Exists` on a directory is false so the migration never runs, but if it ever did it would move the Data directory onto the config path.
- OrganizerAgent.cs:399 — `[JsonSerializable(typeof(List<OrganizerAgent>))]` declares the *manager* type, while `Load`/`Save` (:38, :105) use `OrganizerAgentContext.Default.ListOrganizerConfig` — the context is declared for the wrong type.
- OrganizerAgent.cs:280,312 — walks `sourceCont.Items` by `Next` while `MoveItemQueue` enqueues moves that will relink that same list; the enqueue happens after the walk (:342) but items can be removed by the server mid-walk.
- OrganizerAgent.cs:314-336 — the destination-differs branch has no `break` after a match (unlike the same-container branch at :293), so one item matching several configs is queued once per matching config.
- OrganizerAgent.cs:50,73,96 — commands registered only if absent, and `Unload` unregisters unconditionally; a second `Load` without an intervening `Unload` silently keeps the old closure over the previous `Instance`.
- SimpleAccountManager.cs:8 — hardcodes `<exe>/Data/Profiles` and ignores `Settings.GlobalSettings.ProfilesPath`, which `DressAgentManager.LoadOtherCharacterConfigs` (:71-78) does honour.
- SpellBarManager.cs:40,46,54,69 — `spellBarSettings` dereferenced with no null check in `GetControllerButtonsName`, `GetKetNames`, `ControllerInput`, `KeyPress`; it is only guaranteed non-null after `Load()` (:250).
- SpellBarManager.cs:46,74,77 — `GetKetNames` indexes `HotKeys[slot]`/`KeyMod[slot]` with no bounds check; `KeyPress` bounds-checks `HotKeys` (:74) but then indexes `KeyMod[i]` (:78) unchecked.
- SpellBarManager.cs:62 — `ControllerInput` checks `ControllerButtons.Length <= 0` but then indexes `[i]` for i up to 9.
- SpellBarManager.cs:297 — `SpellSlotIds` setter loops 0..9 over `value[i]` with no length check; a saved file with fewer ids throws during deserialization.
- SpellBarManager.cs:17,20 — all state is static and `Load()` overwrites it; nothing resets `enabled` to false on unload, and `Unload()` (:256) writes to `fullSavePath`/`charPath` captured at the last `Load`.
- SpellBarManager.cs:190 — `ImportPreset` calls `Unload()` to save, which also rewrites `SpellBarSettings.json`.
- Stitchin.cs:87-116 — every `case` body computes a value and discards it; `Read()` parses the file to no effect. `Work`/`BuildCommand` have no observable output.
- TextHistoryManager.cs:21 — `commandHistory.Contains` + `Remove` is O(n) per insert against a 200-entry list on every submitted line.
- TileMarkerManager.cs:43,47 — the singleton's constructor calls `Load()`, which reads `ProfileManager.ProfilePath` (:49); touching `TileMarkerManager.Instance` before a profile is loaded silently falls back to `CUOEnviroment.ExecutablePath` and caches nothing about which path it used.
- TileMarkerManager.cs:160-167 — `UpdateLiveTilesAt` writes `obj.Hue` directly on `Land`/`Static` objects and, on remove, sets hue to **0** rather than the tile's original hue; the original is not saved anywhere.
- TileMarkerManager.cs:124 — `BinaryFormatter.Deserialize` on a file from disk (legacy migration path).
- TileMarkerManager.cs:154 — silently no-ops when the marked tile is on a different map than the one loaded, so `AddTile` for another map updates the dictionary but nothing visible.
- ToolTipOverrideManager.cs:53-79 — seven independent `ProfileManager.CurrentProfile.ToolTipOverride_*` lists indexed in parallel; `Delete()` (:124) removes from each by index, so any list that is short is left misaligned with the others for every subsequent index.
- ToolTipOverrideManager.cs:161 — `GetAllToolTipOverrides` calls `Get(i)` per entry, and `Get` calls `data.Save()` (:86) when anything was missing — a read path that writes back into the profile.
- ToolTipOverrideManager.cs:266,304 — `GetAllToolTipOverrides()` is called once per tooltip and `FilteredOverrides` is re-enumerated **per property line**, so cost is O(properties × overrides) per tooltip build.
- ToolTipOverrideManager.cs:181,208 — `JsonSerializer.Serialize`/`Deserialize<T>` with no generated context (repo convention).
- ToolTipOverrideManager.cs:228-231 — `DecodeUnicodeEscapes` does `input.Substring(index + 2, 4)` and `int.Parse` without checking the remaining length or that the digits are hex.
- ToolTipOverrideManager.cs:33 — `Index` is get-only and set only in the ctor; `ImportOverrideSettings` (:211) constructs each imported rule with `ToolTipOverride_SearchText.Count` as index but the deserialized `imported` objects have `Index` 0 because the property has no setter.
- ToolTipOverrideManager.cs:340 — the `FormatException` catch prints and falls through with `handled` still false, so the raw line is appended *in addition to* whatever partial text was concatenated at :316.
- UseItemQueue.cs:46-49 — `Instance` is assigned by the constructor, so constructing a second `UseItemQueue` silently steals the static; `GameScene.Unload` sets its field to null (`GameScene.cs:498`) but `UseItemQueue.Instance` still points at the dead object.
- UseItemQueue.cs:87 — `ClearCorpses` calls `World.Get` per queued serial each time it runs (per frame under the corpse options).
- WorldMapEntityManager.cs:46 — `_mobileNameCache` is static and never cleared, not even by `Clear()` (:275); names from a previous server/character persist and are returned for a reused serial.
- WorldMapEntityManager.cs:74 — `GetName` uses `Name` as the `out` parameter of `TryGetValue`, so a miss assigns null to `Name` as a side effect of the lookup.
- WorldMapEntityManager.cs:196 — `_corpse.LastUpdate` is a `uint` compared against `Time.Ticks - 1000`; the subtraction is on the `long`/`uint` boundary and underflows for the first second after start.
- WorldMapEntityManager.cs:279 — `Clear()` calls `SetEnable(false)` which, because `v` is already false, does nothing but log-check; the `_lastPacketRecv`/`_lastPacketSend` throttles are not reset.
- WorldMapEntityManager.cs:92 — `Enabled` calls `UIManager.GetGump<WorldMapGump>()` (a linear gump-list scan) on every `AddOrUpdate`, i.e. per party/guild position packet.

## Fork deltas

Clearly TazUO or Holiday-Edition additions rather than stock ClassicUO (stock files carry the 2021 andreakarasho license header; most fork files do not):

**Holiday-Edition (this fork) — identifiable by the long explanatory doc comments written in prose:**
- `HouseManager._footprints` + `RememberFootprint` + `IsInsideKnownHouse` + `IsInsideLoadedHouse` + `RemoveMultiTargetHouse` (HouseManager.cs:51-140, 224-288) — session-lifetime house-bounds memory added to stop the distance cull destroying house contents. The comments cite specific capture numbers (222 items, 41 tiles, 24 minutes).
- `HouseDiagnostics.LogHouseLetGo` call in `TryToRemove` (HouseManager.cs:154).
- `MusicDiagnostics.cs` (whole file) — music event logger, gated on `Settings.GlobalSettings.LogMusicIndices`, referencing `MusicEra`, `MusicMapMode`, `IgnoreServerStopMusic`, `MusicMapManager`.
- `CommandManager` `housedump` (CommandManager.cs:168) and `nearby` (:182) commands, with the same prose comment style.
- `HouseCustomizationManager` inline `//# HOUSE FIXES` / `//HOUSE FIXES` markers at :92, :271, :582, :592, :684, :1428 — `GenerateFloorPlace()` moved into the ctor, `house.Components.Where(...)` replaced by `house.GetMultiAt(...)`, outer-tile hue forced to 161.
- `BandageManager` `MAX_BANDAGE_BUFF_AGE_MS` / `IsBandagingBuffActive` and the "always honor the minimum time" gate (BandageManager.cs:18-20, 56-60, 98-107) — the comments describe a duplicate-bandage fix.

**TazUO (upstream fork of ClassicUO):**
- Agents: `BuySellAgent.cs`, `DressAgentManager.cs`, `OrganizerAgent.cs`, `BandageManager.cs`.
- Gump/HUD systems: `GridContainerSaveData.cs`, `HideHudManager.cs`, `CoolDownBarManager.cs`, `SpellBarManager.cs`, `NameOverHeadManager.cs` (options/hotkeys), `TileMarkerManager.cs`, `ToolTipOverrideManager.cs`, `ForcedTooltipManager.cs`.
- `TextHistoryManager.cs`, `SimpleAccountManager.cs`, `MobileStatusRequestQueue.cs`, `GlobalActionCooldown.cs`, `AnonMetrics.cs` (posts to `metrics.tazuo.org`).
- `MacroType` entries beyond stock: `ToggleGump`, `ToggleDurabilityGump`, `ShowNearbyItems`, `ToggleNearbyLootGump`, `ToggleLegionScripting`, `SetSpellBarRow`, `SpellBarRowUp/Down`, `Dismount`, `Mount`, `SetMount`, `ToggleHouses`, `ToggleHudVisible`, `Resync`, `AddFriend`, `RemoveFriend`, `ToggleHotkeys`, `ClientCommand`, `UseCounterBar`, `RazorMacro` (MacroManager.cs:2699-2801) and their handlers.
- `Macro.HideLabel`/`Hue`/`Graphic`/`Scale` and controller-button binding (MacroManager.cs:2244-2264, 2335-2343, 2446-2462).
- `CommandManager` registrations for `sb`, `updateapi`, `colorpicker`, `cast`, `skill`, `version`, `marktile`, `radius`, `optlink`, `genspelldef`, `setinscreen`, `updatedebug`, `artbrowser`, `animbrowser`, `paperdoll`, `rain`.
- `JournalFilterManager` filtering and `EventSink.InvokeJournalEntryAdded` in `JournalManager.Add` (JournalManager.cs:52, 83); `JournalEntry.Disposed` "Added for py API usage" (:167).
- `InfoBarVarsOutlands` (InfoBarManager.cs:187) and the `CUOEnviroment.IsOutlands` switch at :60.
- `AsyncNetClient.Socket` (vs stock `NetClient.Socket`) used throughout `MacroManager` and `DressAgentManager`.
- Generated JSON serializer contexts (`DressAgentJsonContext`, `GridContainerSerializerContext`, `TileMarkerJsonContext`, `SpellBarRowsContext`, `SpellBarSettingsContext`, `OrganizerAgentContext`) — the repo convention; `BuySellAgent` and `ToolTipOverrideData` do **not** follow it.

**Stock ClassicUO, essentially unmodified:** `AnchorManager.cs`, `AuraManager.cs`, `ChatManager.cs`, `DelayedObjectClickManager.cs`, `EffectManager.cs`, `HouseCustomizationManager.cs` (apart from the HOUSE FIXES markers), `InfoBarManager.cs`, `MessageEventArgs.cs`, `Season.cs`, `Stitchin.cs`, `UseItemQueue.cs` (plus the `GlobalActionCooldown` gate), `WorldMapEntityManager.cs`.
