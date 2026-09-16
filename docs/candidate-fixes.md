# Candidate fixes: read, verified, not applied

Everything below was checked against this fork's own source. Nothing here has
been changed. Sources are TazUO `main` and the 65 forks (see `fork-scan.md`).

## Confirmed: bugs this fork has right now

### 1. `OnConnected` fires inside the connect try/catch

`AsyncNetClient.cs:83`. The receive loop is already started on line 81, so the
socket is genuinely up. Then:

```csharp
OnConnected?.Invoke(this, EventArgs.Empty);
return true;
}
catch (Exception ex)
{
    Log.Error($"Error while connecting {ex}");
    OnError?.Invoke(this, SocketError.SocketError);
    return false;
}
```

`OnConnected` handlers run the login handshake and touch the UI. Anything one of
them throws is caught here, reported to the player as **"Connection lost: Socket
Error"**, and `return false` tears down a connection that was fine.

`Sitch` (`927480fe54`) moves the invoke outside the connect guard and logs a
handler fault as what it is. This matters more here than upstream because our
rewrite starts the receive task before the invoke.

### 2. `Control.Clear()` disposes while enumerating the live list

`Control.cs:823`:

```csharp
public virtual void Clear()
{
    foreach (Control c in Children)
    {
        c.Dispose();
    }
}
```

We already fixed exactly this hazard at `Control.cs:1141`, with a comment saying
why - disposing a child may add to or remove from `Children`, and `List<T>`
throws the moment that happens. `Clear()` was missed.

Sitch's fix snapshots with `.ToArray()`, the same shape as our line 1141.

### 3. A LINQ scan of every item in the world, on every step

`PlayerMobile.cs:1500`, the auto-open-doors check:

```csharp
if (World.Items.Values.Any(s => s.ItemData.IsDoor && s.X == x && s.Y == y && ...))
```

Once per step, over the whole item dictionary, whenever "open doors while
pathfinding" is on. `crameep` (`2e81f4240e`) walks the one map tile instead.

### 4. A deferred removal can delete what the server just placed

`World.ObjectToRemove` is set on pickup and consumed a frame later. We cancel it
in one place only - `PacketHandlers.cs:1787`, and only for the cursor's held
item. Any other path where the server places that serial leaves a stale removal.

`Oleh Romanovskyi` (`f9afddf4c5`) cancels from every placement path, with a test.

---

## Already present - no action

| | |
| --- | --- |
| `PacketHandlers.Handler.Reset()` between connection attempts (credzba `589cef418e`) | We have it, at `PacketHandlers.cs:190` and both `LoginScene` call sites. |
| `Control.Dispose` snapshotting `Children.ToArray()` | Present at `Control.cs:1141`. |
| Account packets (0x80, 0x91) kept out of the packet log | Intact at `PacketLogger.cs:41`. |

---

## Do not take

**Felix `29a0d6a7a0` / `4fccc9348f`.** The useful half downgrades a "need more
data" log line from `Warn` to `Trace` - fine, it fires on every partial TCP
read. The same commit also replaces the account-packet logging guard with
`if (false)`, which would write **account name and password packets into the
log file**. Take the log level, never the guard.

---

## Does not apply to 4.7.2

- **Two-pass UI draw skip** (crameep `bd97465f89`) - our `UIManager` has no
  `uiTransform`; UI scaling is a modern-only feature.
- **Most of TazUO main's crash fixes.** Modern split `PacketHandlers.cs` into
  one file per packet and added managers that do not exist here. Of ten crash
  fixes sampled, seven touch no file this fork has. They need re-deriving from
  the described symptom, not porting.

---

## Still to read

| | |
| --- | --- |
| credzba `b71ed11794` | "AI thinks this is solution to Taz hang on network errors" - the author flags it as a guess. Read before trusting. |
| LasherasGH `66cadd31f3` | Force DirectX 11 via `ForceDriver 4`; claimed general Windows performance win. |
| Andrew Livesay `beb508811c` | Rendertarget and filter fixes. |
| crameep, rest of the perf series | Corpse snapshot caching, scavenger snapshot cache, autoloot decoupling. |
| birdinforest / CanGG weather | Graphics, and a feature rather than a fix - but a wanted one. |

