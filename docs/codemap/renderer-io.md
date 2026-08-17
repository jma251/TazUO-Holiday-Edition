# renderer-io

Partition = `src/ClassicUO.Renderer` + `src/ClassicUO.IO`. 62 files, 8146 lines, all read in full
(`wc -l` totals; verified per-file).

Two assemblies, one job each:

- **ClassicUO.IO** — memory-mapped raw file access (`.mul`/`.idx`/`.uop`), span-based
  packet/record readers and writers, `.def` text parsing, and the audio playback objects
  (`Sound`/`UOSound`/`UOMusic`) that wrap FNA `DynamicSoundEffectInstance`.
- **ClassicUO.Renderer** — the sprite batcher (`UltimaBatcher2D`), the shader hue encoding
  contract, the per-asset-class GPU caches (Art / Gump / Animations / Texmap / Light /
  MultiMap) built on `TextureAtlas` + `PixelPicker`, the camera, the scissor stack, XNB
  bitmap fonts, and a music-file resolver (`Renderer.Sounds.Sound`).

Neither assembly references `ClassicUO.Client`. Data flows one way: the client asks for a
`SpriteInfo`/`Sound` and gets one, lazily built on first request and cached forever.

## Files

### ClassicUO.IO (11 files, 1962 lines)

| Path | Lines | Purpose |
| --- | --- | --- |
| `src/ClassicUO.IO/DataReader.cs` | 285 | Unsafe little-endian cursor over a raw `byte*`; base class of `UOFile`. Bounds check is `[Conditional("DEBUG")]` only. |
| `src/ClassicUO.IO/UOFile.cs` | 125 | Memory-maps a file (`USE_MMF` always defined), acquires a raw pointer from the view handle, hands it to `DataReader.SetData`. |
| `src/ClassicUO.IO/UOFileMul.cs` | 94 | `.mul` + optional `.idx`; `FillEntries` walks 12-byte idx records into `UOFileIndex[]`. |
| `src/ClassicUO.IO/UOFileUop.cs` | 287 | `.uop` container: parses block chain into `Dictionary<ulong,UOFileIndex>`; `CreateHash` is the UOP filename hash. |
| `src/ClassicUO.IO/UOFileIndex.cs` | 109 | `UOFileIndex` struct (address/offset/length/compression/w/h/hue/AnimOffset) + static `Invalid`; `UOFileIndex5D`. |
| `src/ClassicUO.IO/UOFileLoader.cs` | 78 | Abstract base for the loaders in ClassicUO.Assets; owns `Entries[]` and `GetValidRefEntry`. |
| `src/ClassicUO.IO/UOFilesOverrideMap.cs` | 59 | Singleton `Dictionary<string,string>` of `file=path` overrides read from a text file. |
| `src/ClassicUO.IO/StackDataReader.cs` | 488 | `ref struct` span reader used by the packet layer; LE/BE ints, ASCII/Unicode/UTF8 strings, "safe" char filtering. |
| `src/ClassicUO.IO/StackDataWriter.cs` | 436 | `ref struct` span writer backed by `ArrayPool<byte>.Shared`; used to build outgoing packets. |
| `src/ClassicUO.IO/DefReader.cs` | 328 | Parser for UO `.def` text files (bodyconv, mounts, etc.); pre-parses all lines into `List<string[]>`. |
| `src/ClassicUO.IO/Audio/Sound.cs` | 201 | Abstract playable sound; owns the `DynamicSoundEffectInstance`, volume/volume-factor, spam delay. |
| `src/ClassicUO.IO/Audio/UOSound.cs` | 95 | One-shot wave buffer already in memory; `Delay = (len-32)/88.2f` ms. |
| `src/ClassicUO.IO/Audio/UOMusic.cs` | 190 | Streams an MP3 via `MP3Sharp` in 0x8000-byte (~0.9 s) chunks; loop/end callbacks. |

### ClassicUO.Renderer (49 files, 6184 lines)

