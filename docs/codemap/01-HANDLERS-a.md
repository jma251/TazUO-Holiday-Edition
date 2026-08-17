# Server -> Client packet handlers, 0x00 – 0x5F

Handler reference for the 35 server-to-client packet ids in the range 0x00–0x5F that this
fork registers. All handlers live in `src/ClassicUO.Client/Network/PacketHandlers.cs` unless
another file is named. Short file names below resolve as:

| short | path |
| --- | --- |
| `PacketHandlers.cs` | `src/ClassicUO.Client/Network/PacketHandlers.cs` |
| `PacketsTable.cs` | `src/ClassicUO.Client/Network/PacketsTable.cs` |
| `World.cs` | `src/ClassicUO.Client/Game/World.cs` |
| `Entity.cs`, `Mobile.cs`, `Item.cs`, `PlayerMobile.cs`, `GameObject.cs`, `EntityTextContainer.cs` | `src/ClassicUO.Client/Game/GameObjects/` |
| `Chunk.cs`, `Map.cs` | `src/ClassicUO.Client/Game/Map/` |
| `WalkerManager.cs`, `MessageManager.cs`, `WorldTextManager.cs`, `EffectManager.cs`, `ContainerManager.cs`, `TitleBarStatsManager.cs`, `CorpseManager.cs`, `HouseManager.cs`, `SpellVisualRangeManager.cs`, `WorldMapEntityManager.cs`, `AutoLootManager.cs` | `src/ClassicUO.Client/Game/Managers/` |
| `GameController.cs`, `Main.cs` | `src/ClassicUO.Client/` |

**Reader convention.** `AnalyzePacket` (PacketHandlers.cs:181-195) builds a `StackDataReader`
over the shared read buffer and `Seek()`s it to offset **1** for fixed-length packets and
offset **3** for variable-length ones (PacketHandlers.cs:190-191, 215-228). All wire offsets
below are absolute byte offsets into the packet, including the id byte.

---

### 0x03 ClientTalk

- **Purpose** — Server echoes a talk/subcommand byte; the client applies nothing.
- **Wire** — Registered PacketHandlers.cs:239. PacketsTable.cs:44 length -1 (variable), reader
  starts at 3. `[3]` uint8 subcommand (PacketHandlers.cs:534), switched on 0x78 / 0x3C / 0x25 /
  0x2E. No further byte of the body is ever read; the reader is discarded on return.
- **Mutates** — nothing.
- **Creates / Destroys** — nothing.
- **Triggers** — nothing.
- **Ignores / partial** — all four switch cases have empty bodies (PacketHandlers.cs:536-546) and
  any other subcommand value falls out of the switch; every 0x03 is a no-op regardless of
  content. No `World.InGame` / `World.Player` guard exists, because nothing is touched.

---

### 0x0B Damage

- **Purpose** — An entity took N damage; client floats a damage number over it.
- **Wire** — `[1..4]` uint32BE serial (PacketHandlers.cs:557) fed to `World.Get` (World.cs:528);
  `[5..6]` uint16BE damage (PacketHandlers.cs:561), read only if the entity lookup succeeded.
  Declared length is version-dependent: PacketsTable.cs:52 seeds 0x010A, `AdjustPacketSizeByVersion`
  sets 0x07 for CV_500A+ (PacketsTable.cs:307) and 0x10A below (PacketsTable.cs:313). Only 6 body
  bytes are read either way. `damage` is a ushort passed as `int` to `AddDamage(uint,int)` and
  `InvokeOnEntityDamage(object,int)`.
- **Mutates** —
  - `WorldTextManager._damages[serial] = new OverheadDamage(...)` — WorldTextManager.cs:137 (keyed
    via the `Entity -> uint` implicit conversion, Entity.cs:313)
  - `entity._averageOverTime ??= new AverageOverTime(TimeSpan.FromSeconds(15))` — GameObject.cs:141
  - `entity._averageOverTime.AddValue(Time.Ticks, damage)` — GameObject.cs:143 (feeds GetCurrentDPS)
  - `text_obj.Time = Time.Ticks + 1500` — EntityTextContainer.cs:139
  - `OverheadDamage._messages.AddToFront(text_obj)` — EntityTextContainer.cs:141
  - `World.Journal.Add(damage text, hue, name, TextType.CLIENT, MessageType.Damage)` —
    EntityTextContainer.cs:137
- **Creates** — `OverheadDamage` (WorldTextManager.cs:136); `TextObject` from the static
  `QueuedPool<TextObject>` cap 1000 (EntityTextContainer.cs:114 -> TextObject.cs:83-86); `TextBox`
  via `TextBox.GetOne` with profile overhead font/size/width (EntityTextContainer.cs:135);
  `AverageOverTime` on first damage to that GameObject (GameObject.cs:141).
- **Destroys** — when the OverheadDamage deque exceeds 10, the oldest `TextObject` is
  `RemoveFromBack()?.Destroy()`'d (EntityTextContainer.cs:145); `TextObject.Destroy`
  (TextObject.cs:89-105) disposes its TextBox (:100) and returns the object to the pool (:104).
  No entity is created or destroyed.
- **Triggers** — `EventSink.InvokeOnEntityDamage(entity, damage)` — PacketHandlers.cs:566 ->
  EventSink.cs:98; the event has no in-repo subscribers (declaration EventSink.cs:97 only). No
  outgoing packet.
- **Ignores / partial** —
  - `World.Player == null`: return before reading anything (PacketHandlers.cs:552-555); damage lost.
  - `World.Get(serial) == null` (not in World.Mobiles or World.Items): the damage ushort is never
    read (PacketHandlers.cs:559). Damage to an entity already culled by the World.Update sweep
    (World.cs:356 / :438) is silently dropped.
  - `damage == 0`: no overhead text, no journal line, no event (PacketHandlers.cs:563).
  - Hue chosen from profile settings with fallbacks when `ProfileManager.CurrentProfile` is null
    (EntityTextContainer.cs:116-131), but EntityTextContainer.cs:132 then dereferences
    `ProfileManager.CurrentProfile.ShowDPS` unguarded.
  - Pre-5.0.0a clients: 259 further declared body bytes are dequeued and discarded.
  - Frame note: `_damages` is written here while `UpdateDamageOverhead` (WorldTextManager.cs:120)
    and `Draw` (WorldTextManager.cs:76) iterate it; parsing runs at GameController.cs:478 ahead of
    Scene.Update (GameController.cs:486), capped at MAX_PACKETS_PER_FRAME=25 (GameController.cs:136,141).
    `Plugin.ProcessRecvPacket` is invoked inside the parse loop (PacketHandlers.cs:148) and can re-enter.

---

### 0x11 CharacterStatus

- **Purpose** — Full status block for an entity: whole stat sheet for the player, name plus hit
  points for anything else.
- **Wire** — variable length, reader starts at 3.
  - `[3..6]` uint32BE serial (PacketHandlers.cs:578)
  - `[7..36]` ASCII(30) name, written straight to `entity.Name` with no null-trim (:587)
  - `[37..38]` uint16BE Hits (:588); `[39..40]` uint16BE HitsMax (:589)
  - `[41]` bool IsRenamable — only when `SerialHelper.IsMobile(serial)` (:605)
  - `[42]` uint8 type (:606)
  - `[43]` bool IsFemale — only when `type > 0` and `p.Position + 1 <= p.Length` (:608-610)
  - player-only from here: `[44..45]` str, `[46..47]` dex, `[48..49]` int (:625-627)
  - `[50..51]` Stamina, `[52..53]` StaminaMax, `[54..55]` Mana, `[56..57]` ManaMax (:628-631)
  - `[58..61]` uint32BE Gold (:632); `[62..63]` uint16BE cast to short PhysicalResistance (:633);
    `[64..65]` uint16BE Weight (:634)
  - type>=5 block is read **before** the type>=3 and type>=4 blocks: u16 WeightMax (:705), u8 race (:706)
  - type>=3: u16->short StatsCap (:731), u8 Followers (:732), u8 FollowersMax (:733)
  - type>=4: u16 fire/cold/poison/energy resist (:738-741), u16 Luck (:742), u16->short DamageMin
    (:743), DamageMax (:744), u32 TithingPoints (:745)
  - type>=6: fifteen consecutive uint16BE values (:750-779), each guarded individually by
    `p.Position + 2 > p.Length ? 0 : read`
- **Mutates** — `Entity.Name` (field Entity.cs:85) at PacketHandlers.cs:587; `Entity.Hits` via the
  setter Entity.cs:67-79 (fires `EventSink.InvokeOnPlayerStatChange` for a PlayerMobile, Entity.cs:73)
  at :588; `Entity.HitsMax` (Entity.cs:81) at :589; `HitsRequest = Received` at :593 when it was
  Pending; `Mobile.IsRenamable` (Mobile.cs:227) at :605; `Mobile.IsFemale` (Mobile.cs:226) at :610;
  `Stamina/StaminaMax/Mana/ManaMax` (Mobile.cs:233-236) at :628-631; `PlayerMobile.Gold`
  (PlayerMobile.cs:115) at :632, `PhysicalResistance` (PlayerMobile.cs:136) at :633, `Weight`
  (PlayerMobile.cs:148) at :634; `Strength/Dexterity/Intelligence` (PlayerMobile.cs:143/106/119) at
  :699-701 — assigned **after** the delta messages are printed; `WeightMax` (PlayerMobile.cs:149)
  at :705 from the wire or at :719/:725 computed locally from the just-assigned Strength;
  `Mobile.Race` (Mobile.cs:231) at :713; `StatsCap/Followers/FollowersMax` at :731-733;
  `FireResistance/ColdResistance/PoisonResistance/EnergyResistance/Luck/DamageMin/DamageMax/TithingPoints`
  at :738-745; `MaxPhysicResistence, MaxFireResistence, MaxColdResistence, MaxPoisonResistence,
  MaxEnergyResistence, DefenseChanceIncrease, MaxDefenseChanceIncrease, HitChanceIncrease,
  SwingSpeedIncrease, DamageIncrease, LowerReagentCost, SpellDamageIncrease, FasterCastRecovery,
  FasterCasting, LowerManaCost` at :750-779.
- **Creates / Destroys** — nothing; the entity must already exist.
- **Triggers** — `Client.Game.SetWindowTitle(World.Player.Name)` (:618);
  `TitleBarStatsManager.ForceUpdate()` (:621 -> TitleBarStatsManager.cs:96-99, 26);
  `GameActions.Print` str/dex/int delta system messages (:652, :668, :684);
  `EventSink.InvokeOnPlayerStatChange` from the Hits setter (Entity.cs:73);
  `UoAssist.SignalHits/SignalStamina/SignalMana` (:786-788);
  `TitleBarStatsManager.UpdateTitleBar()` (:789).
- **Ignores / partial** —
  - `World.Player == null` -> return (:573); the whole packet is dropped even for other entities.
  - `World.Get(serial) == null` -> return (:581). World.Get also returns null for an `IsDestroyed`
    entity (World.cs:551-554), so a status for an entity awaiting the pool sweep is dropped.
  - Mobile serial resolving to a non-Mobile -> return at :602 **after** Name, Hits, HitsMax and
    HitsRequest have already been written.
  - Non-mobile serial -> IsRenamable, type and everything after are never read.
  - `type == 0`, or fewer than 1 byte left -> IsFemale and every stat field skipped (:608).
  - `mobile != World.Player` -> all stats are skipped entirely; only Name/Hits/HitsMax/
    IsRenamable/IsFemale survive (:612).
  - Stat-change chat suppressed unless `Strength != 0` and profile non-null and
    `ShowStatsChangedMessage` (:636-640).
  - `type < 5` -> the server's WeightMax is ignored and synthesized: `7*(Strength>>1)+40` for
    CV_500A+, else `Strength*4+25` (:717-726).
  - race byte 0 is clamped up to 1 before the RaceType cast (:708-711).
  - Every type>=6 field is clamped to 0 when fewer than 2 bytes remain, so a truncated packet
    silently zeroes the remaining caps instead of aborting (:750-779).
  - Window title only updated when the new name is non-empty and differs from `oldName` (:614-616).

---

### 0x15 FollowR

- **Purpose** — One mobile is now following another.
- **Wire** — Registered PacketHandlers.cs:242. Fixed length 0x0009, reader starts at 1.
  `[1..4]` uint32BE tofollow (PacketHandlers.cs:796); `[5..8]` uint32BE isfollowing (:797). Reader
  ends at 9 = packet length.
- **Mutates** — nothing. The body is two local assignments and a return.
- **Creates / Destroys** — nothing.
- **Triggers** — nothing.
- **Ignores / partial** — both serials are read into locals and discarded. No `World.InGame` /
  `World.Player` guard exists; the handler is unconditionally safe because it writes nothing.

---

### 0x16 NewHealthbarUpdate

- **Purpose** — Toggle named healthbar status flags (poison, yellow bar) on one mobile.
- **Wire** — Registered for both 0x16 (PacketHandlers.cs:243) and 0x17 (:244), same method
  (line 800). Length: 0x16 is 0x01 pre-CV_500A and -1 from CV_500A (PacketsTable.cs:308/314), so on
  CV_500A+ the reader starts at 3.
  - `[3..6]` uint32BE serial -> `World.Mobiles.Get` (:812). Note `EntityCollection.Get` is a plain
    `TryGetValue` (EntityCollection.cs:40-45) and does **not** filter `IsDestroyed`, unlike `World.Get`.
  - `[7..8]` uint16BE count (:819)
  - per entry, `count` times: uint16BE type (:823), 1 byte bool enabled (:824). No bounds check of
    `count` against remaining length; StackDataReader returns 0 past the end
    (StackDataReader.cs:69-75, 178-186).
  - `p[0]` is used at :807 to discriminate 0x16; the indexer ignores its argument and always
    returns `_data[0]`, the packet id (StackDataReader.cs:39).
- **Mutates** — `Mobile._isSA_Poisoned = true/false` (Mobile.cs:272) via `SetSAPoison` called at
  PacketHandlers.cs:832 and :843; `Flags |= Flags.Poisoned` at :836; `Flags &= ~Flags.Poisoned` at
  :847; `Flags |= Flags.YellowBar` at :855; `Flags &= ~Flags.YellowBar` at :859 (Flags field
  declared Entity.cs:66). Nothing is added to or removed from World.Mobiles / World.Items.
- **Creates / Destroys** — nothing.
- **Triggers** — nothing: no outgoing packet, no gump, no event. Healthbar gumps read
  `Mobile.Flags` / `IsPoisoned` on their own draw pass (Mobile.cs:152-156).
- **Ignores / partial** —
  - `World.Player == null` -> return (:802-805).
  - `p[0] == 0x16 && Client.Version < CV_500A` -> return (:807-810); the pre-500A 0x16 is a 1-byte
    packet and the body is never parsed.
  - `World.Mobiles.Get(serial) == null` -> return (:814-817); the whole entry list is discarded and
    no request is made for the unknown mobile. No fallback to World.Items / World.Get.
  - Poison is written to **one of two** places by version: `SetSAPoison` on CV_7000+, the
    `Flags.Poisoned` bit below (:830-849). It never writes both, and the unmatched field stays
    written-but-unread on the wrong version.
  - `type == 3` consumes 3 bytes and does nothing — empty branch commented "???" (:862-865).
  - Any other type value is consumed and silently ignored (no else branch).
  - Because `World.Mobiles.Get` does not screen `IsDestroyed`, flags can be written onto a mobile
    already destroyed and awaiting the sweep at World.cs:396-412.
  - `Flags` is a plain OR / AND-NOT on a shared bitfield; a later 0x77/0x78 assigning Flags
    wholesale overwrites these bits.

---

### 0x17 NewHealthbarUpdate

- **Purpose** — Same handler as 0x16 (PacketHandlers.cs:800); toggles poison / yellow-bar on one mobile.
- **Wire** — PacketsTable 0x17 = -1 (variable) unconditionally, so the reader always starts at 3.
  Fields are identical to 0x16: `[3..6]` serial (:812), `[7..8]` count (:819), then `count` pairs of
  uint16BE type (:823) + 1 byte bool enabled (:824).