---

# Read pass 2: crameep and credzba

Both are active, long-running forks. Read in full.

## Confirmed: the OPL request list is an O(n²) scan in the burst path

`PacketHandlers.AddMegaClilocRequest` (`PacketHandlers.cs:447`):

```csharp
public static void AddMegaClilocRequest(uint serial)
{
    foreach (uint s in Handler._clilocRequests)
    {
        if (s == serial)
        {
            return;
        }
    }

    Handler._clilocRequests.Add(serial);
}
```

A linear scan of the pending list, on every call. It is called once per arriving
object - `PacketHandlers.cs:5722` and `:7501`, and from
`ObjectPropertiesListManager.cs:85`. The list is only emptied when
`SendMegaClilocRequests` runs, so within one burst it just grows.

That makes the cost quadratic in the size of the burst:

| burst | comparisons |
| --- | --- |
| 287 items (a recovery resync) | ~41,000 |
| 487 items (the largest measured) | ~118,000 |
| 1,394 items (login / zone load) | ~970,000 |

Against a walking arrival rate of ~7 items/second this is invisible - the list
never gets long. It only bites when objects arrive in bulk, which is exactly
when the stutter is felt. **This is the best mechanical explanation found so far
for why a forced house recovery hitches and a walk-up does not.**

`crameep` (`ea6e61a2a2`) keeps a parallel `HashSet<uint>` beside the list and
lets `HashSet.Add` do the dedup in constant time. Same fix for the custom-house
request list, which modern has and this fork does not.

## The OPL scan: retracted correction, 2026-09-14

An earlier draft of this file claimed the quadratic never matters because the
queue stays shallow. **That was wrong and is withdrawn.** The reasoning and the
measurement that settles it are both recorded here so neither gets repeated.

**The bad argument.** The client sends one `0xD6` request per frame. In the
busiest second of the capture it sent 39, against a ceiling of about 60 at
60fps, so - the argument went - the queue was empty a third of the time and
never backed up.

That is circular. The send rate is capped by the **frame rate**, and the frame
rate collapsing is the thing being investigated. 39 packets in a second is
equally consistent with a client running at 39fps with the queue full the whole
time. Packet count cannot distinguish the two.

**The measurement that can** is how many serials each packet carried. The cap is
15 (`Math.Min(15, serials.Count)`), so a shallow queue sends near-empty packets
and a backed-up one sends full ones:

| second | request packets | serials requested | serials per packet |
| --- | --- | --- | --- |
| login / zone load | 39 | 565 | **14.5** |
| house approach | 18 | 247 | **13.7** |
| approach | 19 | 251 | **13.2** |
| approach | 24 | 299 | 12.5 |
| approach | 21 | 250 | 11.9 |

**14.5 out of a maximum of 15.** Every send was essentially full, which only
happens if at least 15 serials were waiting each time. The queue is saturated
through the whole burst.

So the original account stands: several hundred serials queue during a burst,
arrivals land in a handful of frames, and `AddMegaClilocRequest` linear-scans
the pending list on every one of them. At a peak depth around 500 that is on the
order of 120,000 comparisons, concentrated in the frames where the client is
already busiest.

**What is still not established** is that this is the whole stutter. It is one
real cost among several - object construction, the tile insertion walk, cold
art loading - and none of those have been measured either. It is a cost worth
removing on its own terms.

**Note on crameep's fix.** He keeps a parallel `HashSet` but then clears and
rebuilds it from the remaining list on every send, which is O(n) per frame. That
trades the arrival cost for a per-frame one. Removing only the serials that were
actually sent - the first 15 - is O(15) and needs no rebuild.

## Confirmed: Python script threads are foreground threads

`LegionScripting.cs:444`:

```csharp
script.PythonThread = new Thread(() => ExecutePythonScript(script));
```

No `IsBackground = true`. A foreground thread keeps the process alive after the
window closes, so a script still running leaves the client as a zombie process
that has to be killed. `crameep` (`81ddea7887`) sets `IsBackground` and joins
with a 3 second timeout.

## Does not apply, after reading

| candidate | why not |
| --- | --- |
| credzba `b71ed11794`, the network hang | Modern keeps a persistent `LoginHandshake.Instance`, so `CurrentLoginStep` survives as `EnteringBritania` and the reconnect guard never matches. Here `LoginScene` is constructed fresh at all six call sites and `CurrentLoginStep` initialises to `Main`, which our guard at `LoginScene.cs:202` accepts. The bug is an artifact of a refactor we never took. |
| credzba `ef664c6a3e`, map loading performance | It removes a global `MapFileIOLock` held around a seek-then-read of every map block. We have no such lock: `Chunk.cs` reads through raw pointers into the memory-mapped file (`MapBlock* block = (MapBlock*) im.MapAddress`). **We are already faster than the thing being fixed** - worth remembering before anyone "modernises" that read path. |
| crameep's `fix(scaling)` series, ~10 commits | All are UIScale > 1 corrections. There is no UI scaling on 4.7.2. |
| crameep's macOS codesign/bundle fixes | This fork ships Windows only. |

## Still unread

crameep's controller overhaul and autoloot work (features), the weather work,
LasherasGH's DirectX 11 driver force, Andrew Livesay's rendertarget fixes.

---

# Read pass 3: the rest

All 65 forks, 1,109 unique commits, read to completion. Nothing applied.

## Confirmed present in this fork

Verified against our own source. The line numbers are ours.

| # | What | Where | From |
| --- | --- | --- | --- |
| 1 | **OPL request list is an O(n²) scan in the burst path** | `PacketHandlers.cs:447` | crameep `ea6e61a2a2` |
| 2 | **`OnConnected` fires inside the connect try/catch** - a UI fault becomes "Connection lost: Socket Error" and tears down a live socket | `AsyncNetClient.cs:83` | Sitch `927480fe54` |
| 3 | **`Control.Clear()` disposes while enumerating the live list** - we fixed the identical hazard at `:1141` and missed this | `Control.cs:823` | Sitch `927480fe54` |
| 4 | **LINQ scan of every item in the world, per step** (auto-open-doors) | `PlayerMobile.cs:1500` | crameep `2e81f4240e`, Sitch `c0b83b598e` |
| 5 | **Deferred removal can delete what the server just placed** | `World.ObjectToRemove`, guarded only at `PacketHandlers.cs:1787` | Oleh Romanovskyi `f9afddf4c5` |
| 6 | **Python script threads are foreground** - a running script holds the process open after the window closes | `LegionScripting.cs:444` | crameep `81ddea7887` |
| 7 | **Three exclusive `FileStream` opens** - no `FileShare.Read`, so a second client cannot read them | `WorldMapGump.cs:1879`, `:1903`, `ClilocLoader.cs:109` | credzba `3eba99ea49` |
| 8 | **`DrawLine` dereferences `texture.Bounds` with no null/disposed guard** | `Batcher2D.cs:789` | Derek Wang `8a7d60e676` |
| 9 | **`HealthLinesManager` draws `gumpInfo.Texture` unguarded** | `HealthLinesManager.cs:334` | fuzzlecutter `f947a39cdd` |
| 10 | **`_localIP` is composed little-endian** for the login seed | `AsyncNetClient.cs:318` | fuzzlecutter `6cec5e7552` |
| 11 | **No door-diagonal guard in the pathfinder.** A door on either cardinal tile flanking a diagonal makes the server reject the step even when the door is open; the client approves and sends it, so the walk is denied - rubber-banding | `Pathfinder.cs` (has `IsDoor` at `:296`, `:300`, no diagonal check) | Claude/bittiez `b5482f6829` |

