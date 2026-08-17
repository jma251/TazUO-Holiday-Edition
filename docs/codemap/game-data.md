# game-data

Partition root: `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Game/Data`
45 files, 12,059 lines, all read in full.

This is the client's static/definition layer: enums that mirror UO protocol values, big
hardcoded lookup tables (chairs, buffs, spells, light colours, static-tile classification),
and a handful of loaders that overlay those tables from user-editable text/JSON files in
`<exe>/Data/Client/`. Almost everything is `static` and mutated exactly once at startup;
the exceptions (spell dictionaries, `WordToTargettype`, `LightColors` dictionaries,
`ChairTable.Table`) stay writable for the life of the process.

## Files

| Path (relative to partition root) | Lines | Purpose |
| --- | --- | --- |
| `Ability.cs` | 126 | `Ability` enum (32 combat abilities) + `AbilityDefinition` struct + `AbilityData.Abilities[32]` name/icon table. |
| `BuffIcon.cs` | 62 | `BuffIcon` instance: type, graphic, absolute expiry tick, text, title. Equality is by `Type` only. |
| `BuffTable.cs` | 471 | `BuffIconType` enum (0x3E9…) and `BuffTable` — graphic id table, loaded from `Data/Client/buff.txt` or the 190-entry `_defaultTable`. |
| `ChairTable.cs` | 1942 | `ChairTable.Table` (graphic → `AnimationsLoader.SittingInfoData`), generated to / reloaded from `Data/Client/chair.txt`; ~190 default entries. |
| `CharacterCreationValues.cs` | 264 | Skin/hair colour palettes and hair/beard graphic+cliloc lists per `RaceType`/sex; `ComboContent` resolves clilocs at construction. |
| `CharacterSpeedType.cs` | 41 | 4-value enum (Normal, FastUnmount, CantRun, FastUnmountAndCantRun). |
| `ClientFeatures.cs` | 91 | `CharacterListFlags` + `ClientFeatures` (server-declared tooltips/popup/paperdoll-books/MaxChars). |
| `ClientProtocol.cs` | 51 | `ClientFlags` expansion bitmask sent in the login handshake. |
| `ContainerData.cs` | 69 | `ContainerData` struct: container gump graphic, open/close sound, bounds, minimizer area. |
| `CustomHouse.cs` | 500 | `CustomHouseObject` hierarchy (Wall/Floor/Roof/Misc/Door/Teleport/Stair/PlaceInfo) with `Parse(string)` scanf-style readers for the housing .txt data files. |
| `Direction.cs` | 225 | `Direction` enum (+`Running` 0x80, `NONE` 0xED) and `DirectionHelper` math: from points/vectors, `GetDirectionAB`, `CalculateDirection`, keyboard arrows, `GetCardinal`, `Reverse`. |
| `EntityFlags.cs` | 50 | `Flags` byte enum from the 0x20/0x77/0x78 mobile packets. |
| `GraphicEffectBlendMode.cs` | 45 | 8 blend modes for graphic effects. |
| `GraphicEffectType.cs` | 46 | Effect kinds; `DragEffect = 0x05` is client-invented. |
| `HideHudFlags.cs` | 32 | `[Flags] ulong` of 21 hideable gump categories, `All = (1UL<<21)-1`. |
| `LayerOrder.cs` | 95 | `UsedLayers[8, Constants.USED_LAYER_COUNT(23)]` — paperdoll/mobile draw order per facing. |
| `Layers.cs` | 95 | `Layer` (0x00–0x1D) and `TooltipLayers` (adds 0xA1/0xA2/0xA3 group pseudo-layers). |
| `LightColors.cs` | 526 | Item-graphic → light colour classification (`GetHue`), shader definitions, `lights.txt`/`lightshaders.txt` load, and `CreateLightTextures` which bakes the 32-step light ramp atlas. |
| `LightShaderData.cs` | 61 | Struct: RGB / hue / per-channel `LightShaderCurve`. |
| `LockedFeatures.cs` | 88 | `LockedFeatureFlags` expansion bits + `LockedFeatures` holder set from packet 0xB9. |
| `MapMessageType.cs` | 44 | Map-gump sub-command enum. |
| `MessageType.cs` | 56 | Speech/message classes; `ChatSystem` and `Damage` are marked TazUO additions. |
| `ModernUIConstants.cs` | 28 | TazUO modern-UI textures resolved through `ExternalImageLoader` + nine-slice border sizes. |
| `MovementSpeed.cs` | 160 | Step delay constants (mount-run 100 ms, mount-walk 200, run 200, walk 400) and `GetPixelOffset` per-direction interpolation. |
| `NotorietyFlag.cs` | 93 | Notoriety enum + `Notoriety.GetHue`/`GetHTMLHue` reading hues out of the current profile. |
| `PopupMenuData.cs` | 111 | Context-menu packet parser (`Parse(ref StackDataReader)`), old and new cliloc forms. |
| `PopupMenuItem.cs` | 50 | Readonly struct for one context-menu entry. |
| `PromptData.cs` | 46 | `ConsolePrompt` enum + `PromptData` struct for server text prompts. |
| `RaceType.cs` | 40 | HUMAN=1, ELF, GARGOYLE. |
| `Reagents.cs` | 68 | 21 reagent ids + `None`. |
| `ServerErrorMessages.cs` | 136 | Four cliloc/string tables keyed by packet id (0x53/0x85/0x27/0x82) with index clamping. |
| `Skill.cs` | 104 | `Lock` enum and `Skill` (name/index/value/base/cap, clickable) plus three static change events. |
| `SpellDefinition.cs` | 742 | The spell record type, `WordToTargettype` power-word index, full-index get/modify, JSON custom-spell load/save (`SpellJson`), name search across all schools. |
| `SpellbookTypes.cs` | 91 | `SpellBookType` enum and `SpellBookDefinition.GetSpellsGroup` (spellID/100 → macro sub-type offset). |
| `SpellsBushido.cs` | 158 | 6 spells, ids 401–406. |
| `SpellsChivalry.cs` | 234 | 10 spells, ids 201–210 (tithing costs). |
| `SpellsMagery.cs` | 993 | 64 spells, ids 1–64, circle names, cached `SpecialReagentsChars`. |
| `SpellsMastery.cs` | 1012 | 45 spells, ids 701–745, `SpellbookIndices`, mastery-group ↔ skill-name mapping. |
| `SpellsMysticism.cs` | 340 | 16 spells, ids 678–693 (keyed 1–16). |
| `SpellsNecromancy.cs` | 327 | 17 spells, ids 101–117. |
| `SpellsNinjitsu.cs` | 186 | 8 spells, ids 501–508. |
| `SpellsSpellweaving.cs` | 298 | 16 spells, ids 601–616. |
| `StaticFilters.cs` | 566 | Per-graphic classification bitfield (cave/stump/hatched/vegetation), generation+load of `cave.txt`/`tree.txt`/`vegetation.txt`, cave-tile black-border baking into the art atlas, and inline `IsRock/IsField/IsFireField/...` predicates. |
| `TextType.cs` | 41 | CLIENT / SYSTEM / OBJECT / GUILD_ALLY. |
| `Waypoints.cs` | 50 | `WaypointsType` enum for the enhanced-map waypoint packet. |

