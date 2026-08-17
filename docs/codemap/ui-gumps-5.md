# ui-gumps-5

Partition = every 6th file (offset 5) of `src/ClassicUO.Client/Game/UI/Gumps/**/*.cs`, sorted.
20 files, 12,508 lines, all read in full.

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `Game/UI/Gumps/BaseOptionsGump.cs` | 3049 | Base class for the modern options window **plus** the whole modern-UI widget library it uses (ThemeSettings, ScrollArea/ScrollBar, ModernButton, InputField+StbTextBox, ComboBoxWithLabel, CheckboxWithLabel, SliderWithLabel, ModernColorPickerWithLabel, HotkeyBox, LeftSideMenuRightSideContent). Static search-text broadcast. |
| `Game/UI/Gumps/CharCreation/CreateCharCityGump.cs` | 408 | `CreateCharSelectionCityGump` — starting-city map picker in char creation; town buttons + facet name + HTML description. |
| `Game/UI/Gumps/CombatBookGump.cs` | 842 | Weapon-ability book: index pages + one detail page per ability, drag-out to `UseAbilityButtonGump`, per-frame sync of primary/secondary ability icon/hue from `World.Player.Abilities`. |
| `Game/UI/Gumps/CustomToolTip.cs` | 185 | Free-floating item tooltip gump used by grid/loot UIs; polls OPL on a background Task until data arrives, self-disposes when the hover reference loses the mouse. |
| `Game/UI/Gumps/DiscordGump/DiscordGump.cs` | 236 | Discord social window: channel list, friend list, chat area; subscribes to `DiscordManager` events. |
| `Game/UI/Gumps/FileSelector.cs` | 361 | In-client file/directory browser gump; synchronous `Directory.*` enumeration on the frame thread. |
| `Game/UI/Gumps/GridHighLight/GridHighLightProperties.cs` | 389 | Editor for one grid-highlight config entry (names, properties, slots, exclusions, rarity). Full teardown+rebuild (`Build()`) on every add/delete/resize. |
| `Game/UI/Gumps/HealthBarGump.cs` | 2190 | `BaseHealthBarGump` + classic `HealthBarGump` + `HealthBarGumpCustom`; per-frame entity poll, notoriety/poison/war colouring, party heal buttons, lock/anchor behaviour, JSON `UISettings` for skins. |
| `Game/UI/Gumps/InspectorGump.cs` | 352 | `-info` object inspector; reflects a `GameObject` into a label list, dumps to `dump_gameobject.txt`. |
| `Game/UI/Gumps/Login/ServerSelectionGump.cs` | 454 | Shard list on login; per-entry ping every 2000 ms. |
| `Game/UI/Gumps/MenuGump.cs` | 363 | Server-driven `MenuGump` (horizontal scrolling item strip) and `GrayMenuGump` (radio list); both send menu responses. |
| `Game/UI/Gumps/ModernPaperdoll.cs` | 884 | TazUO modern paperdoll: fixed layer slots, durability bars, menu/preview/minimized sub-gumps. |
| `Game/UI/Gumps/NameOverheadGump.cs` | 952 | Per-entity nameplate: world→screen positioning every Draw, overlap avoidance, HP/mana/stam bars, search filtering, drag-out to healthbar. |
| `Game/UI/Gumps/PartyInviteGump.cs` | 114 | Accept/decline party invite. |
| `Game/UI/Gumps/RGBColorPickerGump.cs` | 90 | NineSlice RGB colour picker wrapper over `ColorSelectorControl`. |
| `Game/UI/Gumps/SelectableItemListGump.cs` | 170 | Modal keyboard-navigable string list (used by chat autocomplete). |
| `Game/UI/Gumps/SpellBar/SpellBar.cs` | 501 | TazUO spell bar: 10 slots × N rows, mouse-wheel row switching, presets, cast-progress overlay. |
| `Game/UI/Gumps/Supporters.cs` | 123 | Scrolling credits gump; animation driven from `Draw`. |
| `Game/UI/Gumps/TopBarGump.cs` | 495 | Top toolbar; paperdoll/inventory/journal/chat/map/store buttons plus TazUO Assistant / Legion Script / More+ / Xml Gumps menus. |
| `Game/UI/Gumps/VersionHistory.cs` | 350 | Hard-coded changelog text blob in a scrollable NineSliceGump. |

## Types