- **Mutates** — identical to 0x16: Mobile.cs:272 via PacketHandlers.cs:832/843, and
  PacketHandlers.cs:836 / :847 / :855 / :859 on `Entity.Flags` (Entity.cs:66).
- **Creates / Destroys** — nothing.
- **Triggers** — nothing.
- **Ignores / partial** — same set as 0x16 minus the version gate: the `p[0] == 0x16` early-out at
  :807 never fires for 0x17, so the pre-CV_500A refusal does not apply. `World.Player == null`
  (:802) and unknown mobile (:814) still drop the whole packet; type 3 is parsed and ignored
  (:862-865); other types are consumed silently; poison target field still depends on CV_7000.

---

### 0x1A UpdateItem

- **Purpose** — Describes an item (or a mobile-serial object) on the ground: identity, graphic,
  stack count, position, optional direction/hue/flags.
- **Wire** — PacketsTable.cs:67 marks 0x1A variable (-1), packetOffset = 3 (PacketHandlers.cs:227-228).
  - `+3` uint32BE serial (:876). Bit 0x80000000 is a "count follows" flag: `serial &= 0x7FFFFFFF`,
    `count = 1` as a marker (:884-888)
  - `+7` uint16BE graphic (:890). Bit 0x8000 is a "graphicInc follows" flag: `graphic &= 0x7FFF`
    (:892-894)
  - optional uint8 graphicInc, only when that bit was set (:895)
  - optional uint16BE count, only when the serial flag bit was set (:900); otherwise count is
    incremented 0 -> 1 (:904)
  - uint16BE x (:907); bit 0x8000 = "direction follows", `x &= 0x7FFF` (:909-913)
  - uint16BE y (:915); bit 0x8000 = "hue follows", `y &= 0x7FFF` (:917-921); bit 0x4000 tested
    after that mask = "flags follow", `y &= 0x3FFF` (:923-927)
  - optional uint8 direction (:931); int8 z (:934); optional uint16BE hue (:938); optional uint8
    flags (:943)
  - derived, not on the wire: `type = 2` when `graphic >= 0x4000` (:949-953); graphic is not masked
    here, the 0x4000 bit is stripped later at :6663
  - the same local `count` is passed twice to `UpdateGameObject`, as both `count` and the unused
    `UNK` argument (:955-969); `UNK_2` is the literal 1
- **Mutates** —
  - Item path (:6647-6697): `IsMulti = true` and `WantUpdateMulti` recomputed from graphic/x/y/z/hue
    difference, `Graphic = graphic & 0x3FFF` when type==2 (:6656-6663); else `IsDamageable =
    (type == 3)`, `IsMulti = false`, `Graphic = graphic` (:6667-6669)
  - `item.X/Y/Z` (:6672-6674); `item.LightID = (byte)direction` (:6675); `item.Layer =
    (Layer)direction` only when graphic == 0x2006 (:6679)
  - `Entity.Hue` through FixHue (:6682 -> Entity.cs:133; low-14-bit values >= 0x0BB8 collapse to 1,
    Entity.cs:121-124)
  - `item.Amount = count` (:6689); `Flags` (:6690); `Direction` (:6691, setter fires
    OnDirectionChanged, Entity.cs:97-101)
  - `Item.CheckGraphicChange` (:6692 -> Item.cs:607-646): corpses get AnimIndex=99, UsedLayer,
    `Direction &= 0x7F`, `Layer = (Layer)Direction` (Item.cs:617-630); multis call LoadMulti (Item.cs:642)
  - Mobile path, newly created (:6604-6611): `Graphic = graphic + graphic_inc`, `Direction =
    direction & Direction.Up`, Hue via FixHue, X, Y, Z, Flags
  - Mobile path, existing (:6703-6731): X/Y/Z/Direction/IsRunning written and Steps cleared at
    :6710-6715 when `World.Get(mobile) == null` or the mobile sits at 0xFFFF/0xFFFF; the same five
    written again at :6720-6725 when EnqueueStep fails; `Graphic = graphic & 0x3FFF` (:6729),
    FixHue (:6730), Flags (:6731)
  - `Mobile.Steps` appended by EnqueueStep (Mobile.cs:340/348/358); `LastStepTime = Time.Ticks`
    (Mobile.cs:325); SetAnimation writes 10 animation fields (Mobile.cs:393-404)
  - `mobile.SetInWorldTile` (:6770 -> GameObject.cs:265-272): X/Y/Z, `IsPositionChanged = true`,
    then AddToTile -> Chunk.AddGameObject writes `TileChunk/TileCellX/TileCellY` (Chunk.cs:178-180),
    `PriorityZ` (Chunk.cs:283) and `Tiles[x%8,y%8]` (Chunk.cs:289)
  - `item.SetInWorldTile` (:6800), only when `item.OnGround`
  - `ItemHold.UpdatedInWorld = true` (:6585); `ItemHold.Enabled = false`, `Dropped = false` (:6794-6795)
  - `Item.LoadMulti` (Item.cs:335-605) writes MultiGraphic, MultiInfo, MultiDistanceBonus
    (Item.cs:581-584), `house.Bounds` (Item.cs:586), `HouseManager.RememberFootprint` (Item.cs:591)
  - World.Items / World.Mobiles entries added by GetOrCreateItem/GetOrCreateMobile (World.cs:577 / :601)
- **Creates** — Item from the pool via `World.GetOrCreateItem -> Item.Create` (World.cs:576,
  Item.cs:255-261), filed at World.cs:577; Mobile via `World.GetOrCreateMobile -> Mobile.Create`
  (World.cs:600), filed at World.cs:601; `new House(Serial, 0, false)` plus HouseManager entry on
  first multi load (Item.cs:344-345); Multi components appended to `house.Components` (Item.cs:335-584).
- **Destroys** — GetOrCreateItem removes a destroyed item under the same serial and returns it to
  the pool first (World.cs:565-569); GetOrCreateMobile the same (World.cs:589-593);
  `World.RemoveItemFromContainer` for an existing item with a valid container (:6633 ->
  World.cs:617-653: `container.Remove(obj)` :644, `Container = 0xFFFFFFFF` :647, Next/Previous
  nulled and RemoveFromTile :650-652); `house.ClearComponents` inside LoadMulti destroys every
  existing Multi component (Item.cs:359 -> House.cs:197); `GameObject.RemoveFromTile` hands the
  chunk cell to `TNext ?? TPrevious` before unlinking (GameObject.cs:226-232).
- **Triggers** — ContainerGump.RequestUpdateContents, or PaperDollGump + ModernPaperdoll, for the
  held item's container (:6574-6582); `GameActions.SingleClick -> Send_ClickRequest` for new
  mobiles (ShowNewMobileNameIncoming) or corpses (ShowNewCorpseNameIncoming) (:6740/:6747);
  `GameActions.RequestMobileStatus -> Send_StatusRequest` for every newly created mobile (:6777);
  `EventSink.InvokeOnItemCreated / InvokeOnItemUpdated` (:6695/:6697);
  `EventSink.InvokeOnCorpseCreated` (:6807); `World.Player.TryOpenCorpses -> DoubleClickQueued`
  (:6810 -> PlayerMobile.cs:1449-1455); `UoAssist.SignalAddMulti` (Item.cs:635);
  `MiniMapGump.RequestUpdateContents` (Item.cs:593); `GameScene.UpdateMaxDrawZ(true)` (Item.cs:601);
  `BoatMovingManager.ClearSteps(Serial)` (Item.cs:604); `HouseDiagnostics.LogRangeProbe` (:6758) and
  `LogHouseItemArrived` (:6802) — log writes only; `Entity.Update` may later send
  `Send_NameRequest` and add a NameOverheadGump (Entity.cs:180-185).
- **Ignores / partial** —
  - `World.Player == null` -> immediate return, nothing consumed (:871-874).
  - Every optional field is read only when its high bit is set; count defaults to 1,
    direction/hue/flags default to 0 (:884-943).
  - graphic keeps its 0x4000 bit in the handler; the mask is applied only inside UpdateGameObject
    for the multi branch (:6663) and unconditionally for mobiles (:6729).
  - graphic_inc is not added for graphic 0x2006 (:6649-6652); the equivalent line in the handler
    is commented out (:946-947).
  - A mobile serial with `type == 3` is created as an Item, not a Mobile (:6594).
  - Returns without applying anything if GetOrCreateMobile / GetOrCreateItem yields null
    (:6598-6601, :6617-6620) or if `obj` is still null (:6642-6645).
  - `count == 0` is clamped up to 1 (:6684-6687).
  - When `serial == World.Player` the whole position/step block is skipped — 0x1A never moves the
    player (:6703).
  - `EnqueueStep` refuses once `Steps.Count >= Constants.MAX_STEP_COUNT` (Mobile.cs:306-309); the
    handler then snaps the mobile and clears its steps (:6718-6726). It returns true and adds
    nothing when the queued end position already equals the target (Mobile.cs:313-316), silently
    dropping the server's position.
  - SingleClick is skipped when `obj.IsClicked` is already true (:6734) or the profile flags are
    off (:6738/:6745).
  - Items enter a world tile only when `item.OnGround`, i.e. Container invalid (:6798, Item.cs:188).
  - LoadMulti is skipped unless `MultiDistanceBonus == 0` or
    `HouseManager.IsHouseInRange(Serial, ClientViewRange)` (Item.cs:637-640); CheckGraphicChange
    does nothing at all for a multi with `WantUpdateMulti` false (Item.cs:633).
  - AutoOpenCorpses is skipped while targeting (CorpseOpenOptions 1 or 3) or while hidden (2 or 3)
    (PlayerMobile.cs:1439-1447).
  - Frame note: mutates World.Items/World.Mobiles between sweeps (GameController.cs:478 before
    GameScene.cs:913); World.Update then walks both dictionaries by key (World.cs:350, :424) and
    swaps pooled objects out at World.cs:402-408 and :467-473.

---

### 0x1B EnterWorld

- **Purpose** — Server hands the client its player character: serial, body graphic, position,
  facing — the login handoff into the world.
- **Wire** — PacketsTable.cs:68 fixed length 0x25 (37); reader starts at 1.
  - `[1..4]` uint32BE serial (PacketHandlers.cs:974)
  - `[5..8]` `p.Skip(4)` — never read (:978)
  - `[9..10]` uint16BE -> `World.Player.Graphic` (:979)
  - `[11..12]` uint16BE x (:982); `[13..14]` uint16BE y (:983)
  - `[15..16]` read as uint16BE then cast `(sbyte)` -> z (:984); two bytes consumed, value truncated
    to the low byte
  - `[17]` uint8 `& 0x7` -> Direction (:992); run/flag bits masked off
  - `[18..36]` the remaining 19 bytes are never read
- **Mutates** —
  - `World.Player = new PlayerMobile(serial)` — World.cs:194
  - `World.Mobiles[serial] = Player` — World.cs:195 via EntityCollection.cs:59 (Add returns false
    and does nothing if the key already exists)
  - `World.Player.Graphic` — PacketHandlers.cs:979
  - `Mobile.IsFemale` / `Mobile.Race` derived from Graphic — Mobile.cs:1061,1062,1070,1071,1078,
    1079,1086,1087,1094,1095,1102,1103 (only for 0x0190-0x0193, 0x025D, 0x025E, 0x029A, 0x029B;
    anything else leaves both untouched)
  - `World.MapIndex = 0` when `World.Map == null` — PacketHandlers.cs:988; the setter
    (World.cs:106-161) calls InternalMapChangeClear(true) (:113), MapLoader.LoadMap (:148),
    `World.Map = new Map.Map(value)` (:149), `GameCursor.Graphic = 0xFFFF` (:155),
    `UoAssist.SignalMapChanged` (:158)
  - `World.Player.X/Y/Z` — GameObject.cs:267-269 via SetInWorldTile (PacketHandlers.cs:991)
  - `IsPositionChanged = true` — GameObject.cs:252
  - `TileChunk / TileCellX / TileCellY` and `Chunk.Tiles[x,y]` linkage — Chunk.cs:178-180 via
    AddToTile (GameObject.cs:271 -> 173 -> 182)
  - `World.Player.Direction` — Entity.cs:99 (setter fires OnDirectionChanged)
  - `World.RangeSize.X` / `.Y` — PacketHandlers.cs:993 / :994
  - `World.Light.Overall` and `IsometricLight.Recalculate` — PacketHandlers.cs:1001,
    IsometricLight.cs:58-59
  - `World.Season = Desolation` plus `UpdateGraphicBySeason()` on every GameObject in every used
    chunk — World.cs:208 and :218, only when `World.Player.IsDead`
  - `Entity.IsClicked = true` on the player — GameActions.cs:666 via SingleClick (:1024)
- **Creates** — PlayerMobile (World.cs:194); `Map.Map` (World.cs:141 or :149 inside the MapIndex
  setter). No item or non-player mobile is created.
- **Destroys** — If a Player already existed, `World.Clear()` runs first (World.cs:191): destroys
  every Mobile (World.cs:924 -> :710) and Item (World.cs:929 -> :677), disposes the player's
  BaseHealthBarGump (:932), `Items.Clear()` (:936), `Mobiles.Clear()` (:937), `Player.Destroy()`
  (:938), `Map.Destroy()` (:940), resets Light (:942-943), ClientLockedFeatures (:944), Party (:945),
  TargetManager.LastAttack (:946), MessageManager.PromptData (:947), effects (:948), CorpseManager
  (:950), OPL (:951), WMapManager (:952), HouseManager (:953), Season/OldSeason (:955-956), Journal
  (:958), WorldTextManager (:959), ActiveSpellIcons (:960), SkillsRequested (:962). Clear passes the
  entity with `forceRemove` defaulting false, so nothing is removed from the dictionaries or pooled
  inside those loops; the `Clear()` calls at :936/:937 drop the entries wholesale without
  ReturnToPool. `InternalMapChangeClear(true)` (World.cs:965) inside the MapIndex setter
  force-removes and pools every Item and Mobile except the player and items rooted at the player:
  World.cs:995 `RemoveItem(serial,true)` -> Items.Remove + ReturnToPool (World.cs:681-683),
  World.cs:1015 `RemoveMobile(serial,true)` -> Mobiles.Remove + ReturnToPool (World.cs:714-716);
  multis are also dropped from HouseManager (World.cs:987).
- **Triggers** — `EventSink.InvokeOnPlayerCreated()` (World.cs:196);
  `Audio.UpdateCurrentMusicVolume()` (:1007); `Send_GameWindowSize(camera W/H)` (:1013);
  `Send_Language(Settings.GlobalSettings.Language)` (:1019); `Send_ClientVersion` (:1022,
  unconditional); `Send_ClickRequest(player)` via GameActions.SingleClick (GameActions.cs:660,
  called :1024); `Send_SkillsRequest(player)` (:1025);
  `Send_ShowPublicHouseContent(profile.ShowHouseContent)` (:1037); `Send_ToPlugins_AllSkills()`
  (:1042); `Send_ToPlugins_AllSpells()` (:1043); the Direction setter ->
  `PlayerMobile.OnDirectionChanged` -> TryOpenDoors (PlayerMobile.cs:1460-1477), which LINQ-walks
  `World.Items.Values` and may call `GameActions.OpenDoor()`; `Audio.PlayMusic(42)` when dead and
  `CanSeasonMusicTakeOver()` (World.cs:230-232).
- **Ignores / partial** —
  - `World.MapIndex` is forced to 0 only when `World.Map == null` (:986); with a map already loaded
    the position is applied to whatever map is loaded and no map switch happens.
  - Z is read from two bytes and truncated by the `(sbyte)` cast (:984).
  - Direction masked with 0x7 (:992), dropping running/flag bits.
  - Custom light override only when profile non-null and `UseCustomLightLevel` (:996-999); with
    `LightLevelType == 1` it takes `Math.Min(existing Overall, profile level)` rather than the
    profile value (:1003).
  - `Send_GameWindowSize` / `Send_Language` skipped entirely below CV_200 (:1009);
    `Send_GameWindowSize` additionally needs a profile (:1011).
  - `Send_ShowPublicHouseContent` skipped below CV_70796 or with no profile (:1032-1035).
  - Season changed only when `World.Player.IsDead` (:1027); ChangeSeason returns before touching
    music when music < 0 (World.cs:224) — here 42 is passed, so music is attempted but only if
    `CanSeasonMusicTakeOver()` (World.cs:230).
  - `World.Clear()` runs only when a Player already exists (World.cs:189) — a second 0x1B on a live
    session tears the whole world down.
  - `ProfileManager.Load` runs only when `CurrentProfile == null` (World.cs:183).