| Path | Lines | Purpose |
| --- | --- | --- |
| `Batcher2D.cs` | 1596 | `UltimaBatcher2D` — the entire 2D draw path. Also `partial class Resources` with the two embedded `.fxc` shaders. |
| `SpriteFont.cs` | 559 | Reads an XNB SpriteFont out of an embedded byte span (incl. a hand-rolled DXT3 decompressor). |
| `Camera.cs` | 230 | World camera: zoom clamp/step, peek-toward-mouse lerp, screen↔world transform. |
| `Animations/Animation.cs` | 391 | `Animations` — per-body animation frame cache; atlas + pixel picker keyed by (id,action,dir,uop,frame). |
| `Arts/Art.cs` | 202 | `Art` — land/static art cache, cursor SDL surface builder, real (trimmed) art bounds. |
| `Sounds/Sound.cs` | 183 | Music/sound-effect object cache + music-era path resolution. **Holiday addition.** |
| `PixelPicker.cs` | 145 | RLE transparency map per texture id; backs all `PixelCheck` hit-testing. |
| `ScissorStack.cs` | 113 | Static `Stack<Rectangle>` of intersected scissor rects pushed at the device. |
| `ShaderHueTranslator.cs` | 110 | Encodes (hue, mode, alpha) into the `Vector3` the shader reads. Mode constants live here. |
| `TextureAtlas.cs` | 98 | `stb_rect_pack`-backed atlas; adds sprites into 2048²/4096² `Texture2D` pages. |
| `Fonts.cs` | 87 | Embeds 8 XNB fonts, exposes them as statics after `Initialize(device)`. |
| `SolidColorTextureCache.cs` | 71 | `Dictionary<Color,Texture2D>` of 1×1 textures; never evicted. |
| `Gumps/Gump.cs` | 58 | Gump art cache (atlas + picker), with PNG-override support. |
| `Effects/XBREffect.cs` | 48 | xBR upscaler effect wrapper. |
| `MultiMaps/MultiMap.cs` | 46 | Builds a standalone (non-atlas) `Texture2D` for the world map facet. |
| `Texmaps/Texmap.cs` | 44 | Land texmap cache. |
| `Lights/Light.cs` | 42 | Light sprite cache (no picker). |
| `Effects/BasicUOEffect.cs` | 24 | `HueTechnique` parameter handles for `IsometricWorld.fxc`. |
| `Animations/AnimationGroup.cs` | 21 | `AnimationGroup` (8 directions) and `AnimationGroupUop` (offset/lengths). |
| `SpriteInfo.cs` | 14 | `{ Texture2D Texture; Rectangle UV; Point Center; }` + static `Empty`. |
| `Animations/AnimationDirection.cs` | 11 | `{ Address, Size, FrameCount, SpriteInfo[] SpriteInfos, IsVerdata }`. |
| `Batching/*.cs` (26 files) | 1263 | Plain `[StructLayout]` POD command structs (`ViewportCommand`, `ScissorCommand`, `Create*`/`Set*`State/Buffer/Texture, `IndexedPrimitiveDataCommand`, `DestroyResourceCommand`) plus the 216-byte explicit-layout union `BatchCommand`. **Dead in this fork** — nothing in `ClassicUO.Client` references `Renderer.Batching`. |

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `UltimaBatcher2D` | `Batcher2D.cs:46` | Sprite batcher. `Begin/End`, ~10 `Draw` overloads, UO-specific `DrawStretchedLand`/`DrawShadow`/`DrawCharacterSitted`/`DrawTiled`/`DrawRectangle`/`DrawLine`. Implements `IFontStashRenderer`. |
| `PositionNormalTextureColor4` | `Batcher2D.cs:1552` | 4-vertex quad, 192 bytes (`SIZE_IN_BYTES = 4*12*4`); position/normal(=RGB)/texcoord/hue per corner. |
| `Resources` (partial) | `Batcher2D.cs:1589` | `GetUOShader()` / `GetXBRShader()` — `shaders/IsometricWorld.fxc`, `shaders/xBR.fxc` compiled in via EmbedResourceCSharp. |
| `ShaderHueTranslator` | `ShaderHueTranslator.cs:38` | `SHADER_NONE..SHADER_EFFECT_HUED` (0..10), `GUMP_OFFSET = 20`, `SPECTRAL_COLOR_FLAG = 0x4000`. `GetHueVector` at `:66`. |
| `Camera` | `Camera.cs:40` | `MAX_PEEK_DISTANCE=250`, `MIN_PEEK_SPEED=0.01`, `PEEK_TIME_FACTOR=5`. `Update(force, timeDelta, mousePos)` at `:105`. |
| `TextureAtlas` | `TextureAtlas.cs:9` | `AddSprite(pixels,w,h,out uv)` at `:30`; `CreateNewTexture2D` at `:61`. |
| `PixelPicker` | `PixelPicker.cs:6` | `Set(ulong id,w,h,pixels)` `:87` writes a varint RLE run-list; `Get(id,x,y,extraRange,scale)` `:13` walks it. |
| `Animations` | `Animations/Animation.cs:8` | `GetAnimationFrames` `:111` is the hot path; `PixelCheck` `:35`; `GetAnimationDimensions` `:70`. |
| `Animations.IndexAnimation` | `Animations/Animation.cs:381` | Per-body-graphic record: FileIndex, Hue, Flags, `AnimationGroup[] Groups`, `AnimationGroupUop[] UopGroups`, MountedHeightOffset, Type. |
| `Art` | `Arts/Art.cs:11` | `GetLand(idx)` = `Get(idx & ~0x4000)`; `GetArt(idx)` = `Get(idx + 0x4000)`; `CreateCursorSurfacePtr` `:106`; `GetRealArtBounds` `:195`. |
| `Gump` (renderer) | `Gumps/Gump.cs:6` | Gump texture cache; `PixelCheck(idx,x,y,scale)` `:56`. |
| `Renderer.Sounds.Sound` | `Sounds/Sound.cs:13` | 0xFFFF-slot `_sounds`/`_musics` arrays; `SetMusicEra` `:39`, `GetMusic` `:106`, `ResolveMusicPath` `:139`. |
| `IO.Audio.Sound` | `Audio/Sound.cs:39` | `Play(curTime, volume, volumeFactor, spamCheck)` `:151`; `Delay=250` default; `Frequency=22050`, Mono. |
| `UOMusic` | `Audio/UOMusic.cs:40` | `NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK = 0x8000`; `Update()` `:81` pumps the decoder; static `Looped`/`Ended` hooks `:78-79`. |
| `DataReader` | `DataReader.cs:44` | `SetData(byte*/byte[]/IntPtr, length)`; `EnsureSize` is `[Conditional("DEBUG")]` `:254`. |
| `UOFile` | `UOFile.cs:42` | MMF open + `AcquirePointer` `:95`. `Dispose` `:116` releases pointer/accessor/file. |
| `UOFileUop` | `UOFileUop.cs:44` | `UOP_MAGIC_NUMBER = 0x50594D`; `CompressionType { None, Zlib, ZlibBwt=3 }` `:38`. |
| `UOFileIndex` | `UOFileIndex.cs:37` | Mutable struct handed out by `ref` from `UOFileLoader.GetValidRefEntry`. |
| `StackDataReader` | `StackDataReader.cs:10` | `ref struct`; every read is bounds-checked and returns 0 past the end (no throw). |
| `StackDataWriter` | `StackDataWriter.cs:11` | `ref struct`; grows via `ArrayPool.Rent`, must be `Dispose()`d to return the buffer. |
| `DefReader` | `DefReader.cs:42` | `Next()`/`ReadInt()`/`ReadGroup()` cursor over pre-parsed lines. |
| `BatchCommand` | `Batching/BatchCommand.cs:38` | `[StructLayout(Explicit, Size=216)]` union of every command struct; all at `FieldOffset(0)`. |

