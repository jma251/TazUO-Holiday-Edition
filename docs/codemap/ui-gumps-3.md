# ui-gumps-3

Partition = every 6th file (offset 3) of `src/ClassicUO.Client/Game/UI/Gumps/*.cs` (recursive).
20 files, 7851 lines, all read in full.

All paths below are relative to `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/UI/Gumps/`
unless stated otherwise.

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `ArtBrowserGump.cs` | 124 | Debug/dev art tile browser; 5x7 grid of `ResizableStaticPic`, paged over raw art indices, double-click copies graphic id to clipboard. |
| `CharCreation/CharCreationGump.cs` | 231 | Page-machine for character creation (appearance -> profession -> trade -> city); writes skills/stats onto a `PlayerMobile` before it exists server-side. |
| `ChatGumpChooseName.cs` | 192 | 210x330 modal asking for a chat nickname; sends `Send_OpenChat`. |
| `CounterBarGump.cs` | 850 | The counter bar: grid of `CounterItem` cells, each counting an item graphic+hue in equipped containers, or bound to a spell. Includes nested `CounterItem` and `CounterItem.ImageWithText`. |
| `DiscordGump/DiscordChatAreaControl.cs` | 182 | Chat pane inside `DiscordGump`: scrollable message databox, input field, `/players` command, "Share Item" targeting. Fork/TazUO Discord SDK integration. |
| `DressAgentConfigGump.cs` | 396 | Dress-agent config editor on `NineSliceGump`; config combobox, item list, dress/undress, macro creation. Fully rebuilds itself (`BuildGump`) on every mutation. |
| `GridHighLight/GridHighLightMigration.cs` | 59 | One-shot profile migration from the parallel `GridHighlight_*` lists to `GridHighlightSetup` entries. |
| `Gump.cs` | 381 | **Base class for all gumps.** Lock state, alpha scrolling, Save/Restore XML, centering/clamping helpers, `OnButtonClick` -> `GameActions.ReplyGump`. |
| `InfoBarGump.cs` | 424 | Resizable info bar plus `InfoBarControl`, one per configured stat; polls `World.Player` every 250 ms. |
| `Login/LoginBackground.cs` | 106 | Static login screen backdrop; version-gated art, quit button on pre-7.0.64.0. |
| `MapGump.cs` | 572 | Server-sent treasure/map gump: map texture, plot pins, plot-course buttons, world-map/pathfind context menu. Nested `PinControl`. |
| `ModernColorPicker.cs` | 225 | 10x20 hue swatch grid, 15 pages (0-14); nested `HueDisplay` swatch control with flash animation. |
| `MusicInfoGump.cs` | 222 | **Holiday fork.** On-screen music diagnostics overlay; polls `Client.Game.Audio.GetMusicStatus()` every 250 ms, double-click folds to one line. Not persisted. |
| `PaperdollGump.cs` | 1182 | `PaperDollGump` — the paperdoll. Buttons, 17 equipment slots, `PaperDollInteractable`, minimize, drop/equip handling. Nested `EquipmentSlot`, `EquipmentSlot.ItemGumpFixed`, and a `Settings : UISettings` block of ~90 graphic/position knobs. |
| `QuestArrowGump.cs` | 233 | Screen-edge arrow pointing at a quest location; recomputes position and hue-blinks every frame. |
| `ResizableGump.cs` | 258 | Abstract base (over `AnchorableGump`) adding a border, a drag-resize corner button, and lock semantics. |
| `SkillGumpAdvanced.cs` | 753 | TazUO advanced skills window: sortable columns, optional skill groups, drag-resize height. Plus top-level `SkillListEntry`. |
| `StandardSkillsGump.cs` | 1036 | Classic scroll-style skills gump; `SkillsGroupControl` + `SkillItemControl` nested, drag-between-groups, rename, delete-group. |
| `TipNoticeGump.cs` | 132 | Tip-of-the-day / notice scroll, prev/next -> `Send_TipRequest`. |
| `UseSpellButtonGump.cs` | 293 | Draggable spell icon (anchorable); casts on click/double-click, ctrl+alt shows a lock overlay and creates a fast macro. |

## Types

