# ui-gumps-6

Partition = every 6th file (NR%6==0) of `src/ClassicUO.Client/Game/UI/Gumps/**/*.cs`, sorted.
20 files, 15087 lines, all read in full.

All types here derive from `ClassicUO.Game.UI.Controls.Control` / `Gump` / `ResizableGump` /
`TextContainerGump` / `NineSliceGump`. The base contract that governs everything below:

- `Update()` runs **once per frame** for every live control, parent before children.
- `Draw(UltimaBatcher2D, x, y)` runs **once per frame**, `x`/`y` are *screen* coordinates.
- `UpdateContents()` runs on the frame after `RequestUpdateContents()` / `InvalidateContents = true`.
- `Save(XmlTextWriter)` / `Restore(XmlElement)` run on profile save/load only.
- `Dispose()` may be called from a constructor; **C# keeps executing the constructor afterwards**
  unless the author writes an explicit `return`. Several ctors here rely on that, several forget.

---

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `Game/UI/Gumps/BoatControl.cs` | 155 | 8-direction + stop boat steering pad; sends `BoatMovingManager.MoveRequest`. Fork addition. |
| `Game/UI/Gumps/CharCreation/CreateCharProfessionGump.cs` | 209 | Character-creation profession picker; recursive category drill-down. Stock CUO. |
| `Game/UI/Gumps/CommandsGump.cs` | 62 | Scrollable list of `CommandManager.Commands` keys. Fork addition. |
| `Game/UI/Gumps/DebugGump.cs` | 255 | FPS/zoom/pos/profiler overlay, 100 ms refresh, double-click expands. Stock + fork zoom tweak. |
| `Game/UI/Gumps/DiscordGump/DiscordMessageControl.cs` | 75 | One Discord chat line (timestamp + author + body + optional item graphic). Fork addition. |
| `Game/UI/Gumps/GridContainer.cs` | 1997 | The grid container replacement for `ContainerGump`: slot manager, search, sort, locking, multi-move, quick-loot, preview, custom borders. Largest TazUO feature in this partition. |
| `Game/UI/Gumps/GridHighLight/GridHighLightRules.cs` | 260 | Static property-name dictionaries (props/resists/negatives/slayers/rarities) + JSON load/save to `Data/GridHighlightSettings.json`. Fork addition. |
| `Game/UI/Gumps/HouseCustomizationGump.cs` | 2148 | Full house-customization UI: wall/door/floor/stair/roof/misc palettes, floor visibility, backup/commit/revert packets. Stock CUO. |
| `Game/UI/Gumps/JournalGump.cs` | 643 | Classic expandable-scroll journal, 4 type filters, dark mode, 200-entry ring. Stock + fork dark mode. |
| `Game/UI/Gumps/MacroButtonEditorGump.cs` | 341 | Editor for a `MacroButtonGump`'s label/scale/hue/graphic with live preview. Fork addition. |
| `Game/UI/Gumps/MessageBoxGump.cs` | 285 | Modal OK / OK-Cancel box, and `EntryDialog` (modal single-line text prompt). Stock. |
| `Game/UI/Gumps/ModernShopGump.cs` | 530 | TTF-rendered vendor buy/sell gump with search + per-row quantity slider. Fork addition. |
| `Game/UI/Gumps/NearbyItems.cs` | 214 | Transient grid of ground items within drag range, each with Loot/Use halves. Fork addition. |
| `Game/UI/Gumps/PopupMenuGump.cs` | 167 | Server-driven right-click context menu (`PopupMenuData`). Stock. |
| `Game/UI/Gumps/RaceChangeGump.cs` | 823 | Race-change UI with a *fake* `PlayerMobile` + fake hair/beard items and a custom paperdoll. Stock CUO (later versions). |
| `Game/UI/Gumps/ShopGump.cs` | 1216 | Classic buy/sell gump: shop list, transaction list, resizable middle, auto-repeat +/- buttons. Stock + fork resize. |
| `Game/UI/Gumps/SpellBar/SpellQuickSearch.cs` | 135 | Type-to-find-spell popup feeding a callback. Fork addition. |
| `Game/UI/Gumps/SystemChatControl.cs` | 1057 | The bottom-of-screen chat entry + fading system-message lines; chat modes, Ctrl+Q/W history, tab autocomplete. Stock + heavy fork additions. |
| `Game/UI/Gumps/TradingGump.cs` | 635 | Secure-trade window, both item boxes, gold/plat entries, accept checkboxes. Stock. |
| `Game/UI/Gumps/WorldMapGump.cs` | 3880 | World map: radar-colour map texture build, markers (.map/.csv/.xml/.usr), zones (.zones.json), party/guild/mobile dots, grid, free view, pathfinding line. Stock + large fork additions. |

---

## Types

