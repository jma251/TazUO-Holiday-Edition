# net-outgoing

Partition = the client's packet *writing* side: the `Send_*` extension surface, the
per-opcode length table it consults, and the "enhanced packet" (0xCE) sub-protocol.

## Files

| path | lines | purpose |
| --- | --- | --- |
| `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/OutgoingPackets.cs` | 4751 | `NetClientExt` — ~140 `Send_*` extension methods on `AsyncNetClient`; every outbound packet the client can emit. |
| `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/PacketsTable.cs` | 429 | Static `short[255]` of fixed packet lengths by opcode; `-1` = variable-length. Mutated once at startup per client version. |
| `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/EnhancedOutgoingPackets.cs` | 34 | Outbound side of the 0xCE "enhanced packet" extension: `EnabledPackets` gate + BE header/length helpers. |
| `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/EnhancedPacketHandler.cs` | 74 | Inbound dispatcher for 0xCE sub-packets; owns `EPID = 0xCE` and the id→handler dictionary; the one handler replies via `SendEnhancedPacket()`. |
| `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/EnhancedPacketTypeEnum.cs` | 6 | `enum EnhancedPacketType : ushort { None, EnableEnhancedPacket }`. |

## Types

- `NetClientExt` (static) — `OutgoingPackets.cs:52`. All `Send_*` methods. Uniform body shape:
  `length = PacketsTable.GetPacketLength(ID)` → `new StackDataWriter(length < 0 ? 64 : length)` →
  write opcode → if variable, `WriteZero(2)` placeholder → payload → if variable,
  `Seek(1, Begin); WriteUInt16BE(BytesWritten)` else `WriteZero(length - BytesWritten)` →
  `socket.Send(writer.BufferWritten)` → `writer.Dispose()`.
- `PacketsTable` (static) — `PacketsTable.cs:37`. `GetPacketLength(int)` `:298`, `AdjustPacketSizeByVersion(ClientVersion)` `:303`.
- `EnhancedOutgoingPackets` (static) — `EnhancedOutgoingPackets.cs:7`. `SendEnhancedPacket` `:10`,
  private `SetHeader` `:23` (0xCE, 2-byte len placeholder, ushort type BE), `FinalLength` `:30`.
- `EnhancedPacketHandler` — `EnhancedPacketHandler.cs:9`. `const byte EPID = 0xCE` `:13`; static ctor `:15`;
  `EnableEnhancedPacket(ref StackDataReader, int version)` `:26`; `Handle(ref StackDataReader)` `:52`
  (reads id BE then version BE); instance `_handlers` `:48`; singleton `Handler` `:50`.
- `EnhancedPacketType` — `EnhancedPacketTypeEnum.cs:3`.