| Name | File:line | Responsibility |
| --- | --- | --- |
| `BaseOptionsGump` | BaseOptionsGump.cs:18 | Frame + search bar + left-menu/right-content split; page string save/restore (`GetPageString`/`GoToPage`). |
| `BaseOptionsGump.ThemeSettings` | BaseOptionsGump.cs:222 | Static theme constants; `FONT`/`STANDARD_TEXT_SIZE` read `ProfileManager.CurrentProfile` on each get. |
| `BaseOptionsGump.PositionHelper` | BaseOptionsGump.cs:272 | **Static** layout cursor (X/Y/LAST_Y) shared by every options page builder. |
| `BaseOptionsGump.SearchableOption` | BaseOptionsGump.cs:265 | Interface: `Search(text)` / `OnSearchMatch()`. |
| `BaseOptionsGump.LeftSideMenuRightSideContent` | BaseOptionsGump.cs:404 | Two-pane container; `AddToLeft`/`AddToRight` auto-stack; `SetMatchingButton` lights up search hits. |
| `BaseOptionsGump.HotkeyBox` | BaseOptionsGump.cs:548 | Key/mouse/wheel/controller binding capture widget. |
| `BaseOptionsGump.InputField` (+`StbTextBox`) | BaseOptionsGump.cs:800 / :902 | Text entry backed by `StbTextEditSharp.TextEdit`; own caret, selection, clipboard, SDL text-input rect. |
| `ModernButton` | BaseOptionsGump.cs:1738 | HitBox+label button, `SwitchPage`/`Activate`, radio grouping via `_groupnumber`, implements `SearchableOption`. |
| `ScrollArea` / `ScrollArea.ScrollBar` | BaseOptionsGump.cs:1907 / :2092 | Modern scroll container (child index 0 == scrollbar) and its slider. |
| `ComboBoxWithLabel` (+`Combobox`, `ComboboxGump`, `HoveredLabel`) | BaseOptionsGump.cs:2194 / :2276 / :2364 / :2440 | Sorted dropdown; maps display index ↔ original index via `_originalIndices`. |
| `InputFieldWithLabel` / `ModernColorPickerWithLabel` / `CheckboxWithLabel` / `SliderWithLabel` | :2515 / :2592 / :2660 / :2785 | Labelled option widgets, each subscribing to `SearchValueChanged`. |
| `CreateCharSelectionCityGump` (+`CityControl`) | CreateCharCityGump.cs:47 / :313 | City picker; button IDs 2+index. |
| `CombatBookGump` | CombatBookGump.cs:47 | Ability book; `_enqueuePage` deferred page turn. |
| `CustomToolTip` | CustomToolTip.cs:11 | OPL-backed floating tooltip; `OnOPLLoaded` event. |
| `DiscordGump` | DiscordGump.cs:11 | Discord chat window; `ActiveChannel` from chat-area control. |
| `FileSelector` / `FileSelectorType` | FileSelector.cs:13 / :356 | File browser; static `ShowFileBrowser` helper. |
| `GridHighlightProperties` | GridHighLightProperties.cs:11 | Highlight-config editor over `GridHighlightData.GetGridHighlightData(keyLoc)`. |
| `BaseHealthBarGump` | HealthBarGump.cs:51 | Shared healthbar behaviour: targeting, rename, alt-lock, anchor, save/restore. |
| `HealthBarGumpCustom` (+`LineCHB`, `Settings`) | HealthBarGump.cs:467 / :1428 / :1492 | Line-drawn custom bar. |
| `HealthBarGump` (+`Settings`) | HealthBarGump.cs:1524 / :2162 | Classic gump-art bar with party heal buttons. |
| `InspectorGump` | InspectorGump.cs:49 | Object dump viewer. |
| `ServerSelectionGump` (+`ServerEntryGump`) | ServerSelectionGump.cs:48 / :314 | Shard list + ping rows. |
| `MenuGump` (+`ItemView`, `ContainerHorizontal`) | MenuGump.cs:43 / :174 / :210 | Server menu packet UI. |
| `GrayMenuGump` | MenuGump.cs:286 | Radio-list server menu. |
| `ModernPaperdoll` (+`ItemSlot`, `ItemGumpFixed`, `MenuButton`, `MenuGump`, `CharacterPreview`, `MinimizedPaperdoll`) | ModernPaperdoll.cs:21 / :338 / :439 / :582 / :605 / :821 / :840 | Modern paperdoll and its satellites. |
| `NameOverheadGump` | NameOverheadGump.cs:49 | Nameplate. |
| `PartyInviteGump` | PartyInviteGump.cs:41 | Party invite prompt. |
| `RGBColorPickerGump` | RGBColorPickerGump.cs:10 | Colour picker; static `Open`. |
| `SelectableItemListGump` | SelectableItemListGump.cs:10 | Modal list picker. |
| `SpellBar` (+`SpellEntry`) | SpellBar.cs:13 / :268 | Spell bar; `SpellBar.Instance` singleton. |
| `Supporters` | Supporters.cs:12 | Credits scroller. |
| `TopBarGump` (+`RighClickableButton`) | TopBarGump.cs:48 / :471 | Toolbar; static `Create()`. |
| `VersionHistory` | VersionHistory.cs:9 | Changelog. |

## State

Mutable / static / global state owned in this partition:

- `BaseOptionsGump.SearchText` (static string) — BaseOptionsGump.cs:21; written from the search `InputField` handler at :83.
- `BaseOptionsGump.SearchValueChanged` (static event) — BaseOptionsGump.cs:23; every searchable widget subscribes in its ctor and unsubscribes in `Dispose`.
- `BaseOptionsGump.PositionHelper.X/Y/LAST_Y` (static ints) — BaseOptionsGump.cs:274; mutated by `PositionControl`/`Indent`/`BlankLine`/`Reset`.
- `ThemeSettings.*` static settable properties — BaseOptionsGump.cs:224-261.
- `LeftSideMenuRightSideContent.leftY/rightY/leftX/rightX` (per-instance layout cursors) — BaseOptionsGump.cs:407.
- `ScrollArea._scrollBar` occupies `Children[0]`; all iteration starts at index 1 — BaseOptionsGump.cs:1984, :2031, :2049, :2086.
- `Combobox._originalIndices` / `_sortedItems` / `_selectedIndex` — BaseOptionsGump.cs:2281-2283.
- `CombatBookGump._enqueuePage` (-1 sentinel), `_dictionaryPagesCount` (mutated in-place at :207 to `pages + abilityCount`), `_primAbility`/`_secAbility` shared GumpPics added to multiple pages — CombatBookGump.cs:51-53, :176, :199.
- `FileSelector._lastPath` (static string) — FileSelector.cs:25; set by `NavigateToDirectory` :265.
- `GridHighlightProperties.slotCheckboxes` (Dictionary<string,Checkbox>) — GridHighLightProperties.cs:17; never cleared across `Build()` rebuilds.
- `BaseHealthBarGump.LastAttackBar` (static) — HealthBarGump.cs:56; also set from `Restore` :196.
- `HealthBarGumpCustom._settings` / `HealthBarGump._settings` (static `UISettings`, lazily loaded+saved) — HealthBarGump.cs:469, :1526; derived statics `HPB_WIDTH`, `HPB_BAR_SPACELEFT`, cached `Texture2D`s at :491-512.
- Per-bar mutable flags `_outOfRange`, `_isDead`, `_canChangeName`, `_poisoned`, `_yellowHits`, `_normalHits`, `_oldWarMode`, `_oldHits/_oldMana/_oldStam` — HealthBarGump.cs:116-120, :520, :1553-1555.
- `ModernPaperdoll.lastX/lastY` (static ints) — ModernPaperdoll.cs:40; written in `Update`, `Dispose`, `Restore`.
- `ModernPaperdoll.itemLayerSlots` keyed by `Layer[]` **array instance** — ModernPaperdoll.cs:38, filled :87-156.
- `ItemSlot.timedTexts` (List<SimpleTimedTextGump>) — ModernPaperdoll.cs:345.
- `NameOverheadGump.currentHeight` (static int, default 22) + `CurrentHeight` — NameOverheadGump.cs:61-79; written only from the *item* branch of `SetName` (:163).
- `NameOverheadGump._positionLocked/_lockedPosition/_isLastTarget/_needsNameUpdate/_textDrawOffset/_borderColor` — :51-60.
- `SpellBar.Instance` (static) — SpellBar.cs:15; ctor disposes the previous instance and replaces it (:23-24).
- `SpellBar.spellEntries[10]` — SpellBar.cs:17.
- `Supporters.offset` (double), `supporterLabels[]` — Supporters.cs:31-35, mutated inside `Draw`.
- `VersionHistory.updateTexts` (static string[]) — VersionHistory.cs:11.
- `ServerEntryGump._pingCheckTime` (uint deadline) — ServerSelectionGump.cs:321.

## Timing

**Per frame — `Update()`**
- `ScrollArea.Update` → `CalculateScrollBarMaxValue()` walks all children and calls `UpdateOffset` on each (BaseOptionsGump.cs:1952, :2037).
- `SliderWithLabel.Slider.Update` polls `Mouse.Position` while `_clicked` (BaseOptionsGump.cs:2926).
- `ComboboxGump.HoveredLabel.Update` re-hues on hover state (BaseOptionsGump.cs:2470).
- `CityControl.Update` recomputes hover state (CreateCharCityGump.cs:382).
- `CombatBookGump.Update` reads `World.Player.Abilities[0]/[1]` and syncs `_primAbility`/`_secAbility` graphic+hue; then flushes `_enqueuePage` once `Time.Ticks - Mouse.LastLeftButtonClickTime >= Mouse.MOUSE_DELAY_DOUBLE_CLICK` (CombatBookGump.cs:378, :428).
- `HealthBarGumpCustom.Update` / `HealthBarGump.Update` — full entity re-poll every frame: `World.Get(LocalSerial)`, party membership, notoriety hue, poison/yellow flags, HP/mana/stam percents, war mode; may `Dispose()` mid-update (HealthBarGump.cs:555, :1839).
- `MenuGump.Update` increments `_container.Value` by ±1 per frame while an arrow HitBox is held (MenuGump.cs:128).
- `ModernPaperdoll.Update` writes `ProfileManager.CurrentProfile.ModernPaperdollPosition` whenever X/Y changed (ModernPaperdoll.cs:261).
- `NameOverheadGump.Update` — disposes if entity gone/handles closed; toggles last-target border; re-runs `SetName()` while `_needsNameUpdate` (NameOverheadGump.cs:629).
- `ServerEntryGump.Update` — **2000 ms** ping interval: `_pingCheckTime = Time.Ticks + 2000; _entry.DoPing();` (ServerSelectionGump.cs:418-421).

