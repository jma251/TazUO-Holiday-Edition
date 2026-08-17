# ui-controls-b

Every-other-file slice (`awk 'NR%2==0'`) of `Game/UI/Controls` + `Game/UI` (maxdepth 2).
83 files, 41,915 lines, all read in full. Root path prefix omitted below:
`/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/UI/`.

Note: this partition does **not** contain `Control.cs`, `UIManager.cs`, `ScrollArea.cs`,
`ResizableGump.cs`, `DataBox.cs`, `Area.cs`, `AlphaBlendControl.cs`, `Combobox.cs`,
`HSliderBar.cs`, `Line.cs`, `BorderControl.cs`, `ResizePic.cs`, `ItemGump.cs`,
`StbTextBox.cs`, `VBoxContainer.cs`, `Positioner.cs`, `ScrollBarBase.cs`,
`CustomToolTip.cs`, `SimpleProgressBar.cs`, `ModernScrollBar.cs`, `NineSliceGump`'s
`ModernUIConstants`, `ContainerGump.cs`, `GridHighlightMenu.cs`, `PaperDollInteractable.cs`
— those are the odd-numbered half and are heavily depended on from here.

## Files

### Controls/
| path | lines | purpose |
|---|---|---|
| Controls/AnimationDisplay.cs | 98 | Control that plays a body-animation Stand frame loop; frame advances on a fixed ms timer. |
| Controls/ArrowNumbersTextBox.cs | 192 | Numeric StbTextBox + up/down `Button`s with 250 ms auto-repeat. |
| Controls/Button.cs | 356 | Classic 3-graphic gump button (normal/pressed/over) + optional RenderedText caption; server-gump ctor from `List<string>` parts. |
| Controls/Checkbox.cs | 155 | Two-graphic checkbox with `RenderedText` label; `ValueChanged` event; server-gump ctor. |
| Controls/ClickPriority.cs | 40 | `enum ClickPriority { High, Default, Low }`. |
| Controls/ColorBox.cs | 84 | Solid white texture drawn with a hue vector; caches hueVector on Hue/Alpha change. |
| Controls/ColorSelectorControl.cs | 565 | Self-drawn RGBA picker: 4 gradient slider tracks, hex + per-channel `StbTextBox` inputs, preview swatch. |
| Controls/ContextMenuControl.cs | 409 | Context-menu model (`ContextMenuItemEntry`) + `ContextMenuShowMenu` gump + nested `ContextMenuItem` rows and submenus. |
| Controls/CroppedText.cs | 81 | HTML `RenderedText` with `FontStyle.Cropped`; server-gump ctor. |
| Controls/ExpandableScroll.cs | 312 | 4-piece resizable scroll background (top/right/middle/bottom) with a drag "expander" button; height clamped 274..800. |
| Controls/FadingLabel.cs | 33 | Label that decrements Alpha by 0.01 per Draw once a frame counter passes `tickSpeed`, then self-disposes. |
| Controls/GumpPic.cs | 314 | `GumpPicBase` + `GumpPic` (+`EmbeddedGumpPic` for raw Texture2D, `GumpPicInPic` for sub-rect). Pixel-accurate `Contains`. |
| Controls/GumpPicTiled.cs | 184 | Tiled gump texture; tile-aware `Contains` walk-back then pixel check. |
| Controls/HBoxContainer.cs | 93 | Horizontal auto-layout container built on `Positioner`. |
| Controls/HitBox.cs | 98 | Invisible click region; `ClickPriority.High`; draws a translucent white fill on hover. |
| Controls/HotkeyBox.cs | 322 | Classic hotkey capture widget (key/mouse-button/wheel/controller) with OK/Cancel gump buttons. |
| Controls/HoveredLabel.cs | 120 | Label that swaps hue between normal/over/selected each Update. |
| Controls/HttpClickableLink.cs | 59 | TTF text that opens a URL via `PlatformHelper.LaunchBrowser` on left mouse-up. |
| Controls/InputField.cs | 89 | ResizePic background + `StbTextBox`, clipped draw. (Distinct from the nested `BaseOptionsGump.InputField`.) |
| Controls/Label.cs | 134 | `RenderedText` wrapper; server-gump ctor; disposes its RenderedText. |
| Controls/MacroControl.cs | 649 | Classic macro editor control: `HotkeyBox`, DataBox of `MacroEntry` rows, submenu combobox / free-text per macro op. |
| Controls/ModernScrollArea.cs | 262 | Scroll container using `ModernScrollBar` (12 px); recalculates max value in `SlowUpdate`, draws children offset by scroll value. |
| Controls/MultiSelectionShrinkbox.cs | 383 | **Entirely commented out.** Dead file, namespace only. |
| Controls/NiceButton.cs | 195 | `HitBox` + `Label`, radio-group selection by `_groupnumber`, optional border/background. |
| Controls/NineSliceControl.cs | 141 | 9-slice texture control (does not own the texture). |
| Controls/RadioButton.cs | 105 | Checkbox subclass; unchecks siblings in the same `GroupIndex` under the same parent. |
| Controls/ResizableStaticPic.cs | 108 | Art tile scaled/centred into an arbitrary WxH box; optional gray border. |
| Controls/ScissorControl.cs | 72 | Draw-order marker that calls `batcher.ClipBegin` / `ClipEnd`. |
| Controls/ScrollBar.cs | 282 | Classic gump-art vertical scroll bar (gumps 250..257). |
| Controls/ScrollFlag.cs | 188 | Flag-style scrollbar (gump 0x0828); up/down buttons hardcoded off. |
| Controls/SimpleBorder.cs | 56 | 1 px rectangle outline; reports Width/Height as 0 so it never blocks hit tests. |
| Controls/StaticPic.cs | 134 | Art-tile pic with pixel-accurate `Contains`; disposes itself if the art has no texture. |
| Controls/TTFTextInputField.cs | 1017 | TTF text input: outer control + nested `StbTextBox : Control, ITextEditHandler` (stb_textedit, selection, clipboard, caret, placeholder). |
| Controls/TextBox.cs | 578 | **Pooled** FontStashSharp `RichTextLayout` wrapper. `GetOne`/`Dispose` recycle through a static queue. HTML→FSS command conversion, stroke handling. |