## State

Mutable/static/global state owned by this partition:

- `UltimaBatcher2D._vertexInfo` / `_textureInfo` — `Batcher2D.cs:87-88`. Parallel arrays, initial
  `MAX_SPRITES = 0x800` (`:51`), grown by `+0x800` in `EnsureSize` `:1263-1270`, **never shrunk**.
- `UltimaBatcher2D._numSprites` `:60`, `_currentBufferPosition` `:55`, `_started` `:82` — per-frame
  cursor state; `_numSprites` reset to 0 only at the end of `Flush` `:1369`.
- `UltimaBatcher2D._blendState/_sampler/_stencil/_customEffect/_transformMatrix` `:54,81,83,57,84` —
  sticky device state, mutated by `SetBlendState`/`SetStencil`/`SetSampler`/`Begin`.
- `UltimaBatcher2D.TextureSwitches`, `FlushesDone` `:136` — public counters, zeroed in `Begin` `:1151`.
- `_cornerOffsetX/_cornerOffsetY` static float[4] `:48-49`.
- `ScissorStack._scissors` — **static** `Stack<Rectangle>`, `ScissorStack.cs:42`. Process-global; not
  per-batcher, not per-device.
- `SolidColorTextureCache._textures` + `_device` — **static** `SolidColorTextureCache.cs:41,43`.
  Unbounded `Dictionary<Color,Texture2D>`, no eviction, no dispose.
- `Fonts.Regular/Bold/Map1..Map6` — **static** `SpriteFont`s, `Fonts.cs:79-86`, set once in
  `Initialize` `:67`.
- `Animations._dataIndex` — `Animations/Animation.cs:14`, `IndexAnimation[2048]` initial, grown
  geometrically at `:137` and `:199`. Frame textures/`SpriteInfo[]` hang off it forever.
- `Animations._atlas` (4096×4096) `:18`, `Animations._picker` `:13`.
- `Art._spriteInfos` / `_realArtBounds` — `Arts/Art.cs:13,16`, sized to `ArtLoader.Entries.Length`
  at construction; `_atlas` 4096² `:20`; `_picker` `:15`.
- `Gump._spriteInfos` `Gumps/Gump.cs:9`, `Texmap._spriteInfos` `Texmaps/Texmap.cs:9`,
  `Light._spriteInfos` `Lights/Light.cs:10` — same pattern, atlases 4096²/2048²/2048².
- `Renderer.Sounds.Sound._sounds` / `_musics` — `Sounds/Sound.cs:17-18`, 0xFFFF entries each
  (~1 MB of references), never trimmed; `_musics` wholesale nulled by `ClearMusicCache` `:81`.
- `Renderer.Sounds.Sound._era/_eraDirectory/_eraFiles` `:24-26` — directory listing snapshotted at
  `SetMusicEra` time `:61`.
- `PixelPicker.m_IDs` (`Dictionary<ulong,int>`) + `m_Data` (`List<byte>`, initial 0x40000) —
  `PixelPicker.cs:10-11`. Append-only; entries are never removed, `m_Data` only grows.
- `TextureAtlas._textureList` + `_packer` — `TextureAtlas.cs:15,17`. Only the newest page has a
  live packer (`CreateNewTexture2D` `:67-68` disposes the previous one).