| Name | File:line | Role |
| --- | --- | --- |
| `Gump` | `Gump.cs:47` | Base of the whole gump hierarchy (`Control` subclass). |
| `ResizableGump` (abstract) | `ResizableGump.cs:42` | `AnchorableGump` + border + resize grip + `SetLockStatus`. |
| `ArtBrowserGump` | `ArtBrowserGump.cs:9` | Art index browser. |
| `CharCreationGump` | `CharCreation/CharCreationGump.cs:45` | Char creation page machine. |
| `CharCreationGump.CharCreationStep` (enum) | `CharCreation/CharCreationGump.cs:224` | Appearence=0, ChooseProfession=1, ChooseTrade=2, ChooseCity=3. |
| `ChatGumpChooseName` | `ChatGumpChooseName.cs:40` | Chat name prompt. |
| `CounterBarGump` | `CounterBarGump.cs:50` | Counter bar container; `GumpType.CounterBar`. |
| `CounterBarGump.CounterItem` | `CounterBarGump.cs:382` | One counter cell: graphic+hue or spell id. |
| `CounterBarGump.CounterItem.ImageWithText` | `CounterBarGump.cs:729` | Draws art-or-gump graphic plus amount label. |
| `DiscordChatAreaControl` | `DiscordGump/DiscordChatAreaControl.cs:11` | Discord chat pane (namespace is `ClassicUO.Game.UI.Controls`, not `.Gumps`). |
| `DressAgentConfigGump` | `DressAgentConfigGump.cs:12` | Dress agent editor. |
| `GridHighLightProfile` | `GridHighLight/GridHighLightMigration.cs:11` | Static-only migration helper (class name != file name). |
| `InfoBarGump` | `InfoBarGump.cs:46` | `GumpType.InfoBar`, extends `ResizableGump`. |
| `InfoBarControl` | `InfoBarGump.cs:158` | One label+value pair driven by an `InfoBarVars`. |
| `LoginBackground` | `Login/LoginBackground.cs:38` | `LayerOrder = UILayer.Under`. |
| `MapGump` | `MapGump.cs:45` | Server map gump. |
| `MapGump.ButtonType` (enum) | `MapGump.cs:497` | PlotCourse/StopPlotting/ClearCourse. |
| `MapGump.PinControl` | `MapGump.cs:504` | Pin marker, owns a `RenderedText`. |
| `ModernColorPicker` | `ModernColorPicker.cs:12` | Hue grid picker. |
| `ModernColorPicker.HueDisplay` | `ModernColorPicker.cs:103` | One hue swatch; also used standalone as a clickable colour box. |
| `MusicInfoGump` | `MusicInfoGump.cs:21` | `GumpType.MusicInfo`, `ShouldBeSaved => false`. |
| `PaperDollGump` | `PaperdollGump.cs:49` | `TextContainerGump` subclass, `GumpType.PaperDoll`. |
| `PaperDollGump.Buttons` (enum) | `PaperdollGump.cs:827` | Help..Status. |
| `PaperDollGump.EquipmentSlot` | `PaperdollGump.cs:840` | One layer slot. |
| `PaperDollGump.EquipmentSlot.ItemGumpFixed` | `PaperdollGump.cs:945` | Fixed-size item icon; `Contains` always true. |
| `PaperDollGump.Settings : UISettings` | `PaperdollGump.cs:1053` | Persisted skin/layout knobs (fork addition). |
| `QuestArrowGump` | `QuestArrowGump.cs:42` | Quest direction arrow. |
| `SkillGumpAdvanced` | `SkillGumpAdvanced.cs:51` | `GumpType.SkillMenu`. |
| `SkillGumpAdvanced.Buttons` (enum) | `SkillGumpAdvanced.cs:603` | SortName=1..SortLock=5. |
| `SkillListEntry` | `SkillGumpAdvanced.cs:614` | Row in the advanced skills list (top-level internal type). |
| `StandardSkillsGump` | `StandardSkillsGump.cs:51` | `GumpType.SkillMenu` (same enum value as advanced). |
| `StandardSkillsGump.SkillsGroupControl` | `StandardSkillsGump.cs:424` | Collapsible group header + databox. |
| `StandardSkillsGump.SkillItemControl` | `StandardSkillsGump.cs:813` | One skill row; drag source between groups. |
| `TipNoticeGump` | `TipNoticeGump.cs:38` | Tip scroll. |
| `UseSpellButtonGump` | `UseSpellButtonGump.cs:47` | `GumpType.SpellButton`, `AnchorableGump`, `ANCHOR_TYPE.SPELL`. |

## State

Static / global:
- `CounterBarGump.CurrentCounterBarGump` — `CounterBarGump.cs:54`, set in both ctors (`:64`, `:104`), nulled in `Dispose` (`:375`). Read by `MacroManager.cs:2130`.
- `SkillGumpAdvanced._buttonsToSkillsValues` — `SkillGumpAdvanced.cs:55`, static readonly button->`Skill` property-name map (reflected via `typeof(Skill).GetProperty`).
- `SkillGumpAdvanced.Dragging` — `SkillGumpAdvanced.cs:67`, **public static bool**, shared by all instances of the gump.
- `SkillGumpAdvanced._sortAsc` / `_sortField` — `:69`, `:70`, static sort state shared across instances and across relogs within a process.
- `SkillGumpAdvanced.last_x/last_y/last_button` — `:81`, static remembered window position and selected sort column.
- `StandardSkillsGump.last_x/last_y` — `StandardSkillsGump.cs:69`, static remembered position.
- `MusicInfoGump._lastPosition` — `MusicInfoGump.cs:23`, static `Point(-1,-1)`; updated every 250 ms in `Update` (`:108`) and read by the ctor so a re-open lands where the last one was.
- `PaperDollGump._settings` — `PaperdollGump.cs:87`, static lazily-loaded `Settings`, shared by every paperdoll instance (player and others) for the whole process.
- `PaperDollGump.PeaceModeBtnGumps` / `WarModeBtnGumps` — `:51`, `:52`, static readonly graphic triples.

Per-instance mutable:
- `Gump.isLocked` `Gump.cs:49`; `Gump.AlphaOffset` `:71`; `Gump.InvalidateContents` `:67`; `Gump.MasterGumpSerial` `:69`.
- `ResizableGump._clicked`, `_lastSize`, `_savedSize`, `_isLocked`, `_prevCanMove/_prevCloseWithRightClick/_prevBorder` — `ResizableGump.cs:46-51`.
- `CounterBarGump._rows/_columns/_rectSize` — `CounterBarGump.cs:56-58`.
- `CounterItem._amount`, `_time`, `_endHighlight`, `_highlight`, `Graphic`, `Hue`, `SpellID` — `CounterBarGump.cs:384-389`, `413-417`. `HIGHLIGHT_DURATION = 1000` ms at `:388`.
- `InfoBarGump._infobarControls` (List) + `_refreshTime` — `InfoBarGump.cs:50-51`; `InfoBarControl._refreshTime` `:195`, `_warningLinesHue` `:163`.
- `MapGump._container` (List of pin controls) `MapGump.cs:48`, `_currentPin` `:49`, `_lastPoint` `:50`, `_mapTexture` `:52`, `_pinTimer` `:55`, `mapX/mapY/mapFacet/mapEndX/mapEndY/foundMapLoc` `:181-182`, `PlotState` `:168`.
- `ModernColorPicker._cPage` with wrap-around setter `ModernColorPicker.cs:24-43`; `HueDisplay.flash/flashAlpha/rev` `:113-115`.
- `MusicInfoGump._text`, `_nextUpdate`, `IsMinimized` — `MusicInfoGump.cs:26-27`, `:51`.
- `PaperDollGump._isWarMode`, `_isMinimized`, `_slots[9]`, `_slots_right[8]`, `CanLift` — `PaperdollGump.cs:56`, `:64-65`, `:137`.
- `SkillGumpAdvanced._skillListEntries`, `_totalReal/_totalValue`, `_updateSkillsNeeded`, `dragStartH` — `SkillGumpAdvanced.cs:65`, `:72-74`.
- `StandardSkillsGump._skillsControl` (List) `StandardSkillsGump.cs:65`, `_isMinimized` `:61`; `SkillsGroupControl._skills`, `_status` (0/1/2 rename state machine) `:432-434`.
- `DressAgentConfigGump._config`, `_readOnly`, `_allConfigs` — `DressAgentConfigGump.cs:13-20`; `_allConfigs` rebuilt every `BuildGump`.
- `DiscordChatAreaControl._selectedChannel`, `isDM` — `DiscordGump/DiscordChatAreaControl.cs:17-18`.
- `QuestArrowGump._direction`, `_mx/_my`, `_needHue`, `_timer` — `QuestArrowGump.cs:45-49`.

