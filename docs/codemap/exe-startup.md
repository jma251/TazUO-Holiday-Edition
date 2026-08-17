# exe-startup

Partition = `src/ClassicUO.Client/*.cs` (top level) + `Configuration/` + `Input/`.
23 files, 6350 lines, all read in full. Branch checked out: `claude/new-chat-session-0zuwmx` (a `legacy`-lineage branch; `net472`).

This partition is the process entry point, the FNA `Game` loop, the SDL event pump,
the two config stores (global `settings.json`, per-character `profile.json`), and the
static input state that the rest of the client polls.

---

## Files

| Path | Lines | Purpose |
| --- | --- | --- |
| `src/ClassicUO.Client/Main.cs` | 583 | `Bootstrap.Main` — STAThread entry; installs the crash handler, parses argv twice, loads/creates `settings.json`, validates UO dir + client version, `SetDllDirectory("x64")`, then `Client.Run()`. |
| `src/ClassicUO.Client/Client.cs` | 207 | `static Client` — holds `Version`/`Protocol`/`ClientPath`/`Game`; `Load()` loads all UO files and derives protocol flags from client version; `Run()` constructs the `GameController`, creates plugins, starts UoAssist. |
| `src/ClassicUO.Client/GameController.cs` | 1169 | The FNA `Game`. Owns window, graphics device, hue sampler textures, the renderer facades (Animations/Art/Gumps/Texmaps/Lights/MultiMaps/Sounds), `Scene`, `GameCursor`, `Audio`. `Update`/`Draw` per frame, `HandleSdlEvent` as SDL event filter. |
| `src/ClassicUO.Client/CUOEnviroment.cs` | 110 | Static process-wide flags: game thread, exe path, version, Holiday `BuildTag`, `IsUnix`, `Debug`, `IsOutlands`, `CurrentRefreshRate`. |
| `src/ClassicUO.Client/Time.cs` | 40 | Two static fields: `Time.Ticks` (uint ms) and `Time.Delta` (seconds). Written once per `Update`. |
| `src/ClassicUO.Client/ClientException.cs` | 49 | `InvalidClientVersion`, `InvalidClientDirectory`. |
| `src/ClassicUO.Client/HtmlCrashLogGen.cs` | 112 | Writes an HTML crash report to a temp file and opens the browser. Called from the unhandled-exception handler. |
| `src/ClassicUO.Client/DllMap.cs` | 278 | Entirely inside `#if !NETFRAMEWORK` — **dead code on this branch**. Native-library name mapping for .NET Core. |
| `Configuration/Settings.cs` | 212 | `Settings` (global `settings.json` model) + `SettingsJsonContext`. `GlobalSettings` static singleton, `Save()` round-trips through JSON to strip credentials. |
| `Configuration/ConfigurationResolver.cs` | 133 | Generic JSON `Load<T>` / `Save<T>` used by both Settings and Profile. Backslash-escaping regex on read; temp-file + delete + move on write. |
| `Configuration/Profile.cs` | 1262 | `Profile` — ~400 per-character settings, plus `Save`/`SaveGumps`/`ReadGumps` (gumps.xml persistence and reconstruction) and `.bak1/.bak2/.bak3` rotation. |
| `Configuration/ProfileManager.cs` | 133 | `CurrentProfile` / `ProfilePath` statics; `Load(server, user, char)`, `NewFromDefault()`, `UnLoadProfile()`. |
| `Configuration/Language.cs` | 715 | UI string table (`Language.Instance`), loaded from `Data/Language.json` in `Main` before anything else; shared-read + atomic-write for multi-client installs. |
| `Configuration/UISettings.cs` | 133 | Abstract base for gump-local JSON settings under `Data/UI/`; background `Preload()`; `ColorJsonConverter`. |
| `Configuration/Json/FNAPointJsonConverter.cs` | 131 | `Point2Converter` / `NullablePoint2Converter` — hand-rolled positional readers for XNA `Point`. |
| `Input/Mouse.cs` | 183 | Static mouse state: position (backbuffer-scaled), button flags, click times, drag offsets, controller-driven cursor warp. |
| `Input/Keyboard.cs` | 87 | Static `Alt`/`Shift`/`Ctrl`, derived from SDL keymod; `ClearModifiers()` / `Refresh()` for focus transitions. |
| `Input/Controller.cs` | 205 | Static gamepad button state (`ButtonStates` dictionary + per-button bools), chord queries, name formatting. |
| `Input/KeysTranslator.cs` | 365 | Keycode/keymod → display string, with a lazily-grown static dictionary. |
| `Input/InputEventArgs.cs` | 108 | `MouseEventArgs`, `MouseDoubleClickEventArgs`, `MouseWheelEventArgs`, `KeyboardEventArgs`. |
| `Input/MouseButtons.cs` | 45 | `MouseButtonType` enum — values deliberately equal SDL button numbers. |
| `Input/MouseEventType.cs` | 49 | `MouseEventType` enum. |
| `Input/KeyboardEventType.cs` | 41 | `KeyboardEventType` enum. |