## Types

| Type | File:line | Responsibility |
| --- | --- | --- |
| `Ability` / `AbilityDefinition` / `AbilityData` | Ability.cs:38, :75, :89 | Ability ids and the 32-entry name/icon array sized by `Constants.MAX_ABILITIES_COUNT`. |
| `BuffIcon` | BuffIcon.cs:37 | One active buff; `Timer` is an absolute `Time.Ticks` deadline or `0xFFFF_FFFF`. |
| `BuffIconType` | BuffTable.cs:39 | Server buff id space (0x3E9 base, contiguous thereafter). |
| `BuffTable` | BuffTable.cs:230 | `static ushort[] Table` of gump graphics; `Load()` overlays from buff.txt. |
| `ChairTable` | ChairTable.cs:8 | `public static Dictionary<ushort, SittingInfoData> Table`. |
| `CharacterCreationValues` / `ComboContent` | CharacterCreationValues.cs:38, :246 | Char-creation palettes; `ComboContent` resolves cliloc labels eagerly. |
| `CharacterListFlags` / `ClientFeatures` | ClientFeatures.cs:39, :59 | Server feature negotiation; `SetFlags` derives MaxChars/tooltips/popup. |
| `ClientFlags` | ClientProtocol.cs:38 | Expansion bitmask. |
| `ContainerData` | ContainerData.cs:37 | Container gump layout record. |
| `CustomHouseObject` (+8 subclasses, 2 category types) | CustomHouse.cs:38–499 | House-editor tile definitions; `Contains(graphic)` returns slot index or -1. |
| `Direction` / `DirectionHelper` | Direction.cs:39, :54 | Facing values and conversions. |
| `Flags` | EntityFlags.cs:38 | Mobile status bits. |
| `HideHudFlags` | HideHudFlags.cs:6 | HUD-hiding bitmask (TazUO). |
| `LayerOrder` | LayerOrder.cs:35 | `Layer[8,23]` draw order. |
| `Layer` / `TooltipLayers` | Layers.cs:35, :69 | Equipment slots; tooltip grouping. |
| `LightColors` (+`LightShaderCurve`, `ItemLightData`) | LightColors.cs:40, :511, :521 | Light classification and ramp-texture generation. |
| `LightShaderData` | LightShaderData.cs:37 | RGB + 3 curves per shader id. |
| `LockedFeatureFlags` / `LockedFeatures` | LockedFeatures.cs:38, :80 | Account expansion unlocks. |
| `MessageType` | MessageType.cs:38 | Speech classes (`ChatSystem`, `Damage` fork-added). |
| `ModernUIConstants` | ModernUIConstants.cs:6 | Nine-slice panel/button textures + border sizes. |
| `MovementSpeed` | MovementSpeed.cs:37 | Step timings and sub-tile pixel offsets. |
| `NotorietyFlag` / `Notoriety` | NotorietyFlag.cs:37, :49 | Notoriety hue lookup from the profile. |
| `PopupMenuData` / `PopupMenuItem` | PopupMenuData.cs:38, PopupMenuItem.cs:35 | Context-menu packet model. |
| `ConsolePrompt` / `PromptData` | PromptData.cs:35, :42 | Text-prompt state. |
| `Skill` (+`SkillChangeArgs`) | Skill.cs:45, :96 | Per-skill live value holder; three static change events. |
| `SpellDefinition` | SpellDefinition.cs:44 | Immutable spell record; three constructor overloads with different icon/mana semantics. |
| `SpellJson` | SpellDefinition.cs:724 | JSON DTO for custom spells (`SpellIndex = SpellID + SpellOffset`). |
| `SpellBookType` / `SpellBookDefinition` | SpellbookTypes.cs:35, :48 | Book kinds and macro sub-type offsets. |
| `SpellsMagery` … `SpellsMastery` | one per file, class decl at line 38–40 | Per-school `Dictionary<int, SpellDefinition>` with `GetSpell` / `SetSpell` / `Clear` / `GetAllSpells`. |
| `STATIC_TILES_FILTER_FLAGS` / `StaticFilters` | StaticFilters.cs:48, :57 | Tile classification bitfield and predicates. |
| `WaypointsType` | Waypoints.cs:35 | Map waypoint kinds. |

