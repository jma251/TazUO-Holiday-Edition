# Everything on `dev` since the fork

Every change this fork has made to TazUO 4.5.23, measured as the **net diff**
from the fork base `c5b59c873` to `dev` — not the commit list. 218 commits
produced a lot of churn, reverts and re-dos; what follows is what is actually
in the client today.

**107 files: 41 added, 54 modified, 12 deleted. +12,423 / −4,332.**
2026-08-09 to 2026-09-13.

Reference checkouts are now remotes, so all four can be read here directly:

| remote | what |
| --- | --- |
| `cuo/main` | ClassicUO — the original |
| `taz/legacy` | TazUO 4.7.2 — what this fork came from |
| `taz/main` | TazUO modern (net10.0, 5.24.x) |
| `mw/main` | MW Edition — where the ported helpers in section E came from |
| `servuo/master` | ServUO — the server side |

MW also carries `mw/release/0.2` and `mw/release/0.21`, which are the tagged
states the port was read out of.

The column that matters for the cleanup is **Verdict**, and it is a proposal,
not a decision.

---

## A. Gameplay behaviour — the ones that can change how the game plays

Highest scrutiny. Everything here alters what the client does with what the
server said.

| Change | Files | On release? | Verdict |
| --- | --- | --- | --- |
| **Cull `+1`** - keep items and mobiles one tile past the granted view range | `World.cs` (2 sites) | **no** | **CUT.** CUO and Taz both cull at exactly `ClientViewRange`. ServUO settled why: an item is sent on the one step it crosses into range and never again, so the extra tile buys no second chance - it only holds objects the server has stopped maintaining. Suspected cause of mobiles drawn after death and the invisible dragon. |
| **Mobile step timing** - `stepTime /= Steps.Count` | `Mobile.cs` (1 site) | **YES** | **CUT.** Stacks on top of TazUO's own "teleport effect" patch, still in the file directly above it. The two compound; a 5-deep queue plays back 5x fast. Likely cause of monsters jumping while chasing. **This one is live in the public 4.5.2302.** |
| **Chunk cell repair** - hand the cell to a neighbour before unlinking | `GameObject.cs`, `Chunk.cs` | yes | **KEEP.** Real bug: the chunk kept one object per cell as its way in and never reassigned it, so removing that object detached every other object on the tile - alive in `World.Items`, drawn by nobody. |
| **Object pooling** - `ReturnToPool` split out of `Destroy` | `Item.cs`, `Mobile.cs`, `World.cs` | yes | **KEEP, re-read.** Fixes a real aliasing bug. Exists in neither CUO nor Taz, so it is ours to own. |
| **Login view range** - adopting the asked-for range before the server answered | `PacketHandlers.cs`, `World.cs` | yes | **CUT.** Ours, not upstream's. CUO and Taz send `ClientViewRange` and never write to it, so they cull at 40 until the server's `0xC8` reply lands; this fork assigned the slider value (24) at the moment of asking, 1,541 packets before the answer. `World.Clear()` now also resets to 40, which nothing did - only the first login of a process ever started at the ceiling. |
| **House keep range** split from view range | `World.cs`, `Item.cs`, `HouseManager.cs` | yes | **KEEP.** ServUO does the same thing: `BaseMulti.GetUpdateRange` is the client's range plus half the building's longest side, while loose items use the plain range. This mirrors the server rather than inventing something. |
| **Info bar answers a target cursor** | `InfoBarGump.cs` | yes | **KEEP.** Shipped in 4.5.2302 deliberately. |
| Pathfinder, TargetManager, drawing-sort `LastDrawnTime` | 3 files | yes | **DECIDE** - small, mostly diagnostic hooks. |

---

## B. Empty house

| Change | Files | On release? | Verdict |
| --- | --- | --- | --- |
| **Contents recovery** - count the room, resync on a shortfall | `HouseContentsRecovery.cs` (+238) | yes | **KEEP.** Confirmed working by hand. ServUO explains why it is the *only* lever: `0x22` makes the server run `SendEverything` and ignore the crossing-step geometry entirely. |
| **House diagnostics** | `HouseDiagnostics.cs` (1,019 lines) | yes, **18 `HOLIDAY_DEV` refs** | **GUT from release.** Compiled out already, but the lines are still in the release tree. Per the new branch model, remove rather than gate. |
| House let-go / reacquire bookkeeping | `HouseManager.cs` (+76) | yes | **KEEP** - the recovery depends on it. |

---

## C. Network

