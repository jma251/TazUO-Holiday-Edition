# managers-a

Partition = odd-numbered files (NR%2==1) of
`src/ClassicUO.Client/Game/Managers/*.cs`. 35 files, 12,631 lines, all read in full.

All paths below are relative to `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/Managers/`
unless written out in full.

## Files

| File | Lines | Purpose |
| --- | --- | --- |
| ActiveIconsManager.cs | 66 | `ActiveSpellIconsManager` — HashSet of active buff/spell icon ids. |
| AnimatedStaticsManager.cs | 159 | Ticks animated static art by writing `ArtLoader.Instance.Entries[i].AnimOffset` in place. |
| AudioManager.cs | 1042 | Sound + music playback, war-music slot, era switching, and the fork's map-driven region music state machine. |
| AutoLootManager.cs | 578 | Corpse/ground auto-loot: match list, queue, throttled dequeue into MoveItemQueue, JSON config. |
| BoatMovingManager.cs | 461 | Client-side interpolation of multi (boat) movement and everything riding on it. |
| ChatChannel.cs | 46 | Immutable `{Name, HasPassword}` record for chat channels. |
| ChatStatus.cs | 40 | `enum ChatStatus { Disabled, Enabled, EnabledUserRequest }`. |
| ContainerManager.cs | 542 | containers.txt parse + defaults; computes next container gump X/Y. |
| CorpseManager.cs | 134 | Maps corpse serial ↔ dead-mobile serial so death animation direction can be restored. |
| DiscordManager.cs | 749 | Discord Social SDK: OAuth, lobbies, DMs, rich presence, in-game chat bridge. |
| DurabilityManager.cs | 132 | Parses "Durability X / Y" out of equipped-item OPL data. |
| EventSink.cs | 221 | Static event hub (~20 events) that everything else subscribes to. |
| FriendsListManager.cs | 196 | Per-server friends.json list keyed by serial. |
| GraphicsReplacement.cs | 125 | Graphic/hue substitution filters (mobile replacement), JSON-persisted. |
| HealthLinesManager.cs | 367 | Per-frame draw of HP bars over every mobile + target indicator. |
| HotkeysManager.cs | 692 | `HotkeyAction` enum + action table + key/mod → action binding list. |
| HouseDiagnostics.cs | 992 | Fork-only file-logging diagnostic for house contents, packets, culls, destroys. |
| IgnoreManager.cs | 140 | Ignored character-name set, XML-persisted per profile. |
| JournalFilterManager.cs | 72 | Exact-string journal message filters, JSON-persisted. |
| LastCharacterManager.cs | 141 | lastcharacter.json (account+server → character name) with CLI override. |
| MainThreadQueue.cs | 64 | ConcurrentQueue marshalling work from worker threads onto the game loop. |
| MessageManager.cs | 407 | Central speech/label routing: overhead TextObject creation, gump routing, event raising. |
| MoveItemQueue.cs | 120 | Serialised pickup/drop/equip requests, one per global cooldown tick. |
| MusicMapManager.cs | 201 | Loads Data/MusicMap.txt (1998 region rectangles → track lists); answers "what plays here". |
| ObjectPropertiesListManager.cs | 377 | OPL (tooltip) store per serial + `ItemPropertiesData` tooltip parsing/comparison. |
| PartyManager.cs | 296 | Party packet parse, 10-slot member array, party chat messages. |
| SeasonManager.cs | 719 | seasons.txt graphic/landtile substitution tables per season. |
| SkillsGroupManager.cs | 587 | Skill group tree (skillgrp.mul or defaults), XML-persisted. |
| SpellVisualRangeManager.cs | 612 | Spell indicator config, cast detection from power words, cast-timer gump. |
| TargetManager.cs | 772 | Targeting cursor state machine + target packet emission + async target helper. |
| TextRenderer.cs | 320 | Doubly-linked list of overhead `TextObject`s; collision/alpha/draw. |
| TitleBarStatsManager.cs | 137 | Writes HP/MP/SP into the OS window title. |
| UIManager.cs | 766 | Gump list, z-order, mouse/keyboard routing, drag, per-frame Update/Draw. |
| WalkerManager.cs | 197 | Walk sequence bookkeeping, fastwalk stack, deny/confirm handling. |
| WorldTextManager.cs | 161 | TextRenderer subclass + overhead damage numbers keyed by serial. |

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `ActiveSpellIconsManager` | ActiveIconsManager.cs:37 | Set of active icon ids; `Add/Remove/IsActive/Clear`. |
| `AnimatedStaticsManager` | AnimatedStaticsManager.cs:41 | `Initialize()` builds list of animated statics; `Process()` advances frames. |
| `AnimatedStaticsManager.StaticAnimationInfo` | AnimatedStaticsManager.cs:152 | struct {Time, Index, AnimIndex, IsField}. |
| `AudioManager` | AudioManager.cs:46 | Sounds list, 2-slot music (`[0]`=region, `[1]`=war), era, map music. |
| `AudioManager.MusicStatus` | AudioManager.cs:547 | Snapshot struct for the music debug overlay. |
| `AutoLootManager` | AutoLootManager.cs:16 | Singleton (`Instance`, ctor private); loot queue + config list. |
| `AutoLootManager.AutoLootConfigEntry` | AutoLootManager.cs:522 | Graphic/Hue/Regex match rule, UID = Guid. |
| `BoatMovingManager` | BoatMovingManager.cs:42 | static; `AddStep/ClearSteps/PushItemToList/Update`. |
| `BoatMovingManager.BoatStep` / `.ItemInside` | :445 / :455 | step and rider-offset structs. |
| `ChatChannel` | ChatChannel.cs:35 | readonly Name/HasPassword. |
| `ContainerManager` | ContainerManager.cs:44 | static; `Get(graphic)`, `CalculateContainerPosition`, `BuildContainerFile`. |
| `CorpseManager` | CorpseManager.cs:39 | Deque of `CorpseInfo`; `Add/Remove/Exists/GetCorpseObject`. |
| `DiscordManager` | DiscordManager.cs:20 | Singleton `Instance`; SDK client wrapper. |
| `DiscordSettings` / `ServerInfo` | DiscordManager.cs:718 / :727 | JSON-serialised settings and party-join payload. |
| `DurabilityManager` | DurabilityManager.cs:44 | IDisposable; OPL-driven durability map per equipped serial. |
| `DurabiltyProp` | DurabilityManager.cs:114 | {Serial, Durabilty, MaxDurabilty, Percentage} (sic, misspelled). |
| `EventSink` | EventSink.cs:8 | All-static event hub. |
| `OPLEventArgs`/`BuffEventArgs`/`PositionChangedArgs`/`WeatherEventArgs`/`PlayerStatChangedArgs` | EventSink.cs:150-220 | Event payloads. |
| `FriendsListManager` | FriendsListManager.cs:14 | Lazy singleton; per-server friends list. |
| `GraphicsReplacement` | GraphicsReplacement.cs:8 | static; `Replace(graphic, ref newgraphic, ref hue)` called from draw paths. |
| `HealthLinesManager` | HealthLinesManager.cs:41 | `Draw(batcher)` — full pass over `World.Mobiles`. |
| `HotKeyCombination` / `HotkeysManager` / `HotkeyAction` | HotkeysManager.cs:39 / :46 / :325 | Binding record, binding table, action enum. |
| `HouseDiagnostics` | HouseDiagnostics.cs:23 | static; buffered tab-separated log writer + a dozen Log* entry points. |
| `IgnoreManager` | IgnoreManager.cs:14 | static; `IgnoredCharsList` HashSet<string> of names. |
| `JournalFilterManager` | JournalFilterManager.cs:10 | Lazy singleton; HashSet of filtered messages. |
| `LastCharacterManager` / `LastCharacterInfo` | LastCharacterManager.cs:51 / :136 | static store + record. |
| `MainThreadQueue` | MainThreadQueue.cs:7 | static ConcurrentQueue<Action> + blocking `InvokeOnMainThread<T>`. |
| `MessageManager` | MessageManager.cs:68 | static `HandleMessage`, `CreateMessage`. |
| `AffixType` | MessageManager.cs:59 | enum. |
| `MoveItemQueue` | MoveItemQueue.cs:9 | Instance set in ctor; ConcurrentQueue<MoveRequest>. |
| `MoveItemQueue.MoveRequest` | MoveItemQueue.cs:108 | readonly struct, primary ctor. |
| `MusicMapManager` | MusicMapManager.cs:20 | static; `Load/CoversMap/TryGetTrack/BlockOf`. |
| `MusicMapManager.Area` | MusicMapManager.cs:24 | rect + z-range + track list + name. |
| `ObjectPropertiesListManager` | ObjectPropertiesListManager.cs:45 | serial → `ItemProperty`; `Add` raises OPL event. |
| `ItemProperty` | ObjectPropertiesListManager.cs:159 | Name/Data/Revision/NameCliloc. |
| `ItemPropertiesData` (+ `SinglePropertyData`) | :174 / :326 | Tooltip parse into name/number pairs, diffing between two items. |
| `PartyManager` / `PartyMember` | PartyManager.cs:43 / :254 | 10-slot party; `ParsePacket(ref StackDataReader)`. |
| `SeasonManager` | SeasonManager.cs:39 | static; 10 lookup arrays sized by ArtLoader max indices. |
| `SkillsGroup` / `SkillsGroupManager` | SkillsGroupManager.cs:46 / :187 | 60-byte skill id array per group; static `Groups` list. |
| `SpellVisualRangeManager` | SpellVisualRangeManager.cs:28 | Lazy singleton; cast state + range/cursor hue overlay. |
| `SpellVisualRangeManager.SpellRangeInfo` | :508 | per-spell config record. |
| `SpellVisualRangeManager.CastTimerProgressBar` | :533 | Gump drawing the cast bar over the player. |
| `TargetManager` | TargetManager.cs:181 | static; `IsTargeting`, `TargetingState`, `Target(...)`, `CancelTarget`. |
| `LastTargetInfo` / `AutoTargetInfo` / `MultiTargetInfo` / `CursorTarget` / `TargetType` | TargetManager.cs:106 / :159 / :82 / :52 / :74 | Target state records + enums. |
| `TargetHelper` | TargetManager.cs:691 | async `TargetAsync()` polling helper. |
| `TextRenderer` | TextRenderer.cs:42 | Itself a TextObject; head of the DLeft/DRight list. |
| `TitleBarStatsManager` / `TitleBarStatsMode` | TitleBarStatsManager.cs:7 / :131 | window title text. |
| `UIManager` | UIManager.cs:45 | static gump manager. |
| `StepInfo` / `FastWalkStack` / `WalkerManager` | WalkerManager.cs:37 / :50 / :93 | walk sequencing. |
| `WorldTextManager` | WorldTextManager.cs:40 | overhead damage dictionary on top of TextRenderer. |

