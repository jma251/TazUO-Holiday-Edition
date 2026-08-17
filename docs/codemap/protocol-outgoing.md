# Client -> Server Packet Reference (TazUO Holiday Edition, `legacy`)

Sources:
- `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/OutgoingPackets.cs` (4751 lines, `internal static class NetClientExt`)
- `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/EnhancedOutgoingPackets.cs`

## The universal envelope

Every `Send_*` in `OutgoingPackets.cs` (except `Send_Seed_Old` and the enhanced-packet path) follows the same skeleton:

```csharp
int length = PacketsTable.GetPacketLength(ID);      // PacketsTable.cs:298 — returns -1 for variable-length
var writer = new StackDataWriter(length < 0 ? 64 : length);
writer.WriteUInt8(ID);
if (length < 0) writer.WriteZero(2);                // 2-byte length placeholder
... body ...
if (length < 0) { writer.Seek(1, Begin); writer.WriteUInt16BE((ushort)writer.BytesWritten); }
else            { writer.WriteZero(length - writer.BytesWritten); }   // zero-pad to fixed size
socket.Send(writer.BufferWritten);
writer.Dispose();
```

So the byte lists below are *body only*; prepend `[u8 id]` and, for variable-length packets, `[u16be total length]`.
Fixed-length packets are **zero-padded to the table size** — a body shorter than the table entry emits trailing zeros;
a body longer than it makes `length - BytesWritten` negative (`WriteZero` with a negative count — no guard exists).

`AsyncNetClient.Send(Span<byte>, bool ignorePlugin = false, bool skipEncryption = false)`
(`src/ClassicUO.Client/Network/AsyncNetClient.cs:372`) drops the write if `!IsConnected` and routes the buffer through
`Plugin.ProcessSendPacket` unless `ignorePlugin` is set. Only `Send_Seed` / `Send_Seed_Old` pass `true, true`.

All integers are **big-endian** (`WriteUInt16BE` / `WriteUInt32BE` / `WriteUInt64BE`), except
`Send_UnicodePromptResponse`, which writes its text little-endian (`WriteUnicodeLE`).

---

## Table

`Len` column: value from `PacketsTable` (`F=n` fixed n bytes incl. header; `V` = variable, length patched in).

