# ui-gumps-1

Partition = every 6th file (NR%6==1) of `src/ClassicUO.Client/Game/UI/Gumps/**/*.cs`, sorted.
21 files, 7525 lines, all read in full.

Base path for all relative paths below: `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/UI/Gumps/`

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `AnchorableGump.cs` | 258 | Abstract base for gumps that snap into anchor groups (spell/macro buttons, health bars). Draws lock icon + drop preview. |
| `BuffGump.cs` | 383 | Classic buff-icon bar. Rebuilds all icon controls on every content update; per-icon countdown text + alpha pulse. |
| `CharCreation/CreateCharTradeGump.cs` | 369 | Char-creation step 3: stat sliders (str/dex/int) and 3 skill comboboxes + sliders, expansion-filtered skill list. |
| `ContainerGump.cs` | 823 | Classic (non-grid) container window. Item icon layout, drag-drop math, minimize, corpse eye animation. |
| `DiscordGump/DiscordChannelListControl.cs` | 111 | Scrollable Discord lobby/channel list; rebuilds wholesale on lobby create/delete events. |
| `DiscordGump/DiscordUserListItem.cs` | 134 | One user row in the Discord user list; online dot, hover popup after 1250 ms, click selects DM channel. |
| `GridHighLight/GridHighLightConfig.cs` | 84 | 6-column editor of the global property keyword sets used by grid highlighting; debounced async save. |
| `GridHighLight/GridHightlightMenu.cs` | 265 | List editor for per-profile grid highlight rules (color, properties, reorder, delete) + JSON import/export. |
| `IgnoreManagerGump.cs` | 233 | Ignore-list window; target a player to add, per-row remove button, saves XML on dispose. |
| `Login/CharacterSelectionGump.cs` | 441 | Character selection screen; select/login/delete/new, keyboard + controller + mouse-wheel navigation. |
| `MacroButtonGump.cs` | 304 | Draggable anchorable button that runs a macro; graphic/hue/scale/label from `Macro`. |
| `MiniMapGump.cs` | 480 | Radar minimap; rasterizes map blocks into a static pixel buffer and uploads to the shared gump texture each Draw. |
| `MultiItemMoveGump.cs` | 428 | Multi-select item mover: static selection set + queue, drains one item per `ObjDelay` ms into `MoveItemQueue`. |
| `NetworkStatsGump.cs` | 209 | Ping / bytes-in / bytes-out overlay, refreshed every 100 ms, ping-colored text. |
| `ProfileGump.cs` | 252 | Paperdoll profile scroll; editable body text, sends `Send_ProfileUpdate` on dispose if text changed. |
| `RacialAbilitiesBookGump.cs` | 381 | Racial ability book (human/elf/gargoyle); index pages + icon pages, drag icon out to make a `RacialAbilityButton`. |
| `SimpleTimedTextGump.cs` | 42 | Floating text gump that disposes itself from `Draw` once wall-clock `expireAt` passes. |
| `SpellbookGump.cs` | 1583 | All spellbook types (magery…mastery); index pages, spell icon pages, drag to `UseSpellButtonGump`, ctrl+alt fast macro assign. |
| `TextContainerGump.cs` | 92 | Abstract base adding a `TextRenderer` overlay (journal-style floating text) to a gump; `ContainerGump` derives from it. |
| `UpdateTimerViewer.cs` | 74 | Debug gump listing average `Update()` cost per control type; refreshes every 2000 ms. |
| `WorldViewportGump.cs` | 579 | The game world frame: border, resize grip, system chat host, camera bounds ownership, low-HP red outline. Also `BorderControl`. |

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `ANCHOR_TYPE` (enum NONE/SPELL/HEALTHBAR/DISABLED) | AnchorableGump.cs:43 | Anchor group compatibility tag. |
| `AnchorableGump` (public abstract) | AnchorableGump.cs:51 | Anchoring behavior; `GroupMatrixWidth/Height`, `WidthMultiplier/HeightMultiplier`, `ShowLock`. |
| `BuffGump` (internal) | BuffGump.cs:48 | Buff bar; `GumpType.Buff`; saves `graphic`+`direction` in XML. |
| `BuffGump.GumpDirection` (enum) | BuffGump.cs:234 | 4 layouts keyed off graphics 0x757F–0x7582. |
| `BuffGump.BuffControlEntry : GumpPic` | BuffGump.cs:242 | One buff icon; holds `BuffIcon`, `RenderedText _gText`, alpha pulse. |
| `CreateCharTradeGump` (internal) | CharCreation/CreateCharTradeGump.cs:47 | Skill/stat picker; mutates the in-flight `PlayerMobile`. |
| `ContainerGump : TextContainerGump` | ContainerGump.cs:49 | `GumpType.Container`; `Graphic`, `IsMinimized`, `IsChessboard`. |
| `ContainerGump.GumpPicContainer : GumpPic` | ContainerGump.cs:806 | Hit test divided by `UIManager.ContainerScale`. |
| `DiscordChannelListControl : Control` | DiscordGump/DiscordChannelListControl.cs:8 | Channel list; `_currentChanIdList` dedupe set. |
| `DiscordUserListItem : Control` | DiscordGump/DiscordUserListItem.cs:12 | User row; caches `sx/sy/sw/sh` dot rect and `shue` at build time. |
| `GridHighlightConfig : Gump` | GridHighLight/GridHighLightConfig.cs:11 | 6 keyword-set text areas. |
| `GridHighlightMenu : NineSliceGump` | GridHighLight/GridHightlightMenu.cs:13 | Rule list; static `Open()`, `ExportGridHighlightSettings`, `ImportGridHighlightSettings`. |
| `IgnoreManagerGump : Gump` (sealed) | IgnoreManagerGump.cs:13 | Ignore list UI. |
| `IgnoreManagerGump.IgnoreListControl : Control` | IgnoreManagerGump.cs:208 | One name row + remove button, raises `RemoveMarkerEvent`. |
| `CharacterSelectionGump : Gump` | Login/CharacterSelectionGump.cs:49 | Char select; `_selectedCharacter` is an index into `LoginScene.Characters`. |
| `CharacterSelectionGump.CharacterEntryGump : Control` | Login/CharacterSelectionGump.cs:368 | Row; `CharacterIndex`, `Hue` proxies label hue. |
| `MacroButtonGump : AnchorableGump` | MacroButtonGump.cs:46 | `GumpType.MacroButton`; `TheMacro`, `Graphic`, `Hue`, `Scale`, `HideLabel`. |
| `MiniMapGump : Gump` | MiniMapGump.cs:49 | `GumpType.MiniMap`; `ToggleSize`, pixel-accurate `Contains`. |
| `MultiItemMoveGump : NineSliceGump` | MultiItemMoveGump.cs:16 | Static selection/queue + per-instance UI; `ShowNextTo`, `TrySelect`, `ToggleItem`, `IsSelected`, `OnContainerTarget`, `OnTradeWindowTarget`. |
| `MultiItemMoveGump.ProcessType` (enum) | MultiItemMoveGump.cs:420 | None/Container/Ground/TradeWindow. |
| `NetworkStatsGump : Gump` | NetworkStatsGump.cs:45 | `GumpType.NetStats`; `IsMinimized`. |
| `ProfileGump : Gump` | ProfileGump.cs:41 | Profile scroll; `IsMinimized`. |
| `RacialAbilitiesBookGump : Gump` | RacialAbilitiesBookGump.cs:44 | Racial book; static name arrays at :46/:47/:53. |
| `SimpleTimedTextGump : Gump` | SimpleTimedTextGump.cs:10 | Self-expiring text. |
| `TextContainerGump` (public abstract) | TextContainerGump.cs:39 | Owns a `TextRenderer`; `AddText(TextObject)` stamps `msg.Time = Time.Ticks + 4000`. |
| `SpellbookGump : Gump` | SpellbookGump.cs:50 | All spellbooks; `SpellBookType`, `IsMinimized`, `GumpType.SpellBook`. |
| `SpellbookGump.ButtonCircle` (enum) | SpellbookGump.cs:1453 | Magery circle jump buttons. |
| `SpellbookGump.HueGumpPic : GumpPic` | SpellbookGump.cs:1461 | Spell icon; hue 38 while `World.ActiveSpellIcons.IsActive(_spellID)`; ctrl+alt = fast macro assign. |
| `UpdateTimerViewer : Gump` | UpdateTimerViewer.cs:11 | Profiler view; flips `UIManager.UpdateTimerEnabled`. |
| `WorldViewportGump : Gump` | WorldViewportGump.cs:53 | Game window frame; owns `_scene.Camera.Bounds`. |
| `BorderControl : Control` (public) | WorldViewportGump.cs:361 | 9-piece tiled border with per-edge/corner graphic overrides + hue. |