## State

Static / global:

- `ContainerManager._data` (ushort→ContainerData) ContainerManager.cs:46; `X`/`Y` cursor position statics :57-58, mutated by `CalculateContainerPosition` and read by whoever creates the gump next.
- `AudioManager._appliedMusicEra` AudioManager.cs:201 — process-wide, `null` ≠ `""`.
- Audio instance state: `_currentSounds` LinkedList :51, `_currentMusic[2]` :52, `_currentMusicIndices[2]` :53, `_mapPlayedTrack` :617, `_mapTrackLoops` :618, `_lastMusicBlock`/`_lastMusicMap` :623-624, `_mapChoseSilence` :625, `_silentArea` :630, `volatile _mapTrackEnded` :634, `_endedTrack` :635, `_lastServerIndex`/`_lastServerAt` :540-541.
- `AutoLootManager.Instance` :18; `static Queue<uint> lootItems` :25 (static while the rest of the state is instance); `quickContainsLookup` :23, `recentlyLooted` :24, `nextLootTime` :29, `nextClearRecents` :30, `currentLootTotalCount` :32, `progressBarGump` :31.
- `BoatMovingManager._steps` (serial→Deque<BoatStep>) :49, `_toRemove` :50, `_items` (serial→FastList<ItemInside>) :51, `_timePacket` :53.
- `DiscordManager.Instance` :22, `TUOMETA` :29, `userHueMemory` :71, `furthestAction` :329, `unixStart` :347, `DiscordSettings` :39; instance `messageHistory` :70 (capped 75/channel), `currentLobbies` :72, `richPresenceTimer` :74, `pendingDisconnectLeaves` :76.
- `DurabilityManager.HasDurabilityData` static bool :56 mutated from an instance handler :91.
- `EventSink` — ~20 static events, EventSink.cs:13-147. Never unsubscribed globally.
- `FriendsListManager._instance` :16.
- `GraphicsReplacement.graphicChangeFilters` :10 and parallel `quickLookup` :12.
- `HouseDiagnostics._footprints` :118 (grows for the whole session, never pruned), `_lastLoggedViewRange` :27, `_lastCeilingMaxZ/_lastCeilingPlayerZ` :245-246, `_nextContentsLog`/`_lastContentsHouse`/`_lastInsideCount` :420-422, `_pktWindow`/`_pktCounts` :698-699, `_writeLock`/`_buffer`/`_nextFlush` :846-848, `_log`/`_logResolved` :895-896.
- `IgnoreManager.IgnoredCharsList` :19 — public mutable static HashSet, replaced wholesale by `ReadIgnoreList` :108.
- `JournalFilterManager._instance` :17 — nulled inside `Save()` :52.
- `LastCharacterManager.LastCharacters` :56, `LastCharacterNameOverride` :58.
- `MainThreadQueue.QueuedActions` :9.
- `MessageManager.PromptData` :70.
- `MoveItemQueue.Instance` :11 — assigned in the *instance* constructor :20, so the last constructed GameScene queue wins.
- `MusicMapManager._areas` :43, `_loaded` :44.
- `SeasonManager` — 10 static ushort[] tables :41-51, built in the static ctor :56.
- `SkillsGroupManager.Groups` :190, `_isActive` :189.
- `SpellVisualRangeManager.instance` :44; instance `spellRangeCache` :39, `spellRangeOverrideCache` :40, `spellRangePowerWordCache` :41, `isCasting` :46, `currentSpell` :47, `LastCursorTileLoc` :32 (written from LandView/StaticView draw), `saveTimer`/`saveLock`/`hasPendingChanges` :240-242.
- `TargetManager` — everything static: `_targetCursorId`/`_lastAttack` :183, `_lastDataBuffer[19]` :184 (shared scratch packet buffer), `SelectedTarget` :186, `LastTargetInfo` :227, `NextAutoTarget` :228, `MultiTargetInfo` :230, `TargetingState` :232, `IsTargeting` :234, `TargetingType` :236.
- `TargetHelper._executingSource` :693.
- `UIManager` — `_gumpPositionCache` :47, `_mouseDownControls[0xFF]` :48, `_dragOrigin` :52, `_isDraggingControl` :53, `_keyboardFocusControl`/`_lastFocus` :54, `_needSort` :55, `Gumps` LinkedList :61, `MouseOverControl` :63, `DraggingControl` :83, `SystemChat` :85, `PopupMenu` :87, `ContextMenu` :112, `UpdateTimerTotalTime`/`UpdateTimerCount`/`updateTimer` :128-131.
- `TextRenderer._bounds` :44 (collision rects, cleared only when `ProcessWorldText(true)`), `FirstNode` :51 = `this`, `DrawPointer` :52.
- `WorldTextManager._damages` :42, `_subst` :43, `_toRemoveDamages` :44.
- `WalkerManager.StepInfos[MAX_STEP_COUNT]` :101 plus ~10 loose public fields :96-106.