Profile-backed state written from here (client-side truth mirrored into `ProfileManager.CurrentProfile`):
- `CounterGumpLocked` — `CounterBarGump.cs:291`, `:561`.
- `InfoBarLocked` (every 250 ms) — `InfoBarGump.cs:134`; `InfoBarSize` on resize — `:147`.
- `PaperdollPosition` — `PaperdollGump.cs:155`, `:533`, `:662`.
- `AdvancedSkillsGumpHeight` — `SkillGumpAdvanced.cs:568` (written every frame while dragging).

## Timing

Per frame (`Update()` is called by `UIManager` once per frame for every gump):
- `Gump.Update` `Gump.cs:121` — flushes `InvalidateContents` -> `UpdateContents()`, forces `ActivePage = 1` if 0.
- `QuestArrowGump.Update` `QuestArrowGump.cs:68` — recomputes direction, screen position, camera clamping **every frame**; hue toggles on a 1000 ms timer (`:200-203`).
- `ResizableGump.Update` `ResizableGump.cs:130` — reads `Mouse.LDragOffset` every frame; applies size when `_clicked`.
- `MapGump.Update` `MapGump.cs:281` — drags `_currentPin` from `Mouse.LDragOffset` deltas.
- `PaperDollGump.Update` `PaperdollGump.cs:504` — checks mobile destroyed, war mode flag, fake-item preview; writes `PaperdollPosition` when moved.
- `EquipmentSlot.Update` `PaperdollGump.cs:891` — per frame per slot (17 slots), does `World.Items.Get` + `mobile.FindItemByLayer` and rebuilds `ItemGumpFixed` on graphic change.
- `SkillGumpAdvanced.Update` `SkillGumpAdvanced.cs:545` — rebuilds the whole list if `_updateSkillsNeeded`; while `Dragging`, resizes and writes the profile height every frame.
- `StandardSkillsGump.Update` `StandardSkillsGump.cs:347` — re-arranges the container when `_container.WantUpdateSize` was set.
- `TipNoticeGump.Update` `TipNoticeGump.cs:85` — copies `_background.SpecialHeight` into `Height` every frame.
- `CounterItem.ImageWithText.Update` `CounterBarGump.cs:771` — copies parent size every frame.
- `Draw` per frame: `CounterItem.Draw` `CounterBarGump.cs:695` (highlight fade math), `ModernColorPicker.HueDisplay.Draw` `:177` (flash animation steps 0.1 alpha per *draw*, not per ms), `ResizableGump.Draw` `:229` (lock icon while Alt held), `UseSpellButtonGump.Draw` `:136`, `SkillGumpAdvanced.Draw` `:580`, `MapGump.Draw` `:341` (redraws pins a second time — comment "HACK" at `:358`), `InfoBarControl.Draw` `:237`, `MusicInfoGump.Draw` `:210`.

250 ms polls:
- `InfoBarGump.Update` reflow + `InfoBarLocked` write — `InfoBarGump.cs:115-117`.
- `InfoBarControl.Update` value+hue refresh — `InfoBarGump.cs:204-206`.
- `MusicInfoGump.Update` audio status + text remeasure — `MusicInfoGump.cs:101-106`.

100 ms poll:
- `CounterItem.Update` — `CounterBarGump.cs:611-613`; walks the whole equipped-container item tree recursively (`GetAmount`, `:674`) to recount.

Other timers:
- `MapGump._pinTimer = Time.Ticks + 300` set on mouse down (`MapGump.cs:392`); pin is only placed on mouse-up if still inside that 300 ms window (`:322`).
- `CounterItem` highlight lasts `HIGHLIGHT_DURATION = 1000` ms (`CounterBarGump.cs:388`, `:665`).
- `QuestArrowGump` blink 1000 ms (`QuestArrowGump.cs:202`).

Per packet / event driven:
- `MapGump` created in `Network/PacketHandlers.cs:3414`; `MapInfos`, `AddPin`, `SetPlotState`, `SetMapTexture` all called from packet handling.
- `TipNoticeGump` created at `Network/PacketHandlers.cs:3807`.
- `QuestArrowGump` created at `Network/PacketHandlers.cs:4351`.
- `PaperDollGump` created at `Network/PacketHandlers.cs:3341` (open-paperdoll packet).
- Skills packet at `Network/PacketHandlers.cs:2135-2163` creates/updates `StandardSkillsGump` / `SkillGumpAdvanced`; `StandardSkillsGump.Update(int skillIndex)` (`:367`) is the per-skill-change entry.

