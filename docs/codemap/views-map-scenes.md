# views-map-scenes

Partition = `src/ClassicUO.Client/Game/GameObjects/Views/` + `Game/Map/` + `Game/Scenes/`.
16 files, 10,197 lines, all read in full. Branch: `claude/new-chat-session-0zuwmx` (off `legacy`, net472).

This is the per-frame render pipeline: the tile store (Chunk/Map), the traversal +
sort + alpha + hit-test pass (GameSceneDrawingSorting), the draw calls (Views/), and
the two scenes (GameScene, LoginScene).

---

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `Game/GameObjects/Views/View.cs` | 317 | `partial GameObject`: draw-common state (`AlphaHue`, `AllowedToDraw`, `FrameInfo`, `IsFlipped`), `CalculateDepthZ()`, `GetOnScreenRectangle()`, and the static `DrawStatic/DrawGump/DrawStaticRotated/DrawStaticAnimated` helpers all views funnel through. |
| `Views/LandView.cs` | 202 | `Land.Draw` (stretched texmap vs flat art, animated-water wobble) + `Land.CheckMouseSelection`. |
| `Views/StaticView.cs` | 171 | `Static.Draw`/`TransparentTest`/`CheckMouseSelection`; tree→stump substitution, foliage/rock shadow flag. |
| `Views/MultiView.cs` | 178 | `Multi.Draw` with custom-house `CUSTOM_HOUSE_MULTI_OBJECT_FLAGS` handling, house-preview 50% alpha. |
| `Views/ItemView.cs` | 721 | `Item.Draw`, `DrawCorpse`, static `DrawLayer` (corpse equipment layers), `CheckMouseSelection` (art pixel check / per-layer animation pixel check). |
| `Views/MobileView.cs` | 1339 | `Mobile.Draw` (mount, shadow, sitting, 23 equipment layers), `DrawInternal`, `CalculateSitAnimation`, `CheckMouseSelection`, `IsCovered` (layer occlusion table). |
| `Views/GameEffectView.cs` | 262 | `GameEffect.Draw`; 5 lazily-built `BlendState`s keyed off `GraphicEffectBlendMode`. |
| `Views/LightningEffectView.cs` | 83 | `LightningEffect.Draw` — additive gump blit. |
| `Game/Map/Chunk.cs` | 491 | 8×8 tile block. Pooled. Loads land+statics from `MapLoader`, maintains the per-cell `TPrevious/TNext` priority-sorted linked list (`AddGameObject`/`RemoveGameObject`), `Destroy`/`Clear`/`HasNoExternalData`. |
| `Game/Map/Map.cs` | 326 | Per-facet chunk array + `_usedIndices` LinkedList; `GetChunk`/`GetTile`/`GetTileZ`/`GetMapZ`/`CalculateNearZ` (recursive roof walk), `ClearUnusedBlocks`, `Destroy`. |
| `Game/Scenes/Scene.cs` | 105 | Abstract scene: `Camera(0.3f,3.0f,0.05f)`, Load/Unload/Update/Draw, virtual input hooks. |
| `Game/Scenes/SceneType.cs` | 40 | `enum SceneType { None, Login, Game }`. |
| `Game/Scenes/GameScene.cs` | 1713 | Scene lifecycle, light accumulation (`AddLight`), `FillGameObjectList`, `Update` (all per-frame managers), `Draw` + render targets + post-FX (point/linear/aniso/XBR), death screen, follow mode. |
| `Game/Scenes/GameSceneDrawingSorting.cs` | 1265 | `UpdateMaxDrawZ`, foliage transparency, `ProcessAlpha`, circle-of-transparency, `PushToRenderList`, `AddTileToRenderList` (the big per-object dispatch), `GetViewPort`. |
| `Game/Scenes/GameSceneInputHandler.cs` | 1818 | Mouse/keyboard/controller handling, drag-select, item drop, targeting dispatch, macro/walk flags. |
| `Game/Scenes/LoginScene.cs` | 1166 | Login state machine (`LoginSteps`), reconnect loop, server/character list parsing, `ServerListEntry` (ICMP ping), `CityInfo`. |

---

## Types

