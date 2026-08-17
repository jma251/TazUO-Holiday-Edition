# ui-controls-a

Partition = every **odd-numbered** file (1st, 3rd, 5th … of the sorted list) under
`src/ClassicUO.Client/Game/UI/Controls` + `src/ClassicUO.Client/Game/UI` (maxdepth 2).
85 files, 32,639 lines, all read in full.

Root paths below are relative to `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/UI/`.

---

## Files

### Controls/ (35)

| Path | Lines | Purpose |
| --- | --- | --- |
| `Controls/AlphaBlendControl.cs` | 86 | Solid translucent rect; caches `hueVector` on Hue/Alpha set. Backdrop for nearly every modern gump. |
| `Controls/Area.cs` | 34 | Bare container with optional 1px gray border. Layout box used everywhere in TazUO gumps. |
| `Controls/AssistantGumpControls/OrganizerControl.cs` | 326 | Organizer-agent editor embedded in AssistantGump; source/dest container targeting, per-item amount/enable rows. |
| `Controls/ButtonTileArt.cs` | 95 | Server gump element: `Button` + a tile-art overlay at `_tileX/_tileY`. Disposes itself in ctor if art missing. |
| `Controls/CheckerTrans.cs` | 69 | Server gump element: black rect at fixed 0.5 alpha. |
| `Controls/ClickableColorBox.cs` | 115 | `ColorBox` + 0x00D4 frame; left-click opens `ColorPickerGump` or `ModernColorPicker`. |
| `Controls/ColorPickerBox.cs` | 254 | Grid of hue cells (rows×cols); `ShowLivePreview` samples the mouse every Update. Derives from `Gump`, not `Control`. |
| `Controls/Combobox.cs` | 293 | Closed dropdown label + arrow; opening spawns a modal `ComboboxGump` (nested class) holding `HoveredLabel`s in a `ScrollArea`. |
| `Controls/Control.cs` | 1120 | **Base class for the entire UI tree.** Bounds, parenting, page filtering, hit-test, event plumbing, Update/SlowUpdate/Draw recursion, Dispose. |
| `Controls/DataBox.cs` | 133 | Manual-layout child holder; `ReArrangeChildren` (vertical stack) and `ReArrangeChildrenGridStyle` (wrapping grid). `Contains` delegates to children unless `ContainsByBounds`. |
| `Controls/ExternalUrlImage.cs` | 82 | Downloads an image over `WebClient` on a `Task.Run`, caches by URL in a **static non-concurrent** `Dictionary`. |
| `Controls/GumpControlInfo.cs` | 40 | Only holds `enum UILayer { Over, Default, Under }`. |
| `Controls/GumpPicExternalUrl.cs` | 112 | HTTP image via `HttpClient` + `System.Drawing`, on `Task.Factory.StartNew`; draws tiled or stretched. |
| `Controls/GumpPicWithWidth.cs` | 72 | `GumpPic` drawn tiled to `Percent` pixels wide (server progress-bar element). |
| `Controls/HSliderBar.cs` | 402 | Horizontal slider, two gump styles; `_pairedSliders` redistributes deltas (stat sliders on char creation). |
| `Controls/HorizontalScrollArea.cs` | 87 | Entirely commented out; namespace only. |
| `Controls/HotkeyControl.cs` | 115 | Label + list of `HotkeyBox`; binds/unbinds via `GameScene.Hotkeys`. |
| `Controls/HtmlControl.cs` | 335 | `RenderedText` with `IsHTML`, own `ScrollBar`/`ScrollFlag`, weblink click → `PlatformHelper.LaunchBrowser`. |
| `Controls/InfoBarBuilderControl.cs` | 101 | One InfoBar row: label textbox + var Combobox + `ClickableColorBox` + delete. |
| `Controls/ItemGump.cs` | 356 | The in-container item icon. Per-frame drag-to-pickup detection, `SelectedObject.Object` write on hover, pixel-accurate `Contains`. |
| `Controls/Line.cs` | 77 | 1-colour rect; refreshes `hueVector` from `Alpha` only in `SlowUpdate`. |
| `Controls/MenuButton.cs` | 31 | Three-bar "hamburger"; `Contains` always true. |
| `Controls/ModernScrollBar.cs` | 250 | Fork-added flat scrollbar (`ScrollBarBase` impl) with drawn arrows, hover/press thumb colours. |
| `Controls/NameOverheadAssignControl.cs` | 296 | Nameplate-filter editor: hotkey box + ~20 checkboxes in a `ScrollArea`, writes `NameOverheadOption.NameOverheadOptionFlags`. |
| `Controls/NineSliceButton.cs` | 63 | `NineSliceControl` that swaps texture/border on mouse down/up and hue on enter/exit. |
| `Controls/PaperDollInteractable.cs` | 574 | Rebuilds the layered paperdoll avatar from `Mobile.FindItemByLayer`; `GumpPicEquipment` nested class handles lift/double-click. |
| `Controls/RenderedMapArea.cs` | 212 | Renders a whole map facet to one `Texture2D` on a background `Task`; static `ConcurrentDictionary<int,Texture2D>` cache. |
| `Controls/ResizePic.cs` | 436 | 9-slice UO gump background; pixel-accurate `Contains` across all nine pieces. |
| `Controls/ScrollArea.cs` | 267 | Clip + offset container. **Children[0] is always the scrollbar**; content starts at index 1. |
| `Controls/ScrollBarBase.cs` | 249 | Abstract scrollbar: Value/Min/Max, held-button auto-repeat with accelerating `_StepChanger`. |
| `Controls/SettingsSection.cs` | 106 | Title + rule + inner `DataBox`; `Add`/`AddRight` auto-position with an indent stack. |
| `Controls/SimpleProgressBar.cs` | 65 | Two stacked `AlphaBlendControl`s; `SetProgress(value,max)` resizes the foreground. Python-API bar. |
| `Controls/StbTextBox.cs` | 1147 | Text input built on `StbTextEditSharp`: caret, selection, clipboard, SDL text input, placeholder, numbers-only. |
| `Controls/TableContainer.cs` | 92 | Container driven by `Positioner` in table mode (columns × columnWidth). |
| `Controls/VBoxContainer.cs` | 93 | Container driven by `Positioner` in vertical mode. |

### UI/ root (2)

| Path | Lines | Purpose |
| --- | --- | --- |
| `Positioner.cs` | 310 | Layout cursor: vertical/horizontal flow, indent, blank line, and a table mode with per-column alignment. |
| `XmlGumpHandler.cs` | 1167 | Loads `Data/XmlGumps/*.xml` into `XmlGump`; tag handlers for text, colorbox, image, progress bars, macro buttons, borders, hp bars. Also defines `XmlGump` and `XmlHealthBar`. |

### Gumps/ (48)

