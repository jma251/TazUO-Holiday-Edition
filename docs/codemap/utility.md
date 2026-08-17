# utility

Partition: `src/ClassicUO.Utility/` — the leaf assembly. 49 `.cs` files, 9,310 lines, all read in full.
Depends only on FNA (`Microsoft.Xna.Framework` for `Point`/`Vector2`/`Rectangle`/`Color`), `SDL2`, `System.Memory`,
`System.Text.Json`. Nothing in this project references `ClassicUO.Client`, `.Assets`, `.Renderer` or `.IO` — it is
the bottom of the dependency graph and every other partition calls *into* it.

Four namespaces live here: `ClassicUO.Utility`, `ClassicUO.Utility.Collections`, `ClassicUO.Utility.Logging`,
`ClassicUO.Utility.Platforms`, plus two vendored third-party namespaces `StbRectPackSharp` and `StbTextEditSharp`,
and `ZLibNative`.

## Files

| Path (all under `src/ClassicUO.Utility/`) | Lines | Purpose |
| --- | --- | --- |
| `AverageOverTime.cs` | 71 | Sliding-window mean over a `Queue<(uint tick,double)>`; one instance per `GameObject`. |
| `BwtDecompress.cs` | 191 | Burrows–Wheeler + MTF decompressor for a UO asset blob. |
| `ByteFlagHelper.cs` | 34 | Add/Has/Remove flag on `byte` and `ulong`. File-scoped namespace. |
| `ClientVersion.cs` | 226 | `ClientVersion` enum (packed `maj<<24|min<<16|build<<8|extra`) + parse from `client.exe` VS_VERSION_INFO and from text. |
| `Collections/Bag.cs` | 217 | Unordered array-backed bag, swap-remove, struct enumerator. |
| `Collections/CollectionHelper.cs` | 128 | `ReifyCollection<T>` — wrap `IEnumerable` as `IReadOnlyCollection` without copying when possible. |
| `Collections/Deque.cs` | 1060 | Circular-buffer double-ended queue, `IList<T>`/`IList`, `ref` accessors `GetAt/Front/Back`. |
| `Collections/FastList.cs` | 181 | Auto-growing array wrapper with public `Buffer`/`Length` fields. |
| `Collections/OrderedDictionary.cs` | 623 | Insertion-ordered dictionary over `KeyedCollection`, plus `KeyedCollection2`, `Comparer2`, `DictionaryEnumerator`, `IOrderedDictionary<K,V>`. |
| `Collections/ReadOnlyArrayView.cs` | 127 | Readonly struct window `(T[], start, count)` with struct enumerator. |
| `Crypter.cs` | 143 | XOR "encryption" of saved passwords keyed on `Environment.MachineName`. |
| `Easings.cs` | 118 | 30 float easing curves (Kryzarel). |
| `Extensions.cs` | 292 | `static class Exstentions` (sic): `Raise`/`RaiseAsync`, `Task.Catch`, `List.Resize`, `InRect`, `ToHex`, `ToHtmlHex`/`FromHtmlHex`, net472 `ZipArchive.ExtractToDirectory(overwrite)`, `GetAllCharacterPaths`. |
| `FileSystemHelper.cs` | 133 | Folder creation with invalid-char stripping, `RemoveInvalidChars`, recursive `CopyAllTo`, `ReadAllTextShared`. |
| `HuesHelper.cs` | 63 | ARGB1555 <-> 32-bit colour, 32-entry 5→8-bit ramp table. |
| `JsonHelper.cs` | 141 | `SaveAndBackup` (3-deep backup rotation) / `Load` (falls back through backups); two obsolete non-context APIs. |
| `Logging/Log.cs` | 117 | Static facade over a single `Logger`. |
| `Logging/LogFile.cs` | 118 | Append-mode `FileStream` log sink, sync + async write. |
| `Logging/LogTypes.cs` | 49 | `[Flags] LogTypes : byte`. |
| `Logging/Logger.cs` | 158 | Console writer, coloured per level, lock-guarded, indent stack. |
| `MathHelper.cs` | 137 | `InRange`, UO chebyshev `GetDistance`, `Combine`/`GetNumbersFromCombine` (two ints ↔ ulong key), percentage helpers, `MachineEpsilonFloat`. |
| `ObjectPool.cs` | 43 | Generic `Stack<T>` pool with factory + on-return hook, `MaxCapacity` 3000. File-scoped namespace. |
| `Platforms/Native.cs` | 139 | `LoadLibrary`/`GetProcAddress`/`FreeLibrary` over kernel32 or libdl. |
| `Platforms/PlatformHelper.cs` | 76 | `IsWindows/IsLinux/IsOSX/IsMonoRuntime` statics, `LaunchBrowser`. |
| `Profiler.cs` | 280 | Per-frame hierarchical timing, 60-sample ring per context. |
| `QueuedPool.cs` | 91 | `Stack<T>` pool for `new()`-constrained types, pre-filled, on-pickup hook. Backs every GameObject type. |
| `RandomHelper.cs` | 66 | Shared `Random`. |
| `RegexHelper.cs` | 16 | `ConcurrentDictionary<string,Regex>` compiled-regex cache. File-scoped namespace. |
| `StbRectPack/CRuntime.cs` | 86 | `malloc`/`free` over `Marshal.AllocHGlobal`, recursive `qsort`. |
| `StbRectPack/Packer.cs` | 86 | Atlas rect packer wrapper, 2px default gutter. |
| `StbRectPack/StbRectPack.Generated.cs` | 307 | Sichem-generated skyline packer (`stbrp_*`). |
| `StbRectPack/StbRectPack.cs` | 55 | `stbrp_context` struct with malloc'd node arrays, `IDisposable`. |
| `StbTextedit/Enums.cs` | 59 | `[Flags] ControlKeys` (Shift = 0x20000). |
| `StbTextedit/FindState.cs` | 111 | Locate a char index's row/x/y via `ITextEditHandler.LayoutRow`. |
| `StbTextedit/ITextEditHandler.cs` | 44 | `Text`, `Length`, `LayoutRow(int)`, `GetWidth(int)` — implemented by the client's text input gumps. |
| `StbTextedit/TextEdit.cs` | 1032 | The text-input state machine: cursor, selection, insert/delete, word motion, undo/redo, click/drag. |
| `StbTextedit/TextEditRow.cs` | 43 | Row metrics struct. |
| `StbTextedit/UndoRecord.cs` | 44 | `{where, insert_length, delete_length, char_storage}`. |
| `StbTextedit/UndoState.cs` | 198 | Fixed 99-record / 999-char undo+redo ring. |
| `StringHelper.cs` | 415 | cp1252 ↔ unicode, capitalisation helpers, `IsSafeChar`, `AddSpaceBeforeCapital`, `IntToAbbreviatedString`, SDL clipboard read, `GetPluralAdjustedString` (cliloc `%x/y%`), `UnsafeCompare`. |
| `TextFileParser.cs` | 180 | Line/token tokenizer for `.def`/`.txt` UO data files, delimiter+comment+quote aware. |
| `UInt16Converter.cs` | 55 | Lenient hue/id parse (`0x`, negative, >65535 decimal). |
| `UnsafeMemoryManager.cs` | 174 | `UnmanagedMemoryPool` free-list allocator over `AllocHGlobal`. |
| `ValueStringBuilder.cs` | 377 | `ref struct` builder over `Span<char>` + `ArrayPool<char>`. |
| `ZLib.cs` | 278 | zlib facade; picks native `zlib`/`libz` on x64, managed on x86. `ZLibError` enum. |
| `ZLib/Adler32.cs` | 105 | Adler-32 with deferred modulus every 5550 bytes. |
| `ZLib/ZLIBStream.cs` | 375 | zlib framing (2-byte header + trailing Adler) around `DeflateStream`. |
| `ZLib/ZLibHeader.cs` | 155 | CMF/FLG encode/decode, `FLevel` enum. |
| `ZLib/ZLibManaged.cs` | 93 | Byte-at-a-time managed decompress into `byte[]` or `IntPtr`. |

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `AverageOverTime` | `AverageOverTime.cs:6` | Windowed mean/rate; `AddValue(uint currentTicks, double)`. |
| `BwtDecompress` (static) | `BwtDecompress.cs:12` | `Decompress(byte[])`. |
| `ByteFlagHelper` (static) | `ByteFlagHelper.cs:3` | byte/ulong flag ops. |
| `ClientVersion` (enum) | `ClientVersion.cs:40` | 47 named protocol milestones, `CV_OLD`..`CV_7010400`. |
| `ClientVersionHelper` (static) | `ClientVersion.cs:84` | `TryParseFromFile`, `IsClientVersionValid`. |
| `Bag<T>` | `Collections/Bag.cs:39` | Unordered container; `RemoveAt` swaps last into hole. |
| `Bag<T>.BagEnumerator` (struct) | `Collections/Bag.cs:189` | volatile `_bag`/`_index`. |
| `CollectionHelper` (static) | `Collections/CollectionHelper.cs:39` | `ReifyCollection<T>`. |
| `Deque<T>` (sealed) | `Collections/Deque.cs:48` | Circular deque, `DefaultCapacity = 8` (`:53`). |
| `FastList<T>` | `Collections/FastList.cs:15` | Public `Buffer` (`:20`) and `Length` (`:25`) fields; growth `max(len<<1, 10)`. |
| `OrderedDictionary<K,V>` | `Collections/OrderedDictionary.cs:47` | Ordered dict. |
| `KeyedCollection2<K,I>` | `Collections/OrderedDictionary.cs:479` | `KeyedCollection` with delegate key extractor + `Sort`. |
| `Comparer2<T>` | `Collections/OrderedDictionary.cs:542` | `Comparison<T>` → `Comparer<T>`. |
| `DictionaryEnumerator<K,V>` | `Collections/OrderedDictionary.cs:562` | `IDictionaryEnumerator` adapter. |
| `IOrderedDictionary<K,V>` | `Collections/OrderedDictionary.cs:601` | Interface. |
| `ReadOnlyArrayView<T>` (readonly struct) | `Collections/ReadOnlyArrayView.cs:39` | Array window. |
| `Crypter` (static) | `Crypter.cs:38` | `Encrypt`/`Decrypt`; `CalculateKey()` at `:139`. |
| `Easings` (static) | `Easings.cs:7` | Easing curves. |
| `Exstentions` (static) | `Extensions.cs:45` | Extension grab-bag. |
| `FileSystemHelper` (static) | `FileSystemHelper.cs:39` | Path/dir helpers; `ReadAllTextShared` at `:125`. |
| `HuesHelper` (static) | `HuesHelper.cs:38` | `Color16To32` (`:53`), `Color32To16` (`:59`), `RgbaToArgb` (`:47`). |
| `JsonHelper` (static) | `JsonHelper.cs:10` | `SaveAndBackup` (`:59`), `Load` (`:111`), `GetBackupSavePath` (`:139`). |
| `Log` (static) | `Logging/Log.cs:37` | Facade. `Debug` is `[Conditional("DEBUG")]` (`:67`). |
| `LogFile` (sealed) | `Logging/LogFile.cs:40` | File sink. |
| `LogTypes` (flags enum) | `Logging/LogTypes.cs:38` | `Table = 0x30` overlaps `Error|Panic`. |
| `Logger` | `Logging/Logger.cs:38` | Console writer. |
| `MathHelper` (static) | `MathHelper.cs:39` | Geometry/packing. |
| `ObjectPool<T>` | `ObjectPool.cs:6` | `Get`/`Return`/`Clear`, `MaxCapacity` default 3000 (`:11`). |
| `Native` (static) | `Platforms/Native.cs:39` | Dynamic library loading; loader chosen in static ctor (`:43`). |
| `PlatformHelper` (static) | `Platforms/PlatformHelper.cs:40` | OS statics + `LaunchBrowser`. |
| `Profiler` (static) | `Profiler.cs:40` | `ProfileTimeCount = 60` (`:42`). |
| `Profiler.ProfileData` | `Profiler.cs:188` | 60-slot ring of frame times. |
| `Profiler.ContextAndTick` (readonly struct) | `Profiler.cs:263` | Name + stopwatch tick. |
| `QueuedPool<T>` | `QueuedPool.cs:38` | Pre-filled stack pool. |
| `RandomHelper` (static) | `RandomHelper.cs:37` | Shared RNG. |
| `RegexHelper` (static) | `RegexHelper.cs:6` | Compiled-regex cache. |
| `CRuntime` (static unsafe) | `StbRectPack/CRuntime.cs:6` | malloc/free/qsort. |
| `Packer` (unsafe, IDisposable) | `StbRectPack/Packer.cs:10` | Atlas packing; `PackRect` at `:52`. |
| `StbRectPack.stbrp_context` (struct) | `StbRectPack/StbRectPack.cs:8` | Owns two `malloc`'d node blocks. |
| `stbrp_rect` / `stbrp_node` / `stbrp__findresult` | `StbRectPack.Generated.cs:16/27/35` | Packer POD. |
| `ControlKeys` (flags enum) | `StbTextedit/Enums.cs:38` | Editing commands. |
| `FindState` (struct) | `StbTextedit/FindState.cs:35` | `FindCharPosition` at `:44`. |
| `ITextEditHandler` | `StbTextedit/ITextEditHandler.cs:35` | Client-side text host contract. |
| `TextEdit` | `StbTextedit/TextEdit.cs:39` | Editing state machine; `Key(ControlKeys)` at `:468`. |
| `TextEditRow` (struct) | `StbTextedit/TextEditRow.cs:35` | Row metrics. |
| `UndoRecord` (struct) | `StbTextedit/UndoRecord.cs:38` | Undo entry. |
| `UndoState` | `StbTextedit/UndoState.cs:37` | Fixed-size undo/redo arena. |
| `StringHelper` (static) | `StringHelper.cs:42` | Text utilities. |
| `TextFileParser` | `TextFileParser.cs:8` | Tokenizer. |
| `UInt16Converter` (static) | `UInt16Converter.cs:37` | `Parse`. |
| `UnmanagedMemoryPool` (unsafe struct) | `UnsafeMemoryManager.cs:39` | Free-list pool header. |
| `UnsafeMemoryManager` (static unsafe) | `UnsafeMemoryManager.cs:49` | Alloc/Calloc/Free/FreeAll/FreePool/Memset. |
| `ValueStringBuilder` (ref struct) | `ValueStringBuilder.cs:10` | Pooled span builder. |
| `ZLib` (static) | `ZLib.cs:39` | Compressor facade + `ZLibError` (`:91`). |
| `Adler32` | `ZLib/Adler32.cs:35` | Checksum. |
| `ZLIBStream` (sealed Stream) | `ZLib/ZLIBStream.cs:39` | zlib framing. |
| `ZLibHeader` (sealed) / `FLevel` | `ZLib/ZLibHeader.cs:45` / `:37` | Header codec. |
| `ZLibManaged` (static) | `ZLib/ZLibManaged.cs:40` | Managed fallback. |

