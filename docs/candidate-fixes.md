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