### Gumps/ (and Game/UI root)
| path | lines | purpose |
|---|---|---|
| Gumps/AnchorableGump.cs | 258 | Base for gumps that snap into `UIManager.AnchorManager` groups; draws drop preview + Alt lock icon. |
| Gumps/ArtBrowserGump.cs | 124 | Debug art browser: fixed grid of `ResizableStaticPic`, page/graphic inputs, double-click copies graphic id. |
| Gumps/BaseOptionsGump.cs | 3049 | Options framework: `ThemeSettings`, `PositionHelper`, `SettingsOption`, `LeftSideMenuRightSideContent`, `ModernButton`, own `ScrollArea`+`ScrollBar`, `InputField`(+`StbTextBox`), `HotkeyBox`, `ComboBoxWithLabel`, `CheckboxWithLabel`, `SliderWithLabel`, `ModernColorPickerWithLabel`, `InputFieldWithLabel`, static search. |
| Gumps/BuffGump.cs | 383 | Classic buff bar; `BuffControlEntry` pulses alpha and rebuilds the gump when a buff expires. |
| Gumps/ChatGump.cs | 536 | UO Chat channel list / join / leave / create. |
| Gumps/ColorPickerGump.cs | 118 | Dye-tub picker; sends `Send_DyeDataResponse`. |
| Gumps/CommandsGump.cs | 62 | Lists `CommandManager.Commands` keys in a scroll area. |
| Gumps/CoolDownBar.cs | 258 | Timed cooldown bar gump + `CoolDownConditionData` CRUD over parallel profile lists. |
| Gumps/CreditsGump.cs | 84 | Scrolling credits, 1 px per ≥25 ms. |
| Gumps/DebugGump.cs | 255 | FPS/pos/profiler overlay refreshed every 100 ms. |
| Gumps/DurabilityGump.cs | 222 | `DurabilityGumpMinimized` icon + `DurabilitysGump` (NineSliceGump) listing equipment durability bars. |
| Gumps/GridContainer.cs | 1997 | Grid container gump: `GridItem`, `GridSlotManager`, `GridScrollArea`, `GridContainerPreview`. Search, sort, slot locking, multi-item alt-drag select, highlight borders. |
| Gumps/Gump.cs | 381 | Base gump: lock, alpha scroll, page handling, save/restore XML, screen clamping, `OnButtonClick` → `GameActions.ReplyGump`. |
| Gumps/HealthBarGump.cs | 2190 | `BaseHealthBarGump` + `HealthBarGumpCustom` (line-drawn) + `HealthBarGump` (classic art). Per-frame entity poll, rename box, party heal buttons. |
| Gumps/IgnoreManagerGump.cs | 233 | Ignore list editor over `IgnoreManager`. |
| Gumps/InfoBarGump.cs | 424 | Resizable info bar; `InfoBarControl` refreshes its var text/hue every 250 ms. |
| Gumps/InspectorGump.cs | 352 | Dumps `GameObject` properties to labels; click copies to clipboard; button writes `dump_gameobject.txt`. |
| Gumps/MacroButtonEditorGump.cs | 341 | Edits a `Macro`'s button appearance (hide label / scale / hue / graphic) with a live `MacroButtonGump` preview. |
| Gumps/MacroGump.cs | 45 | Thin wrapper hosting a `MacroControl` for fast-assign. |
| Gumps/MarkersManagerGump.cs | 548 | World-map marker manager: per-file tabs, search, edit/remove/goto; writes `userMarkers.usr` on Dispose. |
| Gumps/MessageBoxGump.cs | 285 | Modal OK / OK-Cancel box + `EntryDialog` (modal single-line prompt). |
| Gumps/ModernBookGump.cs | 884 | Book reader/editor; `StbPageTextBox` re-flows the whole book into 8-line pages and tracks per-page dirty flags. |
| Gumps/ModernOptionsGump.cs | 5581 | The whole options UI built on `BaseOptionsGump`; also nested `InfoBarBuilderControl`, `mainScrollArea`, its own `MacroControl`, `NameOverheadAssignControl`, `ProfileLocationData`, `PAGE` enum. |
| Gumps/ModernShopGump.cs | 530 | TazUO vendor gump: search box, per-item expand-to-buy row with quantity slider, drag-resize. |
| Gumps/MultipleToolTipGump.cs | 109 | Hosts several `CustomToolTip`s side by side; static SS* rect used for the screenshot-tooltip feature. |
| Gumps/NameOverHeadHandlerGump.cs | 226 | Nameplate option picker (radio list + search box + toggles). |
| Gumps/NearbyItems.cs | 214 | Ground-item quick panel (`NearbyItemDisplay` with Loot/Use halves); auto-closes on move or after 30 s. |
| Gumps/NineSliceGump.cs | 364 | Resizable 9-slice gump base with corner-drag resize handled in `Update`. |
| Gumps/PartyGump.cs | 412 | Party manifest (NineSliceGump); per-member Msg/Kick + `SimpleProgressBar` HP, refreshed per frame. |
| Gumps/PopupMenuGump.cs | 167 | Server popup menu; hover sets `_selectedItem`, left-up sends `ResponsePopupMenu`. |
| Gumps/ProgressBarGump.cs | 52 | Generic percentage bar gump. |
| Gumps/QuestionGump.cs | 114 | Modal yes/no with an `Action<bool>` callback. |
| Gumps/RaceChangeGump.cs | 823 | Race-change UI: fake `PlayerMobile` + fake hair/beard `Item`s in `World`, custom paperdoll, colour pickers. |
| Gumps/RacialAbilityButton.cs | 96 | Anchorable racial-ability icon (gargoyle fly toggle). |
| Gumps/ResizableJournal.cs | 814 | Tabbed journal; `JournalEntriesContainer` holds a `Deque<JournalData>` of pooled TextBoxes, custom scroll math and per-tab MessageType filters. |
| Gumps/ShopGump.cs | 1216 | Classic buy/sell gump; `ShopItem`, `TransactionItem` (+/- with accelerating repeat), `GumpPicTexture`, `ResizePicLine`; drag expander. |
| Gumps/SkillButtonGump.cs | 161 | Anchorable skill button, restores by skill index. |
| Gumps/SkillProgressBar.cs | 164 | Transient skill-change bar with a static `ConcurrentQueue` scheduler (`QueManager`), 4 s lifetime. |
| Gumps/SplitMenuGump.cs | 220 | Stack-split slider/textbox; sends `GameActions.PickUp`. |
| Gumps/StatusGump.cs | 2216 | `StatusGumpBase` + `StatusGumpOld` / `StatusGumpModern` / `StatusGumpOutlands`; all refresh labels every 250 ms. |
| Gumps/SystemChatControl.cs | 1057 | Chat input line + fading `ChatLineTime` history; chat modes, message history (static), autocomplete, prompt handling. |
| Gumps/TextEntryDialogGump.cs | 158 | Server text-entry dialog; modal, sends `Send_TextEntryDialogResponse`. |
| Gumps/TooltipConfigGump.cs | 363 | Tooltip-override editor rows with 1.5 s debounced save onto the main thread queue. |
| Gumps/TradingGump.cs | 635 | Secure trade window; two `DataBox`es of `ItemGump`, gold/plat entries, accept checkboxes. |
| Gumps/UseAbilityButtonGump.cs | 147 | Anchorable primary/secondary ability icon; hues 38 when active. |
| Gumps/UserMarkerGump.cs | 360 | Add/edit a single world-map marker; appends to `userMarkers.usr`. |
| Gumps/WorldMapGump.cs | 3880 | World map: background map texture build (threaded), markers/zones/icons loading, mobiles/party/guild/corpse/pathfinder overlays, free-view scrolling, zoom, context menu. |
| NearbyLootGump.cs | 570 | Nearby-corpse loot list driven by `EventSink` corpse/OPL/position events; keyboard + controller selection. |
| Tooltip.cs | 329 | The single tooltip renderer: OPL revision tracking, override processing, delay, clamping to window, static X/Y/W/H for the screenshot feature. |

## Types (selected; file:line)

