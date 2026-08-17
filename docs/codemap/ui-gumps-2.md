# ui-gumps-2

Partition = every 6th file (offset 2) of `src/ClassicUO.Client/Game/UI/Gumps/**/*.cs`.
20 files, 5342 lines, all read in full.

Root: `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/UI/Gumps/`

## Files

| path (relative to Gumps/) | lines | purpose |
| --- | --- | --- |
| `AnimBrowser.cs` | 124 | Debug gump: 500x700 grid of `AnimationDisplay`, paged by graphic index; double-click copies graphic id to SDL clipboard. Opened by `-animbrowser` command. |
| `BulletinBoardGump.cs` | 581 | Bulletin board container gump (0x087A) + `BulletinBoardItem` (single article, ExpandableScroll) + `BulletinBoardObject` (list row). Server-driven, sends post/reply/remove packets. |
| `ChatGump.cs` | 536 | UO chat channel list gump (0x0A28). Lists `ChatManager.Channels`, join/leave/create buttons, nested `ChannelCreationBox` and `ChannelListItemControl`. |
| `CoolDownBar.cs` | 258 | 180x30 timed bar gump drawn with two `AlphaBlendControl`s; expires itself in `Draw()`. Also hosts nested `CoolDownConditionData` (profile-backed parallel-list config store). TazUO addition. |
| `DiscordGump/DiscordChannelListItem.cs` | 96 | Row control for a Discord lobby or DM channel in `DiscordGump`; click sets active channel. TazUO addition. |
| `DiscordGump/DiscordUserPopupGump.cs` | 86 | Modal 200x300 popup showing a Discord user's avatar (`ExternalUrlImage`), display name, activity/party. TazUO addition. |
| `GridHighLight/GridHighLightData.cs` | 469 | Not a gump — the highlight rule engine. Wraps `GridHighlightSetupEntry` from profile, matches item OPL property data, drives `Item.MatchesHighlightData/HighlightHue/HighlightColor` and auto-loot. TazUO addition. |
| `GridLootGump.cs` | 591 | Grid corpse-loot window (max 300x420, 50px cells, 2 display groups: non-stackable then stackable), pages, "set loot bag" target, nested `GridLootItem` with amount slider. |
| `ImprovedBuffGump.cs` | 232 | Buff bar gump made of `CoolDownBar`s in a `DataBox`; flips direction up/down; nested static `BuffBarManager` fixed 20-slot array. TazUO addition. |
| `Login/LoadingGump.cs` | 153 | Login-scene modal message box (ResizePic 0x0A28) with OK/Cancel flag enum `LoginButtons`, Enter = OK. |
| `MacroGump.cs` | 45 | Thin wrapper placing a `MacroControl` on an alpha background, positioned from `Client.Game.Scene.Camera.Bounds`. |
| `ModernBookGump.cs` | 884 | Book reader/editor (0x1FE + 0x1FF/0x200 page pics). Nested `StbPageTextBox` re-flows the whole book into fixed 8-lines-per-page slots, tracks dirty pages, requests/sends page data. |
| `MultipleToolTipGump.cs` | 109 | Horizontal row of `CustomToolTip`s used by GridContainer compare-tooltips; self-disposes when hover reference loses mouse; exposes static screenshot rect. TazUO addition. |
| `NineSliceGump.cs` | 364 | Base class: 9-slice textured, corner-drag-resizable gump. Base of the LegionScripting gumps. TazUO addition. |
| `ProgressBarGump.cs` | 52 | Simple percentage bar gump; used by `AutoLootManager`. TazUO addition. |
| `RacialAbilityButton.cs` | 96 | Anchored-less single `GumpPic` button, serial = `7000 + graphic`; gargoyle-fly toggle on double-click; XML save/restore. |
| `SkillButtonGump.cs` | 161 | `AnchorableGump` 88x44 button that uses a skill; XML save/restore by skill index. |
| `SplitMenuGump.cs` | 220 | Stack-split dialog (0x085C): slider + numeric textbox two-way bound, OK calls `GameActions.PickUp`. |
| `TextEntryDialogGump.cs` | 158 | Server-driven modal text prompt (0x0474); sends `Send_TextEntryDialogResponse`. `ShouldBeSaved => false`. |
| `UseAbilityButtonGump.cs` | 147 | `AnchorableGump` primary/secondary combat-ability button; recolors hue 38 while ability active, per-frame in `Draw`. |

## Types

