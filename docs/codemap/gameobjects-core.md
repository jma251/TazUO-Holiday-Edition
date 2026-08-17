# gameobjects-core

Partition = `src/ClassicUO.Client/Game/GameObjects/*.cs` (maxdepth 1), 21 files, 10135 lines,
all read in full. Branch checked out: `claude/new-chat-session-0zuwmx` (a `legacy` descendant).

Several classes here are `partial`; the other halves live in
`Game/GameObjects/Views/` (`ItemView.cs`, `MobileView.cs`, `LandView.cs`, `StaticView.cs`,
`MultiView.cs`, `GameEffectView.cs`, `LightningEffectView.cs`, `View.cs`) and are **not** in
this partition. Fields referenced here but declared there: `AllowedToDraw`, `AlphaHue`,
`IsFlipped`, `FrameInfo`, `ObjectHandlesStatus`, `_canBeTransparent`, `IsHousePreview`,
`Draw(...)`, `DrawStatic(...)`, `CheckMouseSelection()` overrides.

---

## Files

| Path (relative to `src/ClassicUO.Client/Game/GameObjects/`) | Lines | Purpose |
| --- | --- | --- |
| `GameObject.cs` | 532 | Root of the world-object hierarchy: tile linkage, screen position, overhead text, destroy. |
| `Entity.cs` | 341 | Serial-bearing object (Item/Mobile): hits, flags, direction, name, child-item search. |
| `EntityCollection.cs` | 63 | `DictExt` extension methods `Get`/`Contains`/`Add` over `Dictionary<uint,T>` keyed by serial. |
| `Item.cs` | 900 | Item entity + pool, multi/house loading from `multi.mul`/`MultiCollection.uop`, corpse anim. |
| `Mobile.cs` | 1171 | Mobile entity + pool, step queue playback, footstep sound, idle animation, sitting lookup. |
| `MobileAnimation.cs` | 2103 | Static translation of server anim ids -> local animation groups (`GetGroupForAnimation`, `GetObjectNewAnimation*`). |
| `PlayerMobile.cs` | 2056 | The local player: skills, stats, buffs, weapon-ability table, walk request path, gump culling. |
| `Multi.cs` | 121 | House/boat component object + pool, season graphic, custom-house state flags. |
| `Static.cs` | 118 | Map static object + pool, season graphic. |
| `Land.cs` | 263 | Land tile + pool, stretch/normal calculation from neighbour Z. |
| `House.cs` | 204 | Collection of `Multi` components for one multi serial; generate/clear. |
| `GameEffect.cs` | 219 | Base for visual effects: anim-frame stepping, duration, source/target. |
| `FixedEffect.cs` | 105 | Effect glued to a source position/object. |
| `MovingEffect.cs` | 256 | Effect travelling source->target in screen space, spawns explosion on arrival. |
| `DragEffect.cs` | 150 | Effect drifting +8/+8 px every 20 ms; has its own `Draw`. |
| `LightningEffect.cs` | 84 | 10-frame lightning at a source, 400 ms duration. |
| `IsometricLight.cs` | 104 | Global light level model (personal/overall/height). |
| `TextObject.cs` | 140 | Pooled overhead-text node; doubly-linked (`DLeft`/`DRight`) into `TextRenderer`. |
| `EntityTextContainer.cs` | 296 | `TextContainer` (max 5 messages per object) and `OverheadDamage` (damage numbers). |
| `RenderedText.cs` | 748 | Pooled font-rendered texture (`ClassicUO.Game` namespace, not `.GameObjects`). |
| `LineOfSightHelper.cs` | 161 | Bresenham LOS between two `GameObject`s using `Pathfinder` tile queries. |