| Path | Lines | Purpose |
| --- | --- | --- |
| `Gumps/AnimBrowser.cs` | 124 | Debug grid of `AnimationDisplay` cells, paged by graphic index; double-click copies id. |
| `Gumps/AssistantGump.cs` | 1850 | The fork's assistant hub: autoloot/sell/buy, graphic filter, spellbar, HUD flags, spell indicators, journal filter, title bar, dress, bandage, friends, organizer. |
| `Gumps/BoatControl.cs` | 155 | 8-direction boat steering + stop, speed radio checkboxes; sends via `BoatMovingManager`. |
| `Gumps/BulletinBoardGump.cs` | 581 | Board list + `BulletinBoardItem` (post/reply/remove) + `BulletinBoardObject` row. |
| `Gumps/ChatGumpChooseName.cs` | 192 | Chat name entry; OK sends `Send_OpenChat`. |
| `Gumps/CombatBookGump.cs` | 842 | Weapon-ability book; per-page index labels, ability icons draggable to `UseAbilityButtonGump`. |
| `Gumps/ContainerGump.cs` | 823 | Classic container: gump background, per-item `ItemGump`s, drop targeting/coordinate clamping, corpse eye animation. |
| `Gumps/CounterBarGump.cs` | 850 | Grid of `CounterItem` cells counting items in backpack (or casting a spell); nested `ImageWithText`. |
| `Gumps/CustomToolTip.cs` | 185 | Free-floating OPL tooltip with retry loop; disposes itself when the hover reference loses the mouse. |
| `Gumps/DressAgentConfigGump.cs` | 396 | Dress-config editor: combobox of configs, item list, dress/undress/macro/delete. |
| `Gumps/FileSelector.cs` | 361 | In-client file/directory browser; drive list at root, extension filter, `_lastPath` static memory. |
| `Gumps/GridLootGump.cs` | 591 | Corpse loot grid; two display groups (non-stackable then stackable), paged, per-item amount slider. |
| `Gumps/GumpType.cs` | 71 | `enum GumpType` — the save/restore discriminator for profile gump persistence. |
| `Gumps/HouseCustomizationGump.cs` | 2148 | House designer: state machine (wall/door/floor/stair/roof/misc/menu), per-floor visibility, component/fixture counters, all server packets. |
| `Gumps/ImprovedBuffGump.cs` | 232 | Buff cooldown bars; static `BuffBarManager` holds a fixed `CoolDownBar[20]`. |
| `Gumps/InputRequest.cs` | 62 | Two-button modal text prompt with `Action<Result,string>` callback. |
| `Gumps/JournalGump.cs` | 643 | Classic journal; nested `RenderedTextList` with `Deque` ring of 200 entries; subscribes `EventSink.JournalEntryAdded`. |
| `Gumps/MacroButtonGump.cs` | 304 | Anchorable macro button; graphic/hue/scale from `Macro`; runs macro on click or double-click. |
| `Gumps/MapGump.cs` | 572 | Treasure/server map with pins, plot state, world-map handoff context menu. |
| `Gumps/MenuGump.cs` | 363 | Server item menu (horizontal scroller) + `GrayMenuGump` (radio list). |
| `Gumps/MiniMapGump.cs` | 480 | Radar minimap; rebuilds the gump texture in place from map blocks + statics + multis; blinking dots at 500 ms. |
| `Gumps/ModernColorPicker.cs` | 225 | 10×20 hue grid, 15 pages; nested `HueDisplay` cell with flash animation. |
| `Gumps/ModernPaperdoll.cs` | 884 | Fork paperdoll: fixed `ItemSlot` grid keyed by `Layer[]`, durability bars, nested Menu/Preview/Minimized gumps. |
| `Gumps/MultiItemMoveGump.cs` | 428 | Static selection set + queue feeding `MoveItemQueue`; destination = container / ground / trade window. |
| `Gumps/MusicInfoGump.cs` | 222 | Fork/Holiday diagnostic panel for the music system; 250 ms refresh, `ShouldBeSaved => false`. |
| `Gumps/NameOverheadGump.cs` | 952 | One nameplate per entity: position from animation dimensions each Draw, HP/mana/stam bars, overlap avoidance. |
| `Gumps/NetworkStatsGump.cs` | 209 | Ping/in/out; 100 ms refresh; static `_last_position`. |
| `Gumps/PaperdollGump.cs` | 1182 | Classic paperdoll; all graphics/positions come from a JSON-backed `Settings : UISettings` static. |
| `Gumps/PartyInviteGump.cs` | 114 | Accept/decline party invite; writes `World.Party.Leader/Inviter` client-side. |
| `Gumps/ProfileGump.cs` | 252 | Character profile viewer/editor; sends `Send_ProfileUpdate` on dispose if text changed. |
| `Gumps/QuestArrowGump.cs` | 233 | Screen-edge-clamped quest arrow; recomputes world→screen every Update, hue blinks at 1000 ms. |
| `Gumps/RGBColorPickerGump.cs` | 90 | NineSlice RGB picker wrapping `ColorSelectorControl`. |
| `Gumps/RacialAbilitiesBookGump.cs` | 381 | Race ability book; passive/active, drag to `RacialAbilityButton`, gargoyle flying double-click. |
| `Gumps/ResizableGump.cs` | 258 | Abstract resizable `AnchorableGump`: corner drag button, alt-click lock toggle with lock overlay. |
| `Gumps/SelectableItemListGump.cs` | 170 | Modal keyboard-navigable string list (chat autocomplete). |
| `Gumps/SimpleTimedTextGump.cs` | 42 | Floating text that disposes itself in `Draw` once `expireAt` passes. |
| `Gumps/SkillGumpAdvanced.cs` | 753 | Sortable skill table with optional groups; drag-resize height; `SkillListEntry` rows. |
| `Gumps/SpellbookGump.cs` | 1583 | All spellbook types; index pages + per-spell pages; `HueGumpPic` highlights active spells and does fast macro assign. |
| `Gumps/StandardSkillsGump.cs` | 1036 | Classic grouped skills gump; drag skills between `SkillsGroupControl`s; rename via `StbTextBox` state machine. |
| `Gumps/Supporters.cs` | 123 | Scrolling credits over an embedded PNG. |
| `Gumps/TextContainerGump.cs` | 92 | Abstract base adding a `TextRenderer` for overhead text inside a gump (containers, paperdoll). |
| `Gumps/TipNoticeGump.cs` | 132 | Tip/notice scroll, prev/next `Send_TipRequest`. |
| `Gumps/TopBarGump.cs` | 495 | Top menu bar; paged minimize, "More +" context menu, dynamic Xml-gump submenu. |
| `Gumps/UpdateTimerViewer.cs` | 74 | Profiler view of `UIManager.UpdateTimerTotalTime`; toggles `UIManager.UpdateTimerEnabled`. |
| `Gumps/UseSpellButtonGump.cs` | 293 | Anchorable spell icon; cast on click/double-click, ctrl+alt fast macro assign. |
| `Gumps/VersionHistory.cs` | 350 | Static changelog strings in a scrollable `VBoxContainer`; rebuilt on resize. |
| `Gumps/WorldViewportGump.cs` | 579 | Game window frame: border, resize grip, `SystemChatControl` host, low-HP screen outline. Also defines `BorderControl`. |

---

## Types

### Core

| Type | file:line | Responsibility |
| --- | --- | --- |
| `Control` (abstract) | `Controls/Control.cs:47` | Base of everything. `ref Rectangle Bounds` exposed as `ref int X/Y/Width/Height`; `Children` list; `Page`/`ActivePage` filtering; virtual `Update`, `SlowUpdate`, `Draw`, `Contains`, `HitTest`; the whole mouse/keyboard/controller event surface. |
| `UILayer` (enum) | `Controls/GumpControlInfo.cs:35` | `Over` / `Default` / `Under` — draw+hit ordering bucket used by UIManager. |
| `GumpType` (enum) | `Gumps/GumpType.cs:35` | Save/restore discriminator; TazUO adds `GridContainer = 8787`, `NearbyCorpseLoot`, `SpellBar`, `MenuGump`, `TextEntryDialogGump`, `MacroButtonEditor = 6464`, `DurabilityGump = 6465`. |
| `Positioner` | `Positioner.cs:13` | Layout cursor. `Position(c)` sets `c.X/c.Y` and advances; `StartTable/EndTable/NextTableRow/PositionInTableCell`; `TableColumnAlignment` at `Positioner.cs:305`. |

### Containers / layout

`DataBox` `Controls/DataBox.cs:38` · `ScrollArea` `Controls/ScrollArea.cs:46` · `ScrollbarBehaviour` `Controls/ScrollArea.cs:40` ·
`Area` `Controls/Area.cs:6` · `VBoxContainer` `Controls/VBoxContainer.cs:5` · `TableContainer` `Controls/TableContainer.cs:3` ·
`SettingsSection` `Controls/SettingsSection.cs:3`.

### Scroll / slider

`ScrollBarBase` (abstract) `Controls/ScrollBarBase.cs:40` · `ModernScrollBar` `Controls/ModernScrollBar.cs:7` ·
`HSliderBar` `Controls/HSliderBar.cs:49` · `HSliderBarStyle` `Controls/HSliderBar.cs:43`.

### Drawing primitives

`AlphaBlendControl` `Controls/AlphaBlendControl.cs:38` · `Line` `Controls/Line.cs:39` · `CheckerTrans` `Controls/CheckerTrans.cs:39` ·
`ResizePic` `Controls/ResizePic.cs:43` · `GumpPicWithWidth` `Controls/GumpPicWithWidth.cs:39` ·
`BorderControl` `Gumps/WorldViewportGump.cs:361` (9-piece frame with settable corner/edge graphics) ·
`MenuButton` `Controls/MenuButton.cs:5`.

### Input / text