## State

Everything below is process-global (static) unless noted; none of it is per-world or reset on server disconnect.

- `Log._logger` — `Logging/Log.cs:39`. Single `Logger`; `Stop()` nulls it (`:54`).
- `Logger._logTypesInfo` — `Logging/Logger.cs:40`, static readonly dict, no `LogTypes.Table` entry.
- `Logger._indent`, `_isLogging`, `_syncObject` — `Logging/Logger.cs:65,67,68`. Instance, `_syncObject` guards `Message`/`NewLine` only.
- `LogFile.logStream` — `Logging/LogFile.cs:42`. Instance `FileStream`, `FileShare.ReadWrite`, `useAsync: true`, filename stamped `yyyy-MM-dd_hh-mm-ss` (12-hour `hh`).
- `Profiler.m_Context` (stack of open contexts), `m_ThisFrameData`, `m_AllFrameData`, `m_TotalTimeData`, `_timer`, `m_BeginFrameTicks` — `Profiler.cs:43-48`. `Enabled` (mutable public bool) `Profiler.cs:65`.
- `Profiler.ProfileData.Empty` — `Profiler.cs:190`, a *mutable shared* instance returned from `GetContext` misses (`:174,185`).
- `Profiler.ProfileData.m_LastTimes[60]`, `m_LastIndex` — `Profiler.cs:191,192`.
- `HuesHelper._table` — `HuesHelper.cs:40`, 32-byte 5→8-bit ramp.
- `MathHelper.MachineEpsilonFloat` — `MathHelper.cs:41`, computed in static init.
- `RandomHelper._random` — `RandomHelper.cs:39`, one shared `Random`.
- `RegexHelper._regexes` — `RegexHelper.cs:8`, unbounded `ConcurrentDictionary<string,Regex>`; never evicted.
- `StringHelper._dots` — `StringHelper.cs:44`.
- `PlatformHelper.IsMonoRuntime/IsWindows/IsLinux/IsOSX` — `Platforms/PlatformHelper.cs:42-46`.
- `Native._loader` — `Platforms/Native.cs:41`, chosen once in static ctor.
- `ZLib._compressor` — `ZLib.cs:43`, chosen once in static ctor by `Environment.Is64BitProcess` + OS.
- `UnsafeMemoryManager.SizeOfPointer`, `MinimumPoolBlockSize` — `UnsafeMemoryManager.cs:51,53`.
- `ObjectPool<T>._pool` / `MaxCapacity` — `ObjectPool.cs:8,11`. Instance, but every real user is a `static readonly` field (`Pathfinder.cs:130,1114,1184`).
- `QueuedPool<T>._pool` — `QueuedPool.cs:41`. Instance, but every user is `static readonly` (`Chunk.cs:44`, `Mobile.cs:48`, `Item.cs:52`, `Static.cs:42`, `Multi.cs:43`, `Land.cs:45`, `TextObject.cs:44`, `RenderedText.cs:64`).
- `UndoState.undo_char[999]`, `undo_rec[99]` — `StbTextedit/UndoState.cs:41,44`. Per `TextEdit`, allocated eagerly (≈4KB + 1.6KB per text field).
- `TextEdit.CursorIndex/SelectStart/SelectEnd/PreferredX/InsertMode/SingleLine` — `StbTextedit/TextEdit.cs:63-72`. Indices into a string the *handler* owns and can replace behind `TextEdit`'s back.
- `ValueStringBuilder._arrayToReturnToPool/_chars/_pos` — `ValueStringBuilder.cs:12-14`. Rented from `ArrayPool<char>.Shared`.
- `TextFileParser._sb`, `_pos`, `_eol`, `_string`, `_Size` — `TextFileParser.cs:10-16`. Mutable parse cursor; `_sb` is reused across all tokens.
- `stbrp_context.all_nodes/extra` — `StbRectPack/StbRectPack.cs:18,19`. Raw `AllocHGlobal` blocks, freed only in `Dispose`.
- `Packer.PackeRectanglesCount` — `StbRectPack/Packer.cs:16`, monotonic id counter used as `stbrp_rect.id`.
- `AverageOverTime._values`/`_sum` — `AverageOverTime.cs:9,10`. Per-`GameObject` (`GameObject.cs:56,141`, 15-second window).

