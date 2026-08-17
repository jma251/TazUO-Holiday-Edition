# 03 — LIFETIME: Item / Mobile / GameObject in a running frame

Root: `/home/user/TazUO-Holiday-Edition`. All paths below are relative to
`src/ClassicUO.Client/` unless stated otherwise. Line numbers are as of the
`legacy` working tree read for this document.

---

## 0. Where in the frame each phase runs

`GameController.Update` (`GameController.cs:466`) is the whole frame:

| Order | Call | File:line | What happens to entities |
| --- | --- | --- | --- |
| 1 | `Mouse.Update()` | `GameController.cs:474` | — |
| 2 | `ProcessNetworkPackets()` | `GameController.cs:478` | **every** create and destroy driven by a packet id happens here |
| 3 | `Plugin.Tick()` | `GameController.cs:481` | — |
| 4 | `Scene.Update()` → `GameScene.Update` | `GameController.cs:486` | |
| 4a | `World.Update()` | `Game/Scenes/GameScene.cs:913` | per-entity `Update()`, distance cull, **the only sweep that removes dictionary entries and calls `ReturnToPool`** |
| 4b | `BoatMovingManager.Update()` | `Game/Scenes/GameScene.cs:916` | reads entities by serial |
| 5 | `UIManager.Update()` | `GameController.cs:491` | the deferred removal of gumps flagged `IsDisposed` |
| 6 | `LegionScripting.OnUpdate()` | `GameController.cs:495` | — |
| 7 | `Draw` | `GameController.cs:557+` | render lists rebuilt from chunk tiles |

So the whole window between a `Destroy()` in phase 2 and the sweep in phase 4a is
one frame long, and inside phase 2 it is as long as the remainder of the packet
batch.

---

## 1. Every creation site

### 1.1 Entities filed in `World.Items` / `World.Mobiles`

| Site | File:line | Pool | Filed in dictionary |
| --- | --- | --- | --- |
| `Item.Create(serial)` | `Game/GameObjects/Item.cs:255-261` | `Item._pool` (`Item.cs:52`) | by caller |
| `Mobile.Create(serial)` | `Game/GameObjects/Mobile.cs:240-246` | `Mobile._pool` (`Mobile.cs:48`) | by caller |
| `World.GetOrCreateItem` | `Game/World.cs:559-581` — create at `:576`, `Items.Add` at `:577` | yes | yes |
| `World.GetOrCreateMobile` | `Game/World.cs:583-605` — create at `:600`, `Mobiles.Add` at `:601` | yes | yes |
| `new PlayerMobile(serial)` | `Game/World.cs:194`, `Mobiles.Add(Player)` `Game/World.cs:195`, from `World.CreatePlayer` called at `Network/PacketHandlers.cs:976` | **not pooled** | yes |

`Dictionary.Add` is the extension at `Game/GameObjects/EntityCollection.cs:52-62`:
it keys on `entity.Serial` and **refuses** (returns `false`, silently) if the key
already exists. `Get` is `EntityCollection.cs:40-45`.

### 1.2 Callers of `GetOrCreateItem` / `GetOrCreateMobile` — the packet-driven create sites

| Call site | File:line | Reached from packet id |
| --- | --- | --- |
| `UpdateGameObject` mobile branch | `Network/PacketHandlers.cs:6596` | `0x1A`, `0x77`, `0x78`, `0xD2`, `0xD3`, `0xF3`, `0xF6`, `0xF7` |
| `UpdateGameObject` item branch | `Network/PacketHandlers.cs:6615` | same list |
| `AddItemToContainer` | `Network/PacketHandlers.cs:6439` | `0x25`, `0x3C`, `0x27` (branch A), `0xF7` |
| `EquipItem` | `Network/PacketHandlers.cs:1987` | `0x2E` |
| `DenyMoveItem` repair branch | `Network/PacketHandlers.cs:1795` | `0x27` |
| `UpdateObject` equipment loop | `Network/PacketHandlers.cs:3153` | `0x78`, `0xD3` |
| `CorpseEquipment` loop | `Network/PacketHandlers.cs:3391` | `0x89` |
| spellbook container | `Network/PacketHandlers.cs:4810` | `0xBF` cmd 0x1B |
| char-creation paperdoll preview | `Game/UI/Gumps/CharCreation/CreateCharAppearanceGump.cs:1006` | none (UI); serial is `0x4000_0000 + layer` |
| race-change preview | `Game/UI/Gumps/RaceChangeGump.cs:502` | none (UI); same synthetic serial scheme |

Two `Item.Create` calls bypass `World.Items` entirely:

- `Network/PacketHandlers.cs:4830` — `Item.Create(cc)` where `cc` is the **spell
  index 1..64 used as the serial**, graphic `0x1F2E`, pushed into the spellbook's
  child list only. Driven by `0xBF` cmd 0x1B.
- `Game/Scenes/GameScene.cs:999` — `Item.Create(0)`, the multi-placement preview,
  `Graphic` at `:1000`, `Hue` `:1001`, `IsMulti = true` `:1002`. Driven by `0x99`
  setting `MultiTargetInfo` (`Game/Managers/TargetManager.cs:332`); the item is
  built on the following frame, not in the handler.

Two `new PlayerMobile` outside the world: `CreateCharAppearanceGump.cs:260`
(`new PlayerMobile(1)`) and `RaceChangeGump.cs:323` (`new PlayerMobile(0)`).

### 1.3 Non-entity `GameObject`s (chunk-resident)

| Type | Create | Pool | Returned to pool |
| --- | --- | --- | --- |
| `Land` | `Game/Map/Chunk.cs:100` (`Land.Create`) | `Land.cs:45` | **inside `Destroy()`**, `Land.cs:86` |
| `Static` | `Game/Map/Chunk.cs:134` (`Static.Create`) | `Static.cs:42` | **inside `Destroy()`**, `Static.cs:116` |
| `Multi` | `Game/GameObjects/Item.cs:455` (UOP multi), `Item.cs:533` (mul multi), `Game/GameObjects/House.cs:81` (`House.Add`), `Game/GameCursor.cs:321` | `Multi.cs:43` | **inside `Destroy()`**, `Multi.cs:118` |
| `Chunk` | `Game/Map/Map.cs:97` (`Chunk.Create` + `Load`) | `Chunk.cs:44` | inside `Chunk.Destroy()`, `Chunk.cs:428`; **not** inside `Chunk.Clear()` |
| `GameEffect` subclasses | `Game/Managers/EffectManager.cs:109/145/169/188/212` | none — plain `new` | n/a |
| `TextObject` | `Game/GameObjects/TextObject.cs:83-86` (`TextObject.Create`) | `TextObject.cs:~50` | inside `Destroy()`, `TextObject.cs:104` |

