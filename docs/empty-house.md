# The empty house: what is measured, and what is not

A house sometimes loads holding a fraction of its contents and stays that way
until the player steps off the threshold tile and back on. This records what the
captures actually show, and — more importantly — which explanations were tried
and are wrong, so they are not tried again.

Sources: `houselog.txt` captures of 2026-08-16 19:29 → 08-17 20:20 (2.7M lines)
and 08-18 00:58 → 02:17; a second round on 2026-09-12 with `packets.log` alongside,
including one taken across login. House `0x409D4F76`, bounds (833,2362)-(856,2385),
238-241 ground items when whole.

## Measured, and holds

| | |
| --- | --- |
| On a failure the server sends ~17 items where ~222 were deleted | 19:32-19:33, arrivals all `created=False` for 50s |
| A working entry sends the room as new objects | 225 × `created=True`, 831 incoming packets |
| `0x22` repairs it | 19:33:04.103 → 242 arrivals, 222 `created=True`, whole before the next sample |
| Stepping to the threshold tile and back always works | ~70 occurrences, every one recovers within one second |
| The client removes nothing of its own on a failure | 239 removals in the failing gap, all `srvdelete` via `PacketHandlers.DeleteObject`; 0 culls |
| Walks are acknowledged normally through a failing entry | `0x02` out, `0x22` ConfirmWalk in, `0x77` — identical to a working one |
| Items inside a house essentially never cull | 10 of 1,862 cull events had `iteminhouse=True` — **see the blind-spot section below before trusting this row** |
| A resync moves *less* traffic than a normal entry | 797 incoming vs 831 |
| The resync hitch is the walk being discarded, not the packets | 9 of 10 resyncs drew a `0x21` and every one produced a second resync (fixed in h69) |

## Failure rate

**~6% of entries, and it does not depend on how far the player had been.**

| | entries | took >5s to fill |
| --- | --- | --- |
| never left the 24-tile box | 27 | 3 — **11%** |
| went beyond 24 tiles | 51 | 2 — **4%** |

## Explanations tried and DISPROVED

Do not rebuild on any of these.

| explanation | killed by |
| --- | --- |
| Client distance-culls house contents | 10 in-house culls out of 1,862; a 24×24 house is 23 tiles corner to corner. **The cull counter was blind then — re-confirmed in September on other evidence, see below** |
| Contents arrive in bulk past the view range and get culled | 0 destroys in the failing window; removals are all `srvdelete` |
| The player did not really leave the region | walks acknowledged normally throughout |
| It is a recall/gate/teleport | 2 of 5 failures involved no teleport; and see the rate table |
| **Crossing out of the server's 24-tile box** | **the rate table above — short trips fail more often, 11% vs 4%** |
| The stutter is packet volume | a normal entry carries more packets and costs nothing |
| Held-but-not-drawn (draw ceiling / alpha) | `-nearby` showed 19 held where ~238 were expected; the items are absent, not hidden |

The 24-tile-box story was asserted in the h70 commit message and PR #78 before the
base rate was checked. It is wrong. The only part that survives is the packet
counts in the table above.

## 2026-09-12 — the trigger, found by the player rather than the logs

**Which stair tile you enter on decides it.** The staircase is five tiles,
x=842-846 at y=2386, sitting at z=2 between ground (z=0) and the foundation floor
(z=7). Entering over the **middle three** loads the room every time. Entering over
either **end tile** - the curved corner pieces at x=842 and x=846 - is when it
comes back empty.

Reproduced deliberately: 3 failures in 15 entries. In the earlier capture, all
three successes approached from x=844 or x=845 and the one failure from x=846.

That answers "nothing measured on the client predicts which entry it will be". The
client does not measure it, because the client is not the one deciding.

Also measured that day, and consistent with everything above:

| | |
| --- | --- |
| On a failed entry the server sends **0** of the room's items | 15:08:59, 15:09:42, 15:10:05 - 0, 2 and 17 arrivals inside the bounds against 242 on a working entry |
| The client sends **nothing** on entry | `HouseManager` has no `Send_` call at all; the only house packet is the *design* request, queued by the server describing a multi |
| The client discards nothing on a failure | no culls, no destroys, in any failing window |
| Recovery is always one large burst well after entry | 427, 432, 487 packets |
| One recovery followed a forced position correction | 15 × `0x21` MoveReject pinning (842,2384,z7), then a single `0x20` placing the player there, then the room. The other two recoveries had no `0x20` |
| View range is granted at 24 and objects arrive at 25 | `C8 18` out, `C8 18` back, then 5,198 items at dist=25 against 783 at 24 |

The last row is real and was fixed - the cull keeps `ClientViewRange + 1` now - but
it is **not this bug** and must not be filed as such. A ring 24 tiles out has
nothing to do with furniture in the room you are standing in.