## Timing

- **Per frame (60 Hz target, `GameController`)**: `Profiler.EndFrame()`/`BeginFrame()` bracket the draw at
  `ClassicUO.Client/GameController.cs:557-558`; `EnterContext`/`ExitContext` pairs wrap "Mouse", "Packets",
  "Update", "UI Update", "LScript", "Draw-Tiles", "Draw-Scene", "Draw-UI", with an "OutOfContext" span in
  between (`GameController.cs:468-580`). `Profiler.ProfileTimeCount = 60` means each context keeps exactly one
  second of history at 60fps (`Profiler.cs:42`). `BeginFrame`/`EndFrame`/`EnterContext`/`ExitContext` are
  `[Conditional("DEBUG")]` (`Profiler.cs:67,104,116,127`) — in Release they vanish, but `InContext` (`:155`) and
  `GetContext` (`:170`) are **not** conditional and still run, returning `false`/`ProfileData.Empty` because
  `Enabled` stays false.
- **Per frame, hot**: `ValueStringBuilder` is constructed/disposed in text layout and tooltip paths (11 files
  outside this partition); each ctor is an `ArrayPool<char>.Shared.Rent` and each `ToString()`/`Dispose()` a
  `Return`. `HuesHelper.Color16To32` is per-pixel in hue/texture conversion (14 external files).