---

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `BaseGameObject : LinkedObject` | GameObject.cs:45 | Adds `RealScreenPosition`. Base for `GameObject` and `TextObject`. |
| `GameObject` (abstract, partial) | GameObject.cs:50 | X/Y/Z, `Graphic`/`Hue` with replacement hooks, `Offset`, tile list links `TNext`/`TPrevious`, `TextContainer`, `Destroy()`. |
| `Entity` (abstract) | Entity.cs:52 | `Serial`, `Hits`/`HitsMax`/`HitsPercentage`, `Flags`, `Direction`, `AnimIndex`, child-item queries. |
| `HitsRequestStatus` enum | Entity.cs:45 | None/Pending/Received; drives `SendCloseStatus` on destroy. |
| `DictExt` (static) | EntityCollection.cs:38 | Serial-keyed dictionary helpers used by `World.Items` / `World.Mobiles`. |
| `Item : Entity` (partial) | Item.cs:50 | Ground/container item, corpse, or multi container. |
| `Mobile : Entity` (partial) | Mobile.cs:46 | Creature/player; owns `Deque<Step> Steps`. |
| `Mobile.Step` struct | Mobile.cs:1162 | `X,Y` int, `Z` sbyte, `Direction` byte, `Run` bool. |
| `PlayerMobile : Mobile` | PlayerMobile.cs:47 | Local player; `Skills[]`, `Abilities[2]`, `Walker`, buff dictionary. |
| `Multi : GameObject` (sealed partial) | Multi.cs:41 | One house/boat piece. |
| `Static : GameObject` (sealed partial) | Static.cs:40 | One map static. |
| `Land : GameObject` (sealed partial) | Land.cs:43 | One land tile, with `YOffsets` + 4 normals. |
| `House : IEquatable<uint>` | House.cs:43 | `List<Multi> Components`, `Bounds`, `IsCustom`, `Revision`. |
| `GameEffect : GameObject` (abstract partial, internal) | GameEffect.cs:42 | Effect lifetime + `AnimDataFrame` stepping. |
| `FixedEffect` / `MovingEffect` / `DragEffect` / `LightningEffect` | FixedEffect.cs:38 / MovingEffect.cs:39 / DragEffect.cs:42 / LightningEffect.cs:37 | Concrete effects. |
| `IsometricLight` | IsometricLight.cs:37 | Light level; `IsometricLevel = max(Personal, 32-Overall) * 0.03125`. |
| `TextObject : BaseGameObject` | TextObject.cs:42 | Pooled overhead message with a `TextBox`. |
| `TextContainer : LinkedObject` | EntityTextContainer.cs:44 | Per-GameObject message list, `MaxSize = 5`. |
| `OverheadDamage` (internal) | EntityTextContainer.cs:85 | Up to 10 floating damage numbers per parent, 1500 ms each. |
| `RenderedText` (sealed) | RenderedText.cs:62 | Pooled `Texture2D` of rendered text + hit map. |
| `FontStyle` enum | RenderedText.cs:47 | Font style flags. |
| `LineOfSightHelper` (static) | LineOfSightHelper.cs:9 | `IsVisible(observer, target)`. |
| `LineOfSightHelper.Point3D` (readonly struct) | LineOfSightHelper.cs:153 | Bresenham sample point. |

---

## State

Pools (all `QueuedPool<T>`, process-wide statics; the lambda is the **reset on hand-out**, not on return):

- `Item._pool` — Item.cs:52, capacity `Constants.PREDICTABLE_CHUNKS * 3`; reset block Item.cs:54-106.
- `Item._inPool` — Item.cs:333, guards double-return.
- `Mobile._pool` — Mobile.cs:48, capacity `Constants.PREDICTABLE_CHUNKS`; reset block Mobile.cs:50-110.
- `Mobile._inPool` — Mobile.cs:1160.
- `Multi._pool` — Multi.cs:43, `Constants.PREDICTABLE_MULTIS`; returned **inside** `Destroy()` (Multi.cs:118).
- `Static._pool` — Static.cs:42, `Constants.PREDICTABLE_STATICS`; returned inside `Destroy()` (Static.cs:116).
- `Land._pool` — Land.cs:45, `Constants.PREDICTABLE_TILE_COUNT`; returned inside `Destroy()` (Land.cs:86).
- `TextObject._queue` — TextObject.cs:44, capacity 1000; returned inside `Destroy()` (TextObject.cs:104).
- `RenderedText._pool` — RenderedText.cs:64, capacity 3000; returned inside `Destroy()` (RenderedText.cs:745).

Other static/global state:

- `Entity._hitsPercText` — Entity.cs:54: `RenderedText[101]`, one shared texture per whole-percent value, shared across every entity in the world. Filled lazily in `UpdateHits` (Entity.cs:142-162).
- `Mobile._animationIdle` — Mobile.cs:113: `byte[3,3]` idle group table.
- `Mobile.HANDS_BASE_ANIMID` / `HAND2_BASE_ANIMID` — MobileAnimation.cs:43 / :67.
- `Item._mounts` — Item.cs:660: `Dictionary<ushort,ushort>` mount graphic -> anim body.
- `Multi.ForcedTransparency` — Multi.cs:74: `static byte = 40`, written from `House.Add` (House.cs:94) out of the profile.
- `RenderedText._picker` — RenderedText.cs:73: static `PixelPicker` hit-map cache keyed by a hash of text^hue^align^style^font^unicode (RenderedText.cs:279-288, :653-661).

Per-instance mutable state worth naming:

- `GameObject.TileChunk` / `TileCellX` / `TileCellY` — GameObject.cs:192,196,197: which chunk cell this object is linked into. **Holiday addition** (see Fork deltas).
- `GameObject.LastDrawnTime` — GameObject.cs:195, diagnostic only.
- `GameObject._averageOverTime` — GameObject.cs:56: 15-second rolling DPS window, created on first `AddDamage`, nulled in `Destroy` (GameObject.cs:457).
- `GameObject.graphic` / `originalGraphic` / `hue` — GameObject.cs:135.
- `Mobile.Steps` — Mobile.cs:150: `Deque<Step>` capped at `Constants.MAX_STEP_COUNT`; the client's own prediction of position, the server is authoritative.
- `Mobile._animationGroup` (0xFF = none), `_animationInterval`, `_animationRepeateMode`, `_animationRepeatModeCount`, `_animationRepeat`, `_isAnimationForwardDirection`, `_lastAnimationIdleDelay` — Mobile.cs:132-140.
- `Item.WantUpdateMulti` — Item.cs:253: latch consumed by `CheckGraphicChange` -> `LoadMulti`.
- `Item.Container` — Item.cs:242, default `0xFFFF_FFFF`; `OnGround` is `!IsValid(Container)`.
- `PlayerMobile._buffIcons` — PlayerMobile.cs:49.
- `PlayerMobile.AutoOpenedCorpses` / `ManualOpenedCorpses` — PlayerMobile.cs:96,97: `HashSet<uint>` that is **never cleared** anywhere in this file.
- `PlayerMobile.Walker` — PlayerMobile.cs:86: `WalkerManager` with `StepInfos[]`, `WalkSequence`, `StepsCount`.
- `House.Components` — House.cs:53.

---

## Timing

**Per frame** (driven from `GameScene.Update` -> `World.Update`, World.cs:350 and :424):

- `Mobile.Update()` for every mobile (Mobile.cs:281) -> `Entity.Update` -> idle check -> `ProcessAnimation(true)`.
- `Item.Update()` for every item (Item.cs:648) -> `Entity.Update` -> `ProcessAnimation()` (corpses only).
- `Entity.Update` (Entity.cs:170) recomputes `HitsPercentage` from `Hits/HitsMax` every frame when `HitsMax > 0`, and on `ObjectHandlesStatus == OPEN` sends `Send_NameRequest` and adds a `NameOverheadGump`.
- `GameEffect.Update` (GameEffect.cs:106) via `EffectManager.Update` (World.cs:479).
- `OverheadDamage.Update` (EntityTextContainer.cs:149) via the text managers.
- `GameObject.UpdateRealScreenPosition` (GameObject.cs:256) is called by the draw pass; it clears `IsPositionChanged` and recomputes overhead-text coords.

**Timers / intervals seen in this partition:**

| Interval | Where |
| --- | --- |
| distance-cull sweep every **50 ms** | World.cs:338 (`_timeToDelete`), condemns objects `Distance > ClientViewRange`. |
| `Constants.CHARACTER_ANIMATION_DELAY` per animation frame | Mobile.cs:615/720, Item.cs:895. |
| idle animation after **30 s + rand(0..30 s)** | Mobile.cs:277-278 `CalculateRandomIdleTime`. |
| footstep sound: 400 ms walk / 150 ms mounted-run / 350 ms mounted-walk, then `*13/10` | Mobile.cs:582-598. |
| step playback: `MovementSpeed.TimeToCompleteMovement(run, mounted)`, divided by `Steps.Count` when >1 | Mobile.cs:783-788. |
| step overflow: `Constants.MAX_STEP_COUNT` (EnqueueStep refuses past it) | Mobile.cs:306. |
| `Constants.WALKING_DELAY` / `PLAYER_WALKING_DELAY` define `IsWalking` | Mobile.cs:215, PlayerMobile.cs:81. |
| drag effect nudges +8/+8 px every **20 ms** | DragEffect.cs:98-101. |
| lightning: 10 frames, `duration = 400 ms` | LightningEffect.cs:40,61. |
| explosion effect duration 400 ms | GameEffect.cs:170. |
| moving-effect speed is frame-independent: `IntervalInMs * Time.Delta` | MovingEffect.cs:135. |
| damage numbers rise 1 px every **25 ms**, live 1500 ms, max 10 queued | EntityTextContainer.cs:87,139,143. |
| DPS window 15 s | GameObject.cs:141. |
| overhead messages per object capped at 5 | EntityTextContainer.cs:47. |

**Per packet / on demand:** `Item.CheckGraphicChange` (Item.cs:607) and `Mobile.CheckGraphicChange` (Mobile.cs:1054) are called by packet handlers after graphic/direction updates; `Item.LoadMulti` (Item.cs:335) runs from there and does synchronous file I/O + zlib decompression on the frame thread. `House.Generate` / `ClearComponents` run on custom-house packets. `PlayerMobile.Walk` / `WalkNotAvoid` run on input and send `Send_WalkRequest` immediately.

**On load:** `PlayerMobile` ctor builds `Skills[]` from `SkillsLoader` (PlayerMobile.cs:53).

---

## Inbound

