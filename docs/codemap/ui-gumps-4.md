# ui-gumps-4

Partition = every 6th file (offset 4) of `src/ClassicUO.Client/Game/UI/Gumps/**.cs`.
20 files, 15,335 lines, all read in full.

Root for all paths below: `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/UI/Gumps/`

## Files

| path | lines | purpose |
| --- | --- | --- |
| `AssistantGump.cs` | 1850 | TazUO "Assistant Features" window: autoloot / autosell / autobuy / mobile-graphic filter / spellbar / HUD-hide flags / spell indicator editor / journal filter / title bar stats / dress agent / bandage agent / friends list / organizer. Contains 5 nested config-list controls. |
| `CharCreation/CreateCharAppearanceGump.cs` | 1174 | Character creation step 1. Builds a fake `PlayerMobile(1)` + fake `Item`s in `World` to drive a live paperdoll; race/gender radios, hair/beard comboboxes, custom color pickers, name validation (OSI disallowed-word list). |
| `ColorPickerGump.cs` | 118 | Server dye-tub gump (`0x0906`). Slider sets `ColorPickerBox.Graduation`; OK sends `Send_DyeDataResponse` and/or invokes a callback. |
| `CreditsGump.cs` | 84 | Scrolling credits over gump `0x0500`; plays music index 8. Scroll advances 1px per ~25ms. |
| `DiscordGump/DiscordFriendListControl.cs` | 68 | Control (not a Gump) listing Discord friends inside `DiscordGump`; rebuilt wholesale via `BuildFriendsList()`. |
| `DurabilityGump.cs` | 222 | `DurabilityGumpMinimized` (30x30 icon) + `DurabilitysGump` (NineSlice, resizable, saved to XML) listing equipment durability bars sorted by percentage. |
| `GridHighLight/GridHighLightProfile.cs` | 69 | Plain POCO serialization models for grid-highlight rules. No behaviour. |
| `GumpType.cs` | 71 | `GumpType` enum — the persistence discriminator written into gump XML save files. |
| `InputRequest.cs` | 62 | Two-button modal-ish text prompt gump; `Action<Result,string>` callback. |
| `Login/LoginGump.cs` | 819 | Login screen: account/password boxes (`PasswordStbTextBox` masks with `*`), autologin/save/music checkboxes, version labels, wiki/discord links, arrow-button blink. |
| `MarkersManagerGump.cs` | 548 | World-map marker manager: category tabs per marker file, search box, per-row edit/remove/goto. Writes the `.usr` marker file on Dispose if modified. |
| `ModernOptionsGump.cs` | 5581 | The main options window. 15 top-level pages built eagerly at construction; nested `MacroControl`, `MacroEntry`, `NameOverheadAssignControl`, `InfoBarBuilderControl`, `mainScrollArea`, `ProfileLocationData`. Saves the profile on Dispose. |
| `NameOverHeadHandlerGump.cs` | 226 | Small floating nameplate-mode picker: radio button per `NameOverheadOption`, "stay active", hide-at-full-hp toggles, search box. |
| `PartyGump.cs` | 412 | Party manifest (NineSlice): 10 fixed slots, Msg/Kick per slot, live HP bars, loot toggle, leave/disband/add. |
| `QuestionGump.cs` | 114 | Modal yes/no confirmation over gump `0x0816` with a full-screen dim layer. |
| `ResizableJournal.cs` | 814 | TazUO journal: user-defined tabs mapping name -> `MessageType[]`, custom-drawn scrolling entry list (`JournalEntriesContainer`), border styles, per-tab context menu. Subscribes to `EventSink.JournalEntryAdded`. |
| `SkillProgressBar.cs` | 164 | Transient skill-gain bar with a static `ConcurrentQueue` manager; auto-disposes after 4000 ms. |
| `StatusGump.cs` | 2216 | `StatusGumpBase` + `StatusGumpOld` / `StatusGumpModern` / `StatusGumpOutlands`. Polls `World.Player` every 250 ms into `Label[]`. Stat-lock cycling sends `ChangeStatLock`. |
| `TooltipConfigGump.cs` | 363 | Tooltip-override editor (NineSlice): rows of search text / format text / min-max pairs / layer combobox, with a 1500 ms debounced save onto the main thread. |
| `UserMarkerGump.cs` | 360 | Add/edit a single world-map marker; appends a CSV line to the `.usr` file or replaces `_markers[_markerIdx]` in place. |

## Types