---

### 0x1C Talk

- **Purpose** — ASCII speech / system text from an entity or the server, shown overhead and journaled.
- **Wire** — PacketsTable.cs:69 length -1 (variable), reader starts at 3.
  - `[3..6]` uint32BE serial (:1048)
  - `[7..8]` uint16BE graphic (:1050) — read, used only in the SYSTEM-ack test
  - `[9]` uint8 MessageType (:1051); `[10..11]` uint16BE hue (:1052); `[12..13]` uint16BE font (:1053)
  - `[14..43]` ReadASCII(30) fixed-width name (:1054)
  - `[44..]` null-terminated ASCII text, only when `p.Length > 44` (:1057-1061); `p.Seek(44)` at
    :1059 re-anchors to the byte the 30-char name read already left the cursor on
  - `p.Length <= 44` -> text is `string.Empty` and nothing further is read (:1064)
- **Mutates** — `entity.Name = name` (or text when name is empty) — field Entity.cs:85, written at
  PacketHandlers.cs:1098, only when the entity exists and its Name is currently null/empty;
  TextObject appended to the entity's overhead container — MessageManager.cs:162 or :297
  (`parent.AddMessage(msg)`); journal/overhead state via `MessageManager.CreateMessage`
  (MessageManager.cs:317) and gump text routing (MessageManager.cs:219-229, 262-295). No
  World.Items / World.Mobiles / map state is touched.
- **Creates** — TextObject message objects (MessageManager.cs:317); MessageEventArgs for
  `InvokeRawMessageReceived` (MessageManager.cs:98) and `InvokeMessageReceived` (MessageManager.cs:303).
- **Destroys** — nothing.
- **Triggers** — `Send_ACKTalk()` (PacketHandlers.cs:1076, then immediate return);
  `EventSink.InvokeRawMessageReceived` (MessageManager.cs:98); `EventSink.InvokeMessageReceived`
  (MessageManager.cs:303); `GridContainer.HandleObjectMessage` / `ModernPaperdoll.HandleObjectMessage`
  for TextType.OBJECT (MessageManager.cs:223, 227); PaperDollGump / ContainerGump / TradingGump
  AddText for text on a non-ground item (MessageManager.cs:271, 277, 283);
  `ForcedTooltipManager.IsObjectTextRequested` may consume the message entirely (MessageManager.cs:94).
- **Ignores / partial** —
  - Full early-out with only an ACK sent when `serial==0 && graphic==0 && type==Regular &&
    font==0xFFFF && hue==0xFFFF && name.StartsWith("SYSTEM")` (:1067-1079); the text is discarded.
  - text_type stays SYSTEM (never OBJECT) when `type==System || serial==0xFFFFFFFF || serial==0 ||
    (name.ToLower()=="system" && entity==null)` (:1083-1091).
  - `entity.Name` is only overwritten when currently null or empty (:1096); a server-sent name for
    an already-named entity is ignored.
  - `MessageManager.HandleMessage` returns immediately on empty text (MessageManager.cs:85-88).
  - With ForceTooltipsOnOldClients and an OBJECT message the whole message is swallowed
    (MessageManager.cs:94-95).
  - `Profile.OverrideAllFonts` replaces the server's font and unicode flag (MessageManager.cs:111-115).
  - MessageType System / Command / Encoded / ChatSystem fall through with no overhead text at all
    (MessageManager.cs:119-124).
  - Guild / Alliance messages return early under IgnoreGuildMessages / IgnoreAllianceMessages
    (MessageManager.cs:166, 170) — these return, so InvokeMessageReceived at :303 never fires.
  - Party overhead skipped unless DisplayPartyChatOverhead, and dropped if no party member matches
    the name (MessageManager.cs:127, 145).
  - Senders on `IgnoreManager.IgnoredCharsList` dropped unless the type is Spell
    (MessageManager.cs:149, 240).
  - Spell text may be rewritten by SpellDisplayFormat and re-hued by Benefic/Harmful/Neutral profile
    hues, replacing the server's hue (MessageManager.cs:180-205).
  - `parent == null` breaks out of the Limit3Spell path with no message (MessageManager.cs:234-237).

---

### 0x1D DeleteObject

- **Purpose** — Server orders one entity removed from the client's world.
- **Wire** — fixed length 5 (PacketsTable 0x1D = 0x0005), reader starts at 1. `[1..4]` uint32BE
  serial (PacketHandlers.cs:1112).
- **Mutates** —
  - `mob.Mount = null` when the deleted item was on `Layer.Mount` of the root mobile —
    PacketHandlers.cs:1154 (Mobile.cs:180)
  - `ItemHold.Enabled = false` — PacketHandlers.cs:1172
  - HouseManager entry removed and `House.ClearComponents` run — PacketHandlers.cs:1239 ->
    HouseManager.cs:272-273; ClearComponents sets multi `WantUpdateMulti = true` (House.cs:185),
    Destroys and removes every Multi component (House.cs:197-198)
  - container linked-list unlink `cont.Remove(item)` — PacketHandlers.cs:1246 ->
    LinkedObject.cs:100-121
  - Mobile path: `World.RemoveMobile(serial, true)` — PacketHandlers.cs:1230 -> child items removed
    (World.cs:704), `OPL.Remove` (:709), `mobile.Destroy()` (:710), `Mobiles.Remove` (:714),
    `ReturnToPool()` (:716)
  - Item path: `World.RemoveItem(serial, true)` — PacketHandlers.cs:1259 ->
    RemoveItemFromContainer (World.cs:665) sets `Container = 0xFFFFFFFF` (:647), nulls
    Next/Previous (:650-651), RemoveFromTile (:652); recursive child removal (:671); `OPL.Remove`
    (:676); `Destroy()` (:677); `Items.Remove` (:681); `ReturnToPool()` (:683)
  - Chunk tile linkage rewritten by RemoveFromTile: `chunk.Tiles[TileCellX,TileCellY]` handed to
    `TNext ?? TPrevious` (GameObject.cs:231); TNext/TPrevious nulled (:244-245)
  - `GameObject.Destroy` clears Next/Previous/RenderListNext/_averageOverTime, calls Clear(),
    RemoveFromTile(), TextContainer.Clear(), sets IsDestroyed=true, PriorityZ=0,
    IsPositionChanged=false, Hue=0, Offset=Zero, RealScreenPosition=Zero, IsFlipped=false,
    originalGraphic=0, Graphic=0, ObjectHandlesStatus=NONE, FrameInfo=Empty — GameObject.cs:454-472
  - `Entity.Destroy` zeroes AnimIndex and LastAnimationChangeTime — Entity.cs:204-205
  - `World.Player.Abilities[0]` and `[1]` rewritten by UpdateAbilities — PacketHandlers.cs:1263 ->
    PlayerMobile.cs:321-322 and the graphic switch below
- **Creates** — nothing.
- **Destroys** — the addressed Mobile or Item and every item it contains recursively (World.cs:671 /
  :704); the entity handed back to `Item._pool` / `Mobile._pool` (World.cs:683 / :716;
  Item.cs:321-331 guards double-return with `_inPool`); House components when the deleted item was
  a multi (House.cs:197); BulletinBoardItem gump disposed (PacketHandlers.cs:1197); gumps disposed
  inside `Item.Destroy` when `item.Opened`: ContainerGump, GridContainer, SpellbookGump, MapGump,
  GridLootGump (corpses only), BulletinBoardGump, SplitMenuGump (Item.cs:276-289); PaperDollGump and
  ModernPaperdoll disposed inside `Mobile.Destroy` for non-PlayerMobile (Mobile.cs:1139-1140).
- **Triggers** — Trading gump RequestUpdateContents (:1165); PaperDollGump / ModernPaperdoll
  RequestUpdateContents (:1177-1178 and :1250-1251); ContainerGump / GridContainer
  RequestUpdateContents (:1181/:1183); NearbyLootGump and GridLootGump RequestUpdateContents
  (:1188/:1191); `BulletinBoardGump.RemoveBulletinObject(serial)` (:1203); MiniMapGump
  RequestUpdateContents (:1256); `GameActions.SendCloseStatus(Serial, ...)` fired from
  `Entity.Destroy` (Entity.cs:202 — an outgoing packet issued from inside the destroy path);
  `HouseDiagnostics.LogHouseItemDeleteOrdered` / `LogRangeProbe` (:1128/:1131).
- **Ignores / partial** —
  - Returns if `World.Player == null` (:1107).
  - Returns if `serial == World.Player` (:1114) — an order to delete the player is ignored outright.
  - Returns if `World.Get(serial)` is null or destroyed (:1121; World.Get returns null for
    IsDestroyed entities, World.cs:551).
  - Mount cleared only when `it.Layer == Layer.Mount` and the root container resolves to a Mobile
    (:1152).
  - `updateAbilities` set only when the root container is the player and layer is
    OneHanded/TwoHanded (:1159-1160).
  - ItemHold disabled only when the container serial (masked `0x7FFFFFFF` at :1144) equals the
    player and layer is Invalid (:1170).
  - Paperdoll refresh only when `it.Layer != Layer.Invalid` (:1175).
  - GridLootGump refresh only when root graphic is 0x2006 and GridLootType is 1 or 2 (:1186-1189).
  - Bulletin board teardown only when `it.Graphic == 0x0EB0` (:1195).
  - **Hard deferral**: if `World.CorpseManager.Exists(0, serial)` the handler returns at
    :1209-1212 having done only the gump refreshes above. The entity is NOT removed from
    World.Items/World.Mobiles, not destroyed, not pooled. `CorpseManager.Exists` matches on
    ObjectSerial (CorpseManager.cs:91), i.e. anything currently playing a death animation.
  - The `World.Party.Contains(serial)` branch at :1216 is a no-op — both the `m.RemoveFromTile()`
    call and the alternative are commented out, so party members take the normal RemoveMobile path.
  - `HouseManager.Remove` only when `item.IsMulti` (:1237).
  - MiniMapGump refresh only when the item has no resolvable container and is a multi (:1254).
  - UpdateAbilities is only reached in the Item branch (:1261); for a Mobile the flag is computed
    and never consumed.
  - Frame note: mutates World.Items / World.Mobiles, swept with foreach in World.Update
    (World.cs:350, :424). Dispatch is main-thread and ordered before Scene.Update
    (GameController.cs:478 vs GameScene.cs:913), but plugin-injected packets are appended from
    plugin callbacks (Plugin.cs:757, 779) into `_pluginsBuffer` and parsed in the same pass
    (PacketHandlers.cs:95).

---

### 0x20 UpdatePlayer

- **Purpose** — Relocates/reskins the player (teleport, resurrect, polymorph); client resyncs
  position, body, hue, flags and season.
- **Wire** — fixed 19 bytes (PacketsTable.cs:73 = 0x13), reader starts at 1; the read exactly
  consumes the packet.
  - `[1..4]` uint32BE serial (:1275); `[5..6]` uint16BE graphic (:1276)
  - `[7]` uint8 graphic_inc (:1277) — passed to the inner overload as `graph_inc` (:6819) and never
    used in its body
  - `[8..9]` uint16BE hue (:1278); `[10]` uint8 flags (:1279)
  - `[11..12]` uint16BE x (:1280); `[13..14]` uint16BE y (:1281)
  - `[15..16]` uint16BE serverID (:1282) — passed in at :6825 and never used
  - `[17]` uint8 direction (:1283); `[18]` int8 z (:1284)
- **Mutates** —
  - `World.RangeSize.X` / `.Y` — :6831 / :6832
  - `Walker.WalkingFailed = false` — :6838
  - `World.Player.Graphic` — :6839; setter GameObject.cs:107-117 writes originalGraphic, runs
    GraphicsReplacement.Replace, writes Hue (GameObject.cs:118-125 runs ReplaceHue), writes graphic,
    calls OnGraphicSet
  - `World.Player.Direction = direction & Direction.Mask` — :6840; Entity.cs:92-103 fires only on
    change, then OnDirectionChanged -> PlayerMobile.cs:1460-1464 -> TryOpenDoors (:1466-1478)
  - Player hue via FixHue — :6841; Entity.cs:115-134 masks `hue & 0x3FFF`, clamps any value
    >= 0x0BB8 down to 1 (:121-124), re-ORs `hue & 0xC000`, else keeps only `hue & 0x8000`, writes
    Hue at Entity.cs:133
  - `World.Player.Flags = flags` — :6842 (Entity.cs:66 plain field)
  - `Walker.DenyWalk(0xFF, -1, -1, -1)` — :6843 -> WalkerManager.cs:108-121: `ClearSteps()`
    (Mobile.cs:298-302 clears Steps and zeroes Offset), then Reset() (WalkerManager.cs:187-196
    zeroes UnacceptedPacketsCount, StepsCount, WalkSequence, CurrentWalkSequence, WalkingFailed,
    ResendPacketResync, LastStepRequestTime)
  - `GameScene.Weather.Reset()` — :6849 -> Weather.cs:83-90 (Type=0, Count=CurrentCount=
    Temperature=0, Wind=0, _windTimer=_timer=0, CurrentWeather=null)
  - `GameScene.UpdateDrawPosition = true` — :6850 (GameScene.cs:114)
  - `World.Season` — World.cs:208, reached from :6866 (Desolation, music 42) or :6870
    (World.OldSeason, World.OldMusicIndex)
  - every loaded map object's season graphic — World.cs:210-222 walks `Map.GetUsedChunks()`
    (Map.cs:260-266, a lazy yield over `_usedIndices`) and for all 8x8 cells walks the tile list
    from `Chunk.GetHeadObject` (Chunk.cs:160-170) calling `UpdateGraphicBySeason`
    (GameObject.cs:248). Concrete writes: Land.cs:91 Graphic, :92 AllowedToDraw; Static.cs:103
    SetGraphic, :104 AllowedToDraw, :105 IsVegetation; Multi.cs:106 Graphic, :107 IsVegetation
  - `Walker.ResendPacketResync = false` — :6874
  - `World.Player.X/Y/Z` — :6876 SetInWorldTile -> GameObject.cs:267/268/269; then
    UpdateScreenPosition sets `IsPositionChanged = true` (:252) and calls OnPositionChanged; then
    AddToTile (:271)
  - tile linkage — GameObject.cs:171-184 AddToTile calls `World.Map.GetChunk(x, y)` with load=true
    (Map.cs:71-101), which on a cold block does `_usedIndices.AddLast(block)` (Map.cs:96),
    Chunk.Create + chunk.Load (Map.cs:97-98). Then RemoveFromTile (GameObject.cs:220-246) and
    `Chunk.AddGameObject` writes TileChunk/TileCellX/TileCellY (Chunk.cs:178-180), PriorityZ
    (:285), possibly `Tiles[x,y]` (:289)
  - `World.Player.Abilities[0]` and `[1]` — :6877 UpdateAbilities -> PlayerMobile.cs:312-313 reset
    both to `Ability.Invalid`, then reassigned per equipped OneHanded/TwoHanded graphic
    (PlayerMobile.cs:301+)
  - player bank contents removed — :6837 CloseBank -> PlayerMobile.cs:1493-1522
- **Creates** — map chunks: Map.cs:97-98 `Chunk.Create` + `Load` may build a whole new terrain block
  (with its Land/Static objects) as a side effect of SetInWorldTile/AddToTile. No entity is created.
- **Destroys** — every item inside the player's bank: PlayerMobile.cs:1503-1510 loops the bank's
  Items calling `World.RemoveItem(first, true)` (World.cs:655-687) — recursive removal of nested
  contents (:667-674), `OPL.Remove` (:676), `Destroy()` (:677), `Items.Remove` (:681) and
  `ReturnToPool()` (:683, Item pool Item.cs:52+). `bank.Items = null` (PlayerMobile.cs:1512),
  `bank.Opened = false` (:1520). Bank gumps: ContainerGump and GridContainer disposed at
  PlayerMobile.cs:1515 and :1517. `Item.Destroy` (Item.cs:263-331) itself disposes
  ContainerGump/GridContainer/SpellbookGump/MapGump/GridLootGump/BulletinBoardGump for that serial
  when Opened, and returns the Item to `_pool` (Item.cs:329).