## State

Mutable/static state owned here:

- `AbilityData.Abilities` — Ability.cs:91. `static readonly` 32-entry array, never mutated.
- `BuffTable._table` — BuffTable.cs:232. Replaced wholesale by `Load()` (:266) or pointed at `_defaultTable` (:270). `_defaultTable` — :274, static array of 190 graphics.
- `ChairTable.Table` — ChairTable.cs:10. **Public mutable static dictionary**, filled only by `Load()` (:50); `_defaultTable` (:55) is written to disk, never inserted directly.
- `CharacterCreationValues.*` palettes — CharacterCreationValues.cs:40–143, all `static readonly`.
- `LightColors._shaderdata` / `_itemlightdata` — LightColors.cs:43, :44. Both mutated at startup: `MakeDefaultShaders` clears and repopulates (:307-354), `BuildLightShaderFiles` writes file-supplied ids (:415), `LoadLights` writes item ids (:475).
- `SpellDefinition.WordToTargettype` — SpellDefinition.cs:57. Static dictionary appended by *every* `SpellDefinition` constructor via `AddToWatchedSpell()` (:217-227) and pruned only in `FullIndexSetModifySpell` (:666, :671).
- `SpellDefinition.EmptySpell` — SpellDefinition.cs:46. Shared singleton returned by every school's miss path.
- `Spells<School>._spellsDict` — SpellsMagery.cs:42, SpellsNecromancy.cs:40, SpellsChivalry.cs:40, SpellsBushido.cs:40, SpellsNinjitsu.cs:40, SpellsSpellweaving.cs:40, SpellsMysticism.cs:40, SpellsMastery.cs:40. Populated in static ctors, mutated by `SetSpell`, emptied by `Clear()`.
- `SpellsMagery._spRegsChars` — SpellsMagery.cs:44. Lazy cache built at :959, invalidated only by `SpellsMagery.SetSpell` (:985).
- `Spells<School>.SpellBookName` — settable static string in every school except Mastery (SpellsMastery.cs:53 is `readonly`).
- `SpellsMastery.SpellbookIndices` — SpellsMastery.cs:43. Static jagged array of page layouts.
- `StaticFilters._filteredTiles` — StaticFilters.cs:59. `STATIC_TILES_FILTER_FLAGS[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT]`, OR-ed during `Load()` (:236, :260, :277).
- `StaticFilters.CaveTiles` / `TreeTiles` — StaticFilters.cs:61, :62. Public static lists appended during `Load()`.
- `Skill.SkillValueChangedEvent` / `SkillBaseChangedEvent` / `SkillCapChangedEvent` — Skill.cs:47-49. Static events; subscribers are never explicitly removed (`PlayerMobile.cs:61` uses a lambda).
- `ClientFeatures.Flags/MaxChars/...` and `LockedFeatures.Flags` — instance state owned by `World`, written from feature packets.