- `TextBox` — Controls/TextBox.cs:47 — pooled RTL text; `GetOne`/`Reset`/`Dispose` recycle.
- `TextBox.RTLOptions` — Controls/TextBox.cs:506 — layout options record.
- `Control`-derived primitives: `Label` Controls/Label.cs:40, `CroppedText` Controls/CroppedText.cs:39, `HoveredLabel` Controls/HoveredLabel.cs:39, `FadingLabel` Controls/FadingLabel.cs:7, `ColorBox` Controls/ColorBox.cs:38, `SimpleBorder` Controls/SimpleBorder.cs:6, `HitBox` Controls/HitBox.cs:39, `NiceButton` Controls/NiceButton.cs:41, `Button` Controls/Button.cs:50, `Checkbox` Controls/Checkbox.cs:42, `RadioButton` Controls/RadioButton.cs:38, `StaticPic` Controls/StaticPic.cs:41, `ResizableStaticPic` Controls/ResizableStaticPic.cs:6, `GumpPicBase` Controls/GumpPic.cs:43, `GumpPic` Controls/GumpPic.cs:150, `GumpPicInPic` Controls/GumpPic.cs:236, `EmbeddedGumpPic` Controls/GumpPic.cs:106, `GumpPicTiled` Controls/GumpPicTiled.cs:40, `ScissorControl` Controls/ScissorControl.cs:37, `NineSliceControl` Controls/NineSliceControl.cs:7, `AnimationDisplay` Controls/AnimationDisplay.cs:8, `HttpClickableLink` Controls/HttpClickableLink.cs:13, `HBoxContainer` Controls/HBoxContainer.cs:5.
- `ScrollBar` Controls/ScrollBar.cs:40, `ScrollFlag` Controls/ScrollFlag.cs:39, `ModernScrollArea` Controls/ModernScrollArea.cs:8, `ExpandableScroll` Controls/ExpandableScroll.cs:39.
- Text entry: `InputField` Controls/InputField.cs:6; `TTFTextInputField` Controls/TTFTextInputField.cs:14 with nested `StbTextBox` Controls/TTFTextInputField.cs:121; `ArrowNumbersTextBox` Controls/ArrowNumbersTextBox.cs:38; `HotkeyBox` Controls/HotkeyBox.cs:42.
- Context menu: `ContextMenuControl` Controls/ContextMenuControl.cs:43, `ContextMenuItemEntry` :95, `ContextMenuShowMenu` :118, `ContextMenuItem` :244.
- `ColorSelectorControl` Controls/ColorSelectorControl.cs:10, `ColorChangedEventArgs` :556.
- `MacroControl` Controls/MacroControl.cs:47 (+ nested `MacroEntry` :452).
- `Gump` Gumps/Gump.cs:47 — base of every gump here.
- `AnchorableGump` Gumps/AnchorableGump.cs:51, `ANCHOR_TYPE` :43.
- `NineSliceGump` Gumps/NineSliceGump.cs:9 — resizable 9-slice gump base (Party, Durability, TooltipConfig).
- `BaseOptionsGump` Gumps/BaseOptionsGump.cs:18 and nested: `ThemeSettings` :222, `SearchableOption` :265, `PositionHelper` :272, `SettingsOption` :325, `LeftSideMenuRightSideContent` :404, `HotkeyBox` :548, `InputField` :800 (+`StbTextBox` :902), `ModernButton` :1738, `ScrollArea` :1907 (+`ScrollBar` :2092), `ComboBoxWithLabel` :2194 (+`Combobox` :2276, `ComboboxGump` :2364, `HoveredLabel` :2440), `InputFieldWithLabel` :2515, `ModernColorPickerWithLabel` :2592, `CheckboxWithLabel` :2660, `SliderWithLabel` :2785 (+`Slider` :2853).
- `ModernOptionsGump` Gumps/ModernOptionsGump.cs:28 and nested `InfoBarBuilderControl` :4472, `mainScrollArea` :4609, `MacroControl` :4727 (+`MacroEntry` :5076), `NameOverheadAssignControl` :5290, `ProfileLocationData` :5542, `PAGE` :5561.
- `BaseHealthBarGump` Gumps/HealthBarGump.cs:51, `HealthBarGumpCustom` :467 (+`LineCHB` :1428, `Settings` :1492), `HealthBarGump` :1524 (+`Settings` :2162).
- `StatusGumpBase` Gumps/StatusGump.cs:46, `StatusGumpOld` :252, `StatusGumpModern` :621 (+`Settings` :1604), `StatusGumpOutlands` :1657.
- `GridContainer` Gumps/GridContainer.cs:55, `GridItem` :842, `GridSlotManager` :1441, `GridScrollArea` :1715, `GridContainerPreview` :1909, `GridSortMode` :823, `BorderStyle` :829.
- `WorldMapGump` Gumps/WorldMapGump.cs:67, `WMapMarker` :889, `WMapMarkerFile` :902, `CurLoader` :911, `Zone` :1695, `ZoneSet` :1730, `ZoneSets` :1757, `ZonesJsonContext` :65.
- `ResizableJournal` Gumps/ResizableJournal.cs:18, `JournalEntriesContainer` :483, `JournalData` :662, `TabContextEntry` :685.
- `SystemChatControl` Gumps/SystemChatControl.cs:72, `ChatMode` :54, `ChatLineTime` :1000.
- `ModernBookGump` Gumps/ModernBookGump.cs:48, `StbPageTextBox` :547.
- `ShopGump` Gumps/ShopGump.cs:50, `ShopItem` :658, `TransactionItem` :907, `ResizePicLine` :1107, `GumpPicTexture` :1157.
- `ModernShopGump` Gumps/ModernShopGump.cs:15, `ShopItem` :193, `BuySellButton` :470.
- `TradingGump` Gumps/TradingGump.cs:47 (derives `TextContainerGump`).
- `NearbyLootGump` NearbyLootGump.cs:19 + `NearbyItemDisplay` NearbyLootGump.cs:357 — **name collision** with `NearbyItemDisplay` Gumps/NearbyItems.cs:104 (different namespaces: `ClassicUO.Game.UI` vs `ClassicUO.Game.UI.Gumps`).
- `Tooltip` Tooltip.cs:45 — not a Control; owned by `Control`/UIManager and drawn manually.
- `MultipleToolTipGump` Gumps/MultipleToolTipGump.cs:6.
- `SkillProgressBar` Gumps/SkillProgressBar.cs:10 + `QueManager` :111.
- `CoolDownBar` Gumps/CoolDownBar.cs:10 + `CoolDownConditionData` :152.
- `InfoBarGump` Gumps/InfoBarGump.cs:46 + `InfoBarControl` :158.
- `RaceChangeGump` Gumps/RaceChangeGump.cs:14 + `CustomColorPicker` :530, `CustomPaperDollGump` :661.
- `MarkersManagerGump` Gumps/MarkersManagerGump.cs:16 + `DrawTexture` :315, `MakerManagerControl` :333, `SearchTextBoxControl` :468.
- `UserMarkersGump` Gumps/UserMarkerGump.cs:14.
- `MessageBoxGump` Gumps/MessageBoxGump.cs:48, `EntryDialog` :178.
- `DurabilityGumpMinimized` Gumps/DurabilityGump.cs:16, `DurabilitysGump` :55.
- `BuffGump` Gumps/BuffGump.cs:48 + `BuffControlEntry` :242.

## State (mutable / static / global owned here)

Pools and caches
- `TextBox._pool` Controls/TextBox.cs:49 — `static Queue<TextBox>`; `Dispose()` (:467) resets and enqueues, `GetOne` (:93) dequeues and re-inits. Identity is reused across unrelated call sites.
- `ContextMenuShowMenu.ContextMenuItem._moreMenuLabel` Controls/ContextMenuControl.cs:246 — static `RenderedText` shared by every submenu arrow, never destroyed.
- `TextBox._baseFontColorRegex` / `_bodyTextColorRegex` Controls/TextBox.cs:410-411 — static compiled regexes.

Window-position / size memory (static, survives gump close)
- `DebugGump._last_position` Gumps/DebugGump.cs:55.
- `WorldMapGump._last_position` Gumps/WorldMapGump.cs:69.
- `NameOverHeadHandlerGump.LastPosition` Gumps/NameOverHeadHandlerGump.cs:44.
- `GridContainer.lastX/lastY/lastCorpseX/lastCorpseY` Gumps/GridContainer.cs:63; `borderWidth` :65 (static, mutated by whichever container last called `BuildBorder`).
- `ResizableJournal._lastX/_lastY/_lastWidth/_lastHeight` Gumps/ResizableJournal.cs:49-50; `BORDER_WIDTH` :23 (static, mutated per instance in `BuildBorder`).
- `DurabilitysGump.lastWidth/lastHeight/lastX/lastY` Gumps/DurabilityGump.cs:57-58.
- `NearbyLootGump._lastLocation` NearbyLootGump.cs:47.