- **Per object spawn / despawn (packet-driven)**: `QueuedPool<T>.GetOne()`/`ReturnOne()` on `Item`, `Mobile`,
  `Static`, `Multi`, `Land`, `Chunk`, `TextObject`, `RenderedText`. `_on_pickup` runs on every rent
  (`QueuedPool.cs:74`).
- **Per pathfinding request**: `ObjectPool<List<GameObject>>`, `ObjectPool<PathObject>`, `ObjectPool<PathNode>`
  (`Pathfinder.cs:130,1114,1184`).
- **Per packet**: `ZLib.Decompress` on the compressed game stream and on housing/multi data; `BwtDecompress`
  on the asset blob; `UInt16Converter.Parse` on server-sent numeric text.
- **Per keystroke / mouse event**: the whole `StbTextedit` stack. `TextEdit.InputChar` (`TextEdit.cs:437`),
  `Key` (`:468`), `Click` (`:255`), `Drag` (`:270`). Every insert/delete rebuilds the entire string via
  `text.Substring(...) + ... + text.Substring(...)` (`:135,168`) — O(n) allocation per character typed.
- **On load**: `TextFileParser` over `.def`/`.txt` UO data, `ClientVersionHelper.TryParseFromFile` (reads the
  whole `client.exe` into a `byte[]`, `ClientVersion.cs:92-94`), `Packer` atlas construction.
- **On demand / off the frame thread**: `Exstentions.RaiseAsync` (`Extensions.cs:57,65`) marshals the handler
  onto a thread-pool `Task`; `LogFile.WriteAsync` (`Logging/LogFile.cs:88`). `Logger.SetLogger` writes to
  `Console` under a lock and `LogFile.Write` calls `logStream.Flush()` on **every** line (`LogFile.cs:80`) —
  a synchronous disk flush wherever `Log.*` is called, including the frame thread (74 external files use `Log.`).
- **Static initialisation order**: `Native` (`:43`), `ZLib` (`:45`), `Profiler` (`:52`), `PlatformHelper`
  (field inits) all run lazily on first touch. `ZLib`'s static ctor reads `PlatformHelper.IsWindows`, so
  touching `ZLib` forces `PlatformHelper`.

## Inbound

- `ClassicUO.Client` — everything. Notably `GameController.cs:468-580` (Profiler), the `Game/GameObjects/*`
  pool fields, `Game/Pathfinder.cs` (ObjectPool), `Game/UI/Controls/*` (implements `ITextEditHandler`, drives
  `TextEdit`), `Game/Managers/*` (`Log`, `JsonHelper`, `FileSystemHelper`, `StringHelper`), `Network/*`
  (`ZLib`, `UInt16Converter`, `MathHelper.Combine`), `Configuration/*` (`Crypter` for saved passwords,
  `JsonHelper.SaveAndBackup`), `LegionScripting/*` (`RegexHelper`, `StringHelper`).
- `ClassicUO.Assets` — `TextFileParser` for `.def` files, `ZLib` for UOP block decompression, `Log`,
  `FileSystemHelper`, `BwtDecompress`, `HuesHelper`.
- `ClassicUO.Renderer` — `HuesHelper`, `StbRectPackSharp.Packer` (font/texture atlas), `Log`, `MathHelper`.
- `ClassicUO.IO` — `ZLib`, `Log`.
- Entry points by surface area: `Log.*` (74 files), `StringHelper.*` (21), `HuesHelper` (14),
  `MathHelper.*` (14), `FileSystemHelper` (13), `ValueStringBuilder` (11), `UInt16Converter` (10),
  `Deque<>` (9), `QueuedPool<>` (8), `PlatformHelper` (8), `TextFileParser` (7), `ZLib.` (6).