## Not known

Why the server withholds the contents when the entry tile is a corner stair piece.
The client is not a participant: it asks for nothing, refuses nothing, and
discards nothing. Whatever decides it is server-side.

## What the client does about it

`HouseContentsRecovery` watches the symptom rather than the cause: the room the
player is standing in is counted once a second, and a shortfall of ten or more
that survives three seconds is asked about once with `0x22`, after which the new
count becomes the mark. Off by default, on the Experimental checkbox.

Replayed over the captures before shipping: 7 fires in 24 hours (five of them the
five known failures), 3 in tonight's 80 minutes, and none on the ~70 threshold
dips. An earlier version that counted culled items near any house and polled
blindly fired 10 times in 25 minutes with the house full every time.

## What the August numbers could and could not see

The cull-based rows above were measured with instrumentation that had a hole in
it, and that is worth knowing before trusting them.

`HouseDiagnostics.LogItemCulled` returned without writing anything unless the
player was standing inside a house at that moment. So a cull that happened while
the player was **outside** - which is most of them, and all the interesting ones -
went unrecorded. A 2026-09-12 capture showed **242 in-house items destroyed
against 6 cull lines** in the same window.

Fixed on 2026-09-13: it now gates on where the *item* is, through
`InAnyKnownHouse`, the same as `LogHouseItemDestroyed` already did. Cull and
destroy counts are directly comparable in a capture from that date on.

So:

| row | still safe? |
| --- | --- |
| "10 of 1,862 cull events had `iteminhouse=True`" | **no** - the counter could not see culls taken from outside a house |
| "Client distance-culls house contents - killed by 10 in-house culls out of 1,862" | **weakened by the same hole**, though the September captures independently confirm it: 0 destroys and 0 culls in a failing window, and the server sends nothing |
| Everything measured from arrivals, removals, `created` flags, packet counts and the rate table | **unaffected** - different counters, no blind spot |

The conclusion survived, but it survived on the September evidence rather than the
August cull count. Re-measuring it was right. This is what a new instrument is
for, and it is why a DISPROVED row is worth re-testing when the thing that
disproved it has changed.

What is *not* worth redoing is a theory this document killed with a counter that
was never blind - the rate table in particular, which is what makes the
24-tile-box story wrong.

## 2026-09-14 — the server side, read from ServUO source

`ServUO/Server/Mobile.cs`, the per-step send that runs on every move:

```csharp
var eeable = map.GetObjectsInRange(newLocation, Core.GlobalRadarRange);

foreach (var o in eeable)
{
    if (o is Item item)
    {
        var range = item.GetUpdateRange(this);
        var loc = item.GetWorldLocation();

        if (!Utility.InRange(oldLocation, loc, range) && Utility.InRange(newLocation, loc, range) && CanSee(item))
        {
            item.SendInfoTo(ourState);
        }
    }
    ...
```

Three things follow, and together they account for everything measured.

### There is no "already delivered" ledger. The geometry is the ledger.

An item is sent on **the one step where it crosses from out-of-range to
in-range**, and never again while the player stays nearby: from the next step
onward `!InRange(oldLocation, ...)` is false, so the condition cannot be true.
No record is kept because none is needed.

This is why a discarded item never comes back, and why a six-minute capture
showed zero `DeleteObject` packets. It was never a server bookkeeping quirk -
it is that **the client gets exactly one chance at each item**, and anything it
drops on the frame of arrival is gone until something forces a full resend.

It also settles what the cull should do. Extending the cull by a tile does not
buy a second chance; it only holds objects the server has stopped maintaining,
which is where the stale mobiles came from. The lever that actually works is
the one already shipped: `0x22`, which makes the server run `SendEverything`
and disregard the geometry entirely.

### `CanSee(item)` is evaluated at that single moment

The send is conditional on line of sight **at the instant of the transition**.
If the wall is between the player and the room's contents on the step where
they cross into range, `CanSee` is false, nothing is sent, and the geometry
never offers a second opportunity.

That is a mechanism for the stair-tile trigger. Entering over a curved corner
piece puts the player's crossing step where the house wall still occludes the
room; entering over the middle three does not. It predicts the failure depends
on **the step where range is first crossed**, not on the tile being special -
which is consistent with 3 failures in 15 deliberate entries rather than a
clean always/never split.

### Items carry their own update range

`item.GetUpdateRange(m)` is per-item and separate from the mobile update range,
so house contents need not use the same radius as the player's view range. This
is worth knowing before anyone reasons about the two as if they were one number.

**Caveat:** ServUO is the reference, not necessarily this shard. The pattern is
RunUO-lineage and near-universal, but the shard has not been confirmed to run
it. What is certain is that this shape of send logic produces every symptom
recorded above without needing any other explanation.