---

## Types

| Type | file:line | Responsibility |
| --- | --- | --- |
| `Bootstrap` (static) | Main.cs:50 | `[STAThread] Main(string[])`; `ReadSettingsFromArgs` (Main.cs:286) handles ~40 switches. `SetDllDirectory` P/Invoke at Main.cs:52. |
| `Client` (static) | Client.cs:50 | `Version`, `Protocol` (ClientFlags), `ClientPath`, `Game`. `Run()` Client.cs:58, `Load()` Client.cs:99, `ShowErrorMessage` Client.cs:93 (SDL message box). |
| `GameController : Microsoft.Xna.Framework.Game` | GameController.cs:62 | Frame loop + SDL filter. `Initialize` :118, `LoadContent` :149, `UnloadContent` :236, `Update` :466, `Draw` :555, `BeginDraw` :592, `HandleSdlEvent` :619, `OnExiting` :1050. Window helpers :274-464. `SetScene` :300. `ProcessNetworkPackets` :138. |
| `CUOEnviroment` (static) | CUOEnviroment.cs:40 | Process flags; `BuildTag` :66 and `DisplayVersion` :74 are Holiday additions; `ExecutablePath` :104. |
| `Time` (static) | Time.cs:35 | `Ticks`, `Delta`. |
| `HtmlCrashLogGen` (static) | HtmlCrashLogGen.cs:7 | `Generate(stackTrace, title, description)`. |
| `Settings` (sealed) | Settings.cs:55 | Global settings model; `GlobalSettings` :58, `CustomSettingsFilepath` :59, `GetSettingsFilepath()` :176, `Save()` :192, `EnhancedPacketsEnabled` field :163. |
| `SettingsJsonContext` | Settings.cs:43 | Source-gen context; `RealDefault` :46 exists to work around escaping (ClassicUO issue 1663). |
| `ConfigurationResolver` (static) | ConfigurationResolver.cs:42 | `Load<T>` :44 (corrupt file → `.corrupt` copy + null), `Save<T>` :92. |
| `Profile` (sealed, public) | Profile.cs:83 | Per-character settings + gump persistence. `Save` :672, `SaveAsFile` :694, `CreateBackupRotation` :699, `SaveGumps` :737, `ReadGumps` :859. |
| `ProfileJsonContext` | Profile.cs:57 | Snake-case source-gen context; `DefaultToUse` :78. |
| `ProfileManager` (static, public) | ProfileManager.cs:40 | `CurrentProfile` :42, `ProfilePath` :43, `Load` :45, `NewFromDefault` :75, `ValidateFields` :96, `UnLoadProfile` :129. |
| `Language` (public) | Language.cs:10 | `Instance` :24, `Load()` :26, `ReadAllTextShared` :91, `WriteAtomic` :102. Nested string classes :121-714. |
| `UISettings` (abstract) | UISettings.cs:10 | `ReadJsonFile` :16, `Load<T>` :32, `Save<T>` :61, `Preload()` :77. |
| `ColorJsonConverter` | UISettings.cs:101 | `"R:G:B:A"` string form for XNA `Color`. |
| `Point2Converter` / `NullablePoint2Converter` | FNAPointJsonConverter.cs:9 / :71 | Positional `{X,Y}` reader — order-dependent, returns `Point.Zero` on any deviation. |
| `Mouse` (static) | Mouse.cs:40 | `MOUSE_DELAY_DOUBLE_CLICK = 350` :42; `ButtonPress` :45, `ButtonRelease` :80, `Update` :148. |
| `Keyboard` (static) | Keyboard.cs:37 | `ClearModifiers` :50, `OnKeyDown/Up` :57-59, `UpdateModifiers` :66, `Refresh` :81. |
| `Controller` (static) | Controller.cs:7 | `ButtonStates` :28, `OnButtonDown/Up` :30/:35, `AreButtonsPressed` :108, `PressedButtons` :134. |
| `KeysTranslator` (static) | KeysTranslator.cs:39 | `_keys` :41, `_mods` :282, `TryGetKey` :295. |