| Type | File:line | Responsibility |
| --- | --- | --- |
| `BoatControl` | BoatControl.cs:8 | Gump; 9 `GumpPic` buttons wired to `BoatMovingManager.MoveRequest(dir, speed)`; 3 mutually-exclusive speed checkboxes. |
| `CreateCharProfessionGump` | CreateCharProfessionGump.cs:43 | Gump; grid of `ProfessionInfoGump`, `Prev` button walks back up the category tree. |
| `ProfessionInfoGump` | CreateCharProfessionGump.cs:166 | Control; one profession tile, raises `Action<ProfessionInfo> Selected` on left mouse-up. |
| `CommandsGump` | CommandsGump.cs:9 | Gump; static list built once in ctor from `CommandManager.Commands`. |
| `DebugGump` | DebugGump.cs:46 | Gump, `GumpType.Debug`, `UILayer.Over`; caches a formatted string every 100 ms and draws it with `Fonts.Bold`. |
| `DiscordMessageControl` | DiscordMessageControl.cs:9 | Control; namespace is `ClassicUO.Game.UI.Controls` despite living under `Gumps/DiscordGump/`. |
| `GridContainer` | GridContainer.cs:55 | `ResizableGump`, `GumpType.GridContainer`; owns background, search box, sort/quickdrop/menu icons, `GridScrollArea`, `GridSlotManager`. |
| `GridContainer.GridItem` | GridContainer.cs:842 | Control; one 50*scale px slot. Holds `Item _item` + `LocalSerial`; draws art itself, handles ctrl/alt/shift click semantics. |
| `GridContainer.GridSlotManager` | GridContainer.cs:1441 | Owns `Dictionary<int,GridItem> gridSlots`, `Dictionary<int,uint> itemPositions`, `List<uint> itemLocks`; rebuilds slot→item mapping. |
| `GridContainer.GridScrollArea` | GridContainer.cs:1715 | Control; hand-rolled scroll area, scrollbar is child 0, content children start at index 1. |
| `GridContainer.GridContainerPreview` | GridContainer.cs:1909 | Gump; 170x150 hover preview of first 9 items of a nested container. |
| `GridContainer.GridSortMode` / `BorderStyle` | GridContainer.cs:823 / :829 | Enums persisted into `GridContainerEntry`. |
| `GridHighlightRules` | GridHighLightRules.cs:11 | Static class; lazily loads JSON on first property access, exposes 6 `HashSet<string>`. |
| `GridHighlightRules.GridHighlightSettings` | GridHighLightRules.cs:104 | Private DTO for `System.Text.Json`. |
| `HouseCustomizationGump` | HouseCustomizationGump.cs:46 | Gump; two `DataBox` children (`_dataBox` content, `_dataBoxGUI` chrome) fully rebuilt by `Update()`. |
| `HouseCustomizationGump.ID_GUMP_CUSTOM_HOUSE` | HouseCustomizationGump.cs:2104 | Button-ID enum; `ID_GCH_ITEM_IN_LIST` is the base for per-tile IDs. |
| `JournalGump` | JournalGump.cs:48 | Gump, `GumpType.Journal`; `ExpandableScroll` + `ScrollFlag` + `RenderedTextList`. |
| `JournalGump.RenderedTextList` | JournalGump.cs:367 | Control; three parallel `Deque`s (`_entries`, `_hours`, `_text_types`) indexed in lockstep. |
| `MacroButtonEditorGump` | MacroButtonEditorGump.cs:51 | Gump, `GumpType.MacroButtonEditor`; mutates the live `Macro` object directly as controls change. |
| `MessageBoxGump` | MessageBoxGump.cs:48 | Modal gump, takes `Action<bool>`; Enter == OK. |
| `EntryDialog` | MessageBoxGump.cs:178 | Modal gump, takes `Action<string>`; used by WorldMapGump goto/marker prompts. |
| `ModernShopGump` | ModernShopGump.cs:15 | Gump; `List<ShopItem> shopItems` is the master list, `scrollArea.Children` is the filtered view. |
| `ModernShopGump.ShopItem` | ModernShopGump.cs:193 | Control; two overlapping `Area`s (`itemInfo` / `purchaseSell`) toggled by double-click. |
| `ModernShopGump.ShopItem.BuySellButton` | ModernShopGump.cs:470 | Control; label recomputed from quantity*price. |
| `NearbyItems` | NearbyItems.cs:15 | Gump; **static singleton** `NearbyItemGump`. Content built once in ctor. |
| `NearbyItemDisplay` | NearbyItems.cs:104 | Control; caches `SpriteInfo`, hue vector and art bounds at construction. |
| `PopupMenuGump` | PopupMenuGump.cs:42 | Gump; `_selectedItem` set by hover, sent on any left mouse-up. |
| `RaceChangeGump` | RaceChangeGump.cs:14 | Gump; builds a fake `PlayerMobile(0)` + fake hair/beard `Item`s registered in `World`. |
| `RaceChangeGump.CustomColorPicker` | RaceChangeGump.cs:530 | Control; spawns a modal `ColorPickerBox` on click. |
| `RaceChangeGump.CustomPaperDollGump` | RaceChangeGump.cs:661 | `PaperDollInteractable` subclass that rebuilds itself from the fake mobile. |
| `ShopGump` | ShopGump.cs:50 | Gump; `_shopItems` and `_transactionItems` keyed by serial; middle strip drag-resizable. |
| `ShopGump.ShopItem` | ShopGump.cs:658 | Control; `Amount` is stored as the *text of a Label* and parsed back with `int.Parse`. |
| `ShopGump.TransactionItem` | ShopGump.cs:907 | Control; same Label-as-storage pattern; +/- buttons auto-repeat via `MouseOver` + `Time.Ticks`. |
| `ShopGump.ResizePicLine` / `GumpPicTexture` | ShopGump.cs:1107 / :1157 | Drawing helpers that re-fetch gump art every frame. |
| `SpellQuickSearch` | SpellQuickSearch.cs:11 | `NineSliceGump`; TTF input + one `SpellDisplay`. |
| `SpellQuickSearch.SpellDisplay` | SpellQuickSearch.cs:71 | Control; holds a `SpellDefinition` and invokes the caller's action. |
| `ChatMode` | SystemChatControl.cs:54 | Enum incl. fork-only `ServUOCommand`, `PolCommand`. |
| `SystemChatControl` | SystemChatControl.cs:72 | Control (not Gump); owns the `StbTextBox` used for all in-game typing. |
| `SystemChatControl.ChatLineTime` | SystemChatControl.cs:1000 | Non-Control wrapper around a pooled `TextBox` with an expiry tick and a `[xN]` duplicate counter. |
| `TradingGump` | TradingGump.cs:47 | `TextContainerGump`; `_myBox`/`_hisBox` `DataBox`es repopulated by `UpdateContents()`. |
| `WorldMapGump` | WorldMapGump.cs:67 | `ResizableGump`, `GumpType.WorldMap`; everything is drawn immediate-mode in `Draw`, no child controls except `_northIcon`. |
| `WorldMapGump.WMapMarker` / `WMapMarkerFile` | WorldMapGump.cs:889 / :902 | Plain data; `MarkerIcon` is a raw `Texture2D` reference into the static icon dict. |
| `WorldMapGump.CurLoader` | WorldMapGump.cs:911 | `unsafe` .ico/.cur → `Texture2D` decoder via SDL surfaces. |
| `WorldMapGump.Zone` / `ZoneSet` / `ZoneSets` | WorldMapGump.cs:1695 / :1730 / :1757 | Polygon zone overlays loaded from `*.zones.json`. |
| `ZonesFile` / `ZonesFileZoneData` / `ZonesJsonContext` | WorldMapGump.cs:1689 / :1680 / :65 | JSON DTOs + source-generated serializer context (required by repo convention). |

---

## State

Static / global state owned in this partition:

- `DebugGump._last_position` — DebugGump.cs:55. Static `Point`, written in `OnDragEnd`/`OnMove`, read by every new instance.
- `GridContainer.lastX/lastY/lastCorpseX/lastCorpseY` — GridContainer.cs:63. Static ints; last-closed grid position, one pair for corpses one for everything else, shared by all containers.
- `GridContainer.borderWidth` — GridContainer.cs:65. **Static mutable int**, reassigned by any instance's `BuildBorder()` (lines 763, 786) and read by every other instance's layout math.
- `GridContainer.gridItemSize` — GridContainer.cs:64. Static computed property off `ProfileManager.CurrentProfile.GridContainersScale`, no null guard.
- `GridContainer.GridItem._toggledThisAltDrag` / `_altDragActive` — GridContainer.cs:856-857. **Static** per-gesture set/flag shared by every grid slot in every open container.
- `GridHighlightRules._loaded` — GridHighLightRules.cs:14. One-shot load latch; never reset on profile switch.
- `GridHighlightRules.Default*` sets — GridHighLightRules.cs:116/179/186/193/206/238. Static readonly fallbacks.
- `World.CustomHouseManager` — assigned HouseCustomizationGump.cs:64, cleared :2097. Global pointer owned by the gump's lifetime.
- `SystemChatControl._messageHistory` — SystemChatControl.cs:77. **Static unbounded** `List<Tuple<ChatMode,string>>`; never trimmed, survives gump disposal and reconnects.
- `SystemChatControl._messageHistoryIndex` — SystemChatControl.cs:78. Static cursor into that list.
- `SystemChatControl.ChatLineTime.TextBoxOptions` — SystemChatControl.cs:1004. Static shared `RTLOptions` instance (Width 320).
- `NearbyItems.NearbyItemGump` — NearbyItems.cs:18. Static singleton; ctor disposes the previous one (:26), `Dispose` nulls it (:100).
- `PopupMenuGump.CloseNext` — PopupMenuGump.cs:44. Static `uint` sentinel (`uint.MaxValue` = unset) consumed and reset by the next ctor.
- `WorldMapGump._last_position` — WorldMapGump.cs:69. Static.
- `WorldMapGump.following` — WorldMapGump.cs:90. **Static `Mobile` reference**; set from the context menu and never cleared when the mobile leaves view or is removed from `World.Mobiles`.
- `WorldMapGump._mapTexture` / `_pixelBuffer` / `_zBuffer` — WorldMapGump.cs:92-94. Static; sized to the largest map, allocated once (:1517-1519), never disposed by `Dispose()`.
- `WorldMapGump._markerFiles` — WorldMapGump.cs:98. `public static readonly List<WMapMarkerFile>`; also mutated by `MarkersManagerGump`/`UserMarkersGump` outside this partition.
- `WorldMapGump._markerIcons` — WorldMapGump.cs:102. `public static readonly Dictionary<string,Texture2D>`; disposed+cleared wholesale by `LoadMarkers` (:1840-1853).
- `WorldMapGump._colorMap` — WorldMapGump.cs:3795. Static name→`Color` table used by both markers and zones.
- `WorldMapGump.UserMarkersFilePath` — WorldMapGump.cs:82. Static path `Data/Client/userMarkers.usr`.

Per-instance mutable state worth naming:

- `GridContainer.gridContainerEntry` (GridContainer.cs:118) — the persisted `GridContainerEntry` from `GridContainerSaveData`; slot→serial map and lock flags live here across sessions.
- `GridSlotManager.itemPositions` (GridContainer.cs:1448) — slot index → item serial. Serials are reused by the server; a stale slot claim survives until the next rebuild.
- `GridSlotManager.amount` (GridContainer.cs:1446) — slot count, starts 125, grows to `containerContents.Count`, **never shrinks**.
- `ShopGump._shopItems` / `_transactionItems` (ShopGump.cs:66/72) — keyed by item serial.
- `ModernShopGump.shopItems` / `itemY` (ModernShopGump.cs:30/32).
- `WorldMapGump._center` / `_scroll` / `_lastScroll` / `_mouseCenter` (WorldMapGump.cs:70) — the free-view camera; `_center` is overwritten every frame from the player/followed mobile unless `_isScrolling || _freeView`.

---

## Timing

**Per frame (`Update`)**
- `GridContainer.Update` (:619) — null/destroyed container check, corpse-distance-> 3 auto-close (:634), size/scale delta re-layout (:640), backpack position sync to profile (:666), `SelectedObject.Object`/`CorpseObject` set while hovered (:669-675).
- `GridContainer.GridItem.Update` (:1406) — alt+drag multi-select gesture state machine on the **static** flags.
- `GridContainer.GridScrollArea.Update` (:1761) — recomputes scrollbar max by walking all children, then writes `UpdateOffset` on every child (:1902).
- `ShopGump.Update` (:400) — drag-resize from `Mouse.LDragOffset`, self-dispose when `_shopItems.Count == 0` (:457), scroll repeat, total recompute, player-gold label refresh.
- `ModernShopGump.Update` (:115) — drag-resize from `Mouse.LDragOffset`, writes `ProfileManager.CurrentProfile.VendorGumpHeight` every frame while dragging (:126).
- `SystemChatControl.Update` (:406) — expires chat lines, then re-parses `TextBoxControl.Text[0]` **every frame** to pick a `ChatMode` (:424-518).
- `JournalGump.Update` (:284) — `WantUpdateSize = true` unconditionally, re-lays out the 4 filter checkboxes.
- `NearbyItems.Update` (:37) — disposes if the player's X/Y changed **or** 30000 ms since open.
- `WorldMapGump.Update` (:657) — `Load()` if `World.MapIndex` changed, then `World.WMapManager.RequestServerPartyGuildInfo()` every frame.
- `RaceChangeGump.CustomPaperDollGump.Update` (:812) — rebuilds UI only when `requestUpdate`; **does not call `base.Update()`**.
- `GridContainerPreview.Update` (:1979) — disposes when the container is gone or >3 tiles away.

