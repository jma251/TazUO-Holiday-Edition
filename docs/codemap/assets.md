# assets

`src/ClassicUO.Assets` — the UO file-format layer. Memory-maps the shard's `.mul`/`.uop`/`.idx`/`.def`
files, decodes them into CPU-side pixel/metadata buffers, and hands those to `ClassicUO.Renderer`
(which turns them into `Texture2D` atlases) and to `ClassicUO.Client` game systems. 24 files,
11,880 lines, all read in full for this map.

Compiled as its own assembly. Almost every loader is an eagerly-created **static singleton**
(`X.Instance`), and `UOFileManager` is an all-static façade. There is no per-session teardown:
loaders live for process lifetime.

## Files

| Path (relative to `src/ClassicUO.Assets/`) | Lines | Purpose |
| --- | --- | --- |
| `AnimationsLoader.cs` | 1847 | anim*.mul/.idx + AnimationFrame*.uop; body/corpse/equip/bodyconv .def tables; decodes RLE animation frames into `FrameInfo[]`. |
| `FontsLoader.cs` | 3836 | fonts.mul (ASCII) + unifont*.mul (unicode); word-wrap engine, HTML subset parser, rasterizes text to `uint[]`. |
| `MapLoader.cs` | 726 | map*.mul / map*LegacyMUL.uop, statics/staidx, mapdif/stadif patch files; builds `IndexMap[][] BlockData`. |
| `TileDataLoader.cs` | 632 | tiledata.mul → `LandTiles[] LandData`, `StaticTiles[] StaticData`, `TileFlag` enum. |
| `ProfessionLoader.cs` | 433 | Prof.txt char-creation profession tree. |
| `ExternalImageLoader.cs` | 415 | **TazUO addition.** PNG/BMP overrides in `ExternalImages/{gumps,art}` + embedded `gumpartassets.*.png`. |
| `SoundsLoader.cs` | 412 | sound.mul / soundLegacyMUL.uop, Sound.def, Music `Config.txt` parsing (incl. era-folder override). |
| `UOFileManager.cs` | 403 | Static façade: path resolution, `Load()` orchestration of every loader, verdata patching, art.def. |
| `ArtLoader.cs` | 389 | art.mul / artLegacyMUL.uop; land diamonds (44×44) and RLE statics → `ArtInfo`. |
| `ClilocLoader.cs` | 363 | Cliloc.<lang> string table + `Translate()` argument substitution. |
| `TileArt.cs` | 346 | **Newer upstream.** tileart.uop v4 parser (`TileArtInfo`, `TAEFlag`, appearance/body maps). |
| `MultiMapLoader.cs` | 316 | Multimap.rle + facet0*.mul world-map images. |
| `GumpsLoader.cs` | 249 | gumpart.mul / gumpartLegacyMUL.uop + gump.def → `GumpInfo`. |
| `HuesLoader.cs` | 237 | hues.mul + radarcol.mul; hue application helpers. |
| `SpeechesLoader.cs` | 201 | speech.mul keyword matching for the speech-keyword packet. |
| `SkillsLoader.cs` | 189 | skills.mul/idx → `SkillEntry` list + alphabetical sort. |
| `TrueTypeLoader.cs` | 173 | FontStashSharp TTF loading (`Fonts/` folder + embedded); font-size clamp. |
| `TexmapsLoader.cs` | 144 | texmaps.mul + TexTerr.def → land textures. |
| `LightsLoader.cs` | 118 | light.mul/lightidx.mul → `LightInfo`. |
| `MultiLoader.cs` | 112 | multi.mul/idx or MultiCollection.uop; `MultiBlock`/`MultiBlockNew`. |
| `AnimDataLoader.cs` | 100 | animdata.mul; `CalculateCurrentGraphic` for animated statics. |
| `Verdata.cs` | 95 | Static ctor mapping verdata.mul into `UOFileIndex5D[] Patches`. |
| `UOFileLoader.cs` | 86 | Abstract base: `Entries[]`, `GetValidRefEntry`, `Load()`, `Dispose`. |
| `SoundOverrideLoader.cs` | 58 | **TazUO addition.** `SoundOverrides/<id>.mp3` replacement sounds, loaded on a background task. |

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `UOFileLoader` (abstract) | UOFileLoader.cs:40 | Base of every loader; owns `public UOFileIndex[] Entries` (:62), `GetValidRefEntry(int)` (:70) returning `ref UOFileIndex.Invalid` on bad index. |
| `UOFileManager` (static) | UOFileManager.cs:46 | `GetUOFilePath` (:50), `Load(version, basePath, useVerdata, lang)` (:91), `MapLoaderReLoad` (:324), `Read_Art_def` (:330). |
| `Verdata` (static) | Verdata.cs:38 | `Patches`, `File` — populated in a static constructor (:40). |
| `AnimationsLoader` | AnimationsLoader.cs:46 | `GetIndices` (:291), `ReadUOPAnimationFrames` (:1287), `ReadMULAnimationFrames` (:1442), `ReadSpriteData` (:1499), `ProcessBodyConvDef` (:532), `ReplaceBody`/`ReplaceCorpse`/`ReplaceUopGroup` (:245/:258/:271). |
| `AnimationsLoader.FrameInfo` | AnimationsLoader.cs:1572 | `Num/CenterX/CenterY/Width/Height/uint[] Pixels` — the pixel buffer is reused across calls. |
| `AnimationsLoader.AnimIdxBlock` | AnimationsLoader.cs:1618 | Position/Size/Unknown triple read straight from anim*.idx or the UOP hash table. |
| `UopInfo` | AnimationsLoader.cs:1820 | `fixed int _replacedAnim[80]`; `ReplacedAnimations` returns a `Span<int>` over the fixed buffer. |
| `MobTypeInfo`/`BodyInfo`/`BodyConvInfo` | AnimationsLoader.cs:1799/1805/1811 | mobtypes.txt / Body.def / Bodyconv.def rows. |
| `FontsLoader` | FontsLoader.cs:47 | `GenerateASCII` (:380), `GeneratePixelsASCII` (:534), `GetInfoASCII` (:754), `GenerateUnicode` (:1002), `GetInfoUnicode` (:1291), `GeneratePixelsUnicode` (:1578), `GetInfoHTML` (:2218), `GetHTMLData` (:2462), `ParseHTMLTag` (:2715), caret math (:3482/:3601). |
| `FontsLoader.FontInfo` | FontsLoader.cs:368 | `uint[] Data`, width/height, `LineCount`, `FastList<WebLinkRect> Links`. |
| `MultilinesFontInfo` | FontsLoader.cs:3764 | Linked-list line node with `FastList<MultilinesFontData> Data`. |
| `FontCharacterData` | FontsLoader.cs:3750 | `ushort* Data` pointing **into the mapped fonts.mul**, not a copy. |
| `MapLoader` | MapLoader.cs:44 | `Load` (:136), `LoadMap(int, bool useXFiles)` (:280), `ApplyPatches` (:450), `PatchMapBlock`/`PatchStaticBlock` (:409/:426), `GetIndex(map,x,y)` (:639). `Instance` setter disposes the old loader (:60). |
| `IndexMap` | MapLoader.cs:717 | Per-block map/static addresses + Original* copies used to undo diff patches. |
| `TileDataLoader` | TileDataLoader.cs:42 | `ref LandTiles[] LandData`, `ref StaticTiles[] StaticData`. |
| `LandTiles` / `StaticTiles` / `TileFlag` | TileDataLoader.cs:304 / :330 / :484 | Tile metadata; `StaticTiles.SetImpassable` (:390) mutates flags at runtime. |
| `HuesLoader` | HuesLoader.cs:45 | `HuesRange`, `RadarCol`, `GetHueColorRgba5551` (:126), `GetPartialHueColor` (:159), `CreateShaderColors` (:101). |
| `ArtLoader` | ArtLoader.cs:41 | `GetArt` (:370) → `GetRawImage` (:195) → `LoadData` (:95) / `ReadData` (:239). |
| `GumpsLoader` | GumpsLoader.cs:42 | `GetGump(uint)` (:147); `UseUOPGumps` flag (:54). |
| `TileArtLoader` / `TileArtInfo` | TileArt.cs:17 / :162 | Lazy per-graphic tileart.uop entries; `TryGetAppearance` (:338). Not a singleton — instance owned by `UOFileManager.TileArtLoader` (UOFileManager.cs:48). |
| `SoundsLoader` | SoundsLoader.cs:45 | `TryGetSound` (:270), `TryGetMusicData` (:390), `LoadMusicConfig(eraFolder)` (:155). |
| `SoundOverrideLoader` | SoundOverrideLoader.cs:8 | `TryGetSoundOverride` (:42), cache filled by a fire-and-forget `Task` in the ctor (:22). |
| `ExternalImageLoader` | ExternalImageLoader.cs:10 | `LoadGumpTexture`/`LoadArtTexture` (:66/:124), `LoadResourceAssets` (:313), `LoadBmp` (:183), cache clears (:399-413). |
| `ClilocLoader` | ClilocLoader.cs:44 | `GetString` overloads, `Translate` (:194) with `~1_val~` substitution. |
| `SkillsLoader` / `SkillEntry` | SkillsLoader.cs:42 / :110 | `Skills`, `SortedSkills`, `SkillEntry.HardCodedName` enum (:128). |
| `ProfessionLoader` / `ProfessionInfo` | ProfessionLoader.cs:66 / :41 | `Professions` dictionary tree parsed from Prof.txt. |
| `SpeechesLoader` / `SpeechEntry` | SpeechesLoader.cs:43 / :165 | `GetKeywords(text)` (:138) linear scan of every speech entry. |
| `TrueTypeLoader` | TrueTypeLoader.cs:42 | `GetFont(name, size)` (:148) with `ClampFontSize` (:55). |
| `MultiMapLoader` | MultiMapLoader.cs:45 | `LoadMap` (:92), `LoadFacet` (:240), `HasFacet` (:57). |
| `LightsLoader`/`TexmapsLoader`/`MultiLoader`/`AnimDataLoader` | LightsLoader.cs:40 / TexmapsLoader.cs:41 / MultiLoader.cs:40 / AnimDataLoader.cs:42 | Straight index-file loaders. |
| `ArtInfo`/`GumpInfo`/`TexmapInfo`/`LightInfo`/`MultiMapInfo` | ArtLoader.cs:383, GumpsLoader.cs:243, TexmapsLoader.cs:138, LightsLoader.cs:112, MultiMapLoader.cs:311 | `ref struct` carriers of `Span<uint> Pixels` + w/h. Must be consumed before the next loader call. |