- **Triggers** — `GameActions.OpenDoor()` can be sent from the Direction setter (Entity.cs:100 ->
  PlayerMobile.cs:1460 -> TryOpenDoors :1466-1478, which scans `World.Items.Values` with LINQ
  `Any()`); music change World.cs:224-233 — if music index >= 0 and
  `Audio.CanSeasonMusicTakeOver()` (AudioManager.cs:514-524), `Audio.PlayMusic(music, false)`;
  the container/paperdoll gump disposals listed above.
- **Ignores / partial** —
  - The whole packet is dropped if `World.Player` is null (:1270-1273).
  - The inner overload does **nothing at all** unless `serial == World.Player` (:6829). A 0x20
    naming any other serial is fully parsed then silently discarded.
  - `graphic_inc` is parsed and threaded through but never applied — the graphic is used raw.
  - `serverID` is parsed and threaded through but never applied — no map/server switch here.
  - Direction masked to `Direction.Mask` at :6840, dropping the Running/Up high bits.
  - Hue is clamped, not applied verbatim: Entity.cs:121-124 replaces any hue >= 0x0BB8 with 1.
  - Season only touched when the dead/alive state actually flipped (:6862); if `olddead == IsDead`
    season and music are left alone. `IsDead` is derived from Graphic (Mobile.cs:159-169), so the
    flip is decided by the graphic just received.
  - GameScene work at :6847-6851 skipped when `GetScene<GameScene>()` returns null.
  - Target reset on body change is commented out (:6854-6860, "std client keeps the target open!").
  - `CloseRangedGumps` (:6875) is effectively a no-op: PlayerMobile.cs:1528-1529 reads
    `if (UIManager.Gumps.Count > i) continue;`, true for every index visited, so the switch body at
    PlayerMobile.cs:1535+ is never reached.
  - CloseBank does nothing unless a `Layer.Bank` item exists and `bank.Opened` (PlayerMobile.cs:1497),
    and skips the item purge when the bank is empty (:1499).
  - TryOpenDoors is skipped while dead or when `AutoOpenDoors` is off (PlayerMobile.cs:1468).
  - Music is left alone when ChangeSeason is called with music < 0 (World.cs:224-227) — the
    resurrect path can pass `OldMusicIndex`, which defaults to -1 (World.cs:97).
  - AddToTile silently does nothing when `World.Map` is null or GetChunk returns null
    (GameObject.cs:173, 180).
  - Real-time hazard: ChangeSeason (World.cs:210) iterates the lazy `GetUsedChunks()` yield
    (Map.cs:260-266) while walking every tile list; later in the same handler SetInWorldTile ->
    AddToTile -> `Map.GetChunk` can call `_usedIndices.AddLast` (Map.cs:96). Sequential here, but
    both mutate collections the render/update sweep walks; up to 25 packets run back to back
    (GameController.cs:136) before Scene.Update (:486) and UIManager.Update (:491).

---

### 0x21 DenyWalk

- **Purpose** — Server rejects a movement request and states the player's real position and facing.
- **Wire** — Registered PacketHandlers.cs:249. PacketsTable.cs:74 fixed length 0x0008, reader at 1.
  `[1]` uint8 seq (:1296); `[2..3]` uint16BE x (:1297); `[4..5]` uint16BE y (:1298); `[6]` uint8
  direction (:1299) then masked `&= Direction.Up` (0x07 == Direction.Mask, Direction.cs:48-49) at
  :1300, discarding the 0x80 Running bit; `[7]` int8 z (:1301).
- **Mutates** — `Mobile.Steps` cleared and `Offset` zeroed (Mobile.cs:300-301, via
  WalkerManager.cs:110); `UnacceptedPacketsCount / StepsCount / WalkSequence / CurrentWalkSequence /
  WalkingFailed / ResendPacketResync / LastStepRequestTime` zeroed (WalkerManager.cs:189-195 via
  :112); `World.RangeSize.X/.Y` (WalkerManager.cs:116-117); PlayerMobile X/Y/Z
  (GameObject.cs:267-269 via WalkerManager.cs:119); `IsPositionChanged = true` (GameObject.cs:252);
  player unlinked from its old chunk cell — `chunk.Tiles[TileCellX,TileCellY]` handed to a
  neighbour (GameObject.cs:231), `TPrevious.TNext` / `TNext.TPrevious` rewritten (:236, :241);
  relinked into the destination cell — TileChunk/TileCellX/TileCellY (Chunk.cs:178-180), PriorityZ
  (:285), `Tiles[x,y]` (:289) or list splice (:338-341); `Map._terrainChunks[block]` and
  `_usedIndices` when the block was unloaded (Map.cs:96-99), `chunk.LastAccessTime` (Map.cs:116);
  `Entity._direction` (Entity.cs:99, written from PacketHandlers.cs:1304); Weather Type / Count /
  CurrentCount / Temperature / Wind / _windTimer / _timer / CurrentWeather=null (Weather.cs:85-89
  via :1306).
- **Creates** — a Chunk when the corrected position falls in an unloaded block: `Chunk.Create` +
  `Chunk.Load(Index)` (Map.cs:97-98) reached through AddToTile -> `World.Map.GetChunk`
  (GameObject.cs:173).
- **Destroys** — nothing.
- **Triggers** — SetInWorldTile -> UpdateScreenPosition -> `PlayerMobile.OnPositionChanged`
  (PlayerMobile.cs:1418): `Plugin.UpdatePlayerPosition` (:1422), `ScriptRecorder.UpdatePlayerPosition`
  (:1427), TryOpenDoors (:1429), TryOpenCorpses (:1430), `EventSink.InvokeOnPositionChanged`
  (:1432). TryOpenDoors LINQ-enumerates `World.Items.Values` (PlayerMobile.cs:1473) and may send
  `Socket.Send_OpenDoor` (GameActions.cs:1177). TryOpenCorpses enumerates `World.Items.Values`
  (PlayerMobile.cs:1449) and queues `GameActions.DoubleClickQueued -> GameScene.DoubleClickDelayed`
  (GameActions.cs:615). The Direction write at :1304 fires `OnDirectionChanged`
  (PlayerMobile.cs:1460) which calls TryOpenDoors a **second** time — one 0x21 can walk
  `World.Items` twice and send two Send_OpenDoor packets. `Weather.Reset` (Weather.cs:83).
- **Ignores / partial** —
  - Returns without reading anything if `World.Player == null` (:1291).
  - The sequence byte is read at :1296 and passed to `WalkerManager.DenyWalk`, which never uses it
    (WalkerManager.cs:108).
  - Position applied only inside `if (x != -1)` (WalkerManager.cs:114); x is an int widened from a
    ushort, so the guard can never be false.
  - Direction truncated to its low 3 bits at :1300; the Running bit is dropped.
  - Weather reset only when both GameScene and its Weather are non-null (null-conditional chain
    at :1306).
  - Frame note: parsed at GameController.cs:478 ahead of Scene.Update/World.Update at :486, but its
    TryOpenDoors/TryOpenCorpses enumerate `World.Items.Values` while later packets in the same
    batch (e.g. 0x2E) add and remove entries in that dictionary.

---

### 0x22 ConfirmWalk

- **Purpose** — Server acknowledges a walk request, echoing the sequence number and the player's
  notoriety.
- **Wire** — fixed length 0x0003 (PacketsTable.cs:75), reader at 1. `[1]` uint8 sequence (:1316);
  `[2]` uint8 notoriety (:1317), immediately masked `&~0x40` (clears the "can be renamed" bit)
  before use.
- **Mutates** —
  - `World.Player.NotorietyFlag = (NotorietyFlag)noto` — PacketHandlers.cs:1324 (plain public field,
    Mobile.cs:230; no setter side effects)
  - `UnacceptedPacketsCount--` — WalkerManager.cs:127 (only when non-zero)
  - `StepInfos[stepIndex].Accepted = true` — WalkerManager.cs:149
  - `World.RangeSize.X/.Y = StepInfos[stepIndex].X/.Y` — WalkerManager.cs:151-152 (World.cs:59, the
    map-loading anchor)
  - `World.RangeSize.X/.Y = StepInfos[0].X/.Y` — WalkerManager.cs:156-157 (stepIndex==0 late-ack path)
  - `StepInfos[i-1] = StepInfos[i]` array compaction — WalkerManager.cs:161
  - `StepsCount--` (:164), `CurrentWalkSequence--` (:165)
  - `ResendPacketResync = true` (:178), `WalkingFailed = true` (:181, blocks all further Walk calls
    until cleared at PacketHandlers.cs:6838), `StepsCount = 0; CurrentWalkSequence = 0` (:182-183)
    on the bad-step path
  - tile relink: `TileChunk = null` (GameObject.cs:224), `chunk.Tiles[TileCellX,TileCellY]` =
    successor (:231), neighbour TNext/TPrevious relink (:236/:241), TNext/TPrevious nulled
    (:244-245) — RemoveFromTile reached from AddToTile at PacketHandlers.cs:1327; then
    TileChunk/TileCellX/TileCellY (Chunk.cs:178-180), `priorityZ++` for a Mobile (Chunk.cs:207) then
    `obj.PriorityZ = priorityZ` (:285), `Tiles[x,y] = obj` with TPrevious/TNext nulled when the cell
    was empty (:289-291), or z-ordered splice (:338-352)
- **Creates** — nothing.
- **Destroys** — nothing is destroyed or pooled.
- **Triggers** — `NetClient.Socket.Send_Resync()` on a bad step — WalkerManager.cs:177, sent at most
  once until ResendPacketResync is cleared.
- **Ignores / partial** —
  - `World.Player == null` -> return, ack dropped (:1311-1314).
  - The 0x40 bit of the notoriety byte is masked off and never used (:1317).
  - Clamp: a masked notoriety of 0 or >= 8 is forced to 0x01 (Innocent) (:1319-1322); the server's
    value is discarded in that case.
  - `UnacceptedPacketsCount` only decremented when non-zero (WalkerManager.cs:125); an ack with no
    outstanding request leaves the counter alone.
  - If no StepInfos entry carries this sequence, `stepIndex == StepsCount` and the ack is treated as
    a bad step (WalkerManager.cs:132-142); the acked position is never applied to World.RangeSize.
  - An ack for a step earlier than CurrentWalkSequence that is not index 0 is also a bad step
    (WalkerManager.cs:167-170); the position is discarded.
  - Send_Resync is suppressed if ResendPacketResync is already true (WalkerManager.cs:175).
  - AddToTile resolves `World.Map?.GetChunk(x,y)`; if the map is null or the chunk unloaded,
    RemoveFromTile still runs but nothing is relinked, leaving the player unlinked from any tile
    list (GameObject.cs:173 / :180).
  - Frame note: relinks the player into `Chunk.Tiles`, the same per-cell lists the render/update
    sweep walks; runs from GameController.cs:478 ahead of Scene.Update at :486.

---

### 0x23 DragAnimation

- **Purpose** — Show an item flying from one mobile to another (or to/from a bare coordinate).
- **Wire** — fixed length 0x1A = 26, body starts at 1. `[1..2]` uint16BE graphic (:1332); `[3]`
  uint8 graphic increment added to graphic (:1333); `[4..5]` uint16BE hue (:1334); `[6..7]` uint16BE
  count (:1335) — read and never used; `[8..11]` uint32BE source serial (:1336); `[12..13]` uint16BE
  sourceX, `[14..15]` uint16BE sourceY, `[16]` int8 sourceZ (:1337-1339); `[17..20]` uint32BE dest
  serial (:1340); `[21..22]` destX, `[23..24]` destY, `[25]` int8 destZ (:1341-1343).
- **Mutates** — no World/Entity state; only EffectManager's own linked list gains a node
  (EffectManager.cs:240 PushToBack).
- **Creates** — `MovingEffect` (EffectManager.cs:109-130) when either endpoint serial is invalid;
  `DragEffect` (EffectManager.cs:145-164) when both endpoints are valid mobiles; both added to the
  EffectManager list at EffectManager.cs:240.
- **Destroys** — nothing.
- **Triggers** — `World.SpawnEffect` (PacketHandlers.cs:1384 -> World.cs:722 ->
  `EffectManager.CreateEffect` at EffectManager.cs:60).
- **Ignores / partial** —
  - graphic 0x0EED is rewritten to 0x0EEF, 0x0EEA to 0x0EEC, 0x0EF0 to 0x0EF2 before anything else
    (:1345-1356).
  - source is looked up only in `World.Mobiles` (:1358) — an item source is treated as absent and
    the serial is forced to 0 (:1362).
  - When the source mobile IS found, the server's sourceX/sourceY/sourceZ are discarded and replaced
    with the mobile's current position (:1366-1368). Same for dest: World.Mobiles only (:1371),
    serial zeroed if absent (:1375), packet coordinates overwritten if present (:1379-1381).
  - Effect type is Moving rather than DragEffect whenever either serial ends up invalid (:1385-1387).
  - speed (5), duration (5000), fixedDir (true), doesExplode (false), hasparticles (false) and blend
    mode are hardcoded, not from the wire (:1398-1403).
  - EffectManager increments a non-zero hue by 1 (EffectManager.cs:88-91) and multiplies duration by
    `Constants.ITEM_EFFECT_ANIMATION_DELAY` (EffectManager.cs:93).
  - EffectManager returns without creating anything if graphic == 0 (EffectManager.cs:98-101 /
    135-138).
  - `count` is parsed and never used.

---

### 0x24 OpenContainer

- **Purpose** — Open a container / spellbook / vendor buy window for a serial with a given container
  gump graphic.
- **Wire** — Registered PacketHandlers.cs:252. Length version-dependent: base table
  `0x0007, // 0x24`, overridden to 0x09 for CV_7090+ (PacketsTable.cs:392) and 0x07 below (:401).
  Fixed -> reader at 1. `[1..4]` uint32BE serial (:1423); `[5..6]` uint16BE graphic (:1424);
  `[7..8]` on CV_7090+ is **never read** — the handler stops after graphic, so those 2 bytes are
  discarded. `graphic` is also rewritten in place (:1574, 1582, 1590, 1598, 1606, 1614, 1622, 1630,
  1638) before being handed to the gump, so the gump's graphic is not always the wire graphic.
- **Mutates** —
  - static `_requestedGridLoot` (declared :63) = serial — :1552, only when the item is a corpse and
    GridLootType is 1 or 2. Consumed later by AddItemToContainer (:6515-6521).
  - `it.Opened = true` (field Item.cs:249) — :1710. Skipped when graphic == 0x0030, and when
    GridLootType == 1 took the early return at :1556.
  - `ClearContainerAndRemoveItems(it)` — :1714 -> :6881; inside, `World.RemoveItem(it, true)` per
    child (:6908) writes `Container = 0xFFFFFFFF` (World.cs:647), Next/Previous null (:650-651),
    RemoveFromTile (:652 -> GameObject.cs:220-246), `OPL.Remove` (:676), `Destroy()` (:677),
    `Items.Remove` (:681), `ReturnToPool()` (:683). Then `container.Items = null` (:6914).
  - static `ContainerManager.X` / `.Y` (ContainerManager.cs:57-58) written by
    CalculateContainerPosition called at :1675 — writes at ContainerManager.cs:75-76, 99-100,
    106-115, 122, 127, 141, 148, 152, 165, 169, 172, 181-183, 185-186, plus SetPositionNearGameObject
    (ContainerManager.cs:194+).
  - `UIManager._gumpPositionCache` entry for the serial removed — :1696 -> UIManager.cs:302.
  - `NearbyLootGump._corpsesRequested`: the serial is removed as a side effect of the *test* at
    :1540 — `IsCorpseRequested` defaults `remove:true` and deletes the entry (NearbyLootGump.cs:264).
  - `World.Player.ManualOpenedCorpses` / `AutoOpenedCorpses` mutated by the gump constructors:
    GridLootGump.cs:79 Remove, GridContainer.cs:174 Remove, ContainerGump.cs:127 Remove.
  - Vendor branch writes no world state at all.