| type | file:line | responsibility |
| --- | --- | --- |
| `AnimBrowser` | AnimBrowser.cs:9 | debug art/anim browser gump |
| `BulletinBoardGump` | BulletinBoardGump.cs:46 | board window, `_databox` of `BulletinBoardObject` |
| `BulletinBoardItem` | BulletinBoardGump.cs:171 | one article; Post/Reply/Remove `ButtonType` enum at :504 |
| `BulletinBoardObject` | BulletinBoardGump.cs:512 | 230x18 row, dbl-click → `Send_BulletinBoardRequestMessage` |
| `ChatGump` | ChatGump.cs:45 | channel list window |
| `ChatGump.ChannelCreationBox` | ChatGump.cs:311 | nested create-channel modal-ish control |
| `ChatGump.ChannelListItemControl` | ChatGump.cs:453 | selectable channel row, cyan hover fill in `Draw` :513 |
| `CoolDownBar` | CoolDownBar.cs:10 | timed bar gump |
| `CoolDownBar.CoolDownConditionData` | CoolDownBar.cs:152 | static CRUD over 6 parallel profile lists; `MESSAGE_TYPE` enum :249 |
| `DiscordChannelListItem` | DiscordChannelListItem.cs:9 (`ClassicUO.Game.UI.Controls`) | discord channel row |
| `DiscordUserPopupGump` | DiscordUserPopupGump.cs:9 | discord user card, `IsModal`, click-outside closes |
| `GridHighlightData` | GridHighLightData.cs:15 | highlight rule + static OPL match pipeline |
| `GridLootGump` | GridLootGump.cs:46 | corpse grid loot |
| `GridLootGump.GridLootItem` | GridLootGump.cs:443 | one cell; slider + hitbox + custom art draw |
| `ImprovedBuffGump` | ImprovedBuffGump.cs:11 | buff bar container, `GumpType.Buff` |
| `ImprovedBuffGump.BuffBarManager` | ImprovedBuffGump.cs:161 | static 20-slot `CoolDownBar[]` |
| `LoginButtons` (flags enum) | Login/LoadingGump.cs:42 | None=1, OK=2, Cancel=4 |
| `LoadingGump` | Login/LoadingGump.cs:49 | login message box |
| `MacroGump` | MacroGump.cs:7 | macro editor host |
| `ModernBookGump` | ModernBookGump.cs:48 | book gump |
| `ModernBookGump.StbPageTextBox` | ModernBookGump.cs:547 | paged text box, page coords, dirty tracking |
| `MultipleToolTipGump` | MultipleToolTipGump.cs:6 | side-by-side tooltips |
| `NineSliceGump` | NineSliceGump.cs:9 | resizable 9-slice base; `ResizeCorner` enum :51 |
| `ProgressBarGump` | ProgressBarGump.cs:8 | progress bar |
| `RacialAbilityButton` | RacialAbilityButton.cs:43 | racial ability button |
| `SkillButtonGump` | SkillButtonGump.cs:44 | skill button (AnchorableGump) |
| `SplitMenuGump` | SplitMenuGump.cs:40 | amount split dialog |
| `TextEntryDialogGump` | TextEntryDialogGump.cs:39 | server text prompt |
| `UseAbilityButtonGump` | UseAbilityButtonGump.cs:42 | ability button (AnchorableGump) |

## State

Static / global:

- `GridLootGump._lastX` / `_lastY` — GridLootGump.cs:51-52. Static, initialized once from `ProfileManager.CurrentProfile.GridLootType` at first type touch; written in `Dispose()` (:344-345) and read by every new instance (:91-92).
- `GridHighlightData.allConfigs` — GridHighLightData.cs:17. Static cache of all rules; lazily built from `ProfileManager.CurrentProfile.GridHighlightSetup` (:33-35); invalidated by `Delete()` (:128) and by `AllConfigs = null` from the menu gumps.
- `GridHighlightData._queue` (`Queue<uint>`) + `hasQueuedItems` — GridHighLightData.cs:20-21. Static serial queue fed by OPL receipt.
- `GridHighlightData.HtmlTagRegex` — :23 (compiled, never actually used; `StripHtmlTags` at :399 does the work manually).
- `GridHighlightData._normalizeCache` — :24. Per-instance `Dictionary<string,string>`, never cleared or bounded.
- `ImprovedBuffGump.BuffBarManager.coolDownBars` — ImprovedBuffGump.cs:164. Static `CoolDownBar[20]`, shared across all `ImprovedBuffGump` instances.
- `MultipleToolTipGump.SSIsEnabled / SSX / SSY / SSWidth / SSHeight` — MultipleToolTipGump.cs:11-13. Static screenshot rectangle read by `GameController.cs:721-723`.
- `ModernBookGump.StbPageTextBox._sb`, `_handler` — ModernBookGump.cs:549-550. Static `StringBuilder` and static `string[]` scratch shared by every open book gump.
- `CoolDownBar.DEFAULT_X/DEFAULT_Y` — CoolDownBar.cs:13-14. Static properties reading `ProfileManager.CurrentProfile.CoolDownX/Y`.