**This is the structural split**: `Land`, `Static`, `Multi`, `TextObject` and
`Chunk` hand themselves back to their pool *from inside* `Destroy()`.
`Item` and `Mobile` deliberately do not — see §2.

---

## 2. The object pool

### 2.1 The pool itself

`ClassicUO.Utility/QueuedPool.cs:38-91`. It is a `Stack<T>` (`:41`), pre-filled
with `size` instances in the constructor (`:50-53`). `GetOne()` (`:61-77`) pops,
or `new T()` if empty (`:71`), then runs the `_on_pickup` reset action (`:74`).
`ReturnOne(obj)` (`:79-85`) pushes with **no** duplicate check and no reset.

Sizes: `Item._pool` = `Constants.PREDICTABLE_CHUNKS * 3` (`Item.cs:53`),
`Mobile._pool` = `Constants.PREDICTABLE_CHUNKS` (`Mobile.cs:49`).

### 2.2 Where an Item goes on destroy

`Item.Destroy()` — `Item.cs:263-297`:
- early-out if already `IsDestroyed` (`:265-268`)
- `HouseDiagnostics.LogHouseItemDestroyed(this)` (`:272`) — before teardown
- if `Opened`: disposes `ContainerGump` (`:276`), `GridContainer` (`:278`),
  `SpellbookGump` (`:280`), `MapGump` (`:281`), `GridLootGump` if `IsCorpse`
  (`:285`), `BulletinBoardGump` (`:288`), `SplitMenuGump` (`:289`), then
  `Opened = false` (`:291`)
- `base.Destroy()` (`:294`) → `Entity.Destroy` → `GameObject.Destroy`
- **explicitly does not pool** (comment `:296`)

`Item.ReturnToPool()` — `Item.cs:321-331`, guarded by
`if (IsDestroyed && !_inPool)` (`:326`), sets `_inPool = true` (`:328`), pushes
(`:329`). `_inPool` field at `Item.cs:333`.

`Mobile.Destroy()` — `Mobile.cs:1129-1144`: masks the serial `& 0x3FFFFFFF`
(`:1131`), `ClearSteps()` (`:1133`), `base.Destroy()` (`:1135`), and for
non-`PlayerMobile` disposes `PaperDollGump` (`:1139`) and `ModernPaperdoll`
(`:1140`). `Mobile.ReturnToPool()` — `Mobile.cs:1150-1158`, guard
`IsDestroyed && !_inPool && !(this is PlayerMobile)` (`:1153`). A `PlayerMobile`
is therefore **never** pooled.

`Entity.Destroy()` — `Entity.cs:198-206`: `base.Destroy()`, then
`GameActions.SendCloseStatus(Serial, ...)` (`:202`) — note this reads `Serial`
*after* `GameObject.Destroy` has run — then `AnimIndex = 0`,
`LastAnimationChangeTime = 0`.

`GameObject.Destroy()` — `GameObject.cs:447-473`. Clears `Next`, `Previous`,
`RenderListNext`, `_averageOverTime`, calls `Clear()` (the `LinkedObject` child
list), `RemoveFromTile()`, `TextContainer?.Clear()`, then sets `IsDestroyed =
true`, `PriorityZ`, `IsPositionChanged`, `Hue`, `Offset`, `RealScreenPosition`,
`IsFlipped`, `originalGraphic`, `Graphic`, `ObjectHandlesStatus`, `FrameInfo`.

### 2.3 Where they are handed back out

`QueuedPool.GetOne` runs the reset lambda:

- **Item** — `Item.cs:54-106`. Resets `_inPool`, `IsDestroyed`, `Graphic`,
  `Amount`, `Container` (to `0xFFFF_FFFF`), `_isMulti`, `Layer`, `Price`,
  `UsedLayer`, `_displayedGraphic`, `X/Y/Z`, `LightID`, `MultiDistanceBonus`,
  `Flags`, `WantUpdateMulti`, `MultiInfo`, `MultiGraphic`, `AlphaHue`, `Name`,
  `Direction`, `AnimIndex`, `Hits`, `HitsMax`, `LastStepTime`,
  `LastAnimationChangeTime`, `Clear()`, `IsClicked`, `IsDamageable`, `Offset`,
  `HitsPercentage`, `Opened`, `TextContainer?.Clear()`, `IsFlipped`,
  `FrameInfo`, `ObjectHandlesStatus`, `AllowedToDraw`, `ExecuteAnimation`,
  `HitsRequest`, `ResetOriginalGraphic()`, `MatchesHighlightData`,
  `HighlightHue`, `HighlightColor`.
- **Mobile** — `Mobile.cs:50-110`. Resets `_inPool`, `IsDestroyed`, `Graphic`,
  `Steps.Clear()`, `Offset`, `SpeedMode`, `Race`, `Hits`, `HitsMax`, `Mana`,
  `ManaMax`, `Stamina`, `StaminaMax`, `NotorietyFlag`, `IsRenamable`, `Flags`,
  `IsFemale`, `InWarMode`, `IsRunning`, the six animation fields,
  `AnimationFromServer`, `LastStepSoundTime`, `StepSoundOffset`, `Title`,
  `_animationGroup`, `_isDead`, `_isSA_Poisoned`, `_lastAnimationIdleDelay`,
  `X/Y/Z`, `Direction`, `LastAnimationChangeTime`, `TextContainer?.Clear()`,
  `HitsPercentage`, `IsFlipped`, `FrameInfo`, `ObjectHandlesStatus`, `AlphaHue`,
  `AllowedToDraw`, `IsClicked`, `RemoveFromTile()`, `Clear()`, `Next`,
  `Previous`, `Name`, `ExecuteAnimation`, `HitsRequest`,
  `CalculateRandomIdleTime()`, `IsPlayer`, `Mount`, `InParty`.

`Serial` is assigned **after** the reset, by `Create` — `Item.cs:258`,
`Mobile.cs:243`.

### 2.4 Fields that survive reuse (not touched by either reset lambda)

Common to both (declared in `GameObject`/`Entity`/`LinkedObject`):

- `TextContainer` — the object itself survives; only its contents are cleared
  (`Item.cs:93`, `Mobile.cs:90`). A `TextObject` inside it carries `Owner`
  pointing at this instance (`TextObject.cs:75`).