**Throttled**
- `DebugGump.Update` — recomputes its string every **100 ms** (`_timeToUpdate = Time.Ticks + 100`, DebugGump.cs:112-114).
- `ShopGump.ProcessListScroll` — `SCROLL_DELAY = 60` ms between scroll steps while a scroll hitbox is held (ShopGump.cs:62, :495).
- `ShopGump.TransactionItem` +/- auto-repeat — 500 ms initial delay then `45 - _StepChanger` ms per tick, accelerating by 2 ms every 3 steps (ShopGump.cs:984-991, 1006).
- `SystemChatControl.ChatLineTime` — expires at `Time.Ticks + Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT` (:1011).
- `GridContainer.GridItem.AddText` — `SimpleTimedTextGump` lives 2 s (`TimeSpan.FromSeconds(2)`, :907).
- `NearbyItems` — hard 30 s lifetime (:41).

**Per frame (`Draw`)**
- `WorldMapGump.Draw`/`DrawAll` (:2329/:2447) — per frame walks every zone, `World.HouseManager.Houses`, every marker in every non-hidden marker file, `World.Mobiles.Values`, `World.WMapManager.Entities.Values`, and the 10 party slots. No spatial culling before the per-item rotate/clip test.
- `GridContainer.GridItem.Draw` (:1198) — while Ctrl is held and the mouse is over a wearable, **constructs `CustomToolTip`s and calls `UIManager.Add(...)` from inside Draw** (:1232-1254).
- `JournalGump.RenderedTextList.Draw` (:391) — walks the whole 200-entry deque each frame.
- `SystemChatControl.Draw` (:523) — walks the line list backwards and **removes disposed nodes during the draw pass** (:535).

**Per packet / event**
- `JournalGump.AddJournalEntry` — bound to `EventSink.JournalEntryAdded` (subscribe :230, unsubscribe :280).
- `SystemChatControl.ChatOnMessageReceived` — bound to `EventSink.MessageReceived` (subscribe :147, unsubscribe :333).
- `ShopGump.AddItem` / `SetNameTo`, `ModernShopGump.AddItem` / `SetNameTo` — called from vendor packet handlers, one call per item.
- `TradingGump.UpdateContents` — via `RequestUpdateContents()` from trade packets.
- `GridContainer.UpdateContents` (:557) / `HandleObjectMessage` (:718) — container-content and object-message packets.
- `HouseCustomizationGump.OnButtonClick` — sends `Send_CustomHouseGoToFloor/Backup/Restore/Sync/Clear/Commit/Revert` (:1945-2054).
- `PopupMenuGump` — created per server popup packet, sends `GameActions.ResponsePopupMenu` on left-up (:163).

**On load / on demand**
- `WorldMapGump.Load()` (:1476) — `Task.Run` **background thread**; fills `_pixelBuffer`/`_zBuffer` from map+statics blocks and calls `_mapTexture.SetDataPointerEXT` off the frame thread; prints to chat from that thread (:1674).
- `WorldMapGump.LoadMarkers()` (:1830) / `LoadZones()` (:1801) — synchronous file I/O on the frame thread; called from the ctor and from context-menu "Reload".
- `GridHighlightRules.LoadGridHighlightConfiguration` (:57) — first touch of any rules property; synchronous file read.
- `GridContainer.Restore` (:461) — issues `GameActions.DoubleClickQueued(LocalSerial)` to make the server re-send the container on profile load.

---

## Inbound

- `UIManager.Add(...)` / `UIManager.GetGump<T>()` — the universal entry; every gump here is created by a packet handler, a macro, or a context-menu action elsewhere.
- **GridContainer**: constructed by the container-open packet path with `(serial, originalGraphic, useGridStyle)`; `UpdateAllGridContainers()` (:712) is a static fan-out called from the options gump; `HandleObjectMessage(Entity,string,ushort)` (:718) from message handling; `GridContainerEntry`/`GridContainerSaveData` from the save layer; `MultiItemMoveGump` reads back `SelectHighlight` state.
- **GridHighlightRules**: `GridHighlightMenu` and item-highlight evaluation read the six static `HashSet<string>` properties; `Item.MatchesHighlightData`/`HighlightColor` are consumed by `GridItem.Draw` (:1284).
- **HouseCustomizationGump**: opened by the custom-house packet; `World.CustomHouseManager` is read by the game scene while building.
- **JournalGump / SystemChatControl**: `EventSink.JournalEntryAdded` and `EventSink.MessageReceived` push into them; `UIManager.SystemChat` is the global handle other code uses (`UIManager.SystemChat?.SetFocus()` is called from GridContainer.cs:227).
- **ShopGump / ModernShopGump**: vendor packet handler calls `AddItem(...)` then `SetNameTo(...)` as OPL names arrive.
- **TradingGump**: trade packets set `Gold`/`Platinum`/`HisGold`/`HisPlatinum`/`ImAccepting`/`HeIsAccepting` and call `RequestUpdateContents()`; `MultiItemMoveGump.OnTradeWindowTarget(ID1)` is invoked from its `OnMouseUp` (:364).
- **WorldMapGump**: `FollowMobile(Mobile)` (:534), `AddUserMarker`/`RemoveUserMarker` (:2185/:2237), `GoToMarker` (:454), `ReloadUserMarkers` (:2281), `LoadUserMarkers` (:2297), `ParseMarker` (:3759), `GetColor` (:3812) and the static `_markerFiles`/`_markerIcons` are all called from `MarkersManagerGump`, `UserMarkersGump`, and the scripting API.
- **MessageBoxGump / EntryDialog**: used as generic modal prompts across the codebase (WorldMapGump itself uses `EntryDialog` at :340 and :2130).
- **SpellQuickSearch**: constructed by the spell bar with an `Action<SpellDefinition>` callback.
- **MacroButtonEditorGump**: opened from `MacroButtonGump`'s context menu with the live `Macro`.
- **CreateCharProfessionGump**: `CharCreationGump.SetProfession` / `StepBack` (:130, :150).

## Outbound