| type | file:line | responsibility |
| --- | --- | --- |
| `AssistantGump : BaseOptionsGump` | AssistantGump.cs:18 | Owner of the 13 assistant pages; `PAGE` enum at :873. Dispose saves DressAgent + OrganizerAgent. |
| `AssistantGump.SpellRangeEditor : Control` | AssistantGump.cs:893 | Editor bound to one `SpellVisualRangeManager.SpellRangeInfo`; every widget change calls `Save()` -> `DelayedSave()`. |
| `AssistantGump.AutoLootConfigs : Control` | AssistantGump.cs:1012 | DataBox of autoloot entries; `Insert(2, …)` on add. |
| `AssistantGump.SellAgentConfigs / BuyAgentConfigs : Control` | AssistantGump.cs:1200 / :1431 | VBox of `BuySellItemConfig` rows (graphic/hue/max/restock/enabled/delete). |
| `AssistantGump.GraphicFilterConfigs : Control` | AssistantGump.cs:1662 | DataBox of `GraphicChangeFilter`; `Insert(3, …)` on add. |
| `CreateCharAppearanceGump : Gump` | CharCreation/CreateCharAppearanceGump.cs:48 | Char-creation appearance page. |
| `CreateCharAppearanceGump.CustomColorPicker : Control` | …:1046 | Swatch that opens a modal `ColorPickerBox` at fixed screen 489,141. |
| `CreateCharAppearanceGump.ColorSelectedEventArgs` | …:1028 | Carries layer + palette + index. |
| `ColorPickerGump : Gump` | ColorPickerGump.cs:39 | Dye gump; `LocalSerial != 0` => sends dye response. |
| `CreditsGump : Gump` | CreditsGump.cs:13 | Scrolling text. |
| `DiscordFriendListControl : Control` | DiscordGump/DiscordFriendListControl.cs:8 | (namespace `ClassicUO.Game.UI.Controls`, not `.Gumps`). |
| `DurabilityGumpMinimized : Gump` | DurabilityGump.cs:16 | Click opens `DurabilitysGump`; only accepts input when `DurabilityManager.HasDurabilityData`. |
| `DurabilitysGump : NineSliceGump` | DurabilityGump.cs:55 | `GumpType.DurabilityGump`; Save/Restore XML attrs `lastX/lastY/lastWidth/lastHeight`. |
| `GridHighlightSetupEntry` / `GridHighlightSlot` / `GridHighlightProperty` | GridHighLight/GridHighLightProfile.cs:13 / :40 / :63 | Serialized highlight rule shape. |
| `GumpType` (enum) | GumpType.cs:35 | Persistence discriminator; note explicit numeric jumps `MacroButtonEditor = 6464`, `DurabilityGump = 6465`, `GridContainer = 8787`. |
| `InputRequest : Gump` | InputRequest.cs:8 | `Result.BUTTON1/BUTTON2`. |
| `LoginGump : Gump` | Login/LoginGump.cs:48 | Login screen. |
| `LoginGump.PasswordStbTextBox : StbTextBox` | Login/LoginGump.cs:658 | Shadows base `_rendererText`/`_rendererCaret`/`_caretScreenPosition` with `new`; draws `*` per char, keeps plaintext in `Text`. |
| `MarkersManagerGump : Gump` | MarkersManagerGump.cs:16 | Marker list manager. |
| `MarkersManagerGump.MakerManagerControl : Control` | MarkersManagerGump.cs:333 | One marker row; holds `_idx` into the shared `_markers` list. |
| `MarkersManagerGump.SearchTextBoxControl : Control` | MarkersManagerGump.cs:468 | Search field + search/clear buttons (button ids 100/101). |
| `MarkersManagerGump.DrawTexture : Control` | MarkersManagerGump.cs:315 | Raw `Texture2D` blit at 15x15. |
| `ModernOptionsGump : BaseOptionsGump` | ModernOptionsGump.cs:28 | Options root; `PAGE` enum :5561. |
| `ModernOptionsGump.MacroControl : Control` | ModernOptionsGump.cs:4727 | Hotkey box + list of `MacroEntry`; binds a `Macro`. |
| `ModernOptionsGump.MacroControl.MacroEntry : Control` | ModernOptionsGump.cs:5076 | One macro step: type combobox + optional sub-combobox / text field. |
| `ModernOptionsGump.NameOverheadAssignControl : Control` | ModernOptionsGump.cs:5290 | Flag checkboxes + hotkey for one `NameOverheadOption`. |
| `ModernOptionsGump.InfoBarBuilderControl : Control` | ModernOptionsGump.cs:4472 | One infobar row; overrides `Update()` to prune disposed children. |
| `ModernOptionsGump.mainScrollArea : Control` | ModernOptionsGump.cs:4609 | Two-column scroll host used only by the InfoBar page. |
| `ModernOptionsGump.ProfileLocationData` | ModernOptionsGump.cs:5542 | server/username/character `DirectoryInfo` triple for profile copy-out. |
| `NameOverHeadHandlerGump : Gump` | NameOverHeadHandlerGump.cs:42 | `GumpType.NameOverHeadHandler`; `LayerOrder = UILayer.Over`. |
| `PartyGump : NineSliceGump` | PartyGump.cs:43 | Party manifest; `Buttons.KickMember = TellMember + 10`. |
| `QuestionGump : Gump` | QuestionGump.cs:40 | `IsModal = true`, `CanMove = false`. |
| `ResizableJournal : ResizableGump` | ResizableJournal.cs:18 | `GumpType.Journal`; `BorderStyle` enum :144. |
| `ResizableJournal.JournalEntriesContainer : Control` | ResizableJournal.cs:483 | Owns the `Deque<JournalData>` and does all journal drawing itself in `Draw`. |
| `ResizableJournal.JournalEntriesContainer.JournalData` | ResizableJournal.cs:662 | Pair of `TextBox` (entry + timestamp) + text/message type. |
| `ResizableJournal.TabContextEntry : ContextMenuControl` | ResizableJournal.cs:685 | Per-tab MessageType toggles + delete-tab. |
| `SkillProgressBar : Gump` | SkillProgressBar.cs:10 | Transient gain bar. |
| `SkillProgressBar.QueManager` (static) | SkillProgressBar.cs:111 | Queue of pending bars. |
| `StatusGumpBase : Gump` | StatusGump.cs:46 | Shared lock graphics, click-to-healthbar, factory `GetStatusGump`/`AddStatusGump` (:162/:185). |
| `StatusGumpOld` / `StatusGumpModern` / `StatusGumpOutlands` | StatusGump.cs:252 / :621 / :1657 | Three layouts, each with a private `MobileStats` enum indexing `_labels`. |
| `StatusGumpModern.Settings : UISettings` | StatusGump.cs:1604 | Persisted graphic/hue overrides, loaded through a static lazily-built singleton. |
| `TooltipConfigGump : NineSliceGump` | TooltipConfigGump.cs:14 | Tooltip override rows. |
| `UserMarkersGump : Gump` | UserMarkerGump.cs:14 | Add/edit marker dialog; `EditEnd` event. |