| Type | file:line | Role |
| --- | --- | --- |
| `GameObject` (partial, view half) | View.cs:52 | Draw contract + depth math shared by every renderable. |
| `ObjectHandlesStatus` | View.cs:44 | NONE/OPEN/CLOSED/DISPLAYING for name-plate handles. |
| `Chunk` | Chunk.cs:42 | Pooled 8×8 `GameObject[,] Tiles` block; owns per-cell intrusive list. |
| `Map` | Map.cs:41 | One facet. Owns the chunk array and used-index list. |
| `Scene` | Scene.cs:42 | Base scene. |
| `SceneType` | SceneType.cs:35 | Scene tag enum. |
| `GameScene` | GameScene.cs:58 | The in-world scene (partial across 3 files). |
| `GameScene.LightData` | GameScene.cs:1704 | struct {ID, Color, IsHue, DrawX, DrawY}. |
| `GameScene.TreeUnion` | GameSceneDrawingSorting.cs:1253 | struct {Start,End} graphic range for multi-tile trees. |
| `LoginSteps` | LoginScene.cs:57 | Login state machine states. |
| `LoginScene` | LoginScene.cs:71 | Login scene. |
| `ServerListEntry` | LoginScene.cs:974 | Server row + `System.Net.NetworkInformation.Ping`. |
| `CityInfo` | LoginScene.cs:1131 | Starting-city record. |

---

## State

Static / process-wide:
- `Chunk._pool` — `QueuedPool<Chunk>(300)`, reset callback sets `LastAccessTime = Time.Ticks + 3000`, `IsDestroyed=false` — Chunk.cs:44.
- `Map._terrainChunks` — **static** `Chunk[]`, shared across all `Map` instances; grown only if a new facet needs more blocks — Map.cs:43, 52.
- `Map._blockAccessList` — static `bool[0x1000]`, scratch for `CalculateNearZ` — Map.cs:44.
- `GameScene._foliages` — static `GameObject[100]`, grows by 50 — GameSceneDrawingSorting.cs:50, resize at :371.
- `GameScene._treeInfos` — static readonly 9 `TreeUnion` ranges — GameSceneDrawingSorting.cs:51.
- `GameScene._xbr` — static `XBREffect`, disposed+nulled in `Unload` — GameScene.cs:81, 486.
- `GameScene._filterMode` — **static string**, written from per-profile settings — GameScene.cs:100, 182-193.
- `GameScene._youAreDeadText` — static `RenderedText`, never disposed — GameScene.cs:1651.
- `GameScene._darknessBlend` / `_altLightsBlend` — static Lazy BlendState — GameScene.cs:60, 70.
- `GameEffect._multiplyBlendState`, `_screenBlendState`, `_screenLessBlendState`, `_normalHalfBlendState`, `_shadowBlueBlendState` — static Lazy — GameEffectView.cs:18-87.
- `Item._equipConvData` — **static** `EquipConvData?` scratch shared by `DrawLayer` and `CheckMouseSelection` — ItemView.cs:51.
- `Mobile._equipConvData`, `_characterFrameStartY`, `_startCharacterWaistY`, `_startCharacterKneesY`, `_startCharacterFeetY`, `_characterFrameHeight` — **static** sit-animation scratch — MobileView.cs:50-55.
- `ServerListEntry._buffData` (32B), `_pingOptions` — static, shared across all pinging entries — LoginScene.cs:1044-1045.
- `LoginScene.Account` — static string — LoginScene.cs:88.

Per-`Map`:
- `_usedIndices` `LinkedList<int>` — Map.cs:45; nodes stored back on `Chunk.Node` (Chunk.cs:57).

Per-`Chunk`: `Tiles[8,8]`, `IsDestroyed`, `LastAccessTime`, `Node`, `X`, `Y` — Chunk.cs:54-61.