### Notable Send_* groupings (opcode → representative methods)
- Login/handshake: `Send_Seed` 0xEF `:183` (sent with `ignorePlugin=true, skipEncryption=true`), `Send_Seed_Old` `:226` (raw 4 bytes, no opcode), `Send_FirstLogin` 0x80 `:236`, `Send_SelectServer` 0xA0 `:269`, `Send_SecondLogin` 0x91 `:301`, `Send_CreateCharacter` 0x00/0xF8 `:334`, `Send_DeleteCharacter` 0x83 `:478`, `Send_SelectCharacter` 0x5D `:511`, `Send_ClientVersion` 0xBD `:915`, `Send_ClientType` 0xBF/0x0F `:2636`, `Send_Language` 0xBF/0x0B `:2603`.
- Movement/sync: `Send_WalkRequest` 0x02 `:3835`, `Send_Resync` 0x22 `:3804`, `Send_Ping` 0x73 `:121`, `Send_ClientViewRange` 0xC8 `:4379` (clamped to `Constants.MIN/MAX_VIEW_RANGE`), `Send_MultiBoatMoveRequest` 0xBF/0x33 `:3769`.
- Item manipulation: `Send_PickUpRequest` 0x07 `:548`, `Send_DropRequest(_Old)` 0x08 `:579/:621`, `Send_EquipRequest` 0x13 `:665`, `Send_EquipMacroKR` 0xEC `:4672`, `Send_UnequipMacroKR` 0xED `:4705`.
- Interaction: `Send_DoubleClick` 0x06 `:152`, `Send_ClickRequest` 0x09 `:854`, `Send_AttackRequest` 0x05 `:884`, `Send_RequestPopupMenu`/`Send_PopupMenuSelection` 0xBF/0x13,0x15 `:2679/:2711`.
- Targeting: `Send_TargetObject` `:1721`, `Send_TargetXYZ` `:1772`, `Send_TargetCancel` `:1831` (all 0x6C), `Send_TargetSelectedObject` 0xBF/0x2C `:3561`, `Send_TargetByResource` 0xBF/0x30 `:4738`.
- Speech/chat: `Send_ASCIISpeechRequest` 0x03 `:946`, `Send_UnicodeSpeechRequest` 0xAD `:988` (keyword-encoded form built from `SpeechesLoader.Instance.GetKeywords`), `Send_ChatJoinCommand`/`Create`/`Leave`/`Message` 0xB3 `:2744..2890`, `Send_OpenChat` 0xB5 `:2892`, party 0xBF/0x06 family `:2099..2307`.
- Gump/menu/prompt responses: `Send_GumpResponse` 0xB1 `:1294`, `Send_VirtueGumpResponse` `:1356`, `Send_MenuResponse`/`Send_GrayMenuResponse` 0x7D `:1389/:1437`, `Send_TextEntryDialogResponse` 0xAC `:1582`, `Send_ASCIIPromptResponse` 0x9A `:1867`, `Send_UnicodePromptResponse` 0xC2 `:1900`.
- Vendors/trade: `Send_BuyRequest` 0x3B `:3437`, `Send_SellRequest` 0x9F `:3486`, `Send_TradeResponse`/`Send_TradeUpdateGold` 0x6F `:1470/:1517`.
- Books/bulletin: `Send_BookHeaderChanged(_Old)` 0xD4/0x93 `:3291/:3254`, `Send_BookPageData(Request)` 0x66 `:3332/:3402`, bulletin board 0x71 `:2342..2507`.
- Tooltips: `Send_MegaClilocRequest` 0xD6 `:3150` (batches ≤15 serials, **removes them from the caller's list**), `Send_MegaClilocRequest_Old` 0xBF/0x10 `:3118`.
- Custom housing: `Send_CustomHouseDataRequest` 0xBF/0x1E `:3631`; 0xD7 family `Send_CustomHouseBackup/Restore/Commit/BuildingExit/GoToFloor/Sync/Clear/Revert/Response/AddItem/DeleteItem/AddRoof/DeleteRoof/AddStair` `:3873..4377`.
- Misc/legacy: `Send_ACKTalk` 0x03 `:54` (hardcoded 36-byte blob), `Send_RazorACK` 0xF0 `:2509`, `Send_QueryGuildPosition`/`Send_QueryPartyPosition` 0xF0 `:2540/:2572`, `Send_DeathScreen` 0x2C `:4479`, `Send_OpenUOStore` 0xFA `:4419`, `Send_ShowPublicHouseContent` 0xFB `:4448`, `Send_UOLive_HashResponse` 0x3F `:4511`, `Send_ToggleGargoyleFlying` 0xBF/0x32 `:3598`, `Send_StunRequest`/`Send_DisarmRequest` 0xBF/0x09,0x0A `:3663/:3694`, `Send_UseCombatAbility` 0xD7/0x19 `:3525`, `Send_InvokeVirtueRequest` 0x12/0xF4 `:3072`.
- Not actually network sends: `Send_ToPlugins_AllSpells` `:4551` and `Send_ToPlugins_AllSkills` `:4628` build a 0xBF/0xBEEF packet and inject it **inbound** via `Plugin.ProcessRecvPacket(writer.AllocatedBuffer, ref len)`.

## State

- `PacketsTable._packetsTable` — `PacketsTable.cs:39`. `static readonly short[255]`, contents mutated in place by `AdjustPacketSizeByVersion` `:303-427`. Global, no lock. Every `Send_*` reads it.
- `EnhancedOutgoingPackets.EnabledPackets` — `EnhancedOutgoingPackets.cs:9`. `static HashSet<EnhancedPacketType>`, written from the packet-read path (`EnhancedPacketHandler.EnableEnhancedPacket` `:28,:40`), read from the send path `:14`. Never cleared on disconnect.
- `EnhancedPacketHandler._handlers` — `EnhancedPacketHandler.cs:48`, populated once in the static ctor `:20` (only if `Settings.GlobalSettings.EnhancedPacketsEnabled` at type-init time).
- `EnhancedPacketHandler.Handler` — static singleton `:50`.
- No other mutable state in the partition; each `Send_*` owns a stack-local `StackDataWriter` for the duration of the call.
- Global state *read* while writing: `Client.Version`/`Client.Protocol` (`:351,:373,:1092,:2656`), `World.Player.Serial` (`:2988,:3021,:3054,:3540,:3888,:3922,:3956,:3990,:4023,:4059,:4093,:4127,:4160,:4193,:4232,:4273,:4314,:4355`), `MessageManager.PromptData.Data` (`:1882,:1915`), `Settings.GlobalSettings.Language` (`:2759,:2801,:2841,:2873`), `SpeechesLoader.Instance` (`:961,:1011`), `SkillsLoader.Instance.SortedSkills` (`:4646`), `Spells*.GetAllSpells` (`:4605-4611`), `CUOEnviroment.Debug` (`:1768,:1818,:3594`).
- Global state *written* by a send: `World.Player.HasGump = false` in `Send_GumpResponse` `:1352-1353`.

## Timing

- **On startup, once:** `PacketsTable.AdjustPacketSizeByVersion(Version)` from `Client.cs:192`, after UO file load and before encryption calc. The table is treated as immutable after this.
- **Per frame (GameScene.Update):** `PacketHandlers.SendMegaClilocRequests()` — `GameScene.cs:883` — drains `_clilocRequests` 15 serials per call via `Send_MegaClilocRequest` (`PacketHandlers.cs:367`), or one 0xBF/0x10 per serial pre-CV_5090 (`:374`).
- **Every 1000 ms:** ping. `GameScene.cs:899-903` (`_timePing = Time.Ticks + 1000`) → `NetStatistics.SendPing()` → `Send_Ping(_pingIdx)` (`NetStatistics.cs:132`), `_pingIdx` cycles mod `_pings.Length`. LoginScene has its own ping at `LoginScene.cs:233`.
- **Resync watchdog:** if `ForceResyncOnHang` and no ping reply for >5000 ms and >5000 ms since last resync → `Send_Resync()` (`GameScene.cs:905-911`). Also from `WalkerManager.cs:177` and `MacroManager.cs:2161`.
- **Per movement step:** `Send_WalkRequest` from `PlayerMobile.cs:1837` and `:2015`, carrying `Walker.WalkSequence` and `Walker.FastWalkStack.GetValue()` — the seq byte and fastwalk key are owned by the walker, not by this partition.
- **On input / on demand:** everything else — clicks, targeting, gump responses, macros, vendor buy/sell, LegionScripting/Python API calls.
- **Per packet (inbound-driven send):** `EnhancedPacketHandler.EnableEnhancedPacket` replies immediately with `SendEnhancedPacket()` `:45` while still inside the 0xCE read.

## Inbound

- 48 files across the client call `Send_*` (211 call sites). Principal callers: `Game/GameActions.cs`, `Game/Managers/*` (Macro, Walker, Target, Party, Chat, UO store), `Game/UI/Gumps/*`, `Game/Scenes/GameScene.cs` and `LoginScene.cs`, `Game/GameObjects/PlayerMobile.cs`, `LegionScripting/*` (script + Python API), `Network/PacketHandlers.cs` (reply-to-packet paths).
- Entry point is always the extension-method form `NetClient.Socket.Send_X(...)` / `AsyncNetClient.Socket.Send_X(...)`.
- `PacketsTable.GetPacketLength` is also consumed by the receive path (packet framing) outside this partition.
- `EnhancedPacketHandler.Handle` is registered as the 0xCE inbound handler at `PacketHandlers.cs:347`.

## Outbound

- `AsyncNetClient.Send(Span<byte> message, bool ignorePlugin = false, bool skipEncryption = false)` — `AsyncNetClient.cs:372`. Only `Send_Seed` `:221` and `Send_Seed_Old` `:231` pass `true, true`.
- `ClassicUO.IO.StackDataWriter` — buffer, `WriteUInt8/16BE/32BE/64BE`, `WriteASCII`, `WriteUnicodeBE/LE`, `WriteUTF8`, `WriteZero`, `Seek`, `BufferWritten`, `AllocatedBuffer`, `BytesWritten`, `Dispose` (returns pooled buffer).
- `Plugin.ProcessRecvPacket` — `:4624,:4668` (plugin-facing fake inbound packets).
- `ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordVirtue(...)` — `:3079,:3082,:3085`.
- `GameActions.Print` — debug target echo `:1769,:1822,:1826,:3595`.
- Asset loaders: `SpeechesLoader`, `SkillsLoader`; data tables `SpellsMagery/Necromancy/Bushido/Ninjitsu/Chivalry/Spellweaving/Mastery`.
- `ArrayPool<byte>.Shared` rent/return inside `Send_BookPageData` `:3360,:3377`.
- `System.Text.Encoding.UTF8` for bulletin-board and book payloads.

## Hazards

- `OutgoingPackets.cs:4738-4749` — `Send_TargetByResource` builds a `StackDataWriter(11)`, writes the packet, then **never calls `socket.Send` and never `Dispose()`**. The packet is not transmitted and the rented buffer is not returned.
- `OutgoingPackets.cs:3172` — `Send_MegaClilocRequest` calls `serials.RemoveRange(0, count)` on the caller's `List<uint>` (`PacketHandlers.Handler._clilocRequests`, passed `ref`). Mutation of a shared collection inside a "send" method; caller re-checks `Count` at `PacketHandlers.cs:361/365`.
- `OutgoingPackets.cs:1352-1353` — `Send_GumpResponse` clears `World.Player.HasGump` locally; the server has not yet acknowledged the gump close.
- `OutgoingPackets.cs:4624` / `:4668` — `Send_ToPlugins_*` pass `writer.AllocatedBuffer` (the full pooled array, capacity ≥ payload) with `len = writer.BytesWritten` into `Plugin.ProcessRecvPacket`; buffer bytes past `len` are stale pool contents. Named `Send_*` but never touch the socket.
- `OutgoingPackets.cs:2988, 3021, 3054, 3540, 3888, 3922, 3956, 3990, 4023, 4059, 4093, 4127, 4160, 4193, 4232, 4273, 4314, 4355` — `World.Player.Serial` dereferenced with no null check; a send issued after logout/disconnect NREs.
- `OutgoingPackets.cs:1882, 1915` — prompt responses read `MessageManager.PromptData.Data` at send time rather than capturing it when the prompt was raised; a newer server prompt overwrites it in between.
- `OutgoingPackets.cs:113, 144, ... (every fixed-length branch)` — `writer.WriteZero(length - writer.BytesWritten)` with no guard for a negative difference; any fixed-length opcode whose payload exceeds its table entry passes a negative count.
- `OutgoingPackets.cs:1609-1610` — `Send_TextEntryDialogResponse` writes `(ushort)(text.Length + 1)` as the char count and `WriteASCII(text, text.Length + 1)`; length is chars, not encoded bytes.
- `OutgoingPackets.cs:2430` — bulletin-board subject length is written as `(byte)(subject.Length + 1)` (char count) while the body written at `:2432` is UTF-8 bytes; multi-byte subjects produce a length/body mismatch. Line counts at `:2440` and per-line lengths at `:2445` are cast to `byte` with no clamp.
- `OutgoingPackets.cs:3350` — `Send_BookPageData` writes `(ushort)text.Length` lines then emits `text.Length` NUL-terminated strings plus one extra `0x00` at `:3385`.
- `OutgoingPackets.cs:4687, 4720` — `Send_EquipMacroKR`/`Send_UnequipMacroKR` cast `serials.Length`/`layers.Length` to `byte` unclamped; >255 entries silently truncate the declared count while all entries are still written.
- `OutgoingPackets.cs:4722` — `Send_UnequipMacroKR` declares a byte count but writes each layer as `WriteUInt16BE((byte)layer)` (2 bytes per entry).
- `OutgoingPackets.cs:402` — `Send_CreateCharacter` takes `Take(skillcount)` of `character.Skills` ordered by value; if the mobile has fewer skills than `skillcount` the loop writes fewer pairs than the fixed layout expects (the trailing `WriteZero` then shifts subsequent fields for the variable-length case).
- `OutgoingPackets.cs:1332-1336` — `Send_GumpResponse` clamps entry text to 239 chars but writes `switches.Length`/`entries.Length` as full uint counts with no cap.
- `PacketsTable.cs:39` — array is declared `new short[255]` but indices run 0x00..0xFE; the final comment says `// ff` while the slot is 0xFE. `GetPacketLength` `:300` returns `-1` for `id >= 0xFF` and does not bounds-check negative ids.
- `PacketsTable.cs:303-427` — `AdjustPacketSizeByVersion` mutates the shared static table at runtime; entries `0xEE`/`0xEF` are set twice (`:359-360` then `:381-382`) and `0xF1` set in three separate blocks (`:361/:367`, `:396/:405`), so the effective value depends on block order, not on the highest matching version alone.
- `EnhancedOutgoingPackets.cs:20` — `SendEnhancedPacket` sends `writer.BufferWritten` but never calls `writer.Dispose()`; the pooled buffer is not returned (contrast every method in `OutgoingPackets.cs`).
- `EnhancedOutgoingPackets.cs:9` vs `EnhancedPacketHandler.cs:28,40` — `EnabledPackets` is a plain `HashSet` mutated on the packet-handling path and read on the send path with no synchronisation, and is never reset on disconnect/reconnect or character change.
- `EnhancedPacketHandler.cs:15-21` — the handler table is populated in a **static constructor** gated on `Settings.GlobalSettings.EnhancedPacketsEnabled`. Toggling that setting after type-init leaves the table empty forever (the runtime check at `:63` can only disable, never enable).
- `EnhancedPacketHandler.cs:32-42` — `EnableEnhancedPacket` reads `count` then loops reading ids with no bound against remaining buffer length.
- `EnhancedPacketHandler.cs:66-68` — `_handlers.ContainsKey` followed by `_handlers[packetID]` (double lookup); dispatch happens inline on the read thread and the handler sends a reply synchronously `:45`.
- `NetStatistics.cs:106` — `PingReceived` indexes `_pings[idx % len]` with the server-echoed index while `SendPing` (`:121,:133`) advances `_pingIdx` independently; a stale or spoofed echo overwrites a live slot.

## Fork deltas

- `EnhancedOutgoingPackets.cs`, `EnhancedPacketHandler.cs`, `EnhancedPacketTypeEnum.cs` — entire 0xCE sub-protocol is a TazUO addition (opcode 0xCE is `-1`/unused in the stock table, `PacketsTable.cs:247`). Gated by `Settings.GlobalSettings.EnhancedPacketsEnabled`.
- `OutgoingPackets.cs:3076-3087` — `Send_InvokeVirtueRequest` calls `LegionScripting.ScriptRecorder.Instance.RecordVirtue("honor"/"sacrifice"/"valor")` before building the packet. Stock ClassicUO has no ScriptRecorder.
- `OutgoingPackets.cs:1768-1769, 1818-1828, 3594-3595` — `CUOEnviroment.Debug` target-echo `GameActions.Print` calls in `Send_TargetObject`, `Send_TargetXYZ`, `Send_TargetSelectedObject`.
- `OutgoingPackets.cs:1352-1353` — `World.Player.HasGump = false` after a gump response (TazUO gump-tracking for scripting).
- `OutgoingPackets.cs:4479-4508` — `Send_DeathScreen` (0x2C ghost).
- `OutgoingPackets.cs:4738-4749` — `Send_TargetByResource` (0xBF/0x30), unfinished (see hazards).
- Socket type is `AsyncNetClient` throughout rather than stock `NetClient`; `NetStatistics.cs:110-135` carries both `_socket` and `_asocket` with the old sync ping commented out at `:120`.
- `OutgoingPackets.cs:2436-2457` — multi-line UTF-8 bulletin-board post body (stock sends a single line).
- `OutgoingPackets.cs:4672-4736` — `Send_EquipMacroKR`/`Send_UnequipMacroKR` (0xEC/0xED) using `ReadOnlySpan`.
- `GameScene.cs:905-911` — `ForceResyncOnHang` profile option driving `Send_Resync` off ping staleness.