## State

Static / process-lifetime:
- `UOFileManager.Version`, `.BasePath`, `.IsUOPInstallation` — UOFileManager.cs:87-89. Plain static
  fields, read by every loader during `Load()` and later at runtime (`GetGump`, `GetKeywords`, …).
- `UOFileManager.TileArtLoader` — UOFileManager.cs:48, replaced on each `Load()` (:102).
- `Verdata.Patches` / `Verdata.File` — Verdata.cs:92-94, initialised in a static ctor.
- `_instance` singletons: AnimationsLoader.cs:51, ArtLoader.cs:43, GumpsLoader.cs:44, HuesLoader.cs:47,
  FontsLoader.cs:65, MapLoader.cs:46, TileDataLoader.cs:44 (`public static`), SoundsLoader.cs:50,
  SkillsLoader.cs:44, SpeechesLoader.cs:45, ClilocLoader.cs:46, ProfessionLoader.cs:68,
  MultiLoader.cs:42, MultiMapLoader.cs:47, LightsLoader.cs:42, TexmapsLoader.cs:43,
  AnimDataLoader.cs:44, TrueTypeLoader.cs:81, ExternalImageLoader.cs:27 (`public static` field),
  SoundOverrideLoader.cs:10.
- `MapLoader.MAPS_COUNT` — MapLoader.cs:54, a mutable static (UltimaLive rewrites it via `MapsLayouts`).
- `MapLoader.MapsLayouts` — MapLoader.cs:70.
- `TileDataLoader._staticData` / `_landData` — TileDataLoader.cs:46-47, exposed as `ref` properties (:55-56).
- `SoundsLoader._musicData` — SoundsLoader.cs:48, static dictionary rebuilt by `LoadMusicConfig` (:157).
- `IndexMap.Invalid` — MapLoader.cs:725, a **non-readonly** static struct field.
- `SittingInfoData.Empty` — AnimationsLoader.cs:1614, non-readonly static.
- `FontInfo.Empty` — FontsLoader.cs:377, non-readonly static.
- `ProfessionInfo._VoidSkills` / `_VoidStats` — ProfessionLoader.cs:43/48, static mutable arrays handed
  out as the *default* `SkillDefVal`/`StatsVal` of every `ProfessionInfo` (:61-62).