## State

Static / global:
- `MiniMapGump._blankGumpsPixels` — `static readonly uint[4][]`, MiniMapGump.cs:57. Slots 0/1 = pristine small/large gump pixels, 2/3 = scratch buffers. Shared by every MiniMapGump instance; only allocated once, never invalidated on map change.
- `NetworkStatsGump._last_position` — `static Point`, NetworkStatsGump.cs:47. Remembers window position across gump instances.
- `MultiItemMoveGump.MoveItems` — `public static readonly ConcurrentQueue<Item>`, MultiItemMoveGump.cs:50.
- `MultiItemMoveGump._selected` — `static ConcurrentDictionary<uint,byte>` keyed by item serial, MultiItemMoveGump.cs:51.
- `MultiItemMoveGump.ObjDelay` (public static int = 1000), `processing`, `processType`, `_lastMoveTick`, `tradeId`, `containerId`, `groundX/Y/Z` — MultiItemMoveGump.cs:55-60. All process state is static, not per-gump.
- `IgnoreManagerGump._scrollArea` — `private static ScrollArea`, IgnoreManagerGump.cs:23. Static field on a per-instance gump.
- `WorldViewportGump.damageWindowOutline` (static Texture2D) and `DamageWindowOutlineHue` (public static Vector3) — WorldViewportGump.cs:64-65. `.Z` mutated per frame in Draw (:330).
- `RacialAbilitiesBookGump._humanNames/_elfNames/_gargoyleNames` — static readonly string[], :46/:47/:53.
- `UIManager.SystemChat` assigned from `WorldViewportGump` ctor, WorldViewportGump.cs:113.