- **Creates** — SpellbookGump (:1437) when graphic == 0xFFFF, added at :1445 -> UIManager.cs:485;
  ModernShopGump (:1465) or ShopGump (:1467) when graphic == 0x0030 per `UseModernShopGump`,
  populated by AddItem per stock item (:1499 / :1510); a `List<Item> buyList` per shop layer (:1492);
  GridLootGump (:1551) when the item is a corpse and GridLootType is 1 or 2; GridContainer (:1655)
  when `UseGridLayoutContainerGumps` and graphic != 0x091A and none exists; ContainerGump (:1684)
  otherwise, with `InvalidateContents = true` (:1688). No world entity is created.
- **Destroys** — existing SpellbookGump (:1435), ShopGump and ModernShopGump (:1458-1459),
  GridLootGump (:1550), ContainerGump (:1671, its screen coords read at :1669-1670 first and reused);
  every child Item of the container destroyed and pooled at :1714 -> World.cs:677/681/683, with
  `Item.Destroy` (Item.cs:263) additionally disposing that child's ContainerGump / GridContainer /
  SpellbookGump / MapGump / GridLootGump / BulletinBoardGump / SplitMenuGump when Opened
  (Item.cs:274-292); BuySellAgent disposes the shop gump this handler just added
  (BuySellAgent.cs:163) if a buy request was sent.
- **Triggers** — sound 0x0055 for the spellbook branch (:1447); container open sound inside the
  ContainerGump ctor (ContainerGump.cs:141-144) when `playsound` is true, i.e. only when no
  ContainerGump already existed (:1678); `BuySellAgent.Instance?.HandleBuyPacket(buyList, serial)`
  (:1531) — called once **per shop layer** inside the loop, so up to two calls per packet; it can
  send an outgoing 0x3B buy request (BuySellAgent.cs:161), print a system message (:162) and dispose
  the shop gump (:163); `EventSink.InvokeOnOpenContainer(item, serial)` (:1694 -> EventSink.cs:104 ->
  AutoLootManager.cs:225 -> CheckCorpse :174 -> HandleCorpse :141, which walks `corpse.Items` and
  calls CheckAndLoot per item — this runs BEFORE :1714 destroys those very items);
  `GridContainer.RequestUpdateContents()` (:1651) when a grid container already exists.
- **Ignores / partial** —
  - Whole packet dropped if `World.Player == null` (:1418-1421).
  - Spellbook branch aborts if `World.Items.Get(serial)` is null (:1430-1433); the tail block at
    :1704 then finds the item null too, so nothing is marked Opened.
  - Vendor branch aborts if the mobile is unknown (:1453-1456) — the entire buy list is discarded.
  - A shop layer whose container item has no children is skipped (:1477-1481) — that layer's stock
    is dropped silently.
  - List traversal direction is reversed unless `item.Graphic == 0x2AF8` (:1483, comment: matches
    hardcoded original-client logic).
  - `vendor.FindItemByLayer(layer)` is dereferenced with no null guard (:1473-1475);
    FindItemByLayer returns null when the layer is absent (Entity.cs:310).
  - If `NearbyLootGump.IsCorpseRequested(serial)` (:1540), **every** gump-creation path is skipped —
    the server said open, the client opens nothing — but :1694-1696 and the :1704 tail still run.
  - GridLootType == 1 returns from the handler entirely (:1554-1557), so InvokeOnOpenContainer,
    RemovePosition and the whole :1704-1717 tail never run.
  - The large-container graphic substitution (:1561-1643) applies only when CV_706000+ AND profile
    non-null AND `UseLargeContainerGumps` AND the replacement texture actually loads
    (`gumps.GetGump(x).Texture != null`); a missing texture keeps the server's graphic.
  - Grid layout path skipped for graphic 0x091A (:1646).
  - An already-open GridContainer is only refreshed; no new gump, no position recalculation
    (:1649-1652).
  - Unknown item -> only `Log.Error`, nothing opens (:1698-1701).
  - The whole Opened/clear tail is skipped for graphic == 0x0030 (:1704).
  - Contents are NOT cleared when the item is a corpse or graphic == 0xFFFF (:1712).
  - `ProfileManager.CurrentProfile` is dereferenced with no null check at :1464, :1498, :1554, :1646
    while being null-checked at :1563. GridContainer's ctor dereferences
    `World.Player.FindItemByLayer(Layer.Backpack).Serial` unguarded (GridContainer.cs:142).

---

### 0x25 UpdateContainedItem

- **Purpose** — Server places a single item inside a container (backpack, corpse, bank, trade box,
  spellbook, bulletin board).
- **Wire** — fixed 0x15 (21) on CV_6017+, else 0x14 (20) (PacketsTable.cs:346-354); reader at 1.
  - `+1` uint32BE item serial (:1727)
  - `+5` uint16BE **plus** `+7` uint8, summed numerically into one ushort:
    `graphic = (ushort)(ReadUInt16BE() + ReadUInt8())` (:1728) — added, not OR'd
  - `+8` uint16BE amount, `Math.Max((ushort)1, …)` so a server-sent 0 becomes 1 (:1729)
  - `+10` uint16BE x (:1730); `+12` uint16BE y (:1731)
  - `+14` one byte skipped and never read, only on CV_6017+ (:1733-1736) — the grid-index byte
  - `+15` (or `+14` pre-6017) uint32BE container serial (:1738)
  - `+19` (or `+18`) uint16BE hue (:1739)
  - all forwarded to `AddItemToContainer(serial, graphic, amount, x, y, hue, containerSerial)`
    (:1741 -> :6389)
- **Mutates** — `ItemHold.Clear()` when the held item is this serial and was already dropped
  (:6404); `World.RemoveMobile(serial, true)` if the "item" serial is in the mobile range (:6429 ->
  World.cs:689-718); `World.RemoveItem(item, true)` when an existing item is moving to a different
  container (:6436); `Item.Graphic` (:6440); AllowedToDraw / AnimIndex / UsedLayer / Direction /
  Layer via `Item.CheckGraphicChange` (:6441 -> Item.cs:607-646); `Item.Amount` (:6442); `Item.Hue`
  via `Entity.FixHue` — masks 0x3FFF, clamps >= 0x0BB8 to 1, re-ORs 0xC000 (:6443 ->
  Entity.cs:115-133); `Item.X`, `Item.Y` (:6444-6445); **`Item.Z` forced to 0** regardless of
  anything the server said (:6446); unlink from the old container and Container reassigned
  (:6452-6453 -> World.cs:617-653, which sets `Container = 0xFFFFFFFF` :647, nulls Next/Previous
  :650-651, RemoveFromTile :652 -> GameObject.cs:220-246); `container.PushToBack(item)` (:6456 ->
  LinkedObject.cs:57-78); `((Item)container).Opened = true` when the container had a matching gump
  (:6534); `_requestedGridLoot = 0` after a GridLootGump is spawned (:6521).
- **Creates** — Item via `World.GetOrCreateItem` (:6439 -> World.cs:559-581 -> `Item.Create` ->
  `Item._pool.GetOne()`, Item.cs:255-260); a destroyed entry under the same serial is removed and
  pooled first (World.cs:563-572). GridLootGump — `new GridLootGump(_requestedGridLoot)` +
  UIManager.Add (:6519-6520), only when GridLootType > 0, no existing gump, and `_requestedGridLoot`
  matches this container.
- **Destroys** — the previously-known item under the same serial when it lived in a different
  container: `World.RemoveItem(item, true)` (:6436); forceRemove -> Items.Remove + ReturnToPool
  (World.cs:679-684), recursively destroying its contents (World.cs:664-674) and removing its OPL
  entry (:676). A mobile filed under this serial: `World.RemoveMobile(serial, true)` (:6429).
  `Item.Destroy()` also disposes ContainerGump / GridContainer / SpellbookGump / MapGump /
  GridLootGump / BulletinBoardGump / SplitMenuGump for that serial if Opened (Item.cs:263-296).
- **Triggers** — `Send_BulletinBoardRequestMessageSummary(containerSerial, serial)` — an **outgoing**
  packet (:6481) when a BulletinBoardGump exists for the container; RequestUpdateContents on
  TradingGump for the container's secure trade box (:6465), PaperDollGump + ModernPaperdoll
  (:6469-6470 and again via RemoveItemFromContainer, World.cs:627-628), ContainerGump (:6537,
  World.cs:632), GridContainer (:6503, World.cs:634), NearbyLootGump (:6527, World.cs:637),
  SpellbookGump (:6488/:6537), GridLootGump (:6524), TradingGump for the container itself (:6542);
  `ContainerGump.CheckItemControlPosition(item)` (:6496).