Cross-gump singletons / statics
- `NearbyItems.NearbyItemGump` Gumps/NearbyItems.cs:18 — the one open instance.
- `NearbyLootGump._corpsesRequested` / `_openedCorpses` (static `HashSet<uint>`) NearbyLootGump.cs:44-45; `_selectedIndex` :46.
- `SkillProgressBar.QueManager.skillProgressBars` (static `ConcurrentQueue`), `CurrentProgressBar`, `beingReset` Gumps/SkillProgressBar.cs:113-115.
- `SystemChatControl._messageHistory` / `_messageHistoryIndex` Gumps/SystemChatControl.cs:77-78 — static, shared across all chat controls and never trimmed.
- `SystemChatControl.ChatLineTime.TextBoxOptions` Gumps/SystemChatControl.cs:1004 — single static `RTLOptions` instance shared by every chat line.
- `PopupMenuGump.CloseNext` Gumps/PopupMenuGump.cs:44 — static serial gate.
- `Tooltip.IsEnabled/X/Y/Width/Height` Tooltip.cs:53-56 — static; last-drawn tooltip rect.
- `MultipleToolTipGump.SSIsEnabled/SSX/SSY/SSWidth/SSHeight` Gumps/MultipleToolTipGump.cs:11-14.
- `BaseHealthBarGump.LastAttackBar` Gumps/HealthBarGump.cs:56.
- `WorldMapGump._mapTexture`, `_pixelBuffer`, `_zBuffer` Gumps/WorldMapGump.cs:92-94 — static, allocated once at max map size and reused for every map index.
- `WorldMapGump._markerFiles` :98, `_markerIcons` :102 (static `Dictionary<string,Texture2D>`), `following` :90.
- `MarkersManagerGump._markers` :30 and `_markerFiles` :32 — static aliases of `WorldMapGump._markerFiles`.
- `BaseOptionsGump.SearchText` :21 and `static event SearchValueChanged` :23 — every searchable option subscribes in its ctor and unsubscribes in `Dispose`.
- `BaseOptionsGump.PositionHelper.X/Y/LAST_Y` :274 — static layout cursor shared by all option pages being built.
- `BaseOptionsGump.ThemeSettings.*` :222-262 — static theme values.
- `GridContainer.GridItem._toggledThisAltDrag` / `_altDragActive` Gumps/GridContainer.cs:856-857 — static, shared by every grid slot in every open container.
- `ResizableJournal.ReloadTabs` :21 — static request flag polled in `Update`.
- `HealthBarGumpCustom._settings` :469 / `HealthBarGump._settings` :1526 / `StatusGumpModern._settings` :623 — lazily loaded `UISettings` JSON singletons; plus the derived static size/colour fields `HPB_*` Gumps/HealthBarGump.cs:491-512 (read once at type init).
- `IgnoreManagerGump._scrollArea` :23 — **static** field on a per-instance gump.
- `ModernBookGump.StbPageTextBox._sb` / `_handler` :549-550 — static scratch StringBuilder + string[] shared by all open books.
- `MacroControl._allHotkeysNames` / `_allSubHotkeysNames` Controls/MacroControl.cs:49-50 (and the copies in ModernOptionsGump.cs:4729-4730).

Per-instance state worth noting
- `Tooltip._hash/_serial/_textBox/_lastHoverTime/_dirty` Tooltip.cs:47-51.
- `GridSlotManager.gridSlots/itemPositions/itemLocks` Gumps/GridContainer.cs:1443-1449 — slot→serial map persisted through `GridContainerSaveData`.
- `ShopGump._shopItems` / `_transactionItems` Gumps/ShopGump.cs:66,72 — serial-keyed dictionaries.
- `WorldMapGump._center/_scroll/_lastScroll/_zoomIndex/_hiddenMarkerFiles/_hiddenZoneFiles/_zoneSets` :70-118.

## Timing

Per frame (`Update()` on every non-disposed control, driven by UIManager)
- `Gump.Update` Gumps/Gump.cs:121 — applies `InvalidateContents` → `UpdateContents()`, forces `ActivePage=1`.
- `HealthBarGumpCustom.Update` HealthBarGump.cs:555 and `HealthBarGump.Update` :1839 — full entity re-poll every frame: `World.Get(LocalSerial)`, notoriety hue, poison/yellow state, bar widths, out-of-range/dead close logic, `SelectedObject` assignment.
- `PartyGump.Update` PartyGump.cs:62 — writes 10 party HP bars every frame.
- `NineSliceGump.Update` NineSliceGump.cs:198 — corner-drag resize from `Mouse.Position` deltas.
- `GridContainer.Update` GridContainer.cs:619 — dispose checks (container gone, corpse >3 tiles), re-layout on size/scale change, `SelectedObject` assignment.
- `GridItem.Update` GridContainer.cs:1406 — alt+LMB drag multi-select gesture state machine.
- `ModernShopGump.Update` ModernShopGump.cs:115 / `NearbyLootGump.Update` :315 / `ShopGump.Update` :400 — drag-resize from `Mouse.LDragOffset`.
- `ExpandableScroll.Update` ExpandableScroll.cs:230 — resize while `Mouse.LButtonPressed`.
- `ResizableJournal.Update` ResizableJournal.cs:453 — position save, reposition when size changed and LMB released, `ReloadTabs`.
- `SystemChatControl.Update` SystemChatControl.cs:406 — walks the `ChatLineTime` list, expires entries, then re-parses the input line's first char to switch chat mode.
- `ContextMenuItem.Update` ContextMenuControl.cs:317 — submenu visibility from `UIManager.MouseOverControl` chain walk.
- `HoveredLabel.Update` HoveredLabel.cs:75 and `BaseOptionsGump...HoveredLabel.Update` :2470 — hue swap.
- `SplitMenuGump.Update` SplitMenuGump.cs:194 / `NearbyItems.Update` :37 / `GridContainerPreview.Update` :1979 — liveness checks and dispose.
- `NearbyItemDisplay.Update` NearbyLootGump.cs:434 — background hue follows selection/auto-loot state.
- `BaseOptionsGump.ScrollArea.Update` :1952 and `GridScrollArea.Update` :1761 — recompute scroll extents from children every frame.
- `ModernScrollArea` recomputes in `SlowUpdate` (Controls/ModernScrollArea.cs:90), not `Update`.

Timed / interval
- 100 ms — `DebugGump` text rebuild (DebugGump.cs:114).
- 250 ms — `StatusGumpOld/Modern/Outlands` label refresh (StatusGump.cs:575, 1509, 2018); `InfoBarGump` re-layout (InfoBarGump.cs:117); `InfoBarControl` data/hue refresh (:206).
- 250 ms — `ArrowNumbersTextBox` auto-repeat (`TIME_BETWEEN_CLICKS`, first repeat at 500 ms) Controls/ArrowNumbersTextBox.cs:40,89.
- 25 ms — `CreditsGump` scroll step (CreditsGump.cs:69).
- 650 ms default — `AnimationDisplay` frame advance (`_playspeedMs`) Controls/AnimationDisplay.cs:27,70.
- 1000 ms — `BuffControlEntry` tooltip/timer text refresh (BuffGump.cs:301); alpha pulse starts when <10000 ms remain (:316).
- 4000 ms — `SkillProgressBar` lifetime (`SetDuration(4000)`) SkillProgressBar.cs:149.
- 30000 ms — `NearbyItems` auto-close (NearbyItems.cs:41).
- 120000 ms — `NearbyLootGump` clears `_openedCorpses`/`_corpsesRequested` (NearbyLootGump.cs:343-348).
- 60 ms — `ShopGump` scroll-button repeat (`SCROLL_DELAY`) ShopGump.cs:62.
- 45 ms base, accelerating — `TransactionItem` +/- hold repeat (ShopGump.cs:973-991).
- Tooltip display delay — `ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay` (default 250 ms) Tooltip.cs:243, 325.
- 1500 ms — `TooltipConfigGump.SaveWithDelay` sleeps on a Task then posts to `MainThreadQueue` (TooltipConfigGump.cs:332-339).
- `CoolDownBar` uses wall-clock `DateTime.Now` (not `Time.Ticks`) for expiry, evaluated in `Draw` (CoolDownBar.cs:118).

Per packet / event
- `SystemChatControl` ← `EventSink.MessageReceived` (SystemChatControl.cs:147).
- `ResizableJournal` ← `EventSink.JournalEntryAdded` (ResizableJournal.cs:134).
- `NearbyLootGump` ← `EventSink.OnCorpseCreated`, `OnPositionChanged`, `OPLOnReceive` (NearbyLootGump.cs:120-122).
- `ShopGump.AddItem` / `ModernShopGump.AddItem` / `SetNameTo` — called from vendor packet handlers.
- `TradingGump.UpdateContents` — from secure-trade packets.
- `ModernBookGump.ServerSetBookText` / `SetTile` / `SetAuthor` — from book packets.
- `Tooltip.SetGameObject` — from hover; re-reads OPL when `World.OPL` revision changes (Tooltip.cs:66).
- `GridContainer.HandleObjectMessage` — from overhead-message handling.

