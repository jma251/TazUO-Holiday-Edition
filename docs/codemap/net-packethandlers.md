# net-packethandlers

Partition = exactly one file: the server->client packet dispatch table and every
handler body for the classic UO protocol.

## Files

| path | lines | purpose |
| --- | --- | --- |
| `/home/user/TazUO-Holiday-Edition/src/ClassicUO.Client/Network/PacketHandlers.cs` | 7704 | Frames bytes out of two circular buffers, dispatches by packet ID into ~120 static handlers, and applies every server-authoritative change to World/UI/audio/houses. Also contains the server-gump layout interpreter (`CreateGump`) and the custom-house zlib plane decoder. |

Read in full (7704/7704 lines).

## Types

| name | file:line | role |
| --- | --- | --- |
| `PacketHandlers` (sealed class) | PacketHandlers.cs:59 | The whole partition. Instance side owns the receive buffers; static side owns the handler bodies. |
| `PacketHandlers.OnPacketBufferReader` delegate | :61 | `void (ref StackDataReader p)` — handler signature. `ref` means handlers can advance/seek the reader; the reader is a stack struct over the shared `_readingBuffer`. |
| `PacketHandlers.AffixType` (private [Flags] enum) | :7696 | Append/Prepend/System bits for 0xCC cliloc affixes. |
| `PacketHandlers.Handler` (static singleton) | :82 | The one instance; `Add(byte,handler)` is public so other subsystems register IDs (UltimaLive uses 0x3F/0x40). |

Notable helper methods (not types, but the load-bearing shared code):
`ParsePackets` :91/:98, `Append` :160, `Reset` :168, `AnalyzePacket` :181,
`GetPacketInfo` :197, static ctor registration table :234-357,
`SendMegaClilocRequests` :359, `RecomputeDrawCeiling` :409,
`AddMegaClilocRequest` :419, `ReadUnsafeCustomHouseData` :5379,
`AddItemToContainer` :6389, `UpdateGameObject` :6545, `UpdatePlayer` :6816,
`ClearContainerAndRemoveItems` :6881, `CreateGump` :6917, `ReverseLookup` :7637,
`ApplyTrans` :7674.

## State

Owned by this file:

- `static uint _requestedGridLoot` :63 — serial of the corpse whose GridLootGump is pending; set in `OpenContainer` :1552, consumed and zeroed in `AddItemToContainer` :6515-6521. Single slot, no queue.
- `static readonly TextFileParser _parser` :65 — gump-layout tokenizer (`{ }` delimiters). Shared mutable-by-use across all gump parses.
- `static readonly TextFileParser _cmdparser` :71 — per-command tokenizer (`@ @`).
- `List<uint> _clilocRequests` :78 (instance) — pending mega-cliloc/OPL serials; drained once per GameScene.Update :359-379.
- `List<uint> _customHouseRequests` :79 (instance) — pending custom-house design requests; drained/cleared each flush :381-390. Deduped on insert at :4867.
- `readonly OnPacketBufferReader[] _handlers = new OnPacketBufferReader[0x100]` :80 — dispatch table, mutable at runtime via `Add`.
- `byte[] _readingBuffer = new byte[4096]` :86 — single scratch buffer for the current packet, grown by doubling :135-138. Every handler's `StackDataReader` points at this one array.
- `readonly PacketLogger _packetLogger` :87 (declared; the static `PacketLogger.Default` is what actually logs at :142).
- `readonly CircularBuffer _buffer` :88 — network stream bytes.
- `readonly CircularBuffer _pluginsBuffer` :89 — bytes injected by plugins (`Plugin.cs:757/779`).

State the handlers mutate elsewhere (client-side mirrors of server truth):
`World.Player.*` stats/locks/abilities, `World.Items`/`World.Mobiles` maps,
`World.OPL`, `World.HouseManager`, `World.CorpseManager`, `World.WMapManager`,
`World.Party`, `World.Light`, `World.Season`/`OldSeason`/`OldMusicIndex`
(:4396-4404, :2441), `World.ClientViewRange` (:2468, :2741),
`World.ClientLockedFeatures` (:4309), `World.ActiveSpellIcons` (:4970/:4975),
`World.MapIndex` (:4497, :4720-4722), `Client.Game.GameCursor.ItemHold`,
`TargetManager.LastAttack` (:3822), `ChatManager.*`, `SkillsLoader.Instance.Skills`
and `.SortedSkills` (cleared and rebuilt wholesale at :2114-2131).