| Change | Files | On release? | Verdict |
| --- | --- | --- | --- |
| Socket hardening - reconnect teardown, connect timeout, idle polling | `AsyncNetClient.cs` (+157/-50) | yes | **KEEP, but fix.** `OnConnected` fires inside the connect try/catch, so a login-handler fault reports as a socket error and tears down a live connection. See `candidate-fixes.md` #2. |
| Resync on a malformed packet instead of spinning | `PacketHandlers.cs` | yes | **KEEP.** |
| Packet pump | `GameController.cs` | yes | **FIX.** `MAX_PACKETS_PER_FRAME` counts socket *reads*, not packets, so it bounds nothing. Inherited from CUO and Taz, still wrong. |
| `Plugin.Tick` guard, `0x19` null guard, step direction mask | 3 files | **no** | **KEEP** - ported from TazUO main this session, untested. |

---

## D. Music and audio — ~1,700 lines, almost all ours, and most of it ships

| Change | Files | On release? | Verdict |
| --- | --- | --- | --- |
| **Region music map** - play what the area calls for when the server sends none | `MusicMapManager.cs`, `AudioManager.cs` (+647) | **YES, ungated** | **DECIDE.** `MusicMapMode` defaults to `0` (off), so it ships dormant. Largest single feature we added and nobody decided it should ship. |
| **Music overlay** - on-screen panel showing what is playing and why | `MusicInfoGump.cs` (+222) | **YES, ungated, with a player-facing checkbox in Experimental** | **DECIDE.** Defaults off, but this is investigation scaffolding with a switch in the release options menu. |
| **Music diagnostics** | `MusicDiagnostics.cs` (388) | yes, **20 `HOLIDAY_DEV` refs** | **GUT from release.** |
| Sound loader / `Sound.cs` / `UOMusic.cs` | +265 net | yes | **DECIDE** - some fixes, some support the map. |

---

## E. MW's ported work — ~2,500 lines, all switched off, all dev-only

17 `Auto*Manager`, the scheduler and coordinator, the bandage set, `ToastGump`,
`MobileCache`, `ProfileDataStore`, `SkillReader`, `EmergencyHealManager`,
`PoisonCureManager`, plus the "MW's Work" options page.

**Verified: none of it is on `release`.** Nothing reachable at runtime on either
branch.

**Verdict: stays on `dev`.** A parts bin for future work - but 38 files of
unreachable code in every dev build.

---

## F. Spells and casting

| Change | Files | On release? | Verdict |
| --- | --- | --- | --- |
| Real cast times, Faster Casting applied, server decides disturbance | `DefaultSpellIndicatorConfig.json` (+2,701/-2,026), `SpellVisualRangeManager.cs`, `PlayerMobile.cs` | **YES** | **DECIDE.** Largest data change in the fork, four rewrites as theories changed, and it is already in the public build. Worth re-reading end to end. |

---

## G. Diagnostics and logging

| Change | Files | On release? | Verdict |
| --- | --- | --- | --- |
| Log writer fixes - truncated lines, colliding names, unwritten crash logs | `Logger.cs`, `LogFile.cs` | yes | **KEEP** - real fixes to existing code. |
| Feature diagnostics | `FeatureDiagnostics.cs` (179) | **no** | dev-only already. **KEEP on dev.** |
| Crash recovery - rolling backups of `profile.json` / `gumps.xml` | `CrashRecoveryManager.cs` | **no** | dev-only already. **DECIDE** - writes files, and does file I/O in the frame loop. |

---

## H. UI

`ModernOptionsGump.cs` (+451) is mostly the MW page and the Experimental
entries. `LoginGump` version lines moved left so a dev stamp fits.
`NameOverheadGump`, `Control`, `TextBox`, `ResizableJournal` - small,
uninvestigated. **Mostly KEEP.** The two Experimental sliders (view range, house
range) are tuning dials and are already gated off release.

---

## I. Inherited-bug fixes — ported from upstream