On load / restore:
- `Configuration/Profile.cs:931/948/968/986/997/1001/1026/1179/1220` construct the parameterless ctors and then call `Restore(XmlElement)`.
- `GridHighLightProfile.MigrateGridHighlightToSetup` runs once from `Configuration/ProfileManager.cs:61`.
- `LoginBackground` added at `Game/Scenes/LoginScene.cs:101`; `CharCreationGump` at `LoginScene.cs:282`.
- `MusicInfoGump.Toggle` from `Game/Scenes/GameScene.cs:216` (on entering world) and `ModernOptionsGump.cs:2506`.

## Inbound

- `UIManager` calls `Update()`/`Draw()` on every gump each frame, and routes `OnMouseUp/Down/DoubleClick/Wheel`, `OnKeyUp`, `OnDragEnd`, `OnMove`.
- `Configuration/Profile.cs` `Restore` path -> parameterless ctors + `Gump.Restore` overrides (CounterBar, InfoBar, PaperDoll, StandardSkills, SkillGumpAdvanced, UseSpellButton).
- `Network/PacketHandlers.cs` -> `MapGump` (3414), `TipNoticeGump` (3807), `QuestArrowGump` (4351), `PaperDollGump` (3341), skills gumps (2135-2163).
- `Game/GameActions.cs:326-345` -> `UIManager.GetGump<StandardSkillsGump>()`, `OpenSkills`; `GameActions.cs:486` -> `new ChatGumpChooseName()`.
- `Game/Managers/MacroManager.cs:2130` -> `CounterBarGump.CurrentCounterBarGump.GetCounterItem(i).Use()`; `MacroManager.cs:813-815` -> `StandardSkillsGump`.
- `Game/Managers/HideHudManager.cs:52` -> hides `StandardSkillsGump` / `SkillGumpAdvanced` by flag.
- `Game/Managers/CommandManager.cs:362` -> `new ArtBrowserGump()` (`artbrowser` command); `:122` -> `new ModernColorPicker(null, 8787)`; `:306` -> `new PaperDollGump(World.Player, true)`.
- `Game/UI/Controls/ClickableColorBox.cs:97` -> `new ModernColorPicker(s => Hue = s)`.
- `Game/UI/Gumps/AssistantGump.cs:589/631` -> `new DressAgentConfigGump(...)`.
- `Game/UI/Gumps/DiscordGump/DiscordGump.cs:166` -> `new DiscordChatAreaControl(...)`; DiscordGump also calls `AddMessageToChatBox` / `SetActiveChatChannel`.
- `Game/Scenes/GameScene.cs:216` and `ModernOptionsGump.cs:2506` -> `MusicInfoGump.Toggle(bool)`.
- `Game/Scenes/LoginScene.cs:101/282` -> `LoginBackground`, `CharCreationGump`; `CharCreationGump` is then driven by `LoginScene` via `SetCharacter/SetProfession/SetCity/ShowMessage/StepBack`.
- `LegionScripting/API.cs:3532` -> `new QuestArrowGump(identifier, x, y) { CanCloseWithRightClick = true }` (scripted arrows; `QuestArrowGump.OnMouseUp:212` explicitly early-returns for these).
- `Configuration/ProfileManager.cs:61` -> `GridHighLightProfile.MigrateGridHighlightToSetup`.

## Outbound

Network (`ClassicUO.Network.NetClient.Socket` / `AsyncNetClient`):
- `Send_OpenChat` — `ChatGumpChooseName.cs:186`.
- `Send_MapMessage` — `MapGump.cs:257`, `:268`, `:327`.
- `Send_TipRequest` — `TipNoticeGump.cs:99`, `:105`.
- `Send_ASCIISpeechRequest("party", ...)` — `PaperdollGump.cs:484` (Outlands path).
- `Send_SkillStatusChangeRequest` — `StandardSkillsGump.cs:912`.

`GameActions`: `ReplyGump` (`Gump.cs:337`, `PaperdollGump.cs:457`), `CastSpell` (`CounterBarGump.cs:449`, `UseSpellButtonGump.cs:261/272`), `DoubleClick` (`CounterBarGump.cs:464`), `DropItem` (`CounterBarGump.cs:570`, `PaperdollGump.cs:595`), `Equip` (`PaperdollGump.cs:615`), `UseSkill` (`SkillGumpAdvanced.cs:742`, `StandardSkillsGump.cs:891`), `ChangeSkillLockStatus` (`SkillGumpAdvanced.cs:686/693/700`), `QuestArrow` (`QuestArrowGump.cs:219`), `Print` (many), `RequestProfile`/`RequestHelp`/`OpenSettings`/`OpenJournal`/`RequestQuestMenu`/`OpenSkills`/`OpenGuildGump`/`ToggleWarMode`/`OpenAbilitiesBook`/`OpenPaperdoll`/`OpenMacroGump`.

Managers / systems: `UIManager` (`Add`, `GetGump<T>`, `SavePosition`, `RemovePosition`, `AttemptDragControl`, `KeyboardFocusControl`, `SystemChat.SetFocus`, `MouseOverControl`, `LastControlMouseDown`, `Gumps` linked list), `TargetManager`, `TargetHelper.TargetObject` (Dress agent, Discord share), `DelayedObjectClickManager`, `Pathfinder.WalkTo` (`MapGump.cs:154/157`), `SkillsGroupManager`, `DressAgentManager`, `DiscordManager`, `MacroManager`, `AudioManager` (`MusicInfoGump.cs:111`), `SelectedObject`, `Client.Game.GameCursor.ItemHold`.