## Timing

- **Process start, once, before the world exists**: `StaticFilters.Load()` (Client.cs:184), `BuffTable.Load()` (Client.cs:186), `ChairTable.Load()` (Client.cs:187) — all after `UOFileManager.Load`, all synchronous file I/O plus file *generation* on first run.
- **Graphics-device init, once**: `LightColors.CreateLightTextures(buffer, LIGHTS_TEXTURE_HEIGHT)` (GameController.cs:190) → `MakeDefaultShaders` + `BuildLightShaderFiles(false)` (writes/reads `lightshaders.txt`), then `LightColors.LoadLights()` (GameController.cs:220, reads `lights.txt`). Note the ordering: the shader atlas is baked **before** item-light overrides are read.
- **Scene load (GameScene.Load)**: `SpellDefinition.LoadCustomSpells()` (GameScene.cs:248) merges `spelldef.json` from the exe dir and the UO dir; `StaticFilters.ApplyCaveTileBorder()` (GameScene.cs:267) does `GetData`/`SetData` round-trips over every art atlas containing a cave tile, on the frame thread. `StaticFilters.CleanTreeTextures()` (GameScene.cs:480) is a no-op (body commented out, StaticFilters.cs:382-392).
- **Per frame, per drawn object**: `StaticFilters.IsTree/IsVegetation/IsCave/IsRock/IsField*` (all `AggressiveInlining`) from `Static.cs`, `Multi.cs`, `ItemView.cs`, `StaticView.cs`, `GameSceneDrawingSorting.cs`. `LightColors.GetHue` from GameScene.cs:687 for every light-emitting object when `UseColoredLights` is on. `Notoriety.GetHue` from `MobileView`, `HealthLinesManager`, `NameOverheadGump`, `Tooltip`. `LayerOrder.UsedLayers` walked when drawing every mobile.
- **Per frame, per sitting check**: `Mobile.IsSitting()` (Mobile.cs:949) walks the whole tile stack (`TPrevious`/`TNext`) and probes `ChairTable.Table` for each item/static/multi within ±1 Z.
- **Per movement step**: `MovementSpeed.TimeToCompleteMovement` / `GetPixelOffset` from `Mobile`, `PlayerMobile`, `BoatMovingManager`; the four step delays (100/200/200/400 ms) set the interpolation window.
- **Per packet**: `PopupMenuData.Parse` (PacketHandlers.cs, 0xBF sub 0x14), `ServerErrorMessages.GetError` (LoginScene + PacketHandlers), `BuffTable.Table[iconID]` lookup on the buff packet (PacketHandlers.cs:5844-5919), `Skill.Invoke*Changed` on skill packet 0x3A (PacketHandlers.cs:2249-2253), `ClientFeatures.SetFlags` / `LockedFeatures.SetFlags` on the feature packets.
- **On demand (UI/scripting)**: `SpellDefinition.TryGetSpellFromName` (linear scan of all 8 school dictionaries, `ToLower()` allocating per candidate), `SpellDefinition.SaveAllSpellsToJson`, `SpellsMastery.GetSpellListByGroupName`, `CharacterCreationValues.Get*` during char creation.