- **Network** (`ClassicUO.Network.NetClient.Socket`): `Send_CustomHouseGoToFloor/Backup/Restore/Sync/Clear/Commit/Revert/BuildingExit` (HouseCustomizationGump), `Send_BuyRequest`/`Send_SellRequest` (ShopGump :630/:634, ModernShopGump :275/:280/:328/:332), `Send_TradeUpdateGold` (TradingGump :558), `Send_ChangeRaceRequest` (RaceChangeGump :440/:450), `Send_ASCIIPromptResponse`/`Send_UnicodePromptResponse`/`Send_ChatMessageCommand`/`Send_PartyDecline` (SystemChatControl :630/:634/:971/:861).
- **GameActions**: `DropItem`, `PickUp`, `GrabItem`, `DoubleClick`, `DoubleClickQueued`, `SingleClick`, `Print`, `Say`, `SayParty`, `AcceptTrade`, `CancelTrade`, `RequestParty*`, `ResponsePopupMenu`.
- **Managers**: `TargetManager` (Target/CancelTarget/SetTargeting/IsTargeting/TargetingState), `UIManager`, `ProfileManager.CurrentProfile` (read *and written* from `Update`), `AutoLootManager.Instance.AddAutoLootEntry` (GridContainer.cs:1095), `DelayedObjectClickManager.Set`, `MessageManager.HandleMessage`/`PromptData`, `CommandManager.Execute`/`Commands`, `IgnoreManager.IgnoredCharsList`, `TextHistoryManager` (autocomplete + command history), `ChatManager.ChatIsEnabled`, `BoatMovingManager.MoveRequest`, `DiscordManager.GetUserhue`, `ContainerManager.CalculateContainerPosition`/`Get`, `Pathfinder.WalkTo`/`AutoWalking`/`EndPoint`, `World.WMapManager` (`SetEnable`, `GetEntity`, `Entities`, `_corpse`, `RequestServerPartyGuildInfo`).
- **World**: `World.Items`/`World.Mobiles`/`World.Get`/`World.OPL`/`World.Party`/`World.HouseManager`/`World.Map`/`World.MapIndex`/`World.Player`; `World.GetOrCreateItem` and `World.RemoveItem` from RaceChangeGump (:502, :332/:462/:466).
- **Assets/Renderer**: `Client.Game.Arts.GetArt`/`GetRealArtBounds`, `Client.Game.Gumps.GetGump`, `Client.Game.Animations`, `HuesLoader`, `ClilocLoader`, `FontsLoader`, `TileDataLoader`, `MapLoader`, `TrueTypeLoader.EMBEDDED_FONT`, `SolidColorTextureCache`, `ShaderHueTranslator`, `UltimaBatcher2D`, `Fonts.*`.
- **Other gumps**: `ContainerGump`, `MultiItemMoveGump`, `InspectorGump`, `MacroButtonGump`, `MarkersManagerGump`, `UserMarkersGump`, `SelectableItemListGump`, `SimpleTimedTextGump`, `CustomToolTip`/`MultipleToolTipGump`, `GridHighlightMenu`, `ColorPickerBox`, `PaperDollInteractable`.
- **Scene**: `Client.Game.GetScene<GameScene>()` for `MoveItemQueue.EnqueueQuick`, `Macros`, `Camera`.
- **File system**: `Data/GridHighlightSettings.json`; `Data/Client/userMarkers.usr`; `Data/Client|<Server>|<UO dir>/MapMarkers/*.{map,csv,xml,zones.json}` and `MapIcons/*.{cur,ico,png,jpg}`.

---

## Hazards

Factual observations with line numbers. No causal claims.

**GridContainer.cs**
- `:142` `World.Player.FindItemByLayer(Layer.Backpack).Serial` dereferenced with no null check, in the constructor. Same pattern at `:252` (`.DisplayedGraphic`) and `:263`, `:596`, `:987`.
- `:176-180` calls `Dispose()` for the skip-empty-corpse case but does not `return`; the constructor keeps building controls on a disposed gump.
- `:63` / `:65` `lastX/lastY/lastCorpseX/lastCorpseY` and `borderWidth` are static; `BuildBorder()` writes `borderWidth` (:763, :786) and every other open GridContainer's layout reads it.
- `:169-170` `X = isCorpse ? lastCorpseX : lastX = lastPos.X;` — the assignment to `lastX` only happens on the non-corpse branch, and `lastCorpseX`/`lastCorpseY` are never seeded from `lastPos`.
- `:640-642` compares `lastGridItemScale` (a float set from `GridContainersScale / 100f` at `:81`) against `gridItemSize` (an int pixel size), then assigns the int into the float field.
- `:600-609` walks `currentContainer.Items` (`LinkedObject` chain) while calling `UIManager.GetGump<GridContainer>(child)?.Dispose()` inside the loop.
- `:613` skips `UpdateSaveDataEntry` when `isCorpse`, so corpse gump position/size changes are dropped.
- `:684` `GetContainerName()` calls `gridSlotManager.UpdateItems()`, which re-enumerates and re-sorts the whole container; `GetContainerName` is called from `UpdateItems()` (:544) on every content invalidation.
- `:714` `UIManager.Gumps.OfType<GridContainer>()` is enumerated while each `OptionsUpdated()` mutates that gump.
- `:856-857` `_toggledThisAltDrag` and `_altDragActive` are `static` on `GridItem`; `:1414-1436` every slot in every open container runs the same gesture state machine against them, and `:1420` clears the set whenever any one slot observes the gesture start.
- `:968` inside `SetGridItem`, `Y = Height - count.Height;` sets the **GridItem's** `Y`, not `count.Y`; `count` is later drawn at `x + count.X, y + count.Y` (`:1401`).
- `:1046` `ContainerManager.Get(container.Graphic).Bounds` — `container` is the ctor-captured `Item`, not re-fetched, and is not null-checked.
- `:1165-1168` disposes *every* `GridContainerPreview` in the UI, not just this slot's.
- `:1201-1254` `Draw()` allocates `CustomToolTip`s and calls `UIManager.Add(new MultipleToolTipGump(...))` during the draw pass; `:1237-1243` additionally constructs two `ItemPropertiesData` and prints to chat when `CUOEnviroment.Debug`.
- `:1486-1494` `AddLockedItemSlot` runs `ItemPositions.Values.Contains(serial)` then `ItemPositions.First(...)` — O(n) twice — and is called once per unplaced item from the loop at `:1532-1542`.
- `:1520` `if (spot.Key < gridSlots.Count)` uses `Count` as an upper bound on a dictionary keyed by arbitrary ints.
- `:1701-1711` `amount` only ever grows; `gridSlots` entries and their `GridItem` controls are added to `area` and never removed when the container shrinks.
- `:1544` `GridItems.Clear()` then repopulated only for slots with a non-empty `searchText` (`:1549`), so `FindItem(serial)` (used by `HandleObjectMessage`, `:721`) returns null unless a search is active.
- `:1803` / `:1841` / `:1854` / `:1902` `GridScrollArea` loops start at index 1 on `Children`; `Clear()` at `:1839-1845` disposes children while indexing forward through the same live list.
- `:1918` `GridContainerPreview` ctor calls `Dispose()` and returns, but `:1979` `Update()` reads `_container` before `base.Update()`.