---

## State

Mutable/static state owned by this partition:

- `Settings.GlobalSettings` — Settings.cs:58. Initialised to `new Settings()` at type init, then **replaced** by `Main` (Main.cs:137 or :147). Written from arg parsing, from `SetRefreshRate` (GameController.cs:346), and from `UnloadContent` (window position, GameController.cs:241).
- `Settings.CustomSettingsFilepath` — Settings.cs:59. Set only by `-settings`.
- `ProfileManager.CurrentProfile` / `ProfilePath` — ProfileManager.cs:42-43. Null before login and after `UnLoadProfile()`.
- `Profile.GumpsVersion` — Profile.cs:573. A `static` on an otherwise per-instance serialized class.
- `Language.Instance` — Language.cs:24. Replaced wholesale by `Language.Load()`.
- `UISettings.preload` — UISettings.cs:14. `Dictionary<string,string>` filled on a background Task (:79) and mutated (`Remove`, :39) from the game thread.
- `CUOEnviroment.*` — CUOEnviroment.cs:42-52: `GameThread`, `DPIScaleFactor`, `NoSound`, `Args`, `Plugins`, `Debug`, `IsHighDPI`, `CurrentRefreshRate`, `SkipLoginScreen`, `IsOutlands`, `NoServerPing`. `CurrentRefreshRate` rewritten once per second (GameController.cs:511).
- `Time.Ticks` / `Time.Delta` — Time.cs:37-38. Written at GameController.cs:470-471 each `Update`; read everywhere.
- `Client.Version` / `Protocol` / `ClientPath` / `Game` — Client.cs:52-55. Set once in `Load`/`Run`. `Game` is assigned inside a `using` (Client.cs:66), so it points at a disposed object after `Run` returns.
- `GameController` instance state — `_hueSamplers[3]` :66, `_ignoreNextTextInput` :67, `_intervalFixedUpdate[2]` :68, `_totalElapsed/_currentFpsTime/_nextSlowUpdate` :69, `_totalFrames` :70, `_uoSpriteBatch` :71, `_suppressedDraw` :72, `_background` :73, `bufferRect` :74, `bgHueShader` (static) :76, `drawScene` :77, `FrameDelay[2]` :116, `_filter` :64 (must stay rooted — it is the SDL callback delegate).
- `Mouse` statics — Mouse.cs:112-146: `Position`, `L/R/MClickPosition`, `Last{Left,Mid,Right}ButtonClickTime`, `CancelDoubleClick`, `L/R/M/XButtonPressed`, `IsDragging`, `MouseInWindow`, `ControllerSensativity`.
- `Keyboard.Alt/Shift/Ctrl` — Keyboard.cs:41-43.
- `Controller.ButtonStates` + 14 bool properties — Controller.cs:9-28.
- `KeysTranslator._keys` — KeysTranslator.cs:41. Grown at runtime by `TryGetKey` (:314) for unseen keycodes.
- `SettingsJsonContext.RealDefault` / `ProfileJsonContext.DefaultToUse` — Settings.cs:46 / Profile.cs:78, lazily-created singletons.

---

## Timing

**Startup order** (Main.cs):
1. `CultureInfo.InvariantCulture` :59 → `Language.Load()` :60 → `Log.Start` :65 → name game thread :67-68 → install `AppDomain.UnhandledException` :70.
2. `ReadSettingsFromArgs(args)` **first pass** :113 (mutates the default `GlobalSettings` instance).
3. FNA/SDL env vars :117-124 (`FNA3D_BACKBUFFER_SCALE_NEAREST=1`, `FNA3D_OPENGL_FORCE_COMPATIBILITY_PROFILE=1`, mouse click-through hint, PATH += `Data/Plugins`).
4. If `settings.json` missing → `GlobalSettings.Save()` :133; then `ConfigurationResolver.Load<Settings>` :137; null → fresh `Settings` :147.
5. `IsOutlands = ShardType == 2` :151 → `ReadSettingsFromArgs(args)` **second pass** :153 → conditional save :157.
6. `SetDllDirectory(<exe>/x64)` :164 (non-Unix only).
7. Language default from OS :167-189; UO dir default/`FolderBrowserDialog` (blocking modal) :201-227; client-version validation :231-249.
8. `Client.Run()` :279.