- **Ignores / partial** —
  - Whole packet ignored if `!World.InGame` (:1722-1725).
  - Amount clamped up to 1 — a server-sent 0 is not honoured (:1729).
  - Z discarded entirely and forced to 0 (:6446); the packet has no Z field, and the item's previous
    Z is thrown away too.
  - The whole add is abandoned if `World.Get(containerSerial)` is null — logged "No container found"
    and return (:6417-6423). The item is not created and no request is sent for the missing container.
  - The old item is torn down only when `item.Container != containerSerial` AND
    (`container.Graphic != 0x2006` OR `item.Layer == Layer.Invalid`) (:6434). Same-container
    re-sends and corpse-equipment re-sends are left in place (comment at :6433: "prevent closing
    containers when changing facets").
  - Container reassignment likewise only when `item.Container != containerSerial` (:6450);
    otherwise the item keeps its linkage and is merely PushToBack'd again.
  - The 6017 grid-index byte is skipped only on CV_6017+ (:1733-1736).
  - ItemHold cleared only when `Dropped` is set; a still-held un-dropped item is left alone
    (:6401-6405).
  - GridLootGump auto-creation requires GridLootType > 0 AND `_requestedGridLoot == containerSerial`
    (:6507-6522); otherwise an existing gump is merely refreshed.

---

### 0x27 DenyMoveItem

- **Purpose** — Server refuses a pickup/drop; the client puts the held item back where it came from
  and shows an error.
- **Wire** — PacketsTable.cs:80 = 0x0002, reader at 1. `[1]` uint8 code — read at :1863, i.e.
  **after** every piece of state repair has already run. Nothing else is read from the wire; all
  restore data comes from `Client.Game.GameCursor.ItemHold`, not the packet. `p[0]` (the id) is
  passed to `ServerErrorMessages.GetError` at :1869.
- **Mutates** —
  - `World.ObjectToRemove = 0` (field World.cs:69) at :1761 when it equals `ItemHold.Serial` —
    cancels the pending deferred removal World.Update would otherwise perform (World.cs:294-332)
  - Branch A (Layer.Invalid + valid Container, :1771-1792) -> AddItemToContainer (:6389):
    `Graphic` :6440, CheckGraphicChange :6441, `Amount` :6442, FixHue(hue) :6443
    (Entity.cs:115-133 clamps `hue & 0x3FFF >= 0x0BB8` to 1), `X` :6444, `Y` :6445, `Z = 0` :6446,
    RemoveItemFromContainer + `Container = containerSerial` :6452-6453, `container.PushToBack(item)`
    :6456
  - Branch B (:1795-1838): `World.GetOrCreateItem` (World.cs:559) may insert a new Item into
    World.Items (World.cs:577); then `Graphic` :1799, `Hue` :1800 (**raw, not FixHue**), `Amount`
    :1801, `Flags` :1802, `Layer` :1803, `X` :1804, `Y` :1805, `Z` :1806, `CheckGraphicChange()`
    :1807 (Item.cs:607 writes AllowedToDraw, or for corpses AnimIndex=99 / UsedLayer / Direction /
    Layer, or for multis calls LoadMulti which touches World.HouseManager, Item.cs:342-346)
  - Branch B1, container is a mobile (:1813-1823): `World.RemoveItemFromContainer(item)` :1817 ->
    World.cs:617-653 (`Container = 0xFFFFFFFF` :647, Next/Previous nulled :650-651, RemoveFromTile
    :652 -> GameObject.cs:220 hands the chunk cell to `TNext ?? TPrevious`); then
    `container.PushToBack(item)` :1818 (LinkedObject.cs:57); `item.Container = container.Serial` :1819
  - Branch B2, container exists but is not a mobile (:1826-1828): `World.RemoveItem(item, true)` ->
    World.cs:655-687 (recursive removal :667-674, `OPL.Remove` :676, `Destroy()` :677,
    `Items.Remove` :681, `ReturnToPool()` :683, Item.cs:321-331)
  - Branch B3, container null (:1833-1837): RemoveItemFromContainer :1835 then
    `item.SetInWorldTile(item.X, item.Y, item.Z)` :1837 -> GameObject.cs:265-272 (X/Y/Z,
    UpdateScreenPosition, AddToTile -> Chunk.AddGameObject)
  - `ItemHold.Clear()` at :1851 -> ItemHold.cs:118-142 zeroes Serial/Container/Amount/MouseOffset,
    sets X=Y=0xFFFF, Graphic=DisplayedGraphic=0xFFFF, Hue=0xFFFF, Layer=Invalid, Flags=None,
    Dropped=Enabled=UpdatedInWorld=false
  - World.Items is mutated (insert World.cs:577 / remove :681) — the same Dictionary World.Update
    walks with foreach at World.cs:424; parsing at GameController.cs:478 runs before Scene.Update at
    :486, so this lands ahead of the sweep.
- **Creates** — possibly one Item from the pool via `Item.Create` (Item.cs:255-261) through
  `World.GetOrCreateItem` (World.cs:576) — branch A (:6439) and branch B (:1795). GridLootGump can
  be created inside AddItemToContainer at :6519 when GridLootType > 0 and `_requestedGridLoot`
  matches.
- **Destroys** — branch B2: the item is Destroy()ed and pooled (World.cs:677, :683); `Item.Destroy`
  (Item.cs:263-297) additionally disposes ContainerGump, GridContainer, SpellbookGump, MapGump,
  GridLootGump (if IsCorpse), BulletinBoardGump and SplitMenuGump — but only when `item.Opened` was
  true. `World.GetOrCreateItem` evicts and pools a stale destroyed entry under the same serial
  (World.cs:563-572). SplitMenuGump for `ItemHold.Serial` disposed unconditionally on the repair
  path (:1849).
- **Triggers** — `ContainerGump.RequestUpdateContents` for `ItemHold.Container` (:1789-1791);
  PaperDollGump and ModernPaperdoll RequestUpdateContents (:1821, :1822);
  `MessageManager.HandleMessage` with `ServerErrorMessages.GetError(0x27, code)`, hue 0x03b2,
  MessageType.System, font 3, TextType.SYSTEM (:1867-1875); via AddItemToContainer:
  `Send_BulletinBoardRequestMessageSummary` (:6481), RequestUpdateContents on the trading gump
  (:6465, :6542), PaperDollGump/ModernPaperdoll (:6469-6470), GridContainer (:6503), GridLootGump
  (:6524), NearbyLootGump (:6527), and `CheckItemControlPosition` (:6496); Console.WriteLine traces
  at :1777, :1815, :1826, :1833, :1900 and :6403.
- **Ignores / partial** —
  - Returns immediately if `!World.InGame` (:1746).
  - The entire state repair is gated on `ItemHold.Enabled || (ItemHold.Dropped && (firstItem == null
    || !firstItem.AllowedToDraw))` (:1753-1757). If false, the handler only logs "There was a problem
    with ItemHold object" (:1855) and still shows the error text — the denial is otherwise not acted on.
  - Skipped unless `SerialHelper.IsValid(ItemHold.Serial) && ItemHold.Graphic != 0xFFFF`
    (:1764-1767); otherwise Log.Error at :1844 and only `ItemHold.Clear()` runs.
  - Skipped again when `ItemHold.UpdatedInWorld` is true (:1769) — the client assumes the world copy
    is already correct.
  - Branch A only when `ItemHold.Layer == Layer.Invalid` AND the container serial is valid
    (:1771-1774); the comment at :1776 records that the client is deferring to a follow-up 0x25.
  - AddItemToContainer aborts with only a Log.Warn if the container is not in the world
    (:6417-6422) — the restore silently does not happen.
  - AddItemToContainer force-removes the item first only if `item.Container != containerSerial &&
    (container.Graphic != 0x2006 || item.Layer == Layer.Invalid)` (:6434), clears ItemHold early when
    the serials match and Dropped is set (:6399-6405, making the later Clear() a no-op), and
    force-removes a mobile if the serial is in the mobile range, logging "adds mobile as Item"
    (:6427-6431).
  - The error text is suppressed entirely when `code >= 5` (:1865). `GetError` would itself clamp
    code >= 5 to 4 for 0x27 (ServerErrorMessages.cs:112-118), so that clamp is unreachable from here.
  - `Item.Z` is forced to 0 by AddItemToContainer (:6446) but preserved from ItemHold in branch B
    (:1806).

---

### 0x28 EndDraggingItem

- **Purpose** — Server tells the client to stop the drag it had in progress.
- **Wire** — PacketsTable.cs:81 fixed length 5, packetOffset = 1 (:215). **No wire field is read at
  all**; the 4-byte body is ignored.
- **Mutates** — `ItemHold.Enabled = false` (:1886; the setter also resets IsFixedPosition, FixedX,
  FixedY, IgnoreFixedPosition — ItemHold.cs:76-82); `ItemHold.Dropped = false` (:1887).
- **Creates** — nothing.
- **Destroys** — nothing. `ItemHold.Clear()` (ItemHold.cs:118-142) is not called, so Serial,
  Graphic, Hue, Amount, Container and Layer keep their previous values.
- **Triggers** — nothing.
- **Ignores / partial** — returns without touching anything when `!World.InGame`, i.e. Player null or
  Map null (:1881-1884, World.cs:163); the body is never parsed, so whatever object the server named
  is ignored — the flags are cleared for whatever the cursor currently holds.

---

### 0x29 DropItemAccepted

- **Purpose** — Server confirms the drop the client attempted; the cursor may let go of the held item.
- **Wire** — PacketsTable.cs:82 fixed length 0x0001 — nothing but the id byte; the reader is seeked
  to offset 1, already at end. The handler reads no fields at all.
- **Mutates** — `ItemHold.Enabled = false` (:1897; setter ItemHold.cs:69-76 also clears
  IsFixedPosition, FixedX, FixedY, IgnoreFixedPosition); `ItemHold.Dropped = false` (:1898).
- **Creates / Destroys** — nothing.
- **Triggers** — `Console.WriteLine("PACKET - ITEM DROP OK!")` (:1900).
- **Ignores / partial** — returns without touching ItemHold when `!World.InGame` (:1892-1895); the
  confirmation is then dropped on the floor. `ItemHold.Clear()` is not called — Serial, Graphic,
  Container, Layer and the rest of the held-item snapshot survive.

---

### 0x2C DeathScreen

- **Purpose** — Server tells the client the player has died (or is being resurrected).
- **Wire** — fixed length 2 (PacketsTable 0x2C = 0x0002), reader at 1. `[1]` uint8 action (:1906).
- **Mutates** — GameScene Weather reset: Type=0, Count=CurrentCount=Temperature=0, Wind=0,
  _windTimer=_timer=0, CurrentWeather=null (:1910 -> Weather.cs:85-89);
  `World.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER (1500)` (:1916; field
  PlayerMobile.cs:103, read by GameScene.cs:1667);
  `World.WMapManager._corpse = new WMapEntity{X=Player.X, Y=Player.Y, HP=0, Map=World.Map.Index,
  LastUpdate=Time.Ticks+300000, IsGuild=false, Name="Your Corpse"}` (:1920-1929; field
  WorldMapEntityManager.cs:83).
- **Creates** — WMapEntity for the player's corpse marker (:1920).
- **Destroys** — nothing.
- **Triggers** — `Audio.PlayMusic(Audio.DeathMusicIndex, true)` (:1912);
  `GameActions.RequestWarMode(false)` (:1919 -> `Audio.StopWarMusic` GameActions.cs:74 and
  `Send_ChangeWarMode(false)` GameActions.cs:78);
  `EventSink.InvokeOnPlayerDeath(World.Player, World.Player.Serial)` (:1931).
- **Ignores / partial** —
  - Everything is skipped when `action == 1` (:1908); only `action != 1` is treated as death.
  - DeathScreenTimer written only when `ProfileManager.CurrentProfile.EnableDeathScreen` (:1914);
    the profile is dereferenced with no null guard.
  - No `World.InGame` / `World.Player` null guard anywhere: `World.Player.X/Y` (:1922-1923) and
    `World.Map.Index` (:1925) are read unguarded.
  - RequestWarMode's audio side only fires when `!World.Player.IsDead` (GameActions.cs:66); at the
    moment this handler runs the dead flag may not yet have arrived via 0x20/0x78, so which branch
    is taken depends on packet order.
  - `_corpse.LastUpdate` is set 5 minutes into the future, so WorldMapEntityManager.cs:196 (clears
    `_corpse` when `LastUpdate < Ticks-1000`) will not expire it for that long.

---

### 0x2D MobileAttributes

- **Purpose** — Bulk stat update for one entity: hits/mana/stamina current and max.
- **Wire** — fixed 17 bytes (PacketsTable.cs:86 = 0x11), reader at 1. `[1..4]` uint32BE serial
  (:1937); `[5..6]` -> `entity.HitsMax` (:1946); `[7..8]` -> `entity.Hits` (:1947); `[9..10]` ->
  `mobile.ManaMax` (:1963); `[11..12]` -> `mobile.Mana` (:1964); `[13..14]` -> `mobile.StaminaMax`
  (:1965); `[15..16]` -> `mobile.Stamina` (:1966). On the item path the reader is abandoned at
  offset 9, which is harmless because the reader is per-packet (:190).
- **Mutates** — `entity.HitsMax` (:1946; Entity.cs:81 plain field, no event); `entity.Hits` (:1947;
  setter Entity.cs:67-80 writes the backing field and, only when `this is PlayerMobile`, builds a
  PlayerStatChangedArgs from the OLD value and fires `EventSink.InvokeOnPlayerStatChange`,
  Entity.cs:73 / EventSink.cs:129); `entity.HitsRequest = Received` (:1951); `mobile.ManaMax`
  (:1963, Mobile.cs:236); `mobile.Mana` (:1964, Mobile.cs:235); `mobile.StaminaMax` (:1965,
  Mobile.cs:234); `mobile.Stamina` (:1966, Mobile.cs:233); window title text (:1973 ->
  TitleBarStatsManager.cs:26); `BandageManager.nextBandageTime` (BandageManager.cs:118 / 131) when
  the stat-change event leads to a heal; TargetManager auto-target state (BandageManager.cs:129
  `SetAutoTarget(World.Player.Serial, TargetType.Beneficial, CursorTarget.Object)`).
- **Creates** — nothing here; `Entity.Update` (Entity.cs:189-195) will later recompute the
  percentage bar text from these values, allocating RenderedText at Entity.cs:161 for an uncached
  percentage.
- **Destroys** — nothing.
- **Triggers** — `EventSink.OnPlayerStatChange -> BandageManager.OnPlayerStatChanged`
  (BandageManager.cs:39-45) -> OnHpChanged (:77-108) -> AttemptHeal (:110-135), which sends
  `GameActions.BandageSelf()` or `GameActions.DoubleClick(bandage.Serial)` after arming
  TargetManager auto-target — so a stat packet can emit outgoing packets from inside the handler.
  UoAssist window messages (:1970-1972 -> UoAssist.cs:73/78/83 -> PostMessage of STR_STATUS /
  DEX_STATUS / mana, UoAssist.cs:434-449). Window title update (:1973).
- **Ignores / partial** —
  - No `World.InGame` guard at all — this handler runs on serial lookup alone.
  - `World.Get(serial) == null` -> dropped before any read past the serial (:1941-1944); World.Get
    also returns null for a found-but-destroyed entity (World.cs:551-554).
  - HitsRequest is only advanced Pending -> Received; None stays None (:1949-1952).
  - Mana/stamina applied only when `SerialHelper.IsMobile(serial)` (serial < 0x40000000,
    SerialHelper.cs:47-50) (:1954). For an item serial the last 8 wire bytes are ignored entirely.
  - If the serial is in mobile range but the entity resolved to an Item (World.Get falls back across
    both dictionaries, World.cs:532-549), the `as Mobile` cast yields null and the handler returns
    (:1958-1961), leaving hits applied and mana/stam dropped.
  - UoAssist signals and the title bar update only fire for World.Player (:1968).
  - `TitleBarStatsManager.UpdateTitleBar` bails when the profile is null (TitleBarStatsManager.cs:11-14)
    or EnableTitleBarStats is false / World.Player null (:16-19).
  - The Hits event fires only for PlayerMobile (Entity.cs:69); every other mobile's hits change silently.
  - HitsMax is written BEFORE Hits (:1946 then :1947), so the subscriber at BandageManager.cs:87
    divides by the new max, not the old.
  - Real-time note: the Hits setter re-enters arbitrary subscriber code mid-handler, which can send
    packets and set TargetManager state while the rest of the 0x2D body has not yet applied
    mana/stamina.

---

### 0x2E EquipItem

- **Purpose** — Server places an item on a mobile's paperdoll layer.
- **Wire** — Registered PacketHandlers.cs:259. PacketsTable.cs:87 fixed 0x000F, reader at 1.
  `[1..4]` uint32BE serial (:1985); `[5..6]` uint16BE graphic and `[7]` int8 increment, summed and
  cast to ushort in one expression (:2003; left-to-right: base graphic first, then the signed delta);
  `[8]` uint8 layer (:2004); `[9..12]` uint32BE container serial (:2005); `[13..14]` uint16BE hue
  through `Item.FixHue` (:2006). No amount is on the wire; Amount is forced to 1 (:2007).
- **Mutates** — World.Items gains an entry for an unknown serial (World.cs:577); a stored but
  already-destroyed Item under that serial is removed (World.cs:565) and pooled (World.cs:569 ->
  Item.cs:326-330); `World.RemoveItemFromContainer`: old parent's child list spliced (World.cs:644 ->
  LinkedObject.cs:81-121), `Container = 0xFFFFFFFF` (:647), Next/Previous nulled (:650-651),
  RemoveFromTile rewrites the chunk cell and neighbour TNext/TPrevious (:652 -> GameObject.cs:231,
  236, 241); `Item.Graphic` (:2003 -> GameObject.cs:111-115); `Item.Layer` (:2004); `Item.Container`
  (:2005); `Item.Hue` via FixHue (Entity.cs:133, clamped: masked hue >= 0x0BB8 becomes 1,
  Entity.cs:121-124); `Item.Amount = 1` (:2007); container's child list gains the item
  (LinkedObject.PushToBack, LinkedObject.cs:57-79: Remove first at :64, append at :68 or :73-77);
  `Mobile.Mount = item` when layer == Mount (:2015 -> Mobile.cs:180);
  `PlayerMobile.Abilities[0]` and `[1]` rewritten (PlayerMobile.cs:321-322 and the fallback at
  :1393-1397, reached from :2033).
- **Creates** — an Item from the QueuedPool with ~30 fields reset by the pool action (`Item.Create`,
  Item.cs:255-261 and :54-106), only when World.Items has no live entry for the serial.
- **Destroys** — nothing is destroyed; an already-destroyed Item under the same serial is evicted and
  pooled (World.cs:565-569).
- **Triggers** — RequestUpdateContents on ContainerGump / PaperDollGump / ModernPaperdoll keyed on
  the **old** container serial (:1997-2000, read before Container is overwritten at :2005); the same
  on ContainerGump / GridContainer / NearbyLootGump / PaperDollGump / ModernPaperdoll from inside
  RemoveItemFromContainer (World.cs:627-637); PaperDollGump / ModernPaperdoll keyed on the **new**
  container (:2024-2025); up to two UseAbilityButtonGump instances (PlayerMobile.cs:1401-1415). No
  outgoing packet.
- **Ignores / partial** —
  - Returns if `!World.InGame` (:1980).
  - The old container is unlinked only when `item.Graphic != 0 && item.Layer != Layer.Backpack`
    (:1989); a freshly pooled Item has Graphic 0 (Item.cs:58) so the removal is skipped for new
    items, and an item genuinely on layer Backpack is never unlinked.
  - `ClearContainerAndRemoveItems` is commented out at :1991.
  - The old-container gump refresh only runs when `SerialHelper.IsValid(item.Container)` (:1995).
  - Layers 0x1A..0x1C (ShopBuyRestock..ShopSell, Layers.cs:63-65) take a deliberately empty branch
    (:2018-2021), and the `item.Clear()` there is commented out.
  - The new-container paperdoll refresh needs a valid container AND `layer < Mount (0x19)` (:2022);
    Mount, shop layers and Bank get no refresh.
  - `entity?.PushToBack` at :2011 — when the container serial is not present in World, Container is
    still written to that serial but the item is linked into nothing.
  - Mount assignment requires the container entity to be a Mobile (:2013).
  - Ability recalculation only when the container entity is reference-equal to World.Player and the
    layer is One/TwoHanded (:2028-2031).
  - The ItemHold reconciliation upstream performed is commented out (:2036-2041).
  - World.Items is mutated here (World.cs:577) between frames; the sweep enumerating that same
    dictionary by key runs later in World.Update (World.cs:424).

---

### 0x2F Swing

- **Purpose** — A melee swing between attacker and defender; used only to auto-face the player
  toward the last-attacked target.
- **Wire** — fixed 0x000A (PacketsTable.cs:88), reader at 1. `[1]` one byte skipped outright
  (`p.Skip(1)`, :2051 — the leading flag is never inspected); `[2..5]` uint32BE attacker serial
  (:2053); `[6..9]` uint32BE defender serial (:2060), read only after the attacker check passes.
- **Mutates** — nothing directly. All writes come from `World.Player.Walk` (PlayerMobile.cs:1674)
  called at :2091, and only on the narrow path in the conditions below:
  - `Walker.StepInfos[n].Sequence/Accepted/Running/OldDirection/Direction/Timer/X/Y/Z/NoRotation` —
    PlayerMobile.cs:1811-1821 (avoid path) / :1988-1998 (WalkNotAvoid path)
  - `Walker.StepsCount++` — PlayerMobile.cs:1823 / :2000
  - `Mobile.Steps.AddToBack(new Step{...})` — PlayerMobile.cs:1825 / :2002
  - `Walker.WalkSequence` wraps 0xFF -> 1 else ++ — PlayerMobile.cs:1841/1845 / :2020/2024
  - `Walker.UnacceptedPacketsCount++` — PlayerMobile.cs:1848 / :2027
  - `Walker.LastStepRequestTime = Time.Ticks + walkTime` — PlayerMobile.cs:1854 / :2050
  - `Mobile.LastStepTime = Time.Ticks` when the step stack was empty — PlayerMobile.cs:1801 / :1985
  - `AddToTile()` relink — PlayerMobile.cs:1850 / :2029 -> GameObject.cs:224/231/236/241/244-245 and
    Chunk.cs:178-180/285/289-291/338-352
  - `GetGroupForAnimation(this, 0, true)` — PlayerMobile.cs:1855 / :2051; `SetAnimation(0xFF)` when
    not already walking — :1798 / :1982
  - CloseBank: `bank.Opened = false` (PlayerMobile.cs:1520), `bank.Items = null` (:1512)
  - avoid path only: `World.Player.ClearSteps()` (PlayerMobile.cs:1726/1728 -> Mobile.cs:300-301)
    then SetInWorldTile (GameObject.cs:267-271)
- **Creates** — no entity; a `Step` struct is pushed onto `Mobile.Steps` (PlayerMobile.cs:1825/2002).
- **Destroys** — CloseBank calls `World.RemoveItem(first, true)` on every item in the open bank
  container (PlayerMobile.cs:1507), destroying those Items and returning them to the pool via the
  World.Update sweep (World.cs:407); the bank's ContainerGump and GridContainer are disposed
  (PlayerMobile.cs:1515/1517).
- **Triggers** — `NetClient.Socket.Send_WalkRequest(direction, WalkSequence, run,
  FastWalkStack.GetValue())` — PlayerMobile.cs:1837 / :2015; a server-reported swing can cause the
  client to emit a walk/turn request.