- Unreferenced outside this project: `UnsafeMemoryManager` (0), `OrderedDictionary<>` (0),
  `ReadOnlyArrayView<>` (0), `Bag<>` (1).

## Outbound

Nothing in this partition calls into `ClassicUO.Client`, `.Assets`, `.Renderer` or `.IO`. External calls are:

- **FNA / XNA**: `Microsoft.Xna.Framework.Point`, `Vector2`, `Rectangle`, `Color` in `MathHelper.cs`,
  `Extensions.cs`, `HuesHelper.cs`, `StbRectPack/Packer.cs`.
- **SDL2**: `SDL.SDL_HasClipboardText`/`SDL_GetClipboardText` in `StringHelper.cs:320-322`;
  `using SDL2` also present unused in `Platforms/Native.cs:35`.
- **P/Invoke**: `kernel32!LoadLibrary/GetProcAddress/FreeLibrary` (`Platforms/Native.cs:80-87`);
  `libdl!dlopen/dlsym/dlclose/dlerror` (`:112-122`); `zlib!compress/compress2/uncompress/zlibVersion`
  (`ZLib.cs:152-165`); `libz!...` (`ZLib.cs:216-229`).
- **BCL**: `Marshal.AllocHGlobal/FreeHGlobal` (`CRuntime.cs:15,23`, `UnsafeMemoryManager.cs:72,127`),
  `ArrayPool<char>.Shared` (`ValueStringBuilder.cs:37,355,363,374`), `ArrayPool<byte>.Shared`
  (`LogFile.cs:65,84,90,109`), `System.Text.Json` (`JsonHelper.cs`), `DeflateStream`
  (`ZLIBStream.cs:312,324`), `Process.Start` (`PlatformHelper.cs:60,64,68`), `Console.*` (`Logger.cs`),
  `Task.Run` (`Extensions.cs:61,69`).
- **Upward call**: `Extensions.Catch` calls `Log.Panic` (`Extensions.cs:83`) and
  `GetAllCharacterPaths` calls `Log.Error` (`:286`); `JsonHelper`, `PlatformHelper`, `Profiler` also call
  `Log.*` — all within this partition.

## Hazards

- `AverageOverTime.cs:43` — eviction predicate is `(currentTicks - ts) > (currentTicks - _timeWindow.TotalMilliseconds)`.
  The left side is an age (small); the right side is roughly `currentTicks` (large). The condition is false for
  every realistic tick value, so nothing is ever dequeued: `_values` and `_sum` grow without bound for the
  lifetime of the holder. One instance exists per `GameObject` (`GameObject.cs:56,141`).
- `AverageOverTime.cs:43` — `currentTicks - _values.Peek().Timestamp` is `uint` arithmetic; it wraps rather
  than going negative if a stale timestamp is newer than `currentTicks`.
- `Collections/Bag.cs:104-110` — `Count = 0;` executes before `Array.Clear(_items, 0, Count)`, so the clear
  length is always 0 and no element reference is ever released, defeating the stated purpose of the
  `!_isPrimitive` branch.
- `Collections/Bag.cs:117,140` — `element.Equals(...)` throws if `element` is null for a reference `T`.
- `Collections/Deque.cs:168-172` — `Clear()` resets `_offset`/`Count` but leaves every slot of `_buffer`
  populated; the deque keeps references to removed items alive.
- `Collections/Deque.cs:321-338` — `DoRemoveFromBack`/`DoRemoveFromFront` do not null the vacated slot either.
- `Collections/Deque.cs:848-856` — `GetEnumerator` is an iterator that snapshots `Count` once and re-reads
  `DoGetItem(i)` each step; there is no version stamp, so mutation during a `foreach` silently yields shifted
  or stale elements rather than throwing.
- `Collections/Deque.cs:630-643` — `GetAt`/`Front`/`Back` hand out a `ref T` into `_buffer`; any subsequent
  growth (`Capacity` setter, `:150-155`) replaces `_buffer`, leaving the held `ref` pointing at the old array.
- `Collections/ReadOnlyArrayView.cs:97-116` — `Enumerator` starts `_currentIndex` at `_start` and `Current`
  reads `_items[_currentIndex]` before any `MoveNext`; the first `MoveNext` increments, so element `[0]` is
  skipped in a `foreach`. With `Count == 0` the `MoveNext` guard `_currentIndex != _start + Count - 1`
  evaluates `start != start-1` → true, and the enumerator reads past the intended window.
- `Collections/FastList.cs:20,25` — `Buffer` and `Length` are public mutable fields with the documented
  contract "do not change"; nothing enforces it, and a stale `Buffer` reference survives `Add`'s
  `Array.Resize` (`:71`).
- `Profiler.cs:82-95` — `m_AllFrameData` is a linear list searched with a nested loop over `m_ThisFrameData`;
  a new `ProfileData` is appended for every distinct context path and never removed, so the list grows with
  the number of unique nesting paths and `BeginFrame` cost is O(frames × paths).
- `Profiler.cs:201` — `LastTime => m_LastTimes[m_LastIndex % 60]`, but `AddNewHitLength` (`:239-243`) writes at
  `m_LastIndex % 60` and *then* increments, so `LastTime` reads the slot 60 samples old, not the last one.
- `Profiler.cs:190` + `:174,185` — `ProfileData.Empty` is a shared mutable static returned to callers;
  `AddNewHitLength` on it would corrupt every future miss.
- `Profiler.cs:135-140` — `ExitContext` silently returns when the top of `m_Context` does not match, unless
  `errorNotInContext` is passed; an unbalanced `EnterContext` leaves the stack permanently deeper.
- `Profiler.cs:155,170` — `InContext`/`GetContext` are not `[Conditional("DEBUG")]` while every mutator is,
  so in Release they read a stack that nothing ever pushes to.