`StbTextBox` `Controls/StbTextBox.cs:48` (implements `ITextEditHandler`) · `HtmlControl` `Controls/HtmlControl.cs:46` ·
`Combobox` `Controls/Combobox.cs:44` + nested `ComboboxGump` `Controls/Combobox.cs:173` ·
`HotkeyControl` `Controls/HotkeyControl.cs:41`.

### Item / entity views

`ItemGump` `Controls/ItemGump.cs:46` · `PaperDollInteractable` `Controls/PaperDollInteractable.cs:47` +
nested `GumpPicEquipment` `Controls/PaperDollInteractable.cs:487` ·
`ContainerGump.GumpPicContainer` `Gumps/ContainerGump.cs:806` ·
`PaperDollGump.EquipmentSlot` `Gumps/PaperdollGump.cs:840` + `EquipmentSlot.ItemGumpFixed` `Gumps/PaperdollGump.cs:945` ·
`ModernPaperdoll.ItemSlot` `Gumps/ModernPaperdoll.cs:338` + `ModernPaperdoll.ItemGumpFixed` `Gumps/ModernPaperdoll.cs:439` ·
`GridLootGump.GridLootItem` `Gumps/GridLootGump.cs:443` ·
`CounterBarGump.CounterItem` `Gumps/CounterBarGump.cs:382` + `CounterItem.ImageWithText` `Gumps/CounterBarGump.cs:729` ·
`MenuGump.ItemView` `Gumps/MenuGump.cs:174`.

### Colour

`ColorPickerBox` `Controls/ColorPickerBox.cs:43` · `ClickableColorBox` `Controls/ClickableColorBox.cs:41` ·
`ModernColorPicker` `Gumps/ModernColorPicker.cs:12` + `HueDisplay` `Gumps/ModernColorPicker.cs:103` ·
`RGBColorPickerGump` `Gumps/RGBColorPickerGump.cs:10`.

### Xml gumps

`XmlGumpHandler` `XmlGumpHandler.cs:19` (static loader) · `XmlGumpHandler.XmlProgressBarInfo` `XmlGumpHandler.cs:801` ·
`XmlGump` `XmlGumpHandler.cs:818` · `XmlHealthBar` `XmlGumpHandler.cs:970`.

### Gump bases

`TextContainerGump` (abstract) `Gumps/TextContainerGump.cs:39` · `ResizableGump` (abstract) `Gumps/ResizableGump.cs:42`.

### Skills

`SkillGumpAdvanced` `Gumps/SkillGumpAdvanced.cs:51` · `SkillListEntry` `Gumps/SkillGumpAdvanced.cs:614` ·
`StandardSkillsGump` `Gumps/StandardSkillsGump.cs:51` + `SkillsGroupControl` `Gumps/StandardSkillsGump.cs:424` +
`SkillItemControl` `Gumps/StandardSkillsGump.cs:813`.

### Assistant sub-controls

`OrganizerControl` `Controls/AssistantGumpControls/OrganizerControl.cs:14` ·
`AssistantGump.SpellRangeEditor` `Gumps/AssistantGump.cs:893` ·
`AssistantGump.AutoLootConfigs` `Gumps/AssistantGump.cs:1012` ·
`AssistantGump.SellAgentConfigs` `Gumps/AssistantGump.cs:1200` ·
`AssistantGump.BuyAgentConfigs` `Gumps/AssistantGump.cs:1431` ·
`AssistantGump.GraphicFilterConfigs` `Gumps/AssistantGump.cs:1662` ·
`AssistantGump.PAGE` (enum) `Gumps/AssistantGump.cs:873`.

### Misc nested

`ImprovedBuffGump.BuffBarManager` (static) `Gumps/ImprovedBuffGump.cs:161` ·
`JournalGump.RenderedTextList` `Gumps/JournalGump.cs:367` ·
`MapGump.PinControl` `Gumps/MapGump.cs:504` ·
`MenuGump.ContainerHorizontal` `Gumps/MenuGump.cs:210` ·
`ModernPaperdoll.MenuGump / CharacterPreview / MinimizedPaperdoll / MenuButton` `Gumps/ModernPaperdoll.cs:605 / 821 / 840 / 582` ·
`SpellbookGump.HueGumpPic` `Gumps/SpellbookGump.cs:1461` ·
`TopBarGump.RighClickableButton` `Gumps/TopBarGump.cs:471` ·
`PaperDollGump.Settings : UISettings` `Gumps/PaperdollGump.cs:1053` ·
`MultiItemMoveGump.ProcessType` `Gumps/MultiItemMoveGump.cs:420` ·
`FileSelectorType` `Gumps/FileSelector.cs:356` · `InputRequest.Result` `Gumps/InputRequest.cs:56`.

---

## State

### Static / process-wide

- `Control._StepsDone = 1`, `Control._StepChanger = 1` — `Controls/Control.cs:49-50`. **Shared by every scrollbar in the client**; incremented in `ScrollBarBase.Update` (`Controls/ScrollBarBase.cs:158-170`), reset to 1 by any scrollbar's mouse-up (`Controls/ScrollBarBase.cs:227`).
- `ExternalUrlImage._cache` `Dictionary<string,Texture2D>` — `Controls/ExternalUrlImage.cs:17`. Written from background tasks.
- `RenderedMapArea._textureCache` `ConcurrentDictionary<int,Texture2D>` — `Controls/RenderedMapArea.cs:21`. Never evicted.
- `MiniMapGump._blankGumpsPixels` `uint[4][]` — `Gumps/MiniMapGump.cs:57`. Slots 0/1 = pristine gump pixels (small/large), 2/3 = scratch buffers reused every frame.
- `NetworkStatsGump._last_position` — `Gumps/NetworkStatsGump.cs:47`.
- `MusicInfoGump._lastPosition` — `Gumps/MusicInfoGump.cs:23`.
- `GridLootGump._lastX/_lastY` — `Gumps/GridLootGump.cs:51-52`.
- `SkillGumpAdvanced.Dragging`, `_sortAsc`, `_sortField`, `last_x`, `last_y`, `last_button` — `Gumps/SkillGumpAdvanced.cs:67,69,70,81`.
- `StandardSkillsGump.last_x/last_y` — `Gumps/StandardSkillsGump.cs:69`.
- `ModernPaperdoll.lastX/lastY` — `Gumps/ModernPaperdoll.cs:40`.
- `PaperDollGump._settings` (lazy JSON `UISettings`) — `Gumps/PaperdollGump.cs:87`.
- `NameOverheadGump.currentHeight` — `Gumps/NameOverheadGump.cs:61`; `COLLISION_SPACING = 8` `:62`.
- `CounterBarGump.CurrentCounterBarGump` — `Gumps/CounterBarGump.cs:54`; nulled in `Dispose` `:373`.
- `FileSelector._lastPath` — `Gumps/FileSelector.cs:25`.
- `XmlGump.UpdateFrequency = 250` (ms, global for all xml gumps) — `XmlGumpHandler.cs:823`.
- `WorldViewportGump.DamageWindowOutlineHue` / `damageWindowOutline` — `Gumps/WorldViewportGump.cs:64-65`. `.Z` is mutated per-frame in `Draw` `:330`.
- `ImprovedBuffGump.BuffBarManager.coolDownBars` `CoolDownBar[20]` — `Gumps/ImprovedBuffGump.cs:164`. Shared across every buff gump instance.
- `MultiItemMoveGump` statics — `MoveItems` `ConcurrentQueue<Item>` `:50`, `_selected` `ConcurrentDictionary<uint,byte>` `:51`, `ObjDelay` `:55`, `processing` `:56`, `processType` `:57`, `_lastMoveTick` `:58`, `tradeId/containerId` `:59`, `groundX/Y/Z` `:60`.
- `SpellbookGump._spells` `bool[64]` — `Gumps/SpellbookGump.cs:60`. Instance, but **never cleared** in `CreateBook`.
- `Supporters.SUPPORTERS` string table — `Gumps/Supporters.cs:18`.
- `VersionHistory.updateTexts` — `Gumps/VersionHistory.cs:11`.
- `PaperDollInteractable._layerOrder` / `_layerOrder_quiver_fix` — `Controls/PaperDollInteractable.cs:49,76`.
- `PaperDollGump.PeaceModeBtnGumps` / `WarModeBtnGumps` — `Gumps/PaperdollGump.cs:51-52`.

### Per-instance state worth naming