**Per frame — `Draw()`** (these do real work, not just painting)
- `NameOverheadGump.Draw` computes world→screen position, culls against camera bounds, runs `AdjustPositionToAvoidOverlap` (up to 10 iterations over *all* visible nameplates), then assigns `X`/`Y` (NameOverheadGump.cs:676-844).
- `CustomToolTip.Draw` disposes itself when `hoverReference.MouseIsOver` is false (CustomToolTip.cs:129).
- `Supporters.Draw` advances the credit scroll by `offset += 0.9` and mutates label `Y`/`IsVisible` (Supporters.cs:86-108).
- `SpellBar.SpellEntry.Draw` computes cast progress from `DateTime.Now - SpellVisualRangeManager.Instance.LastSpellTime` (SpellBar.cs:425).
- `SpellBar.Draw` / `BaseHealthBarGump.Draw` draw the alt-key lock icon only while `Keyboard.Alt` (SpellBar.cs:242, HealthBarGump.cs:424).
- `ModernPaperdoll.ItemGumpFixed.Draw` disposes itself if `item == null` (ModernPaperdoll.cs:499).

**Event / packet driven**
- `DiscordGump.OnMessageReceived` / `OnUserUpdated` / `OnStatusTextUpdated` — fired from `DiscordManager` (DiscordGump.cs:36-38).
- `SpellBar.EventSinkOnSpellCastBegin` — `EventSink.SpellCastBegin` (SpellBar.cs:38).
- `MenuGump.AddItem` / `GrayMenuGump.AddItem` — called by the menu packet handler as the packet is parsed (MenuGump.cs:138, :314).
- `ModernPaperdoll.UpdateContents` — via `RequestUpdateContents()`, runs on the next UI update after equipment change (ModernPaperdoll.cs:216).
- `HealthBarGump*.UpdateContents` — rebuilds the whole gump (Clear + Children.Clear + BuildGump) (HealthBarGump.cs:536, :1582).
- `BaseHealthBarGump.Save/Restore` — profile save/load (HealthBarGump.cs:162, :174).

**Timers / delays**
- `CustomToolTip.LoadOPLData` retries on a `Task.Factory.StartNew` + `Task.Delay(1500).Wait()`, max 5 attempts (CustomToolTip.cs:57-105).
- `ItemSlot.AddText` shows a `SimpleTimedTextGump` for `TimeSpan.FromSeconds(2)` (ModernPaperdoll.cs:369).
- Party heal buttons set `World.Party.PartyHealTimer = Time.Ticks + 50` (HealthBarGump.cs:2139, :2146).
- `NameOverheadGump` mouse-up schedules `DelayedObjectClickManager.Set(..., Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK)` (NameOverheadGump.cs:458).

**On load / on demand**
- `TopBarGump.Create()` at world enter; `RefreshXmlGumps()` on demand (TopBarGump.cs:362, :324).
- `FileSelector.RefreshFileList()` — synchronous `Directory.EnumerateDirectories` / `GetFiles` / `DriveInfo.GetDrives` on the frame thread (FileSelector.cs:154-246).
- `GridHighlightProperties.Build()` on ctor, on every add/delete button, and on `OnResize` (GridHighLightProperties.cs:29-35).
- `VersionHistory.Build()` on ctor and `OnResize` (VersionHistory.cs:344).

## Inbound

- `UIManager.Add(...)` / `UIManager.GetGump<T>()` — every gump here is created and looked up through UIManager.
- `TopBarGump.Create()` called from world-load code; it is the entry point to `AssistantGump`, `ScriptManagerGump`, `CommandsGump`, `DebugGump`, `NetworkStatsGump`, `DiscordGump`, `SpellQuickSearch`, `BoatControl`, `NearbyLootGump`, `CommandsGump`, XML gumps.
- Network menu packet handlers construct `MenuGump`/`GrayMenuGump` and call `AddItem`/`SetHeight`.
- `NameOverHeadManager` drives nameplate visibility (`IsShowing`, `Search`) and reads `NameOverheadGump.CurrentHeight`; nameplates are created per entity by the object-handles path.
- `NameOverheadGump.UpdateAllOptions()` (static) called from options when nameplate font changes (NameOverheadGump.cs:538).
- `ModernPaperdoll.UpdateAllOptions()` (static) called from options (ModernPaperdoll.cs:253).
- `ModernPaperdoll.HandleObjectMessage(parent, text, hue)` called from the message manager to route overhead text into equipment slots (ModernPaperdoll.cs:205).
- Healthbars: created from `NameOverheadGump.DoDrag`, from `ModernPaperdoll.MenuGump` "Status", from party/target code; restored from profile XML.
- `BaseHealthBarGump.LastAttackBar` read/written by attack-target code outside this partition.
- `SpellBar.Instance` + `SetupHotkeyLabels()` called by `SpellBarManager`/options.
- `FileSelector.ShowFileBrowser(...)` and `RGBColorPickerGump.Open(...)` are the static entry points used by other gumps.
- `BaseOptionsGump` is subclassed by the real options gumps (`ModernOptionsGump`, `AssistantGump`) which consume `ThemeSettings`, `PositionHelper`, `ModernButton`, `ScrollArea`, `CheckboxWithLabel`, etc.
- `SelectableItemListGump` used by chat auto-complete.
- `CustomToolTip` created by grid container / nearby-loot UIs.