`Client.Run` → `Client.Load()` (UOFileManager.Load, StaticFilters, BuffTable, ChairTable, UltimaLive.Enable, PacketsTable, EncryptionHelper) → `new GameController()` → `Plugin.Create` per plugin → `UoAssist.Start()` → `Game.Run()`.

FNA lifecycle: ctor (GameController.cs:79) → `Initialize` :118 (installs `SDL_SetEventFilter` :131) → `LoadContent` :149 (hue samplers, fonts, renderer facades, lights, cursor, audio, background texture, `loadResourceAssets.Wait(10000)` :230, `SetScene(new LoginScene())` :231, `DiscordManager.FromSavedToken()` :233).

**Per frame — `Update`** (GameController.cs:466), in order:
`Time.Ticks/Delta` ← gameTime → `Mouse.Update()` → `ProcessNetworkPackets()` (**max 25 packets/frame**, GameController.cs:136/141) → `Plugin.Tick()` → `Scene.Update()` (only if `drawScene`) → `UIManager.Update()` → `LegionScripting.OnUpdate()` → `MainThreadQueue.ProcessQueue()` → `UIManager.SlowUpdate()` **every 500 ms** (:500-504) → FPS counter (1000 ms window, :509) → frame pacing → `GameCursor.Update()` → `Audio.Update()` → `DiscordManager.Update()`.

**Per frame — `Draw`** (:555): Profiler frame boundary → clear black → tiled background with `bgHueShader` → `Scene.Draw` (if `drawScene`) → `UIManager.Draw` → clear `SelectedObject.HealthbarObject`/`SelectedContainer` (:580-581) → `GameCursor.Draw` → `Plugin.ProcessDrawCmdList`.

**Frame pacing numbers**:
- `IsFixedTimeStep = false`, `TargetElapsedTime = 1000/250 = 4 ms`, `InactiveSleepTime = 0` (GameController.cs:97-99).
- `SetRefreshRate` (:320): clamps to `Constants.MIN_FPS`..`MAX_FPS`; at MIN_FPS uses **80 ms** ("real UO is 12.5 fps", :335); `FrameDelay[0] = frameDelay`, `FrameDelay[1] = frameDelay >> 1`.
- `_intervalFixedUpdate[0] = frameDelay`, `_intervalFixedUpdate[1] = 217` ms (comment says "5 FPS"; 217 ms ≈ 4.6 fps) — index 1 chosen when `!IsActive && CurrentProfile.ReduceFPSWhenInactive` (:517-523).
- When under budget: `_suppressedDraw = true; SuppressDraw(); Thread.Sleep(1)` on the frame thread (:532-538). `BeginDraw` returns false while suppressed (:592).

**On input** — `HandleSdlEvent` (:619) is an `SDL_EventFilter`, so it runs at SDL pump time, not at a defined point in `Update`. It calls `Plugin.ProcessWndProc` first (:623) and returns early if the plugin consumed the event. It calls `Mouse.Update()` again on motion/wheel/button events (:781, :794, :843, :950) — i.e. Mouse position is refreshed more often than once per frame.
Double click: `Mouse.MOUSE_DELAY_DOUBLE_CLICK = 350 ms` (Mouse.cs:42); a consumed double-click writes the sentinel `0xFFFF_FFFF` into the last-click-time (:864) so the matching mouse-up is swallowed (:938).

**On demand / on timer elsewhere**: `Profile.Save` is debounced to 10 `Time.Ticks` ms (Profile.cs:674). `Profile.TitleBarUpdateInterval` default 1000 ms (Profile.cs:286). `Settings.ReconnectTime` floor 1000 ms when set from argv (Main.cs:432).

**Shutdown**: `OnExiting` :1050 disposes `Scene`; `UnloadContent` :236 begins Discord disconnect, records window position into `GlobalSettings`, stops music, `GlobalSettings.Save()`, `Plugin.OnClosing()`, disposes ~17 loader singletons and `World.Map`.

---

## Inbound