Per-instance mutable state worth noting:
- `AnchorableGump._anchorCandidate`, `_prevX/_prevY` — AnchorableGump.cs:53-57. `_prevX/_prevY` only refreshed in `OnMouseDown`/`OnMove`.
- `BuffGump._graphic/_direction/_box/_background` — BuffGump.cs:50-54; `BuffControlEntry._alpha`, `_decreaseAlpha`, `_updateTooltipTime` (:244-247).
- `ContainerGump._data` (ContainerData struct copy), `_corpseEyeTicks`, `_eyeCorspeOffset`, `_isMinimized`, `firstItemsLoaded` — ContainerGump.cs:51-61. `firstItemsLoaded` is set but never read (:673-676).
- `SpellbookGump._spells` — `readonly bool[64]`, SpellbookGump.cs:60. Never cleared between `CreateBook()` calls.
- `SpellbookGump._maxPage`, `_enqueuePage` (:55,:61); `RacialAbilitiesBookGump._enqueuePage` (:60).
- `MiniMapGump._x/_y` last player coords, `_lastMap`, `_timeMS`, `_draw`, `_useLargeMap` — :51-56.
- `WorldViewportGump._lastSize/_savedSize/_clicked` — :58-59.
- `CharacterSelectionGump._selectedCharacter` (uint index), `chars` array — :53-54.
- `GridHighlightConfig` per-category `CancellationTokenSource cts` captured in the `TextChanged` closure — GridHighLight/GridHighLightConfig.cs:50.

## Timing

Per frame (`Update()`, called by UIManager once per game tick per gump):
- `ContainerGump.Update` :520 — nulls out / disposes when the backing `Item` is gone; sets `SelectedObject.SelectedContainer`; corpse eye toggles on a **750 ms** timer (:547-555).
- `MiniMapGump.Update` :107 — recreates map on `World.MapIndex` change; `_draw` blink toggles every **500 ms** (:120-124).
- `NetworkStatsGump.Update` :101 — refreshes stats + re-measures window every **100 ms** (:105-107).
- `MultiItemMoveGump.Update` :309 — dequeues at most **one** item per frame, gated by `ObjDelay` (default 1000 ms, profile `MoveMultiObjectDelay`) and by `ItemHold.Enabled`.
- `UpdateTimerViewer.Update` :64 — rebuilds list every **2000 ms** (`UPDATE_INTERVAL` :13).
- `SpellbookGump.Update` :1323 / `RacialAbilitiesBookGump.Update` :366 — deferred page flip once `Time.Ticks - Mouse.LastLeftButtonClickTime >= Mouse.MOUSE_DELAY_DOUBLE_CLICK`.
- `BuffGump.BuffControlEntry.Update` :279 — tooltip/label text refreshed at most 1/sec (`_updateTooltipTime + 1000`, :301); alpha pulse only inside the last **10000 ms** of the buff; at `delta <= 0` calls `((BuffGump)Parent.Parent)?.RequestUpdateContents()` (:320).
- `DiscordUserListItem.Update` :62 — opens `DiscordUserPopupGump` **1250 ms** after `OnMouseEnter` (:52).
- `ProfileGump.Update` :197 — recomputes scroll/databox geometry every frame.
- `WorldViewportGump.Update` :134 — while `Mouse.IsDragging`, recomputes camera bounds and calls `Resize()`.
- `TextContainerGump.Update` :60 — drives `TextRenderer.Update()`.

Per frame (`Draw()`):
- `MiniMapGump.Draw` :143 — calls `CreateMiniMapTexture` (:230), which rasterizes up to (maxBlockX-minBlockX+1)×(maxBlockY-minBlockY+1) 8×8 map blocks and does `SetDataPointerEXT` on the **shared gump atlas texture** (:413-416). Early-outs if player X/Y unchanged (:239-247). Also draws a dot per `World.Mobiles.Values` entry (:176).
- `SimpleTimedTextGump.Draw` :34 — disposes itself when `DateTime.Now >= expireAt` (wall clock, not `Time.Ticks`).
- `MultiItemMoveGump.Draw` :385 — disposes itself if `SelectedCount == 0 || MoveItems.IsEmpty`.
- `WorldViewportGump.Draw` :322 — reads `World.Player.Hits/HitsMax` and draws 4 outline bars when below `ShowHealthIndicatorBelow`.
- `AnchorableGump.Draw` :154 — lock icon + anchor drop preview rectangle.
- `SpellbookGump.HueGumpPic.Update` :1488 — re-checks `World.ActiveSpellIcons` every frame.