- `Control._bounds` (returned by `ref`) — `Controls/Control.cs:54`. `X/Y/Width/Height` are `ref int` into it, so callers can write through (`int.TryParse(attr.Value, out c.X)` in `XmlGumpHandler.cs:722`).
- `Control._offset` — `Controls/Control.cs:56`; propagated to all children by `UpdateOffset` `:356`, used by ScrollArea to shift hit-testing.
- `Control.LocalSerial` / `ServerSerial` `:74-76` — reused as an arbitrary tag by many gumps (page number in `CombatBookGump`, `RacialAbilitiesBookGump`, `SpellbookGump`; button id in `HouseCustomizationGump`).
- `Control.Tag` `:139` — untyped; `SkillGumpAdvanced` stores a `bool` group-maximised state in it (`Gumps/SkillGumpAdvanced.cs:374`), `SpellbookGump` stores an `int` spell index (`:549`).
- `ScrollArea.ScissorRectangle` (public field) — `Controls/ScrollArea.cs:102`.
- `HouseCustomizationGump._customHouseManager` — `Gumps/HouseCustomizationGump.cs:48`; also assigned to the global `World.CustomHouseManager` `:64` and nulled in `Dispose` `:2097`.
- `XmlGump.TextBoxUpdates` / `ProgressBarUpdates` / `VerticalProgressBarUpdates` — `XmlGumpHandler.cs:824-826`.
- `JournalGump.RenderedTextList._entries/_hours/_text_types` `Deque`s capped at 200 — `Gumps/JournalGump.cs:369`, trim at `:560`.

---

## Timing

**Per frame (`Control.Update`, driven by `UIManager`):**
- `Control.Update` `Controls/Control.cs:402` walks all children, collects disposed ones into a fresh `List<Control>` each call, then recomputes Width/Height from child bounds when `WantUpdateSize`.
- `ItemGump.Update` `Controls/ItemGump.cs:100` — pickup-drag test against `Mouse.LButtonPressed` + `Mouse.MOUSE_DELAY_DOUBLE_CLICK`; writes `SelectedObject.Object` on hover.
- `PaperDollInteractable.GumpPicEquipment.Update` `Controls/PaperDollInteractable.cs:534` — same lift test.
- `ScrollBarBase.Update` `Controls/ScrollBarBase.cs:129` — drag tracking; held arrow auto-repeat gated by `TIME_BETWEEN_CLICKS = 2` (ticks, i.e. ~2 ms) `:42`, with `_StepChanger` growing every 8 steps `:167`.
- `HSliderBar.Update` `Controls/HSliderBar.cs:154` — while `_clicked`, recompute from mouse x.
- `ModernScrollBar.Update` `Controls/ModernScrollBar.cs:207` — recompute thumb rect + hover.
- `ColorPickerBox.Update` `Controls/ColorPickerBox.cs:136` — live-preview hue sampling under the cursor.
- `ResizableGump.Update` `Gumps/ResizableGump.cs:130` and `WorldViewportGump.Update` `Gumps/WorldViewportGump.cs:134` — resize-drag tracking from `Mouse.LDragOffset`.
- `QuestArrowGump.Update` `Gumps/QuestArrowGump.cs:68` — full world→screen recompute + clamp every frame; hue flip every **1000 ms** `:200`.
- `NameOverheadGump.Draw` `Gumps/NameOverheadGump.cs:676` — recomputes X/Y from `Client.Game.Animations.GetAnimationDimensions` **inside Draw** and assigns `X`/`Y` `:843-844`.
- `SkillGumpAdvanced.Update` `Gumps/SkillGumpAdvanced.cs:545` — rebuilds the whole gump when `_updateSkillsNeeded`; resizes while `Dragging`.
- `MultiItemMoveGump.Update` `Gumps/MultiItemMoveGump.cs:309` — dequeues one item per `ObjDelay` ms (default from `profile.MoveMultiObjectDelay`, gump default 1000).
- `CounterBarGump.CounterItem.Update` `Gumps/CounterBarGump.cs:607` — recursive backpack scan every **100 ms** (`_time = Time.Ticks + 100` `:613`).
- `ContainerGump.Update` `Gumps/ContainerGump.cs:520` — corpse eye toggles every **750 ms** `:550`.
- `MiniMapGump.Update` `Gumps/MiniMapGump.cs:107` — dot blink toggles every **500 ms** `:123`; `CreateMiniMapTexture` runs from `Draw` `:165` whenever the player moved.
- `NetworkStatsGump.Update` `Gumps/NetworkStatsGump.cs:101` — **100 ms**.
- `MusicInfoGump.Update` `Gumps/MusicInfoGump.cs:97` — **250 ms**.
- `XmlGump.Update` `XmlGumpHandler.cs:838` — text + progress refresh every `UpdateFrequency` = **250 ms**; deferred file save 5000 ms after a move/lock `:917,935`.
- `UpdateTimerViewer.Update` `Gumps/UpdateTimerViewer.cs:64` — **2000 ms** (`UPDATE_INTERVAL`).
- `CombatBookGump.Update` / `RacialAbilitiesBookGump.Update` / `SpellbookGump.Update` — deferred page turn once `Time.Ticks - Mouse.LastLeftButtonClickTime >= Mouse.MOUSE_DELAY_DOUBLE_CLICK` (`Gumps/CombatBookGump.cs:428`, `Gumps/RacialAbilitiesBookGump.cs:375`, `Gumps/SpellbookGump.cs:1339`).
- `TextContainerGump.Update` `Gumps/TextContainerGump.cs:60` → `TextRenderer.Update`; messages expire at `Time.Ticks + 4000` `:55`.
- `SimpleTimedTextGump.Draw` `Gumps/SimpleTimedTextGump.cs:34` — disposes itself when its `DateTime` deadline passes.
- `Supporters.Draw` `Gumps/Supporters.cs:75` — credit scroll advances by 0.9 per **draw call** (frame-rate dependent).
- `ModernColorPicker.HueDisplay.Draw` `Gumps/ModernColorPicker.cs:177` — flash alpha ±0.1 per frame.
- `MenuGump.Update` `Gumps/MenuGump.cs:128` — scrolls 1 px per frame while an arrow hitbox is held.

**Twice per second (`Control.SlowUpdate`, per its own doc comment `Controls/Control.cs:484-486`):**
- `Line.SlowUpdate` `Controls/Line.cs:54` — re-derives hue vector from Alpha.
- `ScrollArea.SlowUpdate` `Controls/ScrollArea.cs:104` — `CalculateScrollBarMaxValue`, scrollbar visibility, and re-`UpdateOffset` on every content child.

**On demand / event driven:**
- `UpdateContents()` overrides fire from `RequestUpdateContents()`: `ContainerGump:558`, `GridLootGump:222`, `ModernPaperdoll:216`, `PaperDollGump:685`, `SpellbookGump:830`, `MiniMapGump:225`, `ImprovedBuffGump:68`.
- `Save`/`Restore(XmlElement)` run at profile save/load: `NetworkStatsGump:180/187`, `ImprovedBuffGump:134/143`, `MiniMapGump:71/77`, `JournalGump:324/331`, `CounterBarGump:296/321`, `SkillGumpAdvanced:513/522`, `StandardSkillsGump:404/411`, `PaperDollGump:656/665`, `SpellbookGump:113/119`, `MacroButtonGump:276/291`, `UseSpellButtonGump:280/286`, `ContainerGump:566/579`, `ModernPaperdoll:285`.
- `JournalGump` subscribes `EventSink.JournalEntryAdded` in ctor `:230`, unsubscribes in `Dispose` `:280`.
- `MapGump._hit.MouseUp` unsubscribed in `AfterDispose` `:488`.
- `StandardSkillsGump` subscribes `_scrollArea.SizeChanged` `:88`, unsubscribes in `Dispose` `:364`.

**Background threads:**
- `ExternalUrlImage.LoadImage` on `Task.Run` `Controls/ExternalUrlImage.cs:31`.
- `GumpPicExternalUrl.getImageTexture` on `Task.Factory.StartNew`, blocking `.Result` inside `Controls/GumpPicExternalUrl.cs:68-73`.
- `RenderedMapArea.LoadMapTexture` — `Texture2D` allocated on the caller's thread, pixel fill + `SetDataPointerEXT` inside `Task.Run` `Controls/RenderedMapArea.cs:63-210`.
- `CustomToolTip.LoadOPLData` retry chain: `Task.Delay(1500).Wait()` then recurse, up to 4 attempts `Gumps/CustomToolTip.cs:100-104`.
- `XmlGump.SaveFile` on `Task.Run` `XmlGumpHandler.cs:907`.