- `UOFilesOverrideMap.Instance` + `OverrideFile` — **static** `UOFilesOverrideMap.cs:10,12`.
- `UOFileIndex.Invalid` — **static mutable struct field** `UOFileIndex.cs:78`, returned by
  `ref` from `UOFileLoader.GetValidRefEntry` `:66`.
- `UOMusic.Looped` / `UOMusic.Ended` — **static** `Action<UOMusic>` `Audio/UOMusic.cs:78-79`
  (client installs these in `AudioManager` ctor).
- `IO.Audio.Sound._lastPlayedTime` `:41`, `DurationTime` `:70` — per-sound time gate in ms.
- `DataReader._data` / `_handle` `DataReader.cs:46-47` — raw pointer + optional pinned GCHandle.
- `UOFile._accessor` / `_file` `UOFile.cs:56-57` — MMF view whose pointer everything else aliases.

## Timing

- **Per frame, on the FNA game thread.** `UltimaBatcher2D.Begin` → N `Draw*` → `End`
  (`Batcher2D.cs:1137-1164`). `GameController.cs:128` builds the single batcher.
  `Flush` (`:1328`) is also called mid-frame by `ClipBegin`/`ClipEnd`/`EnableScissorTest`/
  `SetBlendState`/`SetStencil`/`SetSampler`, so a frame is many draw calls, not one.
- **Per frame:** `Flush` iterates `_textureInfo` and issues one `DrawIndexedPrimitives` per
  contiguous texture run (`:1347-1360`), in batches of `MAX_SPRITES = 0x800`; the vertex buffer is
  filled with `NoOverwrite` until it wraps, then `Discard` (`UpdateVertexBuffer` `:1483-1513`).
- **Per frame:** `Scene.Update` calls `Camera.Update(true, Time.Delta, Mouse.Position)`
  (`src/ClassicUO.Client/Game/Scenes/Scene.cs:64`) — `force:true` every frame, which is what keeps
  the peek lerp advancing (`CalculatePeek` `Camera.cs:194`).
- **On demand, first use, then cached forever:** every `Get*` in Art/Gump/Texmap/Light/Animations.
  The first request for a graphic does file decode + `PixelPicker.Set` (a full W×H scan) +
  `Texture2D.SetDataPointerEXT` — all synchronously on the frame thread. This is the frame-hitch
  path when new content walks into view.
- **On demand:** `MultiMap.GetMap` `MultiMaps/MultiMap.cs:20` allocates a **new `Texture2D` every
  call** (no cache, no dispose here).
- **Audio pump:** `AudioManager.Update` → `_currentMusic[i]?.Update()`
  (`src/ClassicUO.Client/Game/Managers/AudioManager.cs:1011`) → `UOMusic.OnBufferNeeded`
  (`Audio/UOMusic.cs:128`), which tops the queue up to 3 buffers of 0x8000 bytes (~0.9 s each).
  The MP3 decode (`m_Stream.Read`) happens **on the calling thread** — the frame thread.
  `OnBufferNeeded` is also wired to the FNA `BufferNeeded` event (`Audio/Sound.cs:176`), so it can
  additionally run on an audio callback thread.
- **Sound spam gate:** `Sound.Play` refuses if `_lastPlayedTime > curTime`
  (`Audio/Sound.cs:153`); `_lastPlayedTime = curTime + Delay` (`:174`). `Delay` is 250 ms default,
  0 for music, `(bufLen-32)/88.2` ms for `UOSound`.
- **On load / startup:** `Fonts.Initialize` and `SolidColorTextureCache.Initialize`
  (`GameController.cs:207-208`), then the 7 asset caches (`GameController.cs:212-218`).
  `UOFile.Load` memory-maps; `UOFileUop.Load` walks the whole block chain; `DefReader` ctor parses
  the entire file eagerly (`DefReader.cs:70`).
- **On era change (rare, user action):** `SetMusicEra` does `Directory.GetFiles` synchronously
  (`Sounds/Sound.cs:61`) and nulls all 65535 music slots (`:81-87`).
- **Per packet:** `StackDataReader` / `StackDataWriter` are constructed and discarded per packet
  by the network layer; nothing here is per-frame.

## Inbound

- `GameController.cs:128` constructs the one `UltimaBatcher2D`; `GameController.cs:207-208`
  initializes `Fonts` and `SolidColorTextureCache`; `GameController.cs:212-218` constructs
  `Animations`, `Art`, `Gump`, `Texmap`, `Light`, `MultiMap`, `Renderer.Sounds.Sound` and exposes
  them as `Client.Game.Animations` / `.Arts` / `.Gumps` / `.Texmaps` / `.Lights` / `.MultiMaps` /
  `.Sounds`.