`[ThreadStatic]` reusable scratch buffers:
- `ArtLoader._data` — ArtLoader.cs:47-48. `GetRawImage` returns a `Span<uint>` over it (:212).
- `AnimationsLoader._frames` — AnimationsLoader.cs:53-54. Returned by both frame readers (:1439, :1496).
- `AnimationsLoader._decompressedData` — AnimationsLoader.cs:56-57.

Per-instance mutable:
- `AnimationsLoader._files` / `_filesUop` (:59-60), `_equipConv`, `_mobTypes`, `_bodyInfos`,
  `_corpseInfos`, `_bodyConvInfos`, `_uopInfos` (:62-67), `GroupReplaces` (:76).
- `FontsLoader._fontData` (:106), `_unicodeFontAddress[20]` / `_unicodeFontSize[20]` (:107-108),
  `_webLinks` (:109), `_htmlStatus` (:104), `IsUsingHTML`/`RecalculateWidthByInfo`/`UnusePartialHue`
  (:119-123) — global rendering mode flags mutated by callers via `SetUseHTML` (:991).
- `MapLoader.BlockData`, `MapBlocksSize`, `MapsDefaultSize`, `_filesMap*`, `_filesStatics*`,
  `_mapDif*`, `_staDif*` (:47-51, :72-105).