On packet / server event (via `RequestUpdateContents()` → `UpdateContents()` next frame):
- `BuffGump.UpdateContents` :155 → full `BuildGump()` rebuild of every icon.
- `ContainerGump.UpdateContents` :558 → `Clear(); BuildGump(); IsMinimized = IsMinimized; ItemsOnAdded();`.
- `SpellbookGump.UpdateContents` :830 → `AssignGraphic` + `CreateBook()`.
- `MiniMapGump.UpdateContents` :225 → `CreateMap()`.

On input:
- Anchoring (`OnMove`/`OnMouseDown`/`OnMouseOver`/`OnDragEnd`) AnchorableGump.cs:71-130.
- `ContainerGump.OnMouseUp` :287 — the whole drag-drop / targeting / delayed-double-click path.
- `CharacterSelectionGump.OnMouseWheel` :244, `OnKeyDown` :236, `OnControllerButtonUp` :226.

On load / open / close:
- `ContainerGump` ctor :67 — chooses backpack graphic by `ProfileManager.CurrentProfile.BackpackStyle`, plays `_data.OpenSound`.
- `ContainerGump.Restore` :579 and `SpellbookGump.Restore` :119 — do **not** restore; they queue `DoubleClickDelayed(LocalSerial)` and `Dispose()`.
- `WorldViewportGump` ctor :125-131 — one-shot: shows `VersionHistory` and calls `LegionScripting.DownloadAPIPy()` when `LastVersionHistoryShown != CUOEnviroment.Version`.
- `ProfileGump.Dispose` :243 — sends `Send_ProfileUpdate` if text changed.
- `IgnoreManagerGump.Dispose` :139 — `IgnoreManager.SaveIgnoreList()` + `TargetManager.CancelTarget()`.
- `SpellbookGump.Dispose` :198 — plays sound, `UIManager.SavePosition`.

Async / off-frame:
- `GridHighlightConfig` `TextChanged` handler :51 — `async void`-style lambda; `await Task.Delay(500)` then writes `ProfileManager.CurrentProfile.ConfigurableProperties` and saves. Continuation is not guaranteed to be on the frame thread unless a sync context is installed.
- `GridHighlightMenu` import/export :208/:232 — `File.ReadAllText`/`WriteAllText` on the calling (frame) thread via `FileSelector.ShowFileBrowser` callback.

## Inbound

- `UIManager` drives every gump: `Update()`, `Draw()`, `Contains()`, `OnButtonClick()`, mouse/keyboard dispatch, `UIManager.Add`, `UIManager.GetGump<T>()`, `UIManager.SavePosition`.
- `UIManager.AnchorManager` calls into `AnchorableGump` via `GroupMatrixWidth/Height`, `WidthMultiplier/HeightMultiplier`, `AnchorType`, `TryAttacheToExist()`.
- Gump XML persistence layer calls `Save(XmlTextWriter)` / `Restore(XmlElement)` on `BuffGump`, `ContainerGump`, `MacroButtonGump`, `MiniMapGump`, `NetworkStatsGump`, `SpellbookGump`.
- Packet handlers / `World` call `RequestUpdateContents()` on BuffGump, ContainerGump, SpellbookGump, MiniMapGump.
- `TargetManager` cursor-target callbacks land in `MultiItemMoveGump.OnContainerTarget(uint)` :256, `OnContainerTarget(int,int,int)` :271, `OnTradeWindowTarget(uint)` :276; and `CursorTarget.IgnorePlayerTarget` feeds `IgnoreManagerGump` (:203).
- `GridContainer` calls `MultiItemMoveGump.TrySelect` / `ToggleItem` / `IsSelected` / `ShowNextTo` / `AddMultiItemMoveGumpToUI`.
- `GameScene` constructs `WorldViewportGump(scene)` and reads back via `ResizeGameWindow`, `SetGameWindowPosition`.
- `LoginScene` constructs `CharacterSelectionGump`; `CharCreationGump` constructs `CreateCharTradeGump`.
- `DiscordManager.OnLobbyCreated/OnLobbyDeleted` events invoke `DiscordChannelListControl` handlers (:26-27).
- `GridHighlightMenu.Open()` (static, :202) is the entry point used by options/macros.

## Outbound