## State

Mutable / static / cross-instance state owned in this partition:

- `MarkersManagerGump._markers` — **static** `List<WMapMarker>`, MarkersManagerGump.cs:30. Reassigned to whichever marker file's list the active tab points at (:54, :273). Every `MakerManagerControl` closes over the current value and indexes it by `_idx`.
- `MarkersManagerGump._markerFiles` — **static readonly** alias of `WorldMapGump._markerFiles`, MarkersManagerGump.cs:32.
- `MarkersManagerGump._isMarkerListModified` — :22, drives the file rewrite in `Dispose` (:296).
- `DurabilitysGump.lastWidth/lastHeight/lastX/lastY` — **static**, DurabilityGump.cs:57. Shared by all instances; written in `Dispose` (:122) and `OnResize` (:193).
- `NameOverHeadHandlerGump.LastPosition` — **public static `Point?`**, NameOverHeadHandlerGump.cs:44, written in `OnDragEnd` (:217).
- `ResizableJournal.ReloadTabs` — **public static bool**, ResizableJournal.cs:21. Set from context menus / new-tab dialog, consumed in `Update` (:468) by *every* live journal instance.
- `ResizableJournal.BORDER_WIDTH` — **static non-const int**, ResizableJournal.cs:23, mutated by `BuildBorder()` per style (:240, :263) and read by `JournalEntriesContainer.Update` (:555). `MIN_WIDTH` (:24) is computed once from the initial value.
- `ResizableJournal._lastX/_lastY/_lastWidth/_lastHeight` — **static**, ResizableJournal.cs:49-50, shared across instances.
- `ResizableJournal.JournalEntriesContainer.journalDatas` — `Deque<JournalData>`, :485; trimmed to `profile.MaxJournalEntries` in `AddEntry` (:599).
- `SkillProgressBar.QueManager.skillProgressBars` — **static `ConcurrentQueue<SkillProgressBar>`**, SkillProgressBar.cs:113; `CurrentProgressBar` static field :114; `beingReset` static bool :115.
- `SkillProgressBar.expireAt` — :12, set by `SetDuration` to `Time.Ticks + ms`.
- `StatusGumpModern._settings` — **static** `Settings`, StatusGump.cs:623; lazily loaded/saved via `Settings.Load/Save` keyed on `typeof(StatusGumpModern).ToString()`.
- `StatusGumpBase._labels` / `_lockers` / `_refreshTime` / `_point` — StatusGump.cs:69-72; `_labels` is sized from a *per-subclass* `MobileStats.NumStats`/`Max`.
- `ModernOptionsGump.options` — `List<SettingsOption>`, :30; every page control is constructed before any is shown.
- `ModernOptionsGump.MacroControl._allHotkeysNames/_allSubHotkeysNames` — **static readonly string[]** from `Enum.GetNames`, :4729-4730.
- `CreateCharAppearanceGump.CurrentColorOption` / `CurrentOption` — instance dictionaries keyed by `Layer`, :65-66; `CurrentOption` pre-seeded with Hair=1, Beard=0.
- `CreateCharAppearanceGump._character` — a `PlayerMobile(1)` **inserted into `World.Mobiles`** (:261) and items into `World.Items` at serials `0x4000_0000 + (uint)layer` (:1006).
- `CreateCharAppearanceGump._Disallowed`/`_StartDisallowed`/`_SpaceDashPeriodQuote` — static name-validation tables, :895-987.
- `AssistantGump.profile` / `lang` — :20-21 captured at construction.
- `LoginGump._time` — :58, blink timer.
- `CreditsGump._offset` / `_lastUpdate` — :16-17.
- `TooltipConfigGump.mainContainer` / `dataContainer` — :17-18, replaced wholesale by `Build()`.

## Timing