## Outbound

- `GameActions.*` — `RequestMobileStatus`, `SendCloseStatus`, `Attack`, `OpenCorpse`, `DoubleClick`, `Rename`, `PickUp`, `DropItem`, `Equip`, `CastSpell`, `UsePrimaryAbility`/`UseSecondaryAbility`, `ReplyGump`, `RequestHelp`, `OpenSettings`, `OpenSkills`, `OpenGuildGump`, `ToggleWarMode`, `RequestQuestMenu`, `RequestProfile`, `OpenAbilitiesBook`, `OpenPaperdoll`, `OpenBackpack`, `OpenJournal`, `OpenChat`, `OpenWorldMap`, `Print`, `RequestPartyAccept`.
- `NetClient.Socket.Send_MenuResponse` / `Send_GrayMenuResponse` / `Send_PartyDecline` / `Send_OpenUOStore`.
- `World.*` — `Get`, `Mobiles`, `Items`, `Player`, `Party`, `OPL`, `CorpseManager`, `DurabilityManager`.
- `TargetManager` — `IsTargeting`, `Target`, `LastTargetInfo`, `LastAttack`, `SetTargeting`, `CancelTarget`.
- `ProfileManager.CurrentProfile` — read *and written* (positions, `FollowingMode`/`FollowingTarget`, `TopbarGumpPosition`, `TopbarGumpIsMinimized`, `ModernPaperdollPosition`, `AutoOpenXmlGumps`, `OpenModernPaperdollAtMinimizeLoc`, `LastTargetHealthBarPos`).
- `UIManager` — `Add`, `GetGump`, `Gumps` (LinkedList walk), `AnchorManager`, `KeyboardFocusControl`, `MouseOverControl`, `DraggingControl`, `AttemptDragControl`, `SystemChat`.
- Managers: `DiscordManager`, `SpellBarManager`, `SpellVisualRangeManager`, `NameOverHeadManager`, `GridHighlightData`/`GridHighlightRules`, `XmlGumpHandler`, `MessageManager`, `DelayedObjectClickManager`, `CommandManager`, `ToolTipOverrideData`, `ExternalImageLoader`, `EventSink`.
- Assets: `ClilocLoader`, `TileDataLoader`, `MapLoader`, `GumpsLoader`, `AnimationsLoader`, `TrueTypeLoader`, `Client.Game.Arts/Gumps/Animations/Audio`.
- Renderer: `UltimaBatcher2D`, `ShaderHueTranslator`, `SolidColorTextureCache`.
- Other gumps: `UseAbilityButtonGump`, `StatusGumpBase`, `PartyGump`, `RacialAbilitiesBookGump`, `DurabilitysGump`, `AssistantGump`, `LegionScripting.ScriptManagerGump`, `SpellQuickSearch`, `InputRequest`, `ModernColorPicker`, `InspectorGump`, `NearbyLootGump`, `BoatControl`, `CommandsGump`, `DebugGump`, `NetworkStatsGump`, `SimpleTimedTextGump`, `MinimizedPaperdoll`.
- `Settings.Load<T>/Save<T>` (UISettings JSON) from healthbar skins.
- FS/OS: `Directory`, `DriveInfo`, `LogFile`, `SDL.SDL_SetClipboardText`, `SDL_StartTextInput`/`SDL_SetTextInputRect`.

## Hazards