## Timing

- **Per frame, bounded**: `GameController.ProcessNetworkPackets` (GameController.cs:138) dequeues at most `MAX_PACKETS_PER_FRAME = 25` socket messages per frame and calls `Handler.ParsePackets(message)` (GameController.cs:143). Each call can still emit many protocol packets, since one dequeued message is an arbitrary byte blob framed inside `ParsePackets`.
- **Per parse call**: `ParsePackets(Span)` :91 appends to `_buffer` then drains `_buffer` (plugins allowed) and `_pluginsBuffer` (plugins not allowed), each under `lock (stream)` :102, until the stream is short of a full packet.
- **Per packet**: framing via `PacketsTable.GetPacketLength`; `-1` means a 2-byte BE length at offset 1 and a payload offset of 3 :217-229. Logged twice (`PacketLogger.Default` :142, `HouseDiagnostics.LogPacket` :143), then handed to `Plugin.ProcessRecvPacket` :148 which may rewrite length or swallow the packet, then `AnalyzePacket` :181 seeks past the header and invokes the handler.
- **Per GameScene.Update (once a frame)**: `PacketHandlers.SendMegaClilocRequests()` (GameScene.cs:883) flushes `_clilocRequests` (batched `Send_MegaClilocRequest` on >= CV_5090, else one packet per serial) and flushes+clears `_customHouseRequests`.
- **On scene change / login**: `Handler.Reset()` clears both circular buffers (LoginScene.cs:391, :738).
- **Timer-ish values set from packets**: death-screen timer `Time.Ticks + Constants.DEATH_SCREEN_TIMER` :1916; corpse world-map entity `LastUpdate = Time.Ticks + 300000` (5 min) :1926; turn-to-target window `TIME_TURN_TO_LASTTARGET = 2000` ms in `Swing` :2062; drag effect duration 5000 with speed 5 :1398-1399; party heal window compares `World.Party.PartyHealTimer < Time.Ticks` :440.
- **Blocking on the frame thread**: `Logout` :5194 calls `NetClient.Socket.Disconnect().Wait()` then constructs a new `LoginScene`.
- **Static ctor** :234 builds the dispatch table once at type init; `UltimaLive.cs:81-82` adds two more IDs later at runtime.

## Inbound

- `GameController.ProcessNetworkPackets` -> `PacketHandlers.Handler.ParsePackets(byte[])` (GameController.cs:143), driven by `AsyncNetClient.Socket.TryDequeuePacket`, 25/frame cap.
- `Plugin.cs:755-757` and `Plugin.cs:777-779` -> `Handler.Append(span, fromPlugins: true)` under `lock (PacketHandlers.Handler)` — note that lock is on the *instance*, while `ParsePackets` locks the *CircularBuffer* objects.
- `GameScene.Update` -> `PacketHandlers.SendMegaClilocRequests()` (GameScene.cs:883).
- `LoginScene` -> `PacketHandlers.Handler.Reset()` (LoginScene.cs:391, :738).
- `ObjectPropertiesListManager.cs:85` -> `PacketHandlers.AddMegaClilocRequest(serial)`.
- `UltimaLive.cs:81-82` -> `PacketHandlers.Handler.Add(0x3F/0x40, ...)`.
- `EnhancedPacketHandler.Handle` is registered for `EnhancedPacketHandler.EPID` :347.

## Outbound