## Timing

Per frame (GameController.Update / GameScene.Update / Draw):

- `GameController.cs:542` → `AudioManager.Update()` — music-map state machine, volume, sound reaping.
- `GameController.cs:491` → `UIManager.Update()` (walks `Gumps`, removes disposed); `:498` `MainThreadQueue.ProcessQueue()`; `:576` `UIManager.Draw`.
- `GameController.cs:503` → `UIManager.SlowUpdate()` gated on a **500 ms** timer (`_nextSlowUpdate`).
- `GameScene.cs:914` `HouseDiagnostics.LogHouseContents()`; `:915` `_animatedStaticsManager.Process()`; `:916` `BoatMovingManager.Update()`; `:932` `AutoLootManager.Instance.Update()`; `:933` `_moveItemQueue.ProcessQueue()`.
- `World.cs:480` → `WorldTextManager.Update()`; `GameScene.cs:1490-1491` → `ProcessWorldText(true)` then `Draw`.
- `HealthLinesManager.Draw` — iterates every mobile in the world each frame.
- `SpellVisualRangeManager.ProcessHueForTile` / `LastCursorTileLoc` write — per tile per frame from LandView.cs:74 and StaticView.cs:95.

Intervals / delays:

- AnimatedStatics: `_processTime` gate; next wake floor `Time.Ticks + 250` (AnimatedStaticsManager.cs:97); per-entry delay `ITEM_EFFECT_ANIMATION_DELAY * 2` (:96) times `FrameInterval` (:122).
- Boat steps: 1000 / 500 / 250 ms for speed 0x02 / 0x03 / 0x04, `speed*10` above that (BoatMovingManager.cs:44-68).
- AutoLoot: dequeues one item per `ProfileManager.CurrentProfile.MoveMultiObjectDelay` ms (AutoLootManager.cs:304); `recentlyLooted` cleared 5000 ms after the queue drains (:30, :55, :271).
- MoveItemQueue: one request per `GlobalActionCooldown` (MoveItemQueue.cs:76, 96).
- HouseDiagnostics: contents line at most once per 1000 ms (:464), packet-rate rollup once per 1000 ms (:762), log buffer flushed at 32 KB or once per 1000 ms (:868, :966).
- Discord: `richPresenceTimer` every 30 minutes (DiscordManager.cs:306); `RunLater` spaces queued actions ≥2000 ms apart via a shared `furthestAction` cursor (:331-345); `FinalizeDisconnect` spins 10 ms × 200 ≈ 2 s on the main thread (:157-165).
- SpellVisualRange: `DelayedSave` builds a 500 ms `System.Timers.Timer` (:450).
- TargetHelper.TargetAsync polls every 250 ms (TargetManager.cs:755).