Already applied this session: the `Mobile.ProcessSteps` direction mask, the
`0x19` null guard, and the `Plugin.Tick` guard.

## Read and ruled out - do not spend time on these again

| candidate | why not |
| --- | --- |
| credzba's network hang `b71ed11794` | Fixes a stale step on modern's persistent `LoginHandshake.Instance`. We build `LoginScene` fresh at all six call sites and start at `Main`, which our guard accepts. |
| credzba's map loading perf `ef664c6a3e` | Removes a global `MapFileIOLock` around a seek-and-read. We have no such lock - `Chunk.cs` reads through raw pointers into the memory-mapped file. **We are already faster than the code being repaired.** |
| TazmanianTad's out-of-bounds read fallbacks `299fcdbb37` | Guards the `ReadAt` path that the above introduced. We never had it. |
| `TextureAtlas` packed-rectangle scope `09912d2d0d` | Ours already declares the rectangle outside the loop. |
| crameep's ~10 `fix(scaling)` commits, and the two-pass UI draw skip | UIScale > 1 does not exist on 4.7.2; our `UIManager` has no `uiTransform`. |
| crameep's macOS codesign / .app bundle fixes | Windows-only download. |
| TazmanianTad's BandageManager timer GC `365617fe47` | Our `BandageManager` uses no `System.Threading.Timer`. |

## Large, real, needs its own review before anyone touches it

| | |
| --- | --- |
| **Andrew Livesay `beb508811c`** - rendertarget and filter rework, 313 lines in `GameScene` alone. Changes the default filter from `xbr` to `linear`, adds a max-texture-size clamp and a single `GetActiveScale()`. Plausible rendering correctness and performance win, far too big to take on trust. |
| **TazmanianTad `c426e7448a`** - audio device disconnect recovery and fallback, 272 lines in `AudioManager`. We have already rewritten `AudioManager` heavily for the region music map, so this is a merge, not a port. |
| **LasherasGH `66cadd31f3`** - `ForceDriver 4` selecting D3D11 via `FNA3D_FORCE_DRIVER` and the SDL render-driver hint. Nine lines, self-contained, claimed general Windows performance win. Cheap to try, needs measuring. |
| **Senzaiken `b4270b2926`** - reload dynamic maps only when the facet size actually changes, and relink items, mobiles and houses across the reload instead of dropping them. Touches `World.cs` and the same object-retention question as our cull work. |

## What each fork actually is