---

## Inbound

- **`UIManager`** is the single driver: walks `UIManager.Gumps` (a `LinkedList<Gump>`) calling `Update`, `SlowUpdate`, `Draw`, and routes mouse/keyboard through `Control.HitTest` → `Invoke*`. It also owns `MouseOverControl`, `KeyboardFocusControl`, `DraggingControl`, `LastControlMouseDown`, `ContainerScale`, `SystemChat`, `MakeTopMostGump`, `SavePosition`, `AttemptDragControl`, and the `UpdateTimer*` profiling dictionaries this partition's `UpdateTimerViewer` reads.
- **`GameScene`** creates `WorldViewportGump` and reads `_systemChatControl`; supplies `Macros` and `Hotkeys` to `MacroButtonGump`, `UseSpellButtonGump`, `TopBarGump`, `HotkeyControl`, `SpellbookGump.HueGumpPic`; `DoubleClickDelayed` is called from `ContainerGump.Restore` and `SpellbookGump.Restore`.
- **Packet handlers** (`Network/`) construct and drive: `ContainerGump`, `GridLootGump`, `MapGump` (`SetMapTexture`, `MapInfos`, `AddPin`, `SetPlotState`, `ClearContainer`), `MenuGump`/`GrayMenuGump` (`AddItem`, `SetHeight`), `BulletinBoardGump` (`AddBulletinObject`, `RemoveBulletinObject`), `TipNoticeGump`, `ProfileGump`, `PartyInviteGump`, `QuestArrowGump.SetRelativePosition`, `HouseCustomizationGump`, `SpellbookGump`, `PaperDollGump.UpdateTitle`, `ModernPaperdoll.UpdateTitle`/`HandleObjectMessage`, `ImprovedBuffGump.AddBuff`/`RemoveBuff`, `StandardSkillsGump.Update(int skillIndex)`, `SkillGumpAdvanced.ForceUpdate`.
- **Server gump parser** builds from `List<string> parts`: `ResizePic(List<string>)` `Controls/ResizePic.cs:62`, `CheckerTrans(List<string>)` `:43`, `HtmlControl(List<string>,string[])` `:51`, `StbTextBox(List<string>,string[])` `:117`, `ButtonTileArt(List<string>)` `:49`. These set `IsFromServer = true`.
- **`TargetManager`** callbacks land in `MultiItemMoveGump.OnContainerTarget` / `OnTradeWindowTarget` (`Gumps/MultiItemMoveGump.cs:256-279`).
- **Managers** read/write partition types: `NameOverHeadManager` (`NameOverheadGump`, `NameOverheadAssignControl`), `AutoLootManager`/`BuySellAgent`/`GraphicsReplacement`/`DressAgentManager`/`OrganizerAgent`/`FriendsListManager`/`JournalFilterManager`/`SpellBarManager`/`SpellVisualRangeManager`/`TitleBarStatsManager`/`HideHudManager` (all via `AssistantGump`), `ContainerManager` (`ContainerGump`), `HouseCustomizationManager` (`HouseCustomizationGump`), `SkillsGroupManager` (both skills gumps), `JournalManager`/`IgnoreManager` (`JournalGump`), `InfoBarManager` (`InfoBarBuilderControl`), `AudioManager` (`MusicInfoGump`).
- **Python / Legion Script API** calls `Control.SetRect/SetWidth/SetHeight/SetX/SetY/SetPos/GetX/GetY` (`Controls/Control.cs:282-354`, all commented "Used in python API"), constructs `SimpleProgressBar`, and sets `QuestArrowGump.CanCloseWithRightClick = true` to suppress the server round-trip (`Gumps/QuestArrowGump.cs:212`).
- **`TopBarGump`** is the user-facing entry point to `AssistantGump`, `ScriptManagerGump`, `CommandsGump`, `DebugGump`, `NetworkStatsGump`, `DiscordGump`, `BoatControl`, `NearbyLootGump`, `SpellQuickSearch`, and every Xml gump.

## Outbound

- **Rendering** — `UltimaBatcher2D` (`Draw`, `DrawTiled`, `DrawRectangle`, `DrawLine`, `DrawString`, `ClipBegin/ClipEnd`), `ShaderHueTranslator.GetHueVector`, `SolidColorTextureCache.GetTexture`, `RenderedText.Create/Draw/Destroy`, `TextBox.GetOne` + `TextBox.RTLOptions`, `Fonts.Bold`, `ScissorControl`.
- **Assets** — `Client.Game.Arts.GetArt/PixelCheck/GetRealArtBounds`, `Client.Game.Gumps.GetGump/PixelCheck`, `Client.Game.Animations.GetAnimationDimensions/ConvertBodyIfNeeded`, `TileDataLoader.Instance.StaticData`, `ClilocLoader`, `FontsLoader`, `HuesLoader`, `MapLoader`, `SkillsLoader`, `SoundsLoader`, `AnimationsLoader.EquipConversions`, `UOFileManager.TileArtLoader`, `GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT`, `ExternalImageLoader`, `TrueTypeLoader.EMBEDDED_FONT`.
- **World state (read)** — `World.Items.Get`, `World.Mobiles.Get`, `World.Get`, `World.Player.*`, `World.OPL.TryGetNameAndData`, `World.Party`, `World.Map`/`World.MapIndex`, `World.ClientFeatures`, `World.HouseManager.TryGetHouse`, `World.DurabilityManager`, `World.ActiveSpellIcons`.
- **World state (write)** — `World.CustomHouseManager` (`Gumps/HouseCustomizationGump.cs:64,2097`), `World.Party.Leader/Inviter` (`Gumps/PartyInviteGump.cs:96-97,108`), `SelectedObject.Object` / `SelectedObject.CorpseObject` / `SelectedObject.SelectedContainer`, `Skill.Lock` (`Gumps/SkillGumpAdvanced.cs:685`, `Gumps/StandardSkillsGump.cs:914`), `Item.X/Item.Y` (`ContainerGump.CheckItemControlPosition` `:709-722`).
- **Actions / network** — `GameActions.*` (PickUp, DropItem, DoubleClick, GrabItem, CastSpell, Equip, Attack, Print, OpenPaperdoll/Backpack/Journal/Chat/WorldMap/Settings/Skills/GuildGump/AbilitiesBook/MacroGump, RequestHelp/Profile/QuestMenu, ReplyGump, ToggleWarMode, ChangeSkillLockStatus, UseSkill, QuestArrow), `NetClient.Socket.Send_*` (OpenChat, MenuResponse, GrayMenuResponse, MapMessage, TipRequest, ProfileUpdate, PartyDecline, BulletinBoard*, CustomHouse*, GameWindowSize, OpenUOStore, ToggleGargoyleFlying, ASCIISpeechRequest), `AsyncNetClient.Socket.Send_SkillStatusChangeRequest` (`Gumps/StandardSkillsGump.cs:912` — note the different socket class from every other send in this partition), `MoveItemQueue.Instance.Enqueue`, `Pathfinder.WalkTo`, `BoatMovingManager.MoveRequest`, `TargetManager.SetTargeting/Target/CancelTarget`, `TargetHelper.TargetObject`, `DelayedObjectClickManager.Set`, `Client.Game.Audio.PlaySound`, `Client.Game.SetWindowTitle`, `PlatformHelper.LaunchBrowser`.
- **Config** — `ProfileManager.CurrentProfile.*` read and written directly from Draw/Update paths, `Settings.GlobalSettings.Language`, `UISettings.Load/Save` (`Gumps/PaperdollGump.cs:94-98`), `Language.Instance`.
- **Other UI partitions** — `Gump`, `AnchorableGump`, `Button`, `NiceButton`, `Checkbox`, `RadioButton`, `Label`, `HoveredLabel`, `GumpPic`, `GumpPicTiled`, `GumpPicInPic`, `StaticPic`, `ResizableStaticPic`, `ColorBox`, `HitBox`, `ScrollBar`, `ScrollFlag`, `ExpandableScroll`, `SimpleBorder`, `NineSliceControl`, `NineSliceGump`, `ModernScrollArea`, `HBoxContainer`, `ContextMenuControl`, `ContextMenuItemEntry`, `HotkeyBox`, `ColorSelectorControl`, `HttpClickableLink`, `EmbeddedGumpPic`, `AnimationDisplay`, `BaseOptionsGump` (+ `ModernButton`, `CheckboxWithLabel`, `SliderWithLabel`, `InputField`, `InputFieldWithLabel`, `LeftSideMenuRightSideContent`, `ThemeSettings`), `MessageBoxGump`, `InspectorGump`, `SplitMenuGump`, `WorldMapGump`, `GridContainer`, `BaseHealthBarGump`/`HealthBarGump`/`HealthBarGumpCustom`, `StatusGumpBase`, `PartyGump`, `DurabilitysGump`, `DurabilityGumpMinimized`, `SkillButtonGump`, `UseAbilityButtonGump`, `RacialAbilityButton`, `ColorPickerGump`, `SpellBar`, `ScriptManagerGump`, `NearbyLootGump`, `CommandsGump`, `DebugGump`, `DiscordGump`, `SpellQuickSearch`, `CoolDownBar`.