- `HuesLoader.HuesRange` / `RadarCol` (:55-59) — patched in place by verdata (UOFileManager.cs:299-306).
- `ClilocLoader._entries` (:48), `SkillsLoader.Skills`/`SortedSkills` (:54-55),
  `ProfessionLoader.Professions` (:81), `SpeechesLoader._speech` (:46).
- `ExternalImageLoader.gump_textureCache` / `art_textureCache` / `EmbeddedArt` /
  `gump_availableFilePaths` / `art_availableFilePaths` (:16-23), `GraphicsDevice` (:25).
- `TileArtLoader._tileArtInfos` (TileArt.cs:19) — grows unbounded as graphics are queried.
- `SoundOverrideLoader.soundCache` (:14) + `loaded` flag (:15).

## Timing

**On load (once, at client startup)** — `Client.cs:183` → `UOFileManager.Load`:
- All 18 loader `Load()` calls are launched as `Task.Run` and awaited together with a hard
  **15-second timeout** (UOFileManager.cs:127); on timeout `Log.Panic` is called but the loaders keep
  running. `SkillsLoader` chains `ProfessionLoader` via `ContinueWith` (:116).
- `ExternalImageLoader.Instance.Load()` runs synchronously afterwards (:132), then `Read_Art_def()` (:134),
  then verdata patching (:147-317).
- `Verdata`'s static ctor fires on first touch of `Verdata.File` (UOFileManager.cs:136).
- `SoundOverrideLoader`'s ctor starts an unawaited `Task.Factory.StartNew` that reads every mp3 in
  `SoundOverrides/` (SoundOverrideLoader.cs:22).
- `ExternalImageLoader.LoadResourceAssets()` is started from `GameController.cs:210` on a `Task.Run`
  and creates `Texture2D`s from the graphics device.

**On demand / per frame (renderer cache misses)** — the renderer caches sprites, so these run only
when a graphic is first needed, but that happens *during* the frame:
- `ArtLoader.GetArt` ← `ClassicUO.Renderer/Arts/Art.cs:45`; `ExternalImageLoader.LoadArtTexture` first (:40).
- `GumpsLoader.GetGump` ← `Renderer/Gumps/Gump.cs:32`; external override at :27.
- `TexmapsLoader.GetTexmap` ← `Renderer/Texmaps/Texmap.cs:27`.
- `LightsLoader.GetLight` ← `Renderer/Lights/Light.cs:27`, itself reached from `GameScene.cs:1445`
  while drawing the light pass.
- `AnimationsLoader.GetIndices` / `ReadUOPAnimationFrames` / `ReadMULAnimationFrames`
  ← `Renderer/Animations/Animation.cs:147/260/277`.
- `FontsLoader.Generate*` — every time a text texture is (re)built; `GetInfoASCII/Unicode`,
  `GetWidth*`, `GetCaretPos*` are also called from UI layout code on typing/resize.
- `MultiMapLoader.LoadMap` ← `Renderer/MultiMaps/MultiMap.cs:24` when a world-map gump opens.
- `TileArtLoader.TryGetTileArtInfo` ← `PaperDollInteractable.cs:454`, decompresses on first hit and caches.
- `AnimDataLoader.CalculateCurrentGraphic` — per animated static, per frame, in the tile drawing path.

**On packet / event:**
- `MapLoader.LoadMap(i)` when the player changes facet; `MapLoader.ApplyPatches` from the map-patch packet.
- `SoundsLoader.LoadMusicConfig(era)` from `AudioManager.cs:253` when the music-era setting changes.
- `AnimationsLoader.ProcessBodyConvDef(flags)` from `Animation.cs:325` when body-conv flags change —
  re-reads Bodyconv.def and rewrites `_bodyConvInfos` at runtime.
- `UOFileManager.MapLoaderReLoad` from `UltimaLive.cs:847` — swaps the whole `MapLoader` singleton
  and disposes the previous one.
- `ClilocLoader.GetString`/`Translate` per tooltip/megacliloc packet.
- `SpeechesLoader.GetKeywords` per outgoing speech line — O(entries × keywords) linear scan (:149).

## Inbound

- `ClassicUO.Client/Client.cs:183` → `UOFileManager.Load(...)` — the single entry point that
  populates the whole partition.