- `TileChunk`, `TileCellX`, `TileCellY` (`GameObject.cs:192, 196-197`) — Item's
  lambda never calls `RemoveFromTile()`; Mobile's does (`Mobile.cs:98`), and
  `RemoveFromTile` nulls `TileChunk` (`GameObject.cs:224`) but **leaves
  `TileCellX`/`TileCellY` at their old values**.
- `TNext` / `TPrevious` (`GameObject.cs:129-130`) — Item's lambda does not touch
  them and does not call `RemoveFromTile`.
- `Items` (child list head, `LinkedObject.cs:41`) — cleared via `Clear()` in
  both lambdas.
- `Next`/`Previous` — cleared for Mobile (`Mobile.cs:100-101`), **not** for Item
  (Item relies on `Clear()`, which only walks the *children*, not this object's
  own sibling links — `LinkedObject.cs:201-215`).
- `RenderListNext` (`GameObject.cs:134`) — not reset by either lambda; only by
  `GameObject.Destroy` (`GameObject.cs:456`).
- `LastDrawnTime` (`GameObject.cs:195`), `FoliageIndex` (`GameObject.cs:104`) —
  Item does not reset `FoliageIndex`; `Multi` does (`Multi.cs:50`).
- `_averageOverTime` (`GameObject.cs:56`) — nulled by `Destroy`
  (`GameObject.cs:457`), not by the lambdas.
- `Serial` — overwritten by `Create`, i.e. the object's identity is the last
  thing to change.

Item-specific survivors: `IsHuman`/`IsGargoyle` are Mobile-only; Item does not
reset `Direction`'s backing `_direction` other than via the `Direction` property
(`Item.cs:79`), does not reset `PriorityZ`, `IsPositionChanged`,
`RealScreenPosition`, `Hue` (only `Graphic = 0` which drives `Hue` through the
setter at `GameObject.cs:112-113`).

Mobile-specific survivors: `IsHuman`, `IsGargoyle` (`Mobile.cs:181-182`) — only
recomputed on the next `Graphic` set via `OnGraphicSet` (`Mobile.cs:248-253`),
and `Graphic = 0` in the lambda (`Mobile.cs:54`) does run that; `Steps` deque
object itself is reused (`Mobile.cs:150`, contents cleared at `Mobile.cs:55`).

`Multi`'s lambda (`Multi.cs:47-58`) resets `IsDestroyed`, `AlphaHue`,
`FoliageIndex`, `IsHousePreview`, `MultiOffsetX/Y/Z`, `IsCustom`, `State`,
`IsMovable`, `Offset`, `ForceTransparentHouse` — it does **not** call
`RemoveFromTile()` and does not reset `TNext`/`TPrevious`/`TileChunk`.

`Chunk`'s lambda (`Chunk.cs:47-51`) resets only `LastAccessTime` and
`IsDestroyed`. `Tiles[8,8]` (`Chunk.cs:54`) is a readonly array that is **not**
cleared on pickup — it is cleared by `Destroy`/`Clear` (`Chunk.cs:418`, `:460`)
before the chunk is pushed back.

---

## 3. Identity: where a key can point at an object whose serial is now different

### 3.1 The mechanism

`World.Items` / `World.Mobiles` are keyed by the serial **at the time of
insertion** (`EntityCollection.cs:59`). `Item.Serial` / `Mobile.Serial` are
public mutable fields (`Entity.cs:86`) reassigned by `Create`
(`Item.cs:258`, `Mobile.cs:243`). Nothing keeps them in step.

The ordering rule that holds the invariant is stated in `Item.cs:299-320`:
`ReturnToPool` may only be called once the dictionary entry is out. The three
places that obey it:

- `World.cs:404` + `:407` — `Mobiles.Remove(key)` then `gone.ReturnToPool()`
- `World.cs:469` + `:472` — `Items.Remove(key)` then `gone.ReturnToPool()`
- `World.cs:681/683` and `:714/716` — `Remove(serial)` then `ReturnToPool()`
- `World.cs:565/569` and `:589/593` — evict-then-pool inside `GetOrCreate*`

### 3.2 Window A — removal keyed by `Serial` while iteration is keyed by the dictionary key

`World.RemoveItem` / `World.RemoveMobile` take a `uint`. Every call that passes
an entity object goes through the implicit conversion at `Entity.cs:313-316`,
which yields `entity.Serial`, **not** the key it is filed under:

| Call | File:line | Argument |
| --- | --- | --- |
| `RemoveItem(rem, true)` | `Game/World.cs:303` | `rem` fetched by key `ObjectToRemove` at `:296` |
| `RemoveMobile(mob)` | `Game/World.cs:358` | `mob` = `pair.Value` from the keyed loop at `:350` |
| `RemoveItem(item)` | `Game/World.cs:446`, `:451` | `item` = `pair.Value` from the keyed loop at `:424` |
| `RemoveMobile(mobile)` / `RemoveItem(item)` | `Game/World.cs:924`, `:929` | `.Values` iteration |
| `RemoveItem(first as Item, forceRemove)` | `Game/World.cs:671`, `:704` | child from the linked list |
| `_toRemove.Add(item)` / `_toRemove.Add(mob)` | `Game/World.cs:990`, `:1010` | `_toRemove` is `List<uint>` (`World.cs:56`); the implicit conversion stores **`item.Serial`**, and `:995`/`:1015` then call `RemoveItem(serial, true)` / `RemoveMobile(serial, true)` with it |
| `World.RemoveItem(item, true)` | `Network/PacketHandlers.cs:6436` | object fetched by `World.Items.Get(serial)` at `:6425` |
| `World.RemoveItem(it, true)` | `Network/PacketHandlers.cs:6908` | child of a container's linked list |
| `World.RemoveItem(first, true)` | `Game/GameObjects/PlayerMobile.cs:1507` | child of the bank container |
| `World.RemoveItem((Item)first, true)` | `Game/UI/Gumps/CharCreation/CreateCharAppearanceGump.cs:270`, `Game/UI/Gumps/RaceChangeGump.cs:332` | child |
| `World.RemoveMobile(Serial)` | `Game/GameObjects/Mobile.cs:701`, `:711`, `:717` | own serial, from `ProcessAnimation` |

Inside `RemoveItem` the first act is `Items.Get(serial)` (`World.cs:657`). If
`serial != key`, this looks up a different entry — or none — and the function
returns `false` at `:659-662` without destroying the object that was passed in.

The comments at `World.cs:341-349` and `:414-423` document the observed effect:
an item left under a stale key is never found by `RemoveItem`, never destroyed,
never swept, and is re-condemned by the distance cull every 50 ms
(`_timeToDelete = Time.Ticks + 50`, `World.cs:338`).