## Inbound

- `Client.cs:184-187` → `StaticFilters.Load`, `BuffTable.Load`, `ChairTable.Load`.
- `GameController.cs:190, :220` → `LightColors.CreateLightTextures`, `LightColors.LoadLights`.
- `GameScene.cs:248, :267, :480, :687` → `SpellDefinition.LoadCustomSpells`, `StaticFilters.ApplyCaveTileBorder`, `StaticFilters.CleanTreeTextures`, `LightColors.GetHue`.
- `Network/PacketHandlers.cs` → `BuffTable.Table` (buff packet), `PopupMenuData.Parse`, `ServerErrorMessages.GetError`, `Skill.InvokeSkill*Changed` (:2249-2253), `DirectionHelper`.
- `Game/GameObjects/Mobile.cs:949` → `ChairTable.Table`; `Mobile.cs`/`PlayerMobile.cs`/`BoatMovingManager.cs` → `MovementSpeed`, `DirectionHelper`.
- Render path (`Static.cs`, `Multi.cs`, `Views/ItemView.cs`, `Views/StaticView.cs`, `GameSceneDrawingSorting.cs`, `AnimatedStaticsManager.cs`, `ClassicUO.Assets/ArtLoader.cs`) → `StaticFilters` predicates.
- Gumps: `SpellbookGump`, `SpellBar/*`, `UseSpellButtonGump`, `CounterBarGump`, `AssistantGump` → `SpellDefinition`, `Spells*`, `SpellBookDefinition`; `UseAbilityButtonGump`, `CombatBookGump` → `AbilityData`; `ContainerGump`/`ContainerManager` → `ContainerData`; `PopupMenuGump` → `PopupMenuData`; `CreateCharAppearanceGump`, `RaceChangeGump` → `CharacterCreationValues`; `BuffGump`/`ImprovedBuffGump`/`CoolDownBar`/`StatusGump` → `BuffIcon`; ~20 gump/control files → `ModernUIConstants` and the `Layer`/`LayerOrder` types.
- `Managers/HouseCustomizationManager.cs` → the whole `CustomHouseObject` family.
- `Managers/HideHudManager.cs`, `AssistantGump` → `HideHudFlags`.
- `Managers/SpellVisualRangeManager.cs`, `MacroManager.cs`, `CommandManager.cs`, `MessageManager.cs`, `Network/OutgoingPackets.cs` → spell definitions and `Notoriety`.
- `LegionScripting/API.cs`, `PyClasses/Buff.cs`, `Expressions.cs` → `BuffIcon`, `Notoriety`.