- `ClassicUO.Renderer` sprite caches: `Arts/Art.cs`, `Gumps/Gump.cs`, `Texmaps/Texmap.cs`,
  `Lights/Light.cs`, `MultiMaps/MultiMap.cs`, `Animations/Animation.cs` call the loaders' `Get*`
  methods on cache miss and immediately copy pixels into a `Texture2D` atlas.
- `ClassicUO.Client/GameController.cs:209-210` sets `ExternalImageLoader.GraphicsDevice` and starts
  `LoadResourceAssets()`.
- `ClassicUO.Client/Game/UltimaLive.cs:847` → `UOFileManager.MapLoaderReLoad(this)` (UltimaLive
  subclasses `MapLoader`, which is why the ctor is `protected` and the fields are `protected`).
- `ClassicUO.Client/Game/Managers/AudioManager.cs:253` → `SoundsLoader.LoadMusicConfig(era)`.
- `ClassicUO.Client/Game/UI/Controls/PaperDollInteractable.cs:454` → `UOFileManager.TileArtLoader`.
- Many UI gumps pull embedded textures via `ExternalImageLoader.Instance.TryGetEmbeddedTexture(...)`
  (`SpellBar.cs:118`, `ModernPaperdoll.cs:32`, `DiscordGump.cs:91`, `ModernUIConstants.cs:12`, …).
- 161 files across `ClassicUO.Client` + `ClassicUO.Renderer` reference the `ClassicUO.Assets` namespace.

## Outbound

- `ClassicUO.IO`: `UOFile`, `UOFileMul`, `UOFileUop`, `UOFileIndex`, `UOFileIndex5D`, `DataReader`,
  `StackDataReader`, `UOFileUop.CreateHash`, `UOFile.FillEntries`/`SetData`/`Seek`/`Read*`.
- `ClassicUO.Utility`: `ClientVersion`, `DefReader`, `TextFileParser`, `FileSystemHelper.EnsureFileExists`,
  `HuesHelper.Color16To32`/`RgbaToArgb`, `ZLib.Decompress`, `BwtDecompress.Decompress`, `StringHelper`,
  `ValueStringBuilder`, `FastList<T>`, `UOFilesOverrideMap`, `PlatformHelper`, `Log`.
- `FontStashSharp` — `FontSystem`, `SpriteFontBase` (TrueTypeLoader).
- `Microsoft.Xna.Framework(.Graphics)` — `Texture2D`, `Color`, `GraphicsDevice` (ExternalImageLoader only;
  the rest of the partition is graphics-API free).
- Cross-loader calls inside the partition: `GumpsLoader.GetGump` → `HuesLoader.ApplyHueRgba5551`;
  `FontsLoader` → `HuesLoader.GetPartialHueColor`/`ApplyHueRgba8888`; `MultiMapLoader` →
  `HuesLoader.HuesRange`; `SoundsLoader.TryGetSound` → `SoundOverrideLoader`;
  `ProfessionLoader` → `SkillsLoader` + `ClilocLoader`; `UOFileManager.Read_Art_def` →
  `ArtLoader.Entries` + `TileDataLoader.LandData/StaticData`.

## Hazards

- ArtLoader.cs:47,212 — `GetRawImage` returns a `Span<uint>` over the `[ThreadStatic] _data` buffer;
  the next `GetArt`/`GetRawImage` on the same thread overwrites it. `ArtInfo.Pixels` is only valid
  until the next call.
- AnimationsLoader.cs:1439,1496 — both frame readers return spans over the `[ThreadStatic] _frames`
  array, and `FrameInfo.Pixels` arrays inside it are reused across different animations
  (`ReadSpriteData` refills them, AnimationsLoader.cs:1518-1525).
- AnimationsLoader.cs:1364-1367 / 1481-1484 — `_frames` is reallocated when a bigger frame count
  appears; any span handed out earlier now points at the old array.
- AnimationsLoader.cs:365 — `_files[fileIndex]` is indexed without a bounds check; `fileIndex` comes
  from `_bodyConvInfos` (:355), populated from Bodyconv.def.
- AnimationsLoader.cs:319-320 — `mountHeight = uopInfo.HeightOffset` is read even when
  `TryGetUOPData` returned false and `uopInfo` is `default`.
- AnimationsLoader.cs:1327-1330 — `_decompressedData` is only grown, and the ZLib destination length
  comes from `index.Unknown` read out of the UOP entry.
- AnimationsLoader.cs:1551-1560 — `block = y * frame.Width + x` is computed from file-supplied
  offsets and written into `data[block]` with no clamp against `frame.Pixels.Length`.
- AnimationsLoader.cs:1824-1833 — `UopInfo.ReplacedAnimations` returns a `Span<int>` over a `fixed`
  buffer inside a struct; taken from a `TryGetValue` local copy at :283 and :892.
