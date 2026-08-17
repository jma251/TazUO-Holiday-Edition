# 01 — Protocol Reference

TazUO Holiday Edition, branch `legacy`. All paths relative to `/home/user/TazUO-Holiday-Edition/`.
Unqualified `PacketHandlers.cs` means `src/ClassicUO.Client/Network/PacketHandlers.cs`.

Contents:

1. [Length table — the complete vocabulary](#1-length-table--the-complete-vocabulary)
2. [Handler reference](#2-handler-reference)
3. [The ignore list](#3-the-ignore-list)
4. [Object lifetime by packet](#4-object-lifetime-by-packet)
5. [Outgoing](#5-outgoing)
6. [Transport](#6-transport)

Conventions used throughout:

- `V` = variable length: `PacketsTable` holds `-1`, `GetPacketInfo` (`PacketHandlers.cs:214-229`) reads a
  big-endian u16 at bytes 1-2 as the header-inclusive total, and sets `packetOffset = 3`.
- `F=n` = fixed length `n` bytes including the id byte; `packetOffset = 1`.
- `AnalyzePacket` (`PacketHandlers.cs:181-195`) seeks the `StackDataReader` to `packetOffset`, so every
  offset quoted in §2 is an **absolute** byte offset into the packet, id byte included.
- `p[n]` is `StackDataReader`'s indexer, which **ignores its argument and always returns `_data[0]`**
  (`ClassicUO.IO/StackDataReader.cs:39`) — i.e. the packet id. Several handlers use it deliberately as a
  packet-id discriminator when one method is registered for several ids.

---

## 1. Length table — the complete vocabulary

256 ids exist. `PacketsTable._packetsTable` (`PacketsTable.cs:39`) is a `short[255]` covering `0x00`-`0xFE`;
`GetPacketLength` (`PacketsTable.cs:298-301`) returns `-1` for `id >= 0xFF`. The final array slot is
commented `// ff` but is index 254 = `0xFE` — an off-by-one in the comments only, masked by the guard.

`PacketsTable.AdjustPacketSizeByVersion` (`PacketsTable.cs:303-428`) rewrites 25 slots in place, once, from
`Client.cs:192`. It is static mutable state: one client version per process.

Totals: **119 ids have a registered handler**, **2 more are registered conditionally** (`0x3F`, `0x40`, only
when `UltimaLive.Enable()` runs, `UltimaLive.cs:78-83`), **135 have no handler at all**.

An id with no handler is **not** a framing failure. `AnalyzePacket` (`PacketHandlers.cs:186`) looks up
`_handlers[data[0]]`, finds `null`, and returns; the bytes were already dequeued at `PacketHandlers.cs:140`
using the table length, so the stream stays aligned. Unhandled ids are silently discarded, not logged as
unknown, and still counted in `Statistics.TotalPacketsReceived` (`PacketHandlers.cs:152`,
`GameController.cs:144`).

A number of unhandled ids are **client→server only** in this codebase — the client emits them and never
expects them inbound. They are marked as such and cross-referenced to §5.

| ID | Table len | Version override | Status | Handler |
|---|---|---|---|---|
| `0x00` | F=104 (0x68) | `>=CV_70180` → 0x6A; `<CV_70180` → 0x68 | unhandled | client→server only (see §5) |
| `0x01` | F=5 (0x05) | — | unhandled | — |
| `0x02` | F=7 (0x07) | — | unhandled | client→server only (see §5) |
| `0x03` | V (-1) | — | **HANDLED** | `ClientTalk` |
| `0x04` | F=2 (0x02) | — | unhandled | — |
| `0x05` | F=5 (0x05) | — | unhandled | client→server only (see §5) |
| `0x06` | F=5 (0x05) | — | unhandled | client→server only (see §5) |
| `0x07` | F=7 (0x07) | — | unhandled | client→server only (see §5) |
| `0x08` | F=14 (0x0E) | `>=CV_6017` → 0x0F; `<CV_6017` → 0x0E | unhandled | client→server only (see §5) |
| `0x09` | F=5 (0x05) | — | unhandled | client→server only (see §5) |
| `0x0A` | F=11 (0x0B) | — | unhandled | — |
| `0x0B` | F=266 (0x10A) | `>=CV_500A` → 0x07; `<CV_500A` → 0x10A | **HANDLED** | `Damage` |
| `0x0C` | V (-1) | — | unhandled | — |
| `0x0D` | F=3 (0x03) | — | unhandled | — |
| `0x0E` | V (-1) | — | unhandled | — |
| `0x0F` | F=61 (0x3D) | — | unhandled | — |
| `0x10` | F=215 (0xD7) | — | unhandled | — |
| `0x11` | V (-1) | — | **HANDLED** | `CharacterStatus` |
| `0x12` | V (-1) | — | unhandled | client→server only (see §5) |
| `0x13` | F=10 (0x0A) | — | unhandled | client→server only (see §5) |
| `0x14` | F=6 (0x06) | — | unhandled | — |
| `0x15` | F=9 (0x09) | — | **HANDLED** | `FollowR` |
| `0x16` | F=1 (0x01) | `>=CV_500A` → -1; `<CV_500A` → 0x01 | **HANDLED** | `NewHealthbarUpdate` |
| `0x17` | V (-1) | — | **HANDLED** | `NewHealthbarUpdate` |
| `0x18` | V (-1) | — | unhandled | — |
| `0x19` | V (-1) | — | unhandled | — |
| `0x1A` | V (-1) | — | **HANDLED** | `UpdateItem` |
| `0x1B` | F=37 (0x25) | — | **HANDLED** | `EnterWorld` |
| `0x1C` | V (-1) | — | **HANDLED** | `Talk` |
| `0x1D` | F=5 (0x05) | — | **HANDLED** | `DeleteObject` |
| `0x1E` | F=4 (0x04) | — | unhandled | — |
| `0x1F` | F=8 (0x08) | — | unhandled | — |
| `0x20` | F=19 (0x13) | — | **HANDLED** | `UpdatePlayer` |
| `0x21` | F=8 (0x08) | — | **HANDLED** | `DenyWalk` |
| `0x22` | F=3 (0x03) | — | **HANDLED** | `ConfirmWalk` |
| `0x23` | F=26 (0x1A) | — | **HANDLED** | `DragAnimation` |
| `0x24` | F=7 (0x07) | `>=CV_7090` → 0x09; `<CV_7090` → 0x07 | **HANDLED** | `OpenContainer` |
| `0x25` | F=20 (0x14) | `>=CV_6017` → 0x15; `<CV_6017` → 0x14 | **HANDLED** | `UpdateContainedItem` |
| `0x26` | F=5 (0x05) | — | unhandled | — |
| `0x27` | F=2 (0x02) | — | **HANDLED** | `DenyMoveItem` |
| `0x28` | F=5 (0x05) | — | **HANDLED** | `EndDraggingItem` |
| `0x29` | F=1 (0x01) | — | **HANDLED** | `DropItemAccepted` |
| `0x2A` | F=5 (0x05) | — | unhandled | — |
| `0x2B` | F=2 (0x02) | — | unhandled | — |
| `0x2C` | F=2 (0x02) | — | **HANDLED** | `DeathScreen` |
| `0x2D` | F=17 (0x11) | — | **HANDLED** | `MobileAttributes` |
| `0x2E` | F=15 (0x0F) | — | **HANDLED** | `EquipItem` |
| `0x2F` | F=10 (0x0A) | — | **HANDLED** | `Swing` |
| `0x30` | F=5 (0x05) | — | unhandled | — |
| `0x31` | F=1 (0x01) | `>=CV_500A` → -1; `<CV_500A` → 0x01 | unhandled | — |
| `0x32` | F=2 (0x02) | — | **HANDLED** | `Unknown_0x32` |
| `0x33` | F=2 (0x02) | — | unhandled | — |
| `0x34` | F=10 (0x0A) | — | unhandled | client→server only (see §5) |
| `0x35` | F=653 (0x28D) | — | unhandled | — |
| `0x36` | V (-1) | — | unhandled | — |
| `0x37` | F=8 (0x08) | — | unhandled | — |
| `0x38` | F=7 (0x07) | — | **HANDLED** | `Pathfinding` |
| `0x39` | F=9 (0x09) | — | unhandled | — |
| `0x3A` | V (-1) | — | **HANDLED** | `UpdateSkills` |
| `0x3B` | V (-1) | — | **HANDLED** | `CloseVendorInterface` |
| `0x3C` | V (-1) | — | **HANDLED** | `UpdateContainedItems` |
| `0x3D` | F=2 (0x02) | — | unhandled | — |
| `0x3E` | F=37 (0x25) | — | unhandled | — |
| `0x3F` | V (-1) | — | **COND** | `UltimaLive.OnUltimaLivePacket` — registered only by `UltimaLive.Enable()` (`UltimaLive.cs:81-82`) |
| `0x40` | F=201 (0xC9) | — | **COND** | `UltimaLive.OnUpdateTerrainPacket` — registered only by `UltimaLive.Enable()` (`UltimaLive.cs:81-82`) |
| `0x41` | V (-1) | — | unhandled | — |
| `0x42` | V (-1) | — | unhandled | — |
| `0x43` | F=553 (0x229) | — | unhandled | — |
| `0x44` | F=713 (0x2C9) | — | unhandled | — |
| `0x45` | F=5 (0x05) | — | unhandled | — |
| `0x46` | V (-1) | — | unhandled | — |
| `0x47` | F=11 (0x0B) | — | unhandled | — |
| `0x48` | F=73 (0x49) | — | unhandled | — |
| `0x49` | F=93 (0x5D) | — | unhandled | — |
| `0x4A` | F=5 (0x05) | — | unhandled | — |
| `0x4B` | F=9 (0x09) | — | unhandled | — |
| `0x4C` | V (-1) | — | unhandled | — |
| `0x4D` | V (-1) | — | unhandled | — |
| `0x4E` | F=6 (0x06) | — | **HANDLED** | `PersonalLightLevel` |
| `0x4F` | F=2 (0x02) | — | **HANDLED** | `LightLevel` |
| `0x50` | V (-1) | — | unhandled | — |
| `0x51` | V (-1) | — | unhandled | — |
| `0x52` | V (-1) | — | unhandled | — |
| `0x53` | F=2 (0x02) | — | **HANDLED** | `ReceiveLoginRejection` |
| `0x54` | F=12 (0x0C) | — | **HANDLED** | `PlaySoundEffect` |
| `0x55` | F=1 (0x01) | — | **HANDLED** | `LoginComplete` |
| `0x56` | F=11 (0x0B) | — | **HANDLED** | `MapData` |
| `0x57` | F=110 (0x6E) | — | unhandled | — |
| `0x58` | F=106 (0x6A) | — | unhandled | — |
| `0x59` | V (-1) | — | unhandled | — |
| `0x5A` | V (-1) | — | unhandled | — |
| `0x5B` | F=4 (0x04) | — | **HANDLED** | `SetTime` |
| `0x5C` | F=2 (0x02) | — | unhandled | — |
| `0x5D` | F=73 (0x49) | — | unhandled | client→server only (see §5) |
| `0x5E` | V (-1) | — | unhandled | — |
| `0x5F` | F=49 (0x31) | — | unhandled | — |
| `0x60` | F=5 (0x05) | — | unhandled | — |
| `0x61` | F=9 (0x09) | — | unhandled | — |
| `0x62` | F=15 (0x0F) | — | unhandled | — |
| `0x63` | F=13 (0x0D) | — | unhandled | — |
| `0x64` | F=1 (0x01) | — | unhandled | — |
| `0x65` | F=4 (0x04) | — | **HANDLED** | `SetWeather` |
| `0x66` | V (-1) | — | **HANDLED** | `BookData` |
| `0x67` | F=21 (0x15) | — | unhandled | — |
| `0x68` | V (-1) | — | unhandled | — |
| `0x69` | V (-1) | — | unhandled | — |
| `0x6A` | F=3 (0x03) | — | unhandled | — |
| `0x6B` | F=9 (0x09) | — | unhandled | — |
| `0x6C` | F=19 (0x13) | — | **HANDLED** | `TargetCursor` |
| `0x6D` | F=3 (0x03) | — | **HANDLED** | `PlayMusic` |
| `0x6E` | F=14 (0x0E) | — | **HANDLED** | `CharacterAnimation` |
| `0x6F` | V (-1) | — | **HANDLED** | `SecureTrading` |
| `0x70` | F=28 (0x1C) | — | **HANDLED** | `GraphicEffect` |
| `0x71` | V (-1) | — | **HANDLED** | `BulletinBoardData` |
| `0x72` | F=5 (0x05) | — | **HANDLED** | `Warmode` |
| `0x73` | F=2 (0x02) | — | **HANDLED** | `Ping` |
| `0x74` | V (-1) | — | **HANDLED** | `BuyList` |
| `0x75` | F=35 (0x23) | — | unhandled | client→server only (see §5) |
| `0x76` | F=16 (0x10) | — | unhandled | — |
| `0x77` | F=17 (0x11) | — | **HANDLED** | `UpdateCharacter` |
| `0x78` | V (-1) | — | **HANDLED** | `UpdateObject` |
| `0x79` | F=9 (0x09) | — | unhandled | — |
| `0x7A` | V (-1) | — | unhandled | — |
| `0x7B` | F=2 (0x02) | — | unhandled | — |
| `0x7C` | V (-1) | — | **HANDLED** | `OpenMenu` |
| `0x7D` | F=13 (0x0D) | — | unhandled | client→server only (see §5) |
| `0x7E` | F=2 (0x02) | — | unhandled | — |
| `0x7F` | V (-1) | — | unhandled | — |
| `0x80` | F=62 (0x3E) | — | unhandled | client→server only (see §5) |
| `0x81` | V (-1) | — | unhandled | — |
| `0x82` | F=2 (0x02) | — | **HANDLED** | `ReceiveLoginRejection` |
| `0x83` | F=39 (0x27) | — | unhandled | client→server only (see §5) |
| `0x84` | F=69 (0x45) | — | unhandled | — |
| `0x85` | F=2 (0x02) | — | **HANDLED** | `ReceiveLoginRejection` |
| `0x86` | V (-1) | — | **HANDLED** | `UpdateCharacterList` |
| `0x87` | V (-1) | — | unhandled | — |
| `0x88` | F=66 (0x42) | — | **HANDLED** | `OpenPaperdoll` |
| `0x89` | V (-1) | — | **HANDLED** | `CorpseEquipment` |
| `0x8A` | V (-1) | — | unhandled | — |
| `0x8B` | V (-1) | — | unhandled | — |
| `0x8C` | F=11 (0x0B) | — | **HANDLED** | `ReceiveServerRelay` |
| `0x8D` | V (-1) | — | unhandled | — |
| `0x8E` | V (-1) | — | unhandled | — |
| `0x8F` | V (-1) | — | unhandled | — |
| `0x90` | F=19 (0x13) | — | **HANDLED** | `DisplayMap` |
| `0x91` | F=65 (0x41) | — | unhandled | client→server only (see §5) |
| `0x92` | V (-1) | — | unhandled | — |
| `0x93` | F=99 (0x63) | — | **HANDLED** | `OpenBook` |
| `0x94` | V (-1) | — | unhandled | — |
| `0x95` | F=9 (0x09) | — | **HANDLED** | `DyeData` |
| `0x96` | V (-1) | — | unhandled | — |
| `0x97` | F=2 (0x02) | — | **HANDLED** | `MovePlayer` |
| `0x98` | V (-1) | — | **HANDLED** | `UpdateName` |
| `0x99` | F=26 (0x1A) | `>=CV_7090` → 0x1E; `<CV_7090` → 0x1A | **HANDLED** | `MultiPlacement` |
| `0x9A` | V (-1) | — | **HANDLED** | `ASCIIPrompt` |
| `0x9B` | F=258 (0x102) | — | unhandled | client→server only (see §5) |
| `0x9C` | F=309 (0x135) | — | unhandled | — |
| `0x9D` | F=51 (0x33) | — | unhandled | — |
| `0x9E` | V (-1) | — | **HANDLED** | `SellList` |
| `0x9F` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xA0` | F=3 (0x03) | — | unhandled | client→server only (see §5) |
| `0xA1` | F=9 (0x09) | — | **HANDLED** | `UpdateHitpoints` |
| `0xA2` | F=9 (0x09) | — | **HANDLED** | `UpdateMana` |
| `0xA3` | F=9 (0x09) | — | **HANDLED** | `UpdateStamina` |
| `0xA4` | F=149 (0x95) | — | unhandled | — |
| `0xA5` | V (-1) | — | **HANDLED** | `OpenUrl` |
| `0xA6` | V (-1) | — | **HANDLED** | `TipWindow` |
| `0xA7` | F=4 (0x04) | — | unhandled | client→server only (see §5) |
| `0xA8` | V (-1) | — | **HANDLED** | `ServerListReceived` |
| `0xA9` | V (-1) | — | **HANDLED** | `ReceiveCharacterList` |
| `0xAA` | F=5 (0x05) | — | **HANDLED** | `AttackCharacter` |
| `0xAB` | V (-1) | — | **HANDLED** | `TextEntryDialog` |
| `0xAC` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xAD` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xAE` | V (-1) | — | **HANDLED** | `UnicodeTalk` |
| `0xAF` | F=13 (0x0D) | — | **HANDLED** | `DisplayDeath` |
| `0xB0` | V (-1) | — | **HANDLED** | `OpenGump` |
| `0xB1` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xB2` | V (-1) | — | **HANDLED** | `ChatMessage` |
| `0xB3` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xB4` | V (-1) | — | unhandled | — |
| `0xB5` | F=64 (0x40) | — | unhandled | client→server only (see §5) |
| `0xB6` | F=9 (0x09) | — | unhandled | — |
| `0xB7` | V (-1) | — | **HANDLED** | `Help` |
| `0xB8` | V (-1) | — | **HANDLED** | `CharacterProfile` |
| `0xB9` | F=3 (0x03) | `>=CV_60142` → 0x05; `<CV_60142` → 0x03 | **HANDLED** | `EnableLockedFeatures` |
| `0xBA` | F=6 (0x06) | `>=CV_7090` → 0x0A; `<CV_7090` → 0x06 | **HANDLED** | `DisplayQuestArrow` |
| `0xBB` | F=9 (0x09) | — | **HANDLED** | `UltimaMessengerR` |
| `0xBC` | F=3 (0x03) | — | **HANDLED** | `Season` |
| `0xBD` | V (-1) | — | **HANDLED** | `ClientVersion` |
| `0xBE` | V (-1) | — | **HANDLED** | `AssistVersion` |
| `0xBF` | V (-1) | — | **HANDLED** | `ExtendedCommand` |
| `0xC0` | F=36 (0x24) | — | **HANDLED** | `GraphicEffect` |
| `0xC1` | V (-1) | — | **HANDLED** | `DisplayClilocString` |
| `0xC2` | V (-1) | — | **HANDLED** | `UnicodePrompt` |
| `0xC3` | V (-1) | — | unhandled | — |
| `0xC4` | F=6 (0x06) | — | **HANDLED** | `Semivisible` |
| `0xC5` | F=203 (0xCB) | — | unhandled | — |
| `0xC6` | F=1 (0x01) | — | **HANDLED** | `InvalidMapEnable` |
| `0xC7` | F=49 (0x31) | — | **HANDLED** | `GraphicEffect` |
| `0xC8` | F=2 (0x02) | — | **HANDLED** | `ClientViewRange` |
| `0xC9` | F=6 (0x06) | — | unhandled | — |
| `0xCA` | F=6 (0x06) | — | **HANDLED** | `GetUserServerPingGodClientR` |
| `0xCB` | F=7 (0x07) | — | **HANDLED** | `GlobalQueCount` |
| `0xCC` | V (-1) | — | **HANDLED** | `DisplayClilocString` |
| `0xCD` | F=1 (0x01) | — | unhandled | — |
| `0xCE` | V (-1) | — | **HANDLED** | `EnhancedPacketHandler.Handle` |
| `0xCF` | F=78 (0x4E) | — | unhandled | — |
| `0xD0` | V (-1) | — | **HANDLED** | `ConfigurationFileR` |
| `0xD1` | F=2 (0x02) | — | **HANDLED** | `Logout` |
| `0xD2` | F=25 (0x19) | — | **HANDLED** | `UpdateCharacter` |
| `0xD3` | V (-1) | — | **HANDLED** | `UpdateObject` |
| `0xD4` | V (-1) | — | **HANDLED** | `OpenBook` |
| `0xD5` | V (-1) | `>=CV_7010400` → 0x09; `else` → -1 unchanged | unhandled | — |
| `0xD6` | V (-1) | — | **HANDLED** | `MegaCliloc` |
| `0xD7` | V (-1) | — | **HANDLED** | `GenericAOSCommandsR` |
| `0xD8` | V (-1) | — | **HANDLED** | `CustomHouse` |
| `0xD9` | F=268 (0x10C) | — | unhandled | — |
| `0xDA` | V (-1) | — | unhandled | — |
| `0xDB` | V (-1) | — | **HANDLED** | `CharacterTransferLog` |
| `0xDC` | F=9 (0x09) | — | **HANDLED** | `OPLInfo` |
| `0xDD` | V (-1) | — | **HANDLED** | `OpenCompressedGump` |
| `0xDE` | V (-1) | — | **HANDLED** | `UpdateMobileStatus` |
| `0xDF` | V (-1) | — | **HANDLED** | `BuffDebuff` |
| `0xE0` | V (-1) | — | unhandled | — |
| `0xE1` | V (-1) | `>=CV_5090` → -1; `<CV_5090` → 0x09 | unhandled | — |
| `0xE2` | F=10 (0x0A) | — | **HANDLED** | `NewCharacterAnimation` |
| `0xE3` | V (-1) | `>=CV_6013` → -1; `<CV_6013` → 0x4D | **HANDLED** | `KREncryptionResponse` |
| `0xE4` | V (-1) | — | unhandled | — |
| `0xE5` | V (-1) | — | **HANDLED** | `DisplayWaypoint` |
| `0xE6` | F=5 (0x05) | `>=CV_6013` → 0x05; `<CV_6013` → -1 | **HANDLED** | `RemoveWaypoint` |
| `0xE7` | F=12 (0x0C) | `>=CV_6013` → 0x0C; `<CV_6013` → -1 | unhandled | — |
| `0xE8` | F=13 (0x0D) | `>=CV_6013` → 0x0D; `<CV_6013` → -1 | unhandled | — |
| `0xE9` | F=75 (0x4B) | `>=CV_6013` → 0x4B; `<CV_6013` → -1 | unhandled | — |
| `0xEA` | F=3 (0x03) | `>=CV_6013` → 0x03; `<CV_6013` → -1 | unhandled | — |
| `0xEB` | V (-1) | — | unhandled | — |
| `0xEC` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xED` | V (-1) | — | unhandled | client→server only (see §5) |
| `0xEE` | F=10 (0x0A) | `>=CV_7000` → 0x0A; `<CV_7000` → -1 | unhandled | — |
| `0xEF` | F=21 (0x15) | `>=CV_7000` → 0x15; `<CV_7000` → 0x15 | unhandled | client→server only (see §5) |
| `0xF0` | V (-1) | — | **HANDLED** | `KrriosClientSpecial` |
| `0xF1` | F=9 (0x09) | `>=CV_7090` → 0x09; `<CV_7090` → -1 (0x09 if >=CV_6060) | **HANDLED** | `FreeshardListR` |
| `0xF2` | F=25 (0x19) | `>=CV_7090` → 0x19; `<CV_7090` → -1 | unhandled | — |
| `0xF3` | F=26 (0x1A) | `>=CV_7090` → 0x1A; `<CV_7090` → 0x18 | **HANDLED** | `UpdateItemSA` |
| `0xF4` | V (-1) | — | unhandled | — |
| `0xF5` | F=21 (0x15) | — | **HANDLED** | `DisplayMap` |
| `0xF6` | V (-1) | — | **HANDLED** | `BoatMoving` |
| `0xF7` | V (-1) | — | **HANDLED** | `PacketList` |
| `0xF8` | F=106 (0x6A) | — | unhandled | client→server only (see §5) |
| `0xF9` | V (-1) | — | unhandled | — |
| `0xFA` | V (-1) | `>=CV_706400` → 0x01; `else` → -1 unchanged | unhandled | client→server only (see §5) |
| `0xFB` | V (-1) | `>=CV_706400` → 0x02; `else` → -1 unchanged | unhandled | client→server only (see §5) |
| `0xFC` | V (-1) | — | unhandled | — |
| `0xFD` | V (-1) | — | unhandled | — |
| `0xFE` | V (-1) | — | unhandled | — |
| `0xFF` | V (-1) — no slot; guard `PacketsTable.cs:300` | — | unhandled | — |

Notes on the table:

- `0x16` is `F=1` below `CV_500A` (`PacketsTable.cs:314`) — the handler explicitly refuses to parse it there
  (`PacketHandlers.cs:807-810`).
- `0xEE`/`0xEF` are assigned twice (`PacketsTable.cs:359-360` then `:381-382`); the `CV_7000` block is an
  unconditional if/else and always wins.
- `0xF1` is assigned in both the `CV_6060` block (`PacketsTable.cs:361/367`) and the `CV_7090` block
  (`:396/405`); the later one wins.
- `0xFA`/`0xFB`/`0xD5` are only ever *raised* (`PacketsTable.cs:418-427`); there is no else branch, so on an
  older client they keep the base `-1`.
- `0xCE` is TazUO-specific (`EnhancedPacketHandler.EPID`, `EnhancedPacketHandler.cs:13`), not stock UO.

---

## 2. Handler reference

One section per handled id, ordered by id. Ids sharing a method are documented separately because the
method branches on `p[0]`. Offsets are absolute, id byte included.

### `0x03` — `ClientTalk` (`PacketHandlers.cs:532`, registered `:239`)

**Len** V. **Purpose** server echo of a talk/subcommand byte.

**Fields** — `[3]` u8 subcommand (`:534`), switched on `0x78` / `0x3C` / `0x25` / `0x2E`. No further byte is read.

**Mutates / creates / destroys / triggers** — nothing.

**Ignores** — all four switch cases have empty bodies (`:536-546`); any other value falls out of the switch. Every `0x03` is a no-op. No `World.InGame` guard, because nothing is touched.

### `0x0B` — `Damage` (`PacketHandlers.cs:550`, registered `:240`)

**Len** `F=0x07` on `>=CV_500A`, `F=0x10A` below (`PacketsTable.cs:307/313`). The handler reads 6 body bytes either way, so on a pre-5.0.0a client 259 further bytes are dequeued and discarded.
**Purpose** entity took N damage; float a damage number over it.

**Fields**
- `[1..4]` u32BE serial (`:557`) → `World.Get` (`World.cs:528`)
- `[5..6]` u16BE damage (`:561`), read only if `World.Get` returned non-null; passed as `int` to `AddDamage(uint,int)`

**Mutates**
- `WorldTextManager.cs:137` `_damages[serial] = new OverheadDamage(...)`
- `GameObject.cs:141` `entity._averageOverTime ??= new AverageOverTime(15s)`; `:143` `AddValue(Time.Ticks, damage)`
- `EntityTextContainer.cs:139` `text_obj.Time = Time.Ticks + 1500`; `:141` `_messages.AddToFront`
- `EntityTextContainer.cs:137` `World.Journal.Add(..., TextType.CLIENT, MessageType.Damage)`

**Creates** — `OverheadDamage` (`WorldTextManager.cs:136`); `TextObject` from the static `QueuedPool<TextObject>` cap 1000 (`EntityTextContainer.cs:114` → `TextObject.cs:83-86`); `TextBox` (`EntityTextContainer.cs:135`); `AverageOverTime` (`GameObject.cs:141`).

**Destroys / pools** — over 10 messages, the oldest `TextObject` is `RemoveFromBack()?.Destroy()`'d (`EntityTextContainer.cs:145`), disposing its `TextBox` (`TextObject.cs:100`) and returning it to the pool (`:104`). No entity is created or destroyed.

**Triggers** — `EventSink.InvokeOnEntityDamage` (`:566` → `EventSink.cs:98`); no in-repo subscribers. No outgoing packet.

**Ignores / partial**
- `:552-555` `World.Player == null` → return, serial and damage never read.
- `:559` `World.Get(serial) == null` → the damage u16 is never read. Damage to an entity culled for distance by the sweep (`World.cs:356/438`) is lost.
- `:563` `damage == 0` → no text, no journal line, no event.
- `EntityTextContainer.cs:132` dereferences `ProfileManager.CurrentProfile.ShowDPS` unguarded after the null-tolerant hue fallback at `:116-131`.

### `0x11` — `CharacterStatus` (`PacketHandlers.cs:571`, registered `:241`)

**Len** V. **Purpose** full status block: everything for the player, name+hits for anything else.

**Fields**
- `[3..6]` u32BE serial (`:578`); `[7..36]` ASCII(30) name, written with no NUL trim (`:587`); `[37..38]` u16BE Hits (`:588`); `[39..40]` u16BE HitsMax (`:589`)
- `[41]` bool IsRenamable — only if `SerialHelper.IsMobile(serial)` (`:605`); `[42]` u8 type (`:606`); `[43]` bool IsFemale — only if `type>0 && p.Position+1 <= p.Length` (`:608-610`)
- player only: `[44..49]` u16 str/dex/int (`:625-627`); `[50..57]` Stamina, StaminaMax, Mana, ManaMax (`:628-631`); `[58..61]` u32BE Gold (`:632`); `[62..63]` u16BE→short PhysicalResistance (`:633`); `[64..65]` u16BE Weight (`:634`)
- `type>=5` block is read **before** the `type>=3` and `type>=4` blocks: u16 WeightMax (`:705`), u8 race (`:706`)
- `type>=3`: u16→short StatsCap (`:731`), u8 Followers (`:732`), u8 FollowersMax (`:733`)
- `type>=4`: u16 fire/cold/poison/energy resist (`:738-741`), u16 Luck (`:742`), u16→short DamageMin (`:743`), DamageMax (`:744`), u32 TithingPoints (`:745`)
- `type>=6`: fifteen consecutive u16BE, each individually guarded `p.Position + 2 > p.Length ? 0 : read` (`:750-779`)

**Mutates** — `Entity.Name` (`Entity.cs:85`, written `:587`); `Entity.Hits` (setter `Entity.cs:67-79`, fires `EventSink.InvokeOnPlayerStatChange` at `Entity.cs:73` only for `PlayerMobile`), written `:588`; `Entity.HitsMax` (`Entity.cs:81`) `:589`; `HitsRequest = Received` `:593`; `Mobile.IsRenamable` (`Mobile.cs:227`) `:605`; `Mobile.IsFemale` (`Mobile.cs:226`) `:610`; `Mobile.Stamina/StaminaMax/Mana/ManaMax` (`Mobile.cs:233-236`) `:628-631`; `PlayerMobile.Gold` (`:115`) `:632`, `PhysicalResistance` (`:136`) `:633`, `Weight` (`:148`) `:634`; `Strength/Dexterity/Intelligence` (`PlayerMobile.cs:143/106/119`) written at `:699-701` — **after** the delta messages are printed; `WeightMax` (`:149`) at `:705` from the wire or `:719`/`:725` computed locally; `Mobile.Race` (`Mobile.cs:231`) `:713`; `StatsCap/Followers/FollowersMax` `:731-733`; `Fire/Cold/Poison/EnergyResistance`, `Luck`, `DamageMin/Max`, `TithingPoints` `:738-745`; the fifteen caps/increases `:750-779`.

**Creates / destroys** — nothing; the entity must already exist.

**Triggers** — `Client.Game.SetWindowTitle` (`:618`); `TitleBarStatsManager.ForceUpdate()` (`:621` → `TitleBarStatsManager.cs:96-99, 26`); `GameActions.Print` for str/dex/int deltas (`:652, :668, :684`); `EventSink.InvokeOnPlayerStatChange` from the Hits setter; `UoAssist.SignalHits/SignalStamina/SignalMana` (`:786-788`); `TitleBarStatsManager.UpdateTitleBar` (`:789`).

**Ignores / partial**
- `:573` `World.Player == null` → return; the packet is dropped even for other entities.
- `:581` `World.Get(serial) == null` → return. `World.Get` also returns null for `IsDestroyed` (`World.cs:551-554`).
- `:602` mobile serial that resolved to a non-`Mobile` → return **after** Name, Hits, HitsMax and HitsRequest were already written.
- `:605` non-mobile serial → IsRenamable, type and everything after are never read; the item keeps only name/hits.
- `:608` `type == 0` or fewer than 1 byte left → IsFemale and every stat field skipped.
- `:612` `mobile != World.Player` → every stat the server sent is skipped; only Name/Hits/HitsMax/IsRenamable/IsFemale survive.
- `:636-640` stat-change messages suppressed unless `Strength != 0 && CurrentProfile != null && ShowStatsChangedMessage`.
- `:717-726` `type < 5` → the server's WeightMax is ignored and synthesized: `7*(Str>>1)+40` for `>=CV_500A`, else `Str*4+25`.
- `:708-711` race byte 0 clamped up to 1 before the `RaceType` cast.
- `:750-779` every `type>=6` field clamps to 0 when fewer than 2 bytes remain — a truncated packet silently zeroes the remaining caps.
- `:614-616` window title only updated when the new name is non-empty and differs from the pre-read `oldName`.

### `0x15` — `FollowR` (`PacketHandlers.cs:794`, registered `:242`)

**Len** `F=9`. **Purpose** one mobile now follows another.

**Fields** — `[1..4]` u32BE tofollow (`:796`), `[5..8]` u32BE isfollowing (`:797`). Both assigned to locals and discarded.

**Mutates / creates / destroys / triggers** — nothing. The body is two local assignments. No guard exists because nothing is written.

### `0x16` — `NewHealthbarUpdate` (`PacketHandlers.cs:800`, registered `:243`)

**Len** `-1` on `>=CV_500A`, `F=1` below (`PacketsTable.cs:308/314`). Variable ⇒ offset 3.
**Purpose** toggle named healthbar status flags (poison, yellow bar) on one mobile.

**Fields**
- `[3..6]` u32BE serial → `World.Mobiles.Get` (`:812`) — `Dictionary.TryGetValue` (`EntityCollection.cs:40-45`), which does **not** filter `IsDestroyed`, unlike `World.Get`
- `[7..8]` u16BE count (`:819`)
- per entry: u16BE type (`:823`), 1 byte bool enabled (`:824`). No bounds check of `count` against remaining bytes; `StackDataReader` silently returns 0 past the end (`StackDataReader.cs:69-75, 178-186`).

**Mutates** — `Mobile._isSA_Poisoned` (`Mobile.cs:272`) via `SetSAPoison` from `:832`/`:843`; `Flags |= Flags.Poisoned` `:836`; `&= ~Poisoned` `:847`; `|= YellowBar` `:855`; `&= ~YellowBar` `:859`. Nothing is added to or removed from `World.Mobiles`/`World.Items`.

**Creates / destroys / triggers** — nothing. No outgoing packet, no gump, no `RequestUpdateContents`; healthbar gumps read `Mobile.Flags` on their own draw pass.

**Ignores / partial**
- `:802-805` `World.Player == null` → return.
- `:807-810` `p[0] == 0x16 && Client.Version < CV_500A` → return; the pre-500A form is 1 byte.
- `:814-817` unknown mobile → the whole entry list is discarded; no fallback to `World.Items`/`World.Get`.
- `:862-865` `type == 3` is parsed (3 bytes consumed) and deliberately ignored — empty branch commented `???`.
- any other `type` is consumed and silently ignored (no else).
- poison is written to **one of two different fields** by version: `SetSAPoison` on `>=CV_7000` (`:830`), the `Flags.Poisoned` bit below (`:841`). `Mobile.IsPoisoned` (`Mobile.cs:153-156`) reads only the version-matching one.
- `Flags` is a shared bitfield mutated with OR/AND-NOT; a following `0x77`/`0x78` that assigns `Flags` wholesale overwrites these bits.

### `0x17` — `NewHealthbarUpdate` (same method, registered `:244`)

**Len** V always. Identical body; the `p[0] == 0x16` guard at `:807` never fires, so the pre-500A refusal does not apply. Everything else as `0x16`.

### `0x1A` — `UpdateItem` (`PacketHandlers.cs:869`, registered `:245`)

**Len** V (`PacketsTable.cs:67`). **Purpose** describe an item (or mobile-serial object) on the ground.

**Fields** — a flag-driven layout; every optional field is gated on a high bit of an earlier field.
- `[3..6]` u32BE serial (`:876`). Bit `0x80000000` = "count follows": `serial &= 0x7FFFFFFF`, `count=1` as a marker (`:884-888`)
- `[7..8]` u16BE graphic (`:890`). Bit `0x8000` = "graphicInc follows": `graphic &= 0x7FFF` (`:892-894`)
- optional u8 graphicInc (`:895`); optional u16BE count (`:900`), else count incremented 0→1 (`:904`)
- u16BE x (`:907`); bit `0x8000` = "direction follows", `x &= 0x7FFF` (`:909-913`)
- u16BE y (`:915`); bit `0x8000` = "hue follows", `y &= 0x7FFF` (`:917-921`); bit `0x4000` (tested after that mask) = "flags follow", `y &= 0x3FFF` (`:923-927`)
- optional u8 direction (`:931`); i8 z (`:934`); optional u16BE hue (`:938`); optional u8 flags (`:943`)
- derived, not on the wire: `type = 2` when `graphic >= 0x4000` (`:949-953`). graphic is **not** masked here; the `0x4000` bit is stripped later at `:6663`
- the same local `count` is passed twice to `UpdateGameObject`, as both `count` and the unused `UNK` argument (`:955-969`); `UNK_2` is the literal 1

**Mutates** (all inside `UpdateGameObject`, `:6545`)
- item, type 2: `IsMulti=true`, `WantUpdateMulti` recomputed from graphic/x/y/z/hue difference, `Graphic = graphic & 0x3FFF` (`:6656-6663`); otherwise `IsDamageable=(type==3)`, `IsMulti=false`, `Graphic=graphic` (`:6667-6669`)
- `item.X/Y/Z` (`:6672-6674`); `LightID = (byte)direction` (`:6675`); `Layer = (Layer)direction` when `graphic==0x2006` (`:6679`); Hue via `FixHue` (`:6682` → `Entity.cs:133`; low-14-bit values `>=0x0BB8` collapse to 1, `Entity.cs:121-124`); `Amount = count` (`:6689`); `Flags` (`:6690`); `Direction` (`:6691`, setter fires `OnDirectionChanged`, `Entity.cs:97-101`); `CheckGraphicChange` (`:6692` → `Item.cs:607-646`: corpses get `AnimIndex=99`, `UsedLayer`, `Direction &= 0x7F`, `Layer=(Layer)Direction` at `Item.cs:617-630`; multis call `LoadMulti` at `Item.cs:642`)
- new mobile: `Graphic = graphic+graphic_inc`, `Direction & Direction.Up`, `FixHue`, X, Y, Z, `Flags` (`:6604-6611`)
- existing mobile: X/Y/Z/Direction/IsRunning written and Steps cleared at `:6710-6715` (unknown position) or `:6720-6725` (EnqueueStep refused); `Graphic = graphic & 0x3FFF` (`:6729`), `FixHue` (`:6730`), `Flags` (`:6731`)
- `Mobile.Steps` appended by `EnqueueStep` (`Mobile.cs:340/348/358`); `LastStepTime` (`Mobile.cs:325`); `SetAnimation` writes 10 fields (`Mobile.cs:393-404`)
- `mobile.SetInWorldTile` (`:6770` → `GameObject.cs:265-272`) → `Chunk.AddGameObject` writes `TileChunk`/`TileCellX`/`TileCellY` (`Chunk.cs:178-180`), `PriorityZ` (`Chunk.cs:283`), `Tiles[x%8,y%8]` (`Chunk.cs:289`); `item.SetInWorldTile` at `:6800` (on-ground only)
- `ItemHold.UpdatedInWorld = true` (`:6585`); `Enabled=false`, `Dropped=false` (`:6794-6795`)
- `LoadMulti` (`Item.cs:335-605`) writes `MultiGraphic`, `MultiInfo`, `MultiDistanceBonus` (`Item.cs:581-584`), `house.Bounds` (`:586`), `HouseManager.RememberFootprint` (`:591`)
- `World.Items` / `World.Mobiles` insert (`World.cs:577` / `:601`)

**Creates** — `Item` from the pool (`World.cs:576`, `Item.cs:255-261`); `Mobile` from the pool (`World.cs:600`); `House` + `HouseManager` entry on first multi load (`Item.cs:344-345`); `Multi` components appended to `house.Components` (`Item.cs:335-584`).

**Destroys / pools** — `GetOrCreateItem`/`GetOrCreateMobile` evict a destroyed entry under the same serial and return it to the pool first (`World.cs:565-569` / `:589-593`); `World.RemoveItemFromContainer` for an existing contained item (`:6633` → `World.cs:617-653`); `house.ClearComponents` inside `LoadMulti` destroys every existing `Multi` (`Item.cs:359` → `House.cs:197`); `GameObject.RemoveFromTile` hands the chunk cell to `TNext ?? TPrevious` (`GameObject.cs:226-232`).

**Triggers** — `ContainerGump`/`PaperDollGump`/`ModernPaperdoll` `RequestUpdateContents` for the held item's container (`:6574-6582`); `GameActions.SingleClick` → `Send_ClickRequest` (`:6740`/`:6747`); `GameActions.RequestMobileStatus` → `Send_StatusRequest` (`:6777`); `EventSink.InvokeOnItemCreated`/`InvokeOnItemUpdated` (`:6695`/`:6697`), `InvokeOnCorpseCreated` (`:6807`); `World.Player.TryOpenCorpses` → `GameActions.DoubleClickQueued` (`:6810` → `PlayerMobile.cs:1449-1455`); `UoAssist.SignalAddMulti` (`Item.cs:635`); `MiniMapGump.RequestUpdateContents` (`Item.cs:593`); `GameScene.UpdateMaxDrawZ(true)` (`Item.cs:601`); `BoatMovingManager.ClearSteps` (`Item.cs:604`); `HouseDiagnostics.LogRangeProbe`/`LogHouseItemArrived` (`:6758`/`:6802`).

**Ignores / partial**
- `:871-874` `World.Player == null` → return.
- every optional field defaults when its bit is clear: count 1, direction/hue/flags 0 (`:884-943`).
- `:6649-6652` graphic_inc is **not** added when `graphic == 0x2006`; the equivalent line in the handler is commented out (`:946-947`).
- `:6594` a mobile serial with `type==3` is created as an **Item**, not a Mobile.
- `:6598-6601`, `:6617-6620`, `:6642-6645` a null from the create path returns with nothing applied.
- `:6684-6687` `count == 0` clamped up to 1.
- `:6703` when `serial == World.Player` the whole position/step block is skipped — `0x1A` never moves the player.
- `Mobile.cs:306-309` `EnqueueStep` refuses at `Steps.Count >= MAX_STEP_COUNT`, and the handler then snaps + clears (`:6718-6726`).
- `Mobile.cs:313-316` a step identical to the queued end position is swallowed (returns true, queues nothing) — the server's position is silently dropped.
- `:6734/:6738/:6745` `SingleClick` skipped when `obj.IsClicked` or the profile flag is off.
- `:6798` items only enter a world tile when `OnGround`.
- `Item.cs:633`, `:637-640` `LoadMulti` skipped unless `WantUpdateMulti`, and unless `MultiDistanceBonus == 0 || HouseManager.IsHouseInRange(...)`.
- `PlayerMobile.cs:1439-1447` auto-open-corpse refuses while targeting (`CorpseOpenOptions` 1/3) or hidden (2/3).

### `0x1B` — `EnterWorld` (`PacketHandlers.cs:972`, registered `:236`)

**Len** `F=0x25` (37) (`PacketsTable.cs:68`). **Purpose** the login handoff: serial, body, position, facing.

**Fields** — `[1..4]` u32BE serial (`:974`); `[5..8]` `Skip(4)` never read (`:978`); `[9..10]` u16BE Graphic (`:979`); `[11..12]` x (`:982`); `[13..14]` y (`:983`); `[15..16]` read as **u16BE then cast `(sbyte)`** → z (`:984`) — two bytes consumed, value truncated to the low byte; `[17]` u8 `& 0x7` → Direction (`:992`); `[18..36]` the remaining 19 bytes are never read.

**Mutates** — `World.Player = new PlayerMobile(serial)` (`World.cs:194`); `World.Mobiles[serial]` (`World.cs:195` via `EntityCollection.cs:59` — `Add` returns false and does nothing if the key exists); `Player.Graphic` (`:979`); `IsFemale`/`Race` via graphic (`Mobile.cs:1061-1103`, only for `0x0190-0x0193, 0x025D, 0x025E, 0x029A, 0x029B`); `World.MapIndex = 0` when `Map == null` (`:988`; setter `World.cs:106-161` → `InternalMapChangeClear(true)` `:113`, `LoadMap` `:148`, `new Map.Map` `:149`, `GameCursor.Graphic = 0xFFFF` `:155`, `UoAssist.SignalMapChanged` `:158`); `Player.X/Y/Z` (`GameObject.cs:267-269` via `SetInWorldTile`, `:991`); `IsPositionChanged` (`GameObject.cs:252`); `TileChunk`/`TileCellX`/`TileCellY` + `Chunk.Tiles` (`Chunk.cs:178-180`); `Player.Direction` (`Entity.cs:99`); `World.RangeSize.X/.Y` (`:993-994`); `World.Light.Overall` (`:1001`, `IsometricLight.cs:58-59`); `World.Season = Desolation` + `UpdateGraphicBySeason` on every object of every used chunk (`World.cs:208, :218`) when dead; `Entity.IsClicked` (`GameActions.cs:666` via `:1024`).

**Creates** — `PlayerMobile` (`World.cs:194`); `Map.Map` (`World.cs:141`/`:149`). No item or non-player mobile.

**Destroys / pools** — if `World.Player` was already non-null, `World.Clear()` runs first (`World.cs:191`): destroys every Mobile (`World.cs:924` → `:710`) and Item (`:929` → `:677`), disposes the player's `BaseHealthBarGump` (`:932`), `Items.Clear()`/`Mobiles.Clear()` (`:936-937`), `Player.Destroy()` (`:938`), `Map.Destroy()` (`:940`), resets Light (`:942-943`), `ClientLockedFeatures` (`:944`), Party (`:945`), `TargetManager.LastAttack` (`:946`), `MessageManager.PromptData` (`:947`), effects (`:948`), `CorpseManager` (`:950`), OPL (`:951`), `WMapManager` (`:952`), `HouseManager` (`:953`), Season/OldSeason (`:955-956`), Journal (`:958`), `WorldTextManager` (`:959`), `ActiveSpellIcons` (`:960`), `SkillsRequested` (`:962`). Those foreach loops pass `forceRemove` default false, so nothing is removed from the dictionaries or pooled there; the `Clear()` calls at `:936/:937` drop the entries wholesale without `ReturnToPool`. Separately, `InternalMapChangeClear(true)` (`World.cs:965`) force-removes and **pools** every Item and Mobile except the player and items rooted at the player (`World.cs:995` → `:681-683`; `World.cs:1015` → `:714-716`); multis are dropped from `HouseManager` (`World.cs:987`).

**Triggers** — `EventSink.InvokeOnPlayerCreated` (`World.cs:196`); `Audio.UpdateCurrentMusicVolume` (`:1007`); `Send_GameWindowSize` (`:1013`); `Send_Language` (`:1019`); `Send_ClientVersion` (`:1022`); `Send_ClickRequest` via `SingleClick` (`GameActions.cs:660`, from `:1024`); `Send_SkillsRequest` (`:1025`); `Send_ShowPublicHouseContent` (`:1037`); `Send_ToPlugins_AllSkills`/`_AllSpells` (`:1042-1043`); Direction setter → `TryOpenDoors` (`PlayerMobile.cs:1460-1477`), which LINQ-walks `World.Items.Values` and may `GameActions.OpenDoor()`; `Audio.PlayMusic(42)` when dead and `CanSeasonMusicTakeOver()` (`World.cs:230-232`).

**Ignores / partial**
- `:986` `World.MapIndex` is forced to 0 only when `World.Map == null`; otherwise the position is applied to whatever map is loaded and no switch happens.
- `:984` z read from two bytes and truncated by the `(sbyte)` cast.
- `:992` direction masked with `0x7` — the run/flag bits are dropped.
- `:996-999`/`:1003` custom light override only with a profile; `LightLevelType == 1` takes `Math.Min(existing Overall, profile level)` rather than the profile value.
- `:1009`/`:1011` `Send_GameWindowSize`/`Send_Language` skipped below `CV_200`; window size also needs a profile.
- `:1032-1035` `Send_ShowPublicHouseContent` skipped below `CV_70796` or with no profile.
- `:1027` season only changed when `IsDead`.
- `World.cs:189` a second `0x1B` on a live session tears the whole world down.
- `World.cs:183` `ProfileManager.Load` only when `CurrentProfile == null`.

### `0x1C` — `Talk` (`PacketHandlers.cs:1046`, registered `:246`)

**Len** V (`PacketsTable.cs:69`). **Purpose** ASCII speech/system text.

**Fields** — `[3..6]` u32BE serial (`:1048`); `[7..8]` u16BE graphic (`:1050`, used only in the SYSTEM-ack test); `[9]` u8 MessageType (`:1051`); `[10..11]` hue (`:1052`); `[12..13]` font (`:1053`); `[14..43]` ASCII(30) name (`:1054`); `[44..]` null-terminated ASCII text, only when `p.Length > 44` (`:1057-1061`), with `p.Seek(44)` at `:1059` re-anchoring; otherwise `string.Empty` (`:1064`).

**Mutates** — `entity.Name` (`Entity.cs:85`, written `:1098`) only when the entity exists **and** its name is currently null/empty; overhead `TextObject` appended (`MessageManager.cs:162`/`:297`); journal/overhead routing (`MessageManager.cs:219-229, 262-295, 317`). No `World.Items`/`World.Mobiles`/map state.

**Creates** — `TextObject` (`MessageManager.cs:317`); `MessageEventArgs` (`MessageManager.cs:98`, `:303`). **Destroys** — nothing.

**Triggers** — `Send_ACKTalk()` (`:1076`, then immediate return); `EventSink.InvokeRawMessageReceived` (`MessageManager.cs:98`); `InvokeMessageReceived` (`:303`); `GridContainer`/`ModernPaperdoll` `HandleObjectMessage` for `TextType.OBJECT` (`:223, :227`); `PaperDollGump`/`ContainerGump`/`TradingGump` `AddText` (`:271, :277, :283`); `ForcedTooltipManager.IsObjectTextRequested` may consume the message (`:94`).

**Ignores / partial**
- `:1067-1079` full early-out with only an ACK when `serial==0 && graphic==0 && type==Regular && font==0xFFFF && hue==0xFFFF && name.StartsWith("SYSTEM")`. Text discarded.
- `:1083-1091` text_type stays SYSTEM (never OBJECT) when `type==System || serial==0xFFFFFFFF || serial==0 || (name.ToLower()=="system" && entity==null)`.
- `:1096` a server-sent name for an already-named entity is ignored.
- `MessageManager.cs:85-88` empty text returns immediately.
- `MessageManager.cs:94-95` `ForceTooltipsOnOldClients` + OBJECT swallows the whole message.
- `MessageManager.cs:111-115` `Profile.OverrideAllFonts` replaces the server's font and unicode flag.
- `MessageManager.cs:119-124` System/Command/Encoded/ChatSystem produce no overhead text.
- `MessageManager.cs:166, :170` Guild/Alliance return early under the Ignore* profile flags, so `InvokeMessageReceived` at `:303` never fires.
- `MessageManager.cs:127, :145` party overhead requires `DisplayPartyChatOverhead` and a name match.
- `MessageManager.cs:149, :240` senders on `IgnoreManager.IgnoredCharsList` dropped unless Spell.
- `MessageManager.cs:180-205` spell text may be rewritten by `SpellDisplayFormat` and re-hued by profile hues, replacing the server's hue.
- `MessageManager.cs:234-237` `parent == null` breaks the Limit3Spell path with no message.

### `0x1D` — `DeleteObject` (`PacketHandlers.cs:1105`, registered `:247`)

**Len** `F=5`. **Purpose** remove one entity from the world.

**Fields** — `[1..4]` u32BE serial (`:1112`).

**Mutates** — `mob.Mount = null` when the deleted item was on `Layer.Mount` of the root mobile (`:1154`, `Mobile.cs:180`); `ItemHold.Enabled = false` (`:1172`); `HouseManager._houses` entry removed + `House.ClearComponents` (`:1239` → `HouseManager.cs:272-273`; sets multi `WantUpdateMulti` `House.cs:185`, destroys and removes every `Multi` `House.cs:197-198`); `cont.Remove(item)` (`:1246` → `LinkedObject.cs:100-121`); mobile path `World.RemoveMobile(serial, true)` (`:1230` → child items `World.cs:704`, `OPL.Remove` `:709`, `Destroy()` `:710`, `Mobiles.Remove` `:714`, `ReturnToPool` `:716`); item path `World.RemoveItem(serial, true)` (`:1259` → `RemoveItemFromContainer` `World.cs:665` sets `Container=0xFFFFFFFF` `:647`, nulls `Next`/`Previous` `:650-651`, `RemoveFromTile` `:652`; recursive child removal `:671`; `OPL.Remove` `:676`; `Destroy()` `:677`; `Items.Remove` `:681`; `ReturnToPool` `:683`); chunk cell handed to `TNext ?? TPrevious` (`GameObject.cs:231`), `TNext`/`TPrevious` nulled (`:244-245`); `GameObject.Destroy` clears `Next`/`Previous`/`RenderListNext`/`_averageOverTime`, `Clear()`, `RemoveFromTile()`, `TextContainer.Clear()`, `IsDestroyed=true`, `PriorityZ=0`, `IsPositionChanged=false`, `Hue=0`, `Offset=Zero`, `RealScreenPosition=Zero`, `IsFlipped=false`, `originalGraphic=0`, `Graphic=0`, `ObjectHandlesStatus=NONE`, `FrameInfo=Empty` (`GameObject.cs:454-472`); `Entity.Destroy` zeroes `AnimIndex`, `LastAnimationChangeTime` (`Entity.cs:204-205`); `World.Player.Abilities[0]`/`[1]` (`:1263` → `PlayerMobile.cs:321-322`).

**Creates** — nothing.

**Destroys / pools** — the addressed Mobile or Item and every item it contains recursively (`World.cs:671`/`:704`); the object handed back to `Item._pool`/`Mobile._pool` (`World.cs:683`/`:716`; `Item.cs:321-331` guards double-return with `_inPool`); House `Multi` components (`House.cs:197`); `BulletinBoardItem` gump (`:1197`); gumps disposed inside `Item.Destroy` when `Opened` — ContainerGump, GridContainer, SpellbookGump, MapGump, GridLootGump (corpses), BulletinBoardGump, SplitMenuGump (`Item.cs:276-289`); `PaperDollGump`/`ModernPaperdoll` inside `Mobile.Destroy` for non-`PlayerMobile` (`Mobile.cs:1139-1140`).

**Triggers** — trading gump `RequestUpdateContents` (`:1165`); paperdoll (`:1177-1178`, `:1250-1251`); container/grid (`:1181`/`:1183`); `NearbyLootGump`/`GridLootGump` (`:1188`/`:1191`); `BulletinBoardGump.RemoveBulletinObject` (`:1203`); `MiniMapGump` (`:1256`); **`GameActions.SendCloseStatus` — an outgoing packet issued from inside `Entity.Destroy` (`Entity.cs:202`)**; `HouseDiagnostics.LogHouseItemDeleteOrdered`/`LogRangeProbe` (`:1128`/`:1131`).

**Ignores / partial**
- `:1107` `World.Player == null` → return.
- `:1114` `serial == World.Player` → return. A server order to delete the player is ignored outright.
- `:1121` `World.Get` null or destroyed → return.
- `:1152` Mount cleared only when `Layer.Mount` and the root container resolves to a Mobile.
- `:1159-1160` `updateAbilities` only when the root container is the player and the layer is One/TwoHanded.
- `:1170` ItemHold disabled only when the container serial (masked `0x7FFFFFFF` at `:1144`) is the player and layer is Invalid.
- `:1175` paperdoll refresh only when `Layer != Invalid`.
- `:1186-1189` GridLootGump refresh only for root graphic `0x2006` with `GridLootType` 1 or 2.
- `:1195` bulletin-board teardown only for graphic `0x0EB0`.
- **`:1209-1212` hard deferral**: if `World.CorpseManager.Exists(0, serial)` the handler returns having done only the gump refreshes. The entity is **not** removed from `World.Items`/`World.Mobiles`, not destroyed, not pooled. `CorpseManager.Exists` matches on `ObjectSerial` (`CorpseManager.cs:91`) — anything currently playing a death animation.
- `:1216` the `World.Party.Contains(serial)` branch is a no-op — both the `RemoveFromTile()` call and the alternative are commented out.
- `:1237` `HouseManager.Remove` only when `IsMulti`.
- `:1254` MiniMap refresh only for a container-less multi.
- `:1261` `UpdateAbilities` is only in the Item branch; for a Mobile the flag is computed and never consumed.
- Frame note: mutates `World.Items`/`World.Mobiles`, the dictionaries `World.Update` sweeps (`World.cs:350`, `:424`). Dispatch is main-thread and ordered before `Scene.Update` (`GameController.cs:478` vs `GameScene.cs:913`), but plugin-injected packets are appended from plugin callbacks (`Plugin.cs:757`, `:779`) and parsed in the same pass (`PacketHandlers.cs:95`).

### `0x20` — `UpdatePlayer` (`PacketHandlers.cs:1268`, registered `:248`)

**Len** `F=0x13` (19) (`PacketsTable.cs:73`) — the reads consume it exactly.
**Purpose** relocate/reskin the player (teleport, resurrect, polymorph).

**Fields** — `[1..4]` serial (`:1275`); `[5..6]` graphic (`:1276`); `[7]` graphic_inc (`:1277`) — threaded to the inner overload as `graph_inc` (`:6819`) and **never used**; `[8..9]` hue (`:1278`); `[10]` flags (`:1279`); `[11..12]` x (`:1280`); `[13..14]` y (`:1281`); `[15..16]` serverID (`:1282`) — passed in at `:6825` and **never used**; `[17]` direction (`:1283`); `[18]` i8 z (`:1284`).

**Mutates** — `World.RangeSize.X/.Y` (`:6831-6832`); `Walker.WalkingFailed=false` (`:6838`); `Player.Graphic` (`:6839`; setter `GameObject.cs:107-117` writes `originalGraphic`, runs `GraphicsReplacement.Replace`, writes Hue via `GameObject.cs:118-125`, then graphic, then `OnGraphicSet`); `Player.Direction = direction & Direction.Mask` (`:6840`; `Entity.cs:92-103` fires only on change → `PlayerMobile.cs:1460-1464` → `TryOpenDoors` `:1466-1478`); Hue via `FixHue` (`:6841`, `Entity.cs:115-134`); `Player.Flags` (`:6842`); `Walker.DenyWalk(0xFF,-1,-1,-1)` (`:6843` → `WalkerManager.cs:108-121`: `ClearSteps()` `Mobile.cs:298-302`, then `Reset()` `WalkerManager.cs:187-196` zeroing `UnacceptedPacketsCount`, `StepsCount`, `WalkSequence`, `CurrentWalkSequence`, `WalkingFailed`, `ResendPacketResync`, `LastStepRequestTime`); `Weather.Reset()` (`:6849` → `Weather.cs:83-90`); `GameScene.UpdateDrawPosition = true` (`:6850`); `World.Season` (`World.cs:208`, from `:6866` Desolation/music 42 or `:6870` OldSeason/OldMusicIndex); every loaded map object's season graphic (`World.cs:210-222` walks `Map.GetUsedChunks()` `Map.cs:260-266` and every 8×8 cell from `Chunk.GetHeadObject` `Chunk.cs:160-170` calling `UpdateGraphicBySeason` `GameObject.cs:248` — concrete writes `Land.cs:91-92`, `Static.cs:103-105`, `Multi.cs:106-107`); `Walker.ResendPacketResync=false` (`:6874`); `Player.X/Y/Z` (`:6876` → `GameObject.cs:267-269`, `IsPositionChanged` `:252`, `AddToTile` `:271`); tile linkage (`GameObject.cs:171-184` → `Map.GetChunk(x,y,load:true)` `Map.cs:71-101`, cold block does `_usedIndices.AddLast` `:96` + `Chunk.Create`/`Load` `:97-98`; then `RemoveFromTile` `GameObject.cs:220-246` and `Chunk.AddGameObject` writing `TileChunk`/`TileCellX`/`TileCellY` `Chunk.cs:178-180`, `PriorityZ` `:285`, `Tiles[x,y]` `:289`); `Player.Abilities[0]/[1]` (`:6877` → `PlayerMobile.cs:312-313` reset to `Ability.Invalid` then reassigned per equipped One/TwoHanded graphic `:301+`); player bank contents removed (`:6837` → `PlayerMobile.cs:1493-1522`).

**Creates** — map chunks, as a side effect of `AddToTile` (`Map.cs:97-98` may build a whole terrain block with its Land/Static objects). No entity.

**Destroys / pools** — every item inside the bank container: `PlayerMobile.cs:1503-1510` loops calling `World.RemoveItem(first, true)` (`World.cs:655-687`) — recursive child removal `:667-674`, `OPL.Remove` `:676`, `Destroy()` `:677`, and because `forceRemove`, `Items.Remove` `:681` + `ReturnToPool` `:683`; `bank.Items = null` (`PlayerMobile.cs:1512`), `bank.Opened = false` (`:1520`); bank `ContainerGump` and `GridContainer` disposed (`:1515`, `:1517`); `Item.Destroy` itself disposes that item's gumps when `Opened` and pools it (`Item.cs:263-331`).

**Triggers** — `GameActions.OpenDoor()` can be sent from the Direction setter (`Entity.cs:100` → `PlayerMobile.cs:1460` → `:1466-1478`, LINQ over `World.Items.Values`); music change (`World.cs:224-233`, gated on `music >= 0` and `Audio.CanSeasonMusicTakeOver()` `AudioManager.cs:514-524`).

**Ignores / partial**
- `:1270-1273` `World.Player == null` → drop.
- **`:6829` the inner overload does nothing at all unless `serial == World.Player`.** A `0x20` naming any other serial is fully parsed and silently discarded.
- `graphic_inc` (wire off 7) parsed and never applied — the graphic is used raw.
- `serverID` (wire off 15) parsed and never applied — no map/server switch happens here.
- `:6840` direction masked to `Direction.Mask`, dropping the Running/Up bits.
- `Entity.cs:121-124` hue `>= 0x0BB8` replaced with 1.
- `:6862` season only touched when the dead/alive state actually flipped; `IsDead` is derived from Graphic (`Mobile.cs:159-169`), so the flip is decided by the graphic just sent.
- `:6847-6851` GameScene work skipped when `GetScene<GameScene>()` is null.
- `:6854-6860` target reset on body change is commented out ("std client keeps the target open!").
- **`:6875` `CloseRangedGumps` is effectively a no-op**: `PlayerMobile.cs:1528-1529` reads `if (UIManager.Gumps.Count > i) continue;`, true for every index visited, so the switch body at `:1535+` is never reached.
- `PlayerMobile.cs:1497`/`:1499` `CloseBank` does nothing without an open `Layer.Bank` item, and skips the purge when the bank is empty.
- `PlayerMobile.cs:1468` `TryOpenDoors` skipped while dead or with `AutoOpenDoors` off.
- `World.cs:224-227` `OldMusicIndex` defaults to `-1` (`World.cs:97`), so the resurrect path can pass `-1` and skip the music change.
- `GameObject.cs:173, :180` `AddToTile` silently does nothing when `World.Map` is null or `GetChunk` returns null.

### `0x21` — `DenyWalk` (`PacketHandlers.cs:1289`, registered `:249`)

**Len** `F=0x08` (`PacketsTable.cs:74`). **Purpose** reject a move and state the real position/facing.

**Fields** — `[1]` u8 seq (`:1296`); `[2..3]` x (`:1297`); `[4..5]` y (`:1298`); `[6]` direction (`:1299`) then `&= Direction.Up` (0x07 == `Direction.Mask`, `Direction.cs:48-49`) at `:1300`, discarding the `0x80` Running bit; `[7]` i8 z (`:1301`).

**Mutates** — `Mobile.Steps` cleared and `Offset` zeroed (`Mobile.cs:300-301` via `WalkerManager.cs:110`); `UnacceptedPacketsCount`/`StepsCount`/`WalkSequence`/`CurrentWalkSequence`/`WalkingFailed`/`ResendPacketResync`/`LastStepRequestTime` zeroed (`WalkerManager.cs:189-195` via `:112`); `World.RangeSize.X/.Y` (`WalkerManager.cs:116-117`); `PlayerMobile.X/Y/Z` (`GameObject.cs:267-269` via `WalkerManager.cs:119`); `IsPositionChanged` (`GameObject.cs:252`); old chunk cell handed to a neighbour (`GameObject.cs:231`), `TPrevious.TNext`/`TNext.TPrevious` rewritten (`:236, :241`); relink into the destination cell (`Chunk.cs:178-180`, `PriorityZ` `:285`, `Tiles[x,y]` `:289`, splice `:338-341`); `Map._terrainChunks[block]`/`_usedIndices` on a cold block (`Map.cs:96-99`), `chunk.LastAccessTime` (`:116`); `Entity._direction` (`Entity.cs:99`, written from `:1304`); Weather fields nulled (`Weather.cs:85-89` via `:1306`).

**Creates** — a `Chunk` when the corrected position is in an unloaded block (`Map.cs:97-98`). **Destroys** — nothing.

**Triggers** — `SetInWorldTile` → `UpdateScreenPosition` → `PlayerMobile.OnPositionChanged` (`PlayerMobile.cs:1418`): `Plugin.UpdatePlayerPosition` (`:1422`), `ScriptRecorder.UpdatePlayerPosition` (`:1427`), `TryOpenDoors` (`:1429`), `TryOpenCorpses` (`:1430`), `EventSink.InvokeOnPositionChanged` (`:1432`). `TryOpenDoors` LINQ-enumerates `World.Items.Values` (`PlayerMobile.cs:1473`) and may send `Send_OpenDoor` (`GameActions.cs:1177`); `TryOpenCorpses` enumerates `World.Items.Values` (`:1449`) and queues `GameActions.DoubleClickQueued` (`GameActions.cs:615`). The Direction write at `:1304` then fires `OnDirectionChanged` (`PlayerMobile.cs:1460`) which calls `TryOpenDoors` a **second** time — one `0x21` can walk `World.Items` twice and send two `Send_OpenDoor` packets. `Weather.Reset` (`Weather.cs:83`).

**Ignores / partial**
- `:1291` `World.Player == null` → return.
- the sequence byte is read at `:1296` and passed to `DenyWalk`, which never uses it (`WalkerManager.cs:108`).
- `WalkerManager.cs:114` the position is applied only inside `if (x != -1)`; `x` is an int widened from a ushort, so the guard can never be false.
- `:1300` direction truncated to its low 3 bits — the Running bit is dropped.
- `:1306` weather reset only when both `GameScene` and its `Weather` are non-null.

### `0x22` — `ConfirmWalk` (`PacketHandlers.cs:1309`, registered `:250`)

**Len** `F=0x0003` (`PacketsTable.cs:75`). **Purpose** ack a walk request, echoing sequence and notoriety.

**Fields** — `[1]` u8 sequence (`:1316`); `[2]` u8 notoriety (`:1317`), immediately masked `&~0x40` (clears the "can be renamed" bit) before use.

**Mutates** — `World.Player.NotorietyFlag` (`:1324`; plain field `Mobile.cs:230`); `WalkerManager.UnacceptedPacketsCount--` (`WalkerManager.cs:127`, only when non-zero); `StepInfos[stepIndex].Accepted = true` (`:149`); `World.RangeSize.X/.Y` from `StepInfos[stepIndex]` (`:151-152`) or `StepInfos[0]` on the late-ack path (`:156-157`); array compaction `StepInfos[i-1] = StepInfos[i]` (`:161`); `StepsCount--` (`:164`); `CurrentWalkSequence--` (`:165`); bad-step path `ResendPacketResync = true` (`:178`), `WalkingFailed = true` (`:181`, blocks all further `Walk` calls until cleared at `PacketHandlers.cs:6838`), `StepsCount = 0`, `CurrentWalkSequence = 0` (`:182-183`); tile relink `GameObject.cs:224/231/236/241/244-245` and `Chunk.cs:178-180`, `:207` (`priorityZ++` for a Mobile), `:285`, `:289-291`, `:338-352` (from `AddToTile` at `PacketHandlers.cs:1327`).

**Creates / destroys** — nothing.

**Triggers** — `NetClient.Socket.Send_Resync()` on a bad step (`WalkerManager.cs:177`), at most once until `ResendPacketResync` clears.

**Ignores / partial**
- `:1311-1314` `World.Player == null` → ack dropped.
- `:1317` the `0x40` bit of the notoriety byte is masked off and never used.
- **`:1319-1322` clamp**: masked notoriety of 0 or `>= 8` is forced to `0x01` (Innocent); the server's value is discarded.
- `WalkerManager.cs:125` an ack with no outstanding request leaves the counter alone.
- `WalkerManager.cs:132-142` no `StepInfos` entry with this sequence ⇒ treated as a bad step; the acked position is never applied to `World.RangeSize`.
- `WalkerManager.cs:167-170` an ack earlier than `CurrentWalkSequence` that is not index 0 is also a bad step; the position is discarded.
- `WalkerManager.cs:175` `Send_Resync` suppressed if `ResendPacketResync` is already true.
- `GameObject.cs:173/:180` if `World.Map` is null or the chunk unloaded, `RemoveFromTile` still runs but nothing is relinked, leaving the player unlinked from any tile list.

### `0x23` — `DragAnimation` (`PacketHandlers.cs:1330`, registered `:251`)

**Len** `F=0x1A` (26). **Purpose** show an item flying between two mobiles or coordinates.

**Fields** — `[1..2]` graphic (`:1332`); `[3]` u8 graphic increment, added to graphic (`:1333`); `[4..5]` hue (`:1334`); `[6..7]` count (`:1335`) — read and never used; `[8..11]` source serial (`:1336`); `[12..16]` srcX/srcY/i8 srcZ (`:1337-1339`); `[17..20]` dest serial (`:1340`); `[21..25]` destX/destY/i8 destZ (`:1341-1343`).

**Mutates** — no World/Entity state; only `EffectManager`'s linked list gains a node (`EffectManager.cs:240`).

**Creates** — `MovingEffect` (`EffectManager.cs:109-130`) when either endpoint serial is invalid, else `DragEffect` (`:145-164`); added at `:240`. **Destroys** — nothing.

**Triggers** — `World.SpawnEffect` (`:1384` → `World.cs:722` → `EffectManager.cs:60`).

**Ignores / partial**
- `:1345-1356` graphic `0x0EED`→`0x0EEF`, `0x0EEA`→`0x0EEC`, `0x0EF0`→`0x0EF2` before anything else.
- `:1358`/`:1371` source and dest are looked up **only** in `World.Mobiles`; an item endpoint is treated as absent and the serial forced to 0 (`:1362`, `:1375`).
- `:1366-1368`/`:1379-1381` when the mobile **is** found the server's coordinates are discarded and replaced with the mobile's current position.
- `:1385-1387` effect type falls back to `Moving` rather than `DragEffect` whenever either serial ends up invalid.
- `:1398-1403` speed (5), duration (5000), fixedDir (true), doesExplode (false), hasparticles (false) and blend mode are **hardcoded**, not from the wire.
- `EffectManager.cs:88-91` a non-zero hue is incremented by 1; `:93` duration multiplied by `Constants.ITEM_EFFECT_ANIMATION_DELAY`.
- `EffectManager.cs:98-101 / 135-138` returns without creating anything if `graphic == 0`.

### `0x24` — `OpenContainer` (`PacketHandlers.cs:1416`, registered `:252`)

**Len** `F=0x09` on `>=CV_7090`, `F=0x07` below (`PacketsTable.cs:392/401`).
**Purpose** open a container / spellbook / vendor buy window.

**Fields** — `[1..4]` u32BE serial (`:1423`); `[5..6]` u16BE graphic (`:1424`); `[7..8]` on 7.0.90+ **never read** — the handler stops after graphic, so the two trailing bytes the table accounts for are discarded. `graphic` is not merely read but **rewritten in place** (`:1574, 1582, 1590, 1598, 1606, 1614, 1622, 1630, 1638`) before reaching the gump.

**Mutates** — static `_requestedGridLoot` (`PacketHandlers.cs:63`) `= serial` at `:1552`, only for a corpse with `GridLootType` 1 or 2; consumed later by `AddItemToContainer` (`:6515-6521`). `it.Opened = true` (`:1710`, `Item.cs:249`) — skipped for graphic `0x0030` and skipped when `GridLootType == 1` took the early return at `:1556`. `ClearContainerAndRemoveItems(it)` (`:1714` → `:6881`): per child `World.RemoveItem(it,true)` (`:6908`) writing `World.cs:647` `Container=0xFFFFFFFF`, `:650-651` `Next`/`Previous` null, `:652` `RemoveFromTile`, `:676` `OPL.Remove`, `:677` `Destroy()`, `:681` `Items.Remove`, `:683` `ReturnToPool`; then `container.Items = null` (`:6914`). `ContainerManager.X`/`.Y` statics (`ContainerManager.cs:57-58`) written by `CalculateContainerPosition` (`:1675` → `ContainerManager.cs:75-76, 99-100, 106-115, 122, 127, 141, 148, 152, 165, 169, 172, 181-186, 194+`). `UIManager._gumpPositionCache` entry removed (`:1696` → `UIManager.cs:302`). `NearbyLootGump._corpsesRequested` — the serial is removed **as a side effect of the test** at `:1540`, since `IsCorpseRequested` defaults `remove:true` (`NearbyLootGump.cs:264`). `World.Player.ManualOpenedCorpses`/`AutoOpenedCorpses` mutated by the gump constructors (`GridLootGump.cs:79`, `GridContainer.cs:174`, `ContainerGump.cs:127`).

**Creates** — `SpellbookGump` (`:1437`, added `:1445`) for graphic `0xFFFF`; `ModernShopGump` (`:1465`) or `ShopGump` (`:1467`) for graphic `0x0030`, populated per stock item (`:1499`/`:1510`); `List<Item> buyList` per shop layer (`:1492`); `GridLootGump` (`:1551`); `GridContainer` (`:1655`); `ContainerGump` (`:1684`, `InvalidateContents = true` `:1688`). No world Entity is created.

**Destroys / pools** — existing `SpellbookGump` (`:1435`), `ShopGump`+`ModernShopGump` (`:1458-1459`), `GridLootGump` (`:1550`), `ContainerGump` (`:1671`, its coords read at `:1669-1670` and reused). **Every child Item of the container is destroyed and returned to the pool** at `:1714` → `World.cs:677/681/683`; `Item.Destroy` also disposes that child's gumps when it was `Opened` (`Item.cs:274-292`). `BuySellAgent` disposes the shop gump this handler just added (`BuySellAgent.cs:163`).

**Triggers** — sound `0x0055` for the spellbook branch (`:1447`); container open sound inside the `ContainerGump` ctor (`ContainerGump.cs:141-144`), only when no gump already existed (`:1678`); `BuySellAgent.Instance?.HandleBuyPacket` at `:1531` — **called once per shop layer inside the loop**, so up to two calls per packet; it can send `Send_BuyRequest` (`BuySellAgent.cs:161`), print (`:162`) and dispose the shop gump (`:163`); `EventSink.InvokeOnOpenContainer` (`:1694` → `EventSink.cs:104` → `AutoLootManager.OnOpenContainer` `AutoLootManager.cs:225` → `CheckCorpse` `:174` → `HandleCorpse` `:141`, which walks `corpse.Items` **before** `:1714` destroys those very items); `GridContainer.RequestUpdateContents` (`:1651`).

**Ignores / partial**
- `:1418-1421` `World.Player == null` → drop.
- `:1430-1433` spellbook branch aborts on unknown item; the tail block at `:1704` then finds the item null too, so nothing is marked `Opened`.
- `:1453-1456` vendor branch aborts on unknown mobile — the entire buy list is discarded.
- `:1477-1481` a shop layer whose container item has no children is skipped — that layer's stock is dropped silently.
- `:1483` traversal direction reversed unless `item.Graphic == 0x2AF8` (comment: matches hardcoded original-client logic).
- `:1473-1475` `vendor.FindItemByLayer(layer)` dereferenced with no null guard (`Entity.cs:310` can return null).
- **`:1540` if `NearbyLootGump.IsCorpseRequested(serial)`, every gump-creation path is skipped** — the server said open, the client opens nothing — yet `:1694-1696` and the `:1704` tail still run.
- `:1542-1548` grid loot gump only when `IsCorpse` and `GridLootType` 1 or 2.
- **`:1554-1557` `GridLootType == 1` returns from the handler entirely**, so `InvokeOnOpenContainer`, `RemovePosition` and the whole `:1704-1717` tail never run.
- `:1561-1643` the server's graphic is substituted with a large-container graphic only when `>= CV_706000` **and** profile non-null **and** `UseLargeContainerGumps` **and** the replacement texture loads. A missing texture keeps the server's graphic.
- `:1646` grid layout skipped for graphic `0x091A`.
- `:1649-1652` an already-open `GridContainer` is only refreshed; nothing is rebuilt or repositioned.
- `:1698-1701` unknown item → `Log.Error` only.
- `:1704` the whole `Opened`/clear tail skipped for graphic `0x0030`.
- `:1712` contents are **not** cleared for a corpse or for graphic `0xFFFF`.
- `:1464, :1498, :1554, :1646` dereference `ProfileManager.CurrentProfile` with no null check while `:1563` checks it.

### `0x25` — `UpdateContainedItem` (`PacketHandlers.cs:1720`, registered `:253`)

**Len** `F=0x15` on `>=CV_6017`, `F=0x14` below (`PacketsTable.cs:349/354`).
**Purpose** place a single item inside a container.

**Fields** — `[1..4]` serial (`:1727`); `[5..7]` `graphic = (ushort)(ReadUInt16BE() + ReadUInt8())` — the increment byte is **added**, not OR'd (`:1728`); `[8..9]` amount, `Math.Max((ushort)1, …)` (`:1729`); `[10..11]` x (`:1730`); `[12..13]` y (`:1731`); `[14]` one byte skipped and never read on `>=CV_6017` — the grid index (`:1733-1736`); `[15..18]` (or `[14..17]`) container serial (`:1738`); `[19..20]` hue (`:1739`). All to `AddItemToContainer` (`:1741` → `:6389`).

**Mutates** — `ItemHold.Clear()` when the held item is this serial and was already dropped (`:6404`); `World.RemoveMobile(serial, true)` if the "item" serial is a mobile serial (`:6429`); `World.RemoveItem(item, true)` when an existing item moves to a different container (`:6436`); `Item.Graphic` (`:6440`); `CheckGraphicChange` (`:6441` → `Item.cs:607-646`); `Amount` (`:6442`); Hue via `FixHue` (`:6443` → `Entity.cs:115-133`); `X`, `Y` (`:6444-6445`); **`Z` forced to 0** (`:6446`); unlink + `Container` reassign (`:6452-6453` → `World.cs:617-653`); `container.PushToBack(item)` (`:6456` → `LinkedObject.cs:57-78`); `((Item)container).Opened = true` when a gump exists (`:6534`); `_requestedGridLoot = 0` after a `GridLootGump` spawns (`:6521`).

**Creates** — `Item` via `World.GetOrCreateItem` (`:6439` → `World.cs:559-581` → `Item._pool.GetOne()` `Item.cs:255-260`); `GridLootGump` (`:6519-6520`).

**Destroys / pools** — the previously-known item under the same serial when it lived elsewhere (`:6436`, `forceRemove` ⇒ `Items.Remove` + `ReturnToPool`, `World.cs:679-684`), recursively destroying its contents (`:664-674`) and its OPL entry (`:676`); a mobile filed under this serial (`:6429`); `Item.Destroy` disposes ContainerGump/GridContainer/SpellbookGump/MapGump/GridLootGump/BulletinBoardGump/SplitMenuGump when `Opened` (`Item.cs:263-296`).

**Triggers** — **`Send_BulletinBoardRequestMessageSummary` (`:6481`)** when a `BulletinBoardGump` exists for the container; `RequestUpdateContents` on `TradingGump` for the secure trade box (`:6465`), paperdolls (`:6469-6470`, and `World.cs:627-628`), `ContainerGump` (`:6537`, `World.cs:632`), `GridContainer` (`:6503`, `World.cs:634`), `NearbyLootGump` (`:6527`, `World.cs:637`), `SpellbookGump` (`:6488`/`:6537`), `GridLootGump` (`:6524`), `TradingGump` for the container itself (`:6542`); `ContainerGump.CheckItemControlPosition` (`:6496`).

**Ignores / partial**
- `:1722-1725` `!World.InGame` → drop.
- `:1729` amount clamped up to 1 — a server-sent 0 is not honoured.
- `:6446` Z discarded entirely and forced to 0; the item's previous Z is also thrown away.
- `:6417-6423` unknown container → `Log.Warn "No container found"` and return; the item is not created and nothing is requested.
- `:6434` the old item is torn down only when `item.Container != containerSerial && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid)` — same-container and corpse-equipment re-sends are left in place (comment at `:6433`: "prevent closing containers when changing facets").
- `:6450` container reassignment likewise skipped for a same-container send; the item is merely `PushToBack`'d again.
- `:1733-1736` the grid-index byte only exists and is only skipped on `CV_6017+`.
- `:6401-6405` `ItemHold` cleared only when `Dropped` is set.
- `:6507-6522` `GridLootGump` auto-creation needs `GridLootType > 0` **and** `_requestedGridLoot == containerSerial`.

### `0x27` — `DenyMoveItem` (`PacketHandlers.cs:1744`, registered `:254`)

**Len** `F=0x0002` (`PacketsTable.cs:80`). **Purpose** refuse a pickup/drop; put the held item back.

**Fields** — `[1]` u8 code, read at `:1863` — **after** every piece of state repair has already run. Nothing else is read; all restore data comes from `ItemHold`, not the packet. `p[0]` passed to `ServerErrorMessages.GetError` at `:1869`.

**Mutates** — `World.ObjectToRemove = 0` (`:1761`, `World.cs:69`) when it equals `ItemHold.Serial`, cancelling the deferred removal `World.Update` would do at `World.cs:294-332`.
Branch A (`Layer.Invalid` + valid container, `:1771-1792`) → `AddItemToContainer` (`:6389`): Graphic `:6440`, `CheckGraphicChange` `:6441`, Amount `:6442`, `FixHue` `:6443`, X `:6444`, Y `:6445`, **Z = 0** `:6446`, unlink + `Container` `:6452-6453`, `PushToBack` `:6456`.
Branch B (`:1795-1838`): `World.GetOrCreateItem` may insert into `World.Items` (`World.cs:577`); then Graphic `:1799`, **Hue raw, not `FixHue`** `:1800`, Amount `:1801`, Flags `:1802`, Layer `:1803`, X/Y/Z `:1804-1806`, `CheckGraphicChange()` `:1807`.
 - B1 container is a mobile (`:1813-1823`): `RemoveItemFromContainer` `:1817`, `PushToBack` `:1818`, `Container` `:1819`.
 - B2 container exists but is not a mobile (`:1826-1828`): `World.RemoveItem(item, true)` — the item is destroyed and pooled.
 - B3 container null (`:1833-1837`): `RemoveItemFromContainer` `:1835` then `SetInWorldTile` `:1837`.
`ItemHold.Clear()` (`:1851` → `ItemHold.cs:118-142`: zeroes Serial/Container/Amount/MouseOffset, X=Y=0xFFFF, Graphic=DisplayedGraphic=0xFFFF, Hue=0xFFFF, Layer=Invalid, Flags=None, Dropped=Enabled=UpdatedInWorld=false).

**Creates** — possibly one pooled `Item` (`Item.cs:255-261` via `World.cs:576`) on branch A (`:6439`) or B (`:1795`); `GridLootGump` inside `AddItemToContainer` (`:6519`).

**Destroys / pools** — branch B2 destroys and pools the item (`World.cs:677, :683`); `GetOrCreateItem` evicts and pools a stale destroyed entry (`World.cs:563-572`); `SplitMenuGump` for `ItemHold.Serial` disposed unconditionally on the repair path (`:1849`).

**Triggers** — `ContainerGump.RequestUpdateContents` for `ItemHold.Container` (`:1789-1791`); paperdolls (`:1821-1822`); `MessageManager.HandleMessage` with `ServerErrorMessages.GetError(0x27, code)`, hue `0x03b2`, System, font 3 (`:1867-1875`); via `AddItemToContainer`, `Send_BulletinBoardRequestMessageSummary` (`:6481`) and the gump refreshes listed under `0x25`; `Console.WriteLine` traces at `:1777, :1815, :1826, :1833, :1900, :6403`.

**Ignores / partial**
- `:1746` `!World.InGame` → return.
- `:1753-1757` the entire repair is gated on `ItemHold.Enabled || (ItemHold.Dropped && (firstItem == null || !firstItem.AllowedToDraw))`. If false, the handler only logs "There was a problem with ItemHold object" (`:1855`) and still shows the error text.
- `:1764-1767` skipped unless `SerialHelper.IsValid(ItemHold.Serial) && ItemHold.Graphic != 0xFFFF`; otherwise `Log.Error` (`:1844`) and only `Clear()`.
- `:1769` skipped when `ItemHold.UpdatedInWorld` — the client assumes the world copy is already correct.
- `:1771-1776` branch A only for `Layer.Invalid` + valid container; the comment records that the client defers to a follow-up `0x25`.
- `:6417-6422` `AddItemToContainer` aborts with a `Log.Warn` if the container is unknown — the restore silently does not happen.
- **`:1865` the error text is suppressed entirely when `code >= 5`.** `ServerErrorMessages.cs:112-118` would itself clamp `code >= 5` to 4 for `0x27`, so that clamp is unreachable from here.
- `Z` is forced to 0 by branch A (`:6446`) but preserved from `ItemHold` in branch B (`:1806`).

### `0x28` — `EndDraggingItem` (`PacketHandlers.cs:1879`, registered `:255`)

**Len** `F=5` (`PacketsTable.cs:81`). **Purpose** stop the drag in progress.

**Fields** — **none.** The 4-byte body is never parsed.

**Mutates** — `ItemHold.Enabled = false` (`:1886`; the setter also resets `IsFixedPosition`, `FixedX`, `FixedY`, `IgnoreFixedPosition`, `ItemHold.cs:76-82`); `ItemHold.Dropped = false` (`:1887`).

**Creates / destroys / triggers** — nothing. `ItemHold.Clear()` is **not** called, so Serial, Graphic, Hue, Amount, Container and Layer keep their previous values.

**Ignores / partial** — `:1881-1884` `!World.InGame` → return. Whatever object the server named is ignored; the flags are cleared for whatever the cursor currently holds.

### `0x29` — `DropItemAccepted` (`PacketHandlers.cs:1890`, registered `:256`)

**Len** `F=0x0001` (`PacketsTable.cs:82`) — the packet is nothing but its id byte; the reader is already at end.

**Fields** — none.

**Mutates** — `ItemHold.Enabled = false` (`:1897`, setter also clears `IsFixedPosition`/`FixedX`/`FixedY`/`IgnoreFixedPosition`, `ItemHold.cs:69-76`); `ItemHold.Dropped = false` (`:1898`).

**Creates / destroys** — nothing. **Triggers** — `Console.WriteLine("PACKET - ITEM DROP OK!")` (`:1900`).

**Ignores / partial** — `:1892-1895` returns without touching `ItemHold` when `!World.InGame`; the drop confirmation is dropped on the floor. `ItemHold.Clear()` is not called — the held-item snapshot survives.

### `0x2C` — `DeathScreen` (`PacketHandlers.cs:1903`, registered `:257`)

**Len** `F=0x0002` (`PacketsTable.cs:85`). **Purpose** the player died (or is being resurrected).

**Fields** — `[1]` u8 action (`:1906`).

**Mutates** — `GameScene.Weather` reset: Type=0, Count=CurrentCount=Temperature=0, Wind=0, `_windTimer`=`_timer`=0, `CurrentWeather`=null (`:1910` → `Weather.cs:85-89`); `World.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER` (1500) (`:1916`, field `PlayerMobile.cs:103`, read by `GameScene.cs:1667`); `World.WMapManager._corpse = new WMapEntity{X=Player.X, Y=Player.Y, HP=0, Map=World.Map.Index, LastUpdate=Time.Ticks+300000, IsGuild=false, Name="Your Corpse"}` (`:1920-1929`, field `WorldMapEntityManager.cs:83`).

**Creates** — `WMapEntity` (`:1920`). **Destroys** — nothing.

**Triggers** — `Audio.PlayMusic(Audio.DeathMusicIndex, true)` (`:1912`); `GameActions.RequestWarMode(false)` (`:1919` → `Audio.StopWarMusic` `GameActions.cs:74` and `Send_ChangeWarMode(false)` `:78`); `EventSink.InvokeOnPlayerDeath` (`:1931`).

**Ignores / partial**
- `:1908` everything skipped when `action == 1`. Only `action != 1` is treated as death.
- `:1914` `DeathScreenTimer` written only under `EnableDeathScreen`; `ProfileManager.CurrentProfile` dereferenced with no null guard.
- no `World.InGame` / `World.Player` guard anywhere: `Player.X/Y` (`:1922-1923`) and `World.Map.Index` (`:1925`) are read unguarded.
- `GameActions.cs:66` `RequestWarMode`'s audio side only fires when `!World.Player.IsDead`; at this point the dead flag may not have arrived via `0x20`/`0x78`, so which branch runs depends on packet order.
- `_corpse.LastUpdate` is set 5 minutes ahead, so `WorldMapEntityManager.cs:196` will not expire it for that long.

### `0x2D` — `MobileAttributes` (`PacketHandlers.cs:1935`, registered `:258`)

**Len** `F=0x11` (17) (`PacketsTable.cs:86`). **Purpose** bulk hits/mana/stamina for one entity.

**Fields** — `[1..4]` serial (`:1937`); `[5..6]` → `HitsMax` (`:1946`); `[7..8]` → `Hits` (`:1947`); `[9..10]` `ManaMax` (`:1963`); `[11..12]` `Mana` (`:1964`); `[13..14]` `StaminaMax` (`:1965`); `[15..16]` `Stamina` (`:1966`). On the item path the reader is abandoned at offset 9 — harmless, the reader is per-packet (`:190`).

**Mutates** — `HitsMax` (`:1946`, `Entity.cs:81`, no event); `Hits` (`:1947`, setter `Entity.cs:67-80` writes the backing field and, **only when `this is PlayerMobile`**, builds `PlayerStatChangedArgs` from the OLD value and fires `EventSink.InvokeOnPlayerStatChange` at `Entity.cs:73`, `EventSink.cs:129`); `HitsRequest = Received` (`:1951`); `ManaMax`/`Mana`/`StaminaMax`/`Stamina` (`:1963-1966`, `Mobile.cs:233-236`); window title (`:1973` → `TitleBarStatsManager.cs:26`); `BandageManager.nextBandageTime` (`BandageManager.cs:118`/`:131`); `TargetManager.SetAutoTarget(...)` on the legacy-bandage path (`BandageManager.cs:129`).

**Creates** — nothing directly; `Entity.Update` (`Entity.cs:189-195`) will later recompute the percentage bar text from these values, allocating `RenderedText` (`Entity.cs:161`) for an uncached percentage.

**Destroys** — nothing.

**Triggers** — **`EventSink.OnPlayerStatChange` → `BandageManager.OnPlayerStatChanged` (`BandageManager.cs:39-45`) → `OnHpChanged` (`:77-108`) → `AttemptHeal` (`:110-135`), which sends `GameActions.BandageSelf()` or `GameActions.DoubleClick(bandage.Serial)` after arming auto-target — a stat packet can emit outgoing packets from inside the handler**; `UoAssist` messages (`:1970-1972` → `UoAssist.cs:73/78/83` → `:434-449`); title update (`:1973`).

**Ignores / partial**
- **no `World.InGame` guard at all** — this handler runs on serial lookup alone.
- `:1941-1944` `World.Get` null (including found-but-`IsDestroyed`, `World.cs:551-554`) → drop before reading past the serial.
- `:1949-1952` `HitsRequest` only advanced Pending → Received; a `None` stays `None`.
- `:1954` mana/stamina applied only when `SerialHelper.IsMobile(serial)` (`SerialHelper.cs:47-50`). For an item serial the last 8 bytes are ignored.
- `:1958-1961` a mobile-range serial that resolved to an Item returns, leaving hits applied and mana/stam dropped.
- `:1968` UoAssist and title bar only for `World.Player`.
- `Entity.cs:69` the Hits event fires only for `PlayerMobile`; every other mobile's hits change silently.
- `HitsMax` is written **before** `Hits` (`:1946` then `:1947`), so the subscriber at `BandageManager.cs:87` divides by the new max.
- the Hits setter re-enters arbitrary subscriber code mid-handler, which can send packets and set `TargetManager` state while mana/stamina have not yet been applied.

### `0x2E` — `EquipItem` (`PacketHandlers.cs:1978`, registered `:259`)

**Len** `F=0x000F` (`PacketsTable.cs:87`). **Purpose** put an item on a mobile's paperdoll layer.

**Fields** — `[1..4]` serial (`:1985`); `[5..7]` `(ushort)(u16BE graphic + i8 increment)` in one expression (`:2003`, left-to-right: base first, then the signed delta); `[8]` layer (`:2004`); `[9..12]` container serial (`:2005`); `[13..14]` hue via `Item.FixHue` (`:2006`). **No amount is on the wire; `Amount` is forced to 1** (`:2007`).

**Mutates** — `World.Items` insert (`World.cs:577`); a destroyed entry under that serial is removed and pooled first (`World.cs:565`, `:569` → `Item.cs:326-330`); `RemoveItemFromContainer` splices the old parent list (`World.cs:644` → `LinkedObject.cs:81-121`), `Container = 0xFFFFFFFF` (`:647`), `Next`/`Previous` null (`:650-651`), `RemoveFromTile` rewrites the chunk cell and neighbours (`:652` → `GameObject.cs:231, 236, 241`); `Item.Graphic` (`:2003` → `GameObject.cs:111-115`); `Layer` (`:2004`); `Container` (`:2005`); Hue via `FixHue` (`Entity.cs:133`, masked hue `>= 0x0BB8` becomes 1, `Entity.cs:121-124`); `Amount = 1` (`:2007`); `LinkedObject.PushToBack` (`LinkedObject.cs:57-79`); `Mobile.Mount = item` for `Layer.Mount` (`:2015` → `Mobile.cs:180`); `PlayerMobile.Abilities[0]/[1]` (`PlayerMobile.cs:321-322` and the fallback `:1393-1397`, from `:2033`).

**Creates** — an `Item` from the `QueuedPool` with ~30 fields reset by the pool action (`Item.cs:255-261`, `:54-106`), only when `World.Items` has no live entry.

**Destroys / pools** — nothing is destroyed; an already-destroyed Item under the same serial is evicted and pooled (`World.cs:565-569`).

**Triggers** — `RequestUpdateContents` on ContainerGump/PaperDollGump/ModernPaperdoll keyed on the **old** container serial (`:1997-2000`, read before `Container` is overwritten at `:2005`); the same from inside `RemoveItemFromContainer` (`World.cs:627-637`); paperdolls keyed on the **new** container (`:2024-2025`); up to two `UseAbilityButtonGump` (`PlayerMobile.cs:1401-1415`). **No outgoing packet.**

**Ignores / partial**
- `:1980` `!World.InGame` → return.
- `:1989` the old container is unlinked only when `item.Graphic != 0 && item.Layer != Layer.Backpack`. A freshly pooled Item has `Graphic == 0` (`Item.cs:58`), so removal is skipped for new items; an item genuinely on `Backpack` is never unlinked.
- `:1991` `ClearContainerAndRemoveItems` is commented out.
- `:1995` old-container gump refresh only when `SerialHelper.IsValid(item.Container)`.
- `:2018-2021` layers `0x1A..0x1C` (`ShopBuyRestock..ShopSell`, `Layers.cs:63-65`) take a deliberately empty branch; the `item.Clear()` there is commented out.
- `:2022` new-container paperdoll refresh needs a valid container **and** `layer < Mount` (`0x19`) — Mount, shop layers and Bank get no refresh.
- `:2011` when the container serial is not in World, `Container` is still written to that serial but the item is linked into nothing.
- `:2013` Mount assignment requires the container entity to be a Mobile.
- `:2028-2031` ability recalculation only when the container is reference-equal to `World.Player` and the layer is One/TwoHanded.
- `:2036-2041` the ItemHold reconciliation upstream does is commented out.

### `0x2F` — `Swing` (`PacketHandlers.cs:2044`, registered `:260`)

**Len** `F=0x000A` (`PacketsTable.cs:88`). **Purpose** melee swing report; used only to auto-face the player at the last-attacked target.

**Fields** — `[1]` skipped outright (`:2051`, the leading `0x00` flag is never inspected); `[2..5]` attacker serial (`:2053`); `[6..9]` defender serial (`:2060`), read only after the attacker check passes.

**Mutates** — nothing directly. All writes come from `World.Player.Walk` (`PlayerMobile.cs:1674`) at `:2091`: `Walker.StepInfos[n].*` (`PlayerMobile.cs:1811-1821` avoid path / `:1988-1998` WalkNotAvoid); `StepsCount++` (`:1823`/`:2000`); `Mobile.Steps.AddToBack` (`:1825`/`:2002`); `Walker.WalkSequence` wrap `0xFF→1` else `++` (`:1841/1845` / `:2020/2024`); `UnacceptedPacketsCount++` (`:1848`/`:2027`); `LastStepRequestTime` (`:1854`/`:2050`); `Mobile.LastStepTime` when the stack was empty (`:1801`/`:1985`); `AddToTile()` → `GameObject.cs:224/231/236/241/244-245` + `Chunk.cs:178-180/285/289-291/338-352` (`:1850`/`:2029`); `GetGroupForAnimation` (`:1855`/`:2051`); `SetAnimation(0xFF)` when not already walking (`:1798`/`:1982`); `CloseBank` sets `bank.Opened=false` (`:1520`), `bank.Items=null` (`:1512`); avoid path only, `ClearSteps()` then `SetInWorldTile` (`:1726`/`:1728`).

**Creates** — no entity; a `Step` struct on `Mobile.Steps` (`:1825`/`:2002`).

**Destroys / pools** — `CloseBank` calls `World.RemoveItem(first, true)` on every bank item (`PlayerMobile.cs:1507`), destroying them and pooling them via the sweep (`World.cs:407`); bank `ContainerGump`/`GridContainer` disposed (`:1515`/`:1517`).

**Triggers** — **`NetClient.Socket.Send_WalkRequest(...)` (`PlayerMobile.cs:1837`/`:2015`) — a server-reported swing can make the client emit a walk/turn request.**

**Ignores / partial**
- `:2046-2049` `!World.InGame` → return, nothing read.
- `:2055-2058` `attackers != World.Player` → return. Swings where the player is the defender or an observer are dropped entirely; the defender serial is not even read.
- `:2064-2069` the auto-face happens only when ALL of: `TargetManager.LastAttack == defenders`, `InWarMode`, `Walker.LastStepRequestTime + 2000 < Time.Ticks`, `Steps.Count == 0`.
- `:2071-2073` unknown defender → nothing.
- `:2086-2089` `Pathfinder.CanWalk` must succeed **and** `Direction` must already differ.
- `PlayerMobile.cs:1676` `Walk` routes to `WalkNotAvoid` when `AutoAvoidObstacules` is off or `Pathfinder.AutoWalking`; otherwise the avoiding branch may substitute a different direction (`:1721-1729`) or refuse (`:1732`).
- `PlayerMobile.cs:1685`/`:1892` `Walk` returns false with no state change on `WalkingFailed`, cooldown, `StepsCount >= MAX_STEP_COUNT`, or (`CV_60142+`) `IsParalyzed`.
- `PlayerMobile.cs:1692-1695`/`:1899-1902` run forced false under `SpeedMode >= CantRun`, `Stamina <= 1` while alive, or hidden with `AlwaysRunUnlessHidden`.

### `0x32` — `Unknown_0x32` (`PacketHandlers.cs:2097`, registered `:261`)

**Len** `F=0x0002` (`PacketsTable.cs:91`). Body is `{ }` — the single payload byte is never read. Nothing is mutated, created, destroyed or triggered. Registered so the dispatcher consumes the packet.

### `0x38` — `Pathfinding` (`PacketHandlers.cs:2269`, registered `:262`)

**Len** `F=0x0007`. **Purpose** server hands the client a destination and asks it to walk there itself.

**Fields** — `[1..2]` u16BE x (`:2276`); `[3..4]` u16BE y (`:2277`); `[5..6]` u16BE z (`:2278`) — read **unsigned** and passed into an `int` parameter; no sign extension, no truncation to `sbyte` anywhere on this path. All three go to `Pathfinder.WalkTo(x, y, z, 0)` (`:2280`) — **the distance argument is hardcoded 0**.

**Mutates** — `Pathfinder` statics (`Game/Pathfinder.cs`): `_pointIndex=0` `:1014`, `_goalNode=null` `:1015`, `_run=false` `:1016`, `_startPoint.X/Y` `:1017-1018`, `_endPoint.X/Y` `:1019-1020`, `_endPointZ` `:1021`, `_pathfindDistance=0` `:1022`, `AutoWalking=true` `:1023`, then `_pointIndex=1` on success `:1027` or `AutoWalking=false` on failure `:1032`; `CleanupPathfinding` (`:1072`) empties `_openSet` `:1081`, `_closedSet` `:1090`, `_path` `:1092`, nulls `_goalNode` `:1093`; `_run` may become true at `:909`; `World.Player.Walker` state via `World.Player.Walk` (`:1053`).

**Creates** — `PathNode`s from a pool (`Pathfinder.cs:891` plus one per expanded neighbour); a `_path` list on success (`:930`).

**Destroys / pools** — `PathNode`s from the previous pathfind are returned to the pool by `CleanupPathfinding` (`:1078` open set, `:1086` closed set), **before** the new search, so any node still referenced from the previous run is handed back mid-handler.

**Triggers** — `EventSink.InvokeOnPathFinding(null, new Vector4(x,y,z,distance))` (`:1011`) — fired before any validation of the destination; `ProcessAutoWalk()` (`:1028`) → `World.Player.Walk(...)` (`:1053`), emitting outgoing `0x02` movement packets on this call stack; `StopAutoWalk()` (`:1055`/`:1060`).

**Ignores / partial**
- `:2271-2274` `!World.InGame` → drop.
- `Pathfinder.cs:1006-1009` `WalkTo` refuses outright when `World.Player == null` or `IsParalyzed`; the server's destination is discarded. The stamina check on the same line is commented out.
- `Pathfinder.cs:1025-1033` if `FindPath` cannot reach the destination within `PATHFINDER_MAX_NODES` the client sets `AutoWalking = false` and never moves; the server is not told.
- `Pathfinder.cs:923-926` the search breaks after `maxNodes` closed nodes.
- `Pathfinder.cs:912` the loop is conditioned on `AutoWalking`, so anything clearing it aborts the search.

### `0x3A` — `UpdateSkills` (`PacketHandlers.cs:2099`, registered `:263`)

**Len** V. **Purpose** full skill list, single-skill delta, or replacement of the skill-name table.

**Fields** — `[3]` u8 type (`:2106`). Derived: `haveCap = (type != 0 && type <= 0x03) || type == 0xDF` (`:2107`); `isSingleUpdate = (type == 0xFF || type == 0xDF)` (`:2108`).
- `type == 0xFE`: `[4..5]` u16BE count (`:2112`); per entry bool haveButton (`:2119`), u8 nameLength (`:2120`), ASCII[nameLength] (`:2123`).
- otherwise, while `p.Position < p.Length`: u16BE id (`:2170`); u16BE realVal (`:2187`); u16BE baseVal (`:2188`); u8 Lock (`:2189`); u16BE cap **only if `haveCap`** (`:2192-2195`), otherwise cap is hardcoded **1000** (`:2190`).
- `id` decremented by 1 when `type == 0` or `0x02` (`:2182-2185`). Loop exits early at `:2172-2175` (no bytes left after id), `:2177-2180` (`id == 0 && type == 0`), `:2261-2264` (after one entry when `isSingleUpdate`).

**Mutates** — `SkillsLoader.Instance.Skills.Clear()` + `SortedSkills.Clear()` (`:2114-2115`, type `0xFE` only); `Skills.Add(new SkillEntry(...))` (`:2122-2124`); `SortedSkills` refilled and sorted by name, InvariantCulture (`:2127-2131`); `World.SkillsRequested = false` (`:2149`); `Skill.BaseFixed` (`:2241`, `Skill.cs:62`), `ValueFixed` (`:2242`, `:60`), `CapFixed` (`:2243`, `:64`), `Lock` (`:2244`, `:58`).

**Creates** — `SkillEntry` objects (`:2122`); `StandardSkillsGump { X=100, Y=100 }` (`:2156`); `SkillGumpAdvanced` (`:2163`).

**Destroys / pools** — nothing. `type 0xFE` clears the `SkillsLoader` name tables, but `World.Player.Skills` is a separate array sized at `PlayerMobile` construction (`PlayerMobile.cs:53-59`) and is not resized or rebuilt here.

**Triggers** — `GameActions.Print` "Your skill in X has increased/decreased by N", hue `0x58`, System, font 3 (`:2219-2233`); `Skill.InvokeSkillBaseChanged` (`:2249` → `Skill.cs:82-85`), `InvokeSkillValueChanged` (`:2251` → `:78-81`), `InvokeSkillCapChanged` (`:2253` → `:86-89`); `StandardSkillsGump.Update(id)` (`:2256`); `SkillGumpAdvanced.ForceUpdate()` (`:2257`). No outgoing packets.

**Ignores / partial**
- `:2101-2104` `!World.InGame` → drop.
- **`:2190` `cap` is not read from the wire unless `haveCap`; it is silently set to 1000** and written into `Skill.CapFixed` at `:2243`.
- `:2182-2185` the id off-by-one adjustment applies only to types 0 and `0x02`.
- `:2197` an entry whose `id >= World.Player.Skills.Length` is fully parsed and silently discarded — no growth, no log.
- `:2201` an entry whose `Skills[id]` is null is discarded.
- `:2208-2217` the change message is suppressed unless `change != 0`, `!float.IsNaN(change)`, profile non-null, `ShowSkillsChangedMessage`, and either `ShowSkillsChangedDeltaValue <= 0` or the integer-division bucket differs.
- **`:2246-2254` the Base/Value/Cap change events fire only on `isSingleUpdate`; a full skill-list rewrite at `:2241-2244` fires nothing.**
- `:2261-2264` only the FIRST entry is applied when `isSingleUpdate`, even if the packet carries more.
- `:2147` gump auto-open only when `!isSingleUpdate && (type==1 || type==3 || World.SkillsRequested)`; which gump depends on `StandardSkillsGump` (`:2138-2145, :2152-2165`).
- `:2138` `ProfileManager.CurrentProfile` dereferenced without a null check while being null-checked at `:2206`/`:2211`.

### `0x3B` — `CloseVendorInterface` (`PacketHandlers.cs:2322`, registered `:264`)

**Len** V (`PacketsTable.cs:100`). **Purpose** close the vendor window.

**Fields** — `[3..6]` u32BE serial (`:2329`). Nothing else is read.

**Mutates** — no World state; the matched `ShopGump` is disposed (`:2331`), unlinking it from `UIManager.Gumps`.

**Creates** — nothing. **Destroys** — `ShopGump` whose `LocalSerial == serial` (`:2331`); `UIManager.GetGump<T>` walks from `Last` backwards skipping disposed controls (`UIManager.cs:328-336`), so a second `0x3B` finds nothing.

**Triggers** — none outgoing.

**Ignores / partial**
- `:2324` `!World.InGame` → return.
- **only `ShopGump` is disposed. `ModernShopGump` — which `SellList` creates at `:3648` under `UseModernShopGump` — is not looked up and is left open.**
- only the first (topmost) matching `ShopGump` is disposed; a second one for the same serial survives.

### `0x3C` — `UpdateContainedItems` (`PacketHandlers.cs:2283`, registered `:265`)

**Len** V (`PacketsTable.cs:101`). **Purpose** full contents of a container, one record per item, container serial repeated per record.

**Fields** — `[3..4]` u16BE count (`:2290`); per record: u32BE serial (`:2294`); `(ushort)(u16BE graphic + u8 graphicInc)` (`:2295`) — **the increment is folded in here with no `0x2006` exemption**, unlike `:6649`; u16BE amount clamped `Math.Max(...,1)` (`:2296`); u16BE x (`:2297`); u16BE y (`:2298`); one byte skipped on `>=CV_6017` (`:2300-2303`, the grid/slot index, never stored); u32BE containerSerial (`:2305`); u16BE hue (`:2306`). No check that `count` records fit.

**Mutates** — for record `i==0` only, `ClearContainerAndRemoveItems` on `World.Get(containerSerial)` (`:2314`) → `container.Items` rewritten (`:6914`); then per record `AddItemToContainer` (`:2318` → `:6389`): Graphic `:6440`, `CheckGraphicChange` `:6441`, Amount `:6442`, `FixHue` `:6443`, X `:6444`, Y `:6445`, **Z forced 0** `:6446`, `Container = containerSerial` `:6453` after `RemoveItemFromContainer` `:6452`, `PushToBack` `:6456`, `((Item)container).Opened = true` when a gump exists `:6534`; `World.Items` inserts (`World.cs:577`); `RemoveItemFromContainer` writes `Container=0xFFFFFFFF`, nulls `Next`/`Previous`, `RemoveFromTile` (`World.cs:647-652`).

**Creates** — one pooled `Item` per record (`:6439` → `World.cs:576`, `Item.cs:255`); `GridLootGump` (`:6519-6521`).

**Destroys / pools** — `ClearContainerAndRemoveItems` → `World.RemoveItem(it, true)` per child (`:6908` → `World.cs:655`): recursive removal (`:667-674`), `OPL.Remove` (`:676`), `Destroy` (`:677`), `Items.Remove` + `ReturnToPool` (`:681-683`); `World.RemoveMobile(serial, true)` when a record's serial is in mobile range (`:6429` → `World.cs:689-717`) — the mobile and everything it carries; `World.RemoveItem(item, true)` at `:6436`; `GetOrCreateItem` recycles a destroyed entry (`World.cs:565-569`); `Item.Destroy` disposes the item's gumps when `Opened` (`Item.cs:274-291`).

**Triggers** — `ItemHold.Clear()` (`:6404`); `TradingGump.RequestUpdateContents` for a secure trade box (`:6465`), else paperdolls (`:6469-6470`); **`Send_BulletinBoardRequestMessageSummary` (`:6481`)**; `ContainerGump.CheckItemControlPosition` (`:6496`), `GridContainer` (`:6503`), `GridLootGump` (`:6524`), `NearbyLootGump` (`:6527`), the found gump (`:6537`), trading gump (`:6542`); the old-container refreshes inside `RemoveItemFromContainer` (`World.cs:625-638`).

**Ignores / partial**
- `:2285-2288` `!World.InGame` → return.
- **`:2308-2316` the container is cleared only for record 0.** If the packet carries records for more than one container, the later containers keep their previous contents.
- `:2312` clearing is skipped when `World.Get(containerSerial)` is null; the items are still attempted, and `AddItemToContainer` then logs "No container found" and returns (`:6417-6423`), so those records are dropped.
- `:6899-6905, :6914` `ClearContainerAndRemoveItems` with `remove_unequipped=true` (graphic `0x2006`) keeps every child whose `Layer != 0` and only re-heads the list at the first survivor; survivors' `Previous` pointers are left as they were.
- `:6886-6889` returns immediately for a null or empty container.
- `:6434` an existing item is torn out only when `item.Container != containerSerial && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid)`.
- `:6450` reassignment skipped when the item is already in that container.
- `:2296` amount clamped to 1; `:6446` Z forced to 0; `:2302` the `CV_6017` grid index read and thrown away.
- `:6507-6517` `GridLootGump` only when `GridLootType > 0` and `_requestedGridLoot == containerSerial`.

### `0x3F` / `0x40` — UltimaLive (conditional)

`0x3F` V, `0x40` `F=0xC9`. Registered **only** by `UltimaLive.Enable()` (`UltimaLive.cs:78-83`): `Handler.Add(0x3F, OnUltimaLivePacket)` and `Handler.Add(0x40, OnUpdateTerrainPacket)`. `OnUltimaLivePacket` seeks to absolute 13 and reads a u8 command (`UltimaLive.cs:87-88`), switching on it (e.g. `0xFF` hash query, which additionally requires `_UL != null && p.Length >= 15`). Until `Enable()` runs both ids are unhandled and discarded. `0x3F` is also a client→server id (`Send_UOLive_HashResponse`, §5 #119).

### `0x4E` — `PersonalLightLevel` (`PacketHandlers.cs:2334`, registered `:266`)

**Len** `F=0x0006` (`PacketsTable.cs:119`). **Purpose** light radiating from a specific mobile.

**Fields** — `[1..4]` u32BE serial, compared directly against `World.Player` (`:2341`); `[5]` u8 level (`:2343`).

**Mutates** — `World.Light.RealPersonal = level` (`:2350`; setter `IsometricLight.cs:78-79` assigns `_realPersonal` and calls `Recalculate()`); `World.Light.Personal = level` (`:2354`; `IsometricLight.cs:48-49`).

**Creates / destroys** — nothing. **Triggers** — `IsometricLight.Recalculate()` recomputes `IsometricLevel` (`IsometricLight.cs:49, :79`).

**Ignores / partial**
- `:2336-2339` `!World.InGame` → ignored.
- `:2341` a serial that is not the player's → ignored; the level byte is never even read.
- **`:2345-2348` `level` clamped down to `0x1E` when greater** — the server's value above 30 is silently reduced.
- `:2352` `World.Light.Personal` left alone under `UseCustomLightLevel`; only `RealPersonal` records what the server said.
- `:2352` `ProfileManager.CurrentProfile` dereferenced with no null check.

### `0x4F` — `LightLevel` (`PacketHandlers.cs:2359`, registered `:267`)

**Len** `F=0x0002` (`PacketsTable.cs:120`). **Purpose** global overall light level.

**Fields** — `[1]` u8 level (`:2366`).

**Mutates** — `World.Light.RealOverall` (`:2373` → `IsometricLight.cs:87` then `Recalculate()` → `IsometricLevel` `:96-101`); `World.Light.Overall` (`:2380-2383` → `IsometricLight.cs:58` then `Recalculate()`).

**Creates / destroys / triggers** — nothing.

**Ignores / partial**
- `:2361` `!World.InGame` → the level is dropped, not queued.
- **`:2368-2371` clamp: `level > 0x1E` forced to `0x1E` before anything is written, so `RealOverall` never records what the server actually sent above 30.**
- **`:2375-2378` partial apply: `Overall` is written only when `!UseCustomLightLevel || LightLevelType == 1`.** With `UseCustomLightLevel` on and `LightLevelType != 1` the server's level reaches `RealOverall` only.
- `:2382` with `LightLevelType == 1` it is further clamped to `min(level, Profile.LightLevel)` — the server can only ever make it darker than the user's ceiling.
- `:2376` `ProfileManager.CurrentProfile` dereferenced without a null check.

### `0x53` — `ReceiveLoginRejection` (`PacketHandlers.cs:6374`, registered `:356`)

**Len** `F=0x0002` (`PacketsTable.cs:124`). Shared with `0x82` and `0x85`.
**Purpose** login/connection error code.

**Fields** — `[1]` u8 code, read inside `LoginScene.HandleErrorCode` (`LoginScene.cs:724`); the handler itself reads nothing and forwards the reader by ref (`:6385`). `p[0]` selects the error table (`LoginScene.cs:726` → `ServerErrorMessages.cs:91`).

**Mutates** — `LoginScene.PopupMessage = ServerErrorMessages.GetError(p[0], code)` (`LoginScene.cs:726`, property `:86`); `LoginScene.CurrentLoginStep = LoginSteps.PopUpMessage` (`:727`). No World state.

**Creates** — nothing directly; `LoginScene.Update` rebuilds its gump on the changed step (`LoginScene.cs:190-199, 255-301`) and consumes/nulls `PopupMessage` at `:297`. **Destroys** — nothing.

**Triggers** — `ClilocLoader.Instance.GetString` for the message text (`ServerErrorMessages.cs:101/111/120/129`).

**Ignores / partial**
- `:6376` `World.InGame` → dropped; a mid-game `0x53`/`0x82`/`0x85` is discarded.
- `:6383` not on `LoginScene` → the code byte is never read.
- **codes are clamped, per id**: `0x53` `>= 10` → 9 (`ServerErrorMessages.cs:94-97`); `0x85` `>= 6` → 5 (`:104-107`); `0x82` `>= 9` → 8 (`:123-126`); an unrecognised id returns `string.Empty` (`:132`), leaving `CurrentLoginStep` forced to `PopUpMessage` with no visible message (`LoginScene.cs:293`).

### `0x54` — `PlaySoundEffect` (`PacketHandlers.cs:2387`, registered `:268`)

**Len** `F=0x0C` (12) (`PacketsTable.cs:125`). **Purpose** play a sound at a world location.

**Fields** — `[1]` skipped (`:2394`, the mode/flags byte is discarded); `[2..3]` u16BE index (`:2396`) — the sound id actually used; `[4..5]` u16BE audio (`:2397`) — read into a local and **never used**; `[6..7]` x (`:2398`); `[8..9]` y (`:2399`); `[10..11]` u16BE read then cast `(short)` → z (`:2400`) — read unsigned then reinterpreted, and never used.

**Mutates** — `UOSound.X`/`.Y` (`AudioManager.cs:191-192`); `CalculateByDistance = true` (`:193`); `_currentSounds.AddLast(sound)` (`:195`); playback state inside `UOSound.Play(Time.Ticks, volume, distanceFactor)` (`:189`).

**Creates** — a `UOSound` fetched via `Client.Game.Sounds.GetSound(index)` (`AudioManager.cs:187`) and linked into `_currentSounds`. **Destroys** — nothing.

**Triggers** — audio playback only. No packets, no gumps.

**Ignores / partial**
- `:2389-2392` `World.Player == null` → dropped.
- `AudioManager.cs:153-156` dropped when `!_canReproduceAudio` or `!World.InGame`.
- `AudioManager.cs:172-175` volume forced to 0 when distance `> World.ClientViewRange` — the sound is still fetched and queued, just silent.
- `AudioManager.cs:177-180` returns without playing if the computed volume is outside `[-1, 1]`.
- `AudioManager.cs:182-185` volume forced to 0 when the profile is null, `EnableSound` is off, or the window is inactive with `ReproduceSoundsInBackground` off.
- `AudioManager.cs:189` nothing happens if `GetSound` returns null or `Play()` returns false.
- z is parsed but never fed to the audio system; distance is 2D only (`AudioManager.cs:158-160`).
- the second u16 (`audio`, commonly volume/repeat) is parsed and thrown away.

### `0x55` — `LoginComplete` (`PacketHandlers.cs:2447`, registered `:237`)

**Len** `F=1` (`PacketsTable.cs:126`) — offset 1 is already end-of-packet. **Purpose** switch `LoginScene` → `GameScene` and do the post-login handshake.

**Fields** — **none.** The handler never touches `p`.

**Mutates** — `World.ClientViewRange = clamp(Settings.GlobalSettings.ClientViewRange, MIN_VIEW_RANGE=5, MAX_VIEW_RANGE=40)` (`:2468`; this fork's `MAX_VIEW_RANGE` is 40, `Constants.cs:117`); `GameController.Scene = new GameScene` (`GameController.cs:306`); `drawScene = Scene.IsLoaded` (`:310/312`); `HouseDiagnostics` buffered log flushed (`:305`); `GlobalActionCooldown.nextActionTime = Time.Ticks + Profile.MoveMultiObjectDelay` (`GlobalActionCooldown.cs:15`); `World.Player.HitsRequest = Pending` (`GameActions.cs:988`); `UIManager` gains every gump read from `gumps.xml` (`:2488`); `UIManager.SavePosition(serverSerial, Point)` per saved gump (`Profile.cs:908`); `SkillsGroupManager` static state loaded (`Profile.cs:865`).

**Creates** — `GameScene` (`:2451`); all `Gump` objects deserialized from `<profile>/gumps.xml` (`Profile.ReadGumps`, `Profile.cs:859+`, added `:2488`).

**Destroys** — the previous Scene (`LoginScene`) disposed (`GameController.cs:302`).

**Triggers** — `Send_StatusRequest` via `RequestMobileStatus` (`GameActions.cs:997`); `Send_OpenChat("")` (`:2455`); `Send_SkillsRequest` (`:2457`); `Send_ClientType()` (`:2461`); `Send_ClientViewRange` (`:2473`).

**Ignores / partial**
- `:2449` the whole handler is a no-op unless `World.Player != null` **and** the current scene is a `LoginScene`. A `0x55` arriving in `GameScene` is silently discarded.
- `:2459` `Send_ClientType` only on `>= CV_306E`.
- `:2464` the view-range block only on `>= CV_305D`; on older versions `World.ClientViewRange` keeps its default and the server is never told.
- `:2468-2471` the requested range is clamped both ends before being sent.
- `:2484` gump restore skipped if `ReadGumps` returns null.
- `:2480` `ProfileManager.CurrentProfile` dereferenced unguarded.
- frame context: runs inside `ProcessNetworkPackets` (`GameController.cs:478`), draining up to `MAX_PACKETS_PER_FRAME=25` buffers (`:136-146`). `SetScene` disposes the live scene while the remaining queued packets of the same frame are still to be dispatched.

### `0x56` — `MapData` (`PacketHandlers.cs:2494`, registered `:269`)

**Len** `F=0x000B` (`PacketsTable.cs:127`). **Purpose** manipulate pins/edit state of an open map gump.

**Fields** — `[1..4]` u32BE serial (`:2501`); `[5]` u8 `MapMessageType` (`:2507`, `MapMessageType.cs:37-43`: Add=1, Insert, Move, Remove, Clear, Edit, EditResponse); Add: `[6]` skipped (`:2510`), `[7..8]` x (`:2512`), `[9..10]` y (`:2513`); EditResponse: `[6]` u8 plot state (`:2535`). For every other message type nothing further is read.

**Mutates** — `MapGump._container` and child list gain a `PinControl` (`MapGump.cs:199-200`); `mapX`/`mapY`/`foundMapLoc` (`MapGump.cs:217-222`) and the hit-box tooltip (`:224`); `PlotState` and `IsVisible`/`IsEnabled` of `_buttons[0..2]` (`MapGump.cs:240-246`). **No World, Item, Mobile or Chunk state at all.**

**Creates** — `PinControl` (`MapGump.cs:195`), numbered from `_container.Count + 1` (`:198`).

**Destroys** — `MapMessageType.Clear` disposes every `PinControl` and empties the list (`MapGump.cs:232, :235`).

**Triggers** — nothing.

**Ignores / partial**
- `:2496` `!World.InGame` → return.
- **`:2503-2505` if no `MapGump` matches the serial, the message-type byte is never even read and the packet is dropped silently.**
- **`:2519-2532` Insert, Move, Remove and Edit are empty cases** — the server's insert/move/remove instructions are discarded.
- `:2510` Add throws away one byte the server sends before the coordinates.
- `MapGump.cs:198` pin numbering comes from the local container count, not from the wire.
- `MapGump.cs:201, :222` the estimated world-location computation runs only for the first pin ever added (`foundMapLoc` guard).

### `0x5B` — `SetTime` (`PacketHandlers.cs:2542`, registered `:270`)

**Len** `F=0x0004` (`PacketsTable.cs:132`) — 3 body bytes (hour, minute, second) are dequeued. The body is empty; no field is read. Nothing mutated, created, destroyed or triggered.

**Ignores** — `:2542` the entire packet is unconditionally discarded. The client keeps its own clock; the server's time is never applied. It is still logged by `PacketLogger`/`HouseDiagnostics` (`:142-143`).

### `0x65` — `SetWeather` (`PacketHandlers.cs:2544`, registered `:271`)

**Len** `F=0x0004` (`PacketsTable.cs:142`). **Purpose** weather type, particle count, temperature.

**Fields** — `[1]` u8 `WeatherType` (`:2554`); `[2]` u8 count — **read only when `weather.CurrentWeather != type`** (`:2558`); `[3]` u8 temp — same condition (`:2559`).

**Mutates** — `Weather.Type` (`Game/Weather.cs:101`), `Count` (`:102`), `Temperature` (`:103`), `_timer` (`:104`), `_lastTick` (`:106`), `_windTimer` (`:196`), `CurrentCount` and `_effects[]` (`:198-203`); `Weather.Reset()` first zeroes Type/Count/CurrentCount/Temperature/Wind/`_windTimer`/`_timer` and nulls `CurrentWeather` (`Weather.cs:83-90`); `CurrentWeather` written at `Weather.cs:132/149/168/187`. No World entity state.

**Creates** — `WeatherEffect` entries filled in place in the fixed `_effects` array with random positions from `Client.Game.Scene.Camera.Bounds` (`Weather.cs:198-203`).

**Destroys** — the previous weather state, via `Reset()` (`Weather.cs:99`).

**Triggers** — `EventSink.InvokeOnSetWeather` (`:2562`); `GameActions.Print` of the localized message (`Weather.cs:123, 140, 159, 178`); `PlayThunder()` (`:151, :189`); `PlayWind()` (`:170`).

**Ignores / partial**
- `:2546-2551` no `GameScene` → return, packet dropped.
- **`:2556` if `CurrentWeather` already equals the incoming type, the count and temp bytes are never read and `Generate` is never called.** `Weather.Generate` repeats the guard (`Weather.cs:94-97`).
- `Weather.cs:102` count clamped down to `MAX_WEATHER_EFFECT`.
- `Weather.cs:108-114` `WT_INVALID_0`/`WT_INVALID_1` zero `_timer`, leave `CurrentWeather` null and return early — Type and Count stay written but the weather is never current.
- `Weather.cs:116` `CurrentWeather` is only assigned inside `if (showMessage)`, i.e. only when `Count > 0` — a weather packet with count 0 leaves `CurrentWeather` null, so an identical next packet is not suppressed.
- `Weather.cs:118-193` an unrecognised `WeatherType` falls through the switch with no case, leaving `CurrentWeather` null.

### `0x66` — `BookData` (`PacketHandlers.cs:2566`, registered `:272`)

**Len** V. **Purpose** text of one or more book pages for an already-open book gump.

**Fields** — `[3..6]` u32BE serial (`:2573`); `[7..8]` u16BE pageCnt (`:2574`); then per page from offset 9: u16BE page number (`:2585`), stored as `pageNum = value - 1` (wire page 1 → index 0, wire page 0 → −1); u16BE lineCnt (`:2590`) **only if the page index passed the range test at `:2588`**; `lineCnt` NUL-terminated strings, `ReadUTF8(true)` when `ModernBookGump.IsNewBook` (`Client.Version > CV_200`) else `ReadASCII` (`:2598-2600`). No length prefix per line.

**Mutates** — `ModernBookGump.KnownPages` (`ModernBookGump.cs:86`) gains `pageNum` at `:2586` for **every** page in the packet, including out-of-range and negative values, before any validation; `BookLines[index]` (backing `_bookPage._pageLines`, `ModernBookGump.cs:81`, allocated `bookpages*8` at `:574`) at `:2598`; trailing lines blanked to `string.Empty` at `:2614`; then `ServerSetBookText` (`:2627`) rewrites the same array in place — strips embedded `\n` (`ModernBookGump.cs:108`), appends `\n` to lines that fit (`:124`), `_ServerUpdate = true` (`:128`), `SetText` (`:129`), `CaretIndex = 0` (`:130`), `UpdatePageCoords` (`:131`), `_ServerUpdate = false` (`:132`). No World, Entity, Chunk or House state — purely gump-local.

**Creates / destroys** — nothing.

**Triggers** — `ModernBookGump.ServerSetBookText()` (`:2627`), which re-lays out the whole book and queries `FontsLoader` for widths (`ModernBookGump.cs:114`).

**Ignores / partial**
- `:2568-2571` `!World.InGame` → drop.
- `:2576-2581` no `ModernBookGump` for that serial, or disposed → the page data is discarded without being parsed at all.
- **`:2588` a page is applied only when `pageNum < gump.BookPageCount && pageNum >= 0`. On the failing branch (`:2619-2624`) the handler logs and continues WITHOUT reading `lineCnt` or the lines — the reader is left inside that page's payload and every subsequent page in the same packet is parsed from the wrong offset.**
- **`:2596`/`:2604` a line whose computed index `>= BookLines.Length` is logged and the string is NOT read — same reader desync for the rest of the packet.**
- `:2586` the page is added to `KnownPages` before either check, so refused pages are still recorded as known.
- `:2610` the blank-fill only runs when `lineCnt < 8`; a server sending more than 8 lines for a page writes past that page's slice into the next page's lines (the index test at `:2596` bounds only the whole array).

### `0x6C` — `TargetCursor` (`PacketHandlers.cs:432`, registered `:273`)

**Len** `F=0x13` (19). **Purpose** open (or cancel) a targeting cursor.

**Fields** — `[1]` u8 `CursorTarget` (`:434`); `[2..5]` u32BE cursorId (`:435`); `[6]` u8 `TargetType` (`:436`). **The remaining 12 bytes (x, y, unused, z, graphic) are never read.**

**Mutates** — `TargetManager.IsTargeting = (cursorType < TargetType.Cancel)` (`TargetManager.cs:268`); `TargetingState` (`:269`); `TargetingType` (`:270`); `_targetCursorId` (`:285`); `World.Party.PartyHealTimer = 0` (`:443`); `PartyHealTarget = 0` (`:444`); `TargetManager.NextAutoTarget.Clear()` (`:445`, `:458`); `LastTargetInfo.SetEntity(serial)` (inside `Target()`, ~`TargetManager.cs:364/375`); on cancel, `World.HouseManager.Remove(0)` when the previous state was `MultiPlacement` (`TargetManager.cs:297` → `HouseManager.cs:267-275` → `House.ClearComponents` `House.cs:179-202`); on cancel, `CustomHouseManager.Erasing`/`SeekTile`/`SelectedGraphic`/`CombinedStair` reset (`TargetManager.cs:301-304`).

**Creates** — `QuestionGump` ("This may flag you criminal!") via `UIManager.Add`, from the auto-target path inside `Target()`.

**Destroys** — all `Multi` components of the serial-0 placement preview when a `MultiPlacement` cursor is cancelled (`TargetManager.cs:297` → `House.cs:188-199`). `HouseCustomizationGump` is only `Update()`'d, not disposed (`TargetManager.cs:306`).

**Triggers** — `Send_TargetCancel` (`TargetManager.cs:312`); `Send_TargetObject` from the auto-target/party-heal paths; `GameActions.RequestMobileStatus` when `LastTargetInfo.Serial != serial`; `ScriptRecorder.Instance.RecordTarget`; `HouseCustomizationGump.Update()` (`TargetManager.cs:306`).

**Ignores / partial**
- `TargetManager.cs:262-265` `SetTargeting` returns changing nothing when `cursorTarget == CursorTarget.Invalid` — the server's cursor is silently dropped.
- `TargetManager.cs:276-279` `CancelTarget` runs only when the previous state was targeting and the new type is `>= TargetType.Cancel`.
- `:440` party-heal auto-target fires only when `PartyHealTimer < Time.Ticks && PartyHealTarget != 0`; it takes priority and discards any queued `NextAutoTarget` (`:445`).
- **`:450-451` the queued auto-target fires only when BOTH `ExpectedCursorTarget == cursorTarget` AND `ExpectedTargetType == targetType`. On mismatch the target is NOT sent — but `NextAutoTarget` is cleared anyway (`:458`, comment "no queuing").**
- `TargetManager.cs:345-348` `Target()` returns doing nothing if `!IsTargeting`, and returns early leaving the cursor open if the criminal-query gump is shown.
- the x/y/z/graphic tail of the packet is ignored entirely.

### `0x6D` — `PlayMusic` (`PacketHandlers.cs:2405`, registered `:274`)

**Len** `F=0x0003` (`PacketsTable.cs:150`). **Purpose** region music track, or `0xFFFF` = "no music here".

**Fields** — `[1..2]` u16BE index (`:2407`). That is the whole packet.

**Mutates** — `MusicDiagnostics` anchor, unconditionally and before any decision: `_anchorX`/`_anchorY`/`_anchorMap` (`MusicDiagnostics.cs:54-56`), `_anchorTime` (`:59`), `_lastZoneBand = 0` (`:60`) via `ServerPacket` at `:2412`.
Stop path (`index == 0xFFFF`): `_lastServerIndex = MUSIC_STOP_INDEX` (`AudioManager.cs:472`), `_lastServerAt` (`:473`), `_currentMusicIndices[1] = -1` (`:475`); kept → `_mapPlayedTrack = playing` via `AdoptAsMapTrack` (`:652`, called `:486`); not kept → `StopMusic()` stops/disposes/nulls `_currentMusic[0]` and `[1]` (`:444-452`), `_currentMusicIndices[0] = -1` (`:493`), `ForgetMapTrack` (`:662` via `:495`).
Real-track path: `World.OldMusicIndex = index` (`:2441`, `World.cs:97`); `NotifyServerTrack` (`:2443`) sets `_lastServerIndex` (`AudioManager.cs:532`), `_lastServerAt` (`:533`), `ForgetMapTrack` (`:535/:662`); `PlayMusic` (`:2444`) on an accepted change does `StopMusic()` (`:349`), `_currentMusicIndices[idx] = music` (`:352`), `_currentMusic[idx]` (`:353`), `Play` (`:355`).
No `World.Items`/`World.Mobiles`/chunk state.

**Creates** — a `UOMusic` from `Client.Game.Sounds.GetMusic`, stored in `_currentMusic` (`AudioManager.cs:341, :353`).

**Destroys** — previously playing `UOMusic` objects are `Stop()`ed and `Dispose()`d in `StopMusic` (`AudioManager.cs:448-450`), reached from `StopMusicFromServer` (`:491`) and `PlayMusic` (`:345, :349`).

**Triggers** — `MusicDiagnostics` writes: `RawMusicPacket` (`:2411` → `MusicDiagnostics.cs:112`), `ServerPacket` (`:2412` → `:48`), `StopIgnored` (`:2430` → `:132`), `Kept` (`AudioManager.cs:484`), `Stopped` (`:441`), `Started` (`:359`) / `StartFailed` (`:370`) / `SameTrack` (`:376`). No outgoing packet, no gump.

**Ignores / partial**
- **`:2419` `index == Constants.MUSIC_STOP_INDEX` (0xFFFF, `Constants.cs:101`) is intercepted and never reaches `AudioManager.PlayMusic`** — the comment at `:2414-2418` records that `PlayMusic` would reject any index `>= MAX_MUSIC_DATA_INDEX_COUNT` (150, `Constants.cs:96`) at `AudioManager.cs:303` before it could stop anything.
- **`Settings.GlobalSettings.IgnoreServerStopMusic` (`Settings.cs:118`) makes the stop a no-op for playback** (`:2426-2433`): the running track is adopted as the map's instead (`AudioManager.cs:477-488`).
- `AudioManager.cs:478` even without that setting, the stop is ignored when the track already playing is the one the music map would pick.
- `AudioManager.cs:477, :504-507` the stop is only "kept" if something is still streaming.
- `:2441` `World.OldMusicIndex` is written **before** `AudioManager` can reject the index, so an index `>= 150` is still remembered as the region track.
- `AudioManager.cs:298/303/320-322/329-332/336` `PlayMusic` silently returns on `!_canReproduceAudio`, `music >= 150`, volume outside `[-1,1]`; a null profile or `!EnableMusic` gives volume 0 rather than returning; `!EnableCombatMusic && iswarmode` returns.
- `AudioManager.cs:347, :373-377` nothing happens when the same `UOMusic` instance is already in `_currentMusic[0]` and not war mode.
- no `World.InGame`/`World.Player` guard — the handler runs on the login scene too; `MusicDiagnostics.ServerPacket` skips the anchor when `World.Player` is null (`MusicDiagnostics.cs:52`).

### `0x6E` — `CharacterAnimation` (`PacketHandlers.cs:2630`, registered `:276`)

**Len** `F=0x000E` (14) (`PacketsTable.cs:151`). **Purpose** play an animation group on a mobile.

**Fields** — `[1..4]` u32BE serial via `World.Mobiles.Get` (`:2632`); `[5..6]` u16BE action (`:2639`); `[7..8]` u16BE frame_count (`:2640`) — narrowed to `byte` at `:2649`; `[9..10]` u16BE repeat_count (`:2641`) — narrowed at `:2650`; `[11]` bool, **INVERTED**: `forward = !p.ReadBool()` (`:2642`); `[12]` bool repeat (`:2643`); `[13]` u8 delay (`:2644`), passed as the animation interval.

**Mutates** — `Mobile._animationGroup` (`Mobile.cs:393`); `AnimIndex = forward ? 0 : frameCount` (`:394`); `_animationInterval = delay` (`:395`); **`AnimationFrameCount = forward ? 0 : frameCount`** (`:396` — on a forward animation the server's frame count is discarded and stored as 0); `_animationRepeateMode`/`_animationRepeatModeCount = repeatCount` (`:397-398`); `_animationRepeat` (`:399`); `_isAnimationForwardDirection` (`:400`); `AnimationFromServer = true` (`:401`); `LastAnimationChangeTime` (`:402`); idle timer reseeded (`:404`).

**Creates / destroys** — nothing.

**Triggers** — `Mobile.GetReplacedObjectAnimation(graphic, action)` remaps the group through `AnimationsLoader.Instance.GroupReplaces` and takes it modulo the group's `AnimationCount` (`MobileAnimation.cs:1490-1535`) — the group the server asked for can be silently substituted or wrapped.

**Ignores / partial**
- `:2634-2637` unknown mobile → return. **There is no `World.Player` null guard and no `World.InGame` guard.**
- `World.Mobiles.Get` (`EntityCollection.cs:40`) does not filter destroyed entities, unlike `World.Get` (`World.cs:551-554`) — an animation can be applied to an `IsDestroyed` Mobile still filed in the dictionary.
- `:2649-2650` frame_count and repeat_count arrive 16-bit and are cast to `byte`/`ushort`; frame_count above 255 wraps.
- `:2642` the forward flag is the logical inverse of the wire byte.
- `MobileAnimation.cs:1507/1526` `GetReplacedObjectAnimation` returns the index unchanged when the graphic's group is neither Low nor People.

### `0x6F` — `SecureTrading` (`PacketHandlers.cs:462`, registered `:275`)

**Len** V (`PacketsTable.cs:152`). **Purpose** trade window open/close/accept/gold.

**Fields** — `[3]` u8 type (`:469`); `[4..7]` u32BE serial (`:470`).
- type 0 (open): u32BE id1 (`:474`), u32BE id2 (`:475`), bool hasName (`:483`), then null-terminated ASCII name (`:488`) read only when `hasName && p.Position < p.Length`
- type 1 (close): nothing further
- type 2 (accept): u32BE id1 (`:499`), u32BE id2 (`:500`) — each used only as a non-zero test
- type 3 (his gold): u32BE `HisGold` (`:525`), u32BE `HisPlatinum` (`:526`)
- type 4 (my gold): u32BE `Gold` (`:520`), u32BE `Platinum` (`:521`)
- any other type reads nothing beyond type and serial

**Mutates** — `TradingGump.ImAccepting = id1 != 0` (`:506` → `TradingGump.cs:151-162`, setter calls `SetCheckboxes` on change); `HeIsAccepting = id2 != 0` (`:507` → `:164-175`); `Gold` (`:520` → `:83-98`), `Platinum` (`:521` → `:100-115`), `HisGold` (`:525` → `:117-132`), `HisPlatinum` (`:526` → `:134-149`). **No World, Item, Mobile, Chunk or House state.**

**Creates** — `TradingGump(serial, name, id1, id2)` for type 0 (`:491` → `TradingGump.cs:66-78`).
**Destroys** — `TradingGump` disposed for type 1 (`:495`; `GetTradingGump` matches on `ID1`, `ID2` or `LocalSerial`).

**Triggers** — `TradingGump.RequestUpdateContents` for type 2 (`:509`); the gump's `UpdateContents` reads `World.Get(ID1)` and walks its item list (`TradingGump.cs:177-184`); coin label text is rewritten by the setters only on `>= CV_704565` (`TradingGump.cs:92, 109, 126, 143`).

**Ignores / partial**
- `:464-467` `!World.InGame` → return.
- `:478-481` type 0 aborts after reading id1/id2 if `World.Get(id1)` or `World.Get(id2)` is null — the standard-client rule that an invisible trader suppresses the window; the hasName flag and name are then never read.
- `:486` the name is read only when `hasName && p.Position < p.Length`.
- `:495, :504, :516` types 1-4 do nothing when `GetTradingGump(serial)` is null.
- `TradingGump.cs:88, 105, 122, 139` the Gold/Platinum setters short-circuit on an unchanged value, **and skip the label write entirely below `CV_704565`** — the field is stored but never displayed.
- `TradingGump.cs:156, :169` `ImAccepting`/`HeIsAccepting` only re-run `SetCheckboxes` when the boolean flips.

### `0x70` — `GraphicEffect` (`PacketHandlers.cs:2657`, registered `:277`)

**Len** `F=0x1C` (28) (`PacketsTable.cs:153`). One method serves `0x70`, `0xC0` and `0xC7`, branching on `p[0]`.
**Purpose** spawn a visual effect.

**Fields** — `[1]` u8 `GraphicEffectType` (`:2664`); `[2..5]` source (`:2684`); `[6..9]` target (`:2685`); `[10..11]` graphic (`:2686`); `[12..16]` srcX/srcY/i8 srcZ (`:2687-2689`); `[17..21]` targetX/targetY/i8 targetZ (`:2690-2692`); `[22]` speed (`:2693`); `[23]` duration (`:2694`); `[24..25]` unk — read and discarded (`:2695`); `[26]` fixedDirection (`:2696`); `[27]` doesExplode (`:2697`). ScreenFade sub-branch (`0x70` only): `Skip(8)` then u16BE val (`:2670-2671`).

**Mutates** — a new `GameEffect` pushed onto `EffectManager`'s list (`EffectManager.cs:242` via `World.SpawnEffect` `World.cs:743`); the effect linked into the map chunk tile list: `GameEffect.SetSource` → `SetInWorldTile` (`GameEffect.cs:181/188`) → `GameObject.cs:265-272` → `Chunk.cs:172-180`; hue incremented by 1 when non-zero (`EffectManager.cs:88-91`); duration multiplied by `Constants.ITEM_EFFECT_ANIMATION_DELAY` (50) (`:93`).

**Creates** — `MovingEffect` (type 0, `EffectManager.cs:109`), `LightningEffect` (type 1, `:169`), `FixedEffect` (type 2 FixedXYZ `:188` / type 3 FixedFrom `:212`). `DragEffect` (`:145`) is **not reachable** from this handler.

**Destroys** — nothing here; effects are destroyed later by `EffectManager.Update` when `Distance > World.ClientViewRange` (`EffectManager.cs:50-53`).

**Triggers** — `Log.Warn("Effect not implemented")` for ScreenFade (`:2678`); `Log.Warn` in the manager (`EffectManager.cs:231, :236`).

**Ignores / partial**
- `:2659` `World.Player == null` → return.
- **`:2666-2682` any type `> GraphicEffectType.FixedFrom` (0x03) returns without spawning.** That covers `ScreenFade` (0x04) and `DragEffect` (0x05), so `0x70`/`0xC0`/`0xC7` can never produce a drag or a fade.
- the ScreenFade sub-branch runs only for `p[0] == 0x70`; it clamps `val` to 4 (`:2673-2676`), logs, and throws the value away.
- **`:2701-2705` hue and blendmode are read only for `p[0] != 0x70`. For `0x70` the effect is spawned with hue 0 and blendmode Normal regardless of anything on the wire.**
- `:2705` `blendmode = value % 7` — `GraphicEffectBlendMode.ScreenRed` (0x07) is unreachable and folds to Normal.
- `:2723` hue is read as u32 and truncated to `ushort` at the `SpawnEffect` call.
- `:2734` `hasparticles` is hardcoded `false` at the call site.
- `:2709-2714` the whole `0xC7` tail (tile id, explode effect/sound, serial, layer) is parsed and discarded.
- `EffectManager.cs:98, 135, 183, 207` the effect is dropped when `graphic <= 0`.
- `EffectManager.cs:104, 140` `speed == 0` is bumped to 1 for Moving and Drag only.
- `EffectManager.cs:197, 222` FixedXYZ and FixedFrom pass speed 0 regardless of the wire speed byte.
- `FixedEffect.cs:73-79` FixedFrom falls back to the source coordinates when the source serial is absent or invalid.
- `LightningEffect.cs:40` Lightning ignores the wire graphic and duration entirely (hardcoded `0x4E20` and 400).

### `0x71` — `BulletinBoardData` (`PacketHandlers.cs:2746`, registered `:278`)

**Len** V (`PacketsTable.cs:154`). **Purpose** open a board, add a summary line, or open one message.

**Fields** — `[3]` u8 subcommand (`:2753`).
- case 0 (open): `[4..7]` board serial (`:2758`); `[8..29]` UTF8 fixed 22 bytes, `safe=true`, board name (`:2771` — read lazily as the 4th ctor argument, so only consumed when the item exists)
- case 1 (summary): `[4..7]` board serial (`:2783`); `[8..11]` message serial (`:2790`); `[12..15]` parentID (`:2791`, read and **never used**); then u8 len + UTF8 poster (`:2794-2795`), u8 len + UTF8 subject (`:2798-2799`), u8 len + UTF8 datetime (`:2802-2803`), concatenated with `" - "`
- case 2 (message): `[4..7]` board serial (`:2814`); `[8..11]` message serial (`:2821`); u8 len + **ASCII** poster (`:2823-2824` — ASCII here, not UTF8, unlike case 1); u8 len + UTF8 subject (`:2826-2827`); u8 len + **ASCII** datetime (`:2829-2830`); `Skip(4)` (`:2832`); u8 unk (`:2834`) then `Skip(unk * 4)` (`:2838`); u8 lines (`:2841`); then per line u8 lineLen + UTF8 (`:2848-2852`), each with a trailing `\n`

**Mutates** — `item.Opened = true` (`:2774`, `Item.cs:249`) — the only World-entity write the whole handler makes; `BulletinBoardGump._databox` children via `AddBulletinObject` (`BulletinBoardGump.cs:151-168`: disposes an existing child with the same `LocalSerial` `:157`, adds a `BulletinBoardObject` `:163-164`, `WantUpdateSize` `:166`, `ReArrangeChildren` `:167`); `UIManager.Gumps` (`UIManager.cs:479-494`).

**Creates** — case 0: `BulletinBoardGump` (`:2771`, ctor `BulletinBoardGump.cs:50+`) with `LocalSerial = board serial`, positioned at `(window.Width/2 - 245, window.Height/2 - 205)` (`:2768-2769`), added `:2772`; case 1: a `BulletinBoardObject` child (`BulletinBoardGump.cs:163`); case 2: a `BulletinBoardItem` gump (`:2861-2875`, ctor `BulletinBoardGump.cs:183+`) at X=40, Y=40, plus a stack-allocated 256-char `ValueStringBuilder` (`:2843-2844`) disposed at `:2877`.

**Destroys** — case 0: any pre-existing `BulletinBoardGump` for the same serial (`:2763-2766`); case 1: a same-serial `BulletinBoardObject` child (`BulletinBoardGump.cs:157`).

**Triggers** — gumps only. No outgoing packets. The board gump installs a `HitBox` MouseUp handler (`BulletinBoardGump.cs:80-95`) that later disposes any `BulletinBoardItem` and opens a blank one — deferred, not part of this handler.

**Ignores / partial**
- `:2748-2751` `!World.InGame` → drop.
- **`:2759-2761` case 0: if `World.Items.Get(serial)` is null the gump is not created, the board-name bytes are never read, and nothing at all happens.** Uses `World.Items.Get` directly (item-only), not `World.Get`.
- **`:2784-2788` / `:2815-2819` cases 1 and 2 are discarded entirely if no `BulletinBoardGump` exists for that board serial; the poster/subject/date bytes are never read.**
- `:2795, 2799, 2803, 2824, 2827, 2830` each length-prefixed string collapses to `string.Empty` when its length byte is `<= 0`.
- `:2850` zero-length body lines are skipped, so no blank line is emitted.
- `:2868` leading whitespace of the assembled body is trimmed.
- `:2859` `variant` is 2 when the poster string equals `World.Player.Name`, else 1 — a plain string comparison decides the button set.
- case 1 reads poster/subject/date as UTF8 while case 2 reads poster and datetime as ASCII and subject as UTF8 — the two subcommands decode the same fields differently.
- `parentID` (case 1) is parsed and discarded — no reply threading.
- `:2753-2882` any subcommand other than 0/1/2 falls out of the switch with no default branch.

### `0x72` — `Warmode` (`PacketHandlers.cs:2885`, registered `:279`)

**Len** `F=0x0005` (`PacketsTable.cs:155`). **Purpose** war-mode state.

**Fields** — `[1]` bool war flag (`:2892`); `[2..4]` the remaining three bytes are never read.

**Mutates** — `PlayerMobile.InWarMode` backing field (`PlayerMobile.cs:76`, auto-property), written from `:2892`. **`Mobile.Flags` is not touched**: the base `Mobile.InWarMode` getter derives from `(Flags & Flags.WarMode)` and its setter is an empty body (`Mobile.cs:174-178`); `PlayerMobile` overrides both with an auto-property, so for the player the stored bool and `Flags.WarMode` are independent values.

**Creates / destroys / triggers** — nothing.

**Ignores / partial**
- `:2887` `!World.InGame` → return (dereferences `World.Player` immediately after, relying on `InGame` implying `Player != null`, `World.cs:163`).
- the same assignment against any non-player Mobile would be silently discarded by the empty setter at `Mobile.cs:177`.
- the three trailing bytes the protocol carries are ignored.

### `0x73` — `Ping` (`PacketHandlers.cs:2895`, registered `:280`)

**Len** `F=0x0002` (`PacketsTable.cs:156`). **Purpose** echo of a client ping sequence byte.

**Fields** — `[1]` u8 ping index (`:2897`), passed straight into `NetStatistics.PingReceived(byte idx)`.

**Mutates** — `_pings[idx % 5] = Time.Ticks - _startTickValue` (`NetStatistics.cs:106`); `LastPingReceived = Time.Ticks` (`:107`, used by the connection-timeout logic).

**Creates / destroys / triggers** — nothing. No reply packet.

**Ignores / partial**
- no `World.Player`/`World.InGame` guard — runs on the login scene too.
- `NetStatistics.cs:106` the index is not validated, it is folded with `% _pings.Length` (5). A server echoing an out-of-range or wrong index overwrites an arbitrary slot rather than being rejected.
- `NetStatistics.cs:106` the delta is computed against `_startTickValue`, which `SendPing` (`:131`) overwrites on every send. The measurement is against the most recent ping sent, not the one carrying this index; with more than one in flight the sample is wrong rather than discarded.

### `0x74` — `BuyList` (`PacketHandlers.cs:2900`, registered `:281`)

**Len** V (`PacketsTable.cs:157`). **Purpose** prices and display names for items already delivered into a vendor's buy container.

**Fields** — `[3..6]` u32BE container serial (`:2907`); `[7]` u8 count — read **only if** `container.Layer` is `ShopBuyRestock` or `ShopBuy` (`:2944-2946`); then per entry u32BE price (`:2982`), u8 nameLen (`:2983`), ASCII(nameLen) name (`:2984`). **The vendor serial is not on the wire** — it is taken from `container.Container` (`:2914`).

**Mutates** — `Item.Price` (`Item.cs:251`) at `:2982`; `Item.Name` (`Entity.cs:85`) at `:2988` (from OPL), `:2992` (cliloc), `:3000` (`ItemData.Name`) or `:3004` (literal); the container's child linked list reordered **in place** when `container.Graphic == 0x2AF8` — `SortContents` (`:2960` → `LinkedObject.cs:223-315`) rewrites `Next`/`Previous` on every child and reassigns `container.Items` (`LinkedObject.cs:308`).

**Creates** — `ModernShopGump` (`:2927`) or `ShopGump` (`:2939-2940`).

**Destroys** — the existing `ModernShopGump` is disposed unconditionally when the modern gump is enabled (`:2926`); the existing `ShopGump` is disposed when its `LocalSerial` differs from the vendor or it is not a buy gump (`:2931-2934`).

**Triggers** — `UIManager.Add` (`:2927`/`:2940`). No outgoing packet.

**Ignores / partial**
- `:2902` `!World.InGame` → return.
- `:2909` unknown container item → return before any gump work.
- `:2916` unknown vendor mobile → return.
- **`:2944` if `container.Layer` is neither `ShopBuyRestock` nor `ShopBuy`, the gump is still created/replaced but the entire price list is never parsed and every price the server sent is dropped.**
- `:2950-2953` `container.Items == null` → return with the gump already created.
- **`:2975-2978` the loop breaks the moment the client's local item list runs out, even if `count` says there are more entries; the remaining wire data is silently abandoned.**
- name resolution order: OPL name wins over everything (`:2986`), then a purely numeric name is treated as a cliloc id (`:2990-2997`), then an empty name falls back to `ItemData.Name` (`:2998-3001`), else the literal (`:3004`) — **the server's string is discarded whenever OPL already has one**.
- `:2957-2971, :3007-3014` iteration direction depends on graphic: `0x2AF8` sorts ascending by X and walks forward, everything else walks backwards from the tail.
- `:2924` `ProfileManager.CurrentProfile` dereferenced without a null check.

### `0x77` — `UpdateCharacter` (`PacketHandlers.cs:3019`, registered `:282`)

**Len** `F=0x0011` (17) (`PacketsTable.cs:160`). Shared method with `0xD2` (`F=0x0019`, 25).
**Purpose** update an already-known mobile's body, position, direction, hue, flags, notoriety.

**Fields** — `[1..4]` serial (`:3026`); `[5..6]` graphic (`:3034`); `[7..8]` x (`:3035`); `[9..10]` y (`:3036`); `[11]` i8 z (`:3037`); `[12]` u8 Direction (`:3038`) — the **full byte including the Running bit**, not masked here; `[13..14]` hue (`:3039`); `[15]` Flags (`:3040`); `[16]` NotorietyFlag (`:3041`). Reader ends at 17; for `0xD2` the remaining 8 bytes are never read.

**Mutates** — `mobile.NotorietyFlag` (`:3043`, `Mobile.cs:230`), written for every mobile including the player, before any branch.
Player branch: `Flags` (`:3047`), `Graphic` (`:3048`, setter `GameObject.cs:107` also writes `originalGraphic`, runs `GraphicsReplacement.Replace`, rewrites Hue, calls `OnGraphicSet`), `CheckGraphicChange()` (`:3049`, `Mobile.cs:1054` writes `IsFemale` and `Race`), `FixHue(hue)` (`:3050`, `Entity.cs:115-134`).
Non-player branch: `UpdateGameObject(serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, type:1, 1)` (`:3055`) — direct position write X/Y/Z/Direction/IsRunning/ClearSteps at `:6708-6716` (unknown to `World.Get`, or parked at `0xFFFF,0xFFFF`) or `:6718-6726` (EnqueueStep refused); `Graphic = graphic & 0x3FFF` (`:6729`); `FixHue` (`:6730`); `Flags` (`:6731`); `SetInWorldTile` (`:6770` → `GameObject.cs:267-269, :270-271` → `Chunk.AddGameObject` after `RemoveFromTile` `GameObject.cs:220`). `EnqueueStep` (`Mobile.cs:304`) pushes up to three `Step` structs (`Mobile.cs:340, 348, 358`), may `SetAnimation(0xFF)` (`:322`) and write `LastStepTime` (`:325`). `ItemHold.UpdatedInWorld = true` (`:6585`).

**Creates** — only through `UpdateGameObject`, and only if the mobile vanished between the `Get` at `:3027` and the call: `World.GetOrCreateMobile` (`:6596` → `World.cs:600-601`). In practice the guard at `:3029-3032` makes this unreachable for `0x77`.

**Destroys / pools** — nothing directly. `GetOrCreateMobile` can return a destroyed entry to the pool (`World.cs:589-593`) before creating a replacement.

**Triggers** — held-item container `RequestUpdateContents` (`:6576, :6580, :6581`); `GameActions.SingleClick` → outgoing `0x09` (`:6740`, `GameActions.cs:660`) on the created path with `ShowNewMobileNameIncoming`; `GameActions.RequestMobileStatus` → outgoing status request (`:6777`) on the created path; `HouseDiagnostics.LogRangeProbe` (`:6758`).

**Ignores / partial**
- `:3021-3024` `World.Player == null` → drop (a null check, not `World.InGame`).
- **`:3029-3032` unknown mobile → the server's description is discarded entirely, including its notoriety. A `0x77` for a mobile the client has never seen creates nothing.**
- **`:3045-3052` for the player itself the handler deliberately IGNORES x, y, z and direction** — comment at `:3051`: "x,y,z, direction cause elastic effect, ignore em for the moment". Only flags, graphic and hue are applied.
- direction is masked (`& Direction.Up`) and the running bit split out only inside `UpdateGameObject` (`:6705-6706`); the player branch never masks at all.
- `:6708` position is only slammed in directly when the mobile is unknown to `World.Get` or parked at `0xFFFF/0xFFFF`; otherwise the move is queued as walk steps (`:6718`) and applied over frames — the server's coordinates are deferred, not applied.
- `Mobile.cs:306-309` `EnqueueStep` returns false at `Steps.Count >= MAX_STEP_COUNT`, forcing the hard position write at `:6720-6725`.
- `Mobile.cs:313-316` a step identical to the current end position is swallowed (returns true, queues nothing).
- `:6729` graphic masked with `0x3FFF` — the top two bits the server sent are dropped.
- `Entity.cs:121-124` `FixHue` clamps any hue `>= 0x0BB8` down to 1.

### `0x78` — `UpdateObject` (`PacketHandlers.cs:3059`, registered `:283`)

**Len** V. Shared method with `0xD3`. **Purpose** full description of a mobile plus its entire equipment list.

**Fields** — `[3..6]` serial (`:3066`); `[7..8]` graphic (`:3067`); `[9..10]` x (`:3068`); `[11..12]` y (`:3069`); `[13]` i8 z (`:3070`); `[14]` Direction (`:3071`); `[15..16]` hue (`:3072`); `[17]` Flags (`:3073`); `[18]` NotorietyFlag (`:3074`); **6 bytes skipped at `[19..24]` only when `p[0] != 0x78`, i.e. for `0xD3`** (`:3127-3130`); then a repeating equipment record: u32BE itemSerial (`:3132`/`:3170`), u16BE itemGraphic (`:3139`), u8 layer (`:3140`), then item hue — u16BE unconditionally on `>= CV_70331` (`:3143-3146`), else **only when `(itemGraphic & 0x8000)`** with the high bit stripped (`:3147-3151`). Loop ends on `itemSerial == 0` or `p.Position >= p.Length` (`:3134`). For the player branch x/y/z/direction are read and never used.

**Mutates** — player path: `Graphic` (`:3081`), `CheckGraphicChange` (`:3082`), `FixHue(hue)` (`:3083`), `Flags` (`:3084`).
Non-player path: everything `UpdateGameObject` writes (`:3088` → `:6545`) — see `0x77`, plus the item branch `IsDamageable`/`IsMulti`/`Graphic` (`:6667-6669`), X/Y/Z (`:6672-6674`), `LightID` (`:6675`), `Layer` for graphic `0x2006` (`:6679`), `FixHue` (`:6682`), `Amount` (`:6689`), `Flags` (`:6690`), `Direction` (`:6691`), `CheckGraphicChange` (`:6692`).
`Mobile.NotorietyFlag` (`:3118`). Per equipment record: `Item.Graphic` (`:3154`), `FixHue` (`:3155`), **`Amount` forced to 1** (`:3156`), `World.RemoveItemFromContainer` (`:3157` → `World.cs:617-653`), `Container = serial` (`:3158`), `Layer` (`:3159`), `parMob.Mount = item` for `Layer.Mount` (`:3163`), `CheckGraphicChange` (`:3166`), `obj.PushToBack(item)` (`:3168`).
On a player dead-state transition: `World.ChangeSeason` (`:3183`/`:3187`) → `World.Season` (`World.cs:208`) and `UpdateGraphicBySeason` on **every object of every used chunk** (`World.cs:210-222`). `World.Player.Abilities[0]/[1]` (`:3195` → `PlayerMobile.cs:321-322`). `ent.HitsRequest = Pending` for new mobiles (`GameActions.cs:988` from `:6777`).

**Creates** — `Mobile` via `World.GetOrCreateMobile` (`:6596` → `World.cs:583-605`, pool `Mobile.cs:240-245`); `Item` via `World.GetOrCreateItem` (`:6615` → `World.cs:559-581`); one `Item` per equipment record (`:3153`); `House` + `Multi` components indirectly if `CheckGraphicChange` reaches `LoadMulti` (`Item.cs:633-645` → `:335-600`, `Multi.Create` at `:470`/`:546`).

**Destroys / pools** — **every child item of the object that is not `Opened` and not on `Layer.Backpack` is removed with `World.RemoveItem(serial, true)` (`:3107-3110`), destroyed and pooled (`World.cs:677-684`) — before the new equipment list is read.** The walk saves `next` before each removal (`:3100-3113`) because `World.RemoveItem` unlinks the current node. `GetOrCreateItem`/`GetOrCreateMobile` evict and pool stale destroyed entries (`World.cs:563-571` / `:587-595`). `Item.Destroy` disposes the purged child's gumps when it was `Opened` (`Item.cs:276-289`). In `LoadMulti`, an existing House gets `ClearComponents()`, destroying every `Multi` it holds (`Item.cs:359`, `House.cs:188-199`). `Entity.Destroy` sends `Send_CloseStatus` (`Entity.cs:198-206`).

**Triggers** — paperdoll `RequestUpdateContents` (`:3120-3121`, `:3191-3192`); **`GameActions.RequestEquippedOPL()` (`:3124` self-only inside the mobile branch, and again unconditionally at `:3193`)** — walks every Layer calling `World.OPL.Contains`, queueing mega-cliloc requests; `GameActions.RequestMobileStatus` → `Send_StatusRequest` for every newly created mobile (`:6777`, `GameActions.cs:997`); `GameActions.SingleClick` (`:6740`/`:6747`); `EventSink.InvokeOnItemCreated`/`InvokeOnItemUpdated` (`:6695`/`:6697`), `InvokeOnCorpseCreated` (`:6807`); `World.Player.TryOpenCorpses()` (`:6810`); `Audio.PlayMusic` via `ChangeSeason` (`World.cs:230-233`); container gump refreshes from `RemoveItemFromContainer` (`World.cs:627-637`); `HouseDiagnostics.LogRangeProbe`/`LogHouseItemArrived` (`:6758`/`:6802`); `GameScene.UpdateMaxDrawZ(true)` + `MiniMapGump.RequestUpdateContents` if `LoadMulti` runs (`Item.cs:591-593, :586`).

**Ignores / partial**
- `:3061-3064` `World.Player == null` → drop.
- **`:3078-3085` partial apply for self: only Graphic, hue and Flags are taken. The x/y/z/direction and notoriety the server sent for the player are read and thrown away.** Position for self comes from `0x77`/`0x20` instead.
- `:3091-3096` returns if `World.Get(serial)` is still null after the update; the equipment list is then never parsed.
- `:3107` the child purge spares only `Opened` items and `Layer.Backpack`.
- `:3116` notoriety applied only when `SerialHelper.IsMobile(serial)` **and** `obj is Mobile`; for an item serial the notoriety byte is discarded.
- `:3127` the 6-byte skip is conditional on `p[0] != 0x78`.
- **`:3143-3151` equipment hue rule differs by client version; misjudging it desynchronises the rest of the loop.** Below `CV_70331` with the `0x8000` bit clear, `item_hue` stays 0 and `FixHue(0)` overwrites any hue the item previously had.
- `:3136-3137` the `SerialHelper.IsItem(itemSerial)` guard is commented out — a mobile serial in the equipment list is still turned into an Item by `GetOrCreateItem` at `:3153`.
- `:3156` equipment `Amount` is always overwritten with 1.
- `:3134` the loop stops on `itemSerial == 0` or end of packet — a truncated packet silently ends the list.
- `:3175` season/music change only on a transition of `IsDead`; `Send_DeathScreen` is deliberately commented out at `:3182` with a note about sphere servers.
- **`type` is hardcoded 0 at the call site (`:3088`), so `UpdateGameObject`'s multi branch (`:6654-6664`) and damageable branch (`:6667`) are unreachable — an incoming `0x78`/`0xD3` for a multi will CLEAR `IsMulti`.**
- `graphic_inc` is hardcoded 0, so `:6649-6652` and `:6701` are no-ops.
- `count` is hardcoded 0 and promoted to 1 at `:6684-6686`, so every item described by this packet gets `Amount` 1.
- `:6708-6726` mobile position is only hard-set when unknown / at `0xFFFF` / EnqueueStep refuses.
- frame context: the equipment loop calls `GetOrCreateItem`, which can `Items.Remove` + `ReturnToPool` a stale entry (`World.cs:565-569`) while the same dictionary is what `World.Update` sweeps by key (`World.cs:424`).

### `0x7C` — `OpenMenu` (`PacketHandlers.cs:3199`, registered `:284`)

**Len** V (`PacketsTable.cs:165`). **Purpose** old-style selection menu — icon strip or gray radio list.

**Fields** — `[3..6]` serial (`:3206`); `[7..8]` id (`:3207`); u8 nameLen + ASCII name (`:3208`); u8 count (`:3209`); **u16BE menuid then `p.Seek(p.Position - 2)` rewinds (`:3211-3212`)** — the first entry's graphic field is re-read as the style discriminator.
- `MenuGump` branch, per entry: u16BE graphic (`:3222`), u16BE hue (`:3223`), u8 nameLen + ASCII (`:3224`)
- `GrayMenuGump` branch, per entry: `p.Skip(4)` discarding graphic+hue (`:3262`), u8 nameLen + ASCII (`:3263`)

No check that `count` entries fit in the remaining bytes.

**Mutates** — no World state; only `UIManager.Gumps` via `UIManager.Add` (`:3247`/`:3298` → `UIManager.cs:485`, `_needSort` `:492`).

**Creates** — `MenuGump(serial, id, name)` at X=100 Y=100 (`:3216`) when `menuid != 0`; `LocalSerial = serial`, `ServerSerial = id`, `IsFromServer = true` (`MenuGump.cs:52-57`); one `ItemView` child per drawable entry (`:140`). Or `GrayMenuGump` (`:3251`) positioned from `Client.Game.Window.ClientBounds` (`:3253-3254`), one `RadioButton` per entry (`MenuGump.cs:316`) plus two `Button`s id 0 and 1 (`:3278-3294`).

**Destroys** — nothing. Any menu gump already open for the same serial is left in place.

**Triggers** — `Client.Game.Arts.GetArt(graphic)` per entry (`:3226`). Deferred, on user action: `Send_MenuResponse(LocalSerial, ServerSerial, index, graphic, hue)` (`MenuGump.cs:148-156`), right-click `Send_MenuResponse(..., 0, 0, 0)` (`:171`), `Send_GrayMenuResponse` (`:332`, `:346`).

**Ignores / partial**
- `:3201` `!World.InGame` → return.
- **`:3228-3244` `MenuGump` branch: an entry whose art has `UV.Width == 0` or `UV.Height == 0` is skipped entirely — not added, and `posX` is not advanced. Its bytes are still consumed and its server-side index `i+1` is still burned, so remaining entries keep their correct index but the user cannot pick the skipped one.**
- `:3232-3239` `posY` is 0 when the art is `>= 47` tall, otherwise `(47 - height) >> 1`.
- `:3262` the gray branch throws away the graphic and hue of every entry.
- `:3267-3272` per-entry height clamped up to 21 and each subsequent offset reduced by 1.
- `MenuGump.cs:294, :297` `GrayMenuGump` is created with `CanCloseWithRightClick = false` and initial `ResizePic` Height `111111` until `SetHeight` at `:3296`.
- `:3211` the style discriminator is the *peeked* menuid, not a documented packet field — a first entry with graphic 0 selects the gray list.

### `0x82` — `ReceiveLoginRejection` (registered `:354`)

**Len** `F=0x0002` (`PacketsTable.cs:171`). Identical body to `0x53` (see above); the error table is chosen by `p[0]`, and for `0x82` any code `>= 9` is clamped to 8 (`ServerErrorMessages.cs:124-127`).

### `0x85` — `ReceiveLoginRejection` (registered `:355`)

**Len** `F=0x0002` (`PacketsTable.cs:174`). Identical body to `0x53`; codes `>= 6` clamped to 5 (`ServerErrorMessages.cs:104-107`).

### `0x86` — `UpdateCharacterList` (`PacketHandlers.cs:6344`, registered `:352`)

**Len** V (`PacketsTable.cs:175`). **Purpose** refreshed character list at the login screen (e.g. after a delete).

**Fields** — the handler reads nothing and forwards the reader by ref (`:6355`). `LoginScene.cs:759` `[3]` u8 character slot count; then `count` records of 60 bytes: 30-byte ASCII name `TrimEnd('\0')` (`LoginScene.cs:764`) followed by `p.Skip(30)` (`:766`) — the password field, never read.

**Mutates** — `Characters = new string[count]` (`LoginScene.cs:760`, the previous array discarded wholesale); `Characters[i] = name` (`:764`); `PopupMessage = null` (`:660`); `CurrentLoginStep = CharacterSelection` (`:662`); `_currentGump` (`:667`); `PopupMessage = null` again (`:673`).

**Creates** — `CharacterSelectionGump` (`LoginScene.cs:667`); a modal `LoadingGump` carrying `PopupMessage` with an OK button (`:671-672`); a fresh `string[]` (`:760`).

**Destroys** — `UIManager.GetGump<CharacterSelectionGump>()?.Dispose()` (`:663`); `_currentGump?.Dispose()` (`:665`). No world entity is involved.

**Triggers** — gumps only. No outgoing packet.

**Ignores / partial**
- `:6346-6349` `World.InGame` → the whole packet is discarded without being parsed.
- `:6351-6356` not on `LoginScene` → discarded, again without parsing.
- `LoginScene.cs:658-661` `PopupMessage` is cleared only when `CurrentLoginStep != PopUpMessage`.
- `LoginScene.cs:668` the `LoadingGump` is only created when `PopupMessage` is non-blank, which after `:660` can only happen on the `PopUpMessage` step.
- `LoginScene.cs:759-767` `count` is taken raw from the wire with no bound check against the declared packet length; the loop reads 60 bytes per record regardless.
- `LoginScene.cs:766` the 30-byte password field of each record is skipped, never stored.
- **unlike `ReceiveCharacterList` (`0xA9`), this path does NOT parse cities and does NOT read the `CharacterListFlags`, so `World.ClientFeatures` is left at whatever `0xA9` set.**

### `0x88` — `OpenPaperdoll` (`PacketHandlers.cs:3302`, registered `:285`)

**Len** `F=0x42` (66) (`PacketsTable.cs:177`). **Purpose** show a mobile's paperdoll with title and can-lift flag.

**Fields** — `[1..4]` u32BE serial, looked up with **`World.Mobiles.Get`, not `World.Get`** (`:3304`); `[5..64]` ASCII fixed 60 bytes title (`:3311`); `[65]` u8 flags — only bit `0x02` is used, as `CanLift` (`:3312, :3341, :3346`).

**Mutates** — `mobile.Title` (`:3314`, `Mobile.cs:238`); `PaperDollGump.CanLift` (`:3348`, `PaperdollGump.cs:137`); `PaperDollGump` title label (`:3349` → `PaperdollGump.cs:450`) or `ModernPaperdoll` title label (`:3320` → `ModernPaperdoll.cs:194`); gump `Location` set to the cached position or `(100,100)` on creation (`:3336-3341`). No entity position, hue, graphic or container state.

**Creates** — `ModernPaperdoll` when the profile uses it, the mobile is the player, and none exists (`:3326`); `PaperDollGump` when no paperdoll gump exists for that mobile (`:3341`).

**Destroys** — nothing.

**Triggers** — `GameActions.RequestEquippedOPL` on the modern path (`:3328`), which walks every Layer of `World.Player` and calls `World.OPL.Contains`, issuing property requests for anything uncached; `PaperDollGump.RequestUpdateContents` **only when `CanLift` actually changed** (`:3351-3354`); `SetInScreen`/`BringOnTop` on both paths (`:3321-3322`, `:3356-3357`).

**Ignores / partial**
- `:3306-3309` unknown mobile → return before reading text or flags. An item serial, or a mobile filed under `World.Items`, is never found because `World.Get` is not used.
- `:3315` the modern branch requires `UseModernPaperdoll` **and** `mobile.Serial == World.Player.Serial`; `World.Player` is dereferenced there without a null check and `World.InGame` is never tested.
- **`:3315-3329` on the modern path the flags byte is read but never applied — `CanLift` is not represented.**
- `:3345-3354` an existing gump has its title updated in place and is only asked to rebuild when the lift flag flipped.
- `:3336-3339` cached gump position used when available, otherwise `(100,100)`.

### `0x89` — `CorpseEquipment` (`PacketHandlers.cs:3362`, registered `:286`)

**Len** V (`PacketsTable.cs:178`). **Purpose** the equipment layers of a corpse container.

**Fields** — `[3..6]` u32BE corpse serial (`:3369`); `[7]` u8 Layer (`:3383`) — the loop sentinel; loop body: 4 bytes u32BE item serial (`:3387`), then 1 byte u8 next Layer (`:3399`). **The loop condition (`:3385`) tests `layer != Layer.Invalid && p.Position < p.Length` BEFORE reading the 4-byte serial and the following layer byte, so a record starting at the last byte reads past the declared payload.** The layer written to the item is `layer - 1` (`:3395`), one less than the wire value.

**Mutates** — `World.Items[item_serial]` on an unknown serial (`World.cs:577` via `GetOrCreateItem`); `item.Container = corpse serial` (`:3394`, `Item.cs:242`); `item.Layer = layer - 1` (`:3395`, `Item.cs:245`); `corpse.Items` re-linked at the tail (`LinkedObject.cs:57-79` via `:3396`); prior container unlinked and `Container` reset to `0xFFFFFFFF` before relinking (`World.cs:644, :647` via `:3393`); `Next`/`Previous` nulled (`World.cs:650-651`); the item unlinked from its map tile (`GameObject.cs:220-246` via `World.cs:652`).

**Creates** — `Item` instances from `Item._pool` (`Item.cs:257` via `World.cs:576`).

**Destroys / pools** — a previously-destroyed Item still filed under the same serial is dropped from `World.Items` and pooled before a fresh one is taken (`World.cs:565, :569`).

**Triggers** — `PaperDollGump`/`ModernPaperdoll` `RequestUpdateContents` for a mobile previous container (`World.cs:627-628`); `ContainerGump`/`GridContainer`/`NearbyLootGump` for an item previous container (`World.cs:632, 634, 637`). **No packet is sent back; no gump is opened by this handler.**

**Ignores / partial**
- `:3364-3367` `!World.InGame` → ignored.
- `:3372-3375` `World.Get(serial)` null, including a destroyed entity still in the dictionary (`World.cs:551-554`) → ignored.
- **`:3378-3381` ignored unless `corpse.Graphic == 0x2006` — the equipment list for anything the client does not currently believe is a corpse graphic is discarded entirely.**
- **`:3389-3397` items whose `(layer - 1) == Layer.Backpack` are skipped: the 4-byte serial is consumed but no item is created, no container is set and nothing is linked.**
- `:3385` the loop stops at the first `Layer.Invalid` byte, leaving any remaining payload unparsed.

### `0x8C` — `ReceiveServerRelay` (`PacketHandlers.cs:6329`, registered `:351`)

**Len** `F=0x000B` (`PacketsTable.cs:181`). **Purpose** hand off from login server to game shard.

**Fields** — all reads happen inside `LoginScene.HandleRelayServerPacket`: `[1..4]` u32 **LITTLE-endian** ip (`LoginScene.cs:732`, `ReadUInt32LE` — unlike every other multi-byte field here); `[5..6]` u16BE port (`:733`); `[7..10]` u32BE seed (`:734`).

**Mutates** — the existing socket disconnected synchronously (`LoginScene.cs:736`); `AsyncNetClient.Socket` replaced with a brand new instance (`:737`); `PacketHandlers.Handler.Reset()` clears both the main and plugin `CircularBuffer`s (`:738` → `PacketHandlers.cs:167-177`); `EncryptionHelper` global state re-initialised with the new seed (`:739`); a new TCP connection established (`:741`); compression enabled (`:745`). No World/Item/Mobile/Chunk/gump state.

**Creates** — a new `AsyncNetClient` and its socket connection (`LoginScene.cs:737, :741`).

**Destroys** — the previous socket (`:736`); all bytes still queued in the receive buffers are discarded by `Handler.Reset` (`PacketHandlers.cs:169, :174`).

**Triggers** — a raw 4-byte seed write on the new connection (`LoginScene.cs:748-749`); `Send_SecondLogin(Account, Password, seed)` (`:752`).

**Ignores / partial**
- `:6331` `World.InGame` → return immediately.
- `:6336-6341` nothing happens when the active scene is not a `LoginScene`.
- `LoginScene.cs:743` the seed write, compression and `Send_SecondLogin` are all skipped when `IsConnected` is false after `Connect`; no error is reported.
- `LoginScene.cs:736, :741` `Disconnect().Wait()` and `Connect().Wait()` block the frame thread synchronously (packets are dispatched from `GameController.cs:478`).
- **re-entrancy: `Handler.Reset()` (`LoginScene.cs:738`) clears the very `CircularBuffer` that `ParsePackets` is iterating under `lock (stream)` (`PacketHandlers.cs:101-153`); the lock is reentrant on the same thread, so the buffer is emptied mid-loop and the `while (stream.Length > 0)` walk terminates, dropping any packets that arrived in the same batch after `0x8C`.**

### `0x90` — `DisplayMap` (`PacketHandlers.cs:3403`, registered `:287`)

**Len** `F=0x13` (19) (`PacketsTable.cs:185`). Shared method with `0xF5` (`F=0x15`, 21).
**Purpose** open a treasure/city map gump for an item.

**Fields** — `[1..4]` serial (`:3405`); `[5..6]` gumpid (`:3406`) — passed to the ctor and then ignored by it (`MapGump.cs:57`); `[7..10]` startX/startY (`:3407-3408`); `[11..14]` endX/endY (`:3409-3410`); `[15..18]` width/height (`:3411-3412`); `[19..20]` facet — read **only** when `p[0] == 0xF5` (`:3423-3425`).

**Mutates** — `MapGump.mapX/mapY/mapEndX/mapEndY/mapFacet` via `MapInfos` (`MapGump.cs:186-190`, called `:3430`/`:3436`); `_mapTexture`, `Width`, `Height`, `WantUpdateSize` via `SetMapTexture` (`MapGump.cs:172-178`); `UIManager` gains the gump (`:3448`); `Item.Opened = true` (`:3454`).

**Creates** — `MapGump` (`:3414`); a `Texture2D` for the multimap image (`MultiMap.cs:29`); `ResizePic`, 3 `Button`s, `HitBox`, `MenuButton`, `ContextMenuControl` inside the ctor (`MapGump.cs:67-136`).

**Destroys** — `MapGump._mapTexture` is disposed before being replaced (`MapGump.cs:172`; no prior texture on a fresh gump).

**Triggers** — `Log.Error` + `Console.WriteLine` on texture failure (`:3444-3445`).

**Ignores / partial**
- **no `World.InGame`/`World.Player` guard at all** — the handler runs and adds a gump whenever the packet arrives.
- **no existing-gump check: nothing calls `UIManager.GetGump<MapGump>(serial)?.Dispose()` first, so a repeated `0x90` for the same serial stacks another `MapGump`.**
- `:3419`/`:3423` the facet path is taken when `p[0] == 0xF5` **or** `Client.Version >= CV_308Z`, but facet is only READ off the wire for `0xF5`. For a `0x90` on a `>= 308Z` client, `GetMap` is called with facet = 0 (not null) and `MapInfos` records facet 0 (`:3428`/`:3430`).
- `:3434-3436` older clients take the null-facet path and `MapInfos` defaults facet to `-1`.
- `MultiMap.cs:26-27` `GetMap` returns default (Texture null) when the pixels are empty or dimensions `<= 0` or `> 8192`; `SetMapTexture` is then skipped (`:3439`) and the gump keeps the wire width/height.
- `:3442-3446` any exception in texture creation is swallowed and logged; the gump is still added at `:3448` with no map image.
- `:3452` `Opened` is set only if the item is known to `World.Items`.
- `MapGump.cs:217-218` startX/startY are later overwritten in place by `AddPin` using a hardcoded `Width/300f` multiplier, so the recorded origin is destroyed by the first pin.