## Outbound

- `ClassicUO.Utility.TextFileParser` — every text loader (BuffTable.cs:251, ChairTable.cs:34, LightColors.cs:386/448, StaticFilters.cs:226/243/267).
- `ClassicUO.Utility.FileSystemHelper.ReadAllTextShared` — StaticFilters.cs:74; `ClassicUO.Utility.Logging.Log` — StaticFilters.cs:78/99/223/285.
- `CUOEnviroment.ExecutablePath` + `System.IO` — all loaders build `Data/Client/*` paths and create the directory if absent.
- `ClassicUO.Assets`: `ClilocLoader.Instance.GetString` (CharacterCreationValues.cs:254, ServerErrorMessages.cs:89), `TileDataLoader.Instance.StaticData` (StaticFilters.cs:155, :207), `ArtLoader.MAX_STATIC_DATA_INDEX_COUNT` (StaticFilters.cs:59), `AnimationsLoader.SittingInfoData` (ChairTable.cs:4), `ExternalImageLoader.Instance.TryGetEmbeddedTexture` (ModernUIConstants.cs:12/24/25).
- `Client.Game.Arts.GetArt(...).Texture` and XNA `GetData`/`SetData` — StaticFilters.cs:288-311.
- `ClassicUO.Configuration.ProfileManager.CurrentProfile` — NotorietyFlag.cs:55-65; `Settings.GlobalSettings.UltimaOnlineDirectory` — SpellDefinition.cs:160.
- `ClassicUO.Game.Managers.GameActions.Print` — SpellDefinition.cs:205, :607, :611.
- `Utility.JsonHelper.LoadJsonFile` / `SaveJsonFile` — SpellDefinition.cs:172, :605.
- `World.Player` — StaticFilters.cs:489 (`IsOutStamina`).
- `Time.Ticks` — BuffIcon.cs:43.
- `Client.Version` / `ClientVersion.CV_308Z` — ClientFeatures.cs:87.
- `ClassicUO.Resources.ResGeneral` / `ResErrorMessages` — SpellDefinition.cs:381-442, Skill.cs:93, ServerErrorMessages.cs:43-84.

## Hazards