On load / on demand
- `WorldMapGump.Load()` (:1476) runs the whole map raster on a `Task.Run` worker thread, writing `_pixelBuffer`/`_zBuffer` and calling `Texture2D.SetDataPointerEXT`.
- `WorldMapGump.LoadMarkers()` (:1830) / `LoadZones()` (:1801) do synchronous file IO on the frame thread.
- `MarkersManagerGump.Dispose` / `UserMarkersGump.AddNewMarker` / `WorldMapGump.AddUserMarker`/`RemoveUserMarker` write `userMarkers.usr` synchronously.
- `ModernOptionsGump.BuildTazUO` enumerates the whole profiles directory tree at construction (ModernOptionsGump.cs:3975-3995).
- `ModernOptionsGump.Dispose` saves the profile (:4275).

## Inbound

- **UIManager** drives everything: `Add`, per-frame `Update`, `Draw`, mouse/keyboard routing to `OnMouse*`/`OnKey*`, `KeyboardFocusControl`, `MouseOverControl`, `DraggingControl`, `AnchorManager`, `ShowContextMenu`, `SavePosition`/`RemovePosition`, `GetGump<T>()`.
- **Network packet handlers** construct server gumps and controls: `Button(List<string>)`, `Checkbox(parts,lines)`, `RadioButton(group,parts,lines)`, `Label(parts,lines)`, `CroppedText(parts,lines)`, `StaticPic(parts)`, `GumpPic(parts)`, `GumpPicInPic(parts)`, `GumpPicTiled(parts)`; plus `TextEntryDialogGump`, `PopupMenuGump`, `ColorPickerGump`, `ShopGump`/`ModernShopGump`, `TradingGump`, `ModernBookGump`, `RaceChangeGump`, `QuestionGump`, `MessageBoxGump`.
- **Gump save/restore**: `UIManager`/`ProfileManager` call `Gump.Save(XmlTextWriter)` / `Restore(XmlElement)` — implemented by `AnchorableGump` subclasses, `BuffGump`, `DebugGump`, `GridContainer`, `ResizableJournal`, `DurabilitysGump`, `InfoBarGump`, `HealthBarGump*`, `RacialAbilityButton`, `SkillButtonGump`, `UseAbilityButtonGump`, `WorldMapGump`.
- **GameActions / macros / commands**: `GameActions.OpenSettings` → `ModernOptionsGump`; `StatusGumpBase.AddStatusGump`; `MultiItemMoveGump`, `GridHighlightMenu.Open`, `AutoLootManager`.
- **Managers pushing into gumps**: `NameOverHeadManager` → `NameOverHeadHandlerGump.UpdateCheckboxes/RedrawOverheadOptions`; `DurabilityManager` → `DurabilitysGump`; `SkillProgressBar.QueManager.AddSkill` from skill-change handling; `CoolDownBarManager` → `CoolDownBar`; `ChatManager` → `ChatGump.UpdateConference`.
- **Static broadcast updaters** called from options: `GridContainer.UpdateAllGridContainers()`, `ResizableJournal.UpdateJournalOptions()`, `InfoBarGump.UpdateAllOptions()`, `ModernPaperdoll.UpdateAllOptions()`, `NameOverheadGump.UpdateAllOptions()`.
- **Python / Legion scripting API** reaches `Button.HasBeenClicked()` (Controls/Button.cs:282), `TextBox.SetText`, `InputField`, gump construction.

## Outbound

- `ClassicUO.Renderer`: `UltimaBatcher2D` (Draw/DrawTiled/DrawRectangle/DrawLine/DrawString/ClipBegin/ClipEnd/SetBlendState), `ShaderHueTranslator.GetHueVector`, `SolidColorTextureCache.GetTexture`, `RenderedText`, `Fonts.*`, `SpriteFont`.
- `ClassicUO.Assets`: `Client.Game.Gumps.GetGump/PixelCheck`, `Client.Game.Arts.GetArt/GetRealArtBounds/PixelCheck`, `Client.Game.Animations.GetAnimationFrames/GetAnimType`, `TileDataLoader`, `ClilocLoader`, `FontsLoader`, `HuesLoader`, `MapLoader`, `AnimationsLoader`, `GumpsLoader`, `TrueTypeLoader`.
- `ClassicUO.Network.NetClient.Socket.Send_*`: `DyeDataResponse`, `TextEntryDialogResponse`, `BuyRequest`, `SellRequest`, `TradeUpdateGold`, `PartyChangeLootTypeRequest`, `PartyInviteRequest`, `PartyRemoveRequest`, `PartyDecline`, `ChatJoinCommand`, `ChatLeaveChannelCommand`, `ChatCreateChannelCommand`, `ChatMessageCommand`, `ASCIIPromptResponse`, `UnicodePromptResponse`, `BookPageDataRequest`, `BookPageData`, `BookHeaderChanged(_Old)`, `ChangeRaceRequest`, `ToggleGargoyleFlying`, `VirtueGumpResponse`, `ClientViewRange`.
- `GameActions`: `Print`, `DoubleClick(Queued)`, `SingleClick`, `PickUp`, `DropItem`, `GrabItem`, `UseSkill`, `UsePrimary/SecondaryAbility`, `CastSpell`, `Attack`, `OpenCorpse`, `Rename`, `RequestMobileStatus`, `SendCloseStatus`, `ChangeStatLock`, `ReplyGump`, `ResponsePopupMenu`, `AcceptTrade`, `CancelTrade`, `Say`, `SayParty`, `RequestParty*`, `OpenSettings`.
- `World`: `Player`, `Items`, `Mobiles`, `Party`, `OPL`, `Map`, `MapIndex`, `HouseManager`, `WMapManager`, `CorpseManager`, `DurabilityManager`, `Light`, `ClientFeatures`, `ClientViewRange`, `RemoveItem`, `GetOrCreateItem`.
- `Managers`: `UIManager`, `TargetManager`, `MessageManager`, `SelectedObject`, `DelayedObjectClickManager`, `ContainerManager`, `AutoLootManager`, `MoveItemQueue`, `MacroManager`, `InfoBarManager`, `NameOverHeadManager`, `IgnoreManager`, `ChatManager`, `CommandManager`, `JournalManager`, `TextHistoryManager`, `ToolTipOverrideData`, `SpellVisualRangeManager`, `Pathfinder`, `EventSink`, `MainThreadQueue`, `GridContainerSaveData`.
- `Configuration`: `ProfileManager.CurrentProfile` (read and written directly from option callbacks), `Settings.GlobalSettings`, `UISettings` JSON load/save.
- `Client.Game`: `Scene`, `GetScene<GameScene>()`, `Window.ClientBounds`, `GraphicsDevice`, `GameCursor`, `Audio`, `SetRefreshRate`, `SetWindowBorderless`.
- SDL2 (`SDL_SetClipboardText`, text input, surfaces in `CurLoader`), `System.IO`, `System.Net.Http`, `System.Text.Json`, `System.Windows.Forms` (using in TTFTextInputField.cs:2), `Task.Factory.StartNew`.

## Hazards