- Every UI control and gump draws through `UltimaBatcher2D` (heaviest callers by reference count:
  `Game/UI/Gumps/WorldMapGump.cs`, `Game/UI/Gumps/BaseOptionsGump.cs`,
  `Game/UI/Controls/ColorSelectorControl.cs`, `Game/UI/Controls/ModernScrollBar.cs`,
  `Game/GameObjects/Views/View.cs`, `Game/Scenes/GameScene.cs`, `Game/GameCursor.cs`,
  `Game/Weather.cs`, `Game/Managers/HealthLinesManager.cs`).
- `Game/Scenes/Scene.cs:64` drives `Camera.Update`; `Game/Scenes/GameSceneInputHandler.cs:1288/1292`
  calls `ZoomIn`/`ZoomOut`; `GameScene.cs:146/442` reads/writes `Camera.Zoom` from the profile.
- `Game/Managers/AudioManager.cs` is the only consumer of the audio types: `GetSound` `:139/:187`,
  `GetMusic` `:341`, `_currentMusic[i].Update()` `:1011`, and it installs
  `UOMusic.Looped`/`UOMusic.Ended` at `:76/:78` (routed to `Game/Managers/MusicDiagnostics.cs`).
- `ClassicUO.Assets` loaders derive from `UOFileLoader` and consume `UOFile*`, `DataReader`,
  `DefReader`, `UOFilesOverrideMap`.
- `ClassicUO.Client/Network` builds packets with `StackDataWriter` and parses them with
  `StackDataReader`.
- `ScissorStack` is `internal` — only `UltimaBatcher2D.ClipBegin/ClipEnd/EnableScissorTest` touch it.

## Outbound

- **FNA / `Microsoft.Xna.Framework`** everywhere: `GraphicsDevice`, `Texture2D`,
  `DynamicVertexBuffer.SetDataPointerEXT`, `Texture2D.SetDataPointerEXT`, `Effect`,
  `DynamicSoundEffectInstance`.
- **`ClassicUO.Assets`** (a *later* partition in dependency order, but referenced from the renderer
  caches): `ArtLoader`, `GumpsLoader`, `TexmapsLoader`, `LightsLoader`, `MultiMapLoader`,
  `AnimationsLoader`, `HuesLoader`, `SoundsLoader`, `ExternalImageLoader`, `UOFileManager`
  (`.Version`, `.BasePath`, `.GetUOFilePath`).
- **`ClassicUO.Utility`**: `MathHelper.MachineEpsilonFloat`/`AngleBetweenVectors`, `Easings`,
  `ValueStringBuilder`, `StringHelper.Cp1252ToString`/`IsSafeChar`/`StringToCp1252Bytes`, `Log`.
- **`SDL2`**: `Art.CreateCursorSurfacePtr` calls `SDL_CreateRGBSurfaceWithFormatFrom`
  (`Arts/Art.cs:125`).
- **`StbRectPackSharp`**: `Packer` in `TextureAtlas`.
- **`FontStashSharp.Interfaces.IFontStashRenderer`**: implemented by `UltimaBatcher2D`
  (`Batcher2D.cs:46`, `Draw` at `:155`).
- **`MP3Sharp`**: `MP3Stream` in `UOMusic`.
- **`EmbedResourceCSharp`**: compiles `shaders/*.fxc` and `fonts/*.xnb` into the assembly.
- **`System.Buffers.ArrayPool<byte>.Shared`** from `StackDataWriter.Rent/Return`.

## Hazards

- `src/ClassicUO.Renderer/Arts/Art.cs:55` — `return ref Get(0);` on a missing texture. If index 0
  itself has no pixels, `Get(0)` recurses into itself unconditionally (the `idx > 0` guard at `:48`
  does not stop `Get(0)` from re-entering after `Get(0)` fails to populate `_spriteInfos[0]`).
- `src/ClassicUO.Renderer/Arts/Art.cs:76` — the picker key is `idx - 0x4000` (the raw item id) while
  `Gump`/`Texmap` key their pickers by raw `idx`. Each cache owns its own `PixelPicker`, so this is
  only safe because the pickers are not shared.
- `src/ClassicUO.Renderer/Arts/Art.cs:196` — `idx < 0` on a `uint` parameter is always false; the
  only effective bound is `idx >= _realArtBounds.Length`.
- `src/ClassicUO.Renderer/Arts/Art.cs:98` — `_realArtBounds[idx]` is written only when `idx > 0x4000`
  at entry; art with no non-zero pixel leaves `minX=Width, maxX=0`, i.e. a negative-width rectangle.
- `src/ClassicUO.Renderer/TextureAtlas.cs:45` — `while (!_packer.PackRect(w,h,out pr))` allocates a
  new full-size `Texture2D` each iteration; a sprite larger than the atlas (`_width`/`_height`)
  never packs and the loop allocates textures until the GPU/heap gives out.
- `src/ClassicUO.Renderer/TextureAtlas.cs:67` — `CreateNewTexture2D` disposes the old `Packer` and
  makes a fresh one, so all remaining free space in earlier pages is permanently abandoned.
- `src/ClassicUO.Renderer/TextureAtlas.cs:37-49` — `index` is recomputed from `_textureList.Count-1`
  but the `pr` rectangle comes from the *current* packer; the two are only in sync because the
  packer is replaced in lockstep with the list.