The sweeps themselves were changed to be key-safe: they collect `pair.Key`
(`World.cs:363`, `:457`) and re-verify with
`TryGetValue(_toRemove[i], out … ) && gone.IsDestroyed` before removing
(`World.cs:402`, `:467`). `InternalMapChangeClear` at `World.cs:990`/`:1010` was
**not** — it still collects serials.

### 3.3 Window B — `ReturnToPool` before the entry is out (inside `GetOrCreate*`)

`World.GetOrCreateItem` (`World.cs:559-581`):

```
561  item = Items.Get(serial)
563  if (item != null && item.IsDestroyed)
565      Items.Remove(serial)          <- keyed by the requested serial
569      item.ReturnToPool()
576  item = Item.Create(serial)        <- may pop the very object just pushed
577  Items.Add(item)
```

If the destroyed object was filed under a key different from its own `Serial`
(window A), `Items.Remove(serial)` at `:565` removes nothing, and `:569` still
pools the object. The stale key now points at a pooled object; `Create` at `:576`
can pop that same object and stamp the new serial onto it — at which point the
old key holds a live object carrying somebody else's serial. `Items.Add` at
`:577` refuses if the new serial is also already present
(`EntityCollection.cs:54-56`), and the return value is discarded.

`GetOrCreateMobile` is identical: `World.cs:589` / `:593` / `:600` / `:601`.

### 3.4 Window C — `Clear()` drops entries without pooling

`World.Clear()` (`World.cs:920-963`) calls `RemoveMobile`/`RemoveItem` with
`forceRemove` defaulted to `false` (`:924`, `:929`), so nothing is removed from
either dictionary and nothing is pooled inside those loops. `Items.Clear()`
(`:936`) and `Mobiles.Clear()` (`:937`) then drop every entry wholesale. Those
objects are destroyed with `_inPool == false` and are never pushed back onto the
pool — the pool shrinks to whatever it held, and `GetOne` falls through to
`new T()` (`QueuedPool.cs:71`).

### 3.5 Window D — objects destroyed by chunk teardown, still in the dictionaries

`Chunk.Destroy()` (`Chunk.cs:389-429`) and `Chunk.Clear()` (`Chunk.cs:431-470`)
walk all 64 cells and call `first.Destroy()` on every object except
`World.Player` (`:408-411`, `:450-453`). For an `Item` or `Mobile` standing on
that chunk this sets `IsDestroyed = true` **without** touching `World.Items` /
`World.Mobiles` and without pooling. The next `World.Update` sweep picks them up
by key (`World.cs:361-364`, `:455-458`) — so between the chunk teardown and the
sweep, `World.Get(serial)` returns `null` (`World.cs:551-554`) while the entry
still exists.

`Map.Destroy()` (`Game/Map/Map.cs:297-310`) destroys every chunk unconditionally.
`Map.ClearUnusedBlocks` (`Map.cs:274-290`) destroys a chunk only if
`block.HasNoExternalData()` (`Chunk.cs:472-489`), which returns `false` for any
object that is not `Land` or `Static` — the `Multi` exclusion is commented out at
`Chunk.cs:480`, so a chunk holding house components is never unloaded there.

### 3.6 Synthetic serials that collide with real ones

- `Network/PacketHandlers.cs:4830` — `Item.Create(cc)` with `cc` = spell index
  `1..64` as the serial. Not filed in `World.Items`, but the object comes from
  the same shared `Item._pool`, and `spellbook.Clear()` at `:4812` drops the
  previous batch without destroying or pooling them.
- `CreateCharAppearanceGump.cs:1006` / `RaceChangeGump.cs:502` —
  `World.GetOrCreateItem(0x4000_0000 + layer)`: real entries in `World.Items`
  under serials the server never issues.
- `GameScene.cs:999` — `Item.Create(0)`, serial `0`; the matching house is keyed
  `0` in `HouseManager` and torn down by `TargetManager.CancelTarget`
  (`Game/Managers/TargetManager.cs:293-297`).
- `Mobile.cs:698`, `:708`, `:714` — the `(Serial & 0x80000000) != 0` branch:
  death-animation mobiles carry the high bit; `Mobile.Destroy` masks it off
  (`Mobile.cs:1131`) before looking up gumps, so the gump lookup uses a different
  number than the dictionary key.
- `Network/PacketHandlers.cs:884-887` — `0x1A` strips `0x80000000` off the wire
  serial before use.

---

## 4. Reference holders that outlive a frame

