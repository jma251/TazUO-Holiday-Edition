# Fork audit

Every change this fork has made to the client since it left upstream, read
against ClassicUO and TazUO `main` and judged for whether it can destabilise
the client.

- **Fork point:** `c5b59c873`, 2026-08-05, the last upstream commit
- **Since then:** 297 commits, 105 source files, +12,372 / -4,337, 11 files deleted
- **Compared against:** `jma251/ClassicUO` @ `ac6163e`, and `upstream-main` (TazUO 5.24.5)

The rule applied throughout: a difference from upstream is only worth reporting
once it is known whether it is **ours**, **upstream's**, or **new** - and what
the upstream code does instead. A finding without that comparison is a guess.

## Verdict

**No defects found in the audited surface.** Three deliberate deviations from
upstream, all documented in their commits. Two pre-existing weaknesses noted at
the end, neither introduced by this fork.

---

## Audited

### `Network/PacketHandlers.cs` (-45 / +219)

25 hunks. Most are diagnostic call sites. Four change upstream behaviour:

- **Custom house build order.** Upstream cleared the house and stamped the new
  revision *before* the bounds check, so an early return left a house marked
  `IsCustom` at the server's current revision with zero components. `0xBF cmd
  0x1D` never re-requests that state, so the house regenerated empty for the
  rest of the session. Ours moves the clear after the check. **Real fix.**
- **Duplicate house requests.** `_customHouseRequests` now rejects a serial it
  already holds. One login produced ten requests in a millisecond and rebuilt a
  3,283 component house five times. **Real fix.**
- **Draw ceiling.** `UpdateMaxDrawZ(true)` behind an `EntityIntoHouse` test,
  replaced with an unconditional `RecomputeDrawCeiling()`. The old test was taken
  at the one moment the geometry had changed underneath it.
- **Season and music separated.** `ChangeSeason` no longer carries a music index.

### `Game/World.cs` (-14 / +139)

- **Removal by dictionary key, not `item.Serial`.** An item under a stale key
  could never be found to remove, so the distance cull condemned it twenty times
  a second for the rest of the session. One log was 458 MB of that. **Real fix.**
- **`TryGetValue` + `IsDestroyed` guard** before removing an entry, so a serial
  that has been reused is not removed out from under its new owner. **Real fix.**
- `ClientViewRange` default moved from `MAX_VIEW_RANGE` to `DEFAULT_VIEW_RANGE`.
  Upstream's `MAX_VIEW_RANGE` is 24; this fork raised the ceiling to 40 and added
  a 24 default, so the effective default is unchanged from upstream.
- The cull now keeps `ClientViewRange + 1`. See Deviations.

### Object pooling - entirely this fork's

`ReturnToPool` does not exist in ClassicUO. Both methods and all six call sites
are ours, which makes it the highest-risk addition in the fork: handing the same
object back twice would give one object to two serials at once.

Verified correct:

- returns only when `IsDestroyed`
- `_inPool` makes it idempotent
- the flag is cleared by the pool's reset on hand-out (`Item.cs:57`, `Mobile.cs:52`)
- `PlayerMobile` is excluded
- every call site removes the dictionary entry **before** returning the object

### `Network/AsyncNetClient.cs` (-50 / +157)

- **Disconnect latch.** Upstream set `_isDisconnecting = true` and never cleared
  it, so a second disconnect returned immediately and left the socket up. Ours
  resets it in `finally`. **Real fix.**
- **Teardown order.** Upstream waited on the receive task *before* cancelling it.
  Ours cancels first, then bounds the wait with `Task.WhenAny(task, Delay(5000))`.
- The 1 ms poll loop and the per-read `Array.Copy` allocation are gone.

### `Utility/Logging/LogFile.cs`

- `message.Length` (characters) used to size a byte buffer and a byte count, so
  any non-ASCII message was cut mid-character. Now `Encoding.UTF8.GetByteCount`.
- Filename format was `hh` - 12 hour, no AM/PM - so a 14:30 session and a 02:30
  session shared a name and `FileMode.Append` merged them. Now `HH-mm-ss-fff`.
- `Close()` replaced with a locked `Dispose()`.
- Optional size cap keeps the tail, located by binary search. The split index can
  divide a surrogate pair, which yields a replacement character, not a crash.

### `Game/Map/Chunk.cs`

`firstNode = obj.TNext` alone: if the removed object was the cell's head and had
nothing after it but something before it, the cell was emptied and the rest of
the list lost. That is an item vanishing from the world and a map cell pointing
at an object that is gone. `?? obj.TPrevious` closes it; `Node != null` guards
prevent a null dereference. **Real fix.**

### `Game/UI/Controls/Control.cs`

`IsDisposed = true` moved before the children are walked. A child disposing can
re-enter the same control, and while the flag was still false the guard at the
top let the whole body run a second time. **Real fix.** The `GetWidth`,
`GetHeight`, `GetBounds`, `GetScreenBounds` additions exist because the real
properties are `ref` returns, which reflection cannot read - purely additive.

### `Game/GameObjects/Mobile.cs`

Step timing changed. See Deviations.

### `Game/Managers/BandageManager.cs`

A 15 second maximum age on the bandaging buff, so a missed buff-removed event
cannot leave the agent permanently unable to heal. Backport of upstream #826.

### `Game/Managers/TargetManager.cs`

Queued automatic targets now lapse after a timeout rather than waiting forever
and being consumed by the next unrelated target cursor. Note: `IsSet` now has a
side effect - it calls `Clear()` when expired. Deliberate, documented, but it is
a property that mutates.

### Deletions - 11 files

Discord (9 files, ~1,650 lines) and anonymous login metrics (65 lines).
Verified: **zero** dangling references to `DiscordManager`, `DiscordGump`,
`DiscordSocial`, `LoginMetrics` or `Metrics` anywhere in the source, and nothing
left in the csproj or `Directory.Build.props`.

### Per-frame additions

Everything this fork added to the update path:

| Call | Cost when idle |
| --- | --- |
| `HouseDiagnostics.LogHouseContents()` | `[Conditional("HOLIDAY_DEV")]` - not compiled into release at all |
| `AutomationScheduler.Tick()` | Returns immediately unless the master switch is on; off by default |
| `CrashRecoveryManager.Tick()` | Two comparisons. Copies ~50 KB every 5 minutes, keeps 6 snapshots |

`AudioManager` (+647) has no per-frame hook - it is instantiated once and driven
by events.

### Asset loaders

`StaticFilters.cs` and `SoundsLoader.cs` are refactors with larger replacements
(shared-file access, music eras). Configuration and asset loading, not runtime
state.

---

## Deviations from upstream

Three, all deliberate and all documented in their commits.

**1. Mobile step timing** - `Mobile.cs`

```
ClassicUO / TazUO main   maxDelay = TimeToCompleteMovement(run, mounted) - FrameDelay[1]
this fork                stepTime /= Steps.Count  (then the same subtraction, floored at 1)
```

A mobile is drawn at the front of its step queue; the server's position is the
back. Upstream advances that queue at a fixed rate and can never make up time, so
anything moving faster than the table allows falls further behind until the queue
overflows at five steps - at which point the handler throws the held position away
and the mobile appears somewhere else. Dividing by the queue depth drains it
instead. A full queue plays at five times speed, which looks like something moving
quickly; what it replaces is something appearing somewhere else entirely.

**2. The cull keeps `ClientViewRange + 1`** - `World.cs`

Measured across a capture that includes login: the `0xC8` exchange agrees on 24
in both directions, and 5,198 objects then arrive at distance 25 against 783 at
24 - mobiles the same, 91 against 37. Everything on that outer ring was discarded
on the frame it arrived while the server kept it recorded as delivered, so it was
never sent again and no delete was ever issued for it.

`World.RangeSize`, the centre every distance is measured from, tracks the
server-confirmed step rather than the rendered position, so while moving it lags
by up to one step. That is the most likely source of the extra tile.

`+1` rather than raising the view range setting: the setting is what gets asked
for, and the server answers by sending one past whatever it grants, so asking for
more moves the ring outwards instead of closing it.

**3. `HouseLoadRange` split** - `World.cs`

A multi answers to `HouseLoadRange` (40) plus its distance bonus; loose items
answer to the view range. Upstream uses one range for both. A house is a design
fetched by revision and safe to hold early; a loose object is only as good as the
last update the server sent for it.

---

## Standing weaknesses - not introduced here

**`Connect().Wait()` on the game thread.** `LoginScene.cs:733,738` and
`GameActions.cs:1094`, inside `HandleRelayServerPacket`, which runs on the game
thread from the packet loop. Both are genuinely async. `.Wait()` blocks through
DNS resolution and the TCP handshake - a hard freeze on every server hop, and
until timeout against an unreachable server. Unwinding it changes handler
ordering, because the handler relies on the socket being connected by the time it
returns. Real work, not a one-line change.

**`CrashRecoveryManager` writes on the game thread.** Every 5 minutes, two small
files. Negligible, but it is synchronous I/O in the frame loop.

---

## Second pass - the ported helpers and what holds them up

### `FeatureDiagnostics.Guard` - the isolation

Every helper is invoked through it. It catches everything, records the failure,
and after **3 failures inside a 60 second window** the feature is switched off
for the session rather than throwing once a frame forever. `_entries` is keyed by
feature name, so it is bounded by the number of features (22), not by the number
of failures.

That means a helper cannot take the client down even if it is wrong.

### Null safety in the tick paths

Every ported manager that dereferences `World.Player` guards it first. Checked
across all 24: **no unguarded dereference anywhere**. The ones with no guard also
never touch `World.Player`.

With `Guard` wrapped around them as well, that is two independent protections
against the obvious crash - a helper still ticking through a logout.

### Collections

Scanned every file added since the fork for a collection that grows without
bound. Nothing found:

- `FeatureDiagnostics._entries` - keyed by feature name, 22 max
- `SkillReader._idx` - keyed by skill name, ~58 max
- `MusicMapManager` - its two lists are locals inside the file parser, not state
- everything else clears on `ResetForProfile` / `ResetSession`

### `ModernOptionsGump.cs` (+440)

`profile` is assigned once in the constructor from `ProfileManager.CurrentProfile`
and dereferenced 368 times across the gump. The pages this fork added use it
exactly as every upstream page does, so they introduce no failure mode the gump
did not already have.

### `AudioManager.cs` (+647)

No per-frame hook. Instantiated once in `GameController`; everything else is
driven by events. Large, but it cannot cost frame time when nothing is happening.

### `SpellVisualRangeManager.CheckCastExpiry` - per frame

Called from `World.Update`. Early-returns on two comparisons when nothing is
being cast. While casting it calls `DateTime.Now` once a frame, which does a
timezone conversion; the rest of the codebase uses `Time.Ticks`. Negligible, but
it is an inconsistency worth knowing about.

---

## Coverage

Every file where upstream code was **modified or removed** has been read. That is
where behaviour can change and therefore where a regression can hide.

Everything left is a **pure addition** - `+N / -0`, a new file touching no
upstream code:

    HouseDiagnostics 1075   MusicDiagnostics 353   HouseContentsRecovery 236
    MusicInfoGump 222       MusicMapManager 201    ToastGump 200
    SpellVisualRangeManager 185   FeatureDiagnostics 179
    AutomationScheduler 169  AutoHitListManager 139  BandageSettings 136

A new file cannot change how the client behaved before it existed. It can only
misbehave when something calls it, and everything that calls these goes through
`FeatureDiagnostics.Guard` with the per-frame cost and null safety checked above.

`LegionScripting/` is **untouched** since the fork - zero files changed.