**HouseCustomizationGump.cs**
- `:231` `public new void Update()` hides `Gump.Update()`; the base per-frame `Update` still runs and does not call this one. This method is invoked explicitly from the ctor (`:228`) and from `OnButtonClick`.
- `:266` the eyedropper `Button` is added with `Add(button)` (to the gump) while every other button in the same method goes to `_dataBoxGUI`; `_dataBoxGUI.Clear()` at `:234` therefore never removes it — one new Button accumulates on the gump per `Update()` call.
- `:275` / `:327` / `:383` / `:432` / `:486` index `FloorVisionState[...]` through `associateGraphicTable[...]` with no bounds check on the vision-state value.
- `:485-487` the `FloorCount != 4` branch uses `floorVisionGraphic2` with `FloorVisionState[2]` while the graphic arrays `floorVisionGraphic1`/`floorVisionGraphic3` are declared (`:268`, `:270`) and `floorVisionGraphic3` is never used.
- `:802-805` (and the same pattern at `:862`, `:970`, `:1111`, `:1185`, `:1244`, `:1311`, `:1430`, `:1490`) the `MouseUp` lambda captures the loop-local `pic` and reads `pic.LocalSerial` at click time.
- `:1790-1793` `if (index > 10) { combinedStairs = true; index -= 10; }` while the encoding at `:1180` uses `+ combinedStair` where `combinedStair` is `0` or `10`.
- `:2095-2101` `Dispose()` unconditionally sends `Send_CustomHouseBuildingExit()` and `TargetManager.CancelTarget()`.

**JournalGump.cs**
- `:335-336` `Restore` uses `int.Parse` / `bool.Parse` on XML attributes with no `TryParse`.
- `:261-264` `IsMinimized` setter writes `IsVisible` on **all** children, then re-shows only `_gumpPic`; any child added later while minimized stays visible.
- `:401-405` `Draw` indexes `_entries[i]`, `_hours[i]`, `_text_types[i]` in lockstep; `CalculateScrollBarMaxValue` (`:524`) guards `i < _text_types.Count` but `Draw` does not.
- `:560-567` trims to 199 entries by removing from all three deques; `AddEntry` then does `_scrollBar.MaxValue += rtext.Height` (`:594`) without subtracting the removed entry's height.

**DebugGump.cs**
- `:152-153` `scene.Camera` is dereferenced before the `scene != null` test on the next line (`:155`).
- `:55` `_last_position` is static and initialised to `(-1,-1)`; `:71-72` treat any value `<= 0` as unset.

**SystemChatControl.cs**
- `:77` `_messageHistory` is static and unbounded — every line ever sent, for the lifetime of the process.
- `:440` `TextBoxControl.Text.Substring(1, pos)` uses `pos` as a *length* starting at index 1 while `pos` was computed as an *index* (`:433-438`); when `pos == Text.Length - 1` the length exceeds the remaining string.
- `:424-518` the mode-detection switch runs in `Update()`, i.e. every frame while typing, and mutates `TextBoxControl` text (`:452`, `:502`, `:510`).
- `:348-353` `AppendChatModePrefix` computes `idx` from `TextBoxControl.Text.IndexOf(text)` and then substrings by `Text.Length - labelText.Length - 1`, unrelated to `idx`'s bounds.
- `:527-536` `Draw` mutates `_textEntries` (removes nodes) during the draw pass.
- `:581` / `:610` `_messageHistory[_messageHistoryIndex]` indexed after only `> -1` / `< Count - 1` checks; `:694` sets `_messageHistoryIndex = _messageHistory.Count` (one past the end).
- `:1004` `TextBoxOptions` is a single static `RTLOptions` instance shared by every chat line.

**ShopGump.cs**
- `:457-460` `Update()` disposes the gump whenever `_shopItems.Count == 0`, including before the first `AddItem` for a vendor with an empty list.
- `:567`, `:582`, `:597`, `:599` index `_shopItems[transactionItem.LocalSerial]` with the raw indexer, no `TryGetValue`.
- `:749` / `:1082` `Amount` is read back with `int.Parse(_amountLabel.Text)` — the label text is the storage.
- `:984`, `:988`, `:1037`, `:1041` `_StepChanger` / `_StepsDone` are not declared in this file; they are inherited fields shared by the two auto-repeat lambdas in the *same* `TransactionItem` (both `MouseOver` handlers test the shared `status == 2` and the shared `t0`).
- `:609-616` `ShopItem_MouseClick` enumerates `_shopScrollArea.Children.SelectMany(o => o.Children)` while the click handler may have disposed items.

**ModernShopGump.cs**
- `:175-178` removes items straight out of `scrollArea.Children` (`Children.Remove(i)`) rather than through the control API, then re-`Add`s the survivors; the removed `ShopItem`s are not disposed and keep their parent pointer.
- `:126` writes `ProfileManager.CurrentProfile.VendorGumpHeight` on every frame of a resize drag.
- `:276`/`:281` decrement the captured local `count`, not the `Count` property (`:525`), so `Count` used by shift-double-click (`:321`) stays at the original amount.
- `:462-466` `MatchSearch` calls `data.ToLower()` without a null check on `data`.

**TradingGump.cs**
- `:186-189` and `:243-246` `foreach (Control v in _myBox.Children) v.Dispose();` — disposing a child while enumerating the parent's `Children` list.
- `:264-272` in the *his* box loop the clamp uses `_myBox.Width` / `_myBox.Height`, not `_hisBox`.
- `:371-375` `Dispose()` always calls `GameActions.CancelTrade(ID1)`, including when the gump is closed because the trade already completed.
- `:498`, `:514` `(int)entry.Tag` unboxed with no type check.
- `:630-633` `FormatAsCurrency(uint, bool useComma = true)` ignores `useComma`.