- **Ignores / partial** —
  - `!World.InGame` (World.cs:163) -> return, nothing read (:2046-2049).
  - `attackers != World.Player` -> return (:2055-2058). Swings where the player is the defender or an
    uninvolved observer are dropped entirely; the defender serial is not even read.
  - The auto-face only happens when ALL of: `TargetManager.LastAttack == defenders`,
    `World.Player.InWarMode`, `Walker.LastStepRequestTime + 2000ms < Time.Ticks`, and
    `World.Player.Steps.Count == 0` (:2064-2069). Otherwise the packet has no effect at all.
  - `World.Mobiles.Get(defenders) == null` -> nothing happens (:2071-2073).
  - `Pathfinder.CanWalk` (Pathfinder.cs:700, writes only its ref params) must succeed AND
    `World.Player.Direction` must already differ from the computed direction (:2086-2089).
  - Walk routes to WalkNotAvoid when `AutoAvoidObstacules` is off or `Pathfinder.AutoWalking` is on
    (PlayerMobile.cs:1676); otherwise the obstacle-avoiding branch may substitute a different
    direction (:1721-1729) or refuse (:1732).
  - Walk returns false with no state change if `Walker.WalkingFailed`, `LastStepRequestTime >
    Time.Ticks`, `StepsCount >= Constants.MAX_STEP_COUNT`, or (CV_60142+) `IsParalyzed`
    (PlayerMobile.cs:1685 / :1892).
  - `run` is forced false when SpeedMode >= CantRun, Stamina <= 1 while alive, or hidden with
    AlwaysRunUnlessHidden (PlayerMobile.cs:1692-1695 / :1899-1902).
  - Frame note: the Walk path mutates `Mobile.Steps` and relinks the player in `Chunk.Tiles`, both
    walked by the render/update sweep; CloseBank calls World.RemoveItem, condemning entries in the
    World.Items dictionary World.Update iterates at World.cs:424.

---

### 0x32 Unknown_0x32

- **Purpose** — Registered so the dispatcher consumes the packet; the handler body is empty.
- **Wire** — fixed length 0x0002 (PacketsTable.cs:91), body offset 1. The single payload byte is
  never read — the method body is `{ }` (:2097).
- **Mutates / Creates / Destroys / Triggers** — nothing.
- **Ignores / partial** — everything the server sends in this packet is ignored unconditionally.

---

### 0x38 Pathfinding

- **Purpose** — Server hands the client a destination and asks it to walk there itself.
- **Wire** — Registered PacketHandlers.cs:262. Fixed 0x0007, reader at 1. `[1..2]` uint16BE x
  (:2276); `[3..4]` uint16BE y (:2277); `[5..6]` uint16BE z (:2278) — read as **unsigned** and passed
  straight into an int parameter, with no sign extension and no truncation to sbyte anywhere on this
  path. All three go to `Pathfinder.WalkTo(x, y, z, 0)` at :2280 — the distance argument is hardcoded
  0, nothing on the wire supplies it.
- **Mutates** — Pathfinder statics, all inside WalkTo (`src/ClassicUO.Client/Game/Pathfinder.cs`):
  :1014 `_pointIndex = 0`, :1015 `_goalNode = null`, :1016 `_run = false`, :1017-1018 `_startPoint`
  = player position, :1019-1020 `_endPoint` = packet x/y, :1021 `_endPointZ` = packet z, :1022
  `_pathfindDistance = 0`, :1023 `AutoWalking = true`, then :1027 `_pointIndex = 1` on success or
  :1032 `AutoWalking = false` on failure. `CleanupPathfinding` (Pathfinder.cs:1072) empties
  `_openSet` (:1081), `_closedSet` (:1090), `_path` (:1092) and nulls `_goalNode` (:1093) before the
  search starts. `_run` may be set true at Pathfinder.cs:909 when the start node's goal cost exceeds
  14. `World.Player.Walker` state is written indirectly through `World.Player.Walk`
  (Pathfinder.cs:1053) when ProcessAutoWalk runs.
- **Creates** — PathNode objects from a pool: startNode via `PathNode.Get()` (Pathfinder.cs:891) plus
  one per expanded neighbour during FindPath; a resolved `_path` list on success (ReconstructPath,
  Pathfinder.cs:930).
- **Destroys** — PathNodes from any previous pathfind are returned to the pool by CleanupPathfinding:
  Pathfinder.cs:1078 `node.Return()` for the open set and :1086 `n.Value.Return()` for the closed
  set. This happens before the new search, so any node still referenced from the previous run is
  handed back mid-handler.
- **Triggers** — `EventSink.InvokeOnPathFinding(null, new Vector4(x,y,z,distance))`
  (Pathfinder.cs:1011), fired before any validation of the destination; `ProcessAutoWalk()`
  (Pathfinder.cs:1028) -> `World.Player.Walk(...)` (:1053), which emits outgoing movement (0x02)
  packets on this same call stack; `StopAutoWalk()` (:1055/:1060) if the first Walk fails or the
  index is out of range.
- **Ignores / partial** —
  - Entire packet dropped when `!World.InGame` (:2271-2274).
  - WalkTo refuses outright (returns false, nothing written) when `World.Player == null` or
    `World.Player.IsParalyzed` (Pathfinder.cs:1006-1009) — the destination is discarded. The stamina
    check on the same line is commented out.
  - If FindPath cannot reach the destination within PATHFINDER_MAX_NODES the client sets
    `AutoWalking = false` and never moves; the server is not told (Pathfinder.cs:1025-1033).
  - The search breaks out after maxNodes closed nodes (Pathfinder.cs:923-926), so a long path is
    partially or never applied.
  - The search loop is conditioned on AutoWalking (Pathfinder.cs:912), so anything clearing it during
    the search aborts it.

---

### 0x3A UpdateSkills

- **Purpose** — Character skill list: full list, a single skill delta, or a replacement of the
  skill-name table itself.
- **Wire** — variable length (PacketsTable 0x3A = -1), reader at 3.
  - `+3` uint8 type (:2106). Derived: `haveCap = (type != 0 && type <= 0x03) || type == 0xDF`
    (:2107); `isSingleUpdate = (type == 0xFF || type == 0xDF)` (:2108)
  - type 0xFE: `+4` uint16BE count (:2112); then per entry 1 byte bool haveButton (:2119), 1 byte
    nameLength (:2120), ASCII[nameLength] name (:2123)
  - all other types: loop while `p.Position < p.Length` — uint16BE id (:2170); uint16BE realVal
    (:2187); uint16BE baseVal (:2188); uint8 Lock (:2189); uint16BE cap **only if haveCap**
    (:2192-2195), otherwise cap is hardcoded 1000 (:2190)
  - id is decremented by 1 when type == 0 or type == 0x02 (:2182-2185)
  - loop exits early: Position >= Length right after reading id (:2172-2175); `id == 0 && type == 0`
    (:2177-2180); after one entry if isSingleUpdate (:2261-2264)
- **Mutates** — `SkillsLoader.Instance.Skills.Clear()` and `SortedSkills.Clear()` (:2114-2115, type
  0xFE only); `Skills.Add(new SkillEntry(...))` (:2122-2124); SortedSkills refilled and sorted by
  name, InvariantCulture (:2127-2131); `World.SkillsRequested = false` (:2149); `Skill.BaseFixed`
  (:2241, setter `src/ClassicUO.Client/Game/Data/Skill.cs:62`); `Skill.ValueFixed` (:2242,
  Skill.cs:60); `Skill.CapFixed` (:2243, Skill.cs:64); `Skill.Lock` (:2244, Skill.cs:58).
- **Creates** — SkillEntry objects into `SkillsLoader.Instance.Skills` (:2122, type 0xFE);
  `StandardSkillsGump { X = 100, Y = 100 }` via UIManager.Add (:2156); `SkillGumpAdvanced` via
  UIManager.Add (:2163).
- **Destroys** — nothing is destroyed or pooled. Type 0xFE clears the SkillsLoader name tables, but
  `World.Player.Skills` is a separate array sized at PlayerMobile construction
  (PlayerMobile.cs:53-59) and is not resized or rebuilt here.
- **Triggers** — `GameActions.Print` "Your skill in X has increased/decreased by N", hue 0x58,
  MessageType.System, font 3 (:2219-2233); `Skill.InvokeSkillBaseChanged(id)` (:2249 ->
  Skill.cs:82-85); `Skill.InvokeSkillValueChanged(id)` (:2251 -> Skill.cs:78-81);
  `Skill.InvokeSkillCapChanged(id)` (:2253 -> Skill.cs:86-89); `StandardSkillsGump.Update(id)`
  (:2256); `SkillGumpAdvanced.ForceUpdate()` (:2257). No outgoing packets.
- **Ignores / partial** —
  - Whole packet ignored if `!World.InGame` (:2101-2104).
  - `cap` is not read from the wire unless haveCap; it is silently set to 1000 (:2190) and written
    into `Skill.CapFixed` at :2243 — the server's cap is replaced with a fabricated value.
  - id off-by-one adjustment applies only to type 0 and 0x02 (:2182-2185); other types use the raw id.
  - An entry whose `id >= World.Player.Skills.Length` is fully parsed then silently discarded
    (:2197) — no growth, no log. An entry whose `Skills[id]` is null is discarded (:2201).
  - The skill-changed message is suppressed unless `change != 0`, `!float.IsNaN(change)`, profile
    non-null, ShowSkillsChangedMessage on, and either `ShowSkillsChangedDeltaValue <= 0` or the
    integer-division bucket of old vs new differs (:2208-2217).
  - The Base/Value/Cap change events fire **only** on isSingleUpdate (0xFF/0xDF); a full skill list
    rewrite at :2241-2244 fires nothing (:2246-2254).
  - Only the FIRST entry is applied when isSingleUpdate, even if the packet carries more (:2261-2264).
  - Gump auto-open only when `!isSingleUpdate` AND (type==1 || type==3 || World.SkillsRequested)
    (:2147), and only one of the two gump kinds per `StandardSkillsGump` profile flag
    (:2138-2145, :2152-2165).
  - `ProfileManager.CurrentProfile` is dereferenced with no null check at :2138 while being
    null-checked at :2206/:2211.

---

### 0x3B CloseVendorInterface

- **Purpose** — Close the vendor window for a given vendor.
- **Wire** — PacketsTable 0x3B = -1 (variable, PacketsTable.cs:100), reader at 3. `[3..6]` uint32BE
  serial (:2329). Nothing else is read; the rest of the variable-length body is ignored.
- **Mutates** — no World state at all; only the UI — the matched ShopGump is disposed (:2331), which
  unlinks it from UIManager.Gumps.
- **Creates** — nothing.
- **Destroys** — the ShopGump whose `LocalSerial == serial` (:2331). `UIManager.GetGump<T>` walks
  UIManager.Gumps from Last backwards and skips already-disposed controls (UIManager.cs:328-336), so
  a second 0x3B for the same vendor finds nothing.
- **Triggers** — none outgoing.
- **Ignores / partial** —
  - Returns if `!World.InGame` (:2324).
  - Only `ShopGump` is disposed. `ModernShopGump` — which SellList creates at :3648 when
    `UseModernShopGump` is set — is not looked up and is left open.
  - Only the first (topmost) matching ShopGump is disposed; if two exist for one serial the older one
    survives.

---

### 0x3C UpdateContainedItems

- **Purpose** — Full contents of a container: one record per contained item, container serial
  repeated on every record.
- **Wire** — PacketsTable.cs:101 variable, packetOffset 3.
  - `+3` uint16BE count (:2290) — number of records
  - per record: uint32BE serial (:2294)
  - per record: uint16BE graphic immediately summed with uint8 graphicInc as
    `(ushort)(ReadUInt16BE() + ReadUInt8())` (:2295) — folded in here with **no** 0x2006 exemption,
    unlike :6649
  - per record: uint16BE amount, clamped with `Math.Max(..., 1)` (:2296)
  - per record: uint16BE x (:2297), uint16BE y (:2298)
  - per record: one byte skipped on CV_6017+ (:2300-2303) — the grid/slot index, discarded
  - per record: uint32BE containerSerial (:2305); uint16BE hue (:2306)
- **Mutates** — for record i==0 only: `ClearContainerAndRemoveItems` on `World.Get(containerSerial)`
  (:2314) -> `container.Items` rewritten to new_first or null (:6914). Then AddItemToContainer
  (:2318 -> :6389): `Graphic` (:6440), CheckGraphicChange (:6441 -> Item.cs:607), `Amount` (:6442),
  Hue via FixHue (:6443 -> Entity.cs:133), `X` (:6444), `Y` (:6445), **`Z` forced to 0** (:6446);
  `item.Container = containerSerial` (:6453) only after `World.RemoveItemFromContainer` (:6452);
  `container.PushToBack(item)` (:6456 -> LinkedObject.cs:57-79); `((Item)container).Opened = true`
  when a gump for the container exists (:6534); World.Items entries added by GetOrCreateItem (:6439 ->
  World.cs:577); RemoveItemFromContainer writes `Container = 0xFFFFFFFF`, nulls Next/Previous and
  calls RemoveFromTile (World.cs:647-652).
- **Creates** — Item objects from the pool via `World.GetOrCreateItem` for every record (:6439 ->
  World.cs:576, Item.cs:255); GridLootGump constructed and added when GridLootType > 0, no gump
  exists, and `_requestedGridLoot` matches (:6519-6521).
- **Destroys** — ClearContainerAndRemoveItems -> `World.RemoveItem(it, true)` per child (:6908 ->
  World.cs:655): recursive removal of the child's own contents (:667-674), `OPL.Remove` (:676),
  `Destroy()` (:677), `Items.Remove` + `ReturnToPool()` (:681-683); `World.RemoveMobile(serial, true)`
  when a record's serial falls in the mobile range (:6429 -> World.cs:689-717) — the mobile and
  everything it carries are destroyed and pooled; `World.RemoveItem(item, true)` at :6436 for an item
  that already exists under a different container; GetOrCreateItem recycles a destroyed entry
  (World.cs:565-569); `Item.Destroy` disposes ContainerGump, GridContainer, SpellbookGump, MapGump,
  GridLootGump, BulletinBoardGump and SplitMenuGump for that serial when Opened (Item.cs:274-291).
- **Triggers** — `ItemHold.Clear()` when the dropped item is the one arriving (:6404 ->
  ItemHold.cs:118); TradingGump.RequestUpdateContents when the container mobile has a secure trade
  box (:6465), otherwise PaperDollGump + ModernPaperdoll (:6469-6470);
  `Socket.Send_BulletinBoardRequestMessageSummary` when a BulletinBoardGump is open for the container
  (:6481); `ContainerGump.CheckItemControlPosition` (:6496), GridContainer.RequestUpdateContents
  (:6503), GridLootGump (:6524), NearbyLootGump (:6527), the found gump (:6537), and
  `UIManager.GetTradingGump(containerSerial).RequestUpdateContents` (:6542); RemoveItemFromContainer
  additionally requests updates on PaperDollGump/ModernPaperdoll or
  ContainerGump/GridContainer/NearbyLootGump for the old container (World.cs:625-638).
- **Ignores / partial** —
  - Returns before reading anything when `!World.InGame` (:2285-2288).
  - The container is cleared **only for record 0** (:2308-2316); if the packet carries records for
    more than one container, the later containers keep their previous contents.
  - Clearing is skipped entirely when `World.Get(containerSerial)` is null (:2312) — the items are
    still added afterwards, and AddItemToContainer then logs "No container found" and returns
    (:6417-6423), so those records are dropped.
  - ClearContainerAndRemoveItems with `remove_unequipped = true` (container.Graphic == 0x2006) keeps
    every child whose `Layer != 0` and only re-heads the list at the first survivor (:6899-6905,
    :6914); the survivors' Previous pointers are left as they were. It returns immediately for a null
    or empty container (:6886-6889).
  - An existing item is only torn out when `item.Container != containerSerial` AND
    (`container.Graphic != 0x2006` OR `item.Layer == Layer.Invalid`) (:6434) — the guard keeps
    containers from closing on a facet change. RemoveItemFromContainer + Container reassignment is
    skipped when the item is already in that container (:6450).
  - amount clamped to a minimum of 1 (:2296); z forced to 0 rather than taken from the wire (:6446);
    the CV_6017 grid index byte is read and thrown away (:2302).
  - GridLootGump only created when GridLootType > 0 and `_requestedGridLoot` equals this container
    (:6507-6517).
  - Frame note: each record can destroy and pool entities and add new ones to World.Items in the same
    loop iteration, while World.Update's item sweep (World.cs:424-477) walks that dictionary by key
    and returns condemned objects to the pool at World.cs:467-473.