Per-`GameScene` (render-list plumbing, all reset each frame in `FillGameObjectList`):
- `_renderListStaticsHead/_renderList/_renderListStaticsCount` — GameSceneDrawingSorting.cs:80-82.
- `_renderListTransparentObjectsHead/…Count` — :85-87.
- `_renderListAnimationsHead/…Count` — :90-92.
- `_renderListEffectsHead/…Count` — :94-96.
- `_maxGroundZ`, `_maxZ`, `_noDrawRoofs` — :64-69, written only by `UpdateMaxDrawZ`.
- `_minPixel/_maxPixel/_minTile/_maxTile/_offset/_last_scaled_offset/_lastCamOffset` — :66-73, written by `GetViewPort`.
- `_oldPlayerX/Y/Z` — :74-76, the `UpdateMaxDrawZ` early-out cache.
- `FoliageIndex` (sbyte, cycles 1..99) — :98, incremented per frame at GameScene.cs:742.
- `_foliageCount` — :77.
- `_lights[LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT]` + `_lightCount` — GameScene.cs:89, 88; drained/zeroed in `PrepareLightsRendering` (:1471).
- `_multi` (multi-placement preview Item) — GameScene.cs:92, created :999, destroyed :1059.
- `_world_render_target`, `_light_render_target` — GameScene.cs:108.
- `_time_cleanup` (init `Time.Ticks+5000`), `_timePing`, `_alphaTimer`, `_nextProfileSave`, `_lastResync`, `_timeToPlaceMultiInHouseCustomization` — GameScene.cs:80,95,83,111,140,97.
- Input: `_flags[5]`, `_isMouseLeftDown`, `_isSelectionActive`, `_selectionStart/_selectionEnd`, `_rightMousePressed`, `_continueRunning`, `_boatRun/_boatIsMoving/_lastBoatDirection`, `_requestedWarMode`, `_holdMouse2secOverItemTime` — GameSceneInputHandler.cs:54-65.
- `_useItemQueue`, `_moveItemQueue`, `Hotkeys`, `Macros`, `InfoBars`, `Weather`, `_healthLinesManager`, `_animatedStaticsManager` — GameScene.cs:105-118.
- Follow mode is stored on the **profile**, not the scene: `_followingMode`/`_followingTarget` are property wrappers over `ProfileManager.CurrentProfile` — GameScene.cs:129-138.

Per-`LoginScene`: `_currentGump`, `_lastLoginStep`, `_pingTime`, `_reconnectTime`, `_reconnectTryCounter`, `_autoLogin`, `Servers[]`, `Characters[]`, `Cities[]` — LoginScene.cs:73-89.

---

## Timing