- `QueuedPool.cs:79-85` — `ReturnOne` pushes unconditionally with no `MaxSize` check and no duplicate check;
  returning the same instance twice puts it in the pool twice, and the stack can exceed `MaxSize` without
  bound. `Remains => MaxSize - _pool.Count` (`:59`) goes negative once that happens.
- `QueuedPool.cs:61-77` / `ObjectPool.cs:23-36` — neither pool is thread-safe (`Stack<T>` `Push`/`Pop`
  unsynchronised), yet every instance is a `static readonly` shared field and `Extensions.RaiseAsync`
  (`Extensions.cs:57`) puts handler code on the thread pool.
- `QueuedPool.cs:74` — objects are handed out with whatever field values they were returned with; only the
  optional `_on_pickup` hook resets them. A serial or index left on a pooled `Item`/`Mobile` is visible to the
  next owner of that instance.
- `ObjectPool.cs:34-35` — when `_pool.Count >= MaxCapacity` the object is dropped after `_onReturn` already
  mutated it; `_onReturn` is invoked even on the discard path (`:33`).
- `RegexHelper.cs:15` — `GetOrAdd(pattern, ...)` keys only on the pattern string; a second call with different
  `RegexOptions` returns the regex built with the *first* call's options. The cache is unbounded and never
  evicted, and `RegexOptions.Compiled` is forced on (`:12-13`), so each distinct pattern permanently emits IL.
- `Logging/Log.cs:70,75,80,85,90,95,100,105,110,115` — every method dereferences `_logger` with no null check.
  `Log.Stop()` sets `_logger = null` (`:54`), so any log call after shutdown throws `NullReferenceException`.
  `Log.Resume`/`Log.Pause` (`:59,64`) have the same exposure before `Start`.
- `Logging/Logger.cs:143-144` — `_logTypesInfo[type]` has no entry for `LogTypes.Table` (`LogTypes.cs:47`,
  value `0x30`); `SetLogger` with that type throws `KeyNotFoundException` after passing the
  `(LogTypes & type) == type` guard.
- `Logging/Logger.cs:126` — the filter is `(LogTypes & type) == type`; because `Table = 0x30 = Error|Panic`,
  enabling `Error|Panic` also enables `Table`.
- `Logging/Log.cs:93-96` — `Panic` logs as `LogTypes.Error`, so `LogTypes.Panic` is never emitted and
  filtering panics separately is impossible.
- `Logging/LogFile.cs:65,78` — the buffer is rented at `message.Length` chars but filled with
  `Encoding.UTF8.GetBytes`, which produces more than one byte per char for any non-ASCII input
  (`ArgumentException` from `GetBytes`), and the write length is `message.Length`, not the byte count
  returned by `GetBytes` — multi-byte text writes a truncated line.
- `Logging/LogFile.cs:80` — `logStream.Flush()` after every line; the stream was opened `useAsync: true`
  (`:53`) yet `Write` is synchronous.
- `Logging/LogFile.cs:48` — filename uses `hh` (12-hour) with no AM/PM marker, so two log files 12 hours apart
  in the same day collide; the stream is `FileMode.Append` so they interleave.
- `Logging/Logger.cs:73-76` — `Start(LogFile logFile = null)` ignores its parameter entirely; `LogFile` is
  never wired to `Logger`, so `Log.Start(types, logFile)` writes only to the console.
- `ValueStringBuilder.cs:95-100` — `ToString()` calls `Dispose()`, which returns the pooled array and sets
  `this = default`. Call sites that follow `ToString()` with an explicit `Dispose()`
  (`StringHelper.cs:67,194,229,273,298,391`, `StbTextedit/TextEdit.cs:148,150`) are calling it twice; safe only
  because `_arrayToReturnToPool` is already null. Any use of the builder after `ToString()` writes into a
  zero-length span.
- `ValueStringBuilder.cs:326-333` — `Remove` reassigns `_chars = _chars.Slice(...)`, desynchronising the span
  from `_arrayToReturnToPool`. A later `Grow` (`:355-364`) copies only the sliced view and `Capacity`
  (`:53`) silently shrinks; the full array is still what gets returned to the pool.
- `ValueStringBuilder.cs:295-306` — `Replace(char,char)` searches all of `_chars` (including the region past
  `_pos`, i.e. pooled garbage) and replaces only the first match despite its name.
- `ValueStringBuilder.cs:37` — `ArrayPool<char>.Shared.Rent` returns arrays *larger* than requested and does not
  zero them; `RawChars` (`:103`) exposes that uninitialised tail.
- `StringHelper.cs:178,206,285,361` — `stackalloc char[str.Length]` sized from caller-supplied text (item
  names, cliloc strings, clipboard content). No length cap; a long string overflows the stack.
- `StringHelper.cs:402` — `UnsafeCompare` loops `i < length && i < str.Length` and returns `true` when `str`
  runs out first, so `"ab"` matches a 5-char buffer starting `"ab..."`.
- `StbTextedit/TextEdit.cs:135,168` — `DeleteChars`/`InsertChars` rebuild the whole backing string through
  `Handler.Text` on every edit; `Handler.Text` is a client-owned property that may re-layout, so a single
  keystroke can trigger a full re-measure.
- `StbTextedit/TextEdit.cs:922` — `s.undo_char[...] = (sbyte) text[u.where + i];` casts the char through
  `sbyte`; any code point above 0x7F is sign-extended/truncated and the undo restores a different character.
  `Redo` at `:982` does not cast, so the two paths disagree.
- `StbTextedit/TextEdit.cs:247` — `text[i + r.num_chars - 1]` is indexed without bounds check after the row
  loop; a handler returning a `num_chars` past the end of `Text` throws.
- `StbTextedit/TextEdit.cs:364` — `IsWordBoundary` reads `text[idx]` with no upper-bound check; `MoveToNextWord`
  (`:384-400`) bounds by `Length`, but `Length` comes from the handler and can disagree with `text.Length`.
- `StbTextedit/TextEdit.cs:289-315` — `Clamp()` is the only reconciliation between the cursor/selection indices
  and the handler's current text length. Any handler-side mutation of `Text` between input events leaves
  `CursorIndex`/`SelectStart`/`SelectEnd` pointing into the old string until the next call that clamps.