| Holder | Field / structure | What it holds | On destroy of the target | On recycle of the target |
| --- | --- | --- | --- | --- |
| Chunk tile cells | `Chunk.Tiles[8,8]` (`Chunk.cs:54`) + `TNext`/`TPrevious` chain (`GameObject.cs:129-130`) | direct object refs | `GameObject.Destroy` → `RemoveFromTile()` (`GameObject.cs:459`) hands the cell to `TNext ?? TPrevious` (`GameObject.cs:226-232`) and unlinks | Item's pool lambda does **not** call `RemoveFromTile`; Mobile's does (`Mobile.cs:98`). `Chunk.AddGameObject` calls `obj.RemoveFromTile()` first (`Chunk.cs:174`) |
| Container / equipment lists | `LinkedObject.Items`, `Next`, `Previous` (`LinkedObject.cs:41`) | direct refs | `GameObject.Destroy` nulls own `Next`/`Previous` (`:454-455`) and `Clear()`s children (`:458`) — `Clear` (`LinkedObject.cs:201-215`) only nulls each child's `Next`, it does **not** null their `Previous` and does not remove the child from any dictionary | Item's lambda calls `Clear()` (`Item.cs:86`) but never nulls its own `Next`/`Previous` |
| `House.Components` | `List<Multi>` (`Game/GameObjects/House.cs:53`) | direct `Multi` refs | `ClearComponents` (`House.cs:179-201`) `Destroy()`s each and `RemoveAt(i--)`; `Multi.Destroy` pools immediately (`Multi.cs:118`) | a `Multi` popped from the pool while still listed in some `House.Components` is the failure mode `Item.cs:349-372` is written around |
| `HouseManager` | keyed by multi serial; `HouseManager.Remove(serial)` → `House.ClearComponents` | serial-keyed | `World.cs:987` on map change, `PacketHandlers.cs:1239` on `0x1D`, `:4854` on `0xBF` | footprint is remembered separately (`Item.cs:591`, `World.HouseManager.RememberFootprint`) |
| `EffectManager` | `LinkedObject` list of `GameEffect` (`Game/Managers/EffectManager.cs:39`) | `GameEffect.Source` / `Target` are raw `GameObject` refs (`GameEffect.cs:99-100`) | `GameEffect.Destroy` (`GameEffect.cs:209-216`) removes itself from the manager and nulls `Source`/`Target`. The guard is `if (Source != null && Source.IsDestroyed) Destroy()` (`GameEffect.cs:111-113`) | a recycled `Source` has `IsDestroyed == false` again, so the guard passes and the effect keeps following the object under its new serial |
| `WorldTextManager` / `TextContainer` | `TextObject.Owner` is a raw `GameObject` (`TextObject.cs:75`) | direct ref | `GameObject.Destroy` → `TextContainer?.Clear()` (`GameObject.cs:460`) — `LinkedObject.Clear` does not call `TextObject.Destroy`, so `Owner` is not nulled there; `TextObject.Destroy` nulls it (`TextObject.cs:102`) | `TextContainer?.Clear()` in both pool lambdas (`Item.cs:93`, `Mobile.cs:90`) |
| Gumps | `Control.LocalSerial` / `ServerSerial` | **serials, not refs**, for `ContainerGump`, `GridContainer`, `SpellbookGump`, `MapGump`, `GridLootGump`, `BulletinBoardGump`, `SplitMenuGump`, `PaperDollGump`, `ModernPaperdoll`, `BaseHealthBarGump`, `NameOverheadGump` (`Game/UI/Gumps/NameOverheadGump.cs:98,116,189,210,235` all re-resolve via `World.Get(LocalSerial)`) | disposed by `Item.Destroy` (`Item.cs:276-289`) / `Mobile.Destroy` (`Mobile.cs:1139-1140`) — `Dispose` only flags `IsDisposed`, actual list removal is deferred to `UIManager.Update` | a gump keyed on a serial that has been re-issued resolves to whatever object now holds it |
| Gumps that hold refs | `GridLootGump._corpse` (`Game/UI/Gumps/GridLootGump.cs:57`), `GridContainer.container` / `_item` / `_container` (`GridContainer.cs:846-847, 1444, 1912`), `InspectorGump._obj` (`InspectorGump.cs:53`), `RaceChangeGump.hair/beard/playerMobile` (`RaceChangeGump.cs:663-665`), `CustomToolTip.item/compareTo` (`CustomToolTip.cs:13,17`), `ModernPaperdoll.Item/item` (`ModernPaperdoll.cs:340,441`) | direct `Item`/`Mobile`/`GameObject` refs | nothing nulls them; the gump is disposed if it is registered under the destroyed serial | a recycled object is still reachable through these until the gump is disposed |
| `SelectedObject` | static fields `Object`, `LastLeftDownObject`, `HealthbarObject`, `SelectedContainer`, `CorpseObject` (`Game/SelectedObject.cs:41-45`) | direct refs, static, cross-frame | nothing nulls them on destroy | survives into the next frame pointing at a pooled object |
| Render lists | `_renderList*Head` chains via `GameObject.RenderListNext` (`Game/Scenes/GameSceneDrawingSorting.cs:80-96`), `_foliages[]` (`:50`, filled `:377`) | direct refs | `GameObject.Destroy` nulls `RenderListNext` (`GameObject.cs:456`) — mid-chain, which severs the rest of the list | rebuilt each frame in phase 7 |
| `Mobile.Mount` | `Item` ref (`Mobile.cs:180`) | direct ref | nulled explicitly only on `0x1D` for a `Layer.Mount` item (`PacketHandlers.cs:1152`) | reset to `null` by Mobile's pool lambda (`Mobile.cs:108`) |
| `CorpseManager` | `Deque<CorpseInfo>` of **serials** (`Game/Managers/CorpseManager.cs:41`) | serial pairs | `Remove(corpse, obj)` (`CorpseManager.cs:59-82`) re-resolves through `World.Items.Get` | a re-issued serial resolves to the new object at `CorpseManager.cs:68` |
| `BoatMovingManager` | `Dictionary<uint, FastList<ItemInside>>` (`Game/Managers/BoatMovingManager.cs:51`), `ItemInside.Serial` (`:457`) | serials | resolved via `World.Get(it.Serial)` (`:167`, `:381`) | same |
| `World.LastObject` / `World.ObjectToRemove` | `uint` (`World.cs:69`) | serials | `ObjectToRemove` consumed and zeroed at `World.cs:296-297` | re-issued serial resolves to the new object |
| `ObjectPropertiesListManager` (`World.OPL`) | serial-keyed | serials | `OPL.Remove(serial)` at `World.cs:676` and `:709` | — |
| `Entity._hitsPercText[101]` | `static RenderedText[]` (`Entity.cs:54`) | shared, not per-entity | never freed per entity; rebuilt if `IsDestroyed` (`Entity.cs:144`) | shared across all entities by percentage |

---

## 5. The destroy path, in order

### 5.1 The intended order

```
World.RemoveItem(serial, forceRemove)                       World.cs:655
  657  item = Items.Get(serial)
  659  bail if null or already IsDestroyed
  664  first = item.Items                                  (snapshot of children)
  665  RemoveItemFromContainer(item)                       World.cs:617-653
         627-637  RequestUpdateContents on PaperDoll / ModernPaperdoll /
                  ContainerGump / GridContainer / NearbyLootGump
         640-645  container.Remove(obj)   (unlink from parent's child list)
         647      obj.Container = 0xFFFF_FFFF
         650-652  obj.Next = obj.Previous = null; obj.RemoveFromTile()
  667-674 recurse into every child: RemoveItem(first as Item, forceRemove)
  676  OPL.Remove(serial)
  677  item.Destroy()                                      Item.cs:263
         272   HouseDiagnostics.LogHouseItemDestroyed
         274-292 dispose the 7 gump kinds if Opened
         294   base.Destroy() -> Entity.Destroy -> GameObject.Destroy
  679  if (forceRemove)
  681      Items.Remove(serial)
  683      item.ReturnToPool()                             Item.cs:321
```

`RemoveMobile` is the same shape: `World.cs:689-720`, children at `:698-707`,
`OPL.Remove` `:709`, `Destroy()` `:710`, `Mobiles.Remove` `:714`,
`ReturnToPool()` `:716`. It never calls `RemoveItemFromContainer` on itself.

With `forceRemove == false`, only `Destroy()` runs; the dictionary entry and the
pooling are left to the phase-4a sweep (`World.cs:396-412` for mobiles,
`:461-477` for items), which is the only place that goes
`Remove(key)` → `ReturnToPool()`.