- `World.*` — `World.Items.Get`, `World.Get`, `World.Player` (BuffIcons, Race, TithingPoints, Hits/HitsMax, ManualOpenedCorpses/AutoOpenedCorpses, FindItemByLayer), `World.Mobiles.Values`, `World.Map.GetIndex/GetChunk/BlocksCount`, `World.OPL.TryGetNameAndData`, `World.ActiveSpellIcons`, `World.ClientFeatures`, `World.ClientLockedFeatures`.
- `NetClient.Socket` — `Send_ProfileUpdate` (ProfileGump.cs:247), `Send_ToggleGargoyleFlying` (RacialAbilitiesBookGump.cs:225), `Send_GameWindowSize` (WorldViewportGump.cs:100), `Statistics.*` (NetworkStatsGump.cs:111-113).
- `GameActions` — `DropItem` (ContainerGump.cs:493), `CastSpell` (SpellbookGump.cs:854,1315), `OpenMacroGump` (SpellbookGump.cs:1569), `Print` (MultiItemMoveGump, GridHighlightMenu).
- `MoveItemQueue.Instance.Enqueue` (MultiItemMoveGump.cs:337/347/352).
- `TargetManager` — `SetTargeting`, `Target`, `CancelTarget`, `IsTargeting`, `TargetingState`.
- `Client.Game.Gumps.GetGump` / `Client.Game.Arts.GetArt` — texture lookups returning `ref readonly` sprite info.
- `Client.Game.Audio.PlaySound` — 0x0055 (book page), 0x0051 (drop fail), container open/close sounds.
- `Client.Game.GameCursor.ItemHold` — Enabled / IsFixedPosition / Serial / Graphic / DisplayedGraphic / MouseOffset.
- `Client.Game.GetScene<GameScene>()` — `Macros` (MacroManager), `DoubleClickDelayed`, `UpdateDrawPosition`.
- `ProfileManager.CurrentProfile` — very wide: HoldAltToMoveGumps, HoldDownKeyAltToCloseAnchored, CloseAllAnchoredGumpsInGroupWithRightClick, BuffBarTime, BackpackStyle, HueContainerGumps, SkipEmptyCorpse, ScaleItemsInsideContainers, RelativeDragAndDropItems, HighlightContainerWhenSelected, OverrideContainerLocation(+Setting/Position), CastSpellsByOneClick, FastSpellsAssign, MoveMultiObjectDelay, SetFavoriteMoveBagSerial, GridHighlightSetup, ConfigurableProperties, GameWindowLock, EnableHealthIndicator, ShowHealthIndicatorBelow, HealthIndicatorWidth, LastVersionHistoryShown, OverheadChatFont(+Size).
- `ContainerManager.Get(graphic)` — `ContainerData` (bounds, sounds, minimizer area, iconized graphic).
- `GridHighlightData` / `GridHighlightRules` — `GetGridHighlightData`, `RecheckMatchStatus`, `SaveGridHighlightConfiguration`, `AllConfigs`.
- `IgnoreManager` — `IgnoredCharsList`, `RemoveIgnoredTarget`, `SaveIgnoreList`.
- `DiscordManager.Instance` — `GetLobbies`, `GetChannel`, `MessageHistory`, `GetUserhue`, lobby events.
- `LegionScripting.LegionScripting.DownloadAPIPy()` (WorldViewportGump.cs:130).
- `HuesLoader`, `MapLoader`, `TileDataLoader`, `ClilocLoader`, `SkillsLoader`, `FontsLoader`, `ExternalImageLoader`.
- Spell tables: `SpellsMagery/Necromancy/Chivalry/Bushido/Ninjitsu/Spellweaving/Mysticism/Mastery`, `SpellBookDefinition`.
- Creates other gumps: `InspectorGump`, `GridContainer`, `UseSpellButtonGump`, `RacialAbilityButton`, `DiscordUserPopupGump`, `GridHighlightConfig`, `GridHighlightProperties`, `RGBColorPickerGump`, `VersionHistory`, `LoadingGump`, `MultiItemMoveGump`.

## Hazards