- `StbTextedit/UndoState.cs:181,183,192,194` — `int` values are cast to `short` before being stored into `int`
  fields (`insert_length`, `delete_length`, `char_storage`, `undo_char_point`); inserts longer than 32767
  wrap negative.
- `StbTextedit/UndoState.cs:100` — `DiscardRedo` reads `undo_rec[k]` (k = 98) unconditionally rather than
  `undo_rec[redo_point]`.
- `StbTextedit/UndoState.cs:41,44` — `new int[999]` + `new UndoRecord[99]` are allocated eagerly per `TextEdit`,
  i.e. per text-input control on screen.
- `StbRectPack/Packer.cs:12,41-44` — `_context` is a `readonly` field of mutable struct type; `_context.Dispose()`
  operates on a defensive copy, so `all_nodes`/`extra` are freed but the field's pointers stay non-null. A second
  `Dispose()` frees the same `AllocHGlobal` blocks again.
- `StbRectPack/Packer.cs:19-39` — `stbrp_context(num_nodes)` mallocs, then `stbrp_init_target` is called inside
  `fixed`; if the constructor throws after `malloc` (`StbRectPack.cs:33,36`) the blocks leak. No finalizer.
- `StbRectPack/CRuntime.cs:71-85` — `qsortInternal` is unbounded recursion with a first-element pivot;
  already-sorted input degrades to O(n²) depth and can overflow the stack.
- `StbRectPack/CRuntime.cs:15` — `Marshal.AllocHGlobal((int)size)` silently truncates a 64-bit size.
- `StbRectPack.Generated.cs:293,300` — "not packed" is signalled by writing `x = y = 0xffff` into the rect and
  then testing for that exact pair; a rect legitimately placed at (65535,65535) is indistinguishable.
- `BwtDecompress.cs:86` — `input.Slice(0, 1024)` throws `ArgumentOutOfRangeException` for any input shorter
  than 1024 bytes; there is no length guard.
- `BwtDecompress.cs:20-21` — `header` is read and discarded, `len` is initialised to 0 and never assigned
  before being passed to `InternalDecompress`, so the declared length is always inferred from the frequency
  table (`:92-95`) and the `sum != len` integrity check (`:97`) can never fire.
- `BwtDecompress.cs:25` — `Span<ushort> table = new ushort[65536]` is a heap allocation per call; `BuildTable`
  then allocates a second 128KB array via `table.ToArray()` (`:71`) to sort it.
- `ClientVersion.cs:109` — `buffer.AsSpan(i, 30)` inside a loop over the whole file; the last 29 iterations
  read past the end and throw `ArgumentOutOfRangeException`.
- `ClientVersion.cs:92-94` — the entire `client.exe` is read into one `byte[]` and `fs.Read` return value is
  ignored (a short read leaves the tail zeroed).
- `Crypter.cs:139-141` — the key is `Environment.MachineName`. Renaming the machine makes every stored
  password undecryptable, and `key[kidx]` is cast to `byte` so non-ASCII machine names lose information.
- `Crypter.cs:116-117` — the legacy branch sizes the buffer as `(byte)(source.Length >> 1)`, truncating for any
  input longer than 510 characters, then writes `buff[i >> 1]` for `i` up to `source.Length` — out of range.
- `Crypter.cs:99-104,125-130` — a malformed hex pair is swallowed by `catch { continue; }`, leaving that byte
  of `buff` as 0 and desynchronising `kidx` from the output position.
- `JsonHelper.cs:105` — `SaveAndBackup` returns `true` unconditionally, including after the `catch` at `:93`
  logged a failure; callers cannot tell that the save was lost.
- `JsonHelper.cs:66,90` — writes to `Path.GetTempFileName()` (system temp) then `File.Move`s onto the profile
  path; a cross-volume move throws, and the main file has already been moved to `.backup1` at `:87`, so the
  failure window leaves no file at `pathAndFile`.
- `JsonHelper.cs:27,51` — the two `[Obsolete]` methods use reflection-based `JsonSerializer` overloads, which
  the repo convention (generated serializer contexts, net472/`System.Text.Json` 8.0.5) forbids.
- `FileSystemHelper.cs:50-56` — `CreateFolderIfNotExists` mutates the caller's `parts` array in place while
  stripping invalid characters.
- `Extensions.cs:57-71` — `RaiseAsync` hands the handler to `Task.Run`, so subscriber code that assumes the
  frame thread runs on a pool thread; failures are only surfaced via `Catch` → `Log.Panic` (`:83`).
- `Extensions.cs:245-290` — `GetAllCharacterPaths` does three nested `Directory.GetDirectories` walks
  synchronously and resolves duplicate character names by suffixing `_1`, `_2`… (`:272-277`), so the returned
  key is not a stable identity across calls if the directory set changes.
- `Extensions.cs:232-237` — `FromHtmlHex` calls `Convert.ToInt32(hex, 16)` without try/catch; non-hex input
  throws rather than falling back to `Color.White`.
- `TextFileParser.cs:13` — `_sb` is a single reused `StringBuilder`; `ReadTokens` clears it only when
  `_sb.Length > 0` (`:144-154`), and `GetTokens` (`:163-178`) resets `_pos`/`_string`/`_Size` but not `_eol`
  or `_sb`, so state from a previous parse can leak into the first line of the next.
- `TextFileParser.cs:159` — `_pos = _eol + 1` unconditionally; when `_eol == _Size` this leaves `_pos` one past
  the end, which `IsEOF` (`:32`) handles but `GetTokens`'s `while (_pos < _Size)` also relies on.
- `ZLib/ZLIBStream.cs:281` — `ReadCRC` does `_RawStream.Seek(-4, SeekOrigin.End)` on the caller's stream and
  never restores the position. `ZLibManaged.Decompress` (`ZLibManaged.cs:52,66`) hands it a `MemoryStream` over
  a shared `byte[]` / an `UnmanagedMemoryStream` over a raw pointer; the seek moves that shared cursor and
  `ReadCRC` fires on every end-of-stream, including from `Close()` (`:218`).
