# legionscripting

Partition root: `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/LegionScripting`
27 files, 12,190 lines, all read in full.

Two scripting engines live here side by side:

* **LScript** (`namespace LScript` + `ClassicUO.LegionScripting.Commands/Expressions`) — a
  Steam/Razor-style interpreted language. Runs **on the frame thread**, one AST statement per
  frame, driven by `LegionScripting.OnUpdate()`.
* **Python** (IronPython, `API.cs` + `PyClasses/`) — each script gets its own `Thread`, its own
  `ScriptEngine`/`ScriptScope`/`API` instance, and marshals every game touch back to the frame
  thread through `MainThreadQueue`.

---

## Files

| Path (relative to partition root) | Lines | Purpose |
| --- | --- | --- |
| `API.cs` | 3539 | The entire Python-facing surface (`class API`). ~200 methods, each wrapping a `MainThreadQueue.InvokeOnMainThread` call. Also gump-construction helpers, callback queue, shared vars, ignore list, per-script journal queue. |
| `Commands.cs` | 961 | LScript command handlers (`attack`, `moveitem`, `cast`, `waitforgump`, …). Signature `bool(string, Argument[], bool quiet, bool force)`; `false` = "do not advance, retry next frame". |
| `Constants.cs` | 10 | `LASTITEMINHAND`, `LASTMOUNT`, `FOUND` alias names; `MAX_SERIAL = 2147483647` (the "any" sentinel). |
| `Expressions.cs` | 502 | LScript expression handlers (`findtype`, `hits`, `injournal`, `skill`, …). Return `IComparable`. |
| `Interpreter.cs` | 1594 | LScript runtime: `Script` (AST walker, scopes, journal, goto stack), `Argument`, `Scope`, `TypeConverter`, and the **static** `Interpreter` (aliases, lists, timers, handler registries, `_activeScript`). |
| `LScriptSettings.cs` | 11 | POCO persisted to `Data/lscript.json`: global autostart list, per-char autostart map, group-collapsed map. |
| `LegionScripting.cs` | 891 | Partition entry point. `Init`/`Unload`/`OnUpdate`, script discovery from disk, autostart, python thread launch/abort, `ScriptFile` type, `ScriptStarted/StoppedEvent`. |
| `Lexer.cs` | 635 | LScript lexer/parser → `ASTNode` tree. `ASTNodeType` enum, `SyntaxError`. |
| `PersistentVars.cs` | 285 | Tab-separated key/value store at `Data/legionvars.dat`, scoped Char/Account/Server/Global. Async debounced whole-file rewrite. |
| `PyClasses/Buff.cs` | 24 | Immutable snapshot of a `BuffIcon`. |
| `PyClasses/PyControl.cs` | 72 | Stub type; exists only so the doc generator emits `Control` helper docs. All methods return null. |
| `PyClasses/PyEntity.cs` | 75 | Serial-keyed wrapper over `Entity`; re-resolves through `World.Get` when the cached ref goes stale. Implicit `uint` conversion to serial. |
| `PyClasses/PyGameObject.cs` | 115 | Base wrapper: snapshots X/Y/Z/Graphic/Hue at construction, keeps a raw `GameObject` ref for `Impassible` / `HasLineOfSightFrom`. |
| `PyClasses/PyItem.cs` | 42 | `PyEntity` + Amount/IsCorpse/Opened/Container, re-resolved from `World.Items`. |
| `PyClasses/PyLand.cs` | 23 | Wrapper over `Land`. |
| `PyClasses/PyMobile.cs` | 56 | `PyEntity` + hits/mana/stam/dead/poisoned, re-resolved from `World.Mobiles`. |
| `PyClasses/PyMulti.cs` | 23 | Wrapper over `Multi`. |
| `PyClasses/PyProfile.cs` | 35 | Static read-only view of a handful of `ProfileManager.CurrentProfile` fields. |
| `PyClasses/PyStatic.cs` | 30 | Wrapper over `Static` + IsImpassible/IsVegetation. |
| `ScriptBrowser.cs` | 634 | Gump that browses `github.com/PlayTazUO/PublicLegionScripts` over the GitHub contents API, plus `GitHubContentCache` (10 min TTL, `WebClient`-based). |
| `ScriptEditor.cs` | 99 | Resizable in-client text editor for one `ScriptFile`; Save writes the file and reloads LScript. |
| `ScriptManagerGump.cs` | 652 | The main script list UI: group/subgroup tree (`GroupControl`), per-script row (`ScriptControl`) with play/stop, edit, autostart, macro-button creation, delete. |
| `ScriptRecorder.cs` | 585 | Singleton action recorder + Python code generator. Fed by call sites all over `Game/` and `Network/`. |
| `ScriptRecordingGump.cs` | 578 | UI over `ScriptRecorder`: record/pause/clear, reorder/delete rows, copy or save generated `.py`. |
| `ScriptingInfoGump.cs` | 200 | Live key/value panel ("Last Object", "Last Gump Opened", …) fed by `AddOrUpdateInfo` from game code; click a value to copy to clipboard. |
| `TextParser.cs` | 178 | Quote-aware tokenizer used by the lexer (carried over from ClassicUO's config parser). |
| `Utility.cs` | 341 | `FindItems` (the workhorse world scan), `ContentsCount`, layer/direction string maps, python-ignore-aware nearest scans, hex→Color. |

---

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `static LegionScripting` | LegionScripting.cs:22 | Owns loaded/running script lists, registers all LScript handlers, drives per-frame execution, starts/aborts python threads. |
| `ScriptFile` | LegionScripting.cs:747 | One script on disk. Holds path/group/subgroup, file contents, the compiled `Script` (LScript) **or** the `Thread`+`ScriptEngine`+`ScriptScope`+`API` (Python). |
| `ScriptInfoEvent` | LegionScripting.cs:731 | Payload for `ScriptStartedEvent`/`ScriptStoppedEvent`. |
| `enum ScriptType` | LegionScripting.cs:741 | `LegionScript` \| `Python`, decided by `.py` extension. |
| `LScriptSettings` | LScriptSettings.cs:5 | Serialized settings POCO. |
| `class Script` | Interpreter.cs:263 | Per-script LScript execution state: current statement, scope chain, 50-entry journal, ignore list, goto return stack, pause/timeout state. |
| `static Interpreter` | Interpreter.cs:1195 | Global LScript registries + the single `_activeScript` pointer + pause/timeout primitives. |
| `class Argument` | Interpreter.cs:124 | Lazily-typed AST token; `AsInt/AsUInt/AsSerial/AsString` resolve scope vars then global aliases. |
| `class Scope` | Interpreter.cs:90 | Linked-parent variable namespace, pushed per if/while/for block. |
| `RunTimeError` | Interpreter.cs:12 | Thrown out of command/expression handlers; caught in `OnUpdate` and kills the script. |
| `enum ExecutionState` | Interpreter.cs:1188 | RUNNING / PAUSED / TIMING_OUT. |
| `class ASTNode` | Lexer.cs:80 | Immutable node with `LinkedList` children; `Next()`/`Prev()` give sibling traversal, which is how the interpreter advances/rewinds. |
| `static Lexer` | Lexer.cs:138 | `Lex(string[])` and `Lex(fname)` → AST. |
| `SyntaxError` | Lexer.cs:7 | Parse error carrying line text + number. |
| `internal TextParser` | TextParser.cs:27 | Quote-aware token splitter. |
| `static Commands` | Commands.cs:14 | 60+ LScript command handlers. |
| `static Expressions` | Expressions.cs:15 | ~50 LScript expression handlers. |
| `static Utility` | Utility.cs:13 | World scanning + conversions shared by both engines. |
| `public class API` | API.cs:34 | The python object bound as builtin `API`. One instance per running python script. |
| `PyGameObject` / `PyEntity` / `PyItem` / `PyMobile` / `PyStatic` / `PyMulti` / `PyLand` | PyClasses/* | Python-safe wrappers; snapshot value fields, re-resolve live refs on the main thread. |
| `Buff` | PyClasses/Buff.cs:5 | Immutable buff snapshot. |
| `static PersistentVars` | PersistentVars.cs:12 | Cross-session variable store. |
| `ScriptManagerGump` (+ nested `GroupControl`, `ScriptControl`) | ScriptManagerGump.cs:18/216/448 | Script list UI. |
| `ScriptBrowser` (+ `ItemControl`, `GHFileObject`) | ScriptBrowser.cs:22/270/388 | Remote script browser. |
| `GitHubContentCache` | ScriptBrowser.cs:413 | Async HTTP + in-memory cache. |
| `ScriptEditor` | ScriptEditor.cs:12 | Text editor gump. |
| `ScriptingInfoGump` (+ `InfoEntry`) | ScriptingInfoGump.cs:16/139 | Live info panel. |
| `ScriptRecorder` / `RecordedAction` | ScriptRecorder.cs:14 / 564 | Recording singleton + one recorded action. |
| `ScriptRecordingGump` | ScriptRecordingGump.cs:16 | Recorder UI. |

---

## State

Mutable/static state owned by this partition:

**LegionScripting.cs**
* `ScriptPath` — LegionScripting.cs:24 (set in `Init`, `<exe>/LegionScripts`).
* `_enabled`, `_loaded` — :26. `_loaded` gates one-time handler registration; `_enabled` toggled by Init/Unload.
* `runningScripts` (List) — :28. Walked every frame.
* `removeRunningScripts` (List) — :29. Deferred-removal buffer, cleared each frame.
* `LoadedScripts` (List, public) — :32. Everything discovered on disk; mutated by the gump's Delete.
* `lScriptSettings` — :30.
* `ScriptStartedEvent` / `ScriptStoppedEvent` — :34-35. `ScriptControl` subscribes per row.
* `PyThreads` Dictionary<managedThreadId, ScriptFile> — :37.

**Interpreter.cs (all static, global across every LScript script)**
* `_aliases` Dictionary<string,uint> — :1198. Includes `found`, `lastgump`, `lasthanditem*`, `lastmount*`.
* `_lists` Dictionary<string,List<Argument>> — :1201.
* `_timers` Dictionary<string,DateTime> — :1204.
* `_exprHandlers` / `_commandHandlers` / `_aliasHandlers` — :1210 / :1214 / :1218.
* `_activeScript` — :1220. Single pointer; whatever ran last.
* `Culture` — :1226, forced `.`/`,` number format.

Per-`Script` state: `_statement`, `_scope`, `lineNodes` (Interpreter.cs:271), `returnPoints` Deque (:272), `ExecutionState` (:274), `PauseTimeout` (:276), `TargetRequested` (:278), `TimeoutCallback` (:280), `IgnoreList` (:282), `_journalEntries` capped at 50 (:293, :324).

**Lexer.cs**
* `_curLine` static int — :140.
* `_tfp` static `TextParser` instance — :225 (shared, stateful, reused for every parse).

**API.cs (per instance unless noted)**
* `gumps` ConcurrentBag<Gump> — :43. Gumps auto-closed on script stop.
* `scheduledCallbacks` Queue<Action> — :47, hard-capped at 100 (:56).
* `sharedVars` **static** ConcurrentDictionary — :48. Shared by every python script; never auto-cleared.
* `ignoreList` ConcurrentBag<uint> — :94.
* `journalEntries` ConcurrentQueue<JournalEntry> — :95, swapped via `Interlocked.Exchange` at :2302.
* `backpack`, `player` caches — :96-97.
* `Found` — :171.
* `PyProfile` **static** — :176.
* `Random` — :151.

**PersistentVars.cs**
* `_charScopeKey`/`_accountScopeKey`/`_serverScopeKey` — :18-20, set in `Load()`.
* `_data` nested dictionary — :27, guarded by `_fileLock` (:22).
* `_saveQueue` ConcurrentQueue — :23; `_saveTaskRunning` int flag — :24.

**ScriptRecorder.cs**
* `_instance` — :16, double-checked lock (:19-33).
* `_recordedActions` + `_actionsLock` — :35-36.
* `_isRecording`, `_isPaused`, `_startTime`, `_lastActionTime`, `_lastPlayerX/Y`, `_lastDirection` — :37-43.
* `RecordingStateChanged`, `ActionRecorded` events — :60-61.

**Gump statics**
* `ScriptManagerGump.RefreshContent` bool — ScriptManagerGump.cs:31 (polled in `SlowUpdate`).
* `ScriptManagerGump.lastX/lastY/lastWidth/lastHeight` — :28-29.
* `ScriptBrowser._mainThreadActions` **static** ConcurrentQueue — ScriptBrowser.cs:24.
* `ScriptingInfoGump.infoEntries` static Dictionary + `Instance` — ScriptingInfoGump.cs:21, :28.
* `ScriptRecordingGump._lastX/_lastY/_lastWidth/_lastHeight` — ScriptRecordingGump.cs:31-32.

---

## Timing

* **Per frame, on the frame thread**, in `GameController.Update` (GameController.cs:495):
  `Scene.Update()` → `UIManager.Update()` → **`LegionScripting.OnUpdate()`** → `MainThreadQueue.ProcessQueue()` (GameController.cs:498).
  So a python thread's `InvokeOnMainThread` result lands *after* LScript ran that frame.
* `LegionScripting.OnUpdate` (LegionScripting.cs:388) early-returns unless `_enabled && World.InGame`.
  For each running LScript it calls `Interpreter.ExecuteScript` → `Script.ExecuteNext()` — **exactly one
  statement per script per frame**. Commands returning `false` (e.g. `waitfortarget`, `waitforgump`)
  do not advance and are retried the next frame.
* Pause/timeout resolution is therefore also frame-quantised: `Interpreter.Pause(ms)` sets
  `PauseTimeout = UtcNow.Ticks + ms*10000` (Interpreter.cs:1548) and is only re-checked on the next
  `ExecuteScript` call (:1499-1508).
* Default timeouts: `waitforjournal` 10000 ms (Commands.cs:160), `waitfortarget` 10000 ms (:320),
  `waitforgump` 5000 ms (:621), `waitforprompt` 10000 ms (:854).
* `UIManager.SlowUpdate()` runs every **500 ms** (GameController.cs:500-503); `ScriptManagerGump.SlowUpdate`
  (ScriptManagerGump.cs:142) polls `RefreshContent` and rebuilds the whole gump on that tick.
* **Per packet / per event**: `EventSink.JournalEntryAdded` → `LegionScripting.EventSink_JournalEntryAdded`
  (:138) pushes the entry into every running LScript's 50-entry ring **and** every python script's
  unbounded `ConcurrentQueue`.
* **On player movement**: `PlayerMobile.cs:1427` calls `ScriptRecorder.UpdatePlayerPosition` each step.
* **On action**: `ScriptRecorder.Record*` is called inline from `GameActions`, `TargetManager`,
  `MacroManager`, `OutgoingPackets`, `PacketHandlers` (see Inbound).
* **On scene load / unload**: `PersistentVars.Load()` then `LegionScripting.Init()` (GameScene.cs:260-261);
  `PersistentVars.Unload()` then `LegionScripting.Unload()` (GameScene.cs:431-432).
* **Python threads**: free-running OS threads created at `PlayScript` (LegionScripting.cs:444). They block
  on `ManualResetEvent` inside `MainThreadQueue.InvokeOnMainThread<T>` until the next frame's
  `ProcessQueue`. `API.Pause` is a real `Thread.Sleep` (API.cs:2319).
* **Background tasks**: `Python.CreateEngine()` warm-up at `Init` (LegionScripting.cs:41);
  `PersistentVars` save task (PersistentVars.cs:221); `ScriptBrowser` GitHub fetches + a fire-and-forget
  pre-cache of the first 5 subdirectories (ScriptBrowser.cs:454-471), 30 s request timeout (:550),
  10 min cache TTL (:420); `DownloadAPIPy` (LegionScripting.cs:710).
* `ScriptBrowser.Update` drains at most **10** queued main-thread actions per frame (ScriptBrowser.cs:142).

---

## Inbound

* `Game/Scenes/GameScene.cs:261` → `LegionScripting.Init()`; `:432` → `LegionScripting.Unload()`.
* `GameController.cs:495` → `LegionScripting.OnUpdate()` every frame.
* `Game/GameActions.cs:105` → `UIManager.Add(new ScriptManagerGump())` (the user-facing entry).
* `ScriptRecorder.Instance.Record*` called from:
  `GameActions.cs` 408, 532, 606, 622, 676-694, 778, 891, 925, 938, 1043, 1059, 1107, 1130, 1226, 1249;
  `Game/GameObjects/PlayerMobile.cs:1427`; `Game/Managers/MacroManager.cs:1134,1145`;
  `Game/Managers/TargetManager.cs:351,586`; `Network/PacketHandlers.cs:6926`;
  `Network/OutgoingPackets.cs:3079-3085`.
* `ScriptingInfoGump.AddOrUpdateInfo` called from `GameActions.cs` 607, 624, 869, 939, 1044, 1060, 1108, 1131
  and `Network/PacketHandlers.cs:6927`.
* `EventSink.JournalEntryAdded` → `LegionScripting.EventSink_JournalEntryAdded` (subscribed LegionScripting.cs:48).
* `CommandManager` chat commands registered in `Init`: `playlscript`, `stoplscript`, `togglelscript`
  (LegionScripting.cs:58-135). `togglelscript <file>` is also what the generated macro button runs
  (ScriptManagerGump.cs:521).
* Python scripts call into `API` via the builtin `API` object (bound at LegionScripting.cs:881).

## Outbound

* `ClassicUO.Game.GameActions` — Print, Say, Attack, DoubleClick(+Queued), SingleClick, PickUp, DropItem,
  Equip, GrabItem, UseSkill, CastSpellByName, BandageSelf, Rename, ReplyGump, Logout, AllNames,
  ChangeStatLock, UsePrimary/SecondaryAbility.
* `ClassicUO.Game.World` — `Player`, `Items`, `Mobiles`, `Map`, `OPL`, `Party`, `HouseManager`,
  `MapIndex`, `Get`, `FindNearest`, `GetStaticOrMulti`, `LastObject`, `InGame`.
* Managers: `TargetManager` (Target/SetTargeting/CancelTarget/Reset/SetAutoTarget/NextAutoTarget/LastTargetInfo),
  `UIManager` (Add/GetGump/GetGumpServer/Gumps), `MessageManager` (HandleMessage/PromptData),
  `MacroManager`, `Pathfinder`, `CoolDownBarManager`, `AutoLootManager`, `MoveItemQueue`, `UseItemQueue`,
  `GlobalActionCooldown`, `TileMarkerManager`, `MainThreadQueue`, `ProfileManager`.
* Network: `AsyncNetClient.Socket.Send_*` (Commands.cs, API.TargetResource) **and**
  `NetClient.Socket.Send_*` (most of API.cs) — both are used.
* UI controls: `NineSliceGump`, `ResizableGump`, `ModernScrollArea`, `ScrollArea`, `VBox/HBoxContainer`,
  `DataBox`, `NiceButton`, `TextBox` (TTF), `TTFTextInputField`, `AlphaBlendControl`, `Checkbox`,
  `RadioButton`, `GumpPic`, `ResizableStaticPic`, `SimpleProgressBar`, `QuestArrowGump`, `WorldMapGump`,
  `PopupMenuGump`, `QuestionGump`, `InputRequest`, `MacroButtonGump`.
* Assets/renderer: `TrueTypeLoader.EMBEDDED_FONT`, `TileDataLoader.Instance.StaticData`.
* External: IronPython (`Python.CreateEngine`, `ScriptEngine/ScopeExceptionOperations`),
  `System.Text.Json` (raw, no source-gen context), `System.Net.WebClient`, `SDL2.SDL_SetClipboardText`,
  `System.Diagnostics.Process` (open file in external editor).

---

## Hazards

Factual observations with line numbers. No causes claimed.

**Collection mutated while being iterated**
* `LegionScripting.cs:393` — `foreach (ScriptFile script in runningScripts)` calls
  `Interpreter.ExecuteScript`, which can run `Commands.ToggleScript` (`Commands.cs:29-32`), which calls
  `LegionScripting.StopScript`/`PlayScript`, which do `runningScripts.Remove/Add` (`:486`, `:450`).
* `LegionScripting.cs:143` — `EventSink_JournalEntryAdded` iterates `runningScripts` from the packet path.
* `ScriptManagerGump.cs:537` — `LegionScripting.LoadedScripts.Remove(Script)` from a gump callback while
  `Commands.ToggleScript` (`Commands.cs:25`) and `API.ToggleScript/PlayScript/StopScript`
  (`API.cs:3282`, `:3307`, `:3328`) iterate the same list.
* `LegionScripting.cs:339-343` — `LoadLScriptSettings` iterates `CharAutoStartScripts` by index while
  calling `RemoveAll` on each value list.

**Identity reuse / stale references**
* `LegionScripting.cs:445` — `PyThreads.Add(script.PythonThread.ManagedThreadId, script)`. Managed thread
  IDs are reused after a thread dies; `Dictionary.Add` throws on duplicate key. Removal happens at `:500`
  (StopScript) but the stop for a finished thread is deferred through `MainThreadQueue.EnqueueAction` at `:478`.
* `PyClasses/PyEntity.cs:68` — cached `entity` is validated only by `entity.Serial == Serial`; nothing checks
  `IsDestroyed`. Same pattern `PyItem.cs:35`, `PyMobile.cs:47`.
* `PyClasses/PyGameObject.cs:85-89` — X/Y/Z/Graphic/Hue are copied at construction and never refreshed;
  `_gameObject` (`:114`) holds a raw `GameObject` reference for the wrapper's lifetime.
* `PyClasses/PyItem.cs:39` — `item = World.Items.TryGetValue(Serial, out item) ? item : null;` passes the
  field itself as the `out` argument.
* `API.cs:113` and `:128` — `backpack` and `player` are cached on first access for the life of the API
  instance.
* `Interpreter.cs:1198` — `_aliases` (including `found`, set by every `findtype`/`findalias`/`nearesthostile`
  at `Expressions.cs:107,134,292,366,464,494`) is global to all LScript scripts, not per-script.

**Null / index**
* `Interpreter.cs:1579` — `ClearTimeout()` dereferences `_activeScript` with no null check; it is called from
  `Script.Advance()` (`:931`) and from `Interpreter.Reset()` (`:1591`) *after* `_activeScript` was set to null
  at `:1589`.
* `Interpreter.cs:1545` (`Pause`), `:1555` (`Unpause`), `:1567` (`Timeout`) dereference `_activeScript` unguarded.
* `LegionScripting.cs:700` / `:705` — `LScriptError`/`LScriptWarning` read `Interpreter.ActiveScript.CurrentLine`;
  `ActiveScript` is null outside execution (set to null at `Interpreter.cs:1526`, `:1534`).
* `Commands.cs:84-86` — guard is `args.Length > 3` but the read is `args[4]`.
* `Commands.cs:16-19` — `ToggleScript` throws when `args.Length < 2` yet only reads `args[0]`; usage string
  documents one argument.
* `Commands.cs:82` — `World.Map.GetTile(x,y)` result `g` is dereferenced at `:88`/`:92` without a null check.
* `LegionScripting.cs:337` — if `JsonSerializer.Deserialize` returns null, the method `return`s at `:347`
  leaving `lScriptSettings` null.
* `LegionScripting.cs:780` — `cleanPath.Substring(cleanPath.IndexOf(cleanBasePath) + cleanBasePath.Length)`;
  `IndexOf` returning -1 yields a negative start index.
* `LegionScripting.cs:303` — `GetAccountCharName()` dereferences `ProfileManager.CurrentProfile` unguarded;
  reached from `AutoLoadEnabled`/`SetAutoPlay` called by gump code.

**Inverted / dead logic**
* `Commands.cs:385` — `if (!Interpreter.InIgnoreList(item)) continue;` inside `movetype`: items **not** on the
  ignore list are skipped.
* `Commands.cs:400-404` — in the no-hue branch, `return true` is outside the `if (GameActions.PickUp(...))`
  block, so the loop always returns on the first candidate.
* `Expressions.cs:319-322` — `IsDead` returns `true` when the requested serial is not found in `World.Mobiles`.
* `Lexer.cs:206-207` — `foreach (var l in line.Split(';')) ParseLine(node, line);` parses the whole `line` once
  per `;`-separated segment (the `string[]` overload at `:166-168` correctly uses `l`).
* `API.cs:2230` — `GetJournalEntries(double seconds, …)` computes its cutoff from a hard-coded
  `TimeSpan.FromSeconds(30)`, ignoring `seconds`.
* `ScriptBrowser.cs:185` vs `:291` — `.py` files are listed but only `.lscript` files get a `MouseDown`
  handler, so `.py` rows are inert.

**Shared mutable statics / reentrancy**
* `Lexer.cs:140` `_curLine` and `Lexer.cs:225` `_tfp` (a stateful `TextParser`) are static and reused by every
  `Lex` call, including `ScriptFile` construction from `ScriptBrowser`'s download callback (`ScriptBrowser.cs:329`).
* `TextParser.cs:161` — `_eol = _Size - 1` with the loop condition `_pos < _eol` at `:163`.
* `ScriptBrowser.cs:24` — `_mainThreadActions` is static; every `ScriptBrowser` instance drains the same queue,
  and it is only drained while some `ScriptBrowser.Update` runs (`:142`).
* `ScriptBrowser.cs:439-473, 481-489` — `directoryCache`, `fileContentCache`, `cacheTimestamps` are plain
  `Dictionary`s mutated from `Task.Run` continuations and from the background pre-cache task with no lock.
* `API.cs:48` — `sharedVars` is static across all python scripts and only cleared by an explicit
  `ClearSharedVars()` call.
* `ScriptRecorder.cs:120-130` — `_lastActionTime` is read/written outside `_actionsLock`, and `Time.Ticks`
  (frame-thread clock) is sampled from whatever thread calls `RecordAction`.

**Unbounded growth**
* `LegionScripting.cs:151` — python `JournalEntries.Enqueue` per journal line with no cap; only
  `API.ClearJournal` (`API.cs:2273`) removes entries. (LScript's ring is capped at 50, `Interpreter.cs:324`.)
* `Interpreter.cs:271` `lineNodes` is populated by `GenlineNodes` (`:414`) and never cleared, including on
  `UpdateScript` (`:411`) after an edit.
* `Interpreter.cs:1198-1204` — aliases, lists and timers persist for the whole session; only
  `ClearAllLists()` is called on `Unload` (`LegionScripting.cs:379`).
* `ScriptingInfoGump.cs:21` — `infoEntries` static dictionary keyed by fixed strings; grows only to the number
  of distinct keys but is never cleared between characters.

**Blocking / spinning off the frame thread**
* `MainThreadQueue.InvokeOnMainThread<T>` blocks the calling python thread on a `ManualResetEvent` until the
  next `MainThreadQueue.ProcessQueue()` (GameController.cs:498). It is used by nearly every `API` member.
* `API.cs:1161-1168`, `:1216-1223` (`Pathfind`/`PathfindEntity` wait loops), `:1451` (`WaitForTarget`),
  `:1513` (`RequestTarget`), `:1556` (`RequestAnyTarget`), `:2061` (`WaitForGump`) spin with no sleep,
  issuing a blocking `InvokeOnMainThread` per iteration.
* `API.cs:2314-2319` — `Pause` clamps at 2000 **seconds** then `Thread.Sleep`s.
* `LegionScripting.cs:501` — `script.PythonThread.Abort()`; a thread aborted while parked inside
  `InvokeOnMainThread<T>` leaves its action queued and its `ManualResetEvent` unset.
* `PersistentVars.cs:281` — `Unload()` does `Task.Run(ProcessSaveQueue).Wait()` on the frame thread during
  scene teardown.

**Ordering assumptions**
* `LegionScripting.cs:52-55` — `LoadScriptsFromFile()` must run before `LoadLScriptSettings()` (which prunes
  autostart names against `LoadedScripts`, `:342-345`) and before `AutoPlayGlobal`/`AutoPlayChar`.
* `LegionScripting.cs:291` — `AutoPlayChar` silently no-ops when `World.Player == null` at `Init` time.
* `LegionScripting.cs:839` — `ScriptFile.GenerateScript()` begins with `LegionScripting.StopScript(this)`,
  so recompiling (from the ctor at `:799`, `ReloadFromFile` at `:814`, or `PlayScript` at `:428`) stops a
  running script.
* `LegionScripting.cs:426-450` — `PlayScript` calls `GenerateScript()` (which stops the script and removes it
  from `runningScripts`) and then unconditionally `runningScripts.Add(script)` at `:450`.
* GameController.cs:495 vs :498 — LScript runs before `MainThreadQueue.ProcessQueue`, so a python script's
  queued mutation lands after that frame's LScript pass.
* `API.cs:2403` — `(ScanTypeObject)scanType` casts `API.ScanType` (declared `Hostile,Party,Followers,Objects,Mobiles`
  at `:182-189`) straight to the game's `ScanTypeObject`; the two enum orderings must match.

**Client-side state the server owns**
* `Commands.cs:484` / `API.cs:1823` — write `World.Player.Skills[i].Lock` locally without sending a packet
  (contrast `API.SetStatLock` at `:1860`, which sends via `GameActions.ChangeStatLock`).
* `Commands.cs:871`, `:892`, `API.cs:825` — clear `MessageManager.PromptData` client-side immediately after
  responding.
* `Commands.cs:604-606` / `API.cs:611` — `moveitemoffset` computes the drop Z from the client's map data.
* `Commands.cs:594`, `:606`, `:628`, `API.cs:1927`, `:1974` — all key off `World.Player.LastGumpID`, the
  client's single-slot memory of the last server gump.
* `Commands.cs:496` — `World.OPL.Contains(serial)` is used both as a test and as the request trigger; with
  `force` the command returns true regardless of whether properties arrived.
* `PyEntity.cs:62` — `SetHue` writes `e.Hue` directly on the live world entity.
* `PersistentVars.cs:212` — in-memory value is updated before the file write is queued; the async writer at
  `:250-266` drains the queue without using the items and rewrites the whole file from `_data`.

**Other**
* `API.cs:56-60` — when the callback queue exceeds 100, the **oldest** callback is dequeued and discarded and
  an error is printed once per discard.
* `API.cs:277-288` — `CloseGumps` breaks out after 1000 iterations.
* `API.cs:3199` — empty `catch (Exception ex)` swallowing dispose-callback errors.
* `ScriptRecordingGump.cs:559` — `OnResize` calls `BuildGump()` again, adding a second set of controls to the
  same gump.
* `ScriptRecordingGump.cs:36` — `_displayedActions` is a copy maintained in parallel with
  `ScriptRecorder._recordedActions`; they are kept in sync only through `OnActionRecorded`, `DeleteAction`,
  `MoveActionUp/Down` and the Clear button. A recorder started before the gump opened is not reflected.
* `LegionScripting.cs:58-135` — `CommandManager.Register` for `playlscript`/`stoplscript`/`togglelscript`
  runs inside `Init`, which runs on every scene load, unlike the `_loaded`-guarded block at `:44-50`.
* `ScriptManagerGump.cs:88-95` and `:142-154` — `Refresh()` disposes `this` and constructs a replacement gump;
  the new gump's ctor calls `LegionScripting.LoadScriptsFromFile()` (`:38`).
* `Interpreter.cs:1448` — `SetTimer` refuses to update a timer that already exists and has not expired.
* `Lexer.cs:614` — `for X to LIST` pushes `lexemes[2].Substring(0, len-2)` as the list name (strips two chars).

---

## Fork deltas

The whole `LegionScripting/` directory is **not** in stock ClassicUO — it is a TazUO addition. Within it,
the things that read as newer TazUO work layered on the original LScript port:

* **LScript core** (`Lexer.cs`, `Interpreter.cs`, `TextParser.cs`) is a port of the Steam/UOSteam-style
  language used by Razor Enhanced / ClassicAssist lineage; comments still say "The steam language …"
  (Lexer.cs:468, :498, :587). `TextParser.cs` keeps the original ClassicUO GPL license header (the only file
  here that does).
* **IronPython layer** (`API.cs`, `PyClasses/`, `ScriptFile.SetupPythonEngine/Scope`) is the later addition:
  per-script `Thread` + `ScriptEngine`, `MainThreadQueue` marshalling, `PythonList`/`PythonTuple` returns,
  `iplib` on the search path (LegionScripting.cs:868), `API.py` stub downloaded from the TazUO repo
  (LegionScripting.cs:716).
* Depends on TazUO-only client systems: `MainThreadQueue`, `MoveItemQueue`, `UseItemQueue`,
  `GlobalActionCooldown`, `AutoLootManager`, `CoolDownBarManager`, `TileMarkerManager`, `SpellBarManager`,
  `NineSliceGump`/`ModernScrollArea`/`ModernUIConstants`, `TTFTextInputField`, TTF `TextBox`/`RTLOptions`,
  `VBoxContainer`/`HBoxContainer`, `InputRequest`, `XmlGumpHandler`, `TargetManager.SetAutoTarget`/`NextAutoTarget`,
  `Entity.BackpackOrRootContainer`, `EventSink.JournalEntryAdded`.
* **Mixed net client**: `Commands.cs` and `API.TargetResource` use `AsyncNetClient.Socket`, while most of
  `API.cs` uses `NetClient.Socket` (e.g. API.cs:442, :818, :2081, :2378). Suggests a partial migration.
* `ScriptRecorder`/`ScriptRecordingGump` and `ScriptingInfoGump` are newer still — they required
  instrumentation calls to be sprinkled through `GameActions`, `TargetManager`, `MacroManager`,
  `PacketHandlers`, `OutgoingPackets`, `PlayerMobile`.
* `ScriptBrowser` + `GitHubContentCache` pull from `PlayTazUO/PublicLegionScripts`; `DownloadAPIPy`
  (LegionScripting.cs:716) pulls from the TazUO **`legacy`** branch URL — i.e. already pointed at this
  fork's branch layout.
* Repo convention breach: `JsonSerializer.Deserialize/Serialize` is used with no generated serializer
  context at `LegionScripting.cs:337`, `:362` and `ScriptBrowser.cs:507`.
* Modern C# used on the net472 target: collection expressions (`LegionScripting.cs:162`,
  `ScriptFile` ctor `:784`), `switch` expressions (`Commands.cs:707`), `or` patterns in case labels
  (`API.cs:1447`), file-scoped namespaces in `PyClasses/`, `new()` target-typed (`API.cs:43`).
* Nothing in this partition is named or gated on "Holiday"; it appears to be inherited TazUO code rather than
  fork-local work.