**Per frame (GameScene.Update, called from GameController's update loop):**
- `SelectedObject.TranslatedMousePositionByViewport = Camera.MouseToWorldPosition()` first (GameScene.cs:873) — everything downstream hit-tests against it.
- `World.Map.ClearUnusedBlocks()` gated by `_time_cleanup`: first fire at +5000 ms, thereafter **every 500 ms** (GameScene.cs:877-881). Chunk eligibility inside: `LastAccessTime < Time.Ticks - 3000` (`CLEAR_TEXTURES_DELAY`) and `HasNoExternalData()`; at most 50 chunks per call (`MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR`) — Map.cs:269-295.
- `PacketHandlers.SendMegaClilocRequests()` (:883).
- Ping every **1000 ms** (`_timePing`, :899-903).
- Force-resync if no pong for **5000 ms** and last resync >5000 ms ago (`ForceResyncOnHang`) — :905-911.
- `World.Update()`, `HouseDiagnostics.LogHouseContents()`, `_animatedStaticsManager.Process()`, `BoatMovingManager.Update()`, `Pathfinder.ProcessAutoWalk()`, `DelayedObjectClickManager.Update()` (:913-918).
- `_useItemQueue.Update()`, `AutoLootManager.Instance.Update()`, `_moveItemQueue.ProcessQueue()`, `GridHighlightData.ProcessQueue()` (:930-934).
- Movement: mouse → controller → keyboard `_flags` (:936-949). Follow mode re-pathfinds every frame while active (:951-975).
- `Macros.Update()` (:977).
- Profile autosave every **1 hour** (`1000*60*60`) — :979-983.
- Multi-placement preview rebuild: every frame while `CursorTarget.MultiPlacement`, re-positions every `house.Components` entry (:997-1055).
- House-customization tile paint throttled to **50 ms** (`_timeToPlaceMultiInHouseCustomization`, :1082); pick-up-on-hold fires after **1000 ms** of left-down (:1087).

**Per frame (GameScene.Draw):**
- `DrawWorld` → `SelectedObject.Object = null` then `FillGameObjectList()` (GameScene.cs:1285-1286). Note this runs inside Draw, not Update.
- `FillGameObjectList`: resets all four render lists, `_alphaChanged = _alphaTimer < Time.Ticks` with the timer re-armed to `+ALPHA_TIME (20 ms)` (:735-740); `FoliageIndex++` wrapping at 100 → 1 (:742-747); `GetViewPort()` (→ `UpdateMaxDrawZ()`); two diagonal sweeps over `[_minTile,_maxTile]` calling `AddTileToRenderList` (:790-828); foliage alpha pass (:830-845); `UpdateTextServerEntities` over **all** mobiles then all items (:847-848).
- Draw order: statics list, animations list, effects list, then transparents with `DepthStencilState.DepthRead` (:1301-1325); multi preview (:1350); weather (:1362).
- Lights: accumulated during object `Draw` via `AddLight`, consumed and `_lightCount` zeroed in `PrepareLightsRendering` (:1442-1471). `PrepareLightsRendering` is called **before** `DrawWorld` in the direct path (:1202-1205) but **after** it in the render-target path (:1244-1246, :1254-1256).
- Death screen short-circuits the whole world draw while `World.Player.DeathScreenTimer > Time.Ticks` (:1114, :1667).

**On demand / event:**
- `Chunk.Load` on first `Map.GetChunk` for a block (Map.cs:96-99) — reads mmapped `MapBlock`/`StaticsBlock` pointers.
- `UpdateMaxDrawZ` early-outs unless player X/Y/Z changed or `force` (GameSceneDrawingSorting.cs:106-111).
- Input handlers on SDL events (GameSceneInputHandler.cs:452-1802).

**LoginScene:** `Update` polls the step machine each frame; reconnect retry interval = `Settings.ReconnectTime * 1000`, floored at **1000 ms** (LoginScene.cs:215-222); keepalive ping every **60000 ms** while on char-select/creation (:227-237).

---

## Inbound

- `GameController` / `Client.Game.SetScene` → `Scene.Load/Unload/Update/Draw` and the `On*` input hooks (Scene.cs:62-103).
- `Client.Game.GetScene<GameScene>()` is called from inside the views themselves to reach `AddLight` and `FoliageIndex`: StaticView.cs:130,142; MultiView.cs:129,142; ItemView.cs:66; MobileView.cs:459,470,844; GameEffectView.cs:250.
- `DrawRenderList` → `GameObject.Draw(batcher, RealScreenPosition.X/Y, depth)` (GameScene.cs:1382-1402) — the only caller of the Views in normal flow; the multi preview is drawn directly at :1350.
- `PushToRenderList` → `GameObject.CheckMouseSelection()` (GameSceneDrawingSorting.cs:672).
- Network packet handlers call into `LoginScene.ServerListReceived / ReceiveCharacterList / UpdateCharacterList / HandleErrorCode / HandleRelayServerPacket`.
- `World` / `Pathfinder` / `GameActions` / light and movement code call `Map.GetTile`, `GetChunk`, `GetTileZ`, `GetMapZ`, `CalculateNearZ`.
- `GameScene.DoubleClickDelayed(serial)` (GameScene.cs:199) is the public entry into `_useItemQueue`.

## Outbound

- Renderer: `UltimaBatcher2D` (Draw/DrawShadow/DrawStretchedLand/DrawCharacterSitted/SetBlendState/SetStencil/SetSampler/Begin/End), `ShaderHueTranslator`, `XBREffect`, `SolidColorTextureCache`, `RenderedText`.
- Assets: `Client.Game.Arts/Gumps/Texmaps/Lights/Animations`, `ArtLoader.Instance.GetValidRefEntry`, `GumpsLoader`, `TileDataLoader.Instance.StaticData/LandData`, `AnimationsLoader.Instance` (`GetAnimDirection`, `FixSittingDirection`, `GetDeathAction`, `EquipConversions`), `MapLoader.Instance` (`GetIndex`, `BlockData`, `MapBlocksSize`, `SanitizeMapIndex`), `ClilocLoader`, `LightsLoader`.
- Managers: `SelectedObject`, `TargetManager`, `NameOverHeadManager`, `UIManager`, `ProfileManager.CurrentProfile` (read on nearly every draw branch), `SpellVisualRangeManager`, `TileMarkerManager`, `AuraManager`, `StaticFilters`, `Pathfinder`, `BoatMovingManager`, `DelayedObjectClickManager`, `AutoLootManager`, `MessageManager`, `CommandManager`, `IgnoreManager`, `World.*`.
- Network: `NetClient.Socket` / `AsyncNetClient` (`Send_Resync`, `Send_ChangeWarMode`, `Send_SelectServer`, `Send_SelectCharacter`, `Send_CreateCharacter`, `Send_DeleteCharacter`, `Send_Seed`, `Send_FirstLogin`, `Send_SecondLogin`), `PacketHandlers.SendMegaClilocRequests`, `Plugin.OnConnected/OnDisconnected`, `EventSink`.
- Fork subsystems: `LegionScripting.Init/Unload`, `GridContainerSaveData`, `SpellBarManager`, `BuySellAgent`, `OrganizerAgent`, `DressAgentManager`, `FriendsListManager`, `PersistentVars`, `GraphicsReplacement`, `XmlGumpHandler`, `HouseDiagnostics`, `AnonMetrics`, `JournalFilterManager`, `UISettings`, `BandageManager`, `MoveItemQueue`, `GridHighlightData`.

---

## Hazards

Factual observations, with line numbers. No causal claims.

- `Map.cs:43,52-53` — `_terrainChunks` is `static` but `BlocksCount`/`Index` are per-instance. Constructing a `Map` for a second facet reuses the same array unless the new facet needs more blocks; the array is not cleared on facet change, so entries belonging to the previous facet remain addressable via `GetChunk(block)`.
- `Map.cs:82` — bounds check is `block >= BlocksCount || block >= _terrainChunks.Length`; a negative `block` (from a large `blockX*stride + blockY`, or negative inputs surviving the `x<0||y<0` filter at :73) is not rejected before the array index at :87.
- `Map.cs:186-241` — `CalculateNearZ` recurses on all four neighbours with no depth limit; recursion termination depends solely on the shared static `_blockAccessList`, which is only cleared by an explicit `ClearBockAccess()` call (done at GameSceneDrawingSorting.cs:222 before the call at :223, and once in the ctor at Map.cs:54).
- `Map.cs:262-265` — `GetUsedChunks()` iterates `_usedIndices` and calls `GetChunk(i)`, which can mutate chunk state; `ClearUnusedBlocks` (:274-294) walks the same list while `Chunk.Destroy` removes the node (Chunk.cs:422-425).
- `Chunk.cs:428` — `Destroy()` returns the chunk to `_pool` while `Map._terrainChunks[block]` may still hold the reference; `Map.ClearUnusedBlocks` nulls the slot at :285 only for the path it drives. `Chunk.Clear()` (:431-470) sets `IsDestroyed = true` but does **not** return to the pool, and `Map.GetChunk` at :101-114 revives an `IsDestroyed` chunk in place.
- `Chunk.cs:404-416` / `:444-458` — the destroy loop calls `first.Destroy()` and then dereferences `first.TPrevious/TNext` on the same object after destruction.
- `Chunk.cs:408,450` — `World.Player` is explicitly skipped from destruction but its `TPrevious/TNext` are still nulled and `Tiles[i,j]` is set to null, dropping the player out of the cell list.
- `Chunk.cs:172-354` — `AddGameObject` mutates the intrusive `TPrevious/TNext` list. `AddTileToRenderList` (GameSceneDrawingSorting.cs:732) walks that same list with `obj = obj.TNext` while calling `obj.CheckMouseSelection()` and `ProcessAlpha`.
- `GameSceneDrawingSorting.cs:294-301` — `ApplyFoliageTransparency` walks `tile → obj.TNext` and writes `obj.FoliageIndex`; it is called from `CheckIfBehindATree` (:364) which is itself called from inside the `AddTileToRenderList` walk over a (possibly the same) cell list.
- `GameSceneDrawingSorting.cs:616-640` — `HasSurfaceOverhead` calls `World.Map.GetTile(...)` for a 4×4 neighbourhood per mobile per frame; `GetTile` loads (and pool-allocates) chunks on demand, so the traversal can create chunks during the render walk.
- `GameSceneDrawingSorting.cs:98,300,336,363,368` — `FoliageIndex` is an `sbyte` cycled 1..99 per frame (GameScene.cs:742-747); objects hold last frame's value in `GameObject.FoliageIndex` and are compared for equality, so the value is reused every 99 frames.
- `GameSceneDrawingSorting.cs:377` — `_foliages[_foliageCount++]` stores raw `GameObject` references that are read again in `FillGameObjectList` at GameScene.cs:832-844 after the whole traversal; nothing clears stale slots beyond resetting `_foliageCount`.
- `GameSceneDrawingSorting.cs:665,675-685` — `PushToRenderList` sets `SelectedObject.Object` during traversal, comparing `CalculateDepthZ()` of candidates; `SelectedObject.Object` is also nulled mid-frame at GameScene.cs:1285 and again at :985-988 and :1485-1488.
- `GameSceneDrawingSorting.cs:1003-1007` — `ref StaticTiles itemData = ref (item.IsMulti ? ref TileDataLoader...StaticData[item.MultiGraphic] : ref item.ItemData)` takes a `ref` into the shared `StaticData` array, which `ItemView.cs:96-101` writes to (`SetImpassable`) during `Draw`.
- `GameSceneDrawingSorting.cs:1101` — `ref TileDataLoader.Instance.StaticData[effect.Graphic]` passed by ref into `ProcessAlpha` with no bounds check on `effect.Graphic` (contrast Map.cs:209 which does check).
- `GameSceneDrawingSorting.cs:100-261` — `UpdateMaxDrawZ` early-outs on unchanged player X/Y/Z (:106); `_maxZ`/`_maxGroundZ`/`_noDrawRoofs` therefore keep the values computed for the last position even if the chunk contents at that position changed. `_maxGroundZ` is overwritten at :254 with the local `maxGroundZ` computed at :117/:146, discarding the `CalculateNearZ` result assigned at :223 and the `pz16` assignment at :251.
- `GameSceneDrawingSorting.cs:734-736` — `UpdateRealScreenPosition` is applied inside the traversal, so an object's screen position for this frame depends on whether it was reached before the `screenX < _minPixel.X` break at :741-744 (which `break`s out of the whole cell list, not `continue`s).
- `ItemView.cs:91-105` — `Draw` mutates global tile data: `TileDataLoader.Instance.StaticData[Graphic].SetImpassable(true/false)` based on the player's current stamina, from inside the render loop.
- `ItemView.cs:395-408` — `if (color == 0) { if ((color & 0x8000) != 0) ... }`; the inner test can never be true because the outer condition already requires `color == 0`.
- `ItemView.cs:51,297,327,623` — `Item._equipConvData` is static and is written by both `DrawLayer` (:327) and `CheckMouseSelection` (:623); `CheckMouseSelection` never resets it to null.
- `ItemView.cs:678-714` — array access wrapped in `try/catch(Exception)` logging "This should not ever happen"; per-object, inside the draw/hit-test path.
- `MobileView.cs:50-55, 871-878` — sit-animation geometry (`_characterFrameStartY`, `_startCharacterWaistY`, …) is static and set on the body pass, then read by later equipment passes; correctness depends on the body being drawn first in the same `Draw` call.
- `MobileView.cs:694-717` — when `spriteInfo.Texture == null` and `charIsSitting`, control `goto SKIP`s past the x/y adjustment and then at :721 `batcher.DrawShadow(spriteInfo.Texture, …)` / :760 dereferences `spriteInfo` fields with a null texture in the shadow branch.
- `MobileView.cs:186-188` — `Mount` is re-validated against `World.Items.Get(mount.Serial)` inside `Draw` and nulled if missing; `CheckMouseSelection` (:1006) reads `Mount` without that check.
- `MobileView.cs:476-479` — `FrameInfo` is finalised at the end of `Draw`; `CheckMouseSelection` (:976) and the drag-select box (GameSceneInputHandler.cs:363-372) read `FrameInfo`, i.e. last frame's values for anything hit-tested before it is drawn.
- `View.cs:145-150, 228-231, 261-271` — `DrawStatic*` write back into the shared `ArtLoader` index entry (`index.Width/Height`) as a side effect of drawing; `Static.CheckMouseSelection` (StaticView.cs:155) and `Item.CheckMouseSelection` (ItemView.cs:528) read those same fields.
- `View.cs:91-105` — `CalculateDepthZ` has two branches with identical conditions: `Offset.X < 0 && Offset.Y > 0` appears at :91 ("South") and again at :102 ("West"); the second is unreachable.
- `GameScene.cs:597` — `AddLight` reads `_maxZ`, which is written by `UpdateMaxDrawZ` during `GetViewPort` in `FillGameObjectList`; `AddLight` is called later from object `Draw`.
- `GameScene.cs:1442-1471` — lights are drawn from `_lights[0.._lightCount]` and the count is reset there. In the direct (non-render-target) path `PrepareLightsRendering` runs at :1202 **before** `DrawWorld` at :1205, so the buffer drained is the one filled by the previous frame's draws.
- `GameScene.cs:1043-1053` — `World.HouseManager.TryGetHouse(...)` return value is discarded and `house.Components` is enumerated unconditionally on the next line.
- `GameScene.cs:1014` — `World.Map.GetChunk(gobj.X, gobj.Y)?.Tiles[cellX, cellY]` indexes with `gobj.X % 8`, `gobj.Y % 8`; negative coordinates would produce a negative index.
- `GameScene.cs:847-848` — `UpdateTextServerEntities` iterates `World.Mobiles.Values` and `World.Items.Values` every frame during `FillGameObjectList`, which is itself invoked from `Draw`.
- `GameScene.cs:100,182-193` — `_filterMode` is `static` but is assigned from the current profile in `SetPostProcessingSettings`; `_currentFilter`/`_postFx` (instance fields) are the cache keyed against it.
- `GameScene.cs:1651` — `_youAreDeadText` is a static `RenderedText` created at type-init and never disposed across scene reloads.
- `GameScene.cs:80` — `_time_cleanup` is `uint` initialised to `Time.Ticks + 5000` while compared against `Time.Ticks`; `_timePing` is `long` assigned `(long)Time.Ticks + 1000` (:902) — mixed widths for the same clock.
- `GameScene.cs:497-501` — `Unload` nulls `_useItemQueue`, `Hotkeys`, `Macros` but `Update` (:930, :977) dereferences them; ordering relies on the scene not being updated after unload.
- `GameSceneInputHandler.cs:341` — `UIManager.Gumps.OfType<NameOverheadGump>()` and :408-415 `UIManager.Gumps.OfType<BaseHealthBarGump>().OrderBy(...)` are enumerated inside the `foreach (Mobile mobile in World.Mobiles.Values)` loop at :300, while `UIManager.Add(hbgc)` at :442 adds to `UIManager.Gumps` within the same loop.
- `GameSceneInputHandler.cs:369-372` — `_rectanglePlayer` is reused as scratch by drag-select; the same field is the player's on-screen rect written each frame in `FillGameObjectList` (GameScene.cs:766-779) and read by `CheckIfBehindATree` (GameSceneDrawingSorting.cs:359).
- `GameSceneInputHandler.cs:1611-1623, 1742-1754` — the WASD/arrow key arrays are allocated on every key event.
- `GameSceneInputHandler.cs:855` — `ProfileManager.CurrentProfile.SavedMountSerial = m;` assigns a `Mobile` to a serial field (implicit conversion), then `World.Get(m)` on the next line.
- `LoginScene.cs:736,741` — `NetClient.Socket.Disconnect().Wait()` and `Connect(...).Wait()` block synchronously inside the packet-handling path.
- `LoginScene.cs:242-253` — `foreach (Item item in World.Items.Values) World.RemoveItem(item);` then `World.Items.Clear()` — removal during enumeration of the live collection.
- `LoginScene.cs:1044-1045` — `_buffData` and `_pingOptions` are static and shared by every `ServerListEntry` pinging concurrently; `_resultIndex` is passed as the async user-state and used to index `_last10Results` on completion (:1079-1086) after `_resultIndex` may have advanced.
- `LoginScene.cs:1101` — `PacketLoss = (Math.Max(1, PacketLoss) / Math.Max(1, _resultIndex)) * 100;` integer division before the ×100.
- `LoginScene.cs:697-710` — `charToSelect` stays 0 if `lastCharName` is not found, so autologin falls through to slot 0.
- `LoginScene.cs:559` — `GetCity(index)` checks `index < Cities.Length` but not `index >= 0`, and does not null-check `Cities`.

---

## Fork deltas

Clearly not stock ClassicUO (TazUO and/or Holiday additions):

- **Post-processing pipeline** — `_use_render_target`, `_filterMode` point/linear/anisotropic/**XBR**, `_postFx`, `_postSampler`, `EnsureRenderTargets`, `UpdatePostProcessState`, `BindXbrParams`, `SetPostProcessingSettings` (GameScene.cs:98-104, 1210-1649). Stock has a single fixed world render target.
- **GlobalScaling** — `profile.GlobalScaling`/`GlobalScale` branches throughout `Draw`, `DrawWorldDirect`, `DrawWorldRenderTarget`, `EnsureRenderTargets`, `GetViewPort` (GameScene.cs:1143, 1189-1200, 1233-1251; GameSceneDrawingSorting.cs:1148-1152).
- **`HouseDiagnostics`** — `LogHouseContents()` (GameScene.cs:914) and `LogDrawCeiling(_maxZ, _maxGroundZ, force, _noDrawRoofs)` (GameSceneDrawingSorting.cs:260) with an explanatory comment block at :257-259. Reads as a Holiday debugging addition.
- **Comment style with rationale prose** — Holiday-authored comments explain *why*: Chunk.cs:176-177 (`TileChunk` handover), Chunk.cs:367-368 (`TPrevious as well as TNext`), GameScene.cs:214-215 (music overlay survives relog), GameScene.cs:447-448 (`ForgetMusicState` / login music), GameSceneInputHandler.cs:186-190, :199-204 (`Keyboard.Refresh()` staleness), :264-265, :354-357 (drag-select must match `MobileView.CheckMouseSelection`). Stock ClassicUO comments are terse.
- **Drag-select filters** — `DragSelect_PlayersModifier`, `DragSelect_MonstersModifier`, `DragSelect_NameplateModifier`, `DragSelect_PlayersSkipInvulnerable`, `DragSelect_MonstersSkipFriendly`, `IsFriendlyForDragSelect`, `DragSelectAsAnchor`/`AnchorOffset`, `DragSelectModifierActive()` (GameSceneInputHandler.cs:66, 191-235, 300-352).
- **`GameObject.TileChunk`/`TileCellX`/`TileCellY`** back-references maintained in `Chunk.AddGameObject` (Chunk.cs:178-180) and cleared in `RemoveGameObject` (:372) — not in stock.
- **Managers loaded/unloaded by `GameScene`** that are TazUO-side: `LegionScripting`, `GridContainerSaveData`, `SpellBarManager`, `BuySellAgent`, `OrganizerAgent`, `DressAgentManager`, `FriendsListManager`, `AutoLootManager`, `SpellVisualRangeManager`, `TileMarkerManager`, `PersistentVars`, `GraphicsReplacement`, `XmlGumpHandler`, `JournalFilterManager`, `UISettings`, `BandageManager`, `MoveItemQueue`, `GridHighlightData`, `NameOverHeadManager`, `AnonMetrics` (GameScene.cs:208-267, 420-501).
- **Profile-driven render tweaks not in stock**: `DisplayRadius`/`DisplayRadiusDistance`/`DisplayRadiusHue` (LandView.cs:81-82, StaticView.cs:103-104), `AnimatedWaterEffect` wobble (View.cs:154-171, 285-301; LandView.cs:151-168), `PlayerConstantAlpha` (MobileView.cs:101), `HiddenBodyHue`/`HiddenBodyAlpha` (:126-127), `HiddenLayers`/`HideLayersForSelf` (:395), `OverridePartyAndGuildHue`/`FriendHue`/`DisableGrayEnemies` (:168-171), `HighlightMobilesByInvul/Paralize/Poisoned` (:142-162), `ForceTransparentHouse`/`ForcedTransparency` (MultiView.cs:71-72), `CircleOfTransparencyType == 2` branch (GameSceneDrawingSorting.cs:391-426), `EnableCaveBorder`/`ApplyCaveTileBorder` (GameScene.cs:266-267), `EnableDeathScreen` (:1662), `ForceResyncOnHang` (:905), `UseWASDInsteadArrowKeys` (GameSceneInputHandler.cs:1609), `PathfindSingleClick`, `DisableAutoFollowAlt`, `DisableHotkeys`, `ControllerEnabled`/`MoveCharByController` (:119-175), `PlayerOffset` (GameSceneDrawingSorting.cs:1156-1160).
- **`AutoAvoidObstacules` / `StaticFilters.isHumanAndMonster` / `IsOutStamina`** mutating tiledata during draw (ItemView.cs:91-105) — not stock.
- **`SpellVisualRangeManager` hue hooks** inside `Land.Draw` and `Static.Draw` (LandView.cs:72-79, StaticView.cs:93-101).
- **`GraphicsReplacement`, `AnonMetrics.TrackLoginFireAndForget`** (LoginScene.cs:468-469), **`InputRequest` server-IP prompt** on first run (LoginScene.cs:103-136).
- **`AsyncNetClient`** replacing the stock synchronous socket (LoginScene.cs:390, 737; GameSceneInputHandler.cs:1433, 1765).