- `ZLib/ZLIBStream.cs:149-155` — on a short read the Adler update is skipped entirely (`else` branch), so the
  checksum is computed only over reads that returned data; a partial final read is never mixed in.
- `ZLib/ZLIBStream.cs:298` — CRC mismatch throws a bare `Exception`.
- `ZLib/ZLibManaged.cs:56,72` — decompression is a `ReadByte()` loop, one virtual call and one Adler update per
  output byte.
- `ZLib.cs:47-61` — the managed fallback is selected only for 32-bit processes. On 64-bit the code hard-depends
  on a native `zlib`/`libz` export being resolvable; there is no try/catch around the `DllImport`.
- `ZLib.cs:66` — `Decompress(byte[] source, int offset, ...)` passes `source.Length - offset` as the source
  length but never applies `offset` to the source pointer, so the offset only shortens the length.
- `ZLib/Adler32.cs:64-79` — `Update(byte[],int,int)` computes `nextJToComputeModulus` from the entering `pend`
  but resets `pend = 0` in `UpdateModulus` (`:93`) while continuing to `pend++` per byte, so the deferred-modulus
  interval drifts after the first flush.
- `UnsafeMemoryManager.cs:56-66` — `Memset` writes `long`s and does `count /= 8`, so any tail of 1–7 bytes is
  left untouched; `Calloc` (`:77-84`) therefore does not fully zero non-multiple-of-8 allocations.
- `UnsafeMemoryManager.cs:86-93` — `Alloc(ref pool)` dereferences `pool.Free` with no null check; exhausting the
  pool dereferences null.
- `RandomHelper.cs:39` — one shared `System.Random` with no lock; concurrent `Next()` from a pool thread can
  leave the instance permanently returning 0.
- `Platforms/Native.cs:122` — `dlerror` is declared as `static extern string`, so the default marshaller frees
  the returned pointer with `Marshal.FreeCoTaskMem`; it is never called, but the declaration is live.
- `MathHelper.cs:109-112` — `Hypotenuse` uses `Math.Pow(a,2)` rather than `a*a`.
- `UInt16Converter.cs:51-53` — `uint.TryParse` failure is ignored and 0 is returned; an unparsable server value
  silently becomes hue/id 0.
- `ObjectPool.cs:1` / `RegexHelper.cs:4` / `ByteFlagHelper.cs:1` — file-scoped namespaces, unlike every other
  file in the partition.

## Fork deltas

Files with no ClassicUO BSD licence header are the ones added by TazUO or this fork; the stock files all carry
the 2021 andreakarasho block.

Almost certainly TazUO/Holiday additions:

- `ObjectPool.cs` — no header, file-scoped namespace, `MaxCapacity` cap that `QueuedPool` lacks. Used only by
  the rewritten `Pathfinder` (`Pathfinder.cs:130,1114,1184`).
- `RegexHelper.cs` — no header, file-scoped namespace, `ConcurrentDictionary`. Serves the LegionScripting /
  Python scripting layer.
- `ByteFlagHelper.cs` — no header, file-scoped namespace.
- `JsonHelper.cs` — no header. The `SaveAndBackup`/`Load` + 3-deep backup rotation and the `JsonTypeInfo`
  parameters match this repo's "every JSON serialize/deserialize needs a generated serializer context" rule.
  The `[Obsolete]` `LoadJsonFile`/`SaveJsonFile` are the older TazUO API kept for compatibility.
- `Easings.cs` — no header, credited to Kryzarel. UI animation, not present upstream.
- `AverageOverTime.cs` — no header. Consumed by `GameObject` only.
- `TextFileParser.cs` — carries `// SPDX-License-Identifier: BSD-2-Clause` instead of the andreakarasho block,
  i.e. it was rewritten. The quote-pair handling (`TryGetQuotePair`, `:73`) and `GetTokens` (`:163`) look
  reworked relative to the classic CUO parser.

Fork edits inside otherwise-stock files:

- `FileSystemHelper.ReadAllTextShared` (`FileSystemHelper.cs:119-132`) — added with an explanatory comment
  about two clients sharing an install; opens `FileShare.ReadWrite | FileShare.Delete`.
- `StringHelper.GetPluralAdjustedString` (`StringHelper.cs:343-350`) — the null/empty guard has a fork-style
  comment ("Item data names can legitimately be null or empty… Reached while adding items to a container").
- `Extensions.cs` — `ToHtmlHex`/`FromHtmlHex` (`:224-237`) and `GetAllCharacterPaths` (`:245-290`) are
  additions; the latter encodes this fork's `ProfilesPath/Account/Server/Character` layout.
- `Profiler.ExitContext(string, bool errorNotInContext = false)` (`Profiler.cs:128,137`) — the optional
  suppress-error parameter is not stock; upstream logs unconditionally.
- `ClientVersion.cs:81` — `CV_7010400` ("new file format") is a late addition to the enum.
- `ZLib.cs:74-79` — the `ReadOnlySpan`/`Span` `Decompress` overload is a modernisation over the
  `byte[]`/`IntPtr` pair.
- `StringHelper.Cp1252ToString(ReadOnlySpan<byte>)` (`:56`) and `StringToCp1252Bytes` (`:46`) use
  `ValueStringBuilder`/spans rather than the older `Encoding` path.

Vendored third-party, unmodified apart from the licence header being pasted on:

- `StbRectPack/*` — Sichem-generated from stb_rect_pack, `StbRectPack.Generated.cs:1` timestamps it 2/18/2021.
- `StbTextedit/*` — StbTextEditSharp port of stb_textedit; the `#region license` blocks were added by CUO.
- `ZLib/*` (`Adler32`, `ZLIBStream`, `ZLibHeader`) — `ZLibNative` namespace.
- `ValueStringBuilder.cs:9` — cites LibHac.
- `Collections/Deque.cs`, `Collections/OrderedDictionary.cs` — Nito.Deque and an ordered-dictionary gist.
- `Collections/FastList.cs` — Nez `FastList`, no licence header.