- OS/CLR → `Bootstrap.Main` (Main.cs:57).
- SDL → `GameController.HandleSdlEvent` (GameController.cs:619) via `SDL_SetEventFilter` (:131).
- FNA `Game` base → `Initialize`/`LoadContent`/`Update`/`Draw`/`BeginDraw`/`UnloadContent`/`OnExiting`.
- FNA window → `WindowOnClientSizeChanged` (:597), wired in the ctor (:92).
- Everything in the client reads `Time.Ticks`, `Mouse.*`, `Keyboard.Alt/Shift/Ctrl`, `Controller.*`, `ProfileManager.CurrentProfile`, `Settings.GlobalSettings`, `Client.Game`, `Client.Version`, `Client.Protocol`, `CUOEnviroment.*`.
- `Client.Game.SetScene(...)` is how scenes are swapped (login → game and back).
- `Client.Game.GetScene<T>()` (:294) is the generic scene accessor used throughout.
- Gumps call `UISettings.Load<T>/Save<T>`; `Language.Instance.*` is read by the options gumps.
- `ProfileManager.Load` is called from the login/character-selection flow; `Profile.ReadGumps`/`SaveGumps` by `World`/`UIManager` on world enter/exit.
- `GameController.UpdateBackgroundHueShader()` (:549) is a public static called from options UI.
- `Profile.ControllerMouseSensativity` (Profile.cs:630) writes straight into `Input.Mouse.ControllerSensativity`.

## Outbound

- `ClassicUO.Assets`: `UOFileManager.Load`, `HuesLoader`, `MapLoader.MapsLayouts`, and ~17 `*Loader.Instance.Dispose()` calls in `UnloadContent`.
- `ClassicUO.Renderer`: `UltimaBatcher2D`, `Fonts.Initialize`, `SolidColorTextureCache`, `ExternalImageLoader`, `Animations`/`Art`/`Gump`/`Texmap`/`Light`/`MultiMap`/`Sound`, `ShaderHueTranslator.GetHueVector`.
- `ClassicUO.Network`: `AsyncNetClient.Socket.TryDequeuePacket` + `Statistics`, `PacketHandlers.Handler.ParsePackets`, `PacketsTable.AdjustPacketSizeByVersion`, `PacketLogger.Default`, `EncryptionHelper`, `UltimaLive.Enable`.
- `ClassicUO.Game`: `World`, `UIManager` (Update/SlowUpdate/Draw/mouse+keyboard dispatch/`AnchorManager`/`SavePosition`/`GetGump<T>`), `Scene` subclasses, `GameCursor`, `AudioManager`, `SelectedObject`, `GameActions.Print`, `Plugin.*`, `DiscordManager`, `MainThreadQueue`, `LastCharacterManager`, `IgnoreManager.Initialize`, `HouseDiagnostics.Flush`, `SkillsGroupManager`, `AnonMetrics`, `StaticFilters`, `BuffTable`, `ChairTable`, `LightColors`, `Loader.GetBackgroundImage`, `UoAssist.Start`.
- `LegionScripting.LegionScripting.OnUpdate()` per frame; `LegionScripting.ScriptManagerGump` in `ReadGumps`.
- `ClassicUO.Utility`: `Log`, `Profiler`, `Crypter`, `ClientVersionHelper`, `FileSystemHelper`, `PlatformHelper.LaunchBrowser`, `SerialHelper`, `UOFilesOverrideMap`.
- SDL2 P/Invoke directly (window, event filter, mouse capture/warp, message box, clipboard-adjacent screenshot paths).
- `System.Windows.Forms` — `FolderBrowserDialog` (Main.cs:206) and `Clipboard.SetImage` (GameController.cs:1159). Windows-only, part of why this branch does not build on Linux.
- ~35 gump types constructed by name in `Profile.ReadGumps` (Profile.cs:913-1074, :1176-1225).

---

## Hazards

Factual observations with line numbers.