Per-instance mutable worth noting:

- `CoolDownBar.expire`, `duration`, `startX/startY` — CoolDownBar.cs:18-20. `expire = DateTime.Now + duration` at :36 (wall clock, not `Time.Ticks`).
- `GridLootGump._corpse` (readonly `Item` ref captured at ctor, :68), `_currentPage`, `_pagesCount`, `firstItemsLoaded` (:59-64).
- `ModernBookGump.KnownPages` (`HashSet<int>`, :86), `BookPageCount` (:85), `_bookPage._pageCoords/_pageLines/_pagesChanged` (:585-587).
- `NineSliceGump._isDragging/_dragCorner/_dragStart*` (:18-22) — resize drag state polled in `Update()`.
- `SplitMenuGump._updating/_firstChange/_lastValue` (:42-48) — reentrancy guard for slider↔textbox binding.
- `SkillButtonGump._skill` (:46), `UseAbilityButtonGump.Index/IsPrimary` (:61-62), `RacialAbilityButton.Graphic` (:62) — all restored from XML.

## Timing

- **Per frame (`Update()`)**: `CoolDownBar.Update` (:96) writes `Profile.CoolDownX/Y` whenever the gump has moved and `UseLastMovedCooldownPosition` is on. `GridLootGump.Update` (:373) disposes on corpse destroyed / on-ground and `Distance > 3`, resizes, rewrites the corpse-name label, and sets `SelectedObject.Object/CorpseObject` while hovered. `SplitMenuGump.Update` (:194) disposes when the item is gone. `NineSliceGump.Update` (:198) polls `Mouse.Position`/`Mouse.LButtonPressed` to apply corner resize. `BulletinBoardItem.Update` (:407) repositions its button to `Height - 50`.
- **Per frame (`Draw()`)** — logic executed inside draw, not update:
  - `CoolDownBar.Draw` (:113) compares `DateTime.Now >= expire` and calls `Dispose()`; recomputes `foreground.Width` from remaining/duration; writes `cooldownLabel.Text` every frame.
  - `MultipleToolTipGump.Draw` (:63) calls `Dispose()` when `hoverReference.MouseIsOver` is false; clamps x/y to `Client.Game.Window.ClientBounds` and publishes `SSX/SSY`.
  - `UseAbilityButtonGump.Draw` (:113) reads `World.Player.Abilities[0|1]` and sets `_button.Hue = 38` when the 0x80 active bit is set.
  - `ModernBookGump.Draw` (:381) mutates `_bookPage._caretPage`, `_focusPage` and calls `SetActivePage()` — which can send book packets — from inside the draw call (:436, :443-444, :498-499, :506).
  - `ProgressBarGump.Draw` (:33) draws `CurrentPercentage * Width`.
- **Per frame, from `GameScene.Update`**: `GridHighlightData.ProcessQueue()` at `Game/Scenes/GameScene.cs:934` (immediately after `_moveItemQueue.ProcessQueue()`), dequeuing **at most 3 serials per frame** (GridHighLightData.cs:159) and matching them against every config.
- **Per packet**: `ObjectPropertiesListManager` (`Game/Managers/ObjectPropertiesListManager.cs:65`) calls `GridHighlightData.ProcessItemOpl(serial)` on every OPL receipt → enqueue. `PacketHandlers.cs:1191/1550-1551/6509-6519` create/refresh `GridLootGump`; `:2576-2614` fills `ModernBookGump.BookLines`; `:2771/2862` builds bulletin board gumps/items; `:3473-3486` opens books; `:3847` opens `TextEntryDialogGump`; `:4192` calls `ChatGump.UpdateConference()`.
- **On demand / event**: `RequestUpdateContents()` → `UpdateContents()` for `GridLootGump` (rebuilds all cells), `ChatGump` (rebuilds channel rows), `ImprovedBuffGump` (repositions bars), `UseAbilityButtonGump` (full `BuildGump`).
- **On load**: `Profile.cs:919/1021/1031/1036/1184/1201` constructs `ImprovedBuffGump`, `UseAbilityButtonGump`, `SkillButtonGump`, `RacialAbilityButton` from saved XML, then calls their `Restore`.
- **No timers** in this partition; all timing is either frame-driven or wall-clock (`DateTime.Now` in `CoolDownBar`).