Per packet:
- `PartyManager.ParsePacket` (0xBF sub-command), `WalkerManager.ConfirmWalk/DenyWalk` (0x22/0x21), `BoatMovingManager.AddStep/PushItemToList` (multi move), `ObjectPropertiesListManager.Add` (0xD6), `MessageManager.HandleMessage` (all speech packets), `AudioManager.PlayMusic/StopMusicFromServer/NotifyServerTrack` (0x6D), `HouseDiagnostics.LogPacket` on **every** inbound and outbound packet (PacketHandlers.cs:143, NetClient.cs:304, AsyncNetClient.cs:388).

On input: `UIManager.OnMouseButtonDown/Up/DoubleClick/Wheel/Dragging`, `HotkeysManager.TryExecuteIfBinded`, `TargetManager.Target`.

On load/scene change: `ContainerManager` static ctor → `BuildContainerFile(false)`; `SeasonManager` static ctor → `LoadSeasonFile`; `SkillsGroupManager.Load` from Profile.cs:865; `IgnoreManager.Initialize` from ProfileManager.cs:67; `AutoLootManager.OnSceneLoad/OnSceneUnload`; `SpellVisualRangeManager.OnSceneLoad/OnSceneUnload` (GameScene.cs:249, 469-470); `FriendsListManager.OnSceneLoad/OnSceneUnload`.

## Inbound

- `GameController.cs` — 224 `Audio.Initialize()`, 246 `Audio?.StopMusic()`, 305 `HouseDiagnostics.Flush()`, 491/503/542/576 the per-frame calls above.
- `Game/Scenes/GameScene.cs` — 224-225 constructs/initialises `AnimatedStaticsManager`, 106 owns `MoveItemQueue`, 249/469-470 SpellVisualRange scene hooks, 444-449 audio stop/forget, 914-933 the per-frame manager calls.
- `Game/Scenes/LoginScene.cs` — 142 `Audio.PlayMusic(LoginMusicIndex, false, true)`, 170-172 stop/forget.
- `Network/PacketHandlers.cs` — party packets → `PartyManager.ParsePacket`; OPL → `ObjectPropertiesListManager.Add`; 0x6D → Audio; 621/789/1973/3579/3734/3753/3772 `TitleBarStatsManager`; 565/4950 `WorldTextManager.AddDamage`; 5061 `SpellVisualRangeManager.OnClilocReceived`; 386/1128/1131/2743/5561/6758/6802 HouseDiagnostics.
- `Game/GameActions.cs` — 70/74/250 war music; 1028/1038 `SpellVisualRangeManager.ClearCasting`.
- `Game/GameObjects/Item.cs:272, 352` — `HouseDiagnostics.LogHouseItemDestroyed` / `LogMultiRebuild`.
- `Game/GameObjects/GameObject.cs:439` — `World.WorldTextManager.AddMessage`.
- `Game/World.cs` — 99 owns `WorldTextManager`, 440 `HouseDiagnostics.LogItemCulled`, 480 update, 959 clear.
- Gumps: `MultiItemMoveGump`, `GridContainer`, `NearbyItems`, `NearbyLootGump` → `MoveItemQueue`; `AssistantGump` → `TitleBarStatsManager.ForceUpdate`, `SpellVisualRangeManager.DelayedSave`; `ModernOptionsGump:3522` → `LoadFromString`.
- `LegionScripting/API.cs` — 337/366/496/568 MoveItemQueue, 468/3399 `MainThreadQueue.InvokeOnMainThread` (script thread → game loop).
- Views: `LandView.cs:74-78`, `StaticView.cs:95-100` → SpellVisualRangeManager hue processing.