- `BaseOptionsGump.cs:84` — `SearchValueChanged.Raise()` with no null check; a `Raise()` extension is relied on. Every searchable widget adds a handler to this **static** event in its ctor; leaked/undisposed widgets keep the gump graph alive.
- `BaseOptionsGump.cs:272-274` — `PositionHelper` is static; two options gumps (or a gump and a sub-page) building concurrently share one X/Y cursor.
- `BaseOptionsGump.cs:98-99` — `MainContent.RightArea.GetScrollBar.Dispose(); ... = null;` leaves `Children[0]` as the disposed scrollbar while `ScrollArea.Draw`/`Clear`/`CalculateScrollBarMaxValue` still skip index 0 unconditionally (`start = 0` only when `_scrollBar == null`, but `Clear()` at :2031 and the loops at :2049/:2086 always begin at 1).
- `BaseOptionsGump.cs:2008-2011` — `ScrollArea.OnMouseEnter` steals `UIManager.KeyboardFocusControl` ("Dirty fix for mouse wheel macros"); it is never restored on exit.
- `BaseOptionsGump.cs:2320-2324` — `Combobox.SelectedIndex` getter indexes `_originalIndices[_selectedIndex]`; setter assigns `displayIndex = _originalIndices[0]` (an original index) when `value <= -1`, mixing the two index spaces.
- `BaseOptionsGump.cs:2410-2411` — `labels.Max(...)` throws on an empty `items` array.
- `BaseOptionsGump.cs:2992` — `Slider.OnMouseEnter` sets `UIManager.KeyboardFocusControl = this` unconditionally.
- `BaseOptionsGump.cs:1560` — `int.TryParse(Stb.text + c, ...)` compares the parsed value against `_maxCharCount`, which is a *character count*, not a value bound.
- `CombatBookGump.cs:159-160` — `World.Player.PrimaryAbility` read during `BuildGump`; `((byte)... & 0x7F) - 1` indexes `AbilityData.Abilities` with no bounds check (index -1 when the ability byte is 0).
- `CombatBookGump.cs:389` / `:406` — same unchecked `(index & 0x7F) - 1` indexing runs **every frame** in `Update`.
- `CombatBookGump.cs:176,199` — the same `_primAbility`/`_secAbility` control instance is `Add`ed once per dictionary page loop iteration, so one control is registered under multiple pages; `SetActivePage` then reassigns `.Page` (:466-467).
- `CombatBookGump.cs:207` — `_dictionaryPagesCount += _abilityCount;` mutates the field that `SetActivePage` clamps against, after the index pages were built against the old value.
- `CustomToolTip.cs:100-104` — retry runs on a thread-pool task with `Task.Delay(1500).Wait()`; the recursive call passes `attempt++` (post-increment), so the argument is always the original value and the `attempt > 4` guard never advances — retries only stop via `IsDisposed`. The retry also mutates gump state (`text`, `Width`, `Height`) off the frame thread.
- `CustomToolTip.cs:178` — `text.Draw(...)` without a null check; `text` is disposed and reassigned at :80-81 from the background task.
- `CustomToolTip.cs:44` — `ToolTipOptions` dereferences `ProfileManager.CurrentProfile` unguarded.
- `DiscordGump.cs:59,64,74,79` — `_discordChannelList` / `_discordChatArea` used from `OnMessageReceived` without disposed checks; handler unsubscribe happens in `Dispose` at :233 **after** `base.Dispose()`.
- `FileSelector.cs:213-238` — directory/file enumeration and control creation happen synchronously inside the gump's build/refresh, i.e. on the frame thread.
- `FileSelector.cs:250-251` — `GetFilteredFiles` returns an empty array when `_fileExtensions == null`, so clearing the filter to `*.*` (which sets `_fileExtensions = null` at :321) hides all files.
- `GridHighLightProperties.cs:114-131` and :226-241 — `AddOther(list, i, ...)` captures `index` and calls `others.RemoveAt(index)` in the delete handler; the surrounding `Build()` is re-run on delete, but the captured indices of controls created earlier in the same pass are stale until that rebuild completes.
- `GridHighLightProperties.cs:37` — `Build()` calls `Clear()` then re-adds, but `slotCheckboxes` (:17) is never cleared, so it accumulates entries pointing at disposed `Checkbox` instances; the `otherCheckbox` handler at :176 writes `cb.IsChecked` on whatever is in the dictionary.
- `GridHighLightProperties.cs:169,174,192,197` — per-control reflection (`typeof(GridHighlightSlot).GetProperty(...)`) inside `Build()` and inside a `ValueChanged` handler.
- `HealthBarGump.cs:86` — `SetNewMobile` calls `Children.Clear()` without disposing the old children, then `BuildGump()`; `_textBox` still holds the old (now orphaned) control and its `MouseUp` handler.
- `HealthBarGump.cs:578` / `:1865` — corpse existence probed as `LocalSerial | 0x8000_0000`; the healthbar keeps its own `_outOfRange`/`_isDead` copy of state the server owns.
- `HealthBarGump.cs:582,664,1868,1935` — `CheckIfAnchoredElseDispose()` can `Dispose()` this gump from inside `Update()`, and the method returns immediately after; anything iterating `UIManager.Gumps` during that update sees the mutation.
- `HealthBarGump.cs:658` — `_hpLineRed.IsVisible = ...` with no null check; `_hpLineRed` is set to null in `UpdateContents` (:542) before `BuildGump()` runs.
- `HealthBarGump.cs:1898,1987` — `_buttonHeal1/_buttonHeal2` dereferenced in the `inparty` branch; they are only created in the party branch of `BuildGump` (:1670), so a bar that becomes "in party" without a rebuild hits null.
- `HealthBarGump.cs:491-512` — `HPB_WIDTH`, `HPB_BAR_SPACELEFT` and the `Texture2D` cache are static and initialised once from the JSON settings at type load; editing the settings file requires a restart.
- `HealthBarGump.cs:125` — `private new bool IsLocked` shadows the base member, so base-class code reading `IsLocked` sees a different value.
- `HealthBarGump.cs:1423` — `Contains` returns `true` unconditionally for the custom bar (whole bounding box swallows hit-tests).
- `ModernPaperdoll.cs:38,66,88` — `itemLayerSlots` is a `Dictionary<Layer[], ItemSlot>` keyed by array reference identity; lookup by value is impossible and only enumeration works.
- `ModernPaperdoll.cs:40,265-271` — `lastX/lastY` are **static**, shared by all `ModernPaperdoll` instances and written every frame the gump moves.
- `ModernPaperdoll.cs:427` — `ClearItems` calls `itemArea.Children.Clear()` without disposing the `ItemGumpFixed` children.
- `ModernPaperdoll.cs:371-372` — `AddText` positions the timed text at `ScreenCoordinateX/Y` captured at creation time; the gump may move afterwards.
- `ModernPaperdoll.cs:376-380` — `timedTexts` is pruned only on the next `AddText`; disposed gumps linger in the list until then.
- `ModernPaperdoll.cs:499-511` — `ItemGumpFixed.Draw` calls `Dispose()` when `item == null` then still dereferences `item.Hue` on the next lines if `IsDisposed` was already false-checked; the `item` field is readonly and captured at construction, so it can outlive the real world item.
- `ModernPaperdoll.cs:52,70,362` — `ProfileManager.CurrentProfile` dereferenced before the `!= null` check at :57.
- `ModernPaperdoll.cs:814-818` — the paperdoll `MenuGump` disposes itself on `OnMouseExit`.
- `NameOverheadGump.cs:61,163` — `currentHeight` is static but only assigned from the **item** branch of `SetName`; mobile nameplates never update it, so `CurrentHeight` reflects whichever item nameplate ran last.
- `NameOverheadGump.cs:81-90` — `public new UILayer LayerOrder` shadows the base property and its setter is a no-op, so anything assigning through the base type is silently discarded.
- `NameOverheadGump.cs:594-618` — `AdjustPositionToAvoidOverlap` builds a fresh `List<NameOverheadGump>` by walking `UIManager.Gumps` and loops up to 10 times, **inside `Draw`**, once per nameplate per frame (O(n²) allocation + compare).
- `NameOverheadGump.cs:843-844` — `Draw` assigns `X`/`Y` (control layout mutated during rendering), and reads `other.X/other.Y` of gumps that may not have drawn yet this frame, so the result depends on draw order.
- `NameOverheadGump.cs:162,175` — `_background` dereferenced in `SetName`; `SetName()` is also called from `UpdateOptions` (:562) which can run before/independently of `BuildGump`.
- `NameOverheadGump.cs:864` — `World.Party.Contains(m.Serial)` after `m` came from `World.Mobiles.Get`, no null check on `m` in this branch (the earlier null check returned at :690 but `m` is re-fetched at :862).
- `NameOverheadGump.cs:719,869` — `m.Hits / m.HitsMax` divides by `HitsMax` with no zero guard.
- `PartyInviteGump.cs:49` — `mobile.Name.Length` after `mobile == null ||`, but `Name` itself may be null (the `IsNullOrEmpty` guard only exists on the label at :60).
- `SelectableItemListGump.cs:41-42` — `_background.Width/Height` set from `Width/Height` after `ForceSizeUpdate()`; `CreateItemLabels` at :48 reads `Width` before it has been sized.
- `SpellBar.cs:23-24` — the ctor disposes the previous `Instance` and overwrites the static; `Dispose()` (:231) does **not** clear `Instance`, so `SpellBar.Instance` can point at a disposed gump.
- `SpellBar.cs:43-49` — `EventSinkOnSpellCastBegin` iterates `spellEntries` with no null-element check; entries are only populated in `Build()`.
- `SpellBar.cs:100-103,294-295` — `SpellBarManager.CurrentRow` clamped in `Build()` but `SetSpell` indexes `SpellBarManager.SpellBarRows[row]` with the row captured at build time; "Delete row" (:169) removes from that list while entries still hold the old row index until `Build()` re-runs.
- `SpellBar.cs:321` — `ProfileManager.CurrentProfile.SpellBar_ShowHotkeys` dereferenced with no null check.
- `Supporters.cs:86-108` — animation state (`offset`, label `Y`) mutated inside `Draw`, so scroll speed is tied to frame rate; `offset += 0.9` then `if (offset >= 1) offset = 0` means `(int)offset` is 0 on the first frame of each pair, giving an uneven step.
- `Supporters.cs:29` — texture loaded from disk in a field initialiser (runs during construction on the frame thread).
- `TopBarGump.cs:324-331` — `RefreshXmlGumps` calls `XmlGumps.ContextMenu?.Dispose()` and only allocates a new `ContextMenuControl` when the field is `null`, so after the first refresh it keeps adding entries to a disposed menu; each refresh also re-adds every xml entry plus another "Reload" entry (:358) without clearing.
- `TopBarGump.cs:283-314` — `XmlGumps` is only created when `xmls.Length > 0`; `RefreshXmlGumps()` dereferences it unconditionally (called at :311 and from the Reload entry).
- `TopBarGump.cs:395-404` — right-click snaps the bar to 0,0 and writes the profile, with no `base.OnMouseUp` call.
- `ServerSelectionGump.cs:418-421` — `_pingCheckTime` starts at 0, so the first `Update` pings immediately; `DoPing()` is invoked from the frame thread every 2000 ms per server row.
- `ServerSelectionGump.cs:221-232` — `loginScene.Servers[index]` with `index` from `GetServerIndexFromSettings()`, no bounds check.
- `MenuGump.cs:249` — `ContainerHorizontal.Draw` assigns `child.X` while drawing (layout mutation during render).
- `MenuGump.cs:297` — `ResizePic` created with `Height = 111111` and corrected later by `SetHeight`.
- `CreateCharCityGump.cs:291-296` — `OnButtonClick` treats any `buttonID >= 2` as a city index (`buttonID - 2`) without validating against `_cityControls.Count`.
- `CreateCharCityGump.cs:98` — `Dispose()` is called from the constructor when no city is found, then construction continues to `return`.
- `InspectorGump.cs:154` — the property dictionary is snapshotted once at construction; the gump never refreshes as the object changes.
- `VersionHistory.cs:344-347` — `OnResize` calls `Build()` which `Clear()`s and re-creates every `TextBox` for the full changelog.

