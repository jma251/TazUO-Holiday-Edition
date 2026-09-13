# Razor Enhanced vs Legion — what is actually missing

Scope: **Python API only.** UOSteam deliberately excluded.

Compared `jma251/RazorEnhanced-Holiday-Edition` @ `b38520f` (`release/1.0`) against
TazUO Modern's `LegionAPI.cs` + `ApiPlayer.cs`, and against this fork's `API.cs`.

Everything below was checked by reading both implementations, not by matching names.

---

## First, the thing that changes the plan

**Razor Enhanced's Python API is not riddled with broken calls.**

- **470 public members across 28 classes.** `Player` alone has 140.
- Exactly **one** `NotImplementedException` in the whole scripting layer, and it is in
  `CircularBuffer`, not an API method.
- The "not implemented" markers are **all in `UOSteamEngine.cs`** — eight commands routed
  to a `NotImplemented()` helper that logs and returns true. Out of scope here.

Two genuinely incomplete Python-facing behaviours, and that is the whole list:

| Call | Problem |
| --- | --- |
| `Items.FindByID(..., range)` | `range` is accepted and ignored. Source: *"range should be # of packs deep to search .. but not implemented"* |
| `Items.FindByName(..., range)` | Same |

So the work ahead is **additive, not corrective**. Nothing needs un-breaking first.

---

## The real gaps on `Player`

Verified against RE's actual 140 members, not by name matching.

| Legion has | RE has | Verdict |
| --- | --- | --- |
| `IsCasting` | — | **Genuine gap.** See recipe below. |
| `IsRecovering` | — | Gap, but see the warning below — Modern's is a stub. |
| `IsWalking` | `Walk`, `Run`, `ToggleAlwaysRun` (actions) | **Genuine gap** — RE has no movement *state*. |
| `TithingPoints` | — | Genuine gap. Small. |
| `MaxFireResistance` and the other four caps | the four resistances, no caps | Genuine gap. |
| `Race` | — | Genuine gap. Minor. |

### Not gaps — RE has these under different names

Do not spend time on these; I nearly did.

| Looks missing | RE's name |
| --- | --- |
| `IsHidden` | `Visible` (inverse) |
| `PhysicalResistance` | `AR` |
| `PrimaryAbilityActive` / `SecondaryAbilityActive` | `PrimarySpecial`, `SecondarySpecial`, `HasPrimarySpecial`, `HasSecondarySpecial`, `HasSpecial` |
| `StrLock` / `DexLock` / `IntLock` | `GetStatStatus` / `SetStatStatus` |
| `StatsCap` | `StatCap` |
| `SysMsg` | `Misc.SendMessage` |
| `Pathfind` | `PathFinding.Go` / `Player.PathFindTo` |
| `MoveItem` | `Items.Move` |
| `UseObject` | `Items.UseItem` |
| `X` / `Y` / `Z` | `Player.Position.X` etc. |

A raw name diff reports **213** Legion members "missing from RE". Almost all of it is
this. The functional gap is the short table above.

---

## `Player.IsCasting` — how to build it

Neither client is told by the server that you are casting. Both infer it, and RE can run
the identical inference because it sees every packet the client does.

**Start:** watch the player's own speech for a spell's power words (the mantra). TazUO
keys a dictionary on them and matches on any message whose parent is the player.

**Stop:** any one of these clilocs, or the spell's max duration elapsing.

```
500641   Your concentration is disturbed, thus ruining thy spell.
502625   Insufficient mana...
502630   More reagents are needed for this spell.
500946   You cannot cast this in town!
500015   You do not have that spell
502643   You can not cast a spell while frozen.
1061091  You cannot cast that spell in this form.
502644   You have not yet recovered from casting a spell.
1072060  You cannot cast a spell while calmed.
```

Reference: `Game/Managers/SpellVisualRangeManager.cs` in this repo — `OnRawMessageReceived`,
`OnClilocReceived`, `SetCasting`, `ClearCasting`.

### Two warnings before building on it

**`IsCasting` flaps false when you take damage.** Modern TazUO's own self-heal code works
around this:

> *a cast going quiet (IsCasting dropping to false) is NOT treated as an interrupt on its
> own … the client clears its casting flag on any HP change, so a hit mid-cast flaps
> IsCasting off constantly while healing under fire — even though the cast is still going.*

A naive `if not Player.IsCasting` will misfire exactly when it matters — in a fight. If
the point is "do not let two scripts cast over each other", debounce it or track the
cast's own start time and expected duration rather than trusting the flag edge.

**`IsRecovering` is not implemented anywhere.** Modern's source:

```csharp
public bool IsRecovering => IsCasting; //May incorporate this again later, for now just reference is casting
```

It is an alias. Reproducing it faithfully gets you a duplicate of `IsCasting`. If you want
real cast-recovery tracking, it has to be written from scratch — `502644 You have not yet
recovered from casting a spell` is the only signal either client uses.

---

## `Player.IsWalking` — trivial

TazUO, both versions, identically:

```csharp
public override bool IsWalking => LastStepTime > Time.Ticks - Constants.PLAYER_WALKING_DELAY;
```

A timestamp comparison against the last movement. RE already tracks player movement for
its own pathfinding, so this is a property over existing state, not new plumbing.

---

## Beyond `Player`

Legion has clusters RE has no equivalent for. Worth knowing they exist; none are blocking.

| Cluster | Legion members |
| --- | --- |
| Cooldown bars | `CreateCooldownBar`, `CooldownExists`, `UpdateCooldown`, `RestartCooldown`, `DeleteCooldown`, `IsGlobalCooldownActive` |
| Shared/persistent vars | `SetSharedVar`, `GetSharedVar`, `SavePersistentVar`, `GetPersistentVar` … |
| Script control | `PlayScript`, `StopScript`, `ToggleScript`, `IsScriptRunning`, `ListRunningScripts` |
| Sound log | `GetSoundLog`, `CheckSoundLog`, `ClearSoundLog`, `SoundEntries` |
| Move queue | `QueueMoveItem`, `ClearMoveQueue`, `IsProcessingMoveQueue` |
| Map markers | `AddMapMarker`, `RemoveMapMarker`, `MarkTile`, `RemoveMarkedTile` |

These are TazUO *client features* exposed to scripts (its own gump system, its own script
manager). They are not UO protocol, so RE cannot derive them from packets — they would
have to be built in RE against RE's own UI and script engine. Different job entirely.

---

## Suggested order

1. **`Player.IsWalking`** — smallest, no inference, no caveats.
2. **`Player.IsCasting`** — the one you actually want. Build it with the damage-flap
   caveat handled, not as a raw flag.
3. **`TithingPoints`, `Race`, the four resistance caps** — plain status-packet reads.
4. **`Items.FindByID/FindByName` `range`** — the only genuinely broken thing in the
   Python API.

Everything else on the Legion side is either already in RE under a different name, or is
a TazUO UI feature that does not port.