- GumpsLoader.cs:210-219 — the hued value computed at :212 is immediately discarded and overwritten
  at :218 with the unhued `HuesHelper.Color16To32(gmul[i].Value)`; `entry.Hue` from gump.def has no
  visible effect.
- GumpsLoader.cs:222 — `pixels.AsSpan().Slice((int)pos, count)` uses run lengths straight from the
  file; a corrupt run overruns the row / the buffer.
- GumpsLoader.cs:187-202 — `lookuplist` is read from the decompressed buffer without validating the
  per-row offsets against `len`.
- MultiLoader.cs:87 — `File.FillEntries(ref Entries)` is called unconditionally; if neither
  MultiCollection.uop nor multi.mul/idx exists, `File` is null.
- ArtLoader.cs:86 — same shape: `_file.FillEntries` runs even when neither art.mul nor the uop was found.
- MapLoader.cs:292-299 — `LoadMap(i, useXFiles: true)` permanently overwrites `_filesMap[i]`,
  `_filesStatics[i]`, `_filesIdxStatics[i]` with the X variants; there is no path back to the
  non-X files for that index.
- MapLoader.cs:263-275 — when map1 is missing, `_filesMap[1]`/`_filesStatics[1]`/`_filesIdxStatics[1]`
  are aliased to the map0 objects; disposing one disposes the other.
- MapLoader.cs:329-332 — `fileidx`/`staticfile` are dereferenced with no null check for `i != 1`.
- MapLoader.cs:639-643 — `GetIndex(map, x, y)` does no bounds checking on `map` or the computed block.
- MapLoader.cs:409-423, :426-448 — `PatchMapBlock`/`PatchStaticBlock` index `BlockData[0][block]`
  with an unvalidated `block` from verdata; only `maxBlockCount < 1` is checked.
- MapLoader.cs:725 — `IndexMap.Invalid` is a mutable public static field.
- MapLoader.cs:54,148 — `MAPS_COUNT` is a mutable static rewritten from `MapsLayouts` during `Load()`,
  after arrays sized from the old value may already exist elsewhere.
- UOFileManager.cs:127 — file loading is bounded by a 15-second `Wait`; on timeout the client logs
  `Log.Panic` and continues while the loader tasks are still writing to loader state.
- UOFileManager.cs:182 — verdata FileID 12 writes `GumpsLoader.Instance.Entries[vh.BlockID]` with no
  length check (the FileID 4 branch at :168 does check).
- UOFileManager.cs:213-216 — the verdata skill patch reads from `verdata.StartAddress` (offset 0)
  rather than `vh.Position`.
- ProfessionLoader.cs:43-48,61-62 — `_VoidSkills`/`_VoidStats` are shared static arrays used as the
  default value of every `ProfessionInfo.SkillDefVal`/`StatsVal`; a write through one instance is
  visible in all of them.
- ProfessionLoader.cs:46 — `_VoidSkills` is initialised in a static field initialiser that reads
  `UOFileManager.Version`; the value baked in depends on when the type is first touched relative to
  `UOFileManager.Load` setting `Version` (UOFileManager.cs:95).
- ExternalImageLoader.cs:313-388 — `LoadResourceAssets` runs on a `Task.Run` but calls
  `Texture2D.FromStream`, `GetData`, `SetData` on the FNA graphics device off the frame thread, and
  writes `gump_textureCache` / `EmbeddedArt` concurrently with `LoadGumpTexture` reads.
- ExternalImageLoader.cs:105,164,351,380 — `Dictionary.Add` (not indexer) on the caches; a duplicate
  key throws.
- ExternalImageLoader.cs:66-121 — `gump_availableFilePaths` is checked before the cache, so a graphic
  cached from an embedded resource is only reachable because :356 back-fills a fake path string.
- ExternalImageLoader.cs:16-23 — the caches only grow; `ClearAllPixelCaches` (:409) is the sole eviction.
- SoundOverrideLoader.cs:22-39 — `soundCache` is filled on a background task and read from
  `TryGetSoundOverride` (:42) guarded only by a non-volatile `loaded` bool.
- SoundsLoader.cs:285-292 — `GetValidRefEntry` may return `UOFileIndex.Invalid`, and
  `_file.SetData(entry.Address, entry.FileSize)` is called on it before `entry.Length <= 0` is tested.
- SoundsLoader.cs:313 — `new byte[entry.Length - 40]` throws if a Sound.def-aliased entry is shorter
  than the 40-byte name header.
- SoundsLoader.cs:102 — `index >= _file.Length` compares a sound index against a file byte length.
- SoundsLoader.cs:340 — `int.Parse(splits[0])` on a Config.txt line with no try/catch.
- SoundsLoader.cs:368 — `GetTrueFileName` does a recursive `Directory.GetFiles(dir, "*.mp3", AllDirectories)`
  once per config line.