- **Per frame (`Update`)**
  - `LoginGump.Update` (Login/LoginGump.cs:591): every 1000 ms toggles `_nextArrow0.ButtonGraphicNormal` between normal/over; every frame syncs textbox hue to keyboard focus.
  - `CreditsGump.Update` (CreditsGump.cs:62): `_offset.Y -= 1` then `_lastUpdate = Time.Ticks + 25` — i.e. one pixel per ~25 ms.
  - `StatusGumpOld.Update` (:566), `StatusGumpModern.Update` (:1500), `StatusGumpOutlands.Update` (:2009): all gated on `_refreshTime < Time.Ticks`, then `_refreshTime = Time.Ticks + 250`. Every stat label string is rebuilt each tick regardless of change.
  - `PartyGump.Update` (PartyGump.cs:62): every frame, loops slots 0..9 and pushes `mobile.Hits/HitsMax` into the cached `SimpleProgressBar`s.
  - `ResizableJournal.Update` (:453): every frame compares X/Y to `_lastX/_lastY` and writes `profile.JournalPosition`; calls `Reposition()` when size changed **and** `!Mouse.LButtonPressed`; consumes the static `ReloadTabs` flag.
  - `ResizableJournal.JournalEntriesContainer.Update` (:538): on width/height change re-wraps every cached entry `TextBox` and calls `Update()` on each manually ("this control isn't a child of any gump, it doesn't get updated").
  - `SkillProgressBar.Update` (:95): disposes once `Time.Ticks >= expireAt` (4000 ms set at `QueManager.ShowNext`, :149).
  - `ModernOptionsGump.InfoBarBuilderControl.Update` (:4578): custom child loop that removes disposed children in place with `Children.RemoveAt(i--)`.
- **Per draw**
  - `ResizableJournal.JournalEntriesContainer.Draw` (:508): walks the whole `Deque` each frame inside a clip rect, re-evaluating `CanBeDrawn` filter per entry per frame.
  - `CreditsGump.Draw` (:73), `MarkersManagerGump.DrawTexture.Draw` (:325), `DurabilityGumpMinimized.Draw` (:32).
- **Event-driven (not polled)**
  - `ResizableJournal` subscribes `EventSink.JournalEntryAdded` in the ctor (:134) and unsubscribes in `Dispose` (:478). Entry arrival is per-packet.
  - `DurabilitysGump.UpdateContents` (:129) runs only on `RequestUpdateContents()`; reads `World.DurabilityManager.Durabilities` and `World.Items.Get`.
  - `PartyGump.UpdateContents` (:80) rebuilds the whole gump on party change / loot-type toggle.
- **On demand / one-shot**
  - `TooltipConfigGump.SaveWithDelay` (:332): spawns a `Task`, `Thread.Sleep(1500)`, then `MainThreadQueue.EnqueueAction(saveAction)`. Fires once per keystroke.
  - `ModernOptionsGump` "Import from URL" (:3514): `Task.Factory.StartNew` + `HttpClient.GetStringAsync(uri).Result` (blocking `.Result` inside the task).
  - `ModernOptionsGump.BuildTazUO` settings-transfer section (:3975): synchronous `Directory.GetDirectories` walk of every account/server/character profile folder, executed while the gump is being constructed.
  - `ModernOptionsGump.Dispose` (:4272) saves the profile; `AssistantGump.Dispose` (:865) saves DressAgent + OrganizerAgent.
  - `MarkersManagerGump.Dispose` (:296) rewrites the `.usr` file and calls `ReloadUserMarkers()`.
  - `UserMarkersGump.AddNewMarker` (:273) does a synchronous `File.AppendAllText`.
  - `CreditsGump` ctor calls `Client.Game.Audio.PlayMusic(8, false, true)` (:44).

## Inbound

- `UIManager.Add(...)` / `UIManager.GetGump<T>()` for every gump here. Specific known callers:
  - `StatusGumpBase.GetStatusGump()` / `AddStatusGump(x,y)` (StatusGump.cs:162/:185) are the documented entry points; they branch on `CUOEnviroment.IsOutlands`, `profile.UseOldStatusGump`, and `Client.Version < CV_308Z`.
  - `LoginScene` constructs `LoginGump(scene)`; `LoginGump.OnButtonClick` -> `LoginScene.Connect` (:641), `Client.Game.Exit()` (:647), `new CreditsGump()` (:652).
  - `CharCreationGump` hosts `CreateCharAppearanceGump`; `OnButtonClick` calls `charCreationGump.SetCharacter(_character)` / `StepBack()` (:783/:789).
  - `ModernOptionsGump` is reached via `GameActions.OpenSettings`; it self-recreates on cooldown add/delete (:2758-2772, :4315-4329).
  - `AssistantGump` self-recreates when the `HideHudFlags.All` checkbox toggles (:317, :328).
  - `GridHighlightMenu.Open()` from ModernOptionsGump :2989; `TooltipConfigGump` from :3720; `IgnoreManagerGump` from :70.
  - `MarkersManagerGump.MakerManagerControl` opens `UserMarkersGump` (:448) and calls `WorldMapGump.GoToMarker` (:461). `WorldMapGump` also constructs `UserMarkersGump` directly (ctrl-click add marker).
  - `SkillProgressBar.QueManager.AddSkill(int)` (:118) is called from skill-change handling.
  - `ResizableJournal` receives entries through `EventSink.JournalEntryAdded`; `ResizableJournal.UpdateJournalOptions()` (static, :313) is called from ModernOptionsGump journal options.
  - `DiscordFriendListControl.BuildFriendsList()` / `UpdateSelectedFriend()` called by `DiscordGump`.
  - `Gump.Save/Restore` (XML persistence) reaches `DurabilitysGump` (:201/:211) and `ResizableJournal` (:321/:339) through `GumpType`.

## Outbound