---

## Hazards

Factual observations with line numbers. No causal claims.

**Collection mutation / iteration**

- `Controls/Control.cs:1101-1108` — `Dispose` iterates `Children` calling `c.Dispose()`; the child's own `Dispose` recurses, and `Control.Parent`'s setter (`:182-187`) removes from `_parent.Children`. Nothing in the child path removes it from the list being iterated here, but `Remove`/`Insert` called from a `Disposed` handler would.
- `Controls/Control.cs:377-392` — `Draw` re-checks `Children.Count <= i` each iteration and uses `Children.ElementAt(i)` (LINQ, O(n) on `List<T>` via indexer fast-path but still an interface call) instead of the indexer.
- `Controls/Control.cs:415-464` — `Update` allocates a new `List<Control> removalList` on **every** call for every control with children.
- `Controls/Control.cs:456-463` — the removal loop calls `OnChildRemoved()` before `Children.Remove(c)`; `TableContainer.OnChildRemoved` (`Controls/TableContainer.cs:48`) and `VBoxContainer.OnChildRemoved` (`Controls/VBoxContainer.cs:50`) call `Reposition()`, which iterates `Children` while the outer loop is mid-removal.
- `Controls/Control.cs:792-798` — `Clear()` iterates `Children` calling `Dispose()` on each without clearing the list; `ScrollArea.Clear` (`Controls/ScrollArea.cs:199`) does the same from index 1.
- `Gumps/BulletinBoardGump.cs:137-147` — `RemoveBulletinObject` disposes a child inside a `foreach` over `_databox.Children`, then `return`s.
- `Gumps/BulletinBoardGump.cs:153-161` — `AddBulletinObject` disposes inside `foreach` over `_databox.Children`, then `break`s.
- `Gumps/SkillGumpAdvanced.cs:551-554` — `Update` iterates `Children.OfType<Label>()` disposing each, then `BuildGump()` re-adds. `real`/`value` labels created at `:507-508` are among them.
- `Gumps/ModernColorPicker.cs:90-91` — `FillHueDisplays` iterates `area.Children` calling `c.Dispose()`, then adds 200 new children into the same list.
- `Gumps/ModernPaperdoll.cs:427` — `ClearItems` calls `itemArea.Children.Clear()` directly, without disposing the removed `ItemGumpFixed`s and without nulling their `Parent`.

**Unbounded / never-released**

- `Controls/ExternalUrlImage.cs:17,36-57` — static `Dictionary<string,Texture2D>` read and written from `Task.Run` bodies with no lock; `ContainsKey` then `Add` is not atomic (`:56-57`). Textures never disposed.
- `Controls/RenderedMapArea.cs:21,203` — static `ConcurrentDictionary<int,Texture2D>` of full-map textures, never evicted; a facet texture is map-width × map-height × 4 bytes.
- `Controls/GumpPicExternalUrl.cs:90` — `imageTexture` assigned from a background task, never disposed.
- `Gumps/MapGump.cs:171` — `SetMapTexture` disposes the previous `_mapTexture`, but `_mapTexture` is not disposed in `AfterDispose` (`:488`).
- `Gumps/Supporters.cs:29` — `image` texture obtained per-instance from `ExternalImageLoader`, never released.

**Threading / frame thread**

- `Controls/GumpPicExternalUrl.cs:73` — `httpClient.GetStreamAsync(ImgUrl).Result` inside a `Task.Factory.StartNew` body; blocking wait on a thread-pool thread.
- `Gumps/CustomToolTip.cs:100-104` — `Task.Factory.StartNew(() => { Task.Delay(1500).Wait(); LoadOPLData(attempt++); })`; `attempt++` is post-increment so the value passed is the **unincremented** one, and the guard is `attempt > 4` (`:59`).
- `Controls/RenderedMapArea.cs:63` — `new Texture2D(Client.Game.GraphicsDevice, …)` is created on the calling (frame) thread, then filled and `SetDataPointerEXT`'d from inside `Task.Run` (`:78`, `:201`).
- `Controls/ExternalUrlImage.cs:54` / `Controls/GumpPicExternalUrl.cs:84` — `Texture2D.FromStream(Client.Game.GraphicsDevice, …)` called from a background task.
- `XmlGumpHandler.cs:907,938-967` — `SaveFile` runs on `Task.Run` and reads `X`, `Y`, `IsLocked` (frame-thread state) while writing the file; `savingFile` is a plain `bool`, not interlocked.

**Identity / serial reuse**

- `Controls/ItemGump.cs:60,129,164` — holds `LocalSerial` and re-resolves `World.Items.Get(LocalSerial)` every Draw/Update; the control is not disposed when the item is destroyed (only `ItemGumpFixed` overrides check for null, `Gumps/PaperdollGump.cs:1000-1005`, `Gumps/ModernPaperdoll.cs:499-502`).
- `Gumps/ModernPaperdoll.cs:340,209` — `ItemSlot.Item` caches an `Item` reference; `HandleObjectMessage` compares `layerSlot.Item.Serial` against the incoming parent without a destroyed check.
- `Gumps/ModernPaperdoll.cs:441,499` — `ItemGumpFixed.item` is a cached `Item` reference used unguarded at `:511` and `:517` after the null check only calls `Dispose()` (no `return`), so `item.Hue` is dereferenced when `item == null` and `IsDisposed` was already true is false.
- `Gumps/PaperdollGump.cs:1000-1013` — same pattern: `if (item == null) { Dispose(); }` with no `return`; the following `IsDisposed` check catches it only because `Dispose()` sets the flag.
- `Gumps/PaperdollGump.cs:919` — `EquipmentSlot.LocalSerial` is reassigned in `Update` whenever the layer's item changes, while the slot's `_itemGump` was constructed from the previous serial.
- `Gumps/MultiItemMoveGump.cs:50-51,327-374` — `MoveItems` queue holds `Item` object references while `_selected` holds serials; an item dequeued at `:327` is checked against `_selected` by serial `:329` but its `Amount`/`Graphic` are read from the possibly-stale object (`:335-357`).
- `Gumps/GridLootGump.cs:447-451` — `GridLootItem(uint serial, int size)` is invoked at `:254` with an `Item` (implicit `Entity`→`uint`); the closure at `:496-502` captures the `Item` object, not the serial.
- `Gumps/MacroButtonGump.cs:281-283` — `Save` assigns `LocalSerial = macroIndex + 1000`, so the gump's identity changes at save time and depends on the macro's position in `GetAllMacros()`.
- `Gumps/ImprovedBuffGump.cs:164` — `BuffBarManager.coolDownBars` is `static`; two `ImprovedBuffGump` instances share the same 20 slots, and `AddCoolDownBar` takes the caller's `_boxContainer` (`:165`).

**Ordering assumptions**