## Inbound

- `Network/PacketHandlers.cs` — `GridLootGump` (1191, 1550-1551, 6509-6519), `ModernBookGump` (2576-2614, 3473-3486), `BulletinBoardGump`/`BulletinBoardItem` (2771, 2862), `TextEntryDialogGump` (3847), `ChatGump.UpdateConference` (4192), `SplitMenuGump` dispose (1849).
- `Game/Scenes/GameScene.cs:934` — `GridHighlightData.ProcessQueue()` each frame.
- `Game/Scenes/LoginScene.cs:335, 671` — `new LoadingGump(...)`.
- `Game/Managers/ObjectPropertiesListManager.cs:65` — `GridHighlightData.ProcessItemOpl`.
- `Game/Managers/AutoLootManager.cs:31, 313` — owns a `ProgressBarGump`.
- `Game/Managers/MacroManager.cs:1513, 1754-1761, 2089` — closes all gumps except `ImprovedBuffGump`; toggles buff gump; enumerates `GridLootGump`s.
- `Game/Managers/HideHudManager.cs:50, 58` — hides `SkillButtonGump` and `ImprovedBuffGump` by flag.
- `Game/Managers/CommandManager.cs:364` — `-animbrowser` → `new AnimBrowser()`.
- `Game/GameActions.cs:151` (`MacroGump`), `:472` (`ChatGump`), `:840-847` (`SplitMenuGump`).
- `Game/GameObjects/PlayerMobile.cs:272, 295` — `ImprovedBuffGump.AddBuff/RemoveBuff`; `:1405` refreshes `UseAbilityButtonGump`.
- `Game/GameObjects/Item.cs:285, 289`; `Game/World.cs:326` — dispose/refresh `GridLootGump` and `SplitMenuGump` on item destroy/container change.
- `Game/UI/Controls/ItemGump.cs:270` — looks up `SplitMenuGump`.
- `Game/UI/Gumps/GridContainer.cs:1253` — `new MultipleToolTipGump(...)` at mouse position.
- `Game/UI/Gumps/GridHighLight/GridHightlightMenu.cs` and `GridHighLightProperties.cs` — read/write `GridHighlightData`, call `RecheckMatchStatus()` and null out `AllConfigs`.
- `GameController.cs:721-723` — reads `MultipleToolTipGump.SSIsEnabled/SSX/SSY/SSWidth/SSHeight` for clipboard screenshot.
- `Configuration/Profile.cs` — restores the four saveable gumps.
- `LegionScripting/ScriptRecordingGump.cs:16`, `ScriptBrowser.cs:22`, `ScriptingInfoGump.cs:16`, `ScriptManagerGump.cs:18` — all derive from `NineSliceGump`.

## Outbound