- **Config**: `ProfileManager.CurrentProfile` (read + write, hundreds of sites in ModernOptionsGump/AssistantGump), `ProfileManager.CurrentProfile.Save`, `ProfileManager.SetProfileAsDefault`, `Settings.GlobalSettings` (FPS, ClientViewRange, MusicEra, MusicMapMode, LoginMusic, Username/Password, LogMusicIndices, LogHouseDiagnostics, KeepHouseContentsLoaded).
- **Network**: `NetClient.Socket.Send_DyeDataResponse` (ColorPickerGump.cs:109), `Send_ClientViewRange` (ModernOptionsGump.cs:2381), `Send_PartyChangeLootTypeRequest` / `Send_PartyInviteRequest` / `Send_PartyRemoveRequest` (PartyGump.cs:311/:347/:394), `GameActions.ChangeStatLock` (StatusGump.cs:296 etc.), `GameActions.RequestPartyQuit` (PartyGump.cs:332).
- **Managers**: `AutoLootManager`, `BuySellAgent`, `GraphicsReplacement`, `SpellBarManager`, `SpellVisualRangeManager`, `JournalFilterManager`, `TitleBarStatsManager`, `DressAgentManager`, `FriendsListManager`, `OrganizerAgent`, `HideHudManager`, `TargetManager`, `TargetHelper`, `NameOverHeadManager`, `IgnoreManager`, `JournalManager`, `InfoBarManager`, `MacroManager`, `ContainerManager`, `DurabilityManager`, `CoolDownBarManager`, `DiscordManager`, `SimpleAccountManager`, `MainThreadQueue`.
- **Other gumps**: `GridContainer.UpdateAllGridContainers()`, `ResizableJournal.UpdateJournalOptions()`, `ModernPaperdoll.UpdateAllOptions()`, `NameOverheadGump.UpdateAllOptions()`, `InfoBarGump.UpdateAllOptions()/ResetItems()`, `CounterBarGump.SetLayout`, `WorldViewportGump.ResizeGameWindow/SetGameWindowPosition`, `MusicInfoGump.Toggle`, `MacroButtonGump`, `MacroButtonEditorGump`, `MessageBoxGump`, `EntryDialog`, `QuestionGump`, `InputRequest`, `SimpleTimedTextGump`, `HealthBarGump`/`HealthBarGumpCustom`, `BuffGump`/`ImprovedBuffGump`, `DressAgentConfigGump`, `IgnoreManagerGump`, `TooltipConfigGump`, `GridHighlightMenu`, `SpellBar.SpellBar`.
- **Engine**: `Client.Game.SetRefreshRate`, `SetWindowBorderless`, `SetWindowTitle`, `Audio.PlayMusic/UpdateCurrentMusicVolume/ReloadMusicEra/GetAvailableMusicEras`, `Client.Game.Scene.Camera`, `GameController.UpdateBackgroundHueShader`, `GameScene.SetPostProcessingSettings`, `StaticFilters.ApplyCaveTileBorder`, `World.Light.*`.
- **World model**: `World.Player.*` (StatusGump, PartyGump, SkillProgressBar), `World.Party.Members[0..9]`, `World.Mobiles.TryGetValue/Add`, `World.Items.Get`, `World.GetOrCreateItem`, `World.RemoveItem`, `World.DurabilityManager`, `World.MapIndex`, `World.ClientFeatures/ClientLockedFeatures`.
- **Assets/IO**: `ClilocLoader`, `FontsLoader`, `GumpsLoader.UseUOPGumps`, `TrueTypeLoader.Fonts`, `MapLoader.MapsDefaultSize`, `Client.Game.Gumps.GetGump`, `File.AppendAllText`, `StreamWriter`, `Directory.GetDirectories`, `HttpClient`.

## Hazards