- `World.Update` (World.cs:350, :424) — per-frame `Update()` on every mobile then every item; also the only caller of `Item.ReturnToPool` / `Mobile.ReturnToPool` (World.cs:407, :472, :569, :593, :683, :716).
- `World.GetOrCreateItem` / `GetOrCreateMobile` (World.cs:558, :583) — call `Item.Create` / `Mobile.Create`.
- `Network/PacketHandlers.cs` — sets `Graphic`, `Hue`, `Flags`, `Hits`, `Direction`; calls `EnqueueStep`, `SetAnimation`, `CheckGraphicChange`, `AddMessage`, `House.Generate`, `House.ClearComponents`, `HouseDiagnostics.LogHouseItemArrived` (PacketHandlers.cs:6802).
- `Game/Map/Chunk.cs` — `AddGameObject` sets `TileChunk`/`TileCellX`/`TileCellY`; `Land.Create`, `Static.Create` on chunk load.
- `Game/Scenes/GameSceneDrawingSorting.cs` + `Views/*` — walk `TNext`/`TPrevious`, read `RealScreenPosition`, `Offset`, `PriorityZ`, `FoliageIndex`, `RenderListNext`.
- `Game/Managers/*` — `EffectManager` (creates/removes effects), `HouseManager` (`House` lifetime, `RememberFootprint`), `MessageManager.CreateMessage`, `CorpseManager`, `BoatMovingManager`, `TargetManager`.
- `LegionScripting` / Python API — the `#region Python API accessors` block at Mobile.cs:217-222 (`IsAttackable`, `HitsDiff`, `StamDiff`, `ManaDiff`) exists only for it; `ScriptRecorder.Instance.UpdatePlayerPosition` is called from `PlayerMobile.OnPositionChanged` (PlayerMobile.cs:1427).
- UI gumps read `Entity.Name`, `HitsTexture`, `PlayerMobile.Skills`, `BuffIcons`.

## Outbound

- `ClassicUO.Assets`: `TileDataLoader.Instance.StaticData` / `LandData` (Item.cs:232, Multi.cs:65, Static.cs:57, Land.cs:58), `AnimationsLoader.Instance`, `MultiLoader.Instance` (Item.cs:381-508), `TexmapsLoader` (Land.cs:121), `AnimDataLoader` (GameEffect.cs:54), `FontsLoader` (RenderedText.cs:147+), `SkillsLoader` (PlayerMobile.cs:53).
- `Client.Game.Animations` (`GetAnimationFrames`, `AnimationExists`, `GetAnimType`, `GetAnimFlags`, `ConvertBodyIfNeeded`, `GetAnimationDimensions`), `Client.Game.Arts`, `Client.Game.Audio.PlaySoundWithDistance` (Mobile.cs:597), `Client.Game.Scene.Camera.WorldToScreen`, `Client.Game.FrameDelay[1]` (Mobile.cs:793), `Client.Game.GraphicsDevice` (RenderedText.cs:675).
- `World.*`: `World.Map.GetChunk` / `GetTile` / `GetTileZ`, `World.Items` / `World.Mobiles` / `World.Get`, `World.HouseManager`, `World.CorpseManager`, `World.CustomHouseManager`, `World.WorldTextManager`, `World.Journal`, `World.OPL`, `World.RemoveMobile` (Mobile.cs:701,711,717), `World.RemoveItem` (PlayerMobile.cs:1507).
- Network: `NetClient.Socket.Send_NameRequest` (Entity.cs:182), `Send_WalkRequest` (PlayerMobile.cs:1837, :2015), `GameActions.SendCloseStatus` (Entity.cs:202), `GameActions.DoubleClickQueued` (PlayerMobile.cs:1454), `GameActions.OpenDoor` (PlayerMobile.cs:1475).
- UI: `UIManager.Add` / `GetGump<T>()?.Dispose()` — Entity.cs:185, Item.cs:276-289, Mobile.cs:1139-1140, PlayerMobile.cs:1515-1517, :1544 etc.
- Managers: `GraphicsReplacement.Replace` / `ReplaceHue` (GameObject.cs:112,123), `SeasonManager` (Multi.cs:106, Static.cs:103, Land.cs:91), `StaticFilters.IsVegetation`, `Pathfinder.GetNewXY` / `CanWalk` / `GetAllObjectsAt` / `ObjectBlocksLOS` / `_listPool`, `EventSink.Invoke*` (Entity.cs:73, PlayerMobile.cs:277,289,1432), `Plugin.UpdatePlayerPosition` (PlayerMobile.cs:1422), `UoAssist.SignalAddMulti` (Item.cs:635), `BoatMovingManager.ClearSteps` (Item.cs:604), `HouseDiagnostics.LogHouseItemDestroyed` / `LogMultiRebuild` (Item.cs:272, :352), `GameScene.UpdateMaxDrawZ` (Item.cs:601).

---

## Hazards