**RaceChangeGump.cs**
- `:502` `World.GetOrCreateItem(0x4000_0000 + (uint)layer)` — the fake hair/beard items occupy fixed serials in the `0x40000000` range and are inserted into the global `World.Items`.
- `:495-509` `CreateItem(int id, ...)` ignores `id` for the serial and only uses it as the graphic; the `id == 0` early-out can never fire for the two call sites (`:360`, `:363`).
- `:460-468` the fake items are removed from `World` only on the confirm button; closing with right-click (`CanCloseWithRightClick = true`, `:89`) disposes the gump and leaves them registered.
- `:812-819` `CustomPaperDollGump.Update()` overrides `Update` and never calls `base.Update()`, so no child control of the paperdoll is updated.
- `:807` `public new void RequestUpdate()` hides the base `PaperDollInteractable.RequestUpdate()`; callers holding a base-typed reference get the base implementation.
- `:326-334` walks `fakeMobile.Items` calling `World.RemoveItem((Item)first, true)` (next captured first).
- `:477` `CurrentColorOption[Layer.Invalid]` indexed in `UpdateEquipments`, which is called at `:154` — after `BuildColorOptions` (`:149`) populates it, but `CreateCharacter()` at `:152` documents that ordering requirement only in a comment (`:315-317`).
- `:491` `hair.Graphic = ...` with no null check on `hair`.

**NearbyItems.cs**
- `:97-101` `Dispose()` sets the static `NearbyItemGump = null` unconditionally; if a newer instance already replaced it (`:26`), the older one's disposal clears the newer registration.
- `:51-64` `BuildGump` enumerates `World.Items.Values` and captures each `Item` in a `NearbyItemDisplay` closure (`:130`, `:142`); the display never re-checks whether the item still exists.
- `:66-70` `Dispose()` on empty, then `return` — but the caller has already run `NearbyItemGump = this` (`:27`).
- `:107` reads `ProfileManager.CurrentProfile.GridContainersScale` (grid-container setting) to scale nearby-item art.
- `:20-21` `playerX`/`playerY` field initialisers dereference `World.Player` before the ctor body.

**PopupMenuGump.cs**
- `:51-56` on the `CloseNext` path the ctor `Dispose()`s and returns with `_data` left null; `OnMouseUp` (`:161`, `:163`) dereferences `_data`.
- `:102` `(sender as HitBox).Tag` cast to `ushort` with no null/type guard.
- `:108-135` when an item has flag `0x02` the `arrowAdded` latch stops `height`/`width` accumulating for all *subsequent* items.
- `:149-152` `FindControls<HitBox>()` resizes every hitbox in the gump to `width - 20` after layout.

**WorldMapGump.cs**
- `:1476-1678` `Load()` runs the whole map rasterisation inside `Task.Run`: it constructs a `Texture2D` (`:1517`) and calls `SetDataPointerEXT` (`:1540`, `:1666`) from a worker thread, reads `World.Map.GetIndex` (`:1554`) and raw `MapBlock*`/`StaticsBlock*` pointers, and calls `GameActions.Print` (`:1674`) off the frame thread.
- `:92-94` `_mapTexture`, `_pixelBuffer`, `_zBuffer` are static and are never released by `Dispose()` (`:819-827`).
- `:90` `following` is a static `Mobile` reference read every frame in `Draw` (`:2338-2342`); nothing clears it when that mobile is removed from `World.Mobiles`.
- `:1840-1853` `LoadMarkers` disposes every texture in the static `_markerIcons` and clears the dict, while `WMapMarker.MarkerIcon` fields in `_markerFiles` (and in any other live gump) still point at those textures; `_markerFiles.Clear()` only happens later at `:1946`.
- `:2066` inside the CSV parse loop, an empty line does `return;` — leaving `LoadMarkers` entirely (no `BuildContextMenu`, `_mapMarkersLoaded` stays false) instead of `continue`.
- `:2014` / `:2016` UOAM parsing does `line.Substring(0,1)` and `line.IndexOf(':')` with no check that a `:` exists.
- `:1978-1990` XML marker parsing uses `int.Parse` on `X`/`Y`/`Facet` and `reader.GetAttribute("Icon").ToLower()` with no null guards.
- `:2078-2084` / `:3763-3770` CSV/`.usr` parsing indexes `splits[0..6]` after only checking `splits.Length <= 1`.
- `:3579` `_lastMousePosition.Value` dereferenced in `OnMouseUp` without the null check that `:2800` performs.
- `:2528` / `:2713` `World.Map.Index` dereferenced inside `DrawAll`; `Draw` guards on `World.InGame` (`:2331`) but not on `World.Map`.
- `:2447-2834` `DrawAll` is fully immediate-mode per frame: all zones, all houses, all markers in all files, all `World.Mobiles.Values`, all `World.WMapManager.Entities.Values`, 10 party slots.
- `:1219` `LoadMapChunk` and `:1277` `LoadMapDetails` are private and never called (the live path is the inlined loop at `:1548`).
- `:1888` / `:1912` `_markerIcons.Add(...)` (not indexer) — a duplicate basename across the three icon directories throws, and the throw is caught only inside the per-file `try` (`:1890`, `:1914`).
- `:2156-2161` / `:2202-2207` marker file appends open with `FileShare.Write` and seek-to-end while `LoadUserMarkers` (`:2301`) may hold a reader.
- `:1848-1851` `File.Create(UserMarkersFilePath)` with no directory-exists guard for `Data/Client`.
- `:3005` `marker.Color == Color.Transparent` — value comparison against the "none" colour-map entry (`:3804`) is the only way a marker below zoom threshold is skipped.
- `:666-668` `Update` calls `Load()` whenever `_mapIndex != World.MapIndex`; `_mapIndex` is set synchronously at `:1478` before the task starts, so a second map change during a running load starts a second concurrent task against the same static buffers.

**MacroButtonEditorGump.cs**
- `:299-300` `_previewArea.Clear()` followed by `_previewArea.Children.Clear()` — the second call bypasses control disposal.
- `:168-171`, `:204-208`, `:225-229`, `:273-279` all mutate the live `Macro` object immediately; nothing is saved until button 1 (`:315`).
- `:316-320` locates the existing `MacroButtonGump` by reference equality on `TheMacro` and reassigns the same reference to itself.
- `:59-65` the parameterless ctor creates a throwaway `Macro.CreateEmptyMacro("No Action")` that `BuildGump` is never called on.

