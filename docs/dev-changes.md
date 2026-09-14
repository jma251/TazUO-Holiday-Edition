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
server said, which means it can be wrong in ways a player feels.

| Change | Files | Verdict |
| --- | --- | --- |
| **Cull `+1`** — keep items and mobiles one tile past the granted view range | `World.cs` | **CUT.** Both CUO and Taz cull at exactly `ClientViewRange`. Holding past the grant means the server stops maintaining the object: no movement, no death, no delete. Suspected cause of mobiles drawn after death and of the invisible dragon. |
| **Mobile step timing** — `stepTime /= Steps.Count` when a queue builds | `Mobile.cs` | **CUT.** Stacks on top of TazUO's existing "teleport effect" patch, which already bumps a backlogged mobile to the mounted speed table. The two compound; a 5-deep queue plays back 5× fast. Taz modern solves it properly by recording the *observed* interval per step (`TimeDiff`). Likely cause of monsters jumping while chasing. |
| **Chunk cell repair** — an object being removed handed the cell to a neighbour first | `GameObject.cs`, `Chunk.cs` | **KEEP.** Real bug: the chunk kept one object per cell as its way in and never reassigned it, so removing that object detached every other object on the tile — alive in `World.Items`, drawn by nobody. `Chunk.RemoveGameObject` had always done it correctly and was never called. |
| **Object pooling** (`ReturnToPool` split out of `Destroy`) | `Item.cs`, `Mobile.cs`, `World.cs` | **KEEP, but re-read.** Fixes a real aliasing bug — an object went on the reuse pile while still filed under its old serial. Exists in neither CUO nor Taz, so it is ours to own. |
| **House keep range** split from view range | `World.cs`, `Item.cs`, `HouseManager.cs` | **DECIDE.** Separates "how far a house is held" from "how far loose objects are held". Sound in principle; the default of 40 is untested against 24. |
| Pathfinder, TargetManager, drawing-sort `LastDrawnTime` | `Pathfinder.cs`, `TargetManager.cs`, `GameSceneDrawingSorting.cs` | **DECIDE** — small, mostly diagnostic hooks. |

---

## B. Empty house

| Change | Files | Verdict |
| --- | --- | --- |
| **Contents recovery** — count the room, resync on a shortfall | `HouseContentsRecovery.cs` (+238) | **KEEP.** Shipped, confirmed working by hand. The only lever against the server sending nothing on a corner-stair entry. |
| **House diagnostics** | `HouseDiagnostics.cs` (+1,075) | **CUT from release, keep on dev behind `HOLIDAY_DEV`.** The single largest file we added, and it is pure instrumentation. |
| House let-go / reacquire bookkeeping | `HouseManager.cs` (+76) | **KEEP** — the recovery depends on it. |

---

## C. Network

| Change | Files | Verdict |
| --- | --- | --- |
| Socket hardening — reconnect teardown, connect timeout, idle polling | `AsyncNetClient.cs` (+157/−50) | **DECIDE.** Real robustness work; needs reading against CUO before it ships. |
| Resync on a malformed packet instead of spinning | `PacketHandlers.cs` | **KEEP** — the alternative is a permanently stuck stream. |
| Packet pump | `GameController.cs` | **FIX.** `MAX_PACKETS_PER_FRAME` counts socket *reads*, not packets, so it bounds nothing. Same in CUO and Taz — inherited, not ours — but still wrong. |

---

## D. Music and audio — ~1,700 lines, almost all ours

| Change | Files | Verdict |
| --- | --- | --- |
| Region music map — play what the area calls for when the server sends none | `MusicMapManager.cs`, `AudioManager.cs` (+647) | **DECIDE.** Largest single feature we added and nobody asked whether it should ship. |
| Music diagnostics + on-screen panel | `MusicDiagnostics.cs` (+388), `MusicInfoGump.cs` (+222) | **CUT from release.** Investigation scaffolding. |
| Sound loader / `Sound.cs` / `UOMusic.cs` | +265 net | **DECIDE** — some are fixes, some support the map. |

---

## E. MW's ported work — ~2,500 lines, all switched off

17 `Auto*Manager` files, the scheduler and coordinator, the bandage set
(5 files), `ToastGump`, `MobileCache`, `ProfileDataStore`, `SkillReader`,
`EmergencyHealManager`, `PoisonCureManager`, plus the "MW's Work" options page
in `ModernOptionsGump.cs`.

**Verdict: stays on `dev`, never ships as-is.** None of it is reachable at
runtime today. It is a parts bin for future work, which is what it was meant
to be — but it is also 38 files of unreachable code sitting in every build.

---

## F. Spells and casting

| Change | Files | Verdict |
| --- | --- | --- |
| Real cast times for every spell, Faster Casting applied | `DefaultSpellIndicatorConfig.json` (+2,701/−2,026), `SpellVisualRangeManager.cs`, `PlayerMobile.cs` | **DECIDE.** Largest data change in the fork. Went through four rewrites as theories changed. Worth re-reading end to end before it ships. |

---

## G. Diagnostics and logging

| Change | Files | Verdict |
| --- | --- | --- |
| Log writer fixes — truncated lines, colliding names, unwritten crash logs | `Logger.cs`, `LogFile.cs` | **KEEP** — real fixes to existing code. |
| Feature diagnostics | `FeatureDiagnostics.cs` (+179) | **CUT from release.** |
| Crash recovery — rolling backups of `profile.json` / `gumps.xml` | `CrashRecoveryManager.cs` | **DECIDE.** Writes files on a player's machine and does file I/O in the frame loop. |

---

## H. UI

`ModernOptionsGump.cs` (+451) is mostly the MW page and the two experimental
sliders. `InfoBarGump` targeting **shipped in 4.5.2302**. `LoginGump` version
lines moved left so a dev build stamp fits. `NameOverheadGump`, `Control`,
`TextBox`, `ResizableJournal` — small, uninvestigated.

**Verdict: mostly KEEP**, but the two Experimental sliders (view range, house
range) are tuning dials and are already gated off release.

---

## I. Inherited-bug fixes — ported from upstream PRs

| Fix | File |
| --- | --- |
| `StaticFilters` — survive concurrent access from multiple clients (PR 780) | `StaticFilters.cs` |
| `Chunk` — guard null `Node` before dereferencing (PR 835) | `Chunk.cs` |
| Item mouse selection `IndexOutOfRangeException` (#656) | `GameSceneInputHandler.cs` |
| Animation / font loader fixes | `Animation.cs`, `AnimationsLoader.cs`, `TrueTypeLoader.cs` |

**Verdict: KEEP all.** These are upstream's own fixes re-applied here.

---

## J. Removals

| Removed | Files | Verdict |
| --- | --- | --- |
| Discord client and SDK | 14 | **KEEP REMOVED** — called deliberately. |
| Anonymous login metrics | 1 | **KEEP REMOVED.** |

---

## K. Infrastructure

Version scheme, build stamp, `v.txt`, settings/profile/language plumbing,
`CommandManager`, `EventSink`, 3 test files. **KEEP.**

---

## What to look at first

1. **Cull `+1`** — live symptoms, one line each, deviates from both upstreams.
2. **Step timing** — live symptom, deviates from both upstreams, and Taz modern
   has the correct mechanism to copy.
3. **Invisible mobiles** — the chunk-cell repair (section A) was the fix for
   exactly this symptom in August. It is still happening, so either that fix
   does not cover mobiles or there is a second path.