- **Main.cs:109** — `crashfile.WriteAsync(sb.ToString()).RunSynchronously()`. `RunSynchronously` on a task that was not created by the `Task` constructor throws `InvalidOperationException`; this is inside the unhandled-exception handler, after the HTML report has been written.
- **Main.cs:113 and Main.cs:153** — `ReadSettingsFromArgs` runs twice over the same argv. Side-effecting cases run twice: `Crypter.Encrypt(value)` (:333), `PacketLogger.Default.CreateFile()` (:529), `LastCharacterManager.OverrideLastCharacter` (:376), `Profiler.Enabled` (:411), `AnonMetrics.MetricsEnabled = false` (:577).
- **Main.cs:113 → Main.cs:137** — everything the first pass wrote into `Settings.GlobalSettings` is discarded when `ConfigurationResolver.Load` replaces the object reference at :137. Only the second pass survives (except `CustomSettingsFilepath`, `IsHighDPI`, `Debug`, `SkipLoginScreen`, `NoServerPing`, `UOFilesOverrideMap.OverrideFile`, which live outside the instance).
- **Main.cs:133** — `Settings.GlobalSettings.Save()` is called on the *default* instance before the file is read, so a missing settings file is written from defaults plus first-pass argv.
- **Main.cs:60 vs Main.cs:65** — `Language.Load()` runs before `Log.Start(LogTypes.All)`; the `Log.Error` inside `Language.Load` (Language.cs:66) fires before logging is initialised.
- **Main.cs:151 vs :463** — `CUOEnviroment.IsOutlands = ShardType == 2` at :151 overwrites the `-outlands` flag set by the first argv pass; the second pass at :153 re-applies it.
- **Main.cs:206** — `FolderBrowserDialog.ShowDialog()` is a blocking Win32 modal on the startup path.
- **Client.cs:66** — `using (Game = new GameController())`: `Client.Game` still references the disposed controller after `Run` returns.
- **GameController.cs:131 vs :231** — the SDL event filter is installed in `Initialize`, but `Scene` is not assigned until `SetScene` in `LoadContent`. `HandleSdlEvent` dereferences `Scene` unconditionally at :694, :710, :768, :785, :799, :857, :941, :962, :999, :1034 (no `?.`), unlike `UIManager.KeyboardFocusControl?.`.
- **GameController.cs:604 and :611** — `WindowOnClientSizeChanged` dereferences `ProfileManager.CurrentProfile` with no null check; the handler is wired in the constructor (:92), before any profile exists.
- **GameController.cs:420** — `SetWindowBorderless` dereferences `ProfileManager.CurrentProfile.GameWindowFullSize` with no null check.
- **GameController.cs:141** — packet drain is capped at `MAX_PACKETS_PER_FRAME = 25`; a burst larger than 25×fps per second backs up in the socket queue.
- **GameController.cs:230** — `loadResourceAssets.Wait(10000)` blocks the game thread for up to 10 s inside `LoadContent`.
- **GameController.cs:537** — `Thread.Sleep(1)` on the frame thread inside `Update` when the frame budget was not consumed.
- **GameController.cs:349** — `_intervalFixedUpdate[1] = 217` with the comment `// 5 FPS`; 217 ms is ~4.6 fps.
- **GameController.cs:517-523** — the inactive-FPS branch reads `ProfileManager.CurrentProfile` every frame; profile can become null between frames via `UnLoadProfile()` (the expression is null-guarded here, unlike :604).
- **GameController.cs:956 vs :992/:1019** — `SDL_CONTROLLERBUTTONDOWN` checks `IsActive && CurrentProfile != null && ControllerEnabled`; `CONTROLLERBUTTONUP` and `CONTROLLERAXISMOTION` check only `IsActive`, so button-up/axis are processed with the controller disabled or no profile loaded.
- **GameController.cs:966-976, :1003-1013** — controller stick clicks synthesise `SDL_MOUSEBUTTONDOWN/UP` via `SDL_PushEvent` from inside the event filter, re-entering the same filter later in the pump.
- **GameController.cs:685-699** — when `Plugin.ProcessHotkeys` returns false the client sets `_ignoreNextTextInput = true`, and the *next* `SDL_TEXTINPUT` is dropped (:748) regardless of which key produced it.
- **GameController.cs:66, :73** — `_hueSamplers[]` and `_background` textures are never disposed in `UnloadContent`.
- **GameController.cs:580-581** — `SelectedObject.HealthbarObject`/`SelectedContainer` are nulled inside `Draw`, between `UIManager.Draw` and the cursor draw; anything reading them later in the frame sees null.
- **GameController.cs:1071-1075, :1108-1110** — screenshot paths allocate a full-backbuffer `Color[]` and call `GetBackBufferData` synchronously on the frame thread.
- **Mouse.cs:171-173** — divides by `Client.Game.Window.ClientBounds.Width/Height`; a zero client size divides by zero.
- **Mouse.cs:175-179** — `ProfileManager.CurrentProfile.GlobalScaling` is dereferenced when `World.InGame` is true; guarded only by `World.InGame`, not by a profile null check.
- **Mouse.cs:148 / GameController.cs:474, :781, :794, :843, :950** — `Mouse.Update()` is called both once per frame and from the SDL event filter; `Position` can change several times within one frame, so two consumers in the same frame can read different positions.
- **Mouse.cs:160-167** — polls `GamePad.GetState` and issues `SDL_WarpMouseInWindow` from inside `Mouse.Update`, i.e. also from inside the SDL event filter.
- **Keyboard.cs:66-79** — modifier state is derived from the `mod` field of whatever key event last arrived, not from `SDL_GetModState`; `Refresh()` (:81) and `ClearModifiers()` (:50) are only invoked on focus gained/lost (GameController.cs:665, :673).
- **Controller.cs:28** — `ButtonStates` is a public mutable static `Dictionary` written from the SDL event filter (`SetButtonState` :42) and enumerated by LINQ in `PressedButtons` (:134) / `AreButtonsPressed` (:123) from other call sites.
- **KeysTranslator.cs:314** — `_keys.Add(key, sKey)` mutates a static `Dictionary` from `TryGetKey`, with no synchronisation and no bound on how many distinct keycodes get inserted.
- **UISettings.cs:79-97 vs :36-40** — `preload` is filled on a `Task.Factory.StartNew` background thread and read/`Remove`d from the game thread with no synchronisation.
- **UISettings.cs:67** — `File.Exists(savePath)` is tested against a *directory* path, so it is always false and `Directory.CreateDirectory` runs on every `Save`.
- **ConfigurationResolver.cs:112-115** — save does `File.Delete(file)` then `File.Move(temp, file)`: there is a window in which the settings/profile file does not exist. `tempFile` comes from `Path.GetTempFileName()` (system temp, :105), so the `Move` can be cross-volume.
- **ConfigurationResolver.cs:55-63** — the loaded text is passed through a backslash-doubling regex before deserialisation, altering any string value containing single backslashes.
- **Settings.cs:58** — `GlobalSettings` starts as a real instance, so a read before `Main` finishes returns silently-default settings rather than failing.
- **Settings.cs:163** — `EnhancedPacketsEnabled` is a public *field* whose value is computed from `File.Exists(Data/DISABLE_ENHANCED_PACKETS)` at construction; `Save()` (:195-196) round-trips through source-gen JSON, which does not carry fields, so the round-tripped object recomputes it.
- **Settings.cs:206** — `Save()` unconditionally blanks `ProfilesPath` on the copy being written, so a `-profilespath` value is never persisted.
- **Profile.cs:674** — `Time.Ticks - lastSave < 10` uses the frame clock, which is 0 until the first `Update`; also `uint` subtraction wraps.
- **Profile.cs:630** — `ControllerMouseSensativity` is a per-profile setting backed by the process-global `Input.Mouse.ControllerSensativity`; `UnLoadProfile()` does not restore it, so it persists across character switches.
- **Profile.cs:573** — `public static uint GumpsVersion` is a static member of a per-character serialized type.
- **Profile.cs:978, :986, :1223** — `World.Player.Serial` is dereferenced during `ReadGumps` with no null check.
- **Profile.cs:755-761** — `SaveGumps` enumerates `UIManager.Gumps` while building its own list; any disposal during enumeration mutates the source collection.
- **Profile.cs:775-778** — walks `item.Container` upward with `while (SerialHelper.IsItem(item.Container))`; a container cycle in client-side state does not terminate.
- **Profile.cs:1110-1152** — nested-gump resolution loops until no progress; `World.Get(parent)` decides whether a parent exists, so gumps whose parent has not yet arrived from the server are dropped with a warning (:1149).
- **Profile.cs:699-735** — backup rotation is `Delete .bak3 → Move .bak2→.bak3 → Move .bak1→.bak2 → Copy current→.bak1`, not transactional; an `IOException` mid-way leaves the rotation half-applied (logged, save proceeds).
- **FNAPointJsonConverter.cs:11-60, :73-121** — the `Point` readers are strictly positional (X then Y) and return `Point.Zero` for any other shape, including `null`, rather than signalling an error. `NullablePoint2Converter.Write` (:124) dereferences `value.Value` without a null check.
- **Language.cs:26-68** — `Load()` runs before `Log.Start`; it rewrites `Data/Language.json` whenever the serialized defaults differ from the file, which happens on every version bump for every client sharing the install.
- **HtmlCrashLogGen.cs:102** — `Path.GetTempFileName() + ".html"` creates a zero-byte temp file that is then orphaned, and launches a browser from inside the crash handler.
- **DllMap.cs:12** — the whole file is `#if !NETFRAMEWORK`, so it compiles to nothing on `net472`; native lookup relies solely on `SetDllDirectory` at Main.cs:164.