- FontsLoader.cs:3750-3761 — `FontCharacterData.Data` is a raw `ushort*` into the memory-mapped
  fonts.mul; the `UOFileMul` created at :129 is never stored or disposed.
- FontsLoader.cs:1099,1142,1203,1232 — `table[c]` indexes the unicode font table with the raw char
  value, no bound on `c` against the mapped file size.
- FontsLoader.cs:2143 — `table` at this point is the *last glyph's* font table (reassigned at :1754),
  not the line's font, when drawing the underline.
- FontsLoader.cs:1907,2166 — `pData[block]` writes with `block` derived from `testY * width + nowX`
  where `testY` is clamped but the italic/solid offsets can push `testX` negative (`minXOk = -1` at
  :2028/:2141 without a lower clamp on the resulting index).
- FontsLoader.cs:2232 / 2959-2960 — `stackalloc HTMLChar[len]` sized by the caller's string length and
  `stackalloc char[512]` for attribute values; `bufferValue[valueLength++]` at :2985 has no bound check.
- FontsLoader.cs:3186-3195 — `linkID = _webLinks.Count + 1`; removing a link would make IDs collide,
  and `_webLinks` never shrinks.
- FontsLoader.cs:3207 — `GetWebLink` mutates `IsVisited = true` on the shared `WebLink` as a side
  effect of reading it.
- FontsLoader.cs:104,119-123,997 — `_htmlStatus`, `IsUsingHTML`, `UnusePartialHue`,
  `RecalculateWidthByInfo` are loader-wide mode flags set by whichever caller ran last;
  `GetInfoUnicode` resets part of `_htmlStatus` on entry (:1302-1305) but not `Color`.
- FontsLoader.cs:160-163 — during the font-count scan, an oversized header `continue`s instead of
  breaking, so `fonts.Position` is not advanced and the loop can spin over the remaining 224 slots.
- TileArt.cs:52-79 — `ArrayPool<byte>.Shared.Rent(entry.DecompressedLength)` with a length read from
  the UOP entry; the rented arrays are returned without clearing.
- TileArt.cs:317-321 — the loop reads `unk10Count` but iterates `unk6Count`, so the reader desyncs
  when the two differ.
- TileArt.cs:166-171 — on an unsupported tileart version the constructor returns early leaving a
  half-built `TileArtInfo`, which `TryGetTileArtInfo` (:33-37) still caches as success.
- TileArt.cs:19 — `_tileArtInfos` grows without bound and is never cleared (`ClearResources` not overridden).
- HuesLoader.cs:90-93 — `Unsafe.CopyBlockUnaligned` copies `radarcol.Length` bytes from
  `PositionAddress` into a `ushort[]` sized `Length >> 1`; an odd-length file overruns by a byte.
- HuesLoader.cs:134 — `HuesRange[g].Entries[e].ColorTable[index]`; `index` is trusted to be 0..31
  (documented, not checked).
- HuesLoader.cs:95-96 — the hues.mul/radarcol.mul `UOFileMul`s are disposed at the end of `Load`,
  after the data has been copied out — but `HuesRange` entries were produced by
  `Marshal.PtrToStructure` from those mappings (:80), so the copy must complete first.
- Verdata.cs:59-62 — `Unsafe.CopyBlockUnaligned` of `len * sizeof(UOFileIndex5D)` bytes where `len`
  is the first int in verdata.mul; guarded only by a bare `catch`.
- MultiMapLoader.cs:206 — `huesData = (ushort*)(ptr + 30800)` is a hard-coded byte offset into a
  freshly marshalled `HuesGroup` array.
- MultiMapLoader.cs:168 — `ref byte pixel = ref data[position]` where `position` is computed from
  RLE-decoded coordinates with no bound against `mapSize`.
- MultiMapLoader.cs:208 — `stackalloc uint[byte.MaxValue]` but `colorTable[i]` is filled for
  `i < maxPixelValue`, which is only bounded by the 0xFF pixel clamp at :170.
- MultiMapLoader.cs:59,251 — `HasFacet`/`LoadFacet` dereference `_facets` which is null until `Load()` completes.
- SkillsLoader.cs:99-107 — `GetSortedIndex` bounds-checks against `SkillsCount` but indexes
  `SortedSkills`, and returns `SortedSkills[index].Index`, i.e. the original index, not the sorted one.
- SkillsLoader.cs:87 — `Encoding.UTF8.GetString((byte*)_file.PositionAddress, entry.Length - 1)`
  trusts `entry.Length` from skills.idx.