**CreateCharProfessionGump.cs**
- `:123-124` and `:143-148` call `Parent.Add(...)` / `Parent.Remove(this)` from inside a child control's mouse-up callback, mutating the parent's children list during event dispatch.

**DiscordMessageControl.cs**
- `:27-28` `Dispose(); return;` in the ctor leaves `Width` set and `Height` unset (0).
- `:21` `msg.Metadata()` is invoked twice in one condition; `:46` invokes it a third time.
- `:59` `meta["name"]` indexed directly after only `graphic`/`hue` were checked with `TryGetValue`.

**BoatControl.cs**
- `:140` `World.Player.IsDrivingBoat` with no null check on `World.Player`.
- `:150-152` `Speed()` returns `1` for both the `one` and `slow` checkboxes; the `one` checkbox is created `IsVisible = false` (`:110`).

**CommandsGump.cs**
- `:53-59` the command list is built once in the ctor; commands registered later (e.g. by scripts) never appear.

**MessageBoxGump.cs**
- `:144-145` the ctor seizes `UIManager.KeyboardFocusControl` and does not restore the previous focus on `Dispose`.
- `:80-84` the optional background `ResizePic` is positioned at `X + 30` / `Y + 40` using the gump's `X`/`Y` **before** they are recentred at `:105-106`.

**SpellQuickSearch.cs**
- `:48-49` `SearchEnterPressed` sets `UIManager.KeyboardFocusControl = null` and then may `Dispose()` (`:52`) — `spellDisplay.InvokeAction()` runs in between (`:50`).
- `:110-111` / `:129` `ClearSpell`/`SetSpell` dispose `icon`/`text` but leave the disposed controls in `Children` until the base control prunes them.
- `:62` `SpellDefinition.TryGetSpellFromName(searchField?.Text, ...)` — `searchField` is re-null-checked here after `:60` already dereferenced it.

---

## Fork deltas

Clearly TazUO / Holiday additions rather than stock ClassicUO (no BSD licence header is a strong tell — stock files all carry the 2021 andreakarasho block):

- **`GridContainer.cs`** — the entire grid-container system (slot manager, persisted `GridContainerEntry` slot/lock map, search box with two search modes, sort by graphic+hue or name, auto-sort, visual stacking of non-stackables, quick-drop-to-backpack, single-click looting, alt-drag multi-select feeding `MultiItemMoveGump`, shift-click → `AutoLootManager`, ctrl-hover equipped-item comparison tooltips, nested-container hover preview, 8 border styles, container opacity/hue, `GridHighlightMenu` hook, `DoubleClickToLootInsideContainers`, backpack lock/position persistence). No licence header.
- **`GridHighLight/GridHighLightRules.cs`** — whole file. Note it uses `JsonSerializer.Serialize/Deserialize` with plain `JsonSerializerOptions` (`:48`, `:86`) rather than a generated serializer context, which the repo convention in `CLAUDE.md` requires; `WorldMapGump`'s `ZonesJsonContext` (`:65`) does follow the convention.
- **`BoatControl.cs`**, **`CommandsGump.cs`**, **`NearbyItems.cs`**, **`ModernShopGump.cs`**, **`MacroButtonEditorGump.cs`**, **`SpellBar/SpellQuickSearch.cs`**, **`DiscordGump/DiscordMessageControl.cs`** — whole files, no licence header, all use the fork's `TextBox.GetOne` / `TrueTypeLoader.EMBEDDED_FONT` / `NineSliceGump` / `ModernUIConstants` / `Language.Instance` infrastructure.
- **`RaceChangeGump.cs`** — no licence header; the fake-mobile + `CustomPaperDollGump` approach and the `Send_ChangeRaceRequest` wiring.
- **`SystemChatControl.cs`** — stock base with fork additions: `ChatMode.ServUOCommand` / `ChatMode.PolCommand` (`:68-69`, `:238-248`, `:497-511`, `:975-989`), `TextHistoryManager` command history + Tab autocomplete via `SelectableItemListGump` (`:641-658`, `:737`), duplicate-line `[xN]` collapsing (`:380-388`, `:1022-1026`), `ProfileManager.CurrentProfile.DisableSystemChat` (`:277`), `HideChatGradient` (`:118`, `:262`), `DisableCtrlQWBtn` (`:557`, `:587`), TTF `TextBox`-based `ChatLineTime` with `GameWindowSideChatFont`/`FontSize` (`:1010`).
- **`WorldMapGump.cs`** — stock base with fork additions: follow-a-party-member (`:90`, `:534`, `:546-562`), `_showCorpse` + corpse line (`:112`, `:416`, `:2701-2733`), pathfinding line (`:2735-2751`), Ctrl+right-click `Pathfinder.WalkTo` (`:3577-3581`), `_allowPositionalTarget` (`:120`, `:434`, `:804-817`), `_showMouseCoordinates` (`:106`, `:2800-2833`), zones (`:1680-1823`, `:3247-3292`), grid overlay (`:119`, `:3294-3328`), north icon (`:122`, `:298-316`), PNG/JPG map icons (`:1873-1874`, `:1901-1923`), multi-directory marker/icon search paths incl. per-server `Data/<ServerName>/` (`:78-79`), scripting-facing `AddUserMarker`/`RemoveUserMarker` (`:2185`, `:2237`), `ResizableGump` base + lock/position profile persistence.
- **`JournalGump.cs`** — stock, plus dark mode (`:74-105`) and the `IgnoreManager` filter (`:300`).
- **`ShopGump.cs`** — stock, plus the drag-resizable middle strip (`_expander`, `_leftMiddle`/`_rightMiddle`, `VendorGumpHeight`, `:229-236`, `:322-332`, `:400-455`) and the hitbox-based scroll buttons with `SCROLL_DELAY`.
- **`DebugGump.cs`** — stock, plus the zoom-index suppression branch (`:152-162`).
- **`TradingGump.cs`** — stock, plus the `MultiItemMoveGump.OnTradeWindowTarget` hook (`:360-367`) and `FormatAsCurrency` (`:630`).
- **`HouseCustomizationGump.cs`**, **`MessageBoxGump.cs`**, **`PopupMenuGump.cs`**, **`CharCreation/CreateCharProfessionGump.cs`** — essentially stock ClassicUO. `PopupMenuGump.CloseNext` (`:44`, `:51-56`) and the debug print at `:160-161` look like fork touches.