- `Controls/ScrollArea.cs:158` — `Draw` casts `Children[0]` to `ScrollBarBase` unconditionally; every content loop starts at index 1 (`:163`, `:201`, `:214`, `:261`). `Insert(0, …)` on a ScrollArea would break the cast.
- `Gumps/PaperdollGump.cs:124` — `IsMinimized` setter does `Insert(0, _picBase)` on a `PaperDollGump`; `PaperDollGump` is not a ScrollArea, but the same `Insert` sets `c.Page = 0` unconditionally (`Controls/Control.cs:769`), discarding the page the caller passed to `Add`.
- `Controls/Control.cs:767-778` — `Insert(int index, Control c, int page = 0)` accepts a `page` argument and then ignores it, hard-setting `c.Page = 0`.
- `Gumps/StandardSkillsGump.cs:721-742` — `while (_box.Children.Count != 0)` where the body only removes a child if the inner `for` finds a matching skill index in `first._group`; if no match is found the child is never removed.
- `Gumps/SpellbookGump.cs:60,231-241` — `_spells[64]` is set to `true` per contained spell in `CreateBook` but never reset to `false`; `CreateBook` is re-entered from `UpdateContents` (`:843`).
- `Gumps/HouseCustomizationGump.cs:231` — `public new void Update()` **hides** `Control.Update()` rather than overriding it; it is invoked explicitly from the constructor (`:228`) and from `OnButtonClick`, and the base per-frame `Update` still runs through the vtable.
- `Gumps/HouseCustomizationGump.cs:253-266` — inside `Update`, the eyedropper button is added with `Add(button)` (to the gump) while every other button in the same method goes to `_dataBoxGUI`, which is cleared at `:234` on each call.
- `Gumps/ContainerGump.cs:562` — `UpdateContents` does `IsMinimized = IsMinimized;` after `BuildGump()`; the setter's `if (_isMinimized != value)` guard is commented out at `:161`, so the body always runs and re-multiplies `Width`/`Height` by `GetScale()` (`:167-168`).
- `Gumps/ContainerGump.cs:236-237` / `:553-554` — `Width = _gumpPicContainer.Width = (int)(_gumpPicContainer.Width * scale)` and the corpse-eye `_eyeGumpPic.Width = (int)(_eyeGumpPic.Width * scale)` multiply the already-scaled value each time they run; the eye path runs every 750 ms.
- `Gumps/MacroButtonGump.cs:114-128` — `Scale` setter does `Width = (int)(Width * factor)`, compounding on repeat assignment; `TheMacro` setter (`:88-95`) sets `Scale` then `Graphic`, and `Graphic`'s setter recomputes Width from the texture.
- `Gumps/SkillGumpAdvanced.cs:97` — `Build()` is called from the constructor and adds `background`, `area`, `_databox`, buttons, `BottomArea`, `_sortOrderIndicator`, `resizeDrag` without a `Clear()`; `ForceUpdate` only sets `_updateSkillsNeeded`, which reaches `BuildGump()` (a different method) not `Build()`.
- `Gumps/AssistantGump.cs:317-321,328-332` — toggling the `HideHudFlags.All` checkbox constructs a **new** `AssistantGump` and disposes the current one from inside the checkbox's own value-changed callback.
- `Gumps/DressAgentConfigGump.cs:261` / `:286` / `:306` — `BuildGump()` calls `Clear()` then rebuilds, invoked from a `Combobox.OnOptionSelected` handler and from button `MouseUp` handlers on controls that `Clear()` disposes.
- `Controls/AssistantGumpControls/OrganizerControl.cs:182,206,257,319` — `SelectOrganizerConfig` clears `leftSideContent.RightArea` from inside `MouseUp` handlers of controls that live in that right area.
- `Gumps/CombatBookGump.cs:466-469` — `SetActivePage` mutates `_primAbility.Page` / `_secAbility.Page` to the new page every time, while `BuildGump` added the same two instances to multiple pages (`:176`, `:199`).
- `Gumps/CombatBookGump.cs:388-389,405-406` — `Update` indexes `AbilityData.Abilities[(index & 0x7F) - 1]` with no bounds check; `index` comes from `World.Player.Abilities[0]`/`[1]`.
- `Gumps/RacialAbilitiesBookGump.cs:206` / `Gumps/CombatBookGump.cs:325` / `Gumps/SpellbookGump.cs:861` — drag handlers test `UIManager.DraggingControl != this` where `this` is the **gump**, but the handler is attached to a child `GumpPic`.
- `Gumps/SkillGumpAdvanced.cs:718` — operator precedence: `Mouse.LButtonPressed && Math.Abs(...X) >= N || Math.Abs(...Y) >= N` — the `||` branch does not require `LButtonPressed`.
- `Gumps/PaperdollGump.cs:579-654` — `OnMouseUp` calls `base.OnMouseUp(x, y, button)` at `:581` and again at `:652` in the `else` branch.
- `Gumps/MultiItemMoveGump.cs:385-396` — `Draw` calls `ClearAll()` and `Dispose()` when the selection is empty, i.e. disposal happens inside the draw pass.
- `Gumps/NameOverheadGump.cs:843-844` — `Draw` writes `X` and `Y`; the same values were used for the hit-test earlier in the frame.
- `Gumps/NameOverheadGump.cs:81-90` — `public new UILayer LayerOrder` hides `Control.LayerOrder`; the setter is a no-op `{ }`, so `UIManager` reading through the base type sees the base field.
- `Gumps/NameOverheadGump.cs:594` — `AdjustPositionToAvoidOverlap` builds a fresh `List<NameOverheadGump>` by walking all of `UIManager.Gumps`, per nameplate, per frame (`GetAllVisibleNameOverheads` `:565`), then loops up to 10 iterations `:597`.

**Client-authoritative state vs server**

- `Gumps/PartyInviteGump.cs:96-97` — sets `World.Party.Leader = World.Party.Inviter` and clears `Inviter` locally at the moment Accept is clicked, before the server confirms.
- `Gumps/StandardSkillsGump.cs:912-915` — sends the skill lock change then immediately writes `skill.Lock = (Lock)newStatus` and updates the button graphic.
- `Gumps/SkillGumpAdvanced.cs:685-701` — same pattern: `_skill.Lock` is assigned before/independently of `GameActions.ChangeSkillLockStatus`.
- `Gumps/ContainerGump.cs:707-722` — `CheckItemControlPosition` clamps and writes `item.X` / `item.Y` on the `Item` entity itself, not on the control.
- `Gumps/HouseCustomizationGump.cs:1927-1936` — `FloorVisionState[selectedFloor]` is advanced client-side and `GenerateFloorPlace()` re-renders before any server exchange.
- `Gumps/MapGump.cs:217-222` — estimated world coordinates are computed from a hard-coded `(float)Width / 300f` multiplier and cached in `mapX`/`mapY` with `foundMapLoc = true`; only the first pin is used.

**Bounds / arithmetic**

- `Controls/ColorPickerBox.cs:120` — `SelectedIndex` setter dereferences `_hues.Length` before `CreateTexture()` has necessarily run; `_hues` is allocated only in `CreateTexture` (`:239`), which the constructor reaches via `Graduation = 1` (`:79`).
- `Controls/ColorPickerBox.cs:219-220` — `SetSelectedIndex` divides by `Width / _columns` and `Height / _rows`; both are integer divisions that yield 0 when the box is smaller than its grid.
- `Controls/ColorPickerBox.cs:153` — live preview tests `Bounds.Contains(Mouse.Position.X, Mouse.Position.Y)` (screen coords) against a parent-relative rectangle.
- `Controls/ResizePic.cs:163` — the `bounds6` branch is guarded by `DH >= 1` where `DW` was just recomputed on the line above (`:160`); `DH` still holds the value from `:135`.
- `Controls/ScrollArea.cs:161` — clip width is hard-coded as `Width - 14 + ScissorRectangle.Width`, independent of which scrollbar type was created (`ScrollFlag` adds 15 to `Width` at `:79`).
- `Controls/ModernScrollBar.cs:192` — `visibleRatio = (float)Height / (Height + totalRange)` uses the control's full `Height`, not the track height.
- `Gumps/ChatGumpChooseName.cs:135` — `Width = Width - -x - 17` (double negative) where the constructor argument above used `Width - x - 17`.
- `Gumps/GridLootGump.cs:281-289` — `_background.Width` is computed from `row` (total item count, never reset per line) and then unconditionally overwritten with `MAX_WIDTH`.
- `Gumps/MenuGump.cs:297` — `new ResizePic(0x13EC) { Width = 400, Height = 111111 }`.
- `Gumps/MiniMapGump.cs:275-277` — `_blankGumpsPixels[index].CopyTo(_blankGumpsPixels[index + 2], 0)` every time the player's tile changes; buffers are allocated once for the first gump size seen (`:92-100`) and reused for all instances.
- `Gumps/MiniMapGump.cs:97` — `gumpInfo.Texture.GetData(...)` — a GPU read-back, on the frame thread, on map change.
- `Gumps/Supporters.cs:86,107-108` — `offset += 0.9` per draw then `if (offset >= 1) offset = 0`, so the cast to `int` at `:95` is 0 on most frames.
- `XmlGumpHandler.cs:753` — `GetPercentage(value, max)` divides without a zero check; `max` comes from parsed xml.
- `XmlGumpHandler.cs:1158` — `healthPercent()` divides `mobile.Hits / mobile.HitsMax` without a zero check.
- `Gumps/NameOverheadGump.cs:869,894,907` — HP/mana/stam percentages divided by `HitsMax`/`ManaMax`/`StaminaMax` without zero checks.
- `Gumps/HouseCustomizationGump.cs:274-276,326-328,382-384,431-433,485-487` — `floorVisionGraphicN[associateGraphicTable[FloorVisionState[i]]]`, a double array index with no bounds check on `FloorVisionState[i]` (`associateGraphicTable` has 7 entries).
- `Gumps/HouseCustomizationGump.cs:1790-1793` — stair index: `if (index > 10) { combinedStairs = true; index -= 10; }` while the controls were tagged with `i + combinedStair` where `combinedStair` is 0 or **10** (`:1180`), so index exactly 10 is not treated as combined.