### 5.2 Places that run out of that order

| Out-of-order behaviour | File:line |
| --- | --- |
| `Destroy()` with **no** subsequent dictionary removal or pooling — the object relies on the phase-4a sweep finding it by key | `Chunk.cs:410`, `Chunk.cs:452` (chunk teardown destroys Items and Mobiles directly) |
| `Items.Clear()` / `Mobiles.Clear()` **without** `ReturnToPool` on any element | `World.cs:936`, `:937` |
| `Player.Destroy()` after `Mobiles.Clear()`, then `Player = null` | `World.cs:938-939` |
| `ReturnToPool()` driven by the *requested* serial rather than the key | `World.cs:565/569`, `:589/593` |
| Removal collected as serials, not keys, then removed by serial | `World.cs:990`+`:995`, `World.cs:1010`+`:1015` |
| `Multi.Destroy()` pools immediately from inside `Destroy` — the exact pattern `Item.ReturnToPool` exists to avoid, but `Multi` has no serial and is not dictionary-filed | `Multi.cs:110-119` |
| `Land`, `Static` — same immediate self-pooling | `Land.cs:78-87`, `Static.cs:108-117` |
| `Chunk.Destroy` pools the chunk; `Chunk.Clear` does the identical teardown but does **not** pool | `Chunk.cs:428` vs `Chunk.cs:431-470` |
| `spellbook.Clear()` drops 64 pooled `Item`s from the child list without `Destroy()` or `ReturnToPool()` | `Network/PacketHandlers.cs:4812` |
| `bank.Items = null` after the children were force-removed | `Game/GameObjects/PlayerMobile.cs:1512` |
| `container.Items = remove_unequipped ? new_first : null` — reassigns the head after selectively force-removing children | `Network/PacketHandlers.cs:6913` |
| `Mobile.ProcessAnimation` calls `World.RemoveMobile(Serial)` from **inside** the phase-4a `Mobiles` iteration (`World.cs:350`); safe only because `forceRemove` defaults to `false` | `Mobile.cs:701`, `:711`, `:717` |
| `Entity.Destroy` reads `Serial` and sends a packet **after** `GameObject.Destroy` has already reset the object's graphic/position state | `Entity.cs:200-202` |
| `GameEffect.Destroy` unlinks from the manager *before* `base.Destroy()` | `GameEffect.cs:209-216` |

---

## 6. Per-packet-id table

`C` = creates an `Item`/`Mobile`/`PlayerMobile`; `D` = destroys one; `R` = can
reuse a serial (evict-and-pool then re-create under the same or a colliding
serial). Handler line numbers are in `Network/PacketHandlers.cs` unless noted.

### 6.1 Ids that create and/or destroy entities

| Id | Handler:line | C | D | R | Notes |
| --- | --- | --- | --- | --- | --- |
| `0x1A` UpdateItem | `UpdateItem:869` → `UpdateGameObject` | ✔ `:6596`/`:6615` | ✔ via `RemoveItemFromContainer` `:6633` | ✔ `World.cs:565/569`, `:589/593` | also `new House` + `Multi.Create` in `LoadMulti` (`Item.cs:344-345, 455, 533`) |
| `0x1B` EnterWorld | `EnterWorld:972` | ✔ `PlayerMobile` `World.cs:194` | ✔ `World.Clear()` `World.cs:191`; `InternalMapChangeClear(true)` `World.cs:965` | ✔ | `Clear` empties both dictionaries without pooling (`World.cs:936-937`) |
| `0x1D` DeleteObject | `DeleteObject:1105` | — | ✔ `RemoveMobile(serial,true)` `:1230`; `RemoveItem(serial,true)` `:1259` | — | `HouseManager.Remove` `:1239`; recursive child removal `World.cs:667-674` |
| `0x20` UpdatePlayer | `UpdatePlayer:1268` | Chunks only (`Map.cs:97-98`) | ✔ every bank item `PlayerMobile.cs:1507` | — | `bank.Items = null` `PlayerMobile.cs:1512`; bank gumps `:1515`, `:1517` |
| `0x25` UpdateContainedItem | `UpdateContainedItem:1720` → `AddItemToContainer:6439` | ✔ | ✔ `RemoveItem(item,true)` `:6436`, `RemoveMobile(serial,true)` `:6429` | ✔ | `GridLootGump` at `:6519-6520` |
| `0x27` DenyMoveItem | `DenyMoveItem:1744` | ✔ `:1795`, `:6439` | ✔ `:1828` | ✔ | `SplitMenuGump` disposed `:1849` |
| `0x2E` EquipItem | `EquipItem:1978` | ✔ `:1987` | — | ✔ `World.cs:565-569` | `RemoveItemFromContainer` `:1992` |
| `0x2F` Swing | `Swing:2044` | — | ✔ via `CloseBank` `PlayerMobile.cs:1507` | — | |
| `0x3C` UpdateContainedItems | `UpdateContainedItems:2283` | ✔ `:6439` per record | ✔ `ClearContainerAndRemoveItems` `:6908`; `RemoveMobile` `:6429`; `RemoveItem` `:6436` | ✔ | |
| `0x77` UpdateCharacter | `UpdateCharacter:3019` | ✔ `:6596` (guarded unreachable by the null check `:3029-3032`) | — | ✔ `World.cs:589-593` | |
| `0x78` UpdateObject | `UpdateObject:3059` | ✔ `:6596`, `:6615`, `:3153` per equipment record | ✔ every non-`Opened`, non-`Backpack` child `:3109` | ✔ | `LoadMulti` → `house.ClearComponents` `Item.cs:359` |
| `0x88` OpenPaperdoll | `OpenPaperdoll:3302` | gumps only `:3326`, `:3341` | — | — | |
| `0x89` CorpseEquipment | `CorpseEquipment:3362` | ✔ `:3391` | — | ✔ `World.cs:565`, `:569` | |
| `0x97` MovePlayer | `MovePlayer:3536` | — | ✔ `CloseBank` `PlayerMobile.cs:1507` | — | |
| `0x99` MultiPlacement | `MultiPlacement:3587` | ✔ (next frame) `GameScene.cs:999` `Item.Create(0)` | — | ✔ serial `0` | `MultiTargetInfo` `TargetManager.cs:332` |
| `0xBF` ExtendedCommand | `ExtendedCommand:4423` | ✔ `:4810` spellbook; ✔ `:4830` `Item.Create(cc)` with spell index as serial | ✔ `spellbook.Clear()` `:4812`; `HouseManager.Remove` `:4854`; whole map on cmd 8 / cmd 0x18 (`World.cs:113/117/127`, `:4720-4722`) | ✔ | `new Map.Map` `World.cs:141/149` |
| `0xC0`,`0x70`,`0xC7` GraphicEffect | `GraphicEffect:2657` | effects only `EffectManager.cs:109/145/169/188/212` | effects self-destroy `EffectManager.cs:50-53`, `GameEffect.cs:125-128` | — | `GameEffect.Source`/`Target` are raw refs |
| `0xD1` Logout | `Logout:5179` | `LoginScene` `:5195` | ✔ `World.Clear()` `World.cs:920-963` | — | dictionaries emptied without pooling |
| `0xD2` UpdateCharacter | `UpdateCharacter:3019` | ✔ only if `World.Get` disagrees with `World.Mobiles` (`:6590`) | — | ✔ `World.cs:589/593`, `:565/569` | |
| `0xD3` UpdateObject | `UpdateObject:3059` | ✔ `:6596`, `:6615`, `:3153` | ✔ `:3107-3110` | ✔ | same handler as `0x78` |
| `0xD8` CustomHouse | `CustomHouse:5551` | `new House` `:5579`; one `Multi` per tile via `House.Add` (`House.cs:81`) | ✔ every existing `Multi` — `ClearComponents(true)` `:5584`, `ClearCustomHouseComponents(0)` `:5604`, again at `HouseCustomizationManager.cs:152` | — | `Multi.Destroy` pools immediately (`Multi.cs:118`) |
| `0xF3` UpdateItemSA | `UpdateItemSA:6076` | ✔ `:6596`, `:6615`; `new House` + `Multi.Create` `Item.cs:342-346, 459-470, 531-546` | ✔ `house.ClearComponents()` `Item.cs:359` | ✔ `World.cs:563-572`, `:587-596` | |
| `0xF6` BoatMoving | `BoatMoving:6127` | ✔ `:6596`/`:6615` for entities that exist but are destroyed (`:6590`) | ✔ `CloseBank` `:6837` → `PlayerMobile.cs:1507` | ✔ | `BoatMovingManager.ClearEntities` `BoatMovingManager.cs:188` |
| `0xF7` PacketList | `PacketList:6288` | ✔ `:6615` items, `:6596` mobiles | ✔ evicted stale entries `World.cs:565/569`, `:589/593`; `CloseBank` `:6837`, `CloseRangedGumps` `:6875` | ✔ | wraps many sub-packets |