- `MarkersManagerGump.cs:30` — `_markers` is `static` and reassigned on tab switch (:273). `MakerManagerControl` instances hold a bare `_idx` (:361) into whatever list `_markers` pointed at when they were built.
- `MarkersManagerGump.cs:248` — `_markers.RemoveAt(idx)` shifts every later index; the surviving `MakerManagerControl`s created before the redraw still hold their old `_idx`. Redraw happens immediately after, but `UserMarkersGump` opened earlier (:448) captured `_markerIdx` and writes `_markers[_markerIdx] = editedMarker` (UserMarkerGump.cs:266) with no bounds/identity check.
- `MarkersManagerGump.cs:296` — `Dispose` writes only the *currently selected* file's `_markers` to `_userMarkersFilePath`, regardless of which file the edits came from.
- `MarkersManagerGump.cs:272` — `default:` treats the raw `buttonID` as a marker-file index (`_markerFiles[buttonID]`) with no range check.
- `MarkersManagerGump.cs:277` — `_scrollArea.Clear()` then `DrawArea` adds a *new* `ScrollArea` without removing the old one (`Remove(_scrollArea)` only happens on the remove path at :250).
- `MarkersManagerGump.cs:872` (`CreateCharAppearanceGump.cs:872`) — `name[indexOf - 1]` is read when `indexOf == 0` is false, but the loop at :871 runs only when `!badPrefix`, i.e. `indexOf != 0`; still an unguarded `indexOf - 1` read pattern.
- `CreateCharAppearanceGump.cs:261` — a fake `PlayerMobile(1)` is added to the live `World.Mobiles`, and fake items are created at `World.GetOrCreateItem(0x4000_0000 + (uint)layer)` (:1006). Serial 1 and the `0x4000_0000+layer` range collide with anything the server later assigns.
- `CreateCharAppearanceGump.cs:293` — `CurrentColorOption[Layer.Shirt]` is indexed before `AddCustomColorPicker` has necessarily populated it for the gargoyle path; `HandleRaceChanged` clears the dictionary at :394 and `CreateCharacter` is called at :591 after the pickers are added, but `HandleGenreChange` at :395 also calls into the same path.
- `CreateCharAppearanceGump.cs:1104` — `SetSelectedIndex` no-ops when `_colorPickerBox == null` (it is null until the picker has been opened once), so a restored index is silently dropped.
- `CreateCharAppearanceGump.cs:1117` — `SetCurrentHue()` disposes `_colorPickerBox` but leaves the field non-null; `ColorPickerBoxOnMouseUp` (:1126) then reads `_colorPickerBox.Hues` on a disposed control.
- `ResizableJournal.cs:23` — `BORDER_WIDTH` is `static` but mutated per-instance by `BuildBorder()`; two journals with different border styles fight over it, and `JournalEntriesContainer.Update` (:555) reads it.
- `ResizableJournal.cs:21` — `ReloadTabs` is a static flag consumed by *every* journal instance's `Update` (:468); the first one to run clears it.
- `ResizableJournal.cs:599` — trim loop is `while (count > Max)` *before* the new entry is added, so the deque can settle at Max+1.
- `ResizableJournal.cs:176` — `OnButtonClick(ProfileManager.CurrentProfile.LastJournalTab)` is called from `BuildTabs` before the tabs are `Add`ed to the gump (:179-180).
- `ResizableJournal.cs:355` — `Restore` calls `OnButtonClick(tab)` with an unvalidated saved index (guarded only by `_tab.Count > buttonID` inside).
- `ResizableJournal.cs:465` — `Reposition()` is skipped while the left mouse button is held, so size-dependent children lag during a drag-resize.
- `ResizableJournal.cs:516` — `Draw` iterates `journalDatas` while `AddEntry` (called from the `JournalEntryAdded` event) can mutate the same deque.
- `StatusGump.cs:1511` — `Update` unconditionally writes `_labels[(int)MobileStats.X]` for stats whose labels are only created inside the `Client.Version >= CV_308Z` branch (:653). On older clients most `_labels` entries are null.
- `StatusGump.cs:1546` — `_labels[StatCap]`/`Luck`/`WeightCurrent`/`Gold`/`Damage`/`Followers` are written every tick but are only created inside the `>= CV_308Z` branch.
- `StatusGump.cs:1956` — Outlands layout reads `World.Player.Luck` / `StatsCap` / `ColdResistance` / `FireResistance` / `PoisonResistance` / `EnergyResistance` as stand-ins for hunger / murder count / timers; comments say `FIXME: packet handling`. Client display and server meaning disagree by construction.
- `StatusGump.cs:2058` — same substitution repeated in `Update`.
- `StatusGump.cs:630` — `Settings.Load` result is cast and, if null, a default is created and saved; the `_settings` static is shared across all `StatusGumpModern` instances and never invalidated.
- `StatusGump.cs:56` — the base ctor disposes the player's `HealthBarGump` as a "sanity check" every time any status gump is constructed.
- `PartyGump.cs:69` — `Update` runs every frame and indexes `World.Party.Members[i]` while `_healthBars[i]` may reference a `SimpleProgressBar` for a member who has since been replaced in that slot; the bar is only re-associated on `UpdateContents`.
- `PartyGump.cs:221` — health bars are only created for members already present in `World.Mobiles` at build time; a member who comes into range later never gets a bar until a rebuild.
- `PartyGump.cs:353` — `buttonID >= TellMember && buttonID < KickMember` then `buttonID >= KickMember` with no upper bound; `Members[index]` indexed with an unclamped index.
- `SkillProgressBar.cs:120` — `AddSkill` constructs the gump (which reads `UIManager.GetGump<WorldViewportGump>()` at :21 and dereferences it unguarded) before checking whether the queue should run at all.
- `SkillProgressBar.cs:21` — `vp.Location` dereferenced with no null check on `WorldViewportGump`.
- `SkillProgressBar.cs:108` — `Dispose` calls `QueManager.ShowNext()`, and `ShowNext`'s `Reset()` path (:138) disposes queued bars, which re-enters `Dispose` -> `ShowNext`; guarded only by the `beingReset` static flag (:130).
- `SkillProgressBar.cs:113` — `ConcurrentQueue` implies cross-thread use, but `UIManager.Add` at :150 is called from whichever thread dequeues.
- `DurabilityGump.cs:57` — `lastWidth/lastHeight/lastX/lastY` are static and shared; the last gump to close wins.
- `DurabilityGump.cs:215` — `Restore` does `int.TryParse(..., out X)` writing directly into the `X`/`Y`/`Width`/`Height` properties as `out` targets; a failed parse zeroes them.
- `DurabilityGump.cs:193` — `OnResize` calls `Build()` which calls `Clear()`, destroying and recreating children during the resize callback.
- `DurabilityGump.cs:134` — `World.DurabilityManager?.Durabilities` is enumerated with `OrderBy` while the manager may be updated by durability packets.
- `ModernOptionsGump.cs:1405` / `:4547` — nested loops walk `content.Children` and `scrollArea.Children` and mutate control `Y` while iterating; `:4555` calls `scrollChild.Remove(this)` on unrelated siblings.
- `ModernOptionsGump.cs:4594` — `Children.RemoveAt(i--)` inside a `for (i < Children.Count)` loop.
- `ModernOptionsGump.cs:1292` — `b.IsSelected = true` after the macro loop; if `macroManager.Items` is null the loop never runs and `b` is the "Delete Macro" button from :1223 (or unassigned-path variable reuse).
- `ModernOptionsGump.cs:2572` / `:2653` — `ButtonParameter = page + 1 + content.LeftArea.Children.Count`, so page ids depend on current child count and can collide after a delete.
- `ModernOptionsGump.cs:2595` — the nameplate "Delete entry" button reuses `(int)PAGE.Macros + 1001`, the same parameter as the macro delete button.
- `ModernOptionsGump.cs:2624` — deleting a nameplate option disposes the button but does not dispose the matching `NameOverheadAssignControl` on the right side.
- `ModernOptionsGump.cs:3520` — `httpClient.GetStringAsync(uri).Result` blocks inside a `Task.Factory.StartNew`; `SpellVisualRangeManager.Instance.LoadFromString` is then called off the main thread.
- `ModernOptionsGump.cs:3975` — synchronous recursive `Directory.GetDirectories` over all profile folders during gump construction (frame thread).
- `ModernOptionsGump.cs:4009` / `:4029` — button labels use `locations.Count - 1` / `sameServerLocations.Count - 1`, which is `-1` when the list is empty.
- `ModernOptionsGump.cs:4405` — `int.Parse(_cooldown.Text)` unguarded in the cooldown Save handler (the Preview handler at :4417 uses `TryParse`).
- `ModernOptionsGump.cs:2758` / `:4315` — adding or deleting a cooldown condition disposes the entire `ModernOptionsGump` and constructs a new one from inside a `MouseUp` handler on a child of the gump being disposed.
- `ModernOptionsGump.cs:4719` — `((SearchableOption)button)` hard cast on a `ModernButton`.
- `ModernOptionsGump.cs:5279` — `for (int i = 2; i < Children.Count; i++) Children[i]?.Dispose();` assumes the first two children are the combobox and remove button, and disposes while indexing a list that disposal mutates.
- `ModernOptionsGump.cs:5432` — `NameOverHeadManager.LastActiveNameOverheadOption.Replace("\\u0026", "&")` compared to `Option.Name` — string-identity matching of options (same pattern at NameOverHeadHandlerGump.cs:147 and :189).
- `TooltipConfigGump.cs:332` — `SaveWithDelay` starts a new `Task` + `Thread.Sleep(1500)` on *every* keystroke; all of them fire, each enqueueing a save closure that captures the control. No cancellation of the previous one.
- `TooltipConfigGump.cs:337` — the enqueued action runs later on the main thread and dereferences `searchTextInput` / `data`, which may have been disposed by `BuildTooltipData()` (:152) or `data.Delete()` (:309) in the meantime.
- `TooltipConfigGump.cs:171` — `AddNewTooltipRow` calls `ToolTipOverrideData.Get(index)` with `Count` as the index (i.e. one past the end) to create a new entry.
- `TooltipConfigGump.cs:30` — `OnResize` calls `Build()` -> `Clear()`, disposing children mid-resize.
- `AssistantGump.cs:411` — journal filter edit does Remove(old)+Add(new) on every keystroke; `currentFilter` tracks the last committed text, so a fast edit can leave orphan entries.
- `AssistantGump.cs:1030` / `:1683` — `_dataBox.Insert(2, …)` / `Insert(3, …)` assume a fixed number of header children at the front of the DataBox.
- `AssistantGump.cs:1170` / `:642` / `:817` — delete handlers call `area.Dispose()` without removing the area from its parent container in the AutoLoot / DressAgent / FriendsList cases (the Buy/Sell cases at :1416 and :1647 do call `_container.Remove`).
- `AssistantGump.cs:317` / `:328` — the `HideHudFlags.All` checkbox disposes `this` and adds a replacement `AssistantGump` from inside its own value-changed callback.
- `AssistantGump.cs:944` — `SetSpellRangeInfo` sets `id = -1; spellRangeInfo = null` first and restores them last specifically so intermediate `SetText` callbacks don't write to the previous spell; any `Save()` between those points is silently dropped.
- `AssistantGump.cs:1111` — autoloot graphic parses `0x…` into a **`short`** (`short.TryParse` with `AllowHexSpecifier`) then assigns to an `int Graphic`; graphics above 0x7FFF parse negative or fail.
- `LoginGump.cs:660-663` — `PasswordStbTextBox` hides base fields with `new`; base-class code paths still see the base `_rendererText`. `RealText` is the plaintext password held in `Text` (:709).
- `LoginGump.cs:405` — `UIManager.ContextMenu.X/Y` written immediately after `Show()`, assuming `Show()` installed this menu as the global one.
- `NameOverHeadHandlerGump.cs:154` — `RedrawOverheadOptions` calls `Remove(button)` for each old button but never clears `_overheadButtons`, so the list grows and `UpdateCheckboxes` (:143) touches removed controls.
- `NameOverHeadHandlerGump.cs:193` — `AddOverheadOptionButton` calls `NameOverHeadManager.SetActiveOption(option)` as a side effect of *building* the UI.
- `DiscordFriendListControl.cs:48` — `friends.OrderBy(u => u.User()?.IsOnline() != true)` calls `User()` twice per element (again at :50); ordering is evaluated against live Discord SDK state.
- `DiscordFriendListControl.cs:41` — `_friendList.Clear()` disposes the item controls; anything holding a `DiscordUserListItem` from a previous build is stale.
- `QuestionGump.cs:47` — full-screen `AlphaBlendControl` sized from `Client.Game.Window.ClientBounds` at construction; it does not follow a window resize.
- `UserMarkerGump.cs:37-38` — `_mapMaxX/_mapMaxY` captured from `World.MapIndex` at construction; a map change while the dialog is open validates against the old map.
- `UserMarkerGump.cs:275` — `AddNewMarker` silently returns if the `.usr` file does not exist.
- `UserMarkerGump.cs:288` — synchronous `File.AppendAllText` on the frame thread.
- `CreditsGump.cs:66` — `if (_lastUpdate < Time.Ticks)` where `_lastUpdate` is `uint` and `Time.Ticks` is compared directly; the initial `_lastUpdate = 0` makes the first frame always scroll.