- **Network** (`NetClient.Socket.Send_*`): `Send_BulletinBoardPostMessage`/`RemoveMessage`/`RequestMessage` (BulletinBoardGump.cs:450, 476, 576); `Send_ChatJoinCommand`/`ChatLeaveChannelCommand`/`ChatCreateChannelCommand` (ChatGump.cs:252, 258, 446); `Send_BookPageDataRequest`/`BookHeaderChanged`/`BookHeaderChanged_Old`/`BookPageData` (ModernBookGump.cs:319, 324, 339, 343, 355); `Send_TextEntryDialogResponse` (TextEntryDialogGump.cs:130, 141); `Send_ToggleGargoyleFlying` (RacialAbilityButton.cs:76).
- **GameActions**: `Print` (AnimBrowser.cs:98, GridLootGump.cs:213/311), `GrabItem` (GridLootGump.cs:500), `PickUp` (SplitMenuGump.cs:188), `UseSkill` (SkillButtonGump.cs:124, 132), `UsePrimaryAbility`/`UseSecondaryAbility` (UseAbilityButtonGump.cs:99, 103).
- **Managers**: `TargetManager.SetTargeting(CursorTarget.SetGrabBag, …)` (GridLootGump.cs:214); `AutoLootManager.Instance.LootItem` (GridHighLightData.cs:180); `ChatManager.Channels` / `CurrentChannelName` (ChatGump.cs:124, 155, 275); `DiscordManager.Instance.GetUser/GetLobbyName/GetUserhue` (DiscordUserPopupGump.cs:27, 58; DiscordChannelListItem.cs:85, 90); `UIManager.Add/GetGump/Gumps/MouseOverControl/KeyboardFocusControl/SystemChat` (many).
- **World state read/written**: `World.Items.Get/TryGetValue` (GridLootGump.cs:68/451/514, SplitMenuGump.cs:53/196, GridHighLightData.cs:162/178/206); `World.Player.ManualOpenedCorpses/AutoOpenedCorpses` (GridLootGump.cs:77-82); `World.Player.BuffIcons` (ImprovedBuffGump.cs:120); `World.Player.Abilities` (UseAbilityButtonGump.cs:67, 120); `World.Player.Skills` (SkillButtonGump.cs:151-153); `World.Player.Race` (RacialAbilityButton.cs:74); `World.ClientFeatures.TooltipsEnabled` (GridLootGump.cs:491); `SelectedObject.Object/CorpseObject` (GridLootGump.cs:338-340, 425-426, 433).
- **Item mutation**: `data.item.MatchesHighlightData/HighlightHue/HighlightColor` written from `ProcessQueue` (GridHighLightData.cs:172-174) — client-side rendering state layered onto server objects.
- **Profile**: `CoolDownBar` writes `CoolDownX/Y` (:106-107); `CoolDownConditionData` mutates six `Condition_*` lists; `GridHighlightData` mutates `GridHighlightSetup`.
- **Assets/Renderer**: `Client.Game.Arts.GetArt/GetRealArtBounds`, `FontsLoader.Instance.GetWidthUnicode/GetWidthASCII/GetHeightUnicode`, `ClilocLoader.Instance.GetString`, `TrueTypeLoader.EMBEDDED_FONT`, `SolidColorTextureCache`, `ShaderHueTranslator`, `Client.Game.Audio.PlaySound(0x0055)` (ModernBookGump.cs:262, 307).
- **SDL**: `SDL.SDL_SetClipboardText` (AnimBrowser.cs:97).

## Hazards