| # | Method | ID | Len | Body bytes, in order | Triggered by (file:line) | Clamping / validation |
|---|---|---|---|---|---|---|
| 1 | `Send_ACKTalk` | `0x03` | V | 36 hard-coded bytes: `20 00 34 00 03 db 13 14 3f 45 2c 58 0f 5d 44 2e 50 11 df 75 5c e0 3e 71 4f 31 34 05 4e 18 1e 72 0f 59 ad f5 00` | `Network/PacketHandlers.cs:1076` (login-complete handshake) | none — canned blob |
| 2 | `Send_Ping` | `0x73` | F=2 | `u8 idx` | `Network/NetStatistics.cs:132` (ping timer; the old sync path at `:120` is commented out) | none |
| 3 | `Send_DoubleClick` | `0x06` | F=5 | `u32 serial` | `Game/GameActions.cs:641`, `:644` (`GameActions.DoubleClick`) | none in packet; caller only routes grid-container gumps first |
| 4 | `Send_Seed` | `0xEF` | F=21 | `u32 v`, `u32 major`, `u32 minor`, `u32 build`, `u32 extra` (each version byte widened to u32) | `Game/Scenes/LoginScene.cs:586` (client >= 6.0.5.0 path) | sent with `ignorePlugin=true, skipEncryption=true` |
| 5 | `Send_Seed_Old` | *(no id)* | 4 | raw `u32 v` only — **no packet id, no length header** | `Game/Scenes/LoginScene.cs:590` (pre-6.0.5.0) | `ignorePlugin=true, skipEncryption=true` |
| 6 | `Send_FirstLogin` | `0x80` | F=62 | `ascii user[30]`, `ascii psw[30]`, `u8 0xFF` | `Game/Scenes/LoginScene.cs:593` | fixed-width ASCII truncates/pads user+password to 30 |
| 7 | `Send_SelectServer` | `0xA0` | F=3 | `u8 0x00`, `u8 index` | `Game/Scenes/LoginScene.cs:455` | index is a `byte` — >255 impossible by type |
| 8 | `Send_SecondLogin` | `0x91` | F=65 | `u32 seed`, `ascii user[30]`, `ascii psw[30]` | `Game/Scenes/LoginScene.cs:752` | 30-char truncation |
| 9 | `Send_CreateCharacter` | `0x00` / `0xF8` | F=104 / F=106 | `u32 0xEDEDEDED`, `u32 0xFFFFFFFF`, `u8 0x00`, `ascii name[30]`, `zero[2]`, `u32 Client.Protocol`, `u32 0x01`, `u32 0x00`, `u8 profession`, `zero[15]`, `u8 raceGenderVal`, `u8 str`, `u8 dex`, `u8 int`, then 3 (or 4) x (`u8 skillIndex`, `u8 skillValueFixed`), `u16 hue`, `u16 hairGraphic`, `u16 hairHue`, `u16 beardGraphic`, `u16 beardHue`, `u16 cityIndex`, `zero[2]`, `u16 slot`, `u32 clientIP`, `u16 shirtHue`, `u16 pantsHue` | `Game/Scenes/LoginScene.cs:495` | id switches to `0xF8` and skill count 3→4 at `CV_70160`; `raceGenderVal` = female-bit only below `CV_4011D`, else `(race [-1 below CV_7000]) * 2 + femaleBit`; skills sorted `OrderByDescending(Value).Take(skillcount)`; missing hair/beard/shirt/pants layers emit zeros |
| 10 | `Send_DeleteCharacter` | `0x83` | F=39 | `zero[30]`, `u32 index`, `u32 ipclient` | `Game/Scenes/LoginScene.cs:509` | `index` is a `byte` param widened to u32 |
| 11 | `Send_SelectCharacter` | `0x5D` | F=73 | `u32 0xEDEDEDED`, `ascii name[30]`, `zero[2]`, `u32 Client.Protocol`, `zero[24]`, `u32 index`, `u32 ipclient` | `Game/Scenes/LoginScene.cs:466` | name truncated/padded to 30 |
| 12 | `Send_PickUpRequest` | `0x07` | F=7 | `u32 serial`, `u16 count` | `Game/GameActions.cs:868` (`GameActions.PickUp`), `Game/Managers/MoveItemQueue.cs:85` | caller: `amount <= 0` → `item.Amount`, then cast to `ushort` (silent wrap above 65535) |
| 13 | `Send_DropRequest_Old` | `0x08` | F=14 | `u32 serial`, `u16 x`, `u16 y`, `i8 z`, `u32 container` | `Game/GameActions.cs:903` (< `CV_6017`) | caller casts x/y to ushort, z to sbyte — no range check |
| 14 | `Send_DropRequest` | `0x08` | F=14 | `u32 serial`, `u16 x`, `u16 y`, `i8 z`, `u8 slot`, `u32 container` | `Game/GameActions.cs:894` (>= `CV_6017`) | same casts; `slot` always passed 0 by `DropItem` |
| 15 | `Send_EquipRequest` | `0x13` | F=10 | `u32 serial`, `u8 layer`, `u32 container` | `Game/GameActions.cs:927` (`Equip`), `Game/Managers/MoveItemQueue.cs:93` | `GameActions.Equip` substitutes `World.Player.Serial` when `container` is not a valid serial, and requires `ItemHold.ItemData.IsWearable` |
| 16 | `Send_ChangeWarMode` | `0x72` | F=5 | `bool state`, `u8 0x32`, `u8 0x00` | `Game/GameActions.cs:78`, `Game/Scenes/GameSceneInputHandler.cs:1433` (true), `:1765` (false) | none |
| 17 | `Send_HelpRequest` | `0x9B` | F=258 | `zero[257]` | `Game/GameActions.cs:952` | none |
| 18 | `Send_StatusRequest` | `0x34` | F=10 | `u32 0xEDEDEDED`, `u8 0x04`, `u32 serial` | `Game/GameActions.cs:997` | none |
| 19 | `Send_SkillsRequest` | `0x34` | F=10 | `u32 0xEDEDEDED`, `u8 0x05`, `u32 serial` | `Game/GameActions.cs:354`, `:364`; `Network/PacketHandlers.cs:1025`, `:2457` | callers gate on gump state and set `World.SkillsRequested` |
| 20 | `Send_SkillsStatusRequest` | `0x3A` | V | `u16 skillIndex`, `u8 lockState` | **no callers** — duplicate of #43 | — |
| 21 | `Send_ClickRequest` | `0x09` | F=5 | `u32 serial` | `Game/GameActions.cs:660` (`SingleClick`), `:1162`, `:1170` | none |
| 22 | `Send_AttackRequest` | `0x05` | F=5 | `u32 serial` | `Game/GameActions.cs:610`; `:595` from the "may flag you criminal" confirm gump | `GameActions.Attack` shows `QuestionGump` and defers the send when `EnabledCriminalActionQuery` and both parties innocent/ally |
| 23 | `Send_ClientVersion` | `0xBD` | V | `ascii version` (NUL-terminated, unbounded) | `Network/PacketHandlers.cs:1022`, `:4409` (server 0xBD request) | none |
| 24 | `Send_ASCIISpeechRequest` | `0x03` | V | `u8 type`, `u16 hue`, `u16 font`, `ascii text` | `Game/GameActions.cs:716` (`Say`, client < `CV_200`), `Game/UI/Gumps/PaperdollGump.cs:484` (literal `"party"`) | `SpeechesLoader.GetKeywords(text)` non-empty → `type \|= MessageType.Encoded`; caller substitutes `ProfileManager.CurrentProfile.SpeechHue` when `hue == 0xFFFF` |
| 25 | `Send_UnicodeSpeechRequest` | `0xAD` | V | `u8 type`, `u16 hue`, `u16 font`, `ascii lang[4]`, then either encoded-keyword form (`u8 len>>4`, packed 12-bit keyword ids, UTF-8 text, `u8 0x00`) or `unicodeBE text` | `Game/GameActions.cs:708` (`Say`, >= `CV_200`) | keyword encoding as above; same `0xFFFF` hue substitution; `lang` fixed 4 bytes |
| 26 | `Send_CastSpell` | `0xBF` / `0x12` | V | >= `CV_60142`: `u16 0x1C`, `u16 0x02`, `u16 idx`. Older: `u8 0x56`, `ascii idx.ToString()` | `Game/GameActions.cs:1039` | version-gated id and body |
| 27 | `Send_CastSpellFromBook` | `0x12` | V | `u8 0x27`, `ascii "{idx} {serial}"` | `Game/GameActions.cs:1029` | `GameActions.CastSpellFromBook` skips when `index < 0` |
| 28 | `Send_UseSkill` | `0x12` | V | `u8 0x24`, `ascii "{idx} 0"` | `Game/GameActions.cs:1111` | caller skips when `index < 0` |
| 29 | `Send_OpenDoor` | `0x12` | V | `u8 0x58`, `u8 0x00` | `Game/GameActions.cs:1177` | none |
| 30 | `Send_OpenSpellBook` | `0x12` | V | `u8 0x43`, `u8 type` | `Game/Managers/MacroManager.cs:627`, `:1022` | none |
| 31 | `Send_EmoteAction` | `0x12` | V | `u8 0xC7`, `ascii action` | `Game/GameActions.cs:1182` | none |
| 32 | `Send_GumpResponse` | `0xB1` | V | `u32 local`, `u32 server`, `u32 button`, `u32 switchCount`, `u32[] switches`, `u32 entryCount`, per entry (`u16 id`, `u16 len`, `unicodeBE text[len]`) | `Game/GameActions.cs:941` | **text clamped to `Math.Min(239, text.Length)` chars**; clears `World.Player.HasGump` after send |
| 33 | `Send_VirtueGumpResponse` | `0xB1` | V | `u32 serial`, `u32 0x000001CD`, `u32 code` | `Game/UI/Controls/GumpPic.cs:186` | none |
| 34 | `Send_MenuResponse` | `0x7D` | F=13 | `u32 serial`, `u16 graphic`, and when `code != 0`: `u16 code`, `u16 itemGraphic`, `u16 itemHue` | `Game/UI/Gumps/MenuGump.cs:148`, `:171` (cancel: code=0) | `code == 0` truncates the body; remainder zero-padded to 13 |
| 35 | `Send_GrayMenuResponse` | `0x7D` | F=13 | `u32 serial`, `u16 graphic`, `u16 code` | `Game/UI/Gumps/MenuGump.cs:332` (cancel, code=0), `:346` | none |
| 36 | `Send_TradeResponse` | `0x6F` | V | `code==1`: `u8 0x01`, `u32 serial`. `code==2`: `u8 0x02`, `u32 serial`, `u32 state?1:0` | `Game/GameActions.cs:1148` (code 2, accept toggle), `:1153` (code 1, cancel) | **any other `code` disposes the writer and returns without sending** |
| 37 | `Send_TradeUpdateGold` | `0x6F` | V | `u8 0x03`, `u32 serial`, `u32 gold`, `u32 platinum` | `Game/UI/Gumps/TradingGump.cs:558` | none |
| 38 | `Send_LogoutNotification` | `0xD1` | F=2 | `u8 0x00` | `Game/GameActions.cs:1089` | none |
| 39 | `Send_TextEntryDialogResponse` | `0xAC` | V | `u32 serial`, `u8 parentID`, `u8 button`, `bool code`, `u16 (text.Length+1)`, `ascii text[text.Length+1]` | `Game/UI/Gumps/TextEntryDialogGump.cs:130`, `:141` | length is *char* count, not byte count — non-ASCII text desyncs the declared length |
| 40 | `Send_RenameRequest` | `0x75` | F=35 | `u32 serial`, `ascii name[30]` | `Game/GameActions.cs:1081` | name truncated/padded to 30 |
| 41 | `Send_NameRequest` | `0x98` | V | `u32 serial` | `Game/GameObjects/Entity.cs:182` | none |
| 42 | `Send_TipRequest` | `0xA7` | F=4 | `u16 id`, `u8 flag` | `Game/UI/Gumps/TipNoticeGump.cs:99` (flag 0), `:105` (flag 1) | none |
| 43 | `Send_TargetObject` | `0x6C` | F=19 | `u8 0x00`, `u32 cursorID`, `u8 cursorType`, `u32 entity`, `u16 x`, `u16 y`, `u16 (ushort)z`, `u16 graphic` | `Game/Managers/TargetManager.cs:403`, `:459` | prints a debug line when `CUOEnviroment.Debug`; `z` sign-extended through `ushort` cast |
| 44 | `Send_TargetXYZ` | `0x6C` | F=19 | `u8 0x01`, `u32 cursorID`, `u8 cursorType`, `u32 0x00`, `u16 x`, `u16 y`, `u16 (ushort)z`, `u16 graphic` | `Game/Managers/TargetManager.cs:678` | debug print distinguishes land (graphic 0) vs static |
| 45 | `Send_TargetCancel` | `0x6C` | F=19 | `u8 type` (`CursorTarget`), `u32 cursorID`, `u8 cursorType`, `u32 0x00`, `u32 0xFFFFFFFF`, `u32 0x00000000` | `Game/Managers/TargetManager.cs:312` | none |
| 46 | `Send_ASCIIPromptResponse` | `0x9A` | V | `u64 MessageManager.PromptData.Data`, `u32 (cancel?0:1)`, `ascii text` | `Game/UI/Gumps/SystemChatControl.cs:630` (cancel), `:721`; `LegionScripting/Commands.cs:864`, `:885`; `LegionScripting/API.cs:818` | callers pass `cancel = text.Length < 1`; version gate on `CV_200` chooses ASCII vs Unicode |
| 47 | `Send_UnicodePromptResponse` | `0xC2` | V | `u64 PromptData.Data`, `u32 (cancel?0:1)`, `ascii lang[3]`, `u8 0x00`, **`unicodeLE text`** | `Game/UI/Gumps/SystemChatControl.cs:634`, `:725`; `LegionScripting/Commands.cs:868`, `:889`; `LegionScripting/API.cs:822` | LE (not BE) text; lang fixed 3 bytes |
| 48 | `Send_DyeDataResponse` | `0x95` | F=9 | `u32 serial`, `u16 0`, `u16 hue` | `Game/UI/Gumps/ColorPickerGump.cs:109` | none |
| 49 | `Send_ProfileRequest` | `0xB8` | V | `u8 0x00`, `u32 serial` | `Game/GameActions.cs:962` | none |
| 50 | `Send_ProfileUpdate` | `0xB8` | V | `u8 0x01`, `u32 serial`, `u16 0x01`, `u16 text.Length`, `unicodeBE text` | `Game/UI/Gumps/ProfileGump.cs:247` | length is char count; no max |
| 51 | `Send_ClickQuestArrow` | `0xBF` sub `0x07` | V | `u16 0x07`, `bool rightClick` | `Game/GameActions.cs:1256` | none |
| 52 | `Send_CloseStatusBarGump` | `0xBF` sub `0x0C` | V | `u16 0x0C`, `u32 serial` | `Game/GameActions.cs:1018` | none |
| 53 | `Send_PartyInviteRequest` | `0xBF` sub `0x06` | V | `u16 0x06`, `u8 0x01`, `u32 0` | `Game/GameActions.cs:806`, `Game/UI/Gumps/PartyGump.cs:347` | none |
| 54 | `Send_PartyRemoveRequest` | `0xBF` sub `0x06` | V | `u16 0x06`, `u8 0x02`, `u32 serial` | `Game/GameActions.cs:791` (serial 0), `:796`, `:801` (self = leave); `Game/UI/Gumps/PartyGump.cs:394` | none |
| 55 | `Send_PartyChangeLootTypeRequest` | `0xBF` sub `0x06` | V | `u16 0x06`, `u8 0x06`, `bool type` | `Game/GameActions.cs:811`, `Game/UI/Gumps/PartyGump.cs:311` | none |
| 56 | `Send_PartyAccept` | `0xBF` sub `0x06` | V | `u16 0x06`, `u8 0x08`, `u32 serial` | `Game/GameActions.cs:784` | none |
| 57 | `Send_PartyDecline` | `0xBF` sub `0x06` | V | `u16 0x06`, `u8 0x09`, `u32 serial` | `Game/UI/Gumps/PartyInviteGump.cs:107`, `Game/UI/Gumps/SystemChatControl.cs:861` | none |
| 58 | `Send_PartyMessage` | `0xBF` sub `0x06` | V | `u16 0x06`, then `SerialHelper.IsValid(serial)` ? (`u8 0x03`, `u32 serial`) : `u8 0x04`, then `unicodeBE text` | `Game/GameActions.cs:779` | valid-serial check selects private (0x03) vs party-wide (0x04) |
| 59 | `Send_GameWindowSize` | `0xBF` sub `0x05` | V | `u16 0x05`, `u32 w`, `u32 h` | `Game/UI/Gumps/WorldViewportGump.cs:100`, `Network/PacketHandlers.cs:1013` | none |
| 60 | `Send_BulletinBoardRequestMessage` | `0x71` | V | `u8 0x03`, `u32 serial`, `u32 msgSerial` | `Game/UI/Gumps/BulletinBoardGump.cs:576` | none |
| 61 | `Send_BulletinBoardRequestMessageSummary` | `0x71` | V | `u8 0x04`, `u32 serial`, `u32 msgSerial` | `Network/PacketHandlers.cs:6481` | none |
| 62 | `Send_BulletinBoardPostMessage` | `0x71` | V | `u8 0x05`, `u32 serial`, `u32 msgSerial`, `u8 (subject.Length+1)`, `utf8 subject`, `u8 0x00`, `u8 lineCount`, per line (`u8 (bytes+1)`, `utf8 line`, `u8 0x00`) | `Game/UI/Gumps/BulletinBoardGump.cs:450` | CRLF normalized to `\n` before splitting; **subject length byte uses char count while the payload is UTF-8 bytes**, and both the subject length and each line length are unchecked `byte` casts (>254 wraps) |
| 63 | `Send_BulletinBoardRemoveMessage` | `0x71` | V | `u8 0x06`, `u32 serial`, `u32 msgSerial` | `Game/UI/Gumps/BulletinBoardGump.cs:476` | none |
| 64 | `Send_RazorACK` | `0xF0` | V | `u8 0xFF` | `Network/PacketHandlers.cs:6068` | sent only in response to the server's Razor-detect query |
| 65 | `Send_QueryGuildPosition` | `0xF0` | V | `u8 0x01`, `bool true` | `Game/Managers/WorldMapEntityManager.cs:253` | throttled by the world-map entity refresh timer |
| 66 | `Send_QueryPartyPosition` | `0xF0` | V | `u8 0x00` | `Game/Managers/WorldMapEntityManager.cs:265` | same throttle |
| 67 | `Send_Language` | `0xBF` sub `0x0B` | V | `u16 0x0B`, `ascii lang[3]`, `u8 0x00` | `Network/PacketHandlers.cs:1019` | lang fixed 3 bytes |
| 68 | `Send_ClientType` | `0xBF` sub `0x0F` | V | `u16 0x0F`, `u8 0x0A`, `u32 clientFlag` | `Network/PacketHandlers.cs:2461` | `clientFlag` = `(1 << i)` OR-folded for `i < (uint)Client.Protocol` |
| 69 | `Send_RequestPopupMenu` | `0xBF` sub `0x13` | V | `u16 0x13`, `u32 serial` | `Game/GameActions.cs:1124`; `LegionScripting/API.cs:442`; `LegionScripting/Commands.cs:905` | none |
| 70 | `Send_PopupMenuSelection` | `0xBF` sub `0x15` | V | `u16 0x15`, `u32 serial`, `u16 menuid` | `Game/GameActions.cs:1133`; `LegionScripting/API.cs:443`; `LegionScripting/Commands.cs:906` | script paths fire request+selection back-to-back with no wait for the server's menu |
| 71 | `Send_ChatJoinCommand` | `0xB3` | V | `ascii Settings.GlobalSettings.Language[4]`, `u16 0x62`, `u16 0x22`, `unicodeBE name`, `u16 0x22`, `u16 0x20`, optional `unicodeBE password` | `Game/UI/Gumps/ChatGump.cs:252`; `Network/PacketHandlers.cs:4167` (auto-join "General") | password omitted when null/empty |
| 72 | `Send_ChatCreateChannelCommand` | `0xB3` | V | `ascii Language[4]`, `u16 0x63`, `unicodeBE name`, optional (`u16 0x7B`, `unicodeBE password`, `u16 0x7D`) | `Game/UI/Gumps/ChatGump.cs:446` | password brace-wrapped only when non-empty |
| 73 | `Send_ChatLeaveChannelCommand` | `0xB3` | V | `ascii Language[4]`, `u16 0x43` | `Game/UI/Gumps/ChatGump.cs:258` | none |
| 74 | `Send_ChatMessageCommand` | `0xB3` | V | `ascii Language[4]`, `u16 0x61`, `unicodeBE msg` | `Game/UI/Gumps/SystemChatControl.cs:971` | none |
| 75 | `Send_OpenChat` | `0xB5` | F=64 | `u8 0x00`, `unicodeBE name[len]` | `Game/UI/Gumps/ChatGumpChooseName.cs:186`; `Network/PacketHandlers.cs:2455` (empty name) | **`len = Math.Min(name.Length, 30)`**, skipped entirely when 0 |
| 76 | `Send_MapMessage` | `0x56` | F=11 | `u32 serial`, `u8 action`, `u8 pin`, `u16 x`, `u16 y` | `Game/UI/Gumps/MapGump.cs:257`, `:268`, `:327` | none |
| 77 | `Send_GuildMenuRequest` | `0xD7` sub `0x28` | V | `u32 World.Player.Serial`, `u16 0x28`, `u8 0x0A` | `Game/GameActions.cs:1071` | dereferences `World.Player` — no null guard in the packet |
| 78 | `Send_QuestMenuRequest` | `0xD7` sub `0x32` | V | `u32 World.Player.Serial`, `u16 0x32`, `u8 0x00` | `Game/GameActions.cs:957` | same |
| 79 | `Send_EquipLastWeapon` | `0xD7` sub `0x1E` | V | `u32 World.Player.Serial`, `u16 0x1E`, `u8 0x0A` | `Game/Managers/MacroManager.cs:1805` | same |
| 80 | `Send_InvokeVirtueRequest` | `0x12` | V | `u8 0xF4`, `ascii id.ToString()` | `Game/Managers/MacroManager.cs:1781`; `LegionScripting/API.cs:2378-2380`; `LegionScripting/Commands.cs:745`, `:748`, `:751` | **side effect before send**: `ScriptRecorder.Instance.RecordVirtue("honor"/"sacrifice"/"valor")` for ids 1/2/3 |
| 81 | `Send_MegaClilocRequest_Old` | `0xBF` sub `0x10` | V | `u16 0x10`, `u32 serial` | `Network/PacketHandlers.cs:374`, `:4631` | used when client < `CV_5090` |
| 82 | `Send_MegaClilocRequest` | `0xD6` | V | up to 15 x `u32 serial` | `Network/PacketHandlers.cs:367` (drains `Handler._clilocRequests`) | **`count = Math.Min(15, serials.Count)`; consumed serials are `RemoveRange`d from the caller's list** (`ref List<uint>`) |
| 83 | `Send_StatLockStateRequest` | `0xBF` sub `0x1A` | V | `u16 0x1A`, `u8 stat`, `u8 state` | `Game/GameActions.cs:1076` | none |
| 84 | `Send_SkillStatusChangeRequest` | `0x3A` | V | `u16 skillIndex`, `u8 lockState` | `Game/GameActions.cs:967`; `Game/UI/Gumps/StandardSkillsGump.cs:912` | none |
| 85 | `Send_BookHeaderChanged_Old` | `0x93` | F=99 | `u32 serial`, `u8 0x00`, `u8 0x01`, `u16 0`, `utf8 title[60]`, `utf8 author[30]` | `Game/UI/Gumps/ModernBookGump.cs:343` (client < `CV_308Z`-era path) | fixed 60/30 byte fields |
| 86 | `Send_BookHeaderChanged` | `0xD4` | V | `u32 serial`, `u8 0x00`, `u8 0x00`, `u16 0`, `u16 titleByteLen`, `utf8 title`, `u16 authorByteLen`, `utf8 author` | `Game/UI/Gumps/ModernBookGump.cs:339` | lengths computed with `Encoding.UTF8.GetByteCount` (correct, unlike #39/#62) |
| 87 | `Send_BookPageData` | `0x66` | V | `u32 serial`, `u16 0x01`, `u16 page`, `u16 lineCount`, then per line UTF-8 bytes + `u8 0x00`, then a trailing `u8 0x00` | `Game/UI/Gumps/ModernBookGump.cs:355` | strips `\n` from each line; empty/null lines emit just the terminator; rents from `ArrayPool<byte>` sized `len*2` |
| 88 | `Send_BookPageDataRequest` | `0x66` | V | `u32 serial`, `u16 0x01`, `u16 page`, `u16 0xFFFF` | `Game/UI/Gumps/ModernBookGump.cs:319`, `:324`; `Network/PacketHandlers.cs:3493` | none |
| 89 | `Send_BuyRequest` | `0x3B` | V | `u32 serial`, then if items: `u8 0x02` + per item (`u8 0x1A`, `u32 serial`, `u16 amount`); else `u8 0x00` (clear/close) | `Game/Managers/BuySellAgent.cs:161`; `Game/UI/Gumps/ShopGump.cs:630`; `Game/UI/Gumps/ModernShopGump.cs:275`, `:328` | empty list means "cancel", not "buy nothing" |
| 90 | `Send_SellRequest` | `0x9F` | V | `u32 serial`, `u16 itemCount`, per item (`u32 serial`, `u16 amount`) | `Game/Managers/BuySellAgent.cs:273`; `Game/UI/Gumps/ShopGump.cs:634`; `Game/UI/Gumps/ModernShopGump.cs:280`, `:332` | no count cap — a >65535-entry list wraps the u16 |
| 91 | `Send_UseCombatAbility` | `0xD7` sub `0x19` | V | `u32 World.Player.Serial`, `u16 0x19`, `u32 0`, `u8 idx`, `u8 0x0A` | `Game/GameActions.cs:1203` | `GameActions.UseAbility` maps idx 0/1 to Stun/Disarm specials first (see #96/#97) |
| 92 | `Send_TargetSelectedObject` | `0xBF` sub `0x2C` | V | `u16 0x2C`, `u32 serial`, `u32 targetSerial` | `Game/GameActions.cs:410` (`BandageSelf`); `Game/Managers/MacroManager.cs:1649`, `:1653` | debug print when `CUOEnviroment.Debug`; bandage paths bail out when `FindBandage()` is null |
| 93 | `Send_ToggleGargoyleFlying` | `0xBF` sub `0x32` | V | `u16 0x32`, `u16 0x01`, `u32 0` | `Game/Managers/MacroManager.cs:1799`; `Game/UI/Gumps/RacialAbilitiesBookGump.cs:225`; `Game/UI/Gumps/RacialAbilityButton.cs:76`; `LegionScripting/API.cs:2081`; `LegionScripting/Commands.cs:99` | MacroManager gates on the player being a gargoyle |
| 94 | `Send_CustomHouseDataRequest` | `0xBF` sub `0x1E` | V | `u16 0x1E`, `u32 serial` | `Network/PacketHandlers.cs:385` (drains `_customHouseRequests`) | none |
| 95 | `Send_StunRequest` | `0xBF` sub `0x09` | V | `u16 0x09` | `Game/GameActions.cs:1197`; `Game/Managers/MacroManager.cs:2148`; `LegionScripting/API.cs:2104` | none |
| 96 | `Send_DisarmRequest` | `0xBF` sub `0x0A` | V | `u16 0x0A` | `Game/GameActions.cs:1199`; `Game/Managers/MacroManager.cs:2143`; `LegionScripting/API.cs:2106` | none |
| 97 | `Send_ChangeRaceRequest` | `0xBF` sub `0x2A` | V | `u16 0x2A`, `u16 skinHue`, `u16 hairStyle`, `u16 hairHue`, `u16 beardStyle`, `u16 beardHue` | `Game/UI/Gumps/RaceChangeGump.cs:440`, `:450` | none |
| 98 | `Send_MultiBoatMoveRequest` | `0xBF` sub `0x33` | V | `u16 0x33`, `u32 serial`, `u8 dir`, `u8 dir` (twice), `u8 speed` | `Game/Managers/BoatMovingManager.cs:73` (serial = `World.Player`) | direction written twice by design (movement + facing) |
| 99 | `Send_Resync` | `0x22` | F=3 | *(empty body — zero-padded to 3)* | `Game/Managers/MacroManager.cs:2161`; `Game/Managers/WalkerManager.cs:177` (walk-desync recovery); `Game/Scenes/GameScene.cs:908` | none |
| 100 | `Send_WalkRequest` | `0x02` | F=7 | `u8 direction` (`\|= Direction.Running` when `run`), `u8 seq`, `u32 fastWalk` | `Game/GameObjects/PlayerMobile.cs:1837`, `:2015` | seq/fastwalk come from `Walker.WalkSequence` and `Walker.FastWalkStack.GetValue()` |
| 101 | `Send_CustomHouseBackup` | `0xD7` sub `0x02` | V | `u32 Player.Serial`, `u16 0x02`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2029` | — |
| 102 | `Send_CustomHouseRestore` | `0xD7` sub `0x03` | V | `u32 Player.Serial`, `u16 0x03`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2034` | — |
| 103 | `Send_CustomHouseCommit` | `0xD7` sub `0x04` | V | `u32 Player.Serial`, `u16 0x04`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2049` | — |
| 104 | `Send_CustomHouseBuildingExit` | `0xD7` sub `0x0C` | V | `u32 Player.Serial`, `u16 0x0C`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2098` | — |
| 105 | `Send_CustomHouseGoToFloor` | `0xD7` sub `0x12` | V | `u32 Player.Serial`, `u16 0x12`, `u32 0`, `u8 floor`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:1945`, `:1959`, `:1973`, `:1987` (floors 1-4) | floor values hard-coded 1..4 at the call sites |
| 106 | `Send_CustomHouseSync` | `0xD7` sub `0x0E` | V | `u32 Player.Serial`, `u16 0x0E`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2039` | — |
| 107 | `Send_CustomHouseClear` | `0xD7` sub `0x10` | V | `u32 Player.Serial`, `u16 0x10`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2044` | — |
| 108 | `Send_CustomHouseRevert` | `0xD7` sub `0x1A` | V | `u32 Player.Serial`, `u16 0x1A`, `u8 0x0A` | `Game/UI/Gumps/HouseCustomizationGump.cs:2054` | — |
| 109 | `Send_CustomHouseResponse` | `0xD7` sub `0x0A` | V | `u32 Player.Serial`, `u16 0x0A`, `u8 0x0A` | **no callers** | — |
| 110 | `Send_CustomHouseAddItem` | `0xD7` sub `0x06` | V | `u32 Player.Serial`, `u16 0x06`, `u8 0x00`, `u32 graphic`, `u8 0x00`, `u32 x`, `u8 0x00`, `u32 y`, `u8 0x0A` | `Game/Managers/HouseCustomizationManager.cs:804` | x/y passed as offsets from the foundation item |
| 111 | `Send_CustomHouseDeleteItem` | `0xD7` sub `0x05` | V | `u32 Player.Serial`, `u16 0x05`, `u8 0x00`, `u32 graphic`, `u8 0x00`, `u32 x`, `u8 0x00`, `u32 y`, `u8 0x00`, `u32 z`, `u8 0x0A` | `Game/Managers/HouseCustomizationManager.cs:672` | same |
| 112 | `Send_CustomHouseAddRoof` | `0xD7` sub `0x13` | V | as #110 plus `u8 0x00`, `u32 z` before the `0x0A` | `Game/Managers/HouseCustomizationManager.cs:800` | same |
| 113 | `Send_CustomHouseDeleteRoof` | `0xD7` sub `0x14` | V | as #111 with subcommand `0x14` | `Game/Managers/HouseCustomizationManager.cs:668` | same |
| 114 | `Send_CustomHouseAddStair` | `0xD7` sub `0x0D` | V | `u32 Player.Serial`, `u16 0x0D`, `u8 0x00`, `u32 graphic`, `u8 0x00`, `u32 x`, `u8 0x00`, `u32 y`, `u8 0x0A` | `Game/Managers/HouseCustomizationManager.cs:726` | same |
| 115 | `Send_ClientViewRange` | `0xC8` | F=2 | `u8 range` | `Game/UI/Gumps/ModernOptionsGump.cs:2381`; `Network/PacketHandlers.cs:2473` | **clamped to `[Constants.MIN_VIEW_RANGE, Constants.MAX_VIEW_RANGE]` inside the packet** |
| 116 | `Send_OpenUOStore` | `0xFA` | V | *(empty body)* | `Game/UI/Gumps/TopBarGump.cs:445` | none |
| 117 | `Send_ShowPublicHouseContent` | `0xFB` | V | `bool show` | `Network/PacketHandlers.cs:1037` | value from profile setting |
| 118 | `Send_DeathScreen` | `0x2C` | F=2 | `u8 0x02` (Ghost) | **no live callers** — only commented out at `Network/PacketHandlers.cs:3182` | — |
| 119 | `Send_UOLive_HashResponse` | `0x3F` | V | `u32 block`, `zero[6]`, `u8 0xFF`, `u8 mapIndex`, `u16[] checksums` | `Game/UltimaLive.cs:198` (`CRC_LENGTH` checksums) | count comes from the caller's span slice, unchecked |
| 120 | `Send_EquipMacroKR` | `0xEC` | V | `u8 serials.Length`, `u32[] serials` | `Game/Managers/DressAgentManager.cs:302` | **length written as an unchecked `byte`** — >255 items wraps |
| 121 | `Send_UnequipMacroKR` | `0xED` | V | `u8 layers.Length`, then per layer **`u16` (a byte widened)** | `Game/Managers/DressAgentManager.cs:364` | same unchecked byte count; note the u16-per-layer asymmetry vs #120 |
| 122 | `Send_ToPlugins_AllSpells` | `0xBF` sub `0xBEEF/0x00` | V | `u16 0xBEEF`, `u8 0x00`, then 7 dictionaries (Magery, Necromancy, Bushido, Ninjitsu, Chivalry, Spellweaving, Mastery), each `u16 count` + per spell (`u16 id`, `u16 manaCost`, `u16 minSkill`, `u8 targetType`, `u16 nameLen`, `unicodeBE name`, `u16 powerWordsLen`, `unicodeBE powerWords`, `u16 regCount`, `u8[] regs`) | `Network/PacketHandlers.cs:1043` | **never hits the socket** — handed to `Plugin.ProcessRecvPacket` as a synthetic *incoming* packet |
| 123 | `Send_ToPlugins_AllSkills` | `0xBF` sub `0xBEEF/0x01` | V | `u16 0xBEEF`, `u8 0x01`, `u16 skillCount`, per skill (`u16 index`, `bool hasAction`, `u16 nameLen`, `unicodeBE name`) | `Network/PacketHandlers.cs:1042` | same — plugin-local, not sent |
| 124 | `Send_TargetByResource` | `0xBF` sub `0x30` | *(hand-rolled, 11-byte writer)* | `u8 0xBF`, `zero[2]`, `u16 0x30`, `u32 serial`, `u16 resourceType` | `LegionScripting/API.cs:1688` | **BUG: builds the buffer but never calls `socket.Send`, never patches the length placeholder, and never disposes the writer.** The API call is a silent no-op that leaks a rented buffer. |
| 125 | `SendEnhancedPacket` (`EnhancedOutgoingPackets.cs:10`) | `0xCE` sub `0x0001` | V | `u8 0xCE` (`EnhancedPacketHandler.EPID`), `u16 length`, `u16 EnhancedPacketType.EnableEnhancedPacket` (=1) | `Network/EnhancedPacketHandler.cs:45` | **returns without sending unless `EnabledPackets` contains `EnableEnhancedPacket`**; no `writer.Dispose()` on the success path |

---

## Notes

### Packets that exist but are never sent
- `Send_SkillsStatusRequest` (#20) — dead duplicate of `Send_SkillStatusChangeRequest`, identical bytes.
- `Send_CustomHouseResponse` (#109) — no call sites.
- `Send_DeathScreen` (#118) — only a commented-out call in `PacketHandlers.cs:3182`.
- `Send_ToPlugins_AllSpells` / `Send_ToPlugins_AllSkills` (#122/#123) — deliberately routed to `Plugin.ProcessRecvPacket`, never to the socket.
- `Send_TargetByResource` (#124) — a real bug: no `Send` call at all.

### Shared packet ids (dispatch by subcommand)
- **`0x12`** (generic action): `0x24` use-skill, `0x27` cast-from-book, `0x43` open-spellbook, `0x56` cast-spell (legacy), `0x58` open-door, `0xC7` emote, `0xF4` invoke-virtue.
- **`0xBF`** (generic command, u16 subcommand): `0x05` window size, `0x06` party (sub-subcommand byte 0x01/0x02/0x03/0x04/0x06/0x08/0x09), `0x07` quest arrow, `0x09` stun, `0x0A` disarm, `0x0B` language, `0x0C` close status bar, `0x0F` client type, `0x10` old megacliloc, `0x13` popup request, `0x15` popup selection, `0x1A` stat lock, `0x1C` cast spell (new), `0x1E` custom-house data request, `0x2A` change race, `0x2C` target selected object, `0x30` target by resource (dead), `0x32` gargoyle fly, `0x33` boat move, `0xBEEF` plugin-only.
- **`0xD7`** (generic AOS, u16 subcommand, trailing `0x0A`): `0x02` backup, `0x03` restore, `0x04` commit, `0x05` delete item, `0x06` add item, `0x0A` response, `0x0C` building exit, `0x0D` add stair, `0x0E` sync, `0x10` clear, `0x12` go to floor, `0x13` add roof, `0x14` delete roof, `0x19` combat ability, `0x1A` revert, `0x1E` equip last weapon, `0x28` guild menu, `0x32` quest menu.
- **`0x71`**: bulletin board, subcommand byte `0x03`/`0x04`/`0x05`/`0x06`.
- **`0x6F`**: trade, subcommand byte `0x01`/`0x02`/`0x03`.
- **`0x6C`**: targeting, first byte `0x00` object / `0x01` xyz / `CursorTarget` value on cancel.
- **`0xF0`**: `0x00` party position query, `0x01` guild position query, `0xFF` Razor ACK.
- **`0xB3`**: chat, `0x43` leave / `0x61` message / `0x62` join / `0x63` create.
- **`0x34`**: `0x04` status request, `0x05` skills request.
- **`0x66`**: `page/0xFFFF` = read request, otherwise page write.
- **`0xB8`**: `0x00` profile request, `0x01` profile update.

### Client-version gates
| Gate | Effect |
|---|---|
| `CV_70160` | `Send_CreateCharacter` uses id `0xF8` and 4 skills instead of `0x00` and 3 |
| `CV_7000` | below it, race value is decremented before the gender fold |
| `CV_4011D` | below it, only the female bit is sent (no race) |
| `CV_60142` | `Send_CastSpell` switches from `0x12` ASCII form to `0xBF` sub-`0x1C` |
| `CV_6017` | `GameActions.DropItem` picks `Send_DropRequest` (slot byte) over `Send_DropRequest_Old` |
| `CV_200` | `GameActions.Say` / prompt responses pick Unicode (`0xAD`/`0xC2`) over ASCII (`0x03`/`0x9A`) |
| `CV_5090` (in `PacketHandlers`) | batched `0xD6` megacliloc vs per-serial `0xBF` sub-`0x10` |
| `ModernBookGump` version check | `0xD4` vs `0x93` book header |

### Clamping / validation, gathered
- `Send_GumpResponse`: text entries `Math.Min(239, len)` chars.
- `Send_MegaClilocRequest`: `Math.Min(15, count)` serials, and it mutates the caller's list.
- `Send_ClientViewRange`: clamped to `MIN_VIEW_RANGE`..`MAX_VIEW_RANGE`.
- `Send_OpenChat`: name `Math.Min(30)` chars, skipped if empty.
- `Send_TradeResponse`: unknown `code` → no packet at all.
- `SendEnhancedPacket`: gated on the `EnabledPackets` set.
- Fixed-width ASCII fields silently truncate: user/psw 30, char name 30, rename 30, lang 3 or 4, book title 60 / author 30 (old form).
- **Unchecked casts worth flagging**: `(ushort)amount` in `PickUp`; `(byte)serials.Length` / `(byte)layers.Length` in the KR equip macros; `(byte)(subject.Length + 1)` and `(byte)(bytes.Length + 1)` in the bulletin-board post; `(ushort)items.Length` in `Send_SellRequest`; `(ushort)text.Length` in `Send_ProfileUpdate`.
- **Char-count vs byte-count mismatches** (declared length wrong for non-ASCII): `Send_TextEntryDialogResponse`, `Send_BulletinBoardPostMessage` (subject only), `Send_ProfileUpdate` (u16 chars against a UnicodeBE payload — consistent there, but the field means characters not bytes).
- **`World.Player` dereferenced without a null check** in `Send_GuildMenuRequest`, `Send_QuestMenuRequest`, `Send_EquipLastWeapon`, `Send_UseCombatAbility`, and every `0xD7` custom-house packet.

### Side effects inside packet builders
- `Send_GumpResponse` clears `World.Player.HasGump`.
- `Send_InvokeVirtueRequest` calls `ScriptRecorder.Instance.RecordVirtue(...)` before writing.
- `Send_MegaClilocRequest` removes the sent serials from the caller's `List<uint>`.
- `Send_TargetObject` / `Send_TargetXYZ` / `Send_TargetSelectedObject` emit `GameActions.Print` debug lines when `CUOEnviroment.Debug`.