### 6.2 Ids that touch gumps / managers only (no entity create or destroy)

| Id | Handler:line | Creates | Destroys |
| --- | --- | --- | --- |
| `0x0B` Damage | `Damage:550` | `OverheadDamage` `WorldTextManager.cs:136`; pooled `TextObject` `EntityTextContainer.cs:114`; `TextBox` `:135`; `AverageOverTime` `GameObject.cs:141` | oldest `TextObject` past 10 → `Destroy()` → pooled `TextObject.cs:104` |
| `0x1C` Talk | `Talk:1046` | `TextObject` `MessageManager.cs:317` | — |
| `0x24` OpenContainer | `OpenContainer:1416` | `SpellbookGump` `:1437`, `ModernShopGump` `:1465` / `ShopGump` `:1467`, `GridLootGump` `:1551`, `GridContainer` `:1655`, `ContainerGump` `:1684` | existing same-serial gumps `:1435`, `:1458-1459`, `:1550`, `:1671`; **every child Item force-removed** `:1714` |
| `0x2C` DeathScreen | `DeathScreen:1903` | `WMapEntity` `:1920` | — |
| `0x2D` MobileAttributes | `MobileAttributes:1935` | shared `RenderedText` `Entity.cs:161` | — |
| `0x38` Pathfinding | `Pathfinding:2269` | `PathNode`s `Pathfinder.cs:891` | previous run's nodes returned `Pathfinder.cs:1078`, `:1086` |
| `0x3A` UpdateSkills | `UpdateSkills:2099` | `SkillEntry` `:2122`, `StandardSkillsGump` `:2156`, `SkillGumpAdvanced` `:2163` | — |
| `0x3B` CloseVendorInterface | `CloseVendorInterface:2322` | — | `ShopGump` `:2331` |
| `0x54` PlaySoundEffect | `PlaySoundEffect:2387` | `UOSound` `AudioManager.cs:187` | — |
| `0x55` LoginComplete | `LoginComplete:2447` | `GameScene` `:2451`; gumps from `gumps.xml` `Profile.cs:859+`, added `:2488` | previous `Scene` `GameController.cs:302` |
| `0x56` MapData | `MapData:2494` | `PinControl` `MapGump.cs:195` | all pins on Clear `MapGump.cs:232,235` |
| `0x65` SetWeather | `SetWeather:2544` | in-place `WeatherEffect` slots `Game/Weather.cs:198-203` | `Reset()` `Game/Weather.cs:99` |
| `0x6C` TargetCursor | `TargetCursor:432` | `QuestionGump` in `TargetManager.Target` | placement-preview house `TargetManager.cs:297` → `House.ClearComponents` |
| `0x6D` PlayMusic | `PlayMusic:2405` | `UOMusic` `AudioManager.cs:341,353` | previous music `AudioManager.cs:448-450` |
| `0x6F` SecureTrading | `SecureTrading:462` | `TradingGump` `:491` | `TradingGump` `:495` |
| `0x71` BulletinBoardData | `BulletinBoardData:2746` | `BulletinBoardGump` `:2771`, `BulletinBoardObject` `BulletinBoardGump.cs:163`, `BulletinBoardItem` `:2861-2875` | same-serial gump `:2763-2766`; same-serial child `BulletinBoardGump.cs:157` |
| `0x74` BuyList | `BuyList:2900` | `ModernShopGump` `:2927` / `ShopGump` `:2939-2940` | existing `:2926`, `:2931-2934` |
| `0x7C` OpenMenu | `OpenMenu:3199` | `MenuGump` `:3216` / `GrayMenuGump` `:3251` | nothing — menus stack |
| `0x90` / `0xF5` DisplayMap | `DisplayMap:3403` | `MapGump` `:3414`, `Texture2D` `MultiMap.cs:29` | previous texture `MapGump.cs:172` |
| `0x93` / `0xD4` OpenBook | `OpenBook:3458` | `ModernBookGump` `:3485-3491`, one `Label` per wire page `ModernBookGump.cs:237-257` | — |
| `0x95` DyeData | `DyeData:3513` | `ColorPickerGump` `:3530` | existing `:3528` |
| `0x9E` SellList | `SellList:3621` | `ModernShopGump` `:3648` / `ShopGump` `:3650` | both existing `:3643`, `:3645`; agent path `BuySellAgent.cs:275` |
| `0xA5` OpenUrl | `OpenUrl:3776` | OS process `PlatformHelper.cs:54-68` | — |
| `0xA6` TipWindow | `TipWindow:3786` | `TipNoticeGump` `:3807` | — |
| `0xA8` ServerListReceived | `:6314` | `ServerListEntry[]` `LoginScene.cs:634`, entries `:987-1031`, `Ping` `:978` | all previous entries `LoginScene.cs:633`, `:956-971` |
| `0xA9` ReceiveCharacterList | `:6359` | `CityInfo` `LoginScene.cs:808, 827` | previous arrays by reassignment `:760`, `:773` |
| `0xAA` AttackCharacter | `AttackCharacter:3810` | health bars `TargetManager.cs:208/210/218/220` | none — `LastAttackBar` re-pointed `TargetManager.cs:202` |
| `0xAB` TextEntryDialog | `:3826` | `TextEntryDialogGump` `:3847-3860` | none — dialogs stack |
| `0xAE` UnicodeTalk | `:3865` | `TextObject` `MessageManager.cs:242-251, 294` | — |
| `0xAF` DisplayDeath | `DisplayDeath:4007` | `CorpseInfo` `CorpseManager.cs:55` | nothing — the dying mobile is re-keyed, not destroyed |
| `0xB0` OpenGump | `:4064` → `CreateGump:6969` | `Gump` `:6969-6978` + one control per layout token (`:7002`–`:7616`) | none; reuse path calls `Clear()` on the existing gump `:6954` |
| `0xB2` ChatMessage | `:4124` | `ChatChannel` `ChatManager.cs:99` | `ChatGump` `:4159`; channels `:4144`, `:4156` |
| `0xB8` CharacterProfile | `:4276` | `ProfileGump` `:4291` | existing `:4289` |
| `0xB9` EnableLockedFeatures | `:4296` | `BodyConvInfo` `AnimationsLoader.cs:626` | nothing — entries overwritten in place |
| `0xBA` DisplayQuestArrow | `:4332` | `QuestArrowGump` `:4351` | `:4362` |
| `0xC1` / `0xCC` DisplayClilocString | `:5041` | `TextObject` `MessageManager.cs:317` | every `PartyInviteGump` on cliloc 1008092 / 1005445 `:5063-5072` |
| `0xD6` MegaCliloc | `:5204` | `ItemProperty` `ObjectPropertiesListManager.cs:53`; `ItemPropertiesData` `GridHighLightData.cs:163` | — |
| `0xDD` OpenCompressedGump | `:5684` | rented buffers `:5697`, `:5736`; whatever `CreateGump` builds | buffers returned `:5714`, `:5799` |
| `0xDF` BuffDebuff | `:5826` | `BuffIcon` `PlayerMobile.cs:268`, `:274` | previous icon overwritten `:268` / removed `:290` |
| `0xE5` DisplayWaypoint | `:5960` | `WMapEntity` `WorldMapEntityManager.cs:160` | — |
| `0xE6` RemoveWaypoint | `:6005` | — | `WMapEntity` dropped `WorldMapEntityManager.cs:190` |
| `0xF0` KrriosClientSpecial | `:6012` | `WMapEntity` `WorldMapEntityManager.cs:160` | stale entities `:6053` → `WorldMapEntityManager.cs:209-225` |
| `0x86` UpdateCharacterList | `:6344` | `CharacterSelectionGump` `LoginScene.cs:667`, `LoadingGump` `:671-672` | `:663`, `:665` |
| `0x8C` ReceiveServerRelay | `:6329` | `AsyncNetClient` + socket `LoginScene.cs:737, 741` | previous socket `:736`; buffered bytes `:169`, `:174` |