- `AnimBrowser.cs:112-123` — `BuildPage` loops `while (count < maxEntries)` incrementing `index` unbounded; `GetArt(index)` is called with an ever-growing `uint` and the result is discarded (the filtering `if` is commented out at :115). With a large `Page` (user can type any int, :39-47, or a hex graphic at :58-59) `index` can exceed valid art range and the cast `(ushort)index` at :118 truncates.
- `AnimBrowser.cs:59` — hex graphic input sets `Page = (int)p` directly (a graphic id used as a page number), while the decimal branch at :64-70 divides by `maxEntries`. Two different meanings for the same field.
- `BulletinBoardGump.cs:124-130` — `Dispose()` walks `UIManager.Gumps` and disposes **every** `BulletinBoardItem`, not just those belonging to this board serial.
- `BulletinBoardGump.cs:137-147` and `:153-161` — `foreach (Control child in _databox.Children)` calls `child.Dispose()` inside the iteration.
- `BulletinBoardGump.cs:398-404` / `:488-494` — iterates `_databox.Children` looking for `BulletinBoardItem`, but `_databox` only ever contains an `StbTextBox` (:307) or `BulletinBoardObject` (:164); the branch never fires.
- `BulletinBoardGump.cs:486` — `_databox.Parent.Height = … - 184` with no null check on `Parent`.
- `ChatGump.cs:281-299` — `UpdateContents` disposes the old `ChannelListItemControl`s but never removes them from `_databox`; new ones are `Add`ed on top, so `_databox.Children` accumulates disposed controls between rebuilds.
- `ChatGump.cs:506-511` — `OnMouseDoubleClick` calls `base.OnButtonClick(0)` on the row control (not the parent gump), so double-click does nothing.
- `ChatGump.cs:263-267` — `_channelCreationBox` is `Add`ed to the gump but never removed on `Dispose`; the field is only re-checked for `IsDisposed`.
- `CoolDownBar.cs:36, 118, 121` — uses `DateTime.Now` (local wall clock) rather than `Time.Ticks`; a DST/clock change shifts expiry.
- `CoolDownBar.cs:118-119` — `Dispose()` called from inside `Draw()`, then execution continues to `base.Draw` at :132 and the two `DrawRectangle` calls on a disposed gump.
- `CoolDownBar.cs:128` — `foreground.Width` computed as a fraction of `duration.TotalSeconds`; if `duration` is zero this divides by zero.
- `CoolDownBar.cs:106-107` — writes `ProfileManager.CurrentProfile.CoolDownX/Y` from `Update()` every frame the gump position differs from `startX/startY`.
- `CoolDownBar.cs:174-200, 206-234, 240-246` — six parallel `List<>`s in the profile indexed by the same `key`; `Condition_ReplaceIfExists` is separately length-patched (:182-190, :214-223), so the lists can be different lengths.
- `CoolDownBar.cs:232` — `SaveCondition` appends `createIfNotExist` into `Condition_ReplaceIfExists` instead of `replace_if_exists`.
- `GridHighLightData.cs:190-202` — `GetGridHighlightData(index)`: when `index` is out of range it appends **one** entry then immediately does `new GridHighlightData(list[index])`, which still throws for any `index > list.Count`.
- `GridHighLightData.cs:26-38` — `AllConfigs` getter reads `ProfileManager.CurrentProfile.GridHighlightSetup` with no null-profile check; the cache is not invalidated when a new `GridHighlightData()` is constructed (:114-118) or when `Move()` reorders the list (:131-143).
- `GridHighLightData.cs:24, 377-382` — `_normalizeCache` is per-instance, keyed by arbitrary item property strings, never evicted; grows for the lifetime of the config object.
- `GridHighLightData.cs:204-211` — `RecheckMatchStatus` enqueues every item in `World.Items` into `_queue`; at 3 per frame (:159) a large world takes many frames to drain, and each menu edit calls it again without clearing the queue.
- `GridHighLightData.cs:161-163` — dequeued serials are looked up in `World.Items`; if the item was destroyed between enqueue and dequeue it is silently skipped, leaving stale `MatchesHighlightData` on nothing. Conversely a match sets flags but nothing ever clears them when a rule stops matching.
- `GridHighLightData.cs:352` — `matchingPropertiesCount++` is executed even when `match == null` and the property was optional, so optional non-matches count toward the min/max property count in `IsMatchFromItemPropertiesData` (unlike :270-273 in the other path).
- `GridHighLightData.cs:23` — `HtmlTagRegex` compiled and never used.
- `GridLootGump.cs:51-52, 91-92, 344-345` — `_lastX/_lastY` are static and shared across all corpses; the static initializer reads `ProfileManager.CurrentProfile` at type-init time.
- `GridLootGump.cs:229-231` — `foreach (… Children.OfType<GridLootItem>()) gridLootItem.Dispose()` disposes while enumerating `Children`.
- `GridLootGump.cs:243-278` — page assignment: `y` resets to 20 and `_pagesCount++` only when a row overflows, and `line`/`row` are never reset per page, so `_background.Width/Height` at :281-282 are computed from cumulative counts across pages.
- `GridLootGump.cs:309-313` — `Dispose()` is called from `UpdateContents()` when the corpse has no lootable items; the method then continues to :314-322 on a disposed gump.
- `GridLootGump.cs:447-458` — `GridLootItem(uint serial, …)` is declared taking a serial but is constructed at :254 with `new GridLootItem(it, GRID_ITEM_SIZE)` (an `Item`, via implicit conversion to serial); the closure at :496-502 captures the `Item` instance while `Draw` at :514 re-resolves by `LocalSerial`.
- `GridLootGump.cs:68` — `_corpse` is a readonly `Item` reference captured at construction; the serial can be reused by the server for a different object while the gump holds the old instance.
- `GridLootGump.cs:375` — disposes when `_corpse.OnGround && _corpse.Distance > 3`; no distance check when the corpse is in a container.
- `ImprovedBuffGump.cs:164` — `BuffBarManager.coolDownBars` is `static`, but the gump instance passes its own `_box` into `AddCoolDownBar`/`UpdatePositions`; two `ImprovedBuffGump`s would share one 20-slot array and fight over positions.
- `ImprovedBuffGump.cs:165-183` — `AddCoolDownBar` scans for a same-`buffIconType` slot **or** the first free slot, whichever comes first; a duplicate buff type later in the array is not deduplicated. When all 20 slots are full and none match, the new bar is added to `_box` at :40 but never registered, so it is never repositioned or cleaned up.
- `ImprovedBuffGump.cs:205-217` — `RemoveBuffType` disposes the bar but leaves the array slot pointing at the disposed object (relies on `IsDisposed` checks elsewhere).
- `ImprovedBuffGump.cs:117` — `BuildGump` calls `BuffBarManager.Clear()`, disposing bars owned by any other existing instance.
- `ImprovedBuffGump.cs:149-150` — `int.TryParse(xml.GetAttribute("lastX"), out X)` writes directly into the `X` field via `out`; a missing/invalid attribute leaves `X = 0`.
- `ModernBookGump.cs:436, 443-444, 498-499, 506` — `Draw()` mutates `_caretPage`/`_focusPage` and calls `SetActivePage`, which at :319-355 sends network packets from inside the render pass.
- `ModernBookGump.cs:374-376` — `CloseWithRightClick` calls `SetActivePage(0)`, which is clamped to 1 at :303, so the "flush changes on close" path also flips the visible page.
- `ModernBookGump.cs:549-550` — `_sb` and `_handler` are `static` on `StbPageTextBox`; two open books re-flowing text share the same scratch buffers, and `_handler` is only grown (:801-804), never shrunk to the current book's line count, so stale lines from a longer book are copied into `_pageLines` at :860.
- `ModernBookGump.cs:812-841` — inner reflow loop indexes `split[i][p]` with `pw` pre-computed in the `for` initializer; on a line whose first char already exceeds `MaxWidth` the loop emits an empty handler entry and advances `l` without consuming a char.
- `ModernBookGump.cs:855-860` — `_handler[i]` may be `null` for lines past the parsed text; `_pageLines[i]` is assigned that null and appended to `_sb`.
- `ModernBookGump.cs:104-126` — `ServerSetBookText` dereferences `BookLines[i]` at :106 and :114 before the `if (BookLines[i] == null) continue;` guard at :118.
- `ModernBookGump.cs:573-575` — `_pageCoords` sized `[bookpages,2]` while `_pagesChanged` is `[bookpages+1]`; `Draw` indexes `_pageCoords[startpage,…]` at :392 guarded only by `startpage < BookPageCount`, and `startpage--` at :450 can reach `-1` (guarded at :452).
- `ModernBookGump.cs:518-522` — `Dispose()` calls `base.Dispose()` first, then `_bookPage?.Dispose()`.
- `MultipleToolTipGump.cs:65-66` — `Dispose()` called at the top of `Draw`, then `base.Draw` runs at :99 on the disposed gump; `hoverReference` is dereferenced without a null/disposed check.
- `MultipleToolTipGump.cs:35` — subscribes a closure to `toolTips[i].OnOPLLoaded` capturing `this`; never unsubscribed on `Dispose`.
- `MultipleToolTipGump.cs:11-13, 106` — `SSIsEnabled` is a single static bool; the last gump to dispose clears it even if another is alive.
- `NineSliceGump.cs:90-92` — `CalculateSlices` returns early when `_customTexture` is null, leaving `_slices` as default `Rectangle`s; `Draw` at :289 checks for null texture but `BorderSize` setter (:44-48) can leave slices stale relative to a later texture.
- `NineSliceGump.cs:361` — corner highlight draws `SolidColorTextureCache.GetTexture(Color.White)` but passes `_slices[4]` (a source rect from the 9-slice texture) as the source rectangle of a 1x1 solid texture.
- `NineSliceGump.cs:202-277` — resize runs in `Update()` reading global `Mouse.Position`; there is no capture, so dragging outside the gump keeps resizing until the button is released.
- `RacialAbilityButton.cs:47-49` — sets `LocalSerial = 7000 + graphic` then disposes any existing gump with that serial from inside the constructor of the replacement.
- `RacialAbilityButton.cs:69` — `1112198 + (Graphic - 0x5DD0)` assumes `Graphic >= 0x5DD0`; `Restore` (:93) parses an arbitrary saved value.
- `SkillButtonGump.cs:151` — `Restore` reads `World.Player.Skills` at profile-load time; `_skill` is a live `Skill` reference held for the gump's lifetime.
- `SkillButtonGump.cs:73` — `SkillID => _skill.Index` with no null guard; the parameterless ctor (:57) leaves `_skill` null until `BuildGump`/`Restore`.
- `SplitMenuGump.cs:110-112` — `_textBox.TextChanged` and `_slider.ValueChanged` both call `UpdateText()`, guarded only by the `_updating` bool (:117-122); `_firstChange` is never reset, so the "take last digit" behaviour at :148-154 happens only once per gump.
- `SplitMenuGump.cs:196-201` — disposes on missing item but does not `return` before the `IsDisposed` check at :203 (harmless here, but `base.Update()` is skipped).
- `SplitMenuGump.cs:79` — slider max is `item.Amount` captured at construction; the server can change the stack size afterwards and `PickUp` at :188 sends the stale value.
- `TextEntryDialogGump.cs:118-119` — sets `UIManager.KeyboardFocusControl` directly **and** calls `SetKeyboardFocus()`.
- `UseAbilityButtonGump.cs:67-69` — `Index = ability & 0x7F`, then `AbilityData.Abilities[Index - 1]`; `Index == 0` (no ability set) indexes `[-1]`.
- `UseAbilityButtonGump.cs:120-128` — reads `World.Player` every frame in `Draw` with no null check (login/logout transition).
- `ImprovedBuffGump.cs:37` — `TimeSpan.FromMilliseconds(icon.Timer - Time.Ticks)` can be negative for an already-expired buff, producing a negative `duration` in `CoolDownBar` and a negative `foreground.Width` at CoolDownBar.cs:128.
- `DiscordUserPopupGump.cs:76` — `ServerInfo.FromJson(activitys.Id())` result used without a null check at :78.
- `DiscordChannelListItem.cs:47-50` — `SetSelected` compares `gump.ActiveChannel` to a cached `ID` captured at construction; the underlying `LobbyHandle`/`ChannelHandle` is held indefinitely.
- `MacroGump.cs:19, 26` — positions children from `Client.Game.Scene.Camera.Bounds.Width` at construction only; the child X/Y are absolute-ish offsets inside a gump whose own X/Y is 0, and `SetInScreen()` at :42 is the only correction.
- `GridLootGump.cs:319-322` — `firstItemsLoaded` is set but never read anywhere.