- `SpellbookGump.cs:60` + `:238` — `_spells` (bool[64]) is never reset at the top of `CreateBook()`; a spell removed from the book server-side stays `true` for the life of the gump, so `_maxPage` / page layout keeps counting it.
- `SpellbookGump.cs:222-229` — `CreateBook()` disposes the gump mid-build when the item is gone, after `_dataBox.Clear()` already ran.
- `SpellbookGump.cs:1327-1332` — `Update()` calls `Dispose()` then continues into the `IsDisposed` check; `base.Update()` already ran on a gump whose item vanished.
- `SpellbookGump.cs:755-773` — `spellDef` from `GetSpellDefinition(iconSerial)` is used for `spellDef.ID`/`.Name` without a null check even though `GetSpellDefinition` can return null (:914 initializes `def = null`).
- `SpellbookGump.cs:1274` — `SetActivePage` compares against `_dataBox.ActivePage` and early-returns; `CreateBook` ends with `SetActivePage(1)` (:827), which is a no-op if the databox is already on page 1, leaving `_pageCornerLeft/Right.Page` stale after a rebuild.
- `BuffGump.cs:320` — `((BuffGump)Parent.Parent)?.RequestUpdateContents()` from inside a child's `Update()`; the parent's `UpdateContents` calls `BuildGump()` which does `Clear()` on the collection currently being iterated by the UI update loop.
- `BuffGump.cs:80-83` — `BuildGump` calls `_box?.Clear()` then `Clear()`, disposing the old `_box` while `UpdateElements()` (:160) later indexes `_box.Children`; `_box` is reassigned at :122 in between.
- `BuffGump.cs:126-129` — iterates `World.Player.BuffIcons` (server-owned dictionary) while constructing controls; a buff packet arriving during this walk mutates the same dictionary.
- `ContainerGump.cs:562` — `IsMinimized = IsMinimized;` self-assignment relies on the setter having no equality guard (the guard at :161 is commented out); each call multiplies `Width`/`Height` by `GetScale()` again (:167-168).
- `ContainerGump.cs:236-237` and `:553-554` — `Width = _gumpPicContainer.Width = (int)(_gumpPicContainer.Width * scale)` and the eye pic equivalent compound-scale the already-scaled value on every rebuild / every 750 ms tick.
- `ContainerGump.cs:207-220` — `BuildGump()` disposes `_gumpPicContainer`/`_hitBox` and re-adds new ones; `UpdateContents` (:558) already called `Clear()`, so the dispose targets are stale references.
- `ContainerGump.cs:610` — walks `container.Items` (`LinkedObject` chain owned by `World`) building controls; the chain is mutated by container-content packets.
- `ContainerGump.cs:707-723` — `CheckItemControlPosition` writes back into `item.X`/`item.Y`, i.e. the client overwrites server-supplied item coordinates.
- `ContainerGump.cs:673-676` — `firstItemsLoaded` is written and never read.
- `MiniMapGump.cs:92-100` — `_blankGumpsPixels` is filled once from the gump atlas and shared statically; a second MiniMapGump, or an atlas re-pack, reuses the first instance's snapshot.
- `MiniMapGump.cs:413-416` — `SetDataPointerEXT` writes into `gumpInfo.Texture`, the shared gump atlas texture, from `Draw()`; :163 and :167 draw the same texture before and after the upload in the same frame.
- `MiniMapGump.cs:271-272` — `World.Map.BlocksCount` / `MapLoader.Instance.MapBlocksSize[World.MapIndex,1]` read inside `Draw`; `_lastMap` is only reconciled in `Update` (:114), so a map change between Update and Draw indexes with the old map's dimensions.
- `MiniMapGump.cs:176` — `foreach (Mobile mob in World.Mobiles.Values)` during `Draw`; mobile add/remove from packet processing mutates the same dictionary.
- `MiniMapGump.cs:236` — `CreateMiniMapTexture` dereferences `World.Player` with no null check; `Draw` has no `World.InGame` guard (only `Update` does, :109).
- `MiniMapGump.cs:471-473` — `Contains` indexes `_blankGumpsPixels[index]` after a length check but with no null check; slot is null until `CreateMap()` has run.
- `MultiItemMoveGump.cs:50-60` — all selection and processing state is `static`; disposing and reopening the gump does not reset it, and `ClearAll()` (:404) is only reached from Cancel or the auto-close path.
- `MultiItemMoveGump.cs:327` — `MoveItems.TryDequeue(out Item moveItem)` then `moveItem.Serial` with no null/destroyed check; the queued `Item` reference can outlive the world object.
- `MultiItemMoveGump.cs:388-393` — `Draw()` disposes the gump when the selection empties; disposal happens inside the UI draw walk.
- `MultiItemMoveGump.cs:378` — `MoveItems.IsEmpty && SelectedCount == 0` — the queue and the dictionary are two containers that can disagree (`ToggleItem` removes from `_selected` but leaves the stale entry in the queue, :233).
- `MacroButtonGump.cs:114-128` — `Scale` setter does `Width = (int)(Width * factor)`, compounding on each assignment; `TheMacro` setter (:88) sets `Scale` then `Graphic`, and `Graphic` (:130) recomputes Width from `DEFAULT_WIDTH`, so the ordering of the two setters determines the final size.
- `MacroButtonGump.cs:57` — `_gText` (`RenderedText`) is created in `BuildGump` and never destroyed; there is no `Dispose()` override (contrast `BuffGump.BuffControlEntry.Dispose` :376).
- `MacroButtonGump.cs:283` — `LocalSerial = (uint)macroid + 1000` assigned inside `Save()`; the gump's identity changes at save time and is derived from the macro's index in `GetAllMacros()`, which shifts when macros are added/removed/reordered.
- `MacroButtonGump.cs:212-214` — `RunMacro` calls `gs.Macros.Update()` directly from a mouse handler, i.e. an extra macro tick outside the normal frame order.
- `AnchorableGump.cs:56,82-83` — `_prevX/_prevY` are only refreshed in `OnMove`/`OnMouseDown`; a programmatic `Location =` change (e.g. `SetInScreen`) makes the next `OnMove` delta wrong for `UpdateLocation`.
- `AnchorableGump.cs:73` and `:239` — `ProfileManager.CurrentProfile` dereferenced without null check.
- `WorldViewportGump.cs:65,330` — `DamageWindowOutlineHue` is a public static `Vector3` whose `.Z` is mutated inside `Draw`; every reader shares the last frame's alpha.
- `WorldViewportGump.cs:326` — `World.Player.Hits` read after only `World.InGame` is checked.
- `WorldViewportGump.cs:143-190` — camera bounds are written from `Update()` while dragging; `Resize()` (:244) also writes `_scene.Camera.Bounds`, so the gump is the authoritative owner of camera geometry and any other writer in the same frame is overwritten.
- `WorldViewportGump.cs:113` — the ctor assigns the global `UIManager.SystemChat`; constructing a second WorldViewportGump silently reparents system chat.
- `IgnoreManagerGump.cs:23` — `_scrollArea` is `static` but written per instance in `DrawArea()` (:155); `Redraw()` (:186) removes whatever the static currently points at.
- `IgnoreManagerGump.cs:229` — `RemoveMarkerEvent.Raise()` on a possibly-null event (extension method; behavior depends on `Raise`'s null handling).
- `IgnoreManagerGump.cs:20` — `Client.Game.Scene.Camera.Bounds.Width` read in a field initializer, i.e. before the ctor body and independent of later resizes.
- `IgnoreManagerGump.cs:162-168` — the row-remove handler (`MarkerRemoveEventHandler` → `Redraw`) disposes/rebuilds the scroll area from inside a button click on a child of that area.
- `CharacterSelectionGump.cs:249-268` — `OnMouseWheel` indexes `chars[...]`; if `chars` is empty (`loginScene.Characters` all empty), `chars[chars.Length - 1]` is out of range.
- `CharacterSelectionGump.cs:304` — `loginScene.Characters[_selectedCharacter]` indexes by a stale index; `_selectedCharacter` is an index into the array as it was at construction, and `DeleteCharacter` mutates the server-side list.
- `CharacterSelectionGump.cs:344` — `SelectCharacter` re-hues via `FindControls<CharacterEntryGump>()` while `chars` (:158) is a separate snapshot array used by the wheel handler.
- `CreateCharTradeGump.cs:320` — `_character.Skills[_skillList[...SelectedIndex].Index]` — indexes the `Skills` array by the skill's tiledata index with no bounds check.
- `CreateCharTradeGump.cs:328-330` — assigns slider 1 to `Intelligence` and slider 2 to `Dexterity` while the labels are added in the order strength/dexterity/intelligence (:104-124).
- `CreateCharTradeGump.cs:303,308` — `UIManager.GetGump<CharCreationGump>()` result used without null check in the `Prev` branch (the `ValidateValues` path at :349 does use `?.`).
- `GridHighLight/GridHighLightConfig.cs:51-78` — `async` lambda captures the mutable local `cts` shared across invocations; `oldToken?.Dispose()` runs after the awaited body, and `Add(new FadingLabel(...))` mutates the control tree from a task continuation.
- `GridHighLight/GridHighLightConfig.cs:71` — writes `ProfileManager.CurrentProfile.ConfigurableProperties` from the same handler for all six categories, so any of the six text areas overwrites the single profile list.
- `GridHighLight/GridHightlightMenu.cs:225,242` — `JsonSerializer.Serialize/Deserialize` with reflection-based options; the repo convention (CLAUDE.md) requires a generated serializer context for every JSON round-trip.
- `GridHighLight/GridHightlightMenu.cs:104,118-124` — the `data` (`GridHighlightData`) captured by each row's closures is bound to `keyLoc` (a list index); `Move()` (:158/:172) reorders the underlying list, leaving already-created closures pointing at the old index.
- `GridHighLight/GridHightlightMenu.cs:145-147` — `data.Delete()` then `BuildGump()` clears and recreates the control tree from inside that control's own `MouseUp`.
- `GridHighLight/GridHightlightMenu.cs:50-52` — the "Add +" handler mutates the captured local `y` from `BuildGump`'s scope; `y` was last reassigned at :91-96, so new rows stack from wherever the loop left it.
- `DiscordGump/DiscordChannelListControl.cs:33,50` — lobby create/delete events (raised from the Discord SDK, not necessarily the frame thread) call `BuildChannelList()`, which does `_channelList.Clear()` and re-adds controls.
- `DiscordGump/DiscordChannelListControl.cs:36-41` — `Dispose()` calls `base.Dispose()` *before* unsubscribing from `DiscordManager.Instance` events.
- `DiscordGump/DiscordChannelListControl.cs:83` — enumerates `DiscordManager.Instance.MessageHistory.Keys` (a live dictionary) while adding controls.
- `DiscordGump/DiscordUserListItem.cs:70` — `UIManager.Add(new DiscordUserPopupGump(...))` from `Update()`, adding a gump during the UI update walk.
- `DiscordGump/DiscordUserListItem.cs:43` — `user.Id()` called on a `UserHandle` cached at construction (:31); the handle is not refreshed if the SDK invalidates it.
- `DiscordGump/DiscordUserListItem.cs:119-122` — `shue` (online/offline dot color) is computed once in `Build()` and never updated when the user goes on/offline.
- `ProfileGump.cs:245` — `Dispose()` sends a network packet; disposal can happen during the UI walk or at shutdown.
- `ProfileGump.cs:197-203` — `Update()` writes `_scrollArea.Height`, `_databox.Y`, and two `WantUpdateSize` flags every frame, before `base.Update()`.
- `RacialAbilitiesBookGump.cs:306-316` — `_humanNames[offset]` / `_elfNames[offset]` indexed by `offset` bounded only by `_abilityCount`, which is set from `World.Player.Race` in `GetSummaryBookInfo` (:269); `Race` is server-authoritative and can change between construction and use.
- `RacialAbilitiesBookGump.cs:223` — `World.Player.Race` dereferenced inside a double-click handler with no null check.
- `NetworkStatsGump.cs:47,198-207` — `_last_position` is static and updated from `OnMove`; every NetworkStatsGump instance shares one remembered position, and the ctor (:65-66) treats a saved position of `<= 0` as "unset".
- `NetworkStatsGump.cs:116-128` — `stackalloc char[128]` with `ValueStringBuilder`; the formatted string can exceed 128 chars in the non-minimized branch (:125).
- `SimpleTimedTextGump.cs:16,36` — expiry uses `DateTime.Now` (wall clock, DST/clock-change sensitive) rather than `Time.Ticks`, and `Dispose()` is called from inside `Draw`.
- `SimpleTimedTextGump.cs:18` — `ProfileManager.CurrentProfile` dereferenced in the ctor with no null check.
- `TextContainerGump.cs:66-71` — `Dispose()` calls `TextRenderer.UnlinkD()` but the `TextRenderer.Clear()` is commented out (:70).
- `UpdateTimerViewer.cs:45-48` — enumerates `UIManager.UpdateTimerTotalTime` and indexes `UIManager.UpdateTimerCount[kvp.Key]`; both are populated by the live update loop, and the divisor can be zero/missing for a key present in only one dictionary.
- `UpdateTimerViewer.cs:58-62` — `Dispose()` sets `UIManager.UpdateTimerEnabled = false` unconditionally, so closing one viewer turns profiling off for any other.

## Fork deltas

Clearly TazUO / Holiday additions (no ClassicUO license header, or referencing TazUO-only systems):

- `MultiItemMoveGump.cs` — entire file. TazUO multi-select item mover; `MoveItemQueue`, `CursorTarget.MoveItemContainer`, `CursorTarget.SetFavoriteMoveBag`, `ProfileManager.CurrentProfile.SetFavoriteMoveBagSerial` / `MoveMultiObjectDelay`. Built on `NineSliceGump` + `ModernUIConstants` (TazUO modern UI kit).
- `GridHighLight/GridHighLightConfig.cs` and `GridHighLight/GridHightlightMenu.cs` — entire directory. Grid-container highlighting rules, `GridHighlightData`/`GridHighlightRules`, `RGBColorPickerGump`, `FileSelector`, JSON import/export.
- `DiscordGump/DiscordChannelListControl.cs`, `DiscordGump/DiscordUserListItem.cs` — Discord Social SDK integration (`Discord.Sdk`, `DiscordSocialSDK.Wrapper`, `DiscordManager`, `DiscordUserPopupGump`, `EmbeddedGumpPic`, `TazUOSM.png` embedded asset). Uses file-scoped namespaces (`namespace ClassicUO.Game.UI.Controls;`), unlike the block-scoped stock files.
- `SimpleTimedTextGump.cs` — TazUO; uses `TextBox.GetOne` / `TextBox.RTLOptions` (TazUO TTF text stack) and profile `OverheadChatFont`.
- `UpdateTimerViewer.cs` — TazUO debug tool; `UIManager.UpdateTimerEnabled` / `UpdateTimerTotalTime` / `UpdateTimerCount` are fork-added UIManager statics.
- `ContainerGump.cs` — fork edits inside a stock file: `showGridToggle` ctor overload (:147) and the "Return to grid view" `NiceButton` (:239-255) that swaps to `GridContainer`; `BackpackStyle` graphic selection (:89-119); `HueContainerGumps` (:231); `firstItemsLoaded` (:61).
- `WorldViewportGump.cs` — fork edits: static `damageWindowOutline` / `DamageWindowOutlineHue` low-HP indicator (:64-65, :322-355); `VersionHistory` gump + `LegionScripting.DownloadAPIPy()` on version bump (:125-131); `BorderControl` extended with per-edge/corner graphic overrides and `Hue` (:365-402) — stock ClassicUO's BorderControl has only the four tiled edges.
- `MacroButtonGump.cs` — fork edits: `Scale`, `HideLabel`, `Hue`, nullable `Graphic` and the `RenderedText` label are TazUO macro-button customisation; stock uses a fixed-size label button.
- `SpellbookGump.cs` — `HueGumpPic` (:1461) with `World.ActiveSpellIcons` highlight and `FastSpellsAssign` ctrl+alt macro creation is a fork/TazUO addition layered on the stock gump.
- `CreateCharTradeGump.cs` — the expansion-flag skill filtering chain (:176-219) using `World.ClientLockedFeatures` is more elaborate than stock.
- `CharacterSelectionGump.cs` — `OnMouseWheel` cycling (:244) and `OnControllerButtonUp` (:226) plus the `chars` snapshot array are fork additions.
- `IgnoreManagerGump.cs` — no license header; TazUO/CUO-community ignore-list feature.
- Stock-looking, essentially unmodified: `AnchorableGump.cs`, `BuffGump.cs`, `MiniMapGump.cs`, `NetworkStatsGump.cs`, `ProfileGump.cs`, `RacialAbilitiesBookGump.cs`, `TextContainerGump.cs`.