- **World/entity**: `World.Get/Items.Get/Mobiles.Get/GetOrCreateItem/GetOrCreateMobile/RemoveItem/RemoveMobile/RemoveItemFromContainer/SpawnEffect/CreatePlayer/ChangeSeason`, `World.HouseManager`, `World.CorpseManager`, `World.CustomHouseManager`, `World.WMapManager`, `World.OPL`, `World.Party.ParsePacket` :4490, `World.WorldTextManager.AddDamage`.
- **UI**: `UIManager.Add/GetGump<T>/GetTradingGump/RemovePosition/SavePosition/GetGumpCachePosition/ShowGamePopup`, and direct walks of `UIManager.Gumps` (:4455, :4961, :5065, :6944).
- **Net out**: `NetClient.Socket.Send_*` (ClientVersion, Language, GameWindowSize, SkillsRequest, ClientViewRange, ClientType, MegaClilocRequest(_Old), CustomHouseDataRequest, BookPageDataRequest, BulletinBoardRequestMessageSummary, ChatJoinCommand, OpenChat, RazorACK, ACKTalk, ShowPublicHouseContent, ToPlugins_AllSkills/AllSpells), plus a hardcoded 40-byte `Send(buffer)` handshake reply :3912-3956.
- **Scenes**: `Client.Game.GetScene<GameScene>()/<LoginScene>()`, `Client.Game.SetScene(new GameScene()/new LoginScene())`, `GameScene.UpdateMaxDrawZ(true)` :416, `scene.Weather.Generate/Reset`, `LoginScene.ServerListReceived/HandleRelayServerPacket/UpdateCharacterList/ReceiveCharacterList/HandleErrorCode` :6314-6387.
- **Assets/loaders**: `ClilocLoader`, `SkillsLoader`, `TileDataLoader`, `FontsLoader`, `MapLoader.ApplyPatches` :4694, `AnimationsLoader.GetDeathAction`, `Client.Game.Arts/Gumps/MultiMaps/Animations`, `ZLib.Decompress`.
- **Managers**: `MessageManager.HandleMessage`, `TargetManager`, `ChatManager`, `ContainerManager`, `BoatMovingManager`, `Pathfinder.WalkTo/CanWalk`, `SpellVisualRangeManager`, `TitleBarStatsManager`, `GlobalActionCooldown`, `DelayedObjectClickManager`, `UoAssist.Signal*`, `GameActions.*`, `EventSink.Invoke*`.
- **Fork-specific managers**: `HouseDiagnostics.*`, `MusicDiagnostics.*`, `BuySellAgent.Instance`, `NearbyLootGump`, `GridContainer`/`GridLootGump`, `ModernPaperdoll`, `ModernShopGump`, `ScriptRecorder`, `ScriptingInfoGump`, `HtmlCrashLogGen`.

## Hazards