## Fork deltas

Clearly TazUO / Holiday-Edition additions (no upstream ClassicUO license header, modern C# style — file-scoped namespaces, target-typed `new`, switch expressions):

- `CoolDownBar.cs` (whole file, incl. `CoolDownConditionData` and the `Condition_*` profile lists), and its consumer `Game/Managers/CoolDownBarManager.cs`.
- `ImprovedBuffGump.cs` — TazUO replacement for stock `BuffGump`; both are special-cased side by side in `HideHudManager.cs:58` and `MacroManager.cs:1513`.
- `GridHighLight/GridHighLightData.cs` — the whole grid-highlight/auto-loot rule engine, wired into `ObjectPropertiesListManager` and `GameScene.Update`.
- `DiscordGump/*` — Discord SDK integration (`Discord.Sdk`, `DiscordManager`, `ExternalUrlImage`).
- `NineSliceGump.cs` — base class for the LegionScripting gumps (`ScriptBrowser`, `ScriptManagerGump`, `ScriptingInfoGump`, `ScriptRecordingGump`), paired with `Game/Data/ModernUIConstants.cs`.
- `MultipleToolTipGump.cs` — compare-tooltips for `GridContainer`, plus the static screenshot rect consumed by `GameController.cs:721`.
- `ProgressBarGump.cs` — used only by `AutoLootManager`.
- `AnimBrowser.cs` — `-animbrowser` debug command, uses `AnimationDisplay` and `DataBox.ReArrangeChildrenGridStyle()`.
- `GridLootGump.cs` is stock in origin but carries fork edits: `_setlootbag` button + `CursorTarget.SetGrabBag` (:106-119, :211-215), `_corpseNameLabel` (:160-173, :411), `it.IsLootable` filter (:249), `firstItemsLoaded` (:64, :319-322), and the reformatted (non-upstream) brace/argument style throughout.
- `ModernBookGump.cs`, `BulletinBoardGump.cs`, `ChatGump.cs`, `Login/LoadingGump.cs`, `SplitMenuGump.cs`, `TextEntryDialogGump.cs`, `SkillButtonGump.cs`, `UseAbilityButtonGump.cs`, `RacialAbilityButton.cs` retain the 2021 andreakarasho header and read as stock ClassicUO.
- `MacroGump.cs` and `ProgressBarGump.cs` have no header but use `TextBox.GetOne` / `TrueTypeLoader.EMBEDDED_FONT`, the TazUO text stack.