`StaticFilters` concurrent access (PR 780), `Chunk` null `Node` guard (PR 835),
item mouse selection `IndexOutOfRangeException` (#656), animation and font
loader fixes. **KEEP all** - upstream's own fixes re-applied.

---

## J. Removals

Discord client and SDK (14 files) and anonymous login metrics. **KEEP REMOVED** -
both called deliberately. Neither removal has reached `release`.

---

## K. Infrastructure

Version scheme, build stamp, `v.txt` (now marks dev builds), settings/profile/
language plumbing, `CommandManager`, `EventSink`, 3 test files. **KEEP.**

---

## What changed in this chart since the first version

Three rows were wrong and are corrected above:

- **The cull `+1` is not on `release`.** It was listed as needing a revert on
  both. Only `dev` has it.
- **The diagnostics are already `HOLIDAY_DEV`-gated on `release`**, not merely
  "cut from release" as pending work. They compile out; what remains is to
  remove the lines from the release tree entirely.
- **The music map and its overlay ship on `release`, ungated**, with a
  player-facing checkbox. The first chart implied all music work was dev-side.

## What to do first

1. **Cull `+1`** - dev only, two lines, live symptoms.
2. **Step timing** - both branches eventually; it is in the public build.
3. **OPL linear scan** - the burst stutter. See `candidate-fixes.md` #1.

---

## Audit of the MW automation port (2026-09-14)

Thirty-odd files under `Game/Managers`, none of which exist on `release`. Reached
from the **Automation** page of the modern options gump, polled at 5 Hz by
`AutomationScheduler.Tick()` from `GameScene.Update()`, behind a master switch
that is `profile.AutomationEnabled` (off by default).

### Fixed

| Fault | Where | What it did |
| --- | --- | --- |
| Auto-stealth never subscribed to movement | `AutoStealthManager.cs` | The options toggle assigns `Enabled` directly, so `EnsureHooked` - reachable only from `SetEnabled`, which nothing calls - never ran. `_lastMoveAt` stayed 0, the idle test read "has not moved since the client started", and the helper used Hiding every twelve seconds **while the player was running**. `Tick` now hooks, and treats an unseen `_lastMoveAt` as "standing still from now". |
| Six helpers never reset between characters | `EmergencyHeal`, `PoisonCure`, `AutoCurePotion`, `AutoHealPotion`, `AutoRefreshPotion`, `AutoBuff` | They were the only entries the scheduler's `ResetSession` did not call, and nothing persists them. Switching one on for one character left it armed for the next - helpers that cast and drink, armed by somebody else's choice. Each now has `ResetForProfile()` and the scheduler calls it. |

### Known and unfixed - decisions, not defects

- **Nine more helpers leave `Enabled` set across a character switch**: `AfkReply`,
  `AutoCloseEmptyCorpse`, `AutoMount`, `AutoOpenBackpack`, `AutoOpenPaperdoll`,
  `AutoRearm`, `AutoSayThanks`, `AutoStealth`, `AutoStopOnDeath`. The other half
  of the set resets to off. The inconsistency is the bug; which way to resolve it
  is a choice. Resetting all is safe and loses the setting on every logout;
  persisting all to the profile is what the master switch already does and is the
  better end state.
- **`AutoBandage` and `PetBandage` switch themselves on** once the server sends a
  Healing/Veterinary value of 40 or more, if the player has never expressed a
  preference. `ManualOverride` suppresses it and persists in `bandage.tsv`. MW's
  design, ported intact.
- **`AutomationCoordinator.Enabled` is written `= true`** in its field
  initialiser. Inert - `GameScene.Load` calls `ResetSession` before `Update` ever
  ticks - but it is the wrong default to write down for a master automation
  switch.

### Unreachable

- **`AfkReplyManager` and `AutoSayThanksManager`** have no options toggle and no
  command. Their `EnsureHooked` is reachable only from `SetEnabled`, which nothing
  calls, so neither ever subscribes to `EventSink`. 181 lines that cannot run.
- **`AutoFollowManager`** is armed only by `Set(serial)`, which nothing calls.
- **`AutoBuffManager`'s toggle is inert**: `Arm(buff, spell)` is the only thing
  that sets what to watch, and nothing calls it, so `Tick` always returns at the
  `Watch == -1` guard. The checkbox does nothing.
- **`AutoHitListManager`'s toggle is inert** unless `autohit.tsv` is written by
  hand: `AddPattern` has no caller.
- `AutoMountManager.Pick` and `AutoRearmManager.Pick` also have no callers, but
  both helpers learn their serials in `Tick`, so they work without it.
- `AutoHitListManager.FilePath` is a private property with no reader, duplicating
  the `FILENAME` constant.

### Looked at and sound

`FeatureDiagnostics` (lock-protected, volatile snapshot for lock-free reads,
bounded failure window, marshals its print to the main thread), `ProfileDataStore`
(temp file, `Flush(true)`, `File.Replace` with a backup, every path caught),
`AutoCloseEmptyCorpse` (snapshots the gump list before disposing), the
coordinator's lease arithmetic, and every helper that acts without taking the
lease - all four only call `GameActions.Print`, which is local text.

`SkillReader` caches a failed lookup as `-1` for the life of the process, which
would silently stop the bandage auto-enable forever. Not reachable: `SkillsLoader`
is a lazy singleton loaded by `UOFileManager` at startup, long before any
`GameScene` tick. Recorded rather than changed.

`CrashRecoveryManager` copies `profile.json` and `gumps.xml` into
`crash_recovery/` every five minutes and keeps six of each. It is live on `dev`
and absent from `release`, so it writes nothing on a player's machine.