---

## Fork deltas

Likely TazUO/Holiday additions rather than stock ClassicUO:

**Holiday-specific (this fork):**
- `CUOEnviroment.BuildTag` (CUOEnviroment.cs:66) and `DisplayVersion` (:74) — read `AssemblyInformationalVersionAttribute` for the `v4.5.23-hN` release tag; comments explicitly describe the Holiday release workflow.
- Window title `"TazUO [Legacy] - {Version} - Holiday Edition"` (GameController.cs:94, :279-289) — both `#if DEV_BUILD` branches are identical.
- `Settings` music block (Settings.cs:102-126): `MusicEra`, `MusicMapMode`, `IgnoreServerStopMusic`, `MusicOverlay`, `LogMusicIndices` — global rather than per-profile, with commentary explaining why.
- `Settings.LogHouseDiagnostics` (:131), `KeepHouseContentsLoaded` (:138), `ClientViewRange` default **40** (:144) — house/view-range fork work; `HouseDiagnostics.Flush()` in `SetScene` (GameController.cs:305).
- `ConfigurationResolver.Load` corrupt-file handling (:69-89) — writes a `.corrupt` copy and returns null instead of throwing out of startup.
- `Main.cs:139-158` — the null-`GlobalSettings` guard and deferred save, with the explanatory comment.
- `Language.cs:26-116` — shared-read (`ReadAllTextShared`), atomic write (`WriteAtomic`), and "only rewrite when out of date" logic for multi-client installs.
- `Keyboard.ClearModifiers()` / `Refresh()` (Keyboard.cs:50, :81) plus the focus-gained/lost wiring (GameController.cs:662-676) — stuck-modifier fix.
- `Profile.CreateBackupRotation` (Profile.cs:699) `.bak1/.bak2/.bak3`.
- `Language.Experimental` strings for the above (Language.cs:467-479).