## Fork deltas

Clearly TazUO or Holiday additions (not stock ClassicUO):

- `AssistantGump.cs` in its entirety — autoloot, sell/buy agents, mobile graphic filter, spell bar, HUD-hide flags, spell indicators, journal filters, title-bar stats, dress agent, bandage agent, friends list, organizer. All wiki links point at `github.com/PlayTazUO/TazUO/wiki`.
- `ModernOptionsGump.cs` — replaces stock `OptionsGump`. The whole `PAGE.TUOOptions` tree (grid containers, journal, modern paperdoll, nameplates, gump scaling, hidden layers, controller support, settings transfers, font settings, hotkey reference) is fork content, as is `PAGE.TUOCooldowns`.
  - Holiday-specific, marked by the distinctive prose comments: the `BuildExperimental` scroll-area rewrite and its comment at :2271-2273; `LogMusicIndices` (:2342) and `LogHouseDiagnostics` (:2358) diagnostics; `MusicEra` folder-driven combobox (:2403-2437); `MusicMapMode` / `IgnoreServerStopMusic` / `MusicOverlay` (:2448-2510); `KeepHouseContentsLoaded` (:2395); the trailing 60px spacer with its comment (:2515-2525).
  - Holiday: the classic-bitmap `ChatInputFont` selector with comment "The line you type into is a classic bitmap font, not a TTF one" (:3791-3831).
  - Holiday/TazUO: `ForceHouseTransparency` / `ForcedTransparencyHouseTileHue` / `ForcedHouseTransparency` block (:3621-3659).