- Controls/TextBox.cs:467-476 — `Dispose()` resets the object and enqueues it into a static pool; a caller that keeps a reference after disposing gets a live object whose text/font/options were reassigned to someone else's. Several gumps hold long-lived `TextBox` fields (e.g. `Tooltip._textBox` Tooltip.cs:49, `ChatLineTime.textBox` SystemChatControl.cs:1003, `JournalData.EntryText` ResizableJournal.cs:678).
- Controls/TextBox.cs:49 — pool is unbounded and never trimmed; `Reset()` nulls `_rtl` but the pooled `TextBox` objects live for the process lifetime.
- Controls/TextBox.cs:310 — `Text` getter dereferences `_rtl` with no null check while `Reset()` (:400) sets `_rtl = null`; a pooled-but-not-yet-reinitialised instance throws.
- Gumps/SystemChatControl.cs:77 — `_messageHistory` is a static `List` that grows for the process lifetime and is shared across characters/sessions.
- Gumps/SystemChatControl.cs:406-422 and :527-548 — the `_textEntries` `LinkedList` is walked and `Remove`d in both `Update` and `Draw`; `Draw` mutates the list while iterating it backwards.
- Gumps/SystemChatControl.cs:1004 — one static `RTLOptions` object is passed to every `TextBox.GetOne` for chat lines; `TextBox` stores the reference and later writes `Options.Width` (TextBox.cs:279, :458), so one line resizing mutates the options of all of them.
- Gumps/ModernShopGump.cs:174-178 — items are removed from `scrollArea.Children` directly (`scrollArea.Children.Remove(i)`) rather than through `Remove`, bypassing `OnChildRemoved`/parent bookkeeping.
- Gumps/ShopGump.cs:567, :582, :597 — `_shopItems[transactionItem.LocalSerial]` indexed without `TryGetValue`; a transaction row whose shop item was removed throws `KeyNotFoundException`.
- Gumps/ShopGump.cs:457 — `if (_shopItems.Count == 0) Dispose();` runs every frame, so a vendor gump that receives its item packets a frame late disposes itself.
- Gumps/GridContainer.cs:65 — `borderWidth` is `static` but assigned per instance in `BuildBorder()` (:763, :786); two grid containers with different border styles fight over one value, and `GetWidth`/`GetHeight` (:414,:426) read it.
- Gumps/ResizableJournal.cs:23 — same pattern: `BORDER_WIDTH` is static and reassigned in each instance's `BuildBorder()` (:240, :263).
- Gumps/GridContainer.cs:856-857 — `_toggledThisAltDrag`/`_altDragActive` are static on `GridItem`, so an alt-drag in one container resets the gesture state of slots in every other open container.
- Gumps/GridContainer.cs:1509-1541 — `RebuildContainer` iterates `itemPositions` while `AddLockedItemSlot` (called at :1539 in the second loop) adds/removes entries in `ItemPositions`; the mutation is in a later loop but `filteredItems.Remove(i)` (:1527) also mutates the list being enumerated in the outer `foreach` over `itemPositions`... the `filteredItems` mutation happens inside `foreach (var spot in itemPositions)`, and `filteredItems` is then enumerated at :1532.
- Gumps/GridContainer.cs:966-968 — `SetGridItem` sets `Y = Height - count.Height` on the **GridItem itself** (not on `count`), moving the slot control; `SetGridPositions` (:1582) later overwrites `Y`, so the effect depends on call order within the frame.
- Gumps/GridContainer.cs:1704-1711 — `SetupGridItemControls` grows `gridSlots` to `max(125, contentCount)` and never shrinks; slot controls accumulate for the container's lifetime.
- Gumps/GridContainer.cs:80 — `container` is a property that does `World.Items.Get(LocalSerial)` on every access; the serial can be reassigned by the server to a different item.
- Gumps/HealthBarGump.cs:86 — `SetNewMobile` calls `Children.Clear()` without disposing the old children (`_textBox` in particular), then `BuildGump()`.
- Gumps/HealthBarGump.cs:538-539 and :1584-1585 — `UpdateContents` calls `Clear()` then `Children.Clear()`; `_textBox.MouseUp -= TextBoxOnMouseUp` is done but the `LineCHB`/`GumpPicWithWidth` arrays `_bars`/`_border` keep references to the disposed controls until `BuildGump` overwrites them.
- Gumps/HealthBarGump.cs:658 — `_hpLineRed.IsVisible = ...` with no null guard; `_hpLineRed` is nulled in `UpdateContents` (:542) and only reassigned inside `BuildGump`, and `Update` runs on the same frame path.
- Gumps/HealthBarGump.cs:1898 — `_buttonHeal1.IsVisible = _buttonHeal2.IsVisible = false;` in the non-party out-of-range branch; those are only created in the party layout, so they are null for a non-party bar that was in a party when built.
- Gumps/HealthBarGump.cs:491-512 — `HPB_*` statics are initialised from `settings` at type-init and the derived `HPB_BAR_SPACELEFT` never recomputes if settings change.
- Gumps/StatusGump.cs:1511 onwards — `_labels[(int)MobileStats.X]` indexed unguarded every 250 ms; labels for UOP-only stats are only created when `GumpsLoader.Instance.UseUOPGumps`, and the reads are guarded by the same flag but the flag is read at build time and at update time independently.
- Gumps/StatusGump.cs:56 — `StatusGumpBase` ctor disposes `UIManager.GetGump<HealthBarGump>(World.Player)` but not `HealthBarGumpCustom`.
- Tooltip.cs:139 — `_textBox.Update()` is called manually from inside `Draw` to force a re-layout; the tooltip's TextBox is not in the control tree.
- Tooltip.cs:53-56 — `IsEnabled`/`X`/`Y`/`Width`/`Height` are static and written by whichever tooltip drew last.
- Gumps/MultipleToolTipGump.cs:65-66 — `Dispose()` is called from inside `Draw` when the hover reference is no longer hovered.
- Gumps/MultipleToolTipGump.cs:35 — `toolTips[i].OnOPLLoaded += () => RepositionTooltips();` — closure over `this`, never unsubscribed.
- Controls/FadingLabel.cs:22 — `Dispose()` called from inside `Draw`.
- Gumps/CoolDownBar.cs:119 — `Dispose()` called from inside `Draw`; expiry uses `DateTime.Now`, not `Time.Ticks`.
- Gumps/SkillProgressBar.cs:105-109 — `Dispose()` calls `QueManager.ShowNext()`, which can `UIManager.Add` a new gump from inside a dispose triggered during the update walk.
- Gumps/SkillProgressBar.cs:21-24 — `UIManager.GetGump<WorldViewportGump>()` result used without a null check when `SkillProgressBarPosition` is `Point.Zero`.
- Gumps/WorldMapGump.cs:1476-1678 — `Load()` runs on a `Task.Run` thread and touches `World.Map.GetIndex`, `_mapTexture`, `_pixelBuffer`, `_zBuffer`, and calls `Texture2D.SetDataPointerEXT` off the frame thread while `Draw` (:2411) samples `_mapTexture`.
- Gumps/WorldMapGump.cs:666-669 — `Update` calls `Load()` whenever `_mapIndex != World.MapIndex`; `_mapIndex` is set at the top of `Load` but the raster completes asynchronously, so a map change during the load leaves stale pixels.
- Gumps/WorldMapGump.cs:92-94 — `_mapTexture`/`_pixelBuffer`/`_zBuffer` are static and shared by every `WorldMapGump`; two open maps race on the same buffers.
- Gumps/WorldMapGump.cs:1840-1846 — `LoadMarkers` disposes every texture in `_markerIcons` before clearing, but `WMapMarker.MarkerIcon` fields in `_markerFiles` still point at them until the files are re-parsed; `MarkersManagerGump._markers` (static, MarkersManagerGump.cs:30) also holds them.
- Gumps/WorldMapGump.cs:2066 — inside the CSV marker parser, an empty line does `return;` out of the whole `LoadMarkers` method rather than `continue`, abandoning the remaining files.
- Gumps/WorldMapGump.cs:3579 — `_lastMousePosition.Value` dereferenced in `OnMouseUp` without a null check (`_lastMousePosition` is set to null in `OnMouseExit`, :3738).
- Gumps/WorldMapGump.cs:3053 — hover hit-test for markers compares raw `Mouse.Position` against gump-local `rot` coordinates.
- Gumps/MarkersManagerGump.cs:30-32 — `_markers` and `_markerFiles` are static and aliased to `WorldMapGump._markerFiles`; `MarkerRemoveEventHandler` (:244) does `_markers.RemoveAt(idx)` using an index captured when the row was built, so removing after a search filter changes the list removes the wrong marker.
- Gumps/MarkersManagerGump.cs:296-311 — `Dispose` rewrites the whole `userMarkers.usr` from `_markers`, which is whichever file's list was last selected (`_markerFiles[buttonID].Markers`, :273) — a non-user file's contents can be written over the user marker file.
- Gumps/IgnoreManagerGump.cs:23 — `_scrollArea` is `static`; two gumps (or a reopened one) share and overwrite it, and `Redraw()` (:190) removes whatever the static currently points at.
- Gumps/NearbyLootGump.cs:218 — `_corpsesRequested.Contains(corpse)` / `.Remove(corpse)` pass an `Item` to a `HashSet<uint>` (implicit serial conversion); `_openedCorpses.Add(corpse)` (:221) same. Serials outlive the items.
- Gumps/NearbyLootGump.cs:257 — `_dataBox.Children[SelectedIndex].LocalSerial` indexed after a bounds check against `Count`, but `SelectedIndex` is a static that other gumps/instances also write.
- Gumps/NearbyLootGump.cs:478 — `Draw` dereferences `currentItem.Hue` before the `currentItem != null` check at :534.
- Gumps/NearbyLootGump.cs:274 — `Dispose` clears the static `_corpsesRequested` for all instances.
- Gumps/NearbyItems.cs:51 — `foreach (Item i in World.Items.Values)` while building; `NearbyItemDisplay` captures the `Item` reference in closures (:130, :142) that outlive the item.
- Gumps/NearbyItems.cs:107 and NearbyLootGump.cs:59 — `ProfileManager.CurrentProfile` dereferenced without a null check in field initialisers.
- Gumps/RaceChangeGump.cs:502 — `CreateItem` ignores its `id` parameter for the serial and always uses `0x4000_0000 + layer`, creating real `World` items with synthetic serials; they are only removed on confirm (:460-467), not on right-click close.
- Gumps/RaceChangeGump.cs:807-819 — `CustomPaperDollGump.RequestUpdate` is `new` (hides the base method) and `Update()` does not call `base.Update()`, so children never update.
- Gumps/BuffGump.cs:320 — `((BuffGump)Parent.Parent)?.RequestUpdateContents();` — hard cast of `Parent.Parent`, and `RequestUpdateContents` is invoked from a child's `Update` while the parent is mid-iteration of its children.
- Gumps/BuffGump.cs:80-83 — `_box?.Clear(); _box?.Children.Clear(); Clear();` then rebuilds; `Children.Clear()` drops references without disposing.
- Controls/ExpandableScroll.cs:220 — `_gumpExpander.HitTest(...)` in `Contains` with no null check, but `Dispose()` (:179) sets `_gumpExpander = null` and `_isResizable` may be false so it is never created.
- Controls/ExpandableScroll.cs:56 — `_showButtons = false;` unconditionally overrides the `showbuttons` argument.
- Controls/ScrollFlag.cs:56 — same: `_showButtons = false; // showbuttons;`.
- Controls/HBoxContainer.cs:91 — `UpdateSize(Control c)` sets `Width = c.Width + c.X` (assign, not max), so adding a control that ends left of the previous one shrinks the container.
- Controls/ModernScrollArea.cs:198-207 — `Clear()` disposes children in reverse but never removes them from `Children`; it relies on the base `Update` sweep.
- Controls/ModernScrollArea.cs:157 — `Draw` unconditionally dereferences `_scrollBar`, which `ResetScrollbarPosition` guards against being null (:130).
- Controls/ContextMenuControl.cs:302-310 — a `ContextMenuItem` with sub-entries constructs a whole `ContextMenuShowMenu` gump in its constructor and adds it as a child of its parent gump; recursion depth is unbounded.
- Controls/ContextMenuControl.cs:365 — `RootParent?.Dispose()` from inside `OnMouseUp`, after which `_selectedPic.IsVisible` is still touched (:370).
- Controls/Button.cs:257 — `_fontTexture[_entered ? 1 : 0]` is indexed whenever `_caption` is non-empty, but `_fontTexture[1]` is only created when `hoverHue != ushort.MaxValue` (:98).
- Controls/Checkbox.cs:64-68 — the constructor calls `Dispose()` and returns early when the gump art is missing, leaving `_text` null; `Draw` (:131) and `Text` (:107) dereference it.
- Controls/StaticPic.cs:95 — `Graphic` setter disposes the control when the art has no texture, so assigning a bad graphic silently destroys the control mid-frame.
- Controls/GumpPicBase.cs (GumpPic.cs:65) and GumpPicTiled.cs:91 — same self-dispose-on-missing-texture pattern in a property setter.
- Controls/AnimationDisplay.cs:45 — result of `GetAnimationFrames` discarded (only `hue2` used); `_lastFrame` is a `ushort` that increments forever and is only wrapped in `Draw` (:86), so `Update` can outrun `Draw`.
- Controls/ColorSelectorControl.cs:240-256 — `DrawSliderTrack` issues `width/2` separate `batcher.Draw` calls per slider, 4 sliders, every frame.
- Controls/ColorSelectorControl.cs:459-527 — `_updatingFromInputs` guards are set *inside* the handler after the early-out check, and `UpdateColor()` → `UpdateInputFields()` re-enters while the flag is set, so the input text is not refreshed for the field being typed in (intentional) but also not for the others.
- Controls/TTFTextInputField.cs:863-868 — `DrawSelection` walks `_rendererText.RTL.Lines[...]` with `while` loops that have no bound against `Lines.Count`.
- Controls/TTFTextInputField.cs:2 — `using System.Windows.Forms;` in a control file (ties the build to the Windows reference assemblies noted in CLAUDE.md).
- Gumps/BaseOptionsGump.cs:1761/1767 — every `ModernButton`, `CheckboxWithLabel`, `SliderWithLabel`, `ComboBoxWithLabel`, `InputFieldWithLabel`, `ModernColorPickerWithLabel` subscribes to the **static** `SearchValueChanged` in its ctor; leaking one control leaks it permanently, and `SearchText` (:21) is shared by all options gumps.
- Gumps/BaseOptionsGump.cs:272-322 — `PositionHelper` is a static layout cursor; two options gumps built concurrently (or a rebuild during a build) interleave their positions.
- Gumps/BaseOptionsGump.cs:98-99 — `MainContent.RightArea.GetScrollBar.Dispose(); ... = null;` then `ScrollArea.Draw` (:1975) and `CalculateScrollBarMaxValue` (:2039) null-check, but `Clear()` (:2029) and `CalculateScrollBarMaxValue`'s child loop start at index 1 assuming the scrollbar occupies index 0.
- Gumps/BaseOptionsGump.cs:2008-2011 — `ScrollArea.OnMouseEnter` steals `UIManager.KeyboardFocusControl` ("Dirty fix for mouse wheel macros").
- Gumps/BaseOptionsGump.cs:2320-2324 — `Combobox.SelectedIndex` getter returns `_originalIndices[0]` when `_selectedIndex <= -1`, and the setter assigns `displayIndex = _originalIndices[0]` (an original index) when `value <= -1`, mixing the two index spaces.
- Gumps/ModernOptionsGump.cs:1292 — `b.IsSelected = true;` after the macro loop; `b` is null if the player has no macros.
- Gumps/ModernOptionsGump.cs:2572, :2653 — `ButtonParameter = page + 1 + content.LeftArea.Children.Count` — page ids derived from a mutating child count, so deleting an entry makes later ids collide.
- Gumps/ModernOptionsGump.cs:2595 — the nameplate "Delete entry" button reuses `page = (int)PAGE.Macros + 1001`, the macros delete page id.
- Gumps/ModernOptionsGump.cs:4009, :4029 — `locations.Count - 1` / `sameServerLocations.Count - 1` shown and used with no guard against an empty list.
- Gumps/ModernOptionsGump.cs:2758-2772 and :4315-4329 — adding/removing a cooldown condition disposes the live `ModernOptionsGump` and constructs a replacement from inside the button's `MouseUp` handler.
- Gumps/ModernOptionsGump.cs:4405 — `int.Parse(_cooldown.Text)` unguarded on Save.
- Gumps/TooltipConfigGump.cs:332-339 — `SaveWithDelay` spawns an unbounded number of `Task`s (one per keystroke), each sleeping 1500 ms then posting the *captured* save action; the `currentValue` parameter is never used to debounce.
- Gumps/MacroButtonEditorGump.cs:299-300 — `_previewArea.Clear(); _previewArea.Children.Clear();` then adds a new preview.
- Controls/MacroControl.cs:245-246 and ModernOptionsGump.cs:4857-4858 — `_databox.Clear(); _databox.Children.Clear();` — dispose-then-drop.
- Controls/MacroControl.cs:637-640 / ModernOptionsGump.cs:5279-5282 — `for (int i = 2; i < Children.Count; i++) Children[i]?.Dispose();` disposes while `Children.Count` is re-read each iteration and disposal may remove entries.
- Gumps/ModernBookGump.cs:549-550 — `_sb`/`_handler` are static scratch buffers shared by all open books; `OnTextChanged` (:795) rewrites them.
- Gumps/ModernBookGump.cs:436-444 and :496-507 — `Draw` mutates `_bookPage._caretPage`/`_focusPage` and calls `SetActivePage`, which can send book packets from inside the draw pass.
- Gumps/ModernBookGump.cs:352 — `text[l] = BookLines[x]` indexes `BookLines` by `(i-1)*8 + n` with no bounds check against the array length.
- Gumps/TradingGump.cs:264-272 — the "his box" clamp loop uses `_myBox.Width`/`_myBox.Height` instead of `_hisBox`.
- Gumps/TradingGump.cs:186-189 — `foreach (Control v in _myBox.Children) v.Dispose();` disposes while enumerating the children collection.
- Gumps/ChatGump.cs:283-288 — `foreach (ChannelListItemControl control in _channelList) control.Dispose();` then `_channelList.Clear()`; the disposed controls remain in `_databox.Children` until the sweep.
- Gumps/InfoBarGump.cs:185 — `_data.X = _label.IsVisible ? _label.Width + 3 : _pic.Width;` — `_pic` is only assigned when the label starts with `\` and parses; otherwise it is null while `_label.IsVisible` is true, so the ternary is safe only by that coupling.
- Gumps/InfoBarGump.cs:264 — `_data.ScreenCoordinateY + Parent.Height - 2` dereferences `Parent` in `Draw`.
- Gumps/DurabilityGump.cs:215-218 — `int.TryParse(xml.GetAttribute(...), out X)` writes directly into the control's `X`/`Y`/`Width`/`Height` via `out`, bypassing their setters.
- Gumps/PartyGump.cs:69-75 — `_healthBars[i]` and `World.Party.Members[i]` are checked, but the bar was created for whatever serial occupied slot `i` at build time; a party reshuffle without `UpdateContents` writes another member's HP into it.
- Gumps/PopupMenuGump.cs:53 — `Dispose()` inside the constructor, then the constructor continues to `return` — subsequent field access on a disposed gump is avoided only by that early return.
- Gumps/PopupMenuGump.cs:84-93 — `FontsLoader.Instance.SetUseHTML(true, h)` is set before the `Label` is created and reset after — a global loader mode toggled during control construction.
- Gumps/ArtBrowserGump.cs:112-123 — `while (count < maxEntries)` with `index++` and no upper bound against the art count; `GetArt(index)` is called for every slot every page build.
- Gumps/GridContainer.cs:1046 — `ContainerManager.Get(container.Graphic).Bounds` with no null guard.
- Gumps/GridContainer.cs:179 — `IsVisible = false; Dispose();` inside the constructor for the skip-empty-corpse case, but construction continues past it.
- Controls/ScrollFlag.cs:66-69 — constructor calls `Dispose()` and returns when the flag gump is missing, leaving `_rectUpButton`/`_rectDownButton` unset.
- Gumps/Gump.cs:83-95 — the alpha-scroll loop mutates `c.Alpha` for every child but only clamps on the way up, not down (`c.Alpha -= 0.02f` can go negative).
- Gumps/Gump.cs:139-144 — `Dispose()` sets `it.Opened = false` for `World.Items.Get(LocalSerial)`; for gumps whose `LocalSerial` is not an item (e.g. `RacialAbilityButton` uses `7000 + graphic`, RacialAbilityButton.cs:47) this can hit an unrelated item.

## Fork deltas (TazUO / Holiday additions vs stock ClassicUO)

Whole files that do not exist upstream in ClassicUO (no BSD licence header, TazUO naming, wiki links):
- `Controls/TextBox.cs` (FontStashSharp RTL + pooling), `Controls/TTFTextInputField.cs`, `Controls/ModernScrollArea.cs`, `Controls/NineSliceControl.cs`, `Controls/SimpleBorder.cs`, `Controls/ResizableStaticPic.cs`, `Controls/AnimationDisplay.cs`, `Controls/HttpClickableLink.cs`, `Controls/HBoxContainer.cs`, `Controls/ColorSelectorControl.cs`, `Controls/FadingLabel.cs`.
- `Gumps/NineSliceGump.cs`, `Gumps/BaseOptionsGump.cs`, `Gumps/ModernOptionsGump.cs`, `Gumps/GridContainer.cs`, `Gumps/ResizableJournal.cs`, `Gumps/ModernShopGump.cs`, `Gumps/CoolDownBar.cs`, `Gumps/DurabilityGump.cs`, `Gumps/SkillProgressBar.cs`, `Gumps/NearbyItems.cs`, `Gumps/ArtBrowserGump.cs`, `Gumps/CommandsGump.cs`, `Gumps/TooltipConfigGump.cs`, `Gumps/MultipleToolTipGump.cs`, `Gumps/RaceChangeGump.cs`, `Gumps/IgnoreManagerGump.cs`, `NearbyLootGump.cs`.

Modifications to stock files
- `Gumps/Gump.cs` — `IsLocked`/`CanBeLocked`/`OnLockedChanged` (:99-119), `AlphaOffset` + alt-scroll opacity (:71-97), `CenterXInViewPort`/`CenterYInViewPort` and `GlobalScaling` awareness (:169-231), `PacketGumpText` (:59), controller left-trigger lock (:152).
- `Gumps/HealthBarGump.cs` — `HealthBarGumpCustom` (Syrupz credit block :1471-1490), `UISettings`-backed `Settings` classes, `IsLastAttackBar`/`LastAttackBar`, per-bar `IsLocked`, distance-coloured borders (:740-790), `DisableAutoFollowAlt`.
- `Gumps/StatusGump.cs` — `StatusGumpModern.Settings` (`UISettings` JSON) and `StatusGumpOutlands`.
- `Gumps/SystemChatControl.cs` — `ChatMode.ServUOCommand`/`PolCommand` (:68-69, :497-511, :975-989), TTF `ChatLineTime` with duplicate collapsing `[xN]` (:1020-1026), TAB autocomplete via `TextHistoryManager` (:641-658), `DisableSystemChat`.
- `Gumps/AnchorableGump.cs` — `HoldAltToMoveGumps`, `CloseAllAnchoredGumpsInGroupWithRightClick`.
- `Gumps/BuffGump.cs` — `BuffBarTime` timer text.
- `Gumps/PartyGump.cs` — rebased on `NineSliceGump` with TTF labels and `SimpleProgressBar` HP bars (upstream is a `GumpPic` 0x0819 layout).
- `Gumps/WorldMapGump.cs` — zones (`*.zones.json` + `ZonesJsonContext`), `following`/`FollowMobile`, grid overlay, mouse coordinates, positional targeting, Ctrl+right-click pathfinding, corpse marker, multi-path marker/icon directories, PNG/JPG icons, static shared map texture/buffers.
- `Gumps/MacroButtonEditorGump.cs`, `Controls/MacroControl.cs` — macro button appearance editor and controller-button hotkeys (`Macro.ControllerButtons`).
- `Controls/HotkeyBox.cs` — controller button capture (`OnControllerButtonDown`, :147).
- `Controls/GumpPic.cs` — `EmbeddedGumpPic`, `GumpPicInPic.DrawOffset`, `InternalScale`/`Scale` usage.
- `Controls/Button.cs` — `HasBeenClicked()` for the Python API (:282), `Hue` property.
- `Controls/NiceButton.cs` — `DisplayBorder`, `AlwaysShowBackground`, `SetBackgroundHue`, `SetText`.
- `Tooltip.cs` — TTF rendering, `ToolTipOverrideData.ProcessTooltipText`, per-profile font/size/zoom/BG hue, left/centre alignment options.
- `Gumps/ShopGump.cs` — drag expander + `VendorGumpHeight`, `GumpPicTexture`.
- Holiday-specific comments/behaviour: `ResizableJournal.InitJournalEntries` skip-older-entries optimisation (:423-445), `TextBox` stroke-size staleness tracking (:59-187), `ModernOptionsGump` Experimental page additions — `LogMusicIndices`, `LogHouseDiagnostics`, `MusicEra`, `MusicMapMode`, `IgnoreServerStopMusic`, `MusicOverlay`, `KeepHouseContentsLoaded`, `ClientViewRange` slider, and the scroll-area wrapper comment at :2271-2273.
