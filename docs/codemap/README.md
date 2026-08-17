# Codemap

A read of the whole client, written down. Every `.cs` file under `src/` was read in
full and mapped; the protocol surface was traced packet by packet into the world state
each handler writes.

Generated 2026-08-17 against `fb765aa` (`v4.5.23-h66`). Line numbers are as of that
commit and will drift.

## Coverage

| | |
| --- | --- |
| Files read | 566 of 566 `.cs` under `src/` (excluding `obj/`, `bin/`) |
| Lines | 219,124 |
| Handlers enumerated | 119 of the 255-entry `PacketsTable` |
| Handlers traced to world mutations | 119 |
| Hazards registered | 461, deduplicated, in 5 tiers |

File lists per partition were produced by `find`, so nothing was skipped as
"uninteresting". The `ui-controls` partitions used `-maxdepth 2` on `Game/UI`, which
also swept `Game/UI/Gumps`; 94 gump files were therefore read twice. That is waste,
not a gap.

## Where to start

| File | What it answers |
| --- | --- |
| `00-INDEX.md` | The frame in tick order, socket to screen, object lifetime, cross-partition wiring, full hazard register |
| `01-HANDLERS-a/b/c.md` | Per-packet reference, ids `0x00`–`0x5F`, `0x60`–`0xAF`, `0xB0`–`0xFF` |
| `02-IGNORE-LIST.md` | Every point where the client does not do what the server said, or keeps/invents state the server owns. Sections A–K plus a load-bearing split |
| `03-LIFETIME.md` | Item/Mobile/GameObject creation, pooling, identity reuse, destroy ordering, per-packet create/destroy table |
| `protocol-outgoing.md` | Every client→server packet and what triggers it |
| `protocol-transport.md` | Socket read, framing, encryption, threading, plugin interception |

The remaining 23 files are one map per partition of the tree.

`01-PROTOCOL.md` is a superseded first attempt: a single agent tried to write the whole
reference in one pass and stopped mid-way at `0x90`. It is kept only for its section 1,
the complete 255-entry length table, which is not duplicated elsewhere. For any packet,
use `01-HANDLERS-a/b/c.md` instead.

## Known inaccuracies

Found by spot-checking against the source. Others of the same kind are likely.

| Where | Says | Actually |
| --- | --- | --- |
| `00-INDEX.md`, `02-IGNORE-LIST.md` | `Game/GameController.cs` | `src/ClassicUO.Client/GameController.cs` — client root, not under `Game/`. Line numbers are correct |
| `00-INDEX.md` | serials minted client-side "in the same namespace the server owns", citing spellbook pages | `PacketHandlers.cs:4830` creates them with the spell index as serial but does **not** file them in `World.Items`. `03-LIFETIME.md:62-70` states this correctly |
| `00-INDEX.md` | the frame runs `World.Update` unconditionally | `Scene.Update()` is gated on `drawScene` (`GameController.cs:483`), so there are frames where packets dispatch and the cull does not run |

Treat a path or a claim as a pointer to verify, not as the answer. The line numbers
have held up everywhere they were checked; the prose around them has not always.
