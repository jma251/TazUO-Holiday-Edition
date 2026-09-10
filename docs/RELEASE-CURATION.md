# What goes into 4.5.2301

80 pull requests since the fork started on 2026-08-09. Many of them undo each
other, so this is grouped by **what is in the code today**, not by history.

Each group is marked with how sure I am. That distinction is the point of the
document — some of this is verified, some of it is a title I have not re-checked.

---

## Group A — The fork itself

**Keep. Not really a decision.**

| PR | What |
| --- | --- |
| #1 | Docs and CI restructured for a legacy-only fork |
| #3 | Permanent numbered releases alongside `latest` |
| #4 | "Holiday Edition" window title, flat zip layout |
| #30 | Deleted upstream's Discord feature bot (it woke twice daily to fail) |
| #31, #32 | Build stamp in the binary, login version label repositioned |
| #70 | Branch cleanup button |

Without these there is no Holiday Edition, only a copy of TazUO.

---

## Group B — Real defects fixed

**Keep. This is the actual value of the fork.**

### Verified this session against upstream sources

| PR | What | Evidence |
| --- | --- | --- |
| #36 | Items going invisible — a chunk cell left pointing at a removed object | The hand-over is missing in both upstream ClassicUO **and** Orion. Genuinely novel. |
| #47 | World entry removed before an object is handed back for reuse | Upstream ClassicUO hit the same bug and **disabled object pooling entirely** rather than fix the ordering. This fixes it properly. |
| #67 | A custom house rebuilding itself as the generic shell one frame later | The defect is present in upstream ClassicUO (`House.cs:148` + `PacketHandlers.cs:5285`) and absent in Orion by design. |
| #81 | Two crashes: the walk counter running off its array, and `Dispose` breaking on its own children | Both diagnosed from your crash logs on 09/04 and 09/09. The `Dispose` one is inherited code and affects every gump. |

### Not re-checked — plausible, but I am going on the title

| PR | What |
| --- | --- |
| #2 | Three drag-select bugs (stale modifiers, hit rectangle offset, coordinate space) |
| #6 | Backported safety fixes: chunk null guard, multi-client language file, font atlas crashes, bandage agent |
| #7 | Animation hover guards, dead code removal, TTF border fixes |
| #8 | Crash guards A–E (the house diagnostic bundled with it belongs in Group D) |
| #9 | Journal entry limit made honest, raised to 5000 |
| #10 | Font settings: live font changes, nameplates, chat input font |
| #44 | World entries removed by the key they were filed under |

These are probably all fine. I am flagging them as unverified rather than
claiming otherwise.

---

## Group C — Music

**Untouched. Your audit, not mine.**

17 PRs: #5, #12–#27. Era selection, the region music map and its transition
modes, the war-mode stop, the season packet fix, the on-screen panel, and the
music logging.

I have deliberately not formed an opinion on any of it. When you have finished
your audit, tell me what stays.

One note: **#27's on-screen music panel and the music index logger are
diagnostics.** If they are not meant to ship to other people, they belong with
Group D.

---

## Group D — Investigation scaffolding

**Strip. This is the big one.**

Built to chase the empty-house problem. It is still live and threaded through
**10 files**:

```
HouseDiagnostics.cs          Item.cs                World.cs
PacketHandlers.cs (8 refs)   HouseManager.cs        GameScene.cs
CommandManager.cs            NetClient.cs           GameSceneDrawingSorting.cs
ModernOptionsGump.cs
```

Plus the `log_house_diagnostics` setting and the `-nearby` command.

PRs: #33, #34, #35, #51, #52, #53, #54, #71, #72, #73.

**Why strip it:** it writes a log file during normal play, it is wired into the
packet handlers on the hot path, and it exists to answer a question that is now
answered — the tooltip/property issue turned out to be server-side (UOAlive
suppresses container counts on greatly-worn houses so IDOC hunters cannot script
contents). It has done its job.

**Keeping it is also defensible** if you want to keep investigating. It is
off by default. But it should not ship to strangers either way — nobody else
should be writing `houselog.txt` while they play.

There is precedent: **#50 already did exactly this strip once**, "keeping only
the fixes it found". This is the same operation on the second round.

---

## Group E — Ranges and culling

**Needs your decision. I will not guess this one.**

Roughly 30 PRs went back and forth here: #28, #29, #37–#43, #45, #46, #48, #49,
#55–#66, #68, #74–#76, #78–#80. Several are explicit reverts of the one before.

What is live right now:

| Setting | Value |
| --- | --- |
| `MIN_VIEW_RANGE` / `MAX_VIEW_RANGE` | 5 / 40 |
| `DEFAULT_VIEW_RANGE` | 24 |
| `MIN_HOUSE_RANGE` / `MAX_HOUSE_RANGE` | 5 / 64 |
| `ClientViewRange` (setting default) | 24 |
| `HouseLoadRange` (setting default) | 40 |

Plus the two view-range sliders in the options.

**The honest position:** the empty-house problem was never solved. The codemap
records ~6% of house entries loading short, and it disproved client-side
distance culling, contents culled past view range, region and teleport theories,
and the draw-ceiling/alpha explanation. Nothing client-side predicts it.

So what survives from this block is not a fix. It is: **the sliders**, which you
said made it "almost seamless" at 40, and a default of 24 that matches what the
server actually maintains.

My recommendation: **keep the sliders and the defaults, drop nothing else from
this area, and stop calling it a fix.** It is a user-facing setting that helps,
which is a perfectly good thing to ship — just not as a bug fix.

---

## Group F — Needs a decision

| PR | What | Why it is here |
| --- | --- | --- |
| #66 | Movement queue drains faster when a mobile is behind | Changes how other players' movement *feels*. Real reasoning behind it, never confirmed good by you in play. |
| #49 | Off-by-default timer nudging the view range to refresh distant mobiles | Off by default, so harmless — but it is scaffolding wearing a feature's clothes. |
| #11 | Silent key press logger | Already dropped in #13. Listed only so it is not wondered about. |

---

## Suggested order

1. **Strip Group D.** Biggest single reduction, no behaviour lost, and it is the
   same operation #50 already did once.
2. **You rule on Group C** when the music audit is done.
3. **You rule on Group F** — #66 in particular, since only you can say whether
   movement feels right.
4. **Re-verify Group B's unchecked half** if you want `4.5.2301` to be something
   you would defend to a stranger. This is real work; it can also wait.
5. Whatever is left is `4.5.2301`.