- SpeechesLoader.cs:149 — `_speech` is dereferenced in `GetKeywords` with no null check; it is only
  assigned at the end of the background `Load()` task (:85).
- SpeechesLoader.cs:113 — `input.IndexOf(split[i], input.Length - split[i].Length, ...)` — negative
  start index if the keyword is longer than the input (the `>` guard at :98 allows equality only).
- ClilocLoader.cs:113-115 — reads the whole cliloc into `new byte[fileStream.Length]`; the read loop
  stops on the first zero-byte read.
- ClilocLoader.cs:237-255 — `stackalloc (int,int)[++totalArgs]` sized from the caller-supplied
  argument string, on a path reached per tooltip packet.
- TexmapsLoader.cs:96 — `Entries[index] = Entries[checkindex]` inside the group loop with no `break`,
  so the last group member wins.
- LightsLoader.cs:78 — `new uint[entry.Width * entry.Height]` allocated per `GetLight` call; the loop
  at :82-101 reads `Width*Height` bytes without checking `entry.Length`.
- AnimDataLoader.cs:73-84 — pointer arithmetic `address + (graphic * 68 + 4 * ((graphic >> 3) + 1))`
  is bounds-checked only against the file end, not against a negative/overflowed offset.
- TrueTypeLoader.cs:161-162 — when the requested font is missing, `_fonts.First().Value` is used;
  dictionary ordering is not defined.
- UOFileManager.cs:52 — `UOFilesOverrideMap.Instance.TryGetValue(file.ToLowerInvariant(), ...)`
  is consulted for every path lookup, including per-call paths.
- UOFileManager.cs:58-82 — on non-Windows, a missing file triggers a full `Directory.GetFiles` scan
  of the data directory on every `GetUOFilePath` miss.

## Fork deltas

Clearly TazUO / Holiday-Edition additions rather than stock ClassicUO:

- `ExternalImageLoader.cs` (entire file) — external PNG/BMP art and gump overrides plus
  `gumpartassets` embedded resources; the `LoadResourceAssets` range `40303..40312` (:321) is TazUO's
  own custom gump art. Referenced from TazUO-only UI (`SpellBar`, `ModernPaperdoll`, `DiscordGump`,
  `ModernUIConstants`, `Supporters`).
- `SoundOverrideLoader.cs` (entire file) — `SoundOverrides/<id>.mp3` replacement, hooked into
  `SoundsLoader.TryGetSound` at SoundsLoader.cs:280.
- `SoundsLoader.LoadMusicConfig(string eraFolder)` (:150-252) and `ParseMusicConfig` (:254) — the
  music-era folder override, driven from `AudioManager.cs:253`. Stock ClassicUO parses Config.txt once
  inside `Load()`.
- `TrueTypeLoader.ClampFontSize` + `MIN_FONT_SIZE`/`MAX_FONT_SIZE` (:46-73) and the null-name guard in
  `GetFont` (:156) — the comments ("takes the client down", "does not exist on net472") mark this as a
  Holiday-Edition fix. `LoadEmbeddedFonts` (:111) and the `Fonts/` directory scan are TazUO.
- `AnimationsLoader.ReplaceUopGroup` bounds guard (:271-279) and the `oldGroup` range check in
  `LoadUop` (:904-911) — both carry Holiday-style explanatory comments about shard files crashing
  the client at startup.
- `TileDataLoader.cs:78-79` — comment "Remove artificial 2048 limit to support high item IDs (45535+)";
  `staticscount` is now derived from file length instead of being clamped.
- `UOFileManager.GetUOFilePath` case-insensitive fallback with the ambiguity warning (:58-82) and
  `UOFilesOverrideMap` (:52) — TazUO.
- `AnimationsLoader.cs:167-171` — `uint.TryParse` with an error log replacing stock's throwing
  `uint.Parse` for mobtypes.txt.
- `MapLoader` X-file support (`_filesMapX`, `_filesStaticsX`, `_filesIdxStaticsX`, `LoadMap(i, useXFiles)`)
  — :104-120, :188-206, :237-246, :270-275, :292-300.
- `MultiMapLoader.LoadMap`'s `try/finally` around `Marshal.FreeHGlobal` (:211-230) — leak fix vs. stock.
- `TileArt.cs` — file-scoped namespace, collection expressions (`[]`), `TileArtLoader` as a plain
  instance on `UOFileManager` rather than a singleton; this reads as ported-back-from-`main`
  (newer upstream) code sitting in an otherwise net472-era file set.
- `AnimDataLoader`, `SkillsLoader`, `ClilocLoader`, `HuesLoader`, `SpeechesLoader`, `ProfessionLoader`,
  `Verdata`, `UOFileLoader` are essentially stock ClassicUO.