- `ResizableJournal.cs` — TazUO journal replacement (stock has `JournalGump`). Holiday: `InitJournalEntries` skip-to-newest optimisation with its comment at :425-431, tied to the raised `Constants.MAX_JOURNAL_HISTORY_COUNT` used at :3020.
- `DurabilityGump.cs`, `SkillProgressBar.cs`, `TooltipConfigGump.cs`, `InputRequest.cs`, `GridHighLight/GridHighLightProfile.cs`, `DiscordGump/DiscordFriendListControl.cs` — no ClassicUO license header, TazUO-style code, all fork additions.
- `GumpType.cs` — stock enum plus fork values `NameOverHeadHandler`, `ScriptManager`, `MacroButtonEditor = 6464`, `DurabilityGump = 6465`, `GridContainer = 8787`, `NearbyCorpseLoot`, `SpellBar`, `MenuGump`, `TextEntryDialogGump`.
- `PartyGump.cs` — stock ClassicUO header retained, but rebased onto `NineSliceGump` with `ModernUIConstants.ModernUIPanel`, TTF labels, and per-slot `SimpleProgressBar` health bars; OK/Cancel buttons removed (comment at :275).
- `LoginGump.cs` — stock, plus TazUO version label (:177, :278), TazUO Wiki / TazUO Discord hit-boxes (:479-497), login-music checkbox+slider (:499-546), and `SimpleAccountManager` right-click account picker (:391-409).
- `StatusGump.cs` — stock three-variant gump, plus the `StatusGumpModern.Settings : UISettings` skinning layer (:1604) and the `CUOEnviroment.IsOutlands` branch in the factory (:166, :189).
- `NameOverHeadHandlerGump.cs` — stock base plus the hide-at-full-hp / warmode-only checkboxes (:98-131) and the nameplate search box (:135-138).
- `MarkersManagerGump.cs` / `UserMarkerGump.cs` — stock ClassicUO world-map marker tooling, no fork header; largely unchanged.
- `ColorPickerGump.cs`, `QuestionGump.cs`, `CreditsGump.cs`, `CharCreation/CreateCharAppearanceGump.cs` — stock ClassicUO with license headers; only small deltas (e.g. `okClicked` callback on `ColorPickerGump`, `IsModal` + dim layer on `QuestionGump`).