- `PacketHandlers.cs:104` — `ref var packetBuffer = ref _readingBuffer;` plus the doubling resize at :135-138 means the single instance scratch buffer backs every `StackDataReader`; a handler that stores the span (or that reenters parsing) sees the next packet's bytes.
- `PacketHandlers.cs:102` vs `Plugin.cs:755` — `ParsePackets` locks the `CircularBuffer`; plugin injection locks the `PacketHandlers` instance. Different monitors guard `_pluginsBuffer` writes vs reads.
- `PacketHandlers.cs:6303` — `PacketList` (0xF7) calls `UpdateItemSA(ref p)` recursively on the same reader; `UpdateItemSA` reads `p[0]` at :6121 which is still `0xF7`, not `0xF3`, so the `else if (p[0] == 0xF7)` player branch fires for embedded sub-packets.
- `PacketHandlers.cs:148` — `Plugin.ProcessRecvPacket(packetBuffer, ref packetlength)` can shrink/grow `packetlength` after `packetlength` bytes were already dequeued; the framing offset computed at :109 is used unchanged in `AnalyzePacket`.
- `PacketHandlers.cs:125-132` — on a short stream the loop `break`s but the bytes stay in the CircularBuffer; a bad `GetPacketInfo` (:118) also breaks without consuming, so a malformed ID stalls that buffer permanently until `Reset()`.
- `PacketHandlers.cs:4025-4036` — `DisplayDeath` reassigns `owner.Serial = serial | 0x80000000` and re-keys `World.Mobiles`. Any holder of the old serial (gumps, `TargetManager.LastAttack`, party lists, world-map entities) now points at a key that no longer exists.
- `PacketHandlers.cs:1475` — `Item item = vendor.FindItemByLayer(layer); LinkedObject first = item.Items;` dereferences `item` without a null check (the `first == null` check at :1477 is one step too late).
- `PacketHandlers.cs:6891-6914` — `ClearContainerAndRemoveItems` walks `container.Items` while `World.RemoveItem(it, true)` unlinks nodes; it caches `next` first, then overwrites `container.Items` at :6914 with a node whose `Previous` link is not repaired.
- `PacketHandlers.cs:3100-3113` — `UpdateObject` walks `obj.Items` and calls `World.RemoveItem` inside the walk (next cached).
- `PacketHandlers.cs:5014-5024` — `foreach (Mobile m in World.Mobiles.Values)` matching only the low 16 bits of the serial (`(m.Serial & 0xFFFF) == serial`); first match wins, so animation can land on the wrong mobile.
- `PacketHandlers.cs:4455-4483` and `:5065-5071` and `:4961-4981` — enumerating `UIManager.Gumps` while calling `Dispose()` / `OnButtonClick` on members inside the loop.
- `PacketHandlers.cs:5194` — `NetClient.Socket.Disconnect().Wait()` blocks the frame thread inside a packet handler.
- `PacketHandlers.cs:1552` / `:6515-6521` — `_requestedGridLoot` is a single static slot; opening a second corpse before the first `AddItemToContainer` arrives overwrites it and the first corpse's grid gump is never created.
- `PacketHandlers.cs:6399-6405` — `AddItemToContainer` clears `ItemHold` only when `Dropped`; the identity match is by serial, and serials for spellbook pseudo-items are synthesized as small ints (`spellItem.Serial = cc`, values 1..64, :4830-4832), colliding with the real serial space.
- `PacketHandlers.cs:4830` — `Item.Create(cc)` with `cc` in 1..64 as serial, pushed into `spellbook`; these fake items live in the same identity space that `World.Items.Get` searches.
- `PacketHandlers.cs:1189` — `ProfileManager.CurrentProfile.GridLootType` dereferenced without the null guard used two lines earlier in the same handler; same unguarded pattern at :1464, :1546, :2138, :2924, :3315, :3647, :4000, :6116, :6507, :6738, :6809.
- `PacketHandlers.cs:2114-2131` — `UpdateSkills` type 0xFE clears `SkillsLoader.Instance.Skills`/`SortedSkills` globally and rebuilds; anything holding a `SkillEntry` index across that packet is stale.
- `PacketHandlers.cs:5333` — `stackalloc char[totalLength]` where `totalLength` is the sum of all translated cliloc string lengths from the packet — unbounded by the client.
- `PacketHandlers.cs:2843` / `:4546` — `stackalloc char[256]` then `ValueStringBuilder` appends of packet-controlled length (the builder grows to heap, but the initial span is fixed).
- `PacketHandlers.cs:5619-5633` — `ReadUnsafeCustomHouseData` decompresses `clen` source bytes into `dlen` bytes from a header-derived pair with no cross-check against remaining packet length; `p.Skip(clen)` follows.
- `PacketHandlers.cs:5697-5714` — `OpenCompressedGump` returns the rented `decData` in `finally` while `layout` was built from it inside the `fixed`; the second block at :5736-5800 rents again and creates a `StackDataReader` over the rented array, released before `Return`.
- `PacketHandlers.cs:5690` — `uint clen = p.ReadUInt32BE() - 4;` unsigned underflow if the server sends <4.
- `PacketHandlers.cs:6652-6664` — `WantUpdateMulti` is set from a graphic/position comparison; `CustomHouse` at :5652 explicitly forces `foundation.WantUpdateMulti = false` after parsing, i.e. two writers to the same flag in different frames.
- `PacketHandlers.cs:1114` — `DeleteObject` returns early for the player's own serial, and at :1209 returns before removing anything if `World.CorpseManager.Exists(0, serial)`, leaving the entity in `World.Items`/`World.Mobiles`.
- `PacketHandlers.cs:5055-5057` — `DisplayClilocString` branches on `p[0] == 0xCC` for both the affix flag and the affix string; `p[0]` is the raw packet ID byte read from the shared buffer, not a local.
- `PacketHandlers.cs:1777`, `:1815`, `:1826`, `:1833`, `:1900`, `:6403` — `Console.WriteLine` on the frame thread from item-drag handlers.
- `PacketHandlers.cs:2468-2473` — client sets `World.ClientViewRange` from local settings, then the server's 0xC8 overwrites it at :2741; the two can disagree for the frames in between.
- `PacketHandlers.cs:3212` — `OpenMenu` does `p.Seek(p.Position - 2)` to rewind after peeking `menuid`; the subsequent loop re-reads those bytes.
- `PacketHandlers.cs:4780` / `:4797` — `goto case 0` / `goto case 2` after `p.Seek(pos)` re-parses the same bytes under a different version branch.
- `PacketHandlers.cs:7556` — `Regex.Matches` with a runtime pattern executed inside gump creation, per SOS gump.
- `PacketHandlers.cs:1751-1756` — `DenyMoveItem` reads `ItemHold` state that `AddItemToContainer` (:6404) may already have cleared in the same frame; ordering between the deny packet and the container update decides the outcome.