**Event-handler lifetime**

- `Gumps/MapGump.cs:493` — `_hit.MouseUp -= TextureControlOnMouseUp` in `AfterDispose`; most other gumps in this partition subscribe without unsubscribing (`Gumps/ContainerGump.cs:217,221`, `Gumps/SpellbookGump.cs:152,164,169-175`, `Gumps/PaperdollGump.cs:123,183,309,313,317`).
- `Gumps/PaperdollGump.cs:145-153` — unsubscribes `_virtueMenuPic` / `_partyManifestPic` only when `LocalSerial == World.Player`.
- `Gumps/CustomToolTip.cs:21,95` — `OnOPLLoaded` event never unsubscribed; `RemoveHoverReference()` (`:39`) exists for the other direction.
- `Gumps/ModernPaperdoll.cs:345,376-383` — `timedTexts` list holds `SimpleTimedTextGump`s that dispose themselves in their own `Draw`; the list is pruned only when `AddText` is next called.

---

## Fork deltas

Files/types with no stock-ClassicUO counterpart (no BSD licence header is a reliable tell — stock files all carry the 2021 andreakarasho block):

**TazUO additions**
- `Controls/Area.cs`, `Controls/MenuButton.cs`, `Controls/SettingsSection.cs`, `Controls/TableContainer.cs`, `Controls/VBoxContainer.cs`, `Controls/SimpleProgressBar.cs`, `Controls/ModernScrollBar.cs`, `Controls/NineSliceButton.cs`, `Controls/RenderedMapArea.cs`, `Controls/ExternalUrlImage.cs`, `Controls/GumpPicExternalUrl.cs`.
- `Positioner.cs` + `TableColumnAlignment` — the whole declarative layout system TazUO gumps use instead of hand-set X/Y.
- `XmlGumpHandler.cs` + `XmlGump` + `XmlHealthBar` — user-authored XML HUDs from `Data/XmlGumps`, with `{charname}`/`{hp}`/… token substitution (`XmlGumpHandler.cs:756-799`).
- `Gumps/AssistantGump.cs` and `Controls/AssistantGumpControls/OrganizerControl.cs` — the assistant feature hub (autoloot, buy/sell, dress, bandage, friends, organizer, journal filter, title bar, HUD flags, spell indicators, spellbar).
- `Gumps/ModernPaperdoll.cs`, `Gumps/ModernColorPicker.cs`, `Gumps/RGBColorPickerGump.cs`, `Gumps/DressAgentConfigGump.cs`, `Gumps/MultiItemMoveGump.cs`, `Gumps/FileSelector.cs`, `Gumps/SelectableItemListGump.cs`, `Gumps/InputRequest.cs`, `Gumps/SimpleTimedTextGump.cs`, `Gumps/CustomToolTip.cs`, `Gumps/AnimBrowser.cs`, `Gumps/BoatControl.cs`, `Gumps/ImprovedBuffGump.cs`, `Gumps/UpdateTimerViewer.cs`, `Gumps/VersionHistory.cs`, `Gumps/Supporters.cs`.
- `GumpType` values `MacroButtonEditor = 6464`, `DurabilityGump = 6465`, `GridContainer = 8787`, `NearbyCorpseLoot`, `SpellBar`, `MenuGump`, `TextEntryDialogGump`, `ScriptManager`, `NameOverHeadHandler`, `MusicInfo` (`Gumps/GumpType.cs:56-70`).
- `Control` additions: `Scale` / `InternalScale` / `ScaleWidthAndHeight` / `ScaleXAndY` / `SetInternalScale` (`Controls/Control.cs:83-88, 539-573`), `SlowUpdate` (`:487`), `ForceSizeUpdate` (`:575`), `SetDisposed` (`:262`), `AlphaChanged` (`:733`), `AfterDispose` (`:1119`), `FindControls<T>` (`:805`), `IsModal`/`ModalClickOutsideAreaClosesThisControl` (`:215-216`), the controller-button events (`:680`), and the python-API `SetRect/SetWidth/SetHeight/SetX/SetY/SetPos/GetX/GetY` block (`:282-354`).
- `PaperDollGump.Settings : UISettings` (`Gumps/PaperdollGump.cs:1053`) — every graphic id and coordinate of the classic paperdoll moved into a user-editable JSON file; stock hard-codes them.
- `TopBarGump` "More +" context menu, Assistant / Legion Script / Xml Gumps buttons, `RefreshXmlGumps` (`Gumps/TopBarGump.cs:161-360`).
- `NameOverheadGump` health/mana/stam bars, `NameOverHeadManager.Search` filtering, `NamePlateAvoidOverlap`, `NamePlateHideAtFullHealth`, TTF `TextBox` instead of `RenderedText` (`Gumps/NameOverheadGump.cs:565-627, 686-716, 860-919`).
- `ContainerGump` backpack-style switching (`:81-120`) and the grid-container return button (`:239-255`); `PaperDollInteractable` backpack skins (`Controls/PaperDollInteractable.cs:350-385`) and `tileart.uop` equip conversion (`:454-465`).
- `SkillGumpAdvanced` group support, drag-resize, sort indicator, `AdvancedSkillsGumpHeight` persistence.
- `CounterBarGump` spell assignment (`SpellID`, `GenSpellList`, `CounterItem.Use` casting) and highlight-on-use (`Gumps/CounterBarGump.cs:410,417,447-451,468-551,658-668,711-720`).
- `WorldViewportGump` low-HP screen outline (`Gumps/WorldViewportGump.cs:322-355`) and the first-run `VersionHistory` + `DownloadAPIPy` hook (`:125-131`).
- `ResizableGump.SetLockStatus` / alt-click lock (`Gumps/ResizableGump.cs:190-254`).
- `JournalGump` `IgnoreManager` filtering and the `EventSink.JournalEntryAdded` subscription model (`Gumps/JournalGump.cs:230,300`).
- `QuestArrowGump.OnMouseUp` early-return when `CanCloseWithRightClick` is set by the Python API (`Gumps/QuestArrowGump.cs:212`).
- `StandardSkillsGump` uses `AsyncNetClient.Socket` (`:912`) — TazUO's async networking layer — while everything else uses `NetClient.Socket`.

**Holiday-Edition specific**
- `Gumps/MusicInfoGump.cs` — whole file. Doc comment (`:13-20`) describes it as a music-system diagnostic panel; it deliberately sets `ShouldBeSaved => false` (`:58`) with a comment explaining the option is global and re-applied by `GameScene` on login, and `Toggle(bool)` (`:64`) is the entry point from the settings checkbox. Reads `Client.Game.Audio.GetMusicStatus()` and `SoundsLoader.Instance.TryGetMusicData`, and reports an "era" string — an era-switching music feature not present upstream.
- `Gumps/NameOverheadGump.UpdateAllOptions` (`:538-547`) with a doc comment describing the font-setting-does-nothing-until-reload behaviour it fixes.