## Outbound

- `NetClient.Socket` / `AsyncNetClient.Socket`: `Send_MultiBoatMoveRequest` (BoatMovingManager.cs:73), `Send_TargetObject`/`Send_TargetXYZ`/`Send_TargetCancel`/raw `Send(_lastDataBuffer)` (TargetManager.cs:312, 459, 636, 678), `Send_Resync` (WalkerManager.cs:177), `Send_PickUpRequest`/`Send_EquipRequest` (MoveItemQueue.cs:85, 93).
- `GameActions`: `CastSpell` (all of HotkeysManager), `Print`, `DropItem`, `GrabItem`, `RequestMobileStatus`.
- `UIManager.Add` / `GetGump<T>` from AutoLootManager, DurabilityManager, IgnoreManager, PartyManager, TargetManager, SkillsGroupManager, ContainerManager.
- `World.*`: `World.Items`, `World.Mobiles`, `World.Get`, `World.OPL`, `World.HouseManager`, `World.Party`, `World.Player`, `World.RangeSize`, `World.CorpseManager`, `World.CustomHouseManager`.
- `Client.Game.*`: `Sounds.GetSound/GetMusic/SetMusicEra`, `Gumps.GetGump`, `Animations.GetAnimationDimensions`, `Scene.Camera`, `GameCursor.ItemHold`, `SetWindowTitle`.
- Assets: `AnimDataLoader`, `ArtLoader.Entries` (written), `TileDataLoader`, `SkillsLoader`, `SoundsLoader.LoadMusicConfig`, `UOFileManager`.
- `EventSink.Invoke*` from MessageManager (:98, :303), ObjectPropertiesListManager (:63), SpellVisualRangeManager (:102).
- `MessageManager.HandleMessage` from PartyManager (:198) and DiscordManager (:560).
- `MusicDiagnostics.*` from AudioManager throughout.
- `LegionScripting.ScriptRecorder.Instance.RecordTarget*` from TargetManager (:351, :586).
- Filesystem/JSON: AutoLoot.json, friends.json, journal_filters.json, ignore_list.xml, skillsgroups.xml, lastcharacter.json, SpellVisualRange.json, DiscordSettings.json, .dratoken, MobileReplacementFilter.json, containers.txt, seasons.txt, MusicMap.txt, houselog.txt.

## Hazards