---

### 0x4E PersonalLightLevel

- **Purpose** — Sets the light level radiating from a specific mobile (the player).
- **Wire** — PacketsTable.cs:119 fixed 0x0006, reader at 1. `[1..4]` uint32BE serial, compared
  directly against World.Player (:2341); `[5]` uint8 level (:2343).
- **Mutates** — `World.Light.RealPersonal = level` (:2350; setter IsometricLight.cs:78-79 assigns
  `_realPersonal` and calls Recalculate()); `World.Light.Personal = level` (:2354; setter
  IsometricLight.cs:48-49 assigns `_personal` and calls Recalculate()).
- **Creates / Destroys** — nothing.
- **Triggers** — `IsometricLight.Recalculate()` recomputes IsometricLevel (IsometricLight.cs:49, 79).
- **Ignores / partial** —
  - Whole packet ignored when `!World.InGame` (:2336-2339).
  - Whole packet ignored when the serial is not the player's (:2341); the level byte is never even
    read for another mobile.
  - level clamped to 0x1E when greater (:2345-2348) — a server value above 30 is silently reduced.
  - `World.Light.Personal` is left alone when `UseCustomLightLevel` is set (:2352); only RealPersonal
    records what the server said.
  - `ProfileManager.CurrentProfile` is dereferenced at :2352 without a null check.

---

### 0x4F LightLevel

- **Purpose** — Sets the global (overall) light level.
- **Wire** — fixed length 2 (PacketsTable 0x4F = 0x0002), reader at 1. `[1]` uint8 level (:2366).
- **Mutates** — `World.Light.RealOverall = level` (:2373 -> `IsometricLight._realOveall`
  IsometricLight.cs:87, then Recalculate() -> IsometricLevel :96-101); `World.Light.Overall = level`
  or `min(level, Profile.LightLevel)` (:2380-2383 -> `_overall` IsometricLight.cs:58 then
  Recalculate()).
- **Creates / Destroys** — nothing.
- **Triggers** — nothing: no packets, no gumps.
- **Ignores / partial** —
  - Returns if `!World.InGame` (:2361); the level is dropped, not queued.
  - Clamp: `level > 0x1E` is forced to 0x1E (:2368-2371) **before** anything is written, so
    RealOverall never records what the server actually sent above 30.
  - Partial apply: `World.Light.Overall` is written only when `!UseCustomLightLevel` OR
    `LightLevelType == 1` (:2375-2378). With UseCustomLightLevel on and LightLevelType != 1 the
    server's level reaches RealOverall only and the rendered Overall keeps the user's value.
  - When `LightLevelType == 1` the value is further clamped to `min(level, Profile.LightLevel)`
    (:2382) — the server can only ever make it darker than the user's ceiling.
  - `ProfileManager.CurrentProfile` dereferenced without a null check (:2376).

---

### 0x53 ReceiveLoginRejection

- **Purpose** — Server rejects the login / reports a connection error; the login screen shows the
  matching message.
- **Wire** — Registered for 0x82 (:354), 0x85 (:355) and 0x53 (:356), handler at :6374. PacketsTable
  0x53 = 0x0002 (PacketsTable.cs:124), reader at 1. `[1]` uint8 code, read inside
  `LoginScene.HandleErrorCode` (LoginScene.cs:724); the handler itself reads nothing and forwards the
  reader by ref (:6385). `p[0]` (the packet id) selects the error table inside
  `ServerErrorMessages.GetError` (LoginScene.cs:726 -> ServerErrorMessages.cs:91).
- **Mutates** — `LoginScene.PopupMessage = ServerErrorMessages.GetError(p[0], code)`
  (LoginScene.cs:726; property LoginScene.cs:86); `LoginScene.CurrentLoginStep =
  LoginSteps.PopUpMessage` (LoginScene.cs:727; property LoginScene.cs:82). No World state whatsoever.
- **Creates** — nothing directly. `LoginScene.Update` reacts to the changed step and rebuilds its
  gump (LoginScene.cs:190-199, 255-301), consuming and nulling PopupMessage at :297.
- **Destroys** — nothing.
- **Triggers** — `ClilocLoader.Instance.GetString` for the message text (ServerErrorMessages.cs:101/
  111/120/129).
- **Ignores / partial** —
  - Returns and applies nothing if `World.InGame` (:6376) — Player != null && Map != null
    (World.cs:163). A mid-game 0x53/0x82/0x85 is dropped.
  - Returns if `Client.Game.GetScene<LoginScene>()` is null (:6383) — the code byte is never read.
  - The code is clamped inside GetError per packet id: 0x53 clamps code >= 10 to 9
    (ServerErrorMessages.cs:94-97); 0x85 clamps >= 6 to 5 (:104-107); 0x82 clamps >= 9 to 8
    (:123-126); an unrecognised id returns `string.Empty` (:132).
  - The discriminator is always the packet id byte: StackDataReader's indexer always returns
    `_data[0]` regardless of the index passed (StackDataReader.cs:39).
  - `LoginScene.Update` only shows the popup when PopupMessage is non-empty (LoginScene.cs:293) — an
    empty GetError result leaves the step changed with no visible message.

---

### 0x54 PlaySoundEffect

- **Purpose** — Play a sound effect at a world location.
- **Wire** — fixed 12 bytes (PacketsTable.cs:125 = 0x0C), reader at 1. `[1]` 1 byte skipped
  (`p.Skip(1)`, :2394 — the mode/flags byte is discarded); `[2..3]` uint16BE index (:2396) — the
  sound id actually used; `[4..5]` uint16BE audio (:2397) — read into a local and never used;
  `[6..7]` uint16BE x (:2398); `[8..9]` uint16BE y (:2399); `[10..11]` uint16BE read then cast to
  short -> z (:2400) — read as unsigned, reinterpreted, and never used.
- **Mutates** — `UOSound.X = x` (AudioManager.cs:191); `UOSound.Y = y` (:192);
  `UOSound.CalculateByDistance = true` (:193); `AudioManager._currentSounds.AddLast(sound)` (:195);
  playback state inside `UOSound.Play(Time.Ticks, volume, distanceFactor)` (:189).
- **Creates** — a UOSound fetched/loaded via `Client.Game.Sounds.GetSound(index)`
  (AudioManager.cs:187) and linked into `_currentSounds`.
- **Destroys** — nothing.
- **Triggers** — audio playback only. No packets, no gumps.
- **Ignores / partial** —
  - Dropped entirely when `World.Player == null` (:2389-2392).
  - Dropped when `!_canReproduceAudio` or `!World.InGame` (AudioManager.cs:153-156).
  - Volume forced to 0 when distance > `World.ClientViewRange` (AudioManager.cs:172-175) — the sound
    is still fetched and queued, just silent.
  - Returns without playing if the computed volume falls outside [-1, 1] (AudioManager.cs:177-180).
  - Volume forced to 0 when CurrentProfile is null, EnableSound is off, or the window is inactive and
    ReproduceSoundsInBackground is off (AudioManager.cs:182-185).
  - Nothing happens if `GetSound(index)` returns null or `Play()` returns false (AudioManager.cs:189).
  - The z coordinate is parsed but never fed to the audio system; distance is 2D only
    (AudioManager.cs:158-160, max of |dx|,|dy| against the player's X/Y).
  - The second uint16 (`audio`, commonly volume/repeat) is parsed and thrown away — only `index`
    reaches the mixer.

---

### 0x55 LoginComplete

- **Purpose** — The character is fully in the world; the client switches from LoginScene to GameScene
  and does its post-login handshake.
- **Wire** — fixed length 1; AnalyzePacket seeks to offset 1 (:191), already end-of-packet. **No wire
  field is read at all** — the handler never touches `p`.
- **Mutates** — `World.ClientViewRange = clamp(Settings.GlobalSettings.ClientViewRange,
  Constants.MIN_VIEW_RANGE=5, Constants.MAX_VIEW_RANGE=40)` (:2468; this fork's MAX_VIEW_RANGE is 40,
  Constants.cs:117); `GameController.Scene = new GameScene` (GameController.cs:306);
  `GameController.drawScene = Scene.IsLoaded` (GameController.cs:310/312); HouseDiagnostics buffered
  log flushed to disk (GameController.cs:305); `GlobalActionCooldown.nextActionTime = Time.Ticks +
  Profile.MoveMultiObjectDelay` (GlobalActionCooldown.cs:15); `World.Player.HitsRequest =
  HitsRequestStatus.Pending` via `GameActions.RequestMobileStatus` (GameActions.cs:988); the UIManager
  gump list gains every gump read from gumps.xml (:2488); `UIManager.SavePosition(serverSerial,
  Point)` for each saved gump carrying serverSerial (Profile.cs:908); SkillsGroupManager static state
  loaded (Profile.cs:865).
- **Creates** — GameScene instance (:2451); all Gump objects deserialized from
  `<profile>/gumps.xml` (Profile.ReadGumps, Profile.cs:859+, added at :2488).
- **Destroys** — the previous Scene (LoginScene) is disposed (GameController.cs:302).
- **Triggers** — `Send_StatusRequest(player)` via GameActions.RequestMobileStatus
  (GameActions.cs:997); `Send_OpenChat("")` (:2455); `Send_SkillsRequest(player)` (:2457);
  `Send_ClientType()` (:2461); `Send_ClientViewRange(World.ClientViewRange)` (:2473).
- **Ignores / partial** —
  - Entire handler is a no-op unless `World.Player != null` AND `Client.Game.Scene is LoginScene`
    (:2449). A 0x55 arriving while already in GameScene is silently discarded.
  - `Send_ClientType` only when Client.Version >= CV_306E (:2459).
  - The view-range block only runs when Client.Version >= CV_305D (:2464); on older versions
    World.ClientViewRange keeps its default and the server is never told.
  - The requested view range is clamped at both ends before being sent (:2468-2471).
  - Gump restore skipped if ReadGumps returns null (:2484).
  - `ProfileManager.CurrentProfile` is dereferenced unguarded at :2480.
  - Frame context: runs inside ProcessNetworkPackets (GameController.cs:478), which drains up to
    MAX_PACKETS_PER_FRAME=25 buffers (GameController.cs:136-146). SetScene disposes the live scene
    while the remaining queued packets of the same frame are still to be dispatched.

---

### 0x56 MapData

- **Purpose** — Manipulate the pins / edit state of an open map (treasure or city map) gump.
- **Wire** — Registered PacketHandlers.cs:269. PacketsTable.cs:127 fixed 0x000B, reader at 1.
  `[1..4]` uint32BE serial (:2501); `[5]` uint8 MapMessageType (:2507; MapMessageType.cs:37-43:
  Add=1, Insert, Move, Remove, Clear, Edit, EditResponse). Add: `[6]` skipped (:2510), `[7..8]`
  uint16BE x (:2512), `[9..10]` uint16BE y (:2513). EditResponse: `[6]` uint8 plot state (:2535).
  For every other message type no further byte is read.
- **Mutates** — `MapGump._container` list and the gump child list gain a PinControl
  (MapGump.cs:199-200); `MapGump.mapX / mapY / foundMapLoc` (MapGump.cs:217-222) and the hit-box
  tooltip text (MapGump.cs:224); `MapGump.PlotState` and the IsVisible/IsEnabled of `_buttons[0..2]`
  (MapGump.cs:240-246). No World, Item, Mobile or Chunk state is touched at all.
- **Creates** — PinControl (MapGump.cs:195), numbered from `_container.Count + 1` (MapGump.cs:198).
- **Destroys** — MapMessageType.Clear disposes every PinControl in `_container` and empties the list
  (MapGump.cs:232, 235).
- **Triggers** — nothing.
- **Ignores / partial** —
  - Returns if `!World.InGame` (:2496).
  - If UIManager has no MapGump whose LocalSerial matches, the message type byte is never even read
    and the whole packet is dropped silently (:2503-2505).
  - Insert, Move, Remove and Edit are empty cases (:2519-2532) — the server's insert/move/remove
    instructions are discarded.
  - Add throws away one byte the server sends before the coordinates (:2510).
  - Pin numbering comes from the local container count, not from anything on the wire (MapGump.cs:198).
  - The estimated-world-location computation runs only for the first pin ever added (`foundMapLoc`
    guard, MapGump.cs:201, 222).
  - The debug print at MapGump.cs:215 is gated on CUOEnviroment.Debug.

---

### 0x5B SetTime

- **Purpose** — Server sets the in-game clock (hour/minute/second).
- **Wire** — PacketsTable.cs:132 fixed length 0x0004, so 3 body bytes (hour, minute, second) are
  dequeued by ParsePackets and handed to the handler. The handler body (:2542) is empty — no field is
  read from the reader at all.
- **Mutates / Creates / Destroys / Triggers** — nothing.
- **Ignores / partial** — the entire packet is unconditionally discarded (:2542); the client keeps its
  own clock and the server's time is never applied. Because the packet is still registered (:270) it
  is dequeued and length-consumed correctly, and logged by PacketLogger/HouseDiagnostics
  (:142-143) — it just has no effect.

---

## Cross-cutting: threading and re-entrancy

- **Dispatch order.** `GameController.Update` (GameController.cs:466) calls `ProcessNetworkPackets()`
  at :478, **before** `Scene.Update()` at :486. `GameScene.Update` (GameScene.cs:913) is what calls
  `World.Update()`. So on the main thread a handler is not literally interleaved with the sweep — up
  to `MAX_PACKETS_PER_FRAME = 25` packets (GameController.cs:136, 140-146) are drained per frame ahead
  of it.
- **Reader setup.** `AnalyzePacket` constructs the StackDataReader over the shared `_readingBuffer`
  and Seek()s it to the header offset (PacketHandlers.cs:181-195, 190-191).
- **Plugin injection.** Plugins can inject packets from other threads via
  `PacketHandlers.Handler.Append(..., true)` (Plugin.cs:757, 779); those land in `_pluginsBuffer` and
  are still drained on the main thread under `lock (stream)` (PacketHandlers.cs:102).
- **Shared collections.** Handlers mutate the same dictionaries `World.Update` walks by key:
  World.Mobiles (World.cs:350-394) and World.Items (World.cs:424-459), both iterated as
  `foreach (KeyValuePair<...>)` with removals deferred into `_toRemove` and applied after the loop
  (World.cs:396-412, 461-477). `World.GetOrCreateItem` / `GetOrCreateMobile` can Items.Remove /
  Mobiles.Remove and ReturnToPool mid-handler (World.cs:563-572, 587-596); doing that during the
  sweep's foreach would invalidate the enumerator — it is only safe because handlers run before the
  sweep in the same frame.
- **Pooling contract.** Destroy must not return objects to the pool, precisely because the dictionary
  entry outlives Destroy until the sweep — see the comments at Item.cs:299-331 and Mobile.cs:1147-1160,
  and the by-key sweep notes at World.cs:341-349 and 414-423.
- **Deferred outgoing work.** Queued by handlers and flushed once per frame from GameScene.Update:
  `PacketHandlers.SendMegaClilocRequests()` (GameScene.cs:883 -> PacketHandlers.cs:359-391) drains both
  `Handler._clilocRequests` (filled by 0xDC at :5679) and `Handler._customHouseRequests` (filled by
  0xBF sub-0x1D at :4869).
- **Gump-list iteration.** `Control.Dispose` only sets IsDisposed (Control.cs:1111); the LinkedList
  removal is done later by UIManager.Update (UIManager.cs:429/455/636/642/675/687), so iterating
  UIManager.Gumps while disposing members does not invalidate the enumeration.
- **Linked-object walks.** Child-item wipes (e.g. :3100-3113 and ClearContainerAndRemoveItems
  :6891-6914) save `next` before each removal, because `World.RemoveItem` unlinks the current node.