- `LightColors.cs:415` — `BuildLightShaderFiles` inserts any `ushort` id parsed out of `lightshaders.txt` into `_shaderdata`; the "HARD LIMIT IS 63" at :375 is a comment in the generated header, not a check.
- `LightColors.cs:503` — `buffer[32 * (entry.Key - 1) + i]` is written for every `_shaderdata` key with no bound against the caller's buffer (`LIGHTS_TEXTURE_HEIGHT` rows, GameController.cs:190).
- `LightColors.cs:402-407` — `Enum.TryParse` accepts numeric tokens, so a curve can hold a value outside 0–5; `lightCurveTables[(uint)entry.Value.BlueCurve]` at :504-506 indexes a 6-element array unchecked.
- `LightColors.cs:458, :465, :471` — `Convert.ToUInt16`/`ushort.Parse` on `lights.txt` tokens are unguarded; a malformed line throws out of `LoadLights` (GameController.cs:220) during device init.
- `LightColors.cs:466` — `entry.Color--` on a hue of `0` wraps to `65535`.
- `GameController.cs:190` vs `:220` — the light ramp atlas is baked from `_shaderdata` before `LoadLights` populates `_itemlightdata`; a shader id referenced only by `lights.txt` has no baked ramp.
- `ChairTable.cs:50` — `Table.Add` (not indexer) throws `ArgumentException` if `chair.txt` contains a duplicate graphic; the file is user-editable and generated on first run.
- `ChairTable.cs:42-48` — every `TryParse` return value is discarded; an unparsable line is inserted as graphic `0` with all-zero directions.
- `ChairTable.cs:10` — `Table` is a `public static` mutable dictionary read by `Mobile.IsSitting` (Mobile.cs:949) on the frame thread; nothing guards concurrent mutation.
- `ChairTable.cs:23-32` — the 190 `_defaultTable` entries only reach `Table` by way of the file; if the write fails or the file is truncated, `Table` is silently smaller.
- `BuffTable.cs:266` — `_table` is replaced by whatever `buff.txt` yields; an empty or short file leaves `Table.Length` below the buff id space, and `PacketHandlers.cs:5844` then silently drops those buffs.
- `BuffTable.cs:234` — `Table` is null until `Load()` runs (Client.cs:186).
- `BuffIcon.cs:43` — `Timer` mixes an absolute `Time.Ticks + timer*1000` deadline with the sentinel `0xFFFF_FFFF`; the sentinel is a real tick value the client reaches after ~50 days of uptime.
- `BuffIcon.cs:48-51` — `Equals` compares `Type` only, so two buffs of the same type with different timers/text are interchangeable in any list lookup.
- `SpellDefinition.cs:135-138` — `Equals(SpellDefinition other)` dereferences `other.ID` with no null check, while the miss path of every school returns the shared `EmptySpell`.
- `SpellDefinition.cs:221` — `AddToWatchedSpell` writes `WordToTargettype[PowerWords] = this` from the constructor; identical power words across schools overwrite silently, and `LoadSpellsFromFile` (:176) never removes the replaced spell's old keys the way `FullIndexSetModifySpell` (:666, :671) does.
- `SpellDefinition.cs:507` vs `:715` — Mysticism read uses `(fullidx - 77) % 100`, write uses `id - 77`; the two index transforms are not inverses.
- `SpellDefinition.cs:689-720` — `FullIndexSetModifySpell` builds the record with `fullidx` as its `ID` but stores it under the separate `id` parameter, so key and `ID` can disagree.
- `SpellDefinition.cs:176` — custom-spell load ignores the target dictionary's existing key, and runs twice (exe dir then UO dir, :157/:163), second file winning.
- `SpellsMagery.cs:959` — `_spellsDict.Max(o => o.Key)` throws on an empty dictionary; `SpellsMagery.Clear()` (:991) makes that state reachable.
- `SpellsMagery.cs:985` — only Magery invalidates its cache in `SetSpell`; the array is sized to the old max, so a custom spell keyed above it is still reachable only after another invalidation.
- `SpellsMastery.cs:736` — `int div = (MaxSpellCount * 3) >> 3;` derives skill grouping from the live dictionary count, so loading custom mastery spells shifts every `GetUsedSkillName` result; the switch has no `case 2`, and :767-777 is unreachable after the switch's default fallthrough.
- `StaticFilters.cs:398, :422, :428` — `_filteredTiles[g]` is indexed by a raw `ushort` graphic with no bound against `ArtLoader.MAX_STATIC_DATA_INDEX_COUNT`; these are the per-frame render predicates.
- `StaticFilters.cs:168` — `tree.txt` generation opens `vegetation.txt` in **append** mode, so a missing tree file with a present vegetation file appends duplicates on every such start.
- `StaticFilters.cs:237, :261` — `CaveTiles`/`TreeTiles` are appended without clearing; a second `Load()` doubles them.
- `StaticFilters.cs:288-311` — `ApplyCaveTileBorder` allocates a full `uint[]` per atlas and does GetData/SetData synchronously from `GameScene.Load` (GameScene.cs:267).
- `StaticFilters.cs:489` — `IsOutStamina` dereferences `World.Player` with no null check.
- `StaticFilters.cs:526-563` — dead `AddBlackBorder` indexes `pixels[currentY * width + currentX]` where `currentY`/`currentX` can be `-1` at the edges.
- `NotorietyFlag.cs:55-65` — `ProfileManager.CurrentProfile` is dereferenced with no null check from render-path callers.
- `PopupMenuData.cs:57-107` — `count` from the packet sizes the array and drives the read loop with no length validation against the remaining buffer.
- `EntityFlags.cs:43-44` — `Poisoned` and `Flying` are both `0x04`, so the two states are indistinguishable through this enum.
- `Skill.cs:47-49` — static events with no unsubscribe path; `PlayerMobile.cs:61` attaches a lambda, so the handler outlives each `PlayerMobile`.
- `CharacterCreationValues.cs:254` — `ComboContent` calls `ClilocLoader.Instance.GetString` in its constructor; the label array is snapshotted at that moment.
- `SpellDefinition.cs:229-369` — `TryGetSpellFromName` scans all eight dictionaries and allocates two lowered strings per candidate on every call; it is exposed to scripting.