### 6.3 Ids that neither create nor destroy anything tracked

`0x03` ClientTalk `:532` · `0x11` CharacterStatus `:571` · `0x15` FollowR `:794` ·
`0x16`/`0x17` NewHealthbarUpdate `:800` · `0x21` DenyWalk `:1289` (chunk only,
`Map.cs:97-98`) · `0x22` ConfirmWalk `:1309` · `0x23` DragAnimation `:1330`
(effects only) · `0x28` EndDraggingItem `:1879` (does **not** call
`ItemHold.Clear()`) · `0x29` DropItemAccepted `:1890` · `0x32` `:2097` ·
`0x4E` PersonalLightLevel `:2334` · `0x4F` LightLevel `:2359` · `0x53`/`0x82`/`0x85`
ReceiveLoginRejection `:6374` · `0x5B` SetTime `:2542` · `0x66` BookData `:2566` ·
`0x6E` CharacterAnimation `:2630` · `0x72` Warmode `:2885` · `0x73` Ping `:2895` ·
`0x98` UpdateName `:3547` · `0x9A` ASCIIPrompt `:3607` · `0xA1` UpdateHitpoints `:3713` ·
`0xA2` UpdateMana `:3738` · `0xA3` UpdateStamina `:3757` · `0xB7` Help `:4274` ·
`0xBB` UltimaMessengerR `:4367` · `0xBC` Season `:4369` · `0xBD` ClientVersion `:4407` ·
`0xBE` AssistVersion `:4412` · `0xC2` UnicodePrompt `:5153` · `0xC4` Semivisible `:5167` ·
`0xC6` InvalidMapEnable `:5169` · `0xC8` ClientViewRange `:2739` (changes what the
phase-4a cull condemns — `World.cs:356`, `:436-444`) · `0xCA` `:5173` · `0xCB` `:5175` ·
`0xCE` EnhancedPacketHandler `:347` · `0xD0` `:5177` · `0xD7` GenericAOSCommandsR `:5377` ·
`0xDB` CharacterTransferLog `:5668` · `0xDC` OPLInfo `:5670` · `0xDE` UpdateMobileStatus `:5815` ·
`0xE2` NewCharacterAnimation `:5930` · `0xE3` KREncryptionResponse `:5958` ·
`0xF1` FreeshardListR `:6074`.

Handler registration table: `Network/PacketHandlers.cs:236-343`
(`0x1B`→`:236`, `0x1A`→`:245`, `0x1D`→`:247`, `0x20`→`:248`, `0x22`→`:250`,
`0x25`→`:253`, `0x2E`→`:259`, `0x3C`→`:265`, `0x77`→`:282`, `0x78`→`:283`,
`0x89`→`:286`, `0xF3`→`:343`).