- `src/ClassicUO.Renderer/PixelPicker.cs:87-112` — `Set` appends to `m_Data` forever; nothing ever
  removes an id or compacts the list. `m_Data` is a `List<byte>` that grows for the life of the
  process (one entry per distinct art/gump/anim-frame ever displayed).
- `src/ClassicUO.Renderer/PixelPicker.cs:99` — `pixels[i]` is indexed to `width*height` with no
  check that the span is that long.
- `src/ClassicUO.Renderer/Batcher2D.cs:1263-1270` — `EnsureSize` grows `_vertexInfo`/`_textureInfo`
  instead of flushing (the flush is commented out at `:1258-1261`/`:1265`). A single frame that
  submits many sprites permanently enlarges both arrays.
- `src/ClassicUO.Renderer/Batcher2D.cs:1362-1367` — the multi-batch loop mutates `_numSprites`
  (`_numSprites -= MAX_SPRITES`) while walking, and only resets it to 0 at `:1369`; an exception
  from `InternalDraw` leaves `_numSprites` non-zero and the arrays holding stale `Texture2D`
  references into the next frame.
- `src/ClassicUO.Renderer/Batcher2D.cs:1345` — `_textureInfo[arrayOffset]` is read before any null
  check; only `PushSprite` (`:1274`) validates `texture != null && !texture.IsDisposed`. The
  `AddSprite` path (`:1132`) and the `IFontStashRenderer.Draw` path (`:243`) store the texture with
  no validation, so a disposed atlas page reaches `GraphicsDevice.Textures[0]`.
- `src/ClassicUO.Renderer/Batcher2D.cs:1276-1284` — `PushSprite` returning false leaves the vertex
  already written at `_vertexInfo[_numSprites]` by the caller but does not advance `_numSprites`,
  so the next sprite overwrites it. Callers (`DrawStretchedLand` `:425`, `DrawShadow` `:496`,
  `DrawCharacterSitted` `:582/646/710`) ignore the return value.
- `src/ClassicUO.Renderer/Batcher2D.cs:1532-1548` — `EnsureStarted`/`EnsureNotStarted` are
  `[Conditional("DEBUG")]`; in Release a `Draw` outside `Begin/End` is silently accepted.
- `src/ClassicUO.Renderer/Batcher2D.cs:1459` — `GraphicsDevice.RasterizerState.ScissorTestEnable = enable`
  mutates the shared `_rasterizerState` object in place rather than swapping states.
- `src/ClassicUO.Renderer/ScissorStack.cs:42` — the scissor stack is `static`. An unbalanced
  `ClipBegin`/`ClipEnd` (e.g. `ClipBegin` returning false and the caller still calling `ClipEnd`)
  pops another control's rectangle, or throws on an empty stack at `:81`.
- `src/ClassicUO.Renderer/ScissorStack.cs:44` — `HasScissors` is `Count - 1 > 0`, i.e. true only
  from the *second* pushed rect; with exactly one clip active `EnableScissorTest` can be told to
  disable scissoring.
- `src/ClassicUO.Renderer/SolidColorTextureCache.cs:41` — unbounded static texture dictionary, one
  1×1 `Texture2D` per distinct `Color` ever requested, never disposed and not reset across
  device/graphics resets.
- `src/ClassicUO.Renderer/Animations/Animation.cs:293` — `animDir.SpriteInfos[frame.Num]` indexes by
  the frame's own `Num` field into an array sized `frames.Length`; a `Num >= frames.Length` from the
  file is an out-of-range write.
- `src/ClassicUO.Renderer/Animations/Animation.cs:320` — returns `animDir.SpriteInfos.AsSpan(0, animDir.FrameCount)`,
  a span over a live cached array; a later `GetAnimationFrames` for the same (id,action,dir) can
  replace `SpriteInfos` (`:288`) while a caller still holds the old span.
- `src/ClassicUO.Renderer/Animations/Animation.cs:140-205` — `ref var index = ref _dataIndex[id]` is
  taken, then `Array.Resize(ref _dataIndex, ...)` at `:199` reallocates the array; the ref is
  re-taken at `:202` only inside the `replaced` branch. The `do/while (index == null)` loop can spin
  if `ReplaceBody` keeps mapping to a slot that stays null.
- `src/ClassicUO.Renderer/Animations/Animation.cs:130` — `if (id >= ushort.MaxValue)` — `id` is a
  `ushort`, so this only rejects exactly 65535.
- `src/ClassicUO.Renderer/Animations/Animation.cs:243` — the "already loaded" test is
  `FrameCount <= 0 || SpriteInfos == null`; a direction that legitimately decoded to zero frames is
  re-decoded on every call (`:282-284` sets `FrameCount = 0`).
- `src/ClassicUO.Renderer/Camera.cs:155-182` — `UpdateMatrices` returns immediately unless
  `_updateMatrixes` is set, so `CalculatePeek`'s lerp only advances on frames where
  `Update(force:true,…)` was called. `_timeDelta`/`_mousePos` are stored at `:112-113` and reused by
  whatever calls `ScreenToWorld`/`WorldToScreen` later in the frame.