- AutoLootManager.cs:25 — `lootItems` is `static` while `quickContainsLookup`, `recentlyLooted` and `currentLootTotalCount` are instance fields on the same singleton.
- AutoLootManager.cs:51 — an item is added to `recentlyLooted` *and* `quickContainsLookup` at enqueue; `quickContainsLookup.Remove` happens on dequeue (:282) but `recentlyLooted` is only cleared wholesale 5 s after the queue empties (:270).
- AutoLootManager.cs:217 — iterates `World.Items.Values` on every player position change while `EnableScavenger` is on; `CheckAndLoot` → `LootItem` mutates only the loot queue, not the dictionary.
- AutoLootManager.cs:262 vs :302 — `Update` returns while an item is held, but a `nextLootTime` was already consumed for the previous dequeue; a dequeued serial whose item is out of range is dropped from the queue and from `quickContainsLookup` without being looted (:295-300 returns after the dequeue).
- AutoLootManager.cs:327 — `Load()` runs on a `Task`, sets `loaded` from the worker thread, and `File.Move`s the old config; `loaded` is a plain `bool`.
- AutoLootManager.cs:33 — `IsEnabled` dereferences `ProfileManager.CurrentProfile` with no null check.
- BoatMovingManager.cs:145 — `Console.WriteLine` on every boat-move packet.
- BoatMovingManager.cs:239-345 — `Update` iterates `_steps.Values` and calls `house.Generate` / `AddToTile` inside the loop; entries are only removed after the loop via `_toRemove`, but `ClearEntities` (called from `AddStep`, :143) removes from `_items` directly.
- BoatMovingManager.cs:254 — `step.FacingDir` is never assigned anywhere; `drift` is computed and unused.
- BoatMovingManager.cs:104 — deque is trimmed to >5 from the front, so queued steps are silently discarded.
- BoatMovingManager.cs:392-394 — rider positions are recomputed as `boat - offset` each step; an entity that joined the list with a stale offset keeps it until `ClearEntities`.
- CorpseManager.cs:64 — `Remove(corpse, obj)` matches on *either* serial, so passing 0 for one of them matches every entry with a 0 in that field.
- DiscordManager.cs:331-345 — `RunLater` is `async void`, mutates the static `furthestAction` without synchronisation, and resumes on a thread-pool thread that then calls SDK/`World` state.
- DiscordManager.cs:157-165 — `FinalizeDisconnect` blocks the calling thread up to ~2 s with `Thread.Sleep(10)`.
- DiscordManager.cs:131-142 — `pendingDisconnectLeaves--` from SDK callbacks with no interlock; the `== 0` check races across lobbies.
- DiscordManager.cs:493/500 — `GameGameJoinCallback` (global-lobby callback) retries `JoinGameLobby`, not `JoinGlobalLobby`.
- DiscordManager.cs:560 — Discord messages are pushed into `MessageManager.HandleMessage` from an SDK callback.
- DurabilityManager.cs:76 — `World.Player.Serial` dereferenced in the OPL handler with no null check on `World.Player`.
- DurabilityManager.cs:56/91 — `HasDurabilityData` is static but reflects one instance's dictionary.
- EventSink.cs:13-147 — all events are static; subscribers that do not unsubscribe (e.g. a `DurabilityManager` never `Dispose`d) stay reachable for the process lifetime.
- GraphicsReplacement.cs:42-43 — `Save()` clears `graphicChangeFilters` and `quickLookup` after writing, so the in-memory filter set is emptied by saving.
- GraphicsReplacement.cs:10/12 — two structures kept in sync by hand; `Replace` indexes the dictionary after a `quickLookup` hit (:56).
- HealthLinesManager.cs:105 — iterates `World.Mobiles.Values` every frame during draw; `DrawTargetIndicator` writes `ProfileManager.CurrentProfile.ShowTargetIndicator = false` mid-draw when art 0x756F is missing (:248).
- HealthLinesManager.cs:213 — `World.Player.Serial` dereferenced without a null check inside the draw loop.
- HouseDiagnostics.cs:118 — `_footprints` accumulates every house seen and is never cleared, including across server/facet changes.
- HouseDiagnostics.cs:199, :485, :625 — full scans of `World.Items` (once a second while inside a house, plus on every let-go and dump).
- HouseDiagnostics.cs:387 — a `StackTrace` is walked inside `Item.Destroy` for every item destroyed inside a known house footprint.
- HouseDiagnostics.cs:714 — `LogPacket` is called on the network path; `Write` takes `_writeLock` (:860), so packet threads and the game loop contend on one lock.
- HouseDiagnostics.cs:948 — `GameActions.Print` is called from `ResolveLog`, which runs inside the `_writeLock` from whichever thread flushed first.
- IgnoreManager.cs:19/108 — the public static set is replaced by a new instance on load; anything holding the old reference sees stale data. `AddIgnoredTarget` dereferences `World.Player.Serial` (:36).
- IgnoreManager.cs:96 — `foreach (XmlElement xml in root.ChildNodes)` will throw on a non-element node (comment/text).
- JournalFilterManager.cs:52 — `Save()` sets `_instance = null` unconditionally, ignoring its own `resetInstance` parameter; the next `Instance` access reconstructs and reloads from disk.
- LastCharacterManager.cs:87 — `c.AccountName.Equals(account)` with no null guard on `AccountName`.
- MainThreadQueue.cs:38 — `InvokeOnMainThread<T>` blocks on `resultEvent.WaitOne()` with no timeout; calling it from the main thread, or after `Reset()` (:60) drains the queue, deadlocks.
- MainThreadQueue.cs:62 — `Reset` discards queued actions, so any waiter blocked in `InvokeOnMainThread` is never released.
- MessageManager.cs:90/94 — `currentProfile` is dereferenced at :94 before the null check at :111; `:127` and `:166`/`:170` also dereference `ProfileManager.CurrentProfile` unguarded.
- MessageManager.cs:219 — walks the whole `UIManager.Gumps` list for every OBJECT-type message.
- MoveItemQueue.cs:11/20 — `Instance` is assigned by the instance constructor, so constructing a second `MoveItemQueue` silently repoints every static caller.
- MoveItemQueue.cs:85-93 — `Send_PickUpRequest` then `DropItem`/`Send_EquipRequest` are issued back to back in one frame with no wait for the server's acknowledgement of the pickup.
- MoveItemQueue.cs:41 — `World.Player.FindItemByLayer` with no null check on `World.Player`; :48 dereferences `ProfileManager.CurrentProfile`.
- MusicMapManager.cs:150-163 — `TryGetTrack` scans all areas and keeps the *last* match, relying on the sort at load (:128) for "smallest wins".
- ObjectPropertiesListManager.cs:72/78 — `ProfileManager.CurrentProfile` dereferenced with no null check inside `Contains`, which is called on the tooltip hover path.
- ObjectPropertiesListManager.cs:68-87 — `Contains` has the side effect of sending a MegaCliloc request; callers reading it as a pure query still emit packets.
- ObjectPropertiesListManager.cs:148 — `Remove` is by serial only; entries for destroyed items persist unless someone calls it.
- PartyManager.cs:135 — `Members[i] = new PartyMember(serial)` indexes by the packet's loop counter `i`, which is not the same as the slot index once a member has been removed; combined with the `!Contains(serial)` guard a re-listed member can leave a `null` hole or overwrite another slot.
- PartyManager.cs:107 — `Clear()` (which nulls every slot and the leader) runs before the member list is re-read, so any read of `Members` mid-packet sees an empty party.
- PartyManager.cs:203 — `ProfileManager.CurrentProfile.PartyMessageHue` unguarded; :218 the same for `PartyInviteGump`.
- PartyManager.cs:268 — `PartyMember.Name` does a `World.Mobiles.Get` lookup on every access and caches the last seen name.
- SeasonManager.cs:114 etc. — `_springGraphic[orig] = replace` indexes by a file-supplied ushort with no bounds check against the array length.
- SkillsGroupManager.cs:78 — `SkillsGroup.Add` writes `_list[Count++]` into a fixed 60-byte array with no bounds check; :129 `Sort` copies into a 60-byte `stackalloc`.
- SkillsGroupManager.cs:207/223 — `Remove` indexes `Groups[0]` without checking the list is non-empty.
- SpellVisualRangeManager.cs:450-453 — `DelayedSave` creates a `System.Timers.Timer`, wires `Elapsed`, and never sets `Enabled`/`Start()`, so `PerformSave` only ever runs from `Save()`.
- SpellVisualRangeManager.cs:70-80 — `OnRawMessageReceived` dispatches to `Task.Run`; `SetCasting` then writes `World.Player.Flags |= Flags.Frozen` (:100) off the game thread.
- SpellVisualRangeManager.cs:84-90 — `OnClilocReceived` likewise runs `ClearCasting` on a task, which writes `World.Player.Flags` (:110).
- SpellVisualRangeManager.cs:246-294 — `Load()` runs on a task and mutates `spellRangeCache` while `ProcessHueForTile`/`IsTargetingAfterCasting` may read `currentSpell` from the draw thread.
- SpellVisualRangeManager.cs:110/164/601 — `World.Player.Flags` cleared from three different places (ClearCasting, IsCastingWithoutTarget, the cast bar's Draw).
- SpellVisualRangeManager.cs:127 — `OnSceneUnload` sets `instance = null` while `EventSink.RawMessageReceived` handlers already dispatched onto tasks may still be running.
- SpellVisualRangeManager.cs:394 — `spellRangePowerWordCache.Add` (not indexer) in `AfterLoad`, so two spells sharing power words throw inside the load task.
- SpellVisualRangeManager.cs:36-37 — `savePath`/`overridePath` are captured at construction from `ProfileManager.ProfilePath`; the instance outlives a profile switch unless `OnSceneUnload` ran.
- TargetManager.cs:184 — `_lastDataBuffer` is a single shared 19-byte static buffer written by `Target`, `TargetLast` and `TargetPacket`.
- TargetManager.cs:396-424 — the criminal-query path returns while still targeting; the actual send happens in the gump callback, by which time `_targetCursorId` and `TargetingType` may have been reassigned by a newer server cursor.
- TargetManager.cs:385/494/526 — `ProfileManager.CurrentProfile` dereferenced with no null check.
- TargetManager.cs:734-770 — `TargetAsync` cancels the previous request's token but leaves the *previous* awaiting caller to return `0`; the result is read from the shared `LastTargetInfo.Serial` after the poll loop, so a target set by another path is indistinguishable.
- TextRenderer.cs:71-126 — `Draw` walks the linked list from `DrawPointer` backwards; `ProcessWorldText` is called at the top of `Draw` (:66) and resets `DrawPointer` (:159) while the same list is mutated by `AddMessage`/`MoveToTop` from message handling.
- TextRenderer.cs:44/153 — `_bounds` is only cleared inside `ProcessWorldText(true)`, which the per-frame `Update()` path (`ProcessWorldText(false)`, :61) does not do.
- TextRenderer.cs:249-268 — `AddMessage` links after `FirstNode` (which is `this`); `Clear` (:272) walks left from both `FirstNode` and `DrawPointer` and calls `Destroy`+`Clear` on each node, potentially twice over the same nodes.
- TitleBarStatsManager.cs:39 — `World.Player` dereferenced in `GenerateStatsText` although only `UpdateTitleBar` (:16) checks it; `GetPreviewText` handles null separately (:108).
- UIManager.cs:207-214 — `foreach (Gump s in Gumps)` calls `s.Dispose()` inside the enumeration on mouse-down outside a modal.
- UIManager.cs:496-502 — `Clear()` disposes every gump while enumerating `Gumps`.
- UIManager.cs:628-651 — `MakeTopMostGump` calls `Gumps.Remove(gump)` (by value) then `Gumps.AddFirst(start)` / `AddBefore(Gumps.Last, start)` re-inserting the *node object* that was just removed, inside a `for` loop over the same list.
- UIManager.cs:654-696 — `SortControlsByInfo` removes and re-adds nodes while iterating the outer and inner loops over `Gumps`.
- UIManager.cs:465 — `ProfileManager.CurrentProfile.GlobalScaling` dereferenced when `World.InGame` is true; `IsMouseOverWorld` (:72) does null-check.
- UIManager.cs:48 — `_mouseDownControls` is sized `0xFF` and indexed by `(int)MouseButtonType`; `AttemptDragControl` (:724) clears only indices `< MouseButtonType.Size`.
- UIManager.cs:232-247 — mouse-up delivers `InvokeMouseUp` to a control that may differ from the one that received mouse-down; the `MouseOverControl != null` branch does not re-check `_mouseDownControls[index].IsDisposed` on the first path (:234).
- WalkerManager.cs:110 — `World.Player.ClearSteps()` on a deny-walk packet with no null check.
- WalkerManager.cs:132-171 — `ConfirmWalk` locates a step by sequence number and shifts the array; on a bad step it zeroes `StepsCount`/`CurrentWalkSequence` and resyncs, discarding any pending client-side steps.
- WalkerManager.cs:101 — `StepInfos` is a fixed array of `MAX_STEP_COUNT`; the write side is elsewhere (Player), nothing here bounds-checks `StepsCount`.
- WorldTextManager.cs:76-101 — `Draw` iterates `_damages` and, in the corpse-substitution branch, appends to `_subst` (applied next `Update`) — the key/serial identity of a damage entry is reassigned from mobile serial to corpse serial (:90, :113).
- WorldTextManager.cs:120-128 — `UpdateDamageOverhead` collects removals into `_toRemoveDamages` and the caller removes them (:54-62); `Clear()` (:143) drains that list but leaves `_damages` itself populated.
- AudioManager.cs:634 — `_mapTrackEnded` is set from the decoder thread (`UOMusic.Ended`, wired at :78) and read/cleared in `Update` (:706-711); `_endedTrack` beside it is a plain `int`.
- AudioManager.cs:266-294 — `ReloadMusicEra` reads `_currentMusicIndices[0]` before `StopMusic()`, which does not reset the index; the comment at :270 records that the index outlives the track.
- AudioManager.cs:163 — `PlaySoundWithDistance` reads `currentProfile.SoundVolume` at :163 before the null check at :182.
- AudioManager.cs:410/1002 — the region slot keeps playing at volume 0 underneath war music; `StopWarMusic` (:957) only restarts it when `_currentMusicIndices[0] >= 0 && !StillGoing()`.
- AnimatedStaticsManager.cs:131 — writes `ArtLoader.Instance.Entries[o.Index + 0x4000].AnimOffset` — shared asset state mutated from the frame loop.
- AnimatedStaticsManager.cs:116 — raw pointer arithmetic into the mmap'd AnimData file using an offset recomputed each tick; the bound check done at `Initialize` (:66) is not repeated.
- ContainerManager.cs:57-58/71 — `X`/`Y` are static and mutated by `CalculateContainerPosition`; two containers opening in the same frame both read whatever the last call left.
- ContainerManager.cs:89 — `ProfileManager.CurrentProfile.OverrideContainerLocation` unguarded; :204 `World.Player.FindItemByLayer` unguarded.
- ContainerManager.cs:282 — `BuildContainerFile` clears `_data` and re-parses; anything holding a `ContainerData` reference keeps the old object.
- FriendsListManager.cs:44 — `_savePath` is derived from `World.ServerName` at first `Load()`; `_loaded` is never reset, so a later server change keeps writing to the first server's file.

## Fork deltas

Files/features that are not stock ClassicUO:

- **HouseDiagnostics.cs** — entire file. Holiday-Edition-only. Gated on `Settings.GlobalSettings.LogHouseDiagnostics`, referenced from Item.Destroy, PacketHandlers, NetClient/AsyncNetClient, GameSceneDrawingSorting, World, GameController.
- **AudioManager music-map subsystem** — `_appliedMusicEra`, `GetAvailableMusicEras`, `EnsureMusicEraApplied`, `ReloadMusicEra`, `StopMusicFromServer(keepPlaying)`, `CanSeasonMusicTakeOver`, `NotifyServerTrack`, `GetMusicStatus`, `ForgetMusicState`, `UpdateMusicMap`, `PlayFromMap`, `StopFromMap`, `AdoptAsMapTrack`, `MAP_OFF/AUTHENTIC/SEAMLESS/CONTINUOUS`, plus all `MusicDiagnostics.*` calls (AudioManager.cs:199-927, 986-989). Stock ClassicUO's AudioManager has none of this.
- **MusicMapManager.cs** — entire file; reads `Data/MusicMap.txt` (1998 region rectangles).
- **AutoLootManager.cs** — TazUO feature (scavenger, progress bar gump, per-character import/export).
- **MoveItemQueue.cs**, **MainThreadQueue.cs**, **GlobalActionCooldown** usage — TazUO.
- **DiscordManager.cs** — TazUO (Discord Social SDK, `TazUODiscordSocialSDKLobby`, CLIENT_ID 1255990139499577377).
- **SpellVisualRangeManager.cs** — TazUO spell indicators; embedded resource `DefaultSpellIndicatorConfig.json`.
- **EventSink.cs** — TazUO scripting/plugin event hub; `PreProcessTooltip`/`PostProcessTooltip` delegates and `SpellCastBegin` are TazUO API surface.
- **DurabilityManager.cs**, **FriendsListManager.cs**, **JournalFilterManager.cs**, **GraphicsReplacement.cs**, **TitleBarStatsManager.cs**, **IgnoreManager.cs** — TazUO additions.
- **ObjectPropertiesListManager** — `ItemPropertiesData`/`SinglePropertyData` tooltip parsing/comparison and `ForcedTooltipManager` hooks are TazUO; the base OPL store is stock.
- **HealthLinesManager** — `DrawTargetIndicator` (gump 0x756F), `HealthLineSizeMultiplier`, `TargetManager.SelectedTarget` handling are TazUO additions to the stock draw.
- **TargetManager** — `CursorTarget.Grab/SetGrabBag/SetMount/HueCommandTarget/IgnorePlayerTarget/MoveItemContainer/Internal/SetFavoriteMoveBag`, `AutoTargetInfo`, `SelectedTarget`, `TargetHelper`, and the `ScriptRecorder` calls are TazUO.
- **MessageManager** — `GridContainer`/`ModernPaperdoll` object-message routing (:219-229), `ForceTooltipsOnOldClients` (:94), TextBox/RTLOptions overhead rendering (:356-358) are TazUO.
- **UIManager** — `UpdateTimerEnabled`/`UpdateTimerTotalTime`/`UpdateTimerCount` profiling (:114-131), `SlowUpdate` (:439), `GlobalScaling` in `Draw` (:465) are TazUO.
- **PartyManager** — `mob.InParty` flag maintenance (:137-138, :172-173) is TazUO.
- Language level: file-scoped namespaces (JournalFilterManager, MainThreadQueue, DiscordManager), primary constructors on structs (`MoveRequest` MoveItemQueue.cs:108, `ServerInfo` DiscordManager.cs:727), `case > 0x04:` relational pattern (BoatMovingManager.cs:67) — modern C# compiled against `net472`.
- Stock-ClassicUO files essentially untouched: ChatChannel, ChatStatus, ContainerManager, CorpseManager, SeasonManager, SkillsGroupManager, HotkeysManager, WalkerManager, AnimatedStaticsManager, BoatMovingManager, LastCharacterManager, TextRenderer, WorldTextManager, ActiveIconsManager.