**TazUO (upstream of this fork, not stock ClassicUO):**
- `LegionScripting.OnUpdate()` in the frame loop (GameController.cs:495) and `GumpType.ScriptManager` (Profile.cs:1071).
- `DiscordManager` (GameController.cs:233, :238, :269, :544).
- `AnonMetrics` / `-nometrics` (Main.cs:576).
- `AsyncNetClient` + `MAX_PACKETS_PER_FRAME` batching (GameController.cs:136-147); stock ClassicUO drains differently.
- `MainThreadQueue.ProcessQueue()` (GameController.cs:498).
- Grid containers, modern paperdoll, improved buff bar, nearby-loot, spell bar, auto-loot/scavenger/sell/buy agents, cooldown bars, grid highlight, TTF font settings, global gump scaling, controller support — the bulk of `Profile.cs` and the `TazUO` section of `Language.cs` (:499-697).
- `Input/Controller.cs` in full, `Mouse.ControllerSensativity` + thumbstick cursor warp (Mouse.cs:146, :160-167), controller branches in `HandleSdlEvent` (:955-1044).
- `HtmlCrashLogGen` (whole file) and its use in the unhandled-exception handler (Main.cs:99).
- `Loader.GetBackgroundImage()` + tiled hued background and `UpdateBackgroundHueShader` (GameController.cs:226-228, :549-553, :566).
- `ExternalImageLoader` (GameController.cs:209-210, :230).
- `ClipboardScreenshot` + Ctrl+PrintScreen tooltip/control capture (GameController.cs:713-742, :1106).
- `UISettings` + `Data/UI/*.json` (whole file), `ColorJsonConverter`.
- `Settings.EnhancedPacketsEnabled` / `Data/DISABLE_ENHANCED_PACKETS` (Settings.cs:163-174).
- `Language` as a JSON-backed string table (whole file) — stock ClassicUO uses RESX `Res*` classes, which this partition still also uses (`ResGeneral`, `ResGumps`, `ResErrorMessages`).
- `Profile.ReadGumps` nested-gump (`parent` attribute) resolution pass (Profile.cs:862, :1109-1152).