- `src/ClassicUO.Renderer/MultiMaps/MultiMap.cs:29` — a new `Texture2D` per `GetMap` call with no
  cache and no disposal path in this partition.
- `src/ClassicUO.Renderer/SpriteFont.cs:244` — `texture.SetDataPointerEXT(0, null, ptr, width * height * sizeof(byte))`
  uploads `w*h*1` bytes into a `SurfaceFormat.Color` (4 bytes/px) texture; the decompressed buffer
  is `w*h*4` (`DecompressDxt3` `:332`).
- `src/ClassicUO.Renderer/SpriteFont.cs:289` — `kernings` is preallocated with `croppingCount`, not
  `kerningCount`.
- `src/ClassicUO.Renderer/SpriteFont.cs:121` / `Batcher2D.cs:289` — glyph lookup is
  `List<char>.IndexOf(c)`, a linear scan per character, per draw, per frame.
- `src/ClassicUO.IO/DataReader.cs:254-266` — `EnsureSize` is `[Conditional("DEBUG")]`. In Release
  every `ReadShort`/`ReadInt`/`ReadLong` dereferences `_data + Position` with no bounds check at all.
- `src/ClassicUO.IO/DataReader.cs:69-76` — `SetData(byte*, long)` calls `ReleaseData()` (freeing the
  pinned handle) but leaves `_data` pointing at the new pointer; any previously handed-out
  `StartAddress` (e.g. stored in a `UOFileIndex.Address`, `UOFileMul.cs:62`) is now stale.
- `src/ClassicUO.IO/UOFile.cs:116-124` — `Dispose` dereferences `_accessor` unconditionally; if
  `Load` bailed early (file missing `:71`, or size 0 `:108`) `_accessor` is null and `Dispose`
  throws.
- `src/ClassicUO.IO/UOFile.cs:95-96` — the raw MMF pointer is handed to `DataReader` and then copied
  into every `UOFileIndex.Address`; after `Dispose` releases the pointer, all those indices are
  dangling but still look valid to `GetValidRefEntry`.
- `src/ClassicUO.IO/UOFileLoader.cs:66` — returns `ref UOFileIndex.Invalid`, a **static mutable
  field**. A caller that writes through the returned `ref` (the entries are handed out by `ref` and
  `UOFileIndex` has public mutable fields) corrupts the shared sentinel for everyone.
- `src/ClassicUO.IO/StackDataReader.cs:39` — `public byte this[int index] => _data[0];` — the
  indexer ignores `index` and always returns byte 0.
- `src/ClassicUO.IO/StackDataReader.cs:63` — `Skip` and `Seek` (`:55`) set `Position` with no bounds
  check; the subsequent `_data.Slice(Position)` in the read methods is what throws.
- `src/ClassicUO.IO/StackDataReader.cs:398` — `Read(Span,offset,count)` slices without validating
  `count` against `Remaining` and does not advance `Position`.
- `src/ClassicUO.IO/StackDataWriter.cs:378-385` — `WriteString` encodes into `_allocatedBuffer`, but
  `_buffer` is what `EnsureSize`/`Position` track; `StackDataWriter(Span<byte>)` (`:34`) leaves
  `_allocatedBuffer` null until the first `Rent`, so the two can disagree.
- `src/ClassicUO.IO/StackDataWriter.cs:431-434` — `Dispose` returns the array to `ArrayPool`. A
  writer that escapes without `Dispose` leaks the rental; one disposed twice, or whose `Buffer` is
  read after `Dispose`, reads a recycled array.
- `src/ClassicUO.IO/StackDataWriter.cs:400` — `Rent(Math.Max(BytesWritten + size, _buffer.Length * 2))`
  starts from `_buffer.Length == 0` for a default-constructed writer, so the first grow is exactly
  `size`.
- `src/ClassicUO.IO/UOFileUop.cs:119/139` — `_hashes.Add` (not `[]=`) throws on a duplicate hash in
  a malformed/patched `.uop`.
- `src/ClassicUO.IO/Audio/UOMusic.cs:139-149` — `OnBufferNeeded` loops `while (PendingBufferCount < 3)`
  submitting decoded MP3 chunks; the decode runs on whichever thread raised the event — the frame
  thread via `Update()` (`AudioManager.cs:1011`) or FNA's audio callback via the `BufferNeeded`
  subscription (`Audio/Sound.cs:176`). `m_Stream` is touched from both with no lock.
- `src/ClassicUO.IO/Audio/UOMusic.cs:115` — `GetBuffer` returns the same `m_WaveBuffer` instance
  every call, and `OnBufferNeeded` submits it up to 3 times in a row before the previous submission
  has been consumed.
- `src/ClassicUO.IO/Audio/UOMusic.cs:100` — on loop, the second `Read` fills only the tail of the
  buffer and its return value is discarded; a short read leaves stale PCM in the gap.