| fork | what it is |
| --- | --- |
| `crameep` 206 | Controller overhaul, autoloot, UI scaling, and a genuine performance series. The most useful single fork. |
| `Nesci28` 135 | Grid highlight, nameplates, multi-move. Mostly UI features. |
| `yuval-po` 90 | Myra UI rework, scripting, a real test suite. Little applies to the 4.7.2 UI. |
| `birdinforest` / `CanGG` 82 | Weather - rain sound, splashes, ripples - with unit tests. A feature, and a competent one. |
| `shedar` 46 | Headless WebSocket frontend and a QA harness. Carries the item-removal race fix (#5 above). |
| `openuo-online` / `uu1001com` 46 | OpenUO. A separate project now. |
| `fuzzlecutter` 39 | `uo1998`, an era-locked client. Source of the endianness and texture-crash fixes. |
| `fspy` 27 | Myra widgets and IronPython API guards. |
| `Andries1985` 26 | POL emulator compatibility and character-creation gumps. Shard-specific. |
| `credzba` 21 | Reconnect, file locks, tile markers, a custom status gump. |
| `sitch` 18 | Options and container UI, plus the phantom-disconnect fix and a hardened `TryOpenDoors`. |
| `Senzaiken` | Server-driven dynamic map definitions and dynamic spellbooks. |
| `eddo87` | Self-heal timing from FC/FCR, spell bar hotkeys, world-map pathfinding. |
| `Marc G.` (across forks) | The largest body of UI work anywhere - nameplates, grid highlight, item comparison. Almost none of it ports. |
| `Oleh Romanovskyi` | A ModernUO QA harness, plus the item-removal race fix. |
| `Marcus_Privat` | Mount animation precedence, journal classifier lock removal. |
| `puppyflips` | Paperdoll armour layer ordering. |
| the remainder | A handful of commits each, mostly shard-specific. |

---

# Read pass 4: closing sweep

Everything remaining across both fork networks, plus ServUO and MW Edition.

## Two more confirmed present

| # | What | Where | From |
| --- | --- | --- | --- |
| 12 | **Same-Z statics draw in the wrong order.** The insertion tie-break covers `Land` only - `state` is `0` for Land, `1` for Mobile, `2` for a custom-house preview, and **`-1` for a plain static**. So `Static` vs `Static` at equal `PriorityZ` never breaks, the newly-added one is appended tail-ward, and the head-to-tail draw paints it last. The classic client does the reverse: whichever is stored earlier in `statics.mul` goes on top. K verified it against `client.exe` - 89% of same-Z carpet/floor pairs map-wide store the carpet first, and carpets render above floors. | `Chunk.cs`, the `while (o != null)` insertion walk | K `95d7744b44` |
| 13 | **`FastList<T>.Length` assigned directly in the font wrap path**, six times, instead of `Resize()` - the overflow Kamron Batman fixed. | `FontsLoader.cs:831`, `:923`, `:1399` and three more | Kamron Batman `14af3802f6` |

> **Correction on #12, added 2026-09-14.** K's fix landed on ClassicUO `main`
> on 2026-06-04 and **andreakarasho reverted it eight days later**
> (`b5b77ea149`, 2026-06-12). It is on no ClassicUO branch today - not `main`,
> not `beta`, not `impl/ecs`. The revert carries no explanation, but same-Z
> ordering has a history: a 2018 commit records that *"on Outlands shard some
> tiles have same Z and same PriorityZ, so the mergesort exchanges them every
> time an object gone to this tile"*.
>
> The **observation** still stands - our tie-break covers `Land` only, `state`
> is `-1` for a plain static, so two statics at equal `PriorityZ` never break
> and the later-loaded one is painted on top. What is no longer established is
> that K's fix is the right answer. Treat #12 as **contested, not confirmed**,
> and do not apply it without understanding why upstream backed it out.

## Two more worth a look

| | |
| --- | --- |
| **Character deletion goes by list position, not serial.** `LoginScene.DeleteCharacter(uint index)` sends `Send_DeleteCharacter((byte)index, ...)`. If the client's list order ever differs from the server's, this deletes the wrong character. Valentin (`959c4a56b0`) switched it to the serial and added the null guards on the `FirstOrDefault(...).RawName` lookups beside it, which are unguarded here too. |
| **Wide items drawn behind southern statics.** Our `View.cs` computes `index.Width` and uses it for the x offset but never feeds it into `depth`. Jack Ward (`da7e249ccb`) adds `depth += index.Width / 22f`. We have both the `depth` parameter and `index.Width`, so it is a live candidate. |

## Sources now exhausted

| source | result |
| --- | --- |
| TazUO forks - 65 repos, 1,109 unique commits | read; `fork-scan.md` |
| ClassicUO forks - 419 repos, 2,578 unique commits | read; `cuo-fork-scan.md` |
| TazUO `main` since the fork | read; 124 commits, three applied, most touch files that do not exist on 4.7.2 |
| TazUO `legacy` | frozen at our fork base. Nothing, ever. |
| ClassicUO `main` | comparatively quiet; the live work is the ECS branch and does not port |
| MW Edition | **16** commits not already in TazUO - CI, test enums, version and logo. Nothing to take. |
| ServUO | read for the send path; it answered the empty house. See `empty-house.md`. |

## Where this leaves things

**Thirteen confirmed bugs**, each verified against our own source. Three fixed
already this session (`Mobile.ProcessSteps` direction mask, the `0x19` null
guard, the `Plugin.Tick` guard). Ten waiting on your call.

Nothing has been applied beyond those three.

---

# Read pass 5: the TazUO main backlog, finished

Every fix commit on `taz/main` since the fork, checked for whether its files
exist here. Earlier sampling suggested most did not; over the full set, more do.

## Confirmed present

| # | What | Where | From |
| --- | --- | --- | --- |
| 14 | **The audio probe crashes machines with no sound device.** `new DynamicSoundEffectInstance(0, AudioChannels.Stereo).Dispose()` leaves a partially-constructed object behind when the constructor throws `NoAudioHardwareException`; the GC finalizer then crashes on it. Catching the exception, as we do, does not help - the object is already queued. bittiez replaced the probe with a read of `SoundEffect.MasterVolume`, which needs no object. | `AudioManager.cs:61` | `54a6df2f84` (#967) |
| 15 | **Peripheral input before profiles are loaded.** `string.IsNullOrEmpty(UIManager.SystemChat.TextBoxControl.Text)` runs with no check that `ProfileManager.CurrentProfile` and `GlobalSettings` exist yet. | `GameSceneInputHandler.cs:1607` | `68311fbe46` |

Number 14 matters more than its size suggests: it is a hard crash at startup on
any machine without working audio hardware, and this fork has rewritten
`AudioManager` heavily for the region music map, so it is ours to carry now.

## Not applicable

| | |
| --- | --- |
| `91c844cea3`, profile migration version | Our `Profile.cs` has no `ProfileMigrationVersion`; different lineage. |
| the UI-scaling fixes, `007bbd7d20`, `8521331976` | No UI scaling on 4.7.2. |
| the majority of the crash batches | Modern split `PacketHandlers.cs` into one file per packet and added managers we do not have. Those need re-deriving from the symptom. |

## Still to read

`e4ab9ed33b` (corrupt InfoBar blocking bootstrap - we have the same
`if (root != null)` shape and no per-item guard), `4a95c04d8a` (mouse changes,
seven files we have), `ab0f248bb4` and `9e8ba53483` (door movement blocking),
`ea57b6acd7` (multiple crash fixes, two files we have).

---

# Deep read: #11 and #13, 2026-09-15

## #13 - the font wrap overflow

**Where the idea came from.** Kamron Batman `14af3802f6`, recorded in the read
pass above. The commit itself was not re-read this session; what follows was
worked out from this fork's own code.

**Where the code came from.** Mine. `FastList<T>.Resize` exists on neither
`origin/release` nor `origin/upstream-main` - both have `Add` and
`EnsureCapacity` and nothing else that moves `Length`. Six call sites in
`FontsLoader.cs` changed from `ptr.Data.Length = X` to `ptr.Data.Resize(X)`.

**Is it a real bug here?** Yes, and it is narrow. `FastList.Length` is a public
field, so assigning it is legal and silent, and `Add` is the only thing that
grows `Buffer`. In `GetInfoASCII` the two counts stay in step everywhere except
one branch:

```csharp
else if (countspaces && si != '\0' && lastSpace - eval == ptr.CharCount)
{
    ptr.CharCount++;          // no matching ptr.Data.Add
}
...
ptr.Data.Length = ptr.CharCount;   // now one past what was added
```

`Add` grows the buffer to 5, 10, 20, 40, 80 and so on, so `Length` only
exceeds `Buffer.Length` when the last `Add` filled the buffer exactly and that
`CharCount++` then fires. When it does, anything walking `Data` up to `Length`
indexes one past the end - an `IndexOutOfRangeException` inside the text wrap
path, so a crash while drawing text rather than anything subtle.

The other five sites are consistent; they are changed for uniformity, since a
raw assignment is the hazard whether or not that particular one can overflow.
`Resize` grows first and then assigns, so the assignment means what it looks
like it means.

## #11 - the diagonal, verified against the server

**The rule, from ServUO `Scripts/Services/Pathing/Movement.cs`**, fetched and
read this session:

```csharp
bool checkDiagonals = ((int)d & 0x1) == 0x1;
...
if (moveIsOk && checkDiagonals)
{
    if (m != null && m.Player && m.AccessLevel < AccessLevel.GameMaster)
    {
        if (!Check(left) || !Check(right))   // a player needs BOTH flanks
            moveIsOk = false;
    }
    else
    {
        if (!Check(left) && !Check(right))   // a creature needs EITHER
            moveIsOk = false;
    }
}
```

Two things settle the fix's shape:

- **There is no door in that code.** `Check` is the ordinary passability test on
  the flanking tile, reading the item's current flags. A closed door is
  impassable and blocks; an **open door is passable and does not**. So a fix
  that special-cases `IsDoor` - which is what `b5482f6829` does - would refuse
  legal diagonals past every open door. That is the objection that stopped this
  being applied, and it holds.
- **The client has no flank test at all.** `Pathfinder.OpenNodes` costs a
  diagonal at 2 and calls `CanWalk` on the destination only. So the client will
  path diagonally between two walls, between two crates, or through any other
  blocked pair, send the step, and be refused. Doors are the common case, not
  the cause.

**The fix worth making is the server's rule, not the door case**: on a diagonal,
require `CanWalk` on both flanking cardinal tiles. It uses the test already in
the file, it covers walls and furniture as well as doors, and an open door
passes it. Creatures get the either-flank form, which the client does not path
for anyway.

## Do the two door changes collide?

No. The one already applied is in `PlayerMobile.TryOpenDoors` and answers "is
there a door on the tile I am about to step onto, so should I send an
open-door", replacing a `World.Items.Values.Any(...)` scan of every item in the
world, on every step, with a walk of that one tile's object list. #11 would be
in `Pathfinder`, and answers "is this diagonal step legal at all". Different
files, different questions, no shared state.

One interaction is worth naming rather than hiding: with the flank rule in, a
diagonal flanked by a **closed** door becomes illegal, so the pathfinder routes
around it instead of through - even though auto-open-doors would have opened
that door had the player walked into it. That matches the server, which would
have refused the step, and the route around costs a tile.

---

# Crash read: the animation atlas, 2026-09-16

From a dev-build crash log on `login.uoalive.com`:

```
System.InvalidOperationException: Texture2D creation failed! Error Code: The GPU
will not respond to more commands ... (0x887A0006)
   at ClassicUO.Renderer.TextureAtlas.CreateNewTexture2D()
   at ClassicUO.Renderer.TextureAtlas.AddSprite(...)
   at ClassicUO.Renderer.Animations.Animations.GetAnimationFrames(...)
   at ClassicUO.Game.GameObjects.Mobile.DrawInternal(...)
```

**Not from anything on `dev`.** `git diff origin/release...dev -- src/ClassicUO.Renderer/`
is seven lines, all of them the `DrawLine` null guard, which is on no path in
this stack. The file that crashed is byte-identical on both branches, and
identical again on `origin/upstream-main`.

**What the code did.** `AddSprite` had one exit:

```csharp
while (!_packer.PackRect(width, height, out pr))
{
    CreateNewTexture2D();
    index = _textureList.Count - 1;
}
```

`CreateNewTexture2D` allocates a whole page and **replaces the packer with a
fresh empty one**. So a sprite that does not fit an empty page cannot ever fit,
and the loop allocates another page - 4096x4096x4, **64 MB**, for the art and
animation atlases - and tries again, without limit.

Nothing upstream bounds the size. `AnimationsLoader` reads frame dimensions as
`ReadInt16LE()` and tests only `frame.Width <= 0 || frame.Height <= 0`, so
anything from 4097 to 32767 arrives at a 4096 atlas intact.

**What the error code means, and what it does not.** `0x887A0006` is
`DXGI_ERROR_DEVICE_HUNG` - a TDR, Windows resetting a GPU that stopped
answering. It is not an out-of-memory code; that would be `E_OUTOFMEMORY` or
`DXGI_ERROR_DEVICE_REMOVED` (`0x887A0005`). So the stack proves the allocation
was the call that *noticed* the hung device, not that it caused it. Two readings
fit:

1. The loop ran away and hammered the driver until it stopped answering.
2. The GPU hung for its own reasons and this was simply the next call to touch it.

**How to tell them apart.** `CreateNewTexture2D` writes
`creating texture: 4096x4096 Color` at Trace on every page. In the client's
ordinary log - not the crash log - a burst of those immediately before the
crash means the first reading; one or two means the second.

**Fixed regardless**, because the loop can only ever end in a hang or an
exhausted card:

- A sprite bigger than the atlas is refused up front.
- The loop stops after one fresh page fails, rather than trusting that size
  test to be right about the packer's own arithmetic. A sprite that will not go
  into an empty page will not go into the next empty page either.
- `null` is returned, which is exactly what all five callers already use to mean
  "not loaded", and what `MobileView.DrawInternal` already tests for before
  drawing. A refused sprite does not draw; it does not stop the client.
- `Dispose` no longer assumes `_packer` exists, since a client that only ever
  saw refused sprites would never have built one.

**This is on `release` too**, unchanged, and in upstream TazUO `main`. Getting
it into the public build is the usual promotion off `release`, and needs a
version bump to publish.

## Follow-up, 2026-09-16: what the session logs actually showed

Five session logs, one of them covering the crash. Corrections and findings:

**The runaway atlas loop did not happen.** One `creating texture` line at
`08:55:03.0257`, the crash at `08:55:03.2106`. Page growth over the whole 60-hour
process was 24, then 5, then 3 - a cache filling and plateauing. The bound added
to `AddSprite` stands as hardening for a loop that provably cannot terminate, but
it is **not** the fix for this crash and would not have prevented it.

**The device was lost two and a half hours before the crash.** At `06:23:40` the
same `0x887A0006` already failed, from `RenderedText.CreateTexture()`, which
catches and swallows. The client then ran until `08:55:03` - profile saves firing
on the hour - before a creature needing a new atlas page hit a path with no catch.

Windows' System log carried **nvlddmkm Event 153, `\Device\Video3`, GPUID 100** an
hour before the first failure. That is the display driver reporting a contained
error: the offending context dies, the GPU does not. It is why one client died
and a second client on the same machine kept running.

**The freeze is explained.** FNA presents a frame only if `Draw` returned, so once
the device is gone the window keeps its last frame while `Update` carries on -
movement, sound and network all working against a picture that will never change.

### Changed

| | |
| --- | --- |
| `GpuDeviceWatch` + hooks in `TextureAtlas` and `RenderedText` | The first refusal is now one clear line with uptime, frames drawn, atlas pages per cache, managed memory and the current scene - then an SDL message box (no GPU needed) and a clean exit, instead of hours of frozen zombie followed by a stack pointing at innocent code. |
| `WorldMapGump.LoadMarkers` | **Real GPU texture leak.** `_markerIcons.Add` keyed on the filename without extension, lowercased, across `.cur`/`.ico`/`.png`/`.jpg` from several directories - so `bank.ico` and `bank.png` collide. `Add` threw, the catch logged, and the already-created texture was left referenced by nothing, so the dispose loop at the top of the method never reached it. One leak per duplicate, **on every world entry**. 328 in the captured session. |
| `Gumps.Gump.GetGump` | A null texture is the cache-miss sentinel, so an index the files do not contain was re-asked on every call forever. Misses are remembered now - including a sprite `AddSprite` refuses, which would otherwise be retried for the life of the client. |
| `PaperDollInteractable` | Reports each absent graphic once per session rather than once per draw. Over a thousand log lines in one session came from a handful of graphics. |

### Not chased yet

`No container (1093733167) found`, about eight per second for at least five
seconds, from `PacketHandlers` `AddItemToContainer` - the server pushing contents
into a container this client is not holding.