## Fork deltas

Clearly not stock ClassicUO:

- **Holiday-Edition diagnostics** — `HouseDiagnostics.LogPacket` :143, `LogHouseRequest` :386, `LogHouseItemDeleteOrdered` :1128, `LogRangeProbe` :1131/:6758, `LogViewRange` :2743, `LogHouseResponse` :5561, `LogHouseItemArrived` :6802; `MusicDiagnostics.RawMusicPacket/ServerPacket/StopIgnored/SeasonPacket` :2411-2430, :4384.
- **`RecomputeDrawCeiling()` :409-417** with a long explanatory comment; called unconditionally from house revision :4879 and custom house :5663 (upstream gated this on the player being inside the house).
- **`PlayMusic` rewrite :2405-2445** — `Constants.MUSIC_STOP_INDEX` sentinel, `Settings.GlobalSettings.IgnoreServerStopMusic`, `Audio.StopMusicFromServer(keepPlaying)`, `Audio.NotifyServerTrack(index)`.
- **`Season` :4369-4405** — comment states the second byte is a play-sound bool, not a music index; upstream read it as a track and played CREATE1.
- **`CustomHouse` :5652** — `foundation.WantUpdateMulti = false` with a long comment about the invisible-house-interior failure.
- **`_customHouseRequests` dedupe :4867** with the comment about ten requests in a millisecond rebuilding a 3283-component house five times.
- **`LoginComplete` :2464-2479** — `World.ClientViewRange` from `Settings.GlobalSettings.ClientViewRange` clamped to `MIN/MAX_VIEW_RANGE`, plus `GlobalActionCooldown.BeginCooldown()`.
- **TazUO UI variants** wired into handlers: `GridContainer` (:1183, :1648, :6500), `GridLootGump` (:1191, :1550, :6509), `NearbyLootGump` (:1188, :1540, :6527), `ModernPaperdoll` (throughout), `ModernShopGump` (:1459-1519, :2922-2942, :3644-3708), `ModernBookGump`, `RaceChangeGump` :5006, `RenderedMapArea` :7521, `MenuButton`/`ContextMenuControl` SOS block :7543-7620, `WorldMapGump` marker helpers, `ReverseLookup` :7637.
- **`BuySellAgent.Instance?.HandleBuyPacket/HandleSellPacket/HandleSellPacketFinished`** :1531, :3679, :3710.
- **`EventSink.Invoke*`** hooks: OnEntityDamage :566, OnOpenContainer :1694, OnPlayerDeath :1931, OnSetWeather :2562, ClilocMessageReceived :5139, OnItemCreated/OnItemUpdated :6695-6697, OnCorpseCreated :6807.
- **Scripting**: `ScriptRecorder.Instance.RecordWaitForGump` :6926, `ScriptingInfoGump.AddOrUpdateInfo("Last Gump Opened", …)` :6927, `World.Player.HasGump`/`LastGumpID` :7624-7625.
- **`TitleBarStatsManager`** :621, :789, :1973, :3579, :3734, :3753, :3772 and `Profile.EnableTitleBarStats`.
- **`SpellVisualRangeManager.Instance.ClearCasting()` :3733 / `.OnClilocReceived` :5061.**
- **`EnhancedPacketHandler.EPID` registration :347** — fork custom packet channel.
- **`HtmlCrashLogGen.Generate(...)` :5807** in `OpenCompressedGump`'s catch, with a Discord-referencing message.
- **`ShowSkillsChangedDeltaValue` threshold** :2206-2216 and `Skill.InvokeSkillBaseChanged/ValueChanged/CapChanged` :2249-2253.
- **`ShowStatsChangedMessage` delta printing** :636-697.
- **`AddItemToContainer` facet-change guards** :6434 and :6450 (`item.Container != containerSerial` conditions, commented as preventing containers closing on facet change).
- **`GameActions.RequestEquippedOPL()`** :3124, :3193, :3328.
- **`maparea`, `picinpicphued`, `gumppichued`/`gumppicphued`, `togglelimitgumpscale`, `tilepicasgumppic`, null-terminator break** entries in `CreateGump` :7040, :7471-7523 — layout commands beyond stock.