Assets/render: `Client.Game.Arts.GetArt/GetRealArtBounds`, `Client.Game.Gumps.GetGump`, `SoundsLoader.TryGetMusicData`, `ClilocLoader.GetString`, `TileDataLoader.StaticData`, `SkillsLoader.Instance`, `FontsLoader.GetWidthASCII`, `ShaderHueTranslator`, `SolidColorTextureCache`, `UltimaBatcher2D`, `TextBox.GetOne` / `RenderedText.Create`.

Other gumps referenced: `WorldMapGump` (`MapGump.cs:106/125`), `MessageBoxGump`, `PartyGump`, `RacialAbilitiesBookGump`, `InspectorGump`, `HealthBarGump`/`HealthBarGumpCustom`/`BaseHealthBarGump`, `StatusGumpBase`, `SkillButtonGump`, `DurabilityGumpMinimized`, `LoadingGump`, `CreateChar*Gump`s.

Cross-cutting: `SDL.SDL_SetClipboardText` (`ArtBrowserGump.cs:97`), `Settings.Load<Settings>/Save<Settings>` (`PaperdollGump.cs:94/98`).

## Hazards

- `ArtBrowserGump.cs:112` — `BuildPage` loops `while (count < maxEntries)` incrementing `index` with no upper bound check against the art file size; the `if (art.Texture != null)` guard that would skip missing art is commented out at `:115`.
- `ArtBrowserGump.cs:59` — hex `graphicInput` sets `Page = (int)p` (the raw graphic id), not a page number, while the decimal branch at `:67` divides by `maxEntries`. Two different meanings for `Page` from one field.
- `ArtBrowserGump.cs:37` — `pageInput.TextChanged` calls `BuildPage`, and `BuildPage:107` writes back into `pageInput.SetTextInternally`; reentrancy is only broken by the `npage != Page` test at `:41`.
- `CharCreationGump.cs:72` and `:136` — `SetAttributes()` is called at `:134` and then `SetStep` is called again at `:136` with a *different* comparison (`>= 0` vs `> 0`), so `SetProfession` performs two step transitions with different predicates for `DescriptionIndex == 0`.
- `CharCreationGump.cs:96` — the skill-reset loop indexes `_character.Skills[info.SkillDefVal[k, 0]]` without the `>= Skills.Length` guard applied to `skillIndex` at `:86`.
- `ChatGumpChooseName.cs:135` — `Width = Width - -x - 17` (double negative), giving a text box wider than the gump.
- `CounterBarGump.cs:213-261` — `ApplyLayout` allocates `indices` as all-zero, sets surviving entries to `-1`, then at `:255` disposes `items[i]` whenever `indices[i] >= 0`. Index 0 of a surviving item is set to `-1` so it is kept, but any slot never visited keeps value `0`, which passes `>= 0` and disposes `items[i]` — the disposal set is derived from default-zero rather than an explicit "surplus" marker.
- `CounterBarGump.cs:239` — `ApplyLayout` calls `Add(new CounterItem(...))` inside the row/col loop while `items` (a snapshot from `GetControls<CounterItem>()` at `:213`) is still being indexed; new children are appended to `Children` during the same pass.
- `CounterBarGump.cs:349` — `Restore` calls `items[index]?.SetGraphic(...)` after already dereferencing `items[index].SpellID` unguarded at `:345`.
- `CounterBarGump.cs:329` — `Restore` calls `BuildGump()` which `Add`s a second `_background` and a fresh set of `CounterItem`s; the parameterless ctor (`:62`) did not build, but `_background` is reassigned without disposing any prior one.
- `CounterBarGump.cs:611` — `CounterItem.Update` dereferences `World.Player.Items` (`:630`) with no null check on `World.Player`; runs every 100 ms while the parent is enabled.
- `CounterBarGump.cs:690` — `GetAmount` calls `SetTooltip(item)` for every matching item found during the recursive walk, so the tooltip ends up bound to whichever item was visited last.
- `CounterBarGump.cs:687` — amount matching is by `Graphic` + `Hue` only; the stored `Graphic`/`Hue` outlive the item that seeded them.
- `CounterBarGump.cs:761` — `ImageWithText.ChangeGraphic` reads `Parent.Height` with no null check; `_label` is added in the ctor before the control has a parent.
- `CounterBarGump.cs:423` — `SetGraphic(0, hue)` calls `_image.ChangeGraphic(0, ...)` and returns early *without* clearing `Graphic`, so `Graphic` keeps the old value while the image is blank.
- `CounterBarGump.cs:660` — highlight-on-use compares `int.TryParse(_image.GetText())` against the new amount; when abbreviated display is on (`:646`) the function returns before reaching this, so the comparison text can be a stale abbreviated string.
- `DiscordChatAreaControl.cs:177` — `Dispose()` calls `base.Dispose()` first and unsubscribes `_chatInput.EnterPressed` afterwards.
- `DiscordChatAreaControl.cs:147` — `_chatDataBox` grows unbounded; nothing trims message history in this control.
- `DiscordChatAreaControl.cs:73` — the "Share Item" target callback captures `_selectedChannel` implicitly via `this`, so the channel read at target-completion time may differ from the channel at click time.
- `DressAgentConfigGump.cs:37` — `BuildGump` calls `Clear()` and rebuilds all children; it is re-entered from `OnConfigSelected` (`:261`), `CreateNewConfig` (`:286`) and `DeleteCurrentConfig` (`:306`), i.e. from inside event handlers owned by the controls being cleared.
- `DressAgentConfigGump.cs:363` — the per-item delete button captures the loop variable `item` and calls `RefreshItemsList()` from inside the handler, disposing the button that is executing.
- `DressAgentConfigGump.cs:94` — `nameInput.TextChanged` writes `_config.Name` and calls `DressAgentManager.Instance.Save()` on every keystroke.
- `DressAgentConfigGump.cs:57` — `_allConfigs.FindIndex(c => c == _config)` is reference equality; if the manager re-materialises configs the selection silently falls back to index 0 (`:59`).
- `Gump.cs:83` and `:94` — the alpha-scroll loops mutate `c.Alpha` for all `Children`; the down branch at `:94` has no `< 0` clamp (the up branch clamps at `:86`).
- `Gump.cs:255` — `Restore` adds `alpha` to every child's `Alpha` with no clamp.
- `Gump.cs:139` — `Gump.Dispose` looks up `World.Items.Get(LocalSerial)` and clears `Opened`; for gumps whose `LocalSerial` is a mobile or an arbitrary id this can hit an unrelated item if the serial has been reused.
- `InfoBarGump.cs:185` — `_data.X = _label.IsVisible ? _label.Width + 3 : _pic.Width;` dereferences `_pic`, which is only assigned at `:179` inside the `label.StartsWith("\\")` + `ushort.TryParse` branch. A label starting with `\` whose remainder does not parse leaves `_label.IsVisible == true`, but a non-`\` label leaves `_pic` null and `_label` visible so the ternary short-circuits; the null path is reachable only if `_label.IsVisible` is false with `_pic` null.
- `InfoBarGump.cs:264` — `Draw` reads `Parent.Height` with no null check.
- `InfoBarGump.cs:134` — `ProfileManager.CurrentProfile.InfoBarLocked = IsLocked` is written from `Update` every 250 ms, so any external change to the profile value is overwritten by the gump within a quarter second.
- `InfoBarGump.cs:77-92` — `ResetItems` disposes and re-creates every `InfoBarControl`; called from `UpdateAllOptions` (`:100`) which iterates `UIManager.Gumps.OfType<InfoBarGump>()`.
- `MapGump.cs:295` and `:304` — pin clamping compares `_currentPin.X` against `_hit.Width` / `_hit.Height` (sizes) rather than `_hit.X + _hit.Width` / `_hit.Y + _hit.Height`.
- `MapGump.cs:322` — pin placement requires `_pinTimer > Time.Ticks`, i.e. the click must land within 300 ms of mouse-down; combined with the `< 5 px` drag test at `:320`.
- `MapGump.cs:341` — `Draw` uses `_mapTexture` with no null check; `SetMapTexture` (`:170`) may not have been called yet when the gump is created at `PacketHandlers.cs:3414`.
- `MapGump.cs:170` — `SetMapTexture` disposes the previous texture and reassigns; a frame drawing the old texture in the same tick would see a disposed object.
- `MapGump.cs:429` — `float cosA = (float)Math.Sin(a * pi / 180f);` — cosine computed with `Math.Sin`. (`LineUnderMouse` has no callers in this file.)
- `MapGump.cs:217` — `mapX`/`mapY` are mutated in place from the pin position on the first `AddPin` only (`foundMapLoc` latch at `:222`), so the stored map origin from `MapInfos` is destroyed after the first pin.
- `MapGump.cs:356-359` — `Draw` walks `_container` and calls `Draw` on each pin a second time after `base.Draw` already drew them.
- `ModernColorPicker.cs:31/36` — the `cPage` setter wraps out-of-range values by assigning `_cPage` and `return`ing, so `cPage++` past 14 sets 0 and `cPage--` below 0 sets 14 — but callers at `:77`/`:80` then print `cPage + 1` while the label was initialised with `cPage.ToString()` at `:71`.
- `ModernColorPicker.cs:90` — `FillHueDisplays` disposes `area.Children` while enumerating that same collection with `foreach`.
- `ModernColorPicker.cs:98` — hue index is `(row + (col-1)*ROWS) + page*200 - 1`, so page 0 row 1 col 1 yields hue `0`; page 14 reaches 2999.
- `ModernColorPicker.cs:122` — the `Hue` setter fires both `HueChanged` and the `hueChanged` callback; `HueDisplay.OnMouseUp:165` creates a nested `ModernColorPicker` whose callback writes `Hue`, re-entering the setter.
- `ModernColorPicker.cs:189` — flash alpha steps by 0.1 per `Draw` call, i.e. framerate-dependent, not time-based.
- `MusicInfoGump.cs:108` — `_lastPosition` is static and updated every 250 ms; a second instance would be positioned from the first's coordinates.
- `PaperdollGump.cs:126` — `IsMinimized` setter iterates `Children` setting `IsVisible`, immediately after `Insert(0, _picBase)` added a child inside the same setter.
- `PaperdollGump.cs:120` — `_picBase.Dispose()` is called unconditionally in the `IsMinimized` setter; `_picBase` is null until `BuildGump` runs, and `Restore` (`:675`) sets `IsMinimized` after `BuildGump`, but the parameterless ctor path leaves it null.
- `PaperdollGump.cs:354` — `Mobile mobiles = World.Mobiles.Get(LocalSerial); Item twoHandedItem = mobiles.FindItemByLayer(...)` — unguarded dereference, and `twoHandedItem` is never used.
- `PaperdollGump.cs:609` — `container.FindItemByLayer(...)` where `container` came from `World.Mobiles.Get(LocalSerial)` at `:584` with no null check.
- `PaperdollGump.cs:1013` — `ItemGumpFixed.Draw` calls `Dispose()` when `item == null` at `:1004` and then reads `item.Hue` at `:1013`; only the `IsDisposed` early-return at `:1007` prevents the deref, which depends on `Dispose` setting `IsDisposed` synchronously.
- `PaperdollGump.cs:919` — `EquipmentSlot.LocalSerial` is reassigned to whatever item currently occupies the layer; the slot's identity therefore changes under any code holding the old serial.
- `PaperdollGump.cs:87-99` — `Settings` is a process-wide static shared by the player paperdoll and every other-player paperdoll; `Position_Y_Journal` and `Position_Y_Quest` are both `44 + 27*3` (`:1127`, `:1130`).
- `PaperdollGump.cs:579-653` — `OnMouseUp` calls `base.OnMouseUp` at `:581` and again at `:652` on the `else` path.
- `PaperdollGump.cs:675` — `bool.Parse(xml.GetAttribute("isminimized"))` throws on a missing/blank attribute.
- `QuestArrowGump.cs:72-77` — `Update` calls `Dispose()` when `!World.InGame` and then continues to `Client.Game.GetScene<GameScene>()`; the `IsDisposed` check at `:79` is what stops it.
- `QuestArrowGump.cs:200` — `_timer` is a `float` compared against `Time.Ticks` (uint ms); precision degrades as the session runs long.
- `QuestArrowGump.cs:212` — `OnMouseUp` returns early when `CanCloseWithRightClick` is true, which is exactly the flag `LegionScripting/API.cs:3532` sets — server quest-arrow replies are suppressed for scripted arrows by that shared flag.
- `ResizableGump.cs:66-73` — `_borderControl` is constructed with `Width`/`Height` before they are assigned at `:92-93`.
- `ResizableGump.cs:139` — `_lastSize = _savedSize` at the top of every `Update`, so a size set directly on `Width`/`Height` outside `ResizeWindow` is reverted on the next frame (`:162`).
- `ResizableGump.cs:192-194` — `_prevCanMove ??= CanMove` captures the *current* values on first `SetLockStatus` call; if the first call happens while already locked, the "previous" state recorded is the locked state.
- `ResizableGump.cs:216-226` — the alt-click lock toggle tests `x`/`y` against the 0x82C gump bounds, using coordinates relative to whatever control was hit, not the gump.
- `SkillGumpAdvanced.cs:67` — `public static bool Dragging` is shared: two open advanced-skill gumps both resize when either grip is dragged (`Update:563`).
- `SkillGumpAdvanced.cs:551` — `Update` disposes every `Label` in `Children` before `BuildGump`, and `BuildGump` re-adds `real`/`value` labels at `:507-508` without removing the previous ones from `Children` first (they were disposed, not removed).
- `SkillGumpAdvanced.cs:344-363` — `BuildGump` sorts and `Reverse()`s the shared `SkillsGroupManager.Groups` list in place, mutating global group ordering as a side effect of drawing.
- `SkillGumpAdvanced.cs:400` — `skills.OrderBy(s => pi.GetValue(s, null))` reflects a property whose name comes from XML (`Restore:529`); an unknown name makes `pi` null and `pi.GetValue` throws.
- `SkillGumpAdvanced.cs:374-387` — the group header's `MouseUp` handler stores collapse state in `a.Tag` (boxed bool) *and* in `g.IsMaximized`, updating both independently.
- `SkillGumpAdvanced.cs:568` — writes `ProfileManager.CurrentProfile.AdvancedSkillsGumpHeight` every frame while dragging.
- `SkillGumpAdvanced.cs:718` — `SkillListEntry.OnMouseOver` operator precedence: `Mouse.LButtonPressed && Math.Abs(X) >= N || Math.Abs(Y) >= N` — the Y branch is not gated on the button being pressed.
- `SkillGumpAdvanced.cs:628` — `skillDoubleClick` reads `_skill`, which is assigned at `:647` *after* the handler is wired at `:624` (fine at invoke time, but the closure is created before the field is set).
- `StandardSkillsGump.cs:721-742` — `OnKeyUp` delete loop is `while (_box.Children.Count != 0)` but the body only *inserts* into `first._box` inside a matching `for`; if no match is found the child is never removed from `_box` and the loop does not terminate.
- `StandardSkillsGump.cs:642-659` — `OnMouseOver` moves a `SkillItemControl` between groups by mutating `originalGroup._skills`, `_group`, `_skills` and `_box` from inside a mouse-over callback, while the drag source is still parented elsewhere.
- `StandardSkillsGump.cs:633` — `(SkillsGroupControl) skillControl.Parent.Parent` unchecked cast (only the depth-2 shape is assumed).
- `StandardSkillsGump.cs:572` — `IsMinimized` setter dereferences `Parent.WantUpdateSize`; the setter is invoked at `:274` and `:311` right after `_container.Add(control)`, so ordering matters.
- `StandardSkillsGump.cs:228` — `IsMinimized` setter iterates `Children` setting `IsVisible = !value` for every child, then restores `_gumpPic.IsVisible = true`; any control added while minimized stays visible.
- `StandardSkillsGump.cs:118` — ctor does `World.Player.Skills.Sum(...)` with no null check on `World.Player`, while `LoadSkills:303` does check.
- `StandardSkillsGump.cs:414` — `int.Parse(xml.GetAttribute("height"))` throws on missing attribute.
- `StandardSkillsGump.cs:912` — `Send_SkillStatusChangeRequest` then `skill.Lock = newStatus` locally at `:914`; the client sets its own lock state before the server confirms.
- `SkillGumpAdvanced.cs:685-701` — same pattern: `_skill.Lock` is assigned client-side alongside `ChangeSkillLockStatus`.
- `StandardSkillsGump.cs:206` and `SkillGumpAdvanced.cs:307` — both gumps report `GumpType.SkillMenu`, so the saved-gump restore path distinguishes them only by `ProfileManager.CurrentProfile.StandardSkillsGump` (`Profile.cs:995`).
- `TipNoticeGump.cs:89` — `Height` and `_scrollArea.Height` are recomputed from `_background` every frame with no change test.
- `UseSpellButtonGump.cs:66` — ctor does `Client.Game.GetScene<GameScene>().Macros` unguarded; the parameterless ctor is used by the profile restore path (`Profile.cs:1026`).
- `UseSpellButtonGump.cs:289` — `SpellDefinition.FullIndexGetSpell(int.Parse(...))` in `Restore`; `BuildGump:86` then dereferences `_spell.GumpIconSmallID`.
- `UseSpellButtonGump.cs:131-133` — `Alpha += AlphaOffset` in `BuildGump`, and `Gump.Restore:253-256` also adds `alpha` to `Alpha` and children; `Restore` calls `BuildGump` at `:290` after `base.Restore`, so the offset is applied twice on the restore path.
- `UseSpellButtonGump.cs:110` — `hotKeyString += (char)macro.Key` casts an SDL keycode to a char.
- `GridHighLight/GridHighLightMigration.cs:20-47` — the migration reads `profile.GridHighlight_Name.Count` as the authority and pulls every parallel list with `ElementAtOrDefault`, then clears all legacy lists at `:50-57`; entries present in a longer parallel list but absent from `_Name` are dropped silently.

## Fork deltas

Clear TazUO / Holiday additions (no upstream ClassicUO license header, or obviously non-stock behaviour):

- `MusicInfoGump.cs` — **Holiday-specific.** No license header, doc-comment written in this fork's prose style, references `Client.Game.Audio.GetMusicStatus()`, `st.Era`, `Constants.MUSIC_STOP_INDEX`, music-era config. `ShouldBeSaved => false` with an explanatory comment about two sources of truth. Toggled from `Settings.GlobalSettings.MusicOverlay`.
- `ArtBrowserGump.cs` — no license header; dev tool wired to the `artbrowser` command.
- `DiscordGump/DiscordChatAreaControl.cs` — no license header; whole Discord SDK integration (`Discord.Sdk`, `DiscordManager`) is a fork feature.
- `DressAgentConfigGump.cs` — no license header; `DressAgentManager`, `NineSliceGump`, `ModernUIConstants`, `ModernScrollArea`, `VBoxContainer`, `TargetHelper` are all TazUO-era types.
- `GridHighLight/GridHighLightMigration.cs` — no license header; grid highlight is a TazUO feature, this migrates an older TazUO schema.
- `ModernColorPicker.cs` — no license header; `serial == 8787` magic value at `:98` selects "print the hue to chat" mode, driven from `CommandManager.cs:122`.
- `Gump.cs` — fork additions inside a stock file: `AlphaOffset` + alt-wheel alpha scrolling (`:71`, `:73-97`), `IsLocked`/`CanBeLocked`/`OnLockedChanged` (`:99-119`, `:305`), `Controller.Button_LeftTrigger` in the lock chord (`:152`), `PacketGumpText` (`:59`), `GlobalScaling`-aware `CenterXInScreen`/`CenterYInScreen`/`CenterXInViewPort`/`CenterYInViewPort` (`:169-231`).
- `ResizableGump.cs` — `SetLockStatus` / `_prevCanMove` / `_prevCloseWithRightClick` / `_prevBorder` lock machinery (`:50-51`, `:190-210`), alt-click lock toggle and lock-icon overlay draw (`:212-257`).
- `CounterBarGump.cs` — `CurrentCounterBarGump` static (`:54`), spell binding (`SpellID`, `GenSpellList`, gump-icon rendering, `:410`, `:417`, `:468-551`, `:753-832`), abbreviated amounts (`CounterBarDisplayAbbreviatedAmount`), highlight-on-use (`HIGHLIGHT_DURATION`, `:658-668`, `:711-720`), ctrl+alt lock (`:289`, `:557`).
- `PaperdollGump.cs` — the entire `Settings : UISettings` class (`:1053-1180`) and every `settings.*` reference; `Scale`/`InternalScale`/`ScaleWidthAndHeight`/`ScaleXAndY`/`SetInternalScale` scaling calls throughout; `DurabilityGumpMinimized` (`:346`); `PaperdollScale`, `PaperdollPosition`, `PaperdollLocked` profile fields.
- `SkillGumpAdvanced.cs` — skill groups inside the advanced gump (`SkillsGroupManager.IsActive`, `:239-253`, `:342-483`), Lock column and lock cycling (`:199-214`, `:676-705`), drag-resize (`Dragging`, `:262-266`, `:534-577`), static `last_x/last_y/last_button` with `Save`/`Restore` of sort state (`:513-532`), `AdvancedSkillsGumpHeight` profile field.
- `StandardSkillsGump.cs` — static `last_x/last_y` (`:69`, `:194-199`), `_resetGroups` NiceButton + reset-confirmation MessageBoxGump (`:172-180`, `:278-298`), `AsyncNetClient` usage at `:912`.
- `MapGump.cs` — the whole `MenuButton` context menu (`:95-164`): world-map marker, "Add as marker", pathfind-to-estimate, plus `MapInfos`/`mapX/mapY/mapFacet`/`foundMapLoc` estimation (`:181-226`) and the tooltip "Estimated loc". Stock ClassicUO has no such menu.
- `QuestArrowGump.cs` — the `CanCloseWithRightClick` early-return at `:212` with the comment "This is set to true in Python API"; Python scripting API integration.
- `UseSpellButtonGump.cs` — `SpellIconScale` sizing (`:64-65`), `SpellIcon_DisplayHotkey` / `SpellIcon_HotkeyHue` hotkey label (`:95-124`), `AlphaOffset` application (`:131-133`).
- `InfoBarGump.cs` — `ResizableGump` base with lock, `TextBox.GetOne` TTF rendering with `InfoBarFont`/`InfoBarFontSize`, `ResizableStaticPic` graphic-instead-of-label via a leading `\` (`:174-181`), `InfoBarHighlightType` warning lines (`:216-231`, `:241-269`), `InfoBarSize`/`InfoBarLocked` profile fields.
- `CharCreationGump.cs` — `CUOEnviroment.IsOutlands` special-case at `:91`.

Stock-looking (2021 andreakarasho header, little or no fork content): `ChatGumpChooseName.cs`, `Login/LoginBackground.cs`, `TipNoticeGump.cs`.