- **GameObject.cs:107-117** — `Graphic` setter assigns `originalGraphic = value` *before* `GraphicsReplacement.Replace`, then does `Hue = hue;` (re-running `ReplaceHue` against the new original) and finally `OnGraphicSet`. Setting the same graphic twice runs hue replacement twice on an already-replaced hue.
- **GameObject.cs:465-470** — `Destroy()` writes `Hue = 0` and `Graphic = 0` through the property setters, so `GraphicsReplacement.Replace/ReplaceHue` and the virtual `OnGraphicSet` run on an object mid-teardown (`Mobile.OnGraphicSet` recomputes `IsHuman`/`IsGargoyle` at Mobile.cs:248).
- **GameObject.cs:145-148** — `GetCurrentDPS()` dereferences `_averageOverTime` with no null check; the field is only created by `AddDamage` (line 141) and is nulled in `Destroy` (line 457). Called unconditionally from `OverheadDamage.Add` at EntityTextContainer.cs:132 when `ShowDPS` is on — that path calls `Parent.AddDamage` first, other callers may not.
- **GameObject.cs:226-231** — `RemoveFromTile` hands the chunk cell to `TNext ?? TPrevious`. Correct only if `TileChunk`/`TileCellX/Y` were kept in step by `Chunk.AddGameObject`; nothing here validates that `chunk.Tiles[TileCellX,TileCellY]` still belongs to the same chunk after a chunk unload.
- **GameObject.cs:293-333 / Item.cs:783-846 / Mobile.cs:970-1049** — `UpdateTextCoordsV` walks the text list to the tail then back via `Previous`, skipping entries whose `TextBox` is disposed; `offY` accumulation depends on the first non-expired entry being reached, so an expired newest message silently shifts every older one.
- **Entity.cs:54,107,142** — `_hitsPercText` is one static 101-slot array shared by every entity. `HitsTexture` returns the slot without a null check (a percentage never passed through `UpdateHits` yields `null`), and a `RenderedText.Destroy()` elsewhere returns that instance to the pool while the array still points at it (`IsDestroyed` is re-checked only on the next `UpdateHits` for that same percentage).
- **Entity.cs:189-195** — `Hits`/`HitsMax` are server-authoritative but `HitsPercentage` is recomputed locally every frame; a server-sent percentage packet and this computation both write the same field.
- **Entity.cs:202** — `Destroy()` sends a network packet (`SendCloseStatus`) from inside the per-frame world sweep.
- **Entity.cs:196-265 / 267-295** — `FindItem` and `GetItemByGraphic(deepsearch:true)` recurse over `Items`; `GetItemByGraphic` at line 280 restarts from `Items` (the *outer* list) inside the deep-search branch rather than from `item.Items`, so it re-walks the same siblings.
- **Item.cs:196-208 / 217-229** — `RootContainer` / `BackpackOrRootContainer` follow `Container` with no cycle guard; a container chain that loops never terminates.
- **Item.cs:232** — `ItemData` returns `ref StaticData[IsMulti ? MultiGraphic : Graphic]` with no bounds check on the index.
- **Item.cs:52-106** — the pool reset does not clear `Serial`, `TextContainer` (only `Clear()`s it), `MultiDistanceBonus` is reset but `MultiGraphic` is set to 0 while `_displayedGraphic` is nulled separately; a reused Item keeps its old `TextContainer` instance.
- **Item.cs:321-331 / Mobile.cs:1150-1158** — `ReturnToPool` is the only safe hand-back point and is guarded by `_inPool`; `Destroy()` deliberately does not pool. Any new call site that pools from `Destroy` re-opens the serial-reuse window described in the comment at Item.cs:299-320.
- **Item.cs:379** — `WantUpdateMulti = false` must stay *after* `house.ClearComponents()` (which sets it back to `true` at House.cs:185); moving it earlier makes a custom house rebuild every frame.
- **Item.cs:381-505** — `LoadMulti` does synchronous file seek + `ZLib.Decompress` + `stackalloc`/`ArrayPool` on the frame thread, and creates one `Multi` per block (hundreds for a house).
- **Item.cs:575-579** — `foreach (Multi m in house.Components)` immediately after the loop that added to that same list; safe here only because nothing else mutates `Components` during the loop.
- **Item.cs:599-601** — `LoadMulti` calls `GameScene.UpdateMaxDrawZ(true)` mid-packet-handling, i.e. draw state is mutated outside the draw pass.
- **Item.cs:855-896** — corpse animation gates on `LastAnimationChangeTime < Time.Ticks`; `AnimIndex` is clamped to `frames.Length - 1` so a corpse stops, but `LastAnimationChangeTime` keeps being pushed forward every tick forever.
- **Mobile.cs:98** — the pool reset calls `RemoveFromTile()` on hand-**out**, not on return; between `Destroy()` and the next `GetOne()` the object is still linked into a chunk cell (`Destroy` also calls it at GameObject.cs:459, so this is belt-and-braces).
- **Mobile.cs:329-359** — `EnqueueStep` reuses one `Step` local across up to three `AddToBack` calls; each push copies the struct, but the field writes are interleaved (`step.X` set after the first push in the `moveDir != endDir` branch).
- **Mobile.cs:785-788** — step playback is accelerated by dividing `stepTime` by `Steps.Count`; drawn position is the queue front while the server position is the queue back, so the two disagree by up to `MAX_STEP_COUNT` tiles.
- **Mobile.cs:793** — `maxDelay` reads `Client.Game.FrameDelay[1]`, coupling movement interpolation to the frame-rate cap.
- **Mobile.cs:856-883** — indexes `World.Player.Walker.StepInfos[World.Player.Walker.CurrentWalkSequence]` with no bounds check, and shifts the array in place while `StepsCount` is decremented.
- **Mobile.cs:901-914** — `Steps.RemoveFromFront()` then `AddToTile()` only `if (TNext != null || TPrevious != null)`: an object that is the sole occupant of its cell (both links null) is **not** re-added to the tile after moving.
- **Mobile.cs:904-909** — `ProcessSteps` recurses into itself on a pure direction change; depth is bounded only by how many consecutive same-tile steps are queued.
- **Mobile.cs:698-718** — `ProcessAnimation` calls `World.CorpseManager.Remove` + `World.RemoveMobile(Serial)` for serials with bit 31 set, from inside `World.Update`'s `foreach` over `World.Mobiles`. Safe only because `RemoveMobile(serial)` defaults `forceRemove:false` and does not touch the dictionary (World.cs:688); `RemoveItem` on its equipment is called in the same call chain.
- **Mobile.cs:937-956** — `TryGetSittingInfo` walks the tile list from `TPrevious` back to head then forward through `TNext`; if `RemoveFromTile` left the chunk pointing at a detached object this walk sees nothing.
- **Mobile.cs:1129-1144** — `Mobile.Destroy` computes `serial = Serial & 0x3FFFFFFF` before `base.Destroy()` and uses it for gump lookup; `PlayerMobile` is excluded from pooling here and in `ReturnToPool` (line 1153).
- **PlayerMobile.cs:96-97** — `AutoOpenedCorpses` / `ManualOpenedCorpses` grow without bound; nothing in this file removes from them.
- **PlayerMobile.cs:1449-1456** — `TryOpenCorpses` iterates `World.Items.Values` and calls `GameActions.DoubleClickQueued`, from `OnPositionChanged`, which is reached from `Mobile.ProcessSteps` (Mobile.cs:889) inside `World.Update`'s item/mobile sweep.
- **PlayerMobile.cs:1473** — `TryOpenDoors` runs a LINQ `Any` over every item in the world on every position and every direction change.
- **PlayerMobile.cs:1526-1531** — `CloseRangedGumps`: `if (UIManager.Gumps.Count > i) continue;` means the loop body is unreachable for every in-range index — the whole method is a no-op as written, and `Gumps.ElementAt(i)` would be O(n) on a `LinkedList` if it ever ran.
- **PlayerMobile.cs:1501-1512** — `CloseBank` walks `bank.Items` calling `World.RemoveItem(first, true)` (force), which removes from `World.Items` and pools the item, while holding `next` captured before the call.
- **PlayerMobile.cs:1803-1811** — `if (Walker.StepsCount == -1) Walker.StepsCount = 1;` then indexes `Walker.StepInfos[Walker.StepsCount]`; the `-1` case writes to index 1 and leaves index 0 unset. `WalkNotAvoid` (line 1988) has no such guard.
- **PlayerMobile.cs:337-346** — `TileDataLoader.Instance.StaticData[equippedGraphic ± 1]` with no bounds check.
- **PlayerMobile.cs:1674-1860 vs 1890-2054** — `Walk` and `WalkNotAvoid` are near-duplicate bodies; only `Walk` has the obstacle-avoidance prologue and the `StepsCount == -1` guard.
- **House.cs:109-140** — `ClearCustomHouseComponents` calls `component.Destroy()` (which returns the `Multi` to the pool immediately, Multi.cs:118) and only then checks `IsDestroyed` to `RemoveAt(i--)`; a pooled-and-reissued `Multi` would report `IsDestroyed == false` and stay in the list.
- **House.cs:149-174** — `Generate` iterates `Components` without checking `IsDestroyed`, and calls `SetInWorldTile` on each.
- **House.cs:188-199** — `ClearComponents` destroys and removes in the same loop with `i--`; the `removeCustomOnly` `continue` at line 192-195 skips without decrementing, which is correct, but the destroyed `Multi` is pooled before it leaves the list.
- **House.cs:92-95** — `World.Map.GetTile(x, y)` result is used without a null check, and `Multi.ForcedTransparency` (a static) is rewritten per component added.
- **Multi.cs:110-119 / Static.cs:108-117 / Land.cs:78-87** — these three pool from inside `Destroy()`, unlike `Item`/`Mobile`. Any surviving reference (e.g. `House.Components`, a chunk tile list, an effect's `Source`) points at an object that may already have been handed out again.
- **GameEffect.cs:111-116** — an effect self-destroys when `Source.IsDestroyed`; since `Static`/`Multi`/`Land` are pooled on destroy, `Source.IsDestroyed` can read `false` again after reuse.
- **GameEffect.cs:135** — `AnimDataFrame.FrameData[AnimIndex]` indexed inside `unsafe` with `AnimIndex` bounded only by `FrameCount` checked *after* the read.
- **GameEffect.cs:209-217** — `Destroy` calls `_manager?.Remove(this)`, i.e. mutates the effect manager's collection from inside `EffectManager.Update`'s own iteration (World.cs:479).
- **MovingEffect.cs:105-110** — `if (Target != null && Target.IsDestroyed)` copies the destroyed target's coordinates but never clears `Target`, so it keeps reading a destroyed (possibly pooled and reissued) object every frame.
- **MovingEffect.cs:112-114** — dereferences `World.Player` with no null check.
- **DragEffect.cs:91-104** — `Update` returns early before `base.Update()` while `_lastMoveTime > Time.Ticks`, so duration expiry is also deferred by 20 ms steps.
- **DragEffect.cs:114-118** — `ProfileManager.CurrentProfile` and `World.Player` dereferenced with no null check inside `Draw`.
- **TextObject.cs:44-67** — the pool reset disposes `TextBox` on hand-out; `Destroy()` (line 89) also disposes it and returns to the pool, so a `TextObject` reference held after `Destroy` (e.g. by `TextContainer` or `TextRenderer`) sees a reused object.
- **TextObject.cs:123-138** — `ToTopD` walks `DLeft` to the head and casts it to `TextRenderer`; an invalid cast if the chain head is a plain `TextObject`.
- **EntityTextContainer.cs:49-62** — `TextContainer.Add` destroys `Items` (returning it to the pool) and *then* calls `Remove(Items)`; `Size` is only incremented on the non-overflow branch, so it saturates at `MaxSize`.
- **EntityTextContainer.cs:158-185** — `OverheadDamage.Update` mutates `_messages` (`RemoveAt(i--)`) while indexing it.
- **EntityTextContainer.cs:265-276** — `Draw` iterates `_messages` with `foreach` and writes `item.X/Y`; `Update` removes from the same deque in the same frame.
- **EntityTextContainer.cs:132** — `ProfileManager.CurrentProfile.ShowDPS` is read without the null guard used on the lines immediately above it.
- **RenderedText.cs:279-288 / 653-661** — the pixel-picker key is `Text.GetHashCode() ^ hue ^ align ^ style ^ font ^ unicode`; two different strings that collide share a hit map, and `_picker` is never pruned.
- **RenderedText.cs:731-746** — `Destroy` returns to the pool but does not null `Texture`, `_info` or `_text`; `Create` (line 218) relies on `r.Text != text` to decide whether to regenerate, so a reused instance with the same text reuses the previous `_info`.
- **RenderedText.cs:666-686** — if `Texture2D` construction throws, `Texture` stays null and `Width`/`Height` stay 0 while `Links`/`LinesCount` are still populated.
- **LineOfSightHelper.cs:31, :65** — allocates a `List<Point3D>` and a `List<int>` per call; `IsVisible` is reachable per object per frame via `GameObject.HasLineOfSightFrom` (GameObject.cs:58).
- **LineOfSightHelper.cs:105** — `World.Map.GetTile(x, y, false)` with no null check on `World.Map`.
- **LineOfSightHelper.cs:122,133** — `steps = altitude / zlist.Count` is integer division; for short lines with a large altitude difference the per-step allowance collapses to 0.
- **LineOfSightHelper.cs:136-137** — the `playerZ < mobileZ` branch computes `acceptable = mobileZ - (steps * count)`, descending from the *target* height rather than ascending from the observer; the mirror of the branch above uses `mobileZ + ...`.
- **World.cs:350 / :424 (callers, for context)** — the per-frame sweeps iterate `World.Mobiles` / `World.Items` with `foreach` while `Update()` on each element can call `World.RemoveMobile` / `RemoveItem`; removal from the dictionary is deferred to `_toRemove` precisely to avoid mutating during iteration.

---

## Fork deltas

Things that read as TazUO or Holiday additions rather than stock ClassicUO:

**Holiday-specific (this fork), identifiable by the long explanatory comments in the house/pooling area:**

- `GameObject.TileChunk` / `TileCellX` / `TileCellY` / `LastDrawnTime` and the rewritten `RemoveFromTile` — GameObject.cs:186-246. The comment names the failure it repairs ("the house is here and the things inside it are not") and notes `Chunk.RemoveGameObject` was never called from anywhere.
- `Item.ReturnToPool` / `Mobile.ReturnToPool` + `_inPool` — Item.cs:296-333, Mobile.cs:1146-1160. Pooling moved out of `Destroy()`; the matching call sites are in `World.Update`'s sweeps and `GetOrCreate*` (World.cs:407,472,569,593,683,716) with their own comments.
- `Item.LoadMulti` rework — Item.cs:349-379: `HouseDiagnostics.LogMultiRebuild`, resetting `house.Revision = 0` and `IsCustom = false` on rebuild, and moving `WantUpdateMulti = false` to *after* `ClearComponents`. Comment references "the h61 fix".
- `World.HouseManager.RememberFootprint(Serial)` call — Item.cs:591, backed by `HouseManager.RememberFootprint` (HouseManager.cs:82).
- `GameScene.UpdateMaxDrawZ(true)` after multi load — Item.cs:596-602.
- `Managers.HouseDiagnostics.LogHouseItemDestroyed(this)` at the top of `Item.Destroy` — Item.cs:270-272. The whole `HouseDiagnostics` manager is a fork addition (referenced from PacketHandlers, NetClient, GameController, CommandManager).
- Accelerated step drain `stepTime /= Steps.Count` and the `Math.Max(1, ...)` guard — Mobile.cs:769-793, with a long comment about the client having "no catch-up, only overflow".
- The distance-cull `keepWithin` multi-bonus logic in the caller — World.cs:430-453.

**TazUO-level additions (present upstream in TazUO, not in stock ClassicUO):**

- `GraphicsReplacement.Replace` / `ReplaceHue` wired into the `Graphic`/`Hue` setters, plus `OriginalGraphic` / `ResetOriginalGraphic` — GameObject.cs:105-126.
- `AverageOverTime` DPS tracking: `AddDamage` / `GetCurrentDPS` (GameObject.cs:139-148) and the `ShowDPS` suffix on damage numbers (EntityTextContainer.cs:132-135).
- `LineOfSightHelper.cs` — entire file, plus `GameObject.HasLineOfSightFrom` (GameObject.cs:58-64). No license header, unlike every other file here.
- `EventSink` hooks: `InvokeOnPlayerStatChange` inside the `Hits` setter (Entity.cs:67-80), `InvokeOnBuffAdded` / `InvokeOnBuffRemoved` (PlayerMobile.cs:277,289), `InvokeOnPositionChanged` (PlayerMobile.cs:1432).
- `HitsRequestStatus` enum + `GameActions.SendCloseStatus(Serial, HitsRequest >= Pending)` — Entity.cs:45-50, :202.
- Item highlight fields `MatchesHighlightData`, `HighlightHue`, `HighlightColor` — Item.cs:116-118, reset at Item.cs:103-105.
- Grid container / modern UI gump disposal: `GridContainer`, `GridLootGump`, `ModernPaperdoll` in `Item.Destroy` (Item.cs:277-285), `Mobile.Destroy` (Mobile.cs:1140), `PlayerMobile.CloseBank`/`CloseRangedGumps` (PlayerMobile.cs:1517, :1608) — all wrapped in `#region GridContainer`.
- `Mobile.IsPlayer`, `Mobile.InParty`, `Mobile.Mount` as a cached `Item` property — Mobile.cs:180-203; pool reset clears them at Mobile.cs:107-109.
- `#region Python API accessors - Added for Python API` — Mobile.cs:217-222.
- `ScriptRecorder.Instance.UpdatePlayerPosition` in `OnPositionChanged` — PlayerMobile.cs:1424-1427.
- `SpellVisualRangeManager.CastTimerProgressBar` and `SkillProgressBar.QueManager` wired into the `PlayerMobile` constructor — PlayerMobile.cs:61-70.
- `ImprovedBuffGump` / `UseImprovedBuffBar` branches in `AddBuff` / `RemoveBuff` — PlayerMobile.cs:270-298.
- Obstacle-avoidance walking: `Walk` with `AutoAvoidObstacules`, `IsCardinalDirection`, `IsObstacle`, `TryToAvoid`, and the untouched original preserved as `WalkNotAvoid` — PlayerMobile.cs:1674-2054.
- `AutoOpenCorpses` / `CorpseOpenOptions` / `AutoOpenCorpseRange` and `AutoOpenDoors` — PlayerMobile.cs:1435-1478.
- Forced house transparency: `Multi.ForceTransparentHouse`, `Multi.ForcedTransparency`, and the `ForceHouseTransparency` block in `House.Add` — Multi.cs:73-74, House.cs:90-96.
- `TextBox`-based overhead text (`TextObject.TextBox`, `TextBox.GetOne(...)` with `RTLOptions`, `OverheadChatFont`/`FontSize`/`Width`, `DisableMouseInteractionOverheadText`) replacing stock `RenderedText` for messages — TextObject.cs:77, EntityTextContainer.cs:135.
- Per-notoriety damage hues (`DamageHueSelf`/`Other`/`Pet`/`Ally`/`DamageHueLastAttck`) and journal echo of damage — EntityTextContainer.cs:116-137.
- `RenderedText.MaxHeight` and the try/catch around `Texture2D` creation / `SetData` — RenderedText.cs:99, :666-708.
- Expanded weapon-ability table entries for Publish 103 whips and Publish 119 paladin weapons — PlayerMobile.cs:1295-1384; note the C# 9 `case 0x4076 or 0x907:` pattern at PlayerMobile.cs:1165 (compiles on `net472` with a modern SDK).