- `src/ClassicUO.IO/Audio/Sound.cs:107` — `IsPlaying(curTime)` compares `DurationTime > curTime`
  where `DurationTime` is `curTime + sampleDuration` from the last `Play` (`:181`) — it only knows
  about the most recently submitted buffer, not the stream. (`UOMusic.IsStreaming` `:74` exists
  because of this; a Holiday addition.)
- `src/ClassicUO.IO/Audio/Sound.cs:118-127` — `Dispose` nulls `SoundInstance` but the object stays
  in `Renderer.Sounds.Sound._sounds[index]`; the next `GetSound` returns the disposed wrapper
  because the slot is non-null (`Sounds/Sound.cs:95`).
- `src/ClassicUO.Renderer/Sounds/Sound.cs:81-87` — `ClearMusicCache` nulls all slots while
  `AudioManager` may still hold a reference to the currently playing `UOMusic` in `_currentMusic`;
  the comment at `:78-80` says callers are "expected" to have stopped playback.
- `src/ClassicUO.Renderer/Sounds/Sound.cs:170-181` — `FindInEra` is a linear scan of the era
  directory listing per lookup; the listing is a snapshot from `SetMusicEra` and goes stale if files
  change on disk.
- `src/ClassicUO.IO/DefReader.cs:77` — `PartsCount => _parts[Line].Length` with `Line` initialized
  to `-1` (`:67`); calling it before `Next()` throws.
- `src/ClassicUO.IO/UOFilesOverrideMap.cs:47` — `Add(file, filePath)` throws on a duplicate key; the
  `try` at `:34` swallows it and skips the rest of that line only.
- `src/ClassicUO.Renderer/Batching/*` — 26 files, 1263 lines of command structs and a 216-byte
  `BatchCommand` union that nothing in `ClassicUO.Client` references. Dead weight that still
  compiles and still constrains struct layout.

## Fork deltas

Clearly **Holiday-Edition / TazUO additions** rather than stock ClassicUO:

- `src/ClassicUO.Renderer/Sounds/Sound.cs` — the whole **music-era** mechanism (`MusicEra`,
  `SetMusicEra` `:39`, `ClearMusicCache` `:81`, `ResolveMusicPath` `:139`, `StockMusicPath` `:158`,
  `FindInEra` `:170`) and the `_useDigitalMusicFolder` probe `:30`. Comments explicitly describe
  pushing the era in "from the client" because the assembly cannot see the profile. The MIDI
  fallback at `:116-123` names an unimplemented "Part B" that would add a `UOMidi`.
- `src/ClassicUO.IO/Audio/UOMusic.cs:59-79` — `Path`, `IsLooping`, `IsStreaming`, and the static
  `Looped`/`Ended` diagnostic hooks, all with fork-voice comments ("This assembly cannot see the
  client's settings or world, so the diagnostic subscribes from there"). Consumed by
  `Game/Managers/MusicDiagnostics.cs`, itself a fork file.
- `src/ClassicUO.Renderer/Animations/Animation.cs:45-52` — added group/direction validation in
  `PixelCheck` with a comment explaining the previous guard "was really just an accident of call
  order".
- `src/ClassicUO.Renderer/Animations/Animation.cs:94-96` — added `frameIndex < frames.Length` bound
  in `GetAnimationDimensions` with an explanatory comment.
- `src/ClassicUO.Renderer/Animations/Animation.cs:135-137` and `:197-199` — geometric
  `Array.Resize(ref _dataIndex, Math.Max(id + 1, _dataIndex.Length * 2))` replacing an exact-size
  resize; the second copy is mis-indented, consistent with a hand-applied patch.
- `src/ClassicUO.Renderer/Arts/Art.cs:40-70` and `src/ClassicUO.Renderer/Gumps/Gump.cs:27-49` —
  `ExternalImageLoader` PNG-override path (`LoadArtTexture`/`LoadGumpTexture` +
  `ClearArtPixelCache`/`ClearGumpPixelCache`), a TazUO feature absent from stock ClassicUO.
- `src/ClassicUO.Renderer/Gumps/Gump.cs:56` — `PixelCheck(..., double scale)` and the `scale`
  parameter threaded into `PixelPicker.Get` (`PixelPicker.cs:13,21-25`), for TazUO's scaled gumps.
- `src/ClassicUO.Renderer/ShaderHueTranslator.cs:50` — `SHADER_EFFECT_HUED = 10` plus the `effect`
  flag and its `TODO` at `:90`.
- `src/ClassicUO.IO/StackDataReader.cs:274-351` — `ReadRawString` using
  `StringHelper.Cp1252ToString` in place of the generic `Encoding` path (the original is left
  commented at `:356/:363`), and the `IsSafeChar` filtering loop.
- `.NET Framework 4.7.2 accommodations:** the `#if !NETFRAMEWORK && !NETSTANDARD2_0` guards that
  strip `MethodImplOptions.AggressiveOptimization` (`StackDataReader.cs:13-16`,
  `StackDataWriter.cs:13-17`) — these exist because this branch targets `net472`.