## Fork deltas

Clearly TazUO / Holiday additions (not stock ClassicUO — no BSD license header, TazUO naming, or TazUO-only managers):

- `BaseOptionsGump.cs` in its entirety — the modern options framework (`ThemeSettings`, `ModernButton`, modern `ScrollArea`, `LeftSideMenuRightSideContent`, `CheckboxWithLabel`, `SliderWithLabel`, `ComboBoxWithLabel`, `InputFieldWithLabel`, `ModernColorPickerWithLabel`, `HotkeyBox`) plus the static search system.
- `ModernPaperdoll.cs` — whole file; TazUO paperdoll with durability bars, `CharacterPreview`, `MinimizedPaperdoll`, embedded `modern-paperdollgump.png`.
- `SpellBar/SpellBar.cs` — whole file; `GumpType.SpellBar`, `SpellBarManager`, presets, embedded up/down icons.
- `DiscordGump/DiscordGump.cs` — whole file; `Discord.Sdk` integration, `DiscordManager`.
- `GridHighLight/GridHighLightProperties.cs` — whole file; grid highlight + auto-loot-on-match.
- `FileSelector.cs`, `RGBColorPickerGump.cs`, `SelectableItemListGump.cs`, `CustomToolTip.cs`, `Supporters.cs`, `VersionHistory.cs` — all TazUO-only gumps (no license header; `NineSliceGump`/`ModernUIConstants`/`Positioner`/`VBoxContainer`/`HttpClickableLink` are TazUO controls).
- `HealthBarGump.cs` — stock file heavily extended: `UISettings`-backed JSON skins (`Settings` classes, `ColorJsonConverter`), `IsLastAttackBar`/`LastAttackBar`, alt-click lock via gump `0x82C`, `IsLastTarget` + `LastTargetHealthBarPos`, distance-tinted borders (:740-790), `CBBlackBGToggled`, `DisableAutoFollowAlt`, `CloseHealthBarIfAnchored`, `SaveHealthbars` save/restore attributes.
- `NameOverheadGump.cs` — stock file extended: TTF `TextBox` nameplates with `NamePlateFont`/`NamePlateFontSize`, `NamePlateOpacity`/`NamePlateBorderOpacity`, `NamePlateHealthBar` with mana/stam bars, `NamePlateHideAtFullHealth(InWarmode)`, `NamePlateAvoidOverlap`, `NameOverHeadManager.Search` filtering, `UpdateAllOptions()` (comment at :532-537 reads as a Holiday-branch fix).
- `TopBarGump.cs` — stock toolbar plus TazUO buttons: Assistant, Legion Script, "More +" context menu (Commands, Toggle nameplates, Discord, Tools submenu: Spell quick cast / Boat control / Nearby Loot), Xml Gumps menu, `Language.Instance` localisation.
- `CombatBookGump.cs` — mostly stock; `_enqueuePage` double-click deferral (:144, :428) and the null-guarded `_primAbility`/`_secAbility` creation (:157, :179) look like fork edits.
- `MenuGump.cs`, `InspectorGump.cs`, `ServerSelectionGump.cs`, `PartyInviteGump.cs`, `CreateCharCityGump.cs` — essentially stock ClassicUO (BSD headers intact); small fork touches: controller support in `ServerSelectionGump.OnControllerButtonUp` (:272), `CUOEnviroment.NoServerPing`, `CUOEnviroment.IsOutlands` branches in the city gump.
- `FileSelector.cs:251` uses the C# 12 collection expression `[]`, and several files use file-scoped namespaces — both compile on net472 with the SDK in use, but are newer-syntax markers relative to stock.