## Fork deltas

Clearly not stock ClassicUO:

- `HideHudFlags.cs` — whole file. No license header, file-scoped namespace, TazUO gump categories (`GridContainers`, `NearbyCorpseLoot`, `ScriptManagerGump`, `SpellBar`, `DurabilityTracker`).
- `ModernUIConstants.cs` — whole file. TazUO modern-UI nine-slice textures (`TUOGumpBg.png`, `TUOUIButtonUp/Down.png`) via `ExternalImageLoader`.
- `MessageType.cs:53-54` — `ChatSystem` and `Damage` explicitly commented "TazUO Addition".
- `Skill.cs:47-49, :78-89, :96-103` — the three static change events and `SkillChangeArgs` are a fork addition (consumed by `PlayerMobile.cs:61` and the skill UI).
- `SpellDefinition.cs:152-215, :229-369, :513-613, :724-742` — custom-spell JSON pipeline (`LoadCustomSpells`, `LoadSpellsFromFile`, `TryGetSpellFromName`, `GetAllSpells`, `SaveAllSpellsToJson`, `SpellJson`). Uses `System.Text.Json` attributes and repo-convention `JsonHelper`.
- `SpellDefinition.cs:516-525` — collection-expression spread (`[.. x, .. y]`), a C# 12 construct sitting in a `net472` project.
- Every `Spells<School>` file — `SpellBookName` settable property, `GetAllSpells`, `MaxSpellCount`, and `Clear()` are fork surface supporting the JSON pipeline and scripting.
- `Layers.cs:69-95` — `TooltipLayers` including the `Body_Group`/`Jewelry_Group`/`Weapon_Group` pseudo-layers (TazUO tooltip/grid-highlight feature).
- `StaticFilters.cs:64-82, :88-100, :110-224` — `ReadFilterFile`, the try/catch around directory and file generation, and the "two clients starting together" comments are Holiday-Edition hardening (merge `f9f0829` "claude/never-silent"); stock CUO throws here.
- `StaticFilters.cs:283-378` — `ApplyCaveTileBorder`/`ApplyBorderToAtlasRegion` rewritten for the texture-atlas renderer; the original per-texture path survives as dead `AddBlackBorder` (:526) and the commented-out `CleanTreeTextures` (:382-392).
- `StaticFilters.cs:491-524` — `isHumanAndMonster` under a `// ## BEGIN - END ## // MISC2` marker, with commented-out `World.Mobiles` scanning; imported from another CUO fork's patch scheme, not TazUO style.
- `LightColors.cs:44, :46-57, :421-478, :521-525` — the `lights.txt` item-light override file and `ItemLightData` (hue-or-shader per item graphic); `BuildLightShaderFiles` + `lightshaders.txt` + the `LightShaderCurve` set likewise look fork-added on top of stock's fixed switch.
- `CharacterSpeedType.cs` — small enum with no stock consumer visible in this partition; fork movement-speed handling.
- `BuffIcon.cs:39, :61` — the `title` constructor parameter and `Title` field (used by the TazUO improved buff gump).
- `Waypoints.cs` — enhanced-client waypoint enum, used by the TazUO world map.
