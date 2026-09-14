# What the TazUO forks are carrying

65 forks of `PlayTazUO/TazUO` scanned on 2026-09-14. Every branch was listed
with `ls-remote`, every SHA checked against upstream history, and the 237
branches holding commits that exist nowhere in `taz/main`, `taz/dev` or
`taz/legacy` were fetched and read.

Method note: the fork list cannot be enumerated from this session — the GitHub
API is scoped to the session's own repositories. The list was supplied by hand.
Plain git works for any public repo, so everything after that step is scriptable.

## The forks that actually contain work

| fork | unique commits | what it is |
| --- | --- | --- |
| `crameep/TazUO` | 206 | The most active fork by a distance. Controller overhaul, render scaling, autoloot, and a run of real performance work. |
| `Nesci28/TazUO` | 135 | Large, mostly its own direction. |
| `yuval-po/TazUO` | 90 | Scripting/Myra UI rework, tests, a lot of "CR fixes". |
| `birdinforest`, `CanGG` | 82 each | Same branches — weather and atmospheric effects. |
| `shedar/TazUO` | 46 | A headless client experiment plus some genuine race fixes. |
| `openuo-online`, `uu1001com` | 46 each | OpenUO — a separate project now. |
| `fuzzlecutter/uo1998` | 39 | An era-specific client. |
| `credzba/TazUO` | 21 | Reconnect and image-loader fixes. |

The rest carry a handful of commits each, mostly shard-specific tweaks.

---

## Confirmed present in this fork

Both verified against our own source, not assumed.

### 1. A LINQ scan over every item in the world, on every step

`PlayerMobile.cs:1500`, in the auto-open-doors check:

```csharp
if (World.Items.Values.Any(s => s.ItemData.IsDoor && s.X == x && s.Y == y && ...))
```

That walks the entire item dictionary once per step, for every step the player
takes, whenever "open doors while pathfinding" is on. In a busy area the
dictionary is thousands of entries.

`crameep` replaced it with a direct map-tile lookup — walk to the head of the
tile's object list and check only the objects standing on that one tile.

Commit `2e81f4240e`, "perf: Reduce per-frame and per-step overhead for smoother
movement". The same commit also caches the corpse snapshot in `World.cs`
instead of rebuilding the array whenever it is asked for.

**This is per-step cost during movement, which is where stutter is felt.**

### 2. A deferred item removal can delete what the server just placed

We have `World.ObjectToRemove` — a serial queued for client-side removal when
the player picks something up — and it is consumed a frame later in
`World.Update`.

We guard it in exactly one place, `PacketHandlers.cs:1787`, and only for the
item currently held by the cursor. If the server authoritatively places that
same serial through any other path before the deferred removal runs, the
removal is stale and takes the server's object with it.

`Oleh Romanovskyi` added `CancelPendingItemRemoval(serial)` and calls it from
every path where the server places an object — equip, item helpers, object
helpers — plus a unit test. Commit `f9afddf4c5`.

**This is an item vanishing for no reason the player can see.**

---

## Worth reading next, not yet verified against our code

| what | who | why it matters here |
| --- | --- | --- |
| Auto-reconnect gets stuck: stale packet buffer not cleared between attempts (`8b834ef6af`, `589cef418e`) | credzba | We rewrote reconnect teardown in `AsyncNetClient`. Same area, same failure. |
| "Taz hang on network errors" (`b71ed11794`) | credzba | Network hang. The commit message is honest that it is a guess, so read before trusting. |
| Phantom "Connection lost: Socket Error" on connect (`927480fe54`) | Sitch | Login-path socket handling, which we also touched. |
| Downgrade "need more data" packet log from Warn to Trace (`29a0d6a7a0`) | Felix | We rewrote the log writer and the packet framing. A per-partial-read warning is noise at best. |
| Force DirectX 11 via `ForceDriver 4` (`66cadd31f3`) | LasherasGH | Claimed general performance win on Windows. |
| Various rendertarget/filter fixes (`beb508811c`) | Andrew Livesay | Rendering correctness. |
| `perf(rendering)`: skip the two-pass UI draw when UIScale is 1.0 (`bd97465f89`) | crameep | Most players never scale the UI, so most players pay for a pass they do not need. |

## Not relevant

OpenUO, `uo1998`, the headless frontend, the Godot port and the weather work
are separate directions rather than fixes. The bulk of `yuval-po`'s volume is
a Myra UI rework that does not apply to the 4.7.2 UI.
