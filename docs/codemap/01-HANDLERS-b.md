# Handler reference — server→client packets 0x60–0xAF

Scope: 39 packets. All line references are into `/home/user/TazUO-Holiday-Edition/src/`.
Unqualified `PacketHandlers.cs` = `ClassicUO.Client/Network/PacketHandlers.cs`.
Length column source is `ClassicUO.Client/Network/PacketsTable.cs`; a length of `-1` means
variable, so `GetPacketInfo` sets `packetOffset = 3` (PacketHandlers.cs:224-228), otherwise 1.

---

### 0x65 SetWeather

**Purpose** — Set weather type, particle count and temperature for the current region.

**Wire** — fixed 0x0004 (PacketsTable.cs:142), body offset 1.
- `[1]` uint8 → `WeatherType type` — PacketHandlers.cs:2554
- `[2]` uint8 `count` — PacketHandlers.cs:2558, read **only** when `weather.CurrentWeather != type`
- `[3]` uint8 `temp` — PacketHandlers.cs:2559, same condition

**Mutates**
- `Weather.Reset()` zeroes Type, Count, CurrentCount, Temperature, Wind, `_windTimer`, `_timer`, nulls `CurrentWeather` — Game/Weather.cs:83-90, called at Weather.cs:99
- `Weather.Type` — Weather.cs:101; `Count` — Weather.cs:102; `Temperature` — Weather.cs:103; `_timer` — Weather.cs:104; `_lastTick` — Weather.cs:106
- `_windTimer` — Weather.cs:196; `CurrentCount` and `_effects[]` — Weather.cs:198-203
- `Weather.CurrentWeather` — Weather.cs:132, 149, 168 or 187 depending on type
- No World entity state.

**Creates** — `WeatherEffect` entries filled in place in the fixed `_effects` array, random screen positions from `Client.Game.Scene.Camera.Bounds` — Weather.cs:198-203.

**Destroys** — previous weather state discarded by `Reset()` — Weather.cs:99.

**Triggers**
- `EventSink.InvokeOnSetWeather(null, new WeatherEventArgs(type,count,temp))` — PacketHandlers.cs:2562
- `GameActions.Print` of the localized weather message — Weather.cs:123, 140, 159, 178
- `PlayThunder()` — Weather.cs:151, 189; `PlayWind()` — Weather.cs:170

**Ignores / partial**
- `Client.Game.GetScene<GameScene>() == null` → packet dropped — PacketHandlers.cs:2546-2551
- `weather.CurrentWeather == type` → count/temp bytes never read, `Generate` never called — PacketHandlers.cs:2556; the same guard repeats at Weather.cs:94-97
- `count` clamped down to `MAX_WEATHER_EFFECT` via `Math.Min` — Weather.cs:102
- `WT_INVALID_0` / `WT_INVALID_1` → `_timer` zeroed, `CurrentWeather` left null, early return; Type/Count stay written — Weather.cs:108-114
- `CurrentWeather` assigned only inside `if (showMessage)` i.e. only when `Count > 0` — Weather.cs:116; a count-0 packet leaves it null, so the identical next packet is not suppressed
- Unrecognised `WeatherType` falls through the switch with no case, `CurrentWeather` stays null — Weather.cs:118-193

---

### 0x66 BookData

**Purpose** — Deliver the text of one or more book pages to an already-open book gump.

**Wire** — registered PacketHandlers.cs:272; variable (`-1, // 0x66`), body offset 3.
- `[3..6]` uint32BE `serial` — PacketHandlers.cs:2573
- `[7..8]` uint16BE `pageCnt` — PacketHandlers.cs:2574
- per page, sequentially from offset 9:
  - uint16BE page number — PacketHandlers.cs:2585; stored as `pageNum = value - 1` (wire page 1 → index 0, wire page 0 → index -1)
  - uint16BE `lineCnt` — PacketHandlers.cs:2590, read **only** if the page index passed the range test at 2588
  - `lineCnt` NUL-terminated strings: `ReadUTF8(true)` when `ModernBookGump.IsNewBook` (`Client.Version > CV_200`), else `ReadASCII` — PacketHandlers.cs:2598-2600. No length prefixes.
- No trailing field; the loop consumes the remainder.

**Mutates**
- `ModernBookGump.KnownPages` (ModernBookGump.cs:86) gains `pageNum` for **every** page including out-of-range and negative — PacketHandlers.cs:2586
- `ModernBookGump.BookLines[index]` (backing `_bookPage._pageLines`, ModernBookGump.cs:81, allocated `bookpages*8` at ModernBookGump.cs:574) — PacketHandlers.cs:2598
- Trailing lines blanked to `string.Empty` when the server sent fewer than 8 — PacketHandlers.cs:2614
- `ServerSetBookText` rewrites the same array: strips embedded `\n` — ModernBookGump.cs:108; appends `\n` to fitting lines — ModernBookGump.cs:124; `_ServerUpdate = true` — :128; `_bookPage.SetText` — :129; `CaretIndex = 0` — :130; `UpdatePageCoords()` — :131; `_ServerUpdate = false` — :132
- No World / Entity / Chunk / House state; purely gump-local.

**Creates** — nothing.

**Destroys** — nothing.

**Triggers** — `ModernBookGump.ServerSetBookText()` — PacketHandlers.cs:2627; queries FontsLoader for widths — ModernBookGump.cs:114.

**Ignores / partial**
- `!World.InGame` → dropped — PacketHandlers.cs:2568-2571
- No `ModernBookGump` for the serial, or disposed → dropped without parsing any page — PacketHandlers.cs:2576-2581
- Page applied only when `pageNum < gump.BookPageCount && pageNum >= 0` — PacketHandlers.cs:2588. Failing branch logs and `continue`s **without** reading `lineCnt` or the lines — PacketHandlers.cs:2619-2624 — leaving the reader inside that page's payload, so every subsequent page in the packet parses from the wrong offset.
- Within an accepted page, a line whose index `>= BookLines.Length` is logged (PacketHandlers.cs:2604) and the string is **not** read — same reader desync — PacketHandlers.cs:2596
- `KnownPages` is written at 2586 before both checks, so refused pages are still recorded as known
- Blank-fill only runs when `lineCnt < 8` — PacketHandlers.cs:2610; more than 8 lines writes past the page's slice into the next page's lines (the 2596 test bounds the whole array, not the page)

---

### 0x6C TargetCursor

**Purpose** — Open (or cancel) a targeting cursor with a cursor id and target type.

**Wire** — fixed 0x13 (19); reader `Seek(1)`.
- `[1]` uint8 → `CursorTarget` — PacketHandlers.cs:434
- `[2..5]` uint32BE `cursorId` — PacketHandlers.cs:435
- `[6]` uint8 → `TargetType` — PacketHandlers.cs:436
- The remaining 12 bytes (x, y, unused, z, graphic) are never read.

**Mutates**
- `TargetManager.IsTargeting = (cursorType < TargetType.Cancel)` — Game/Managers/TargetManager.cs:268
- `TargetManager.TargetingState` — TargetManager.cs:269; `TargetingType` — :270; `_targetCursorId` — :285
- `World.Party.PartyHealTimer = 0` — PacketHandlers.cs:443; `World.Party.PartyHealTarget = 0` — :444
- `TargetManager.NextAutoTarget.Clear()` — PacketHandlers.cs:445 and :458
- `TargetManager.LastTargetInfo.SetEntity(serial)` — inside `TargetManager.Target()` (~TargetManager.cs:364/375)
- Cancel path: `World.HouseManager.Remove(0)` when previous state was `MultiPlacement` — TargetManager.cs:297 → HouseManager.cs:267-275 → `House.ClearComponents` (House.cs:179-202)
- Cancel path: `World.CustomHouseManager.Erasing / SeekTile / SelectedGraphic / CombinedStair` reset — TargetManager.cs:301-304

**Creates** — `QuestionGump` ("This may flag you criminal!") via `UIManager.Add`, inside `Target()`, when the auto-target resolves to a mobile and the criminal-query profile options match.

**Destroys** — all Multi components of the serial-0 placement preview house when a MultiPlacement cursor is cancelled — TargetManager.cs:297 → House.cs:188-199 (`s.Destroy()` per component). `HouseCustomizationGump` is only `Update()`d, not disposed — TargetManager.cs:306.

**Triggers**
- OUT `Send_TargetCancel(TargetingState, _targetCursorId, TargetingType)` — TargetManager.cs:312
- OUT `Send_TargetObject(...)` from `Target()` on the auto-target / party-heal paths
- OUT `GameActions.RequestMobileStatus(serial)` from `Target()` when `LastTargetInfo.Serial != serial`
- `ScriptRecorder.Instance.RecordTarget(serial)` — TargetManager.cs
- `UIManager.GetGump<HouseCustomizationGump>()?.Update()` — TargetManager.cs:306

**Ignores / partial**
- `cursorTarget == CursorTarget.Invalid` → `SetTargeting` returns changing nothing — TargetManager.cs:262-265
- `CancelTarget` only runs when previously targeting and the new type is `>= TargetType.Cancel` — TargetManager.cs:276-279
- Party-heal auto-target requires `PartyHealTimer < Time.Ticks && PartyHealTarget != 0` — PacketHandlers.cs:440; this branch wins and discards any queued `NextAutoTarget` — :445
- Queued auto-target fires only when `NextAutoTarget.ExpectedCursorTarget == cursorTarget && ExpectedTargetType == targetType` — PacketHandlers.cs:450-451; on mismatch nothing is sent but `NextAutoTarget` is cleared anyway — :458
- `Target()` returns doing nothing if `!IsTargeting` — TargetManager.cs:345-348; returns early leaving the cursor open when the criminal QuestionGump is shown
- x/y/z/graphic tail ignored entirely

---

### 0x6D PlayMusic

**Purpose** — Name the music track for the current region, or 0xFFFF meaning "no music here".

**Wire** — fixed 0x0003 (PacketsTable.cs:150), body offset 1.
- `[1..2]` uint16BE `index` — PacketHandlers.cs:2407. That is the whole packet.

**Mutates**
- MusicDiagnostics anchor, unconditionally and before any decision: `_anchorX = World.Player.X`, `_anchorY`, `_anchorMap = World.MapIndex` — MusicDiagnostics.cs:54-56; `_anchorTime` — :59; `_lastZoneBand = 0` — :60; via `ServerPacket` at PacketHandlers.cs:2412
- Stop path (`index == 0xFFFF`): `AudioManager._lastServerIndex = Constants.MUSIC_STOP_INDEX` — AudioManager.cs:472; `_lastServerAt = Time.Ticks` — :473; `_currentMusicIndices[1] = -1` — :475
- Stop path, kept: `_mapPlayedTrack = playing` via `AdoptAsMapTrack` — AudioManager.cs:652, called at :486
- Stop path, not kept: `StopMusic()` stops/disposes/nulls `_currentMusic[0..1]` — AudioManager.cs:444-452; `_currentMusicIndices[0] = -1` — :493; `ForgetMapTrack` → `_mapPlayedTrack = -1` — :662 via :495
- Real-track path: `World.OldMusicIndex = index` — PacketHandlers.cs:2441 (World.cs:97)
- `NotifyServerTrack` — PacketHandlers.cs:2443: `_lastServerIndex` — AudioManager.cs:532; `_lastServerAt` — :533; `ForgetMapTrack` — :535/:662
- `PlayMusic` — PacketHandlers.cs:2444: `StopMusic()` — AudioManager.cs:349; `_currentMusicIndices[idx] = music` — :352; `_currentMusic[idx] = (UOMusic)m` — :353; `Play` — :355
- No World.Items / World.Mobiles / chunk state.

**Creates** — a `UOMusic` from `Client.Game.Sounds.GetMusic`, stored in `_currentMusic` — AudioManager.cs:341, 353.

**Destroys** — previously playing `UOMusic` objects `Stop()`ed and `Dispose()`d in `StopMusic` — AudioManager.cs:448-450, reached from :491 and :345/:349.

**Triggers** — MusicDiagnostics writes only: `RawMusicPacket` (PacketHandlers.cs:2411 → MusicDiagnostics.cs:112), `ServerPacket` (:2412 → :48), `StopIgnored` (:2430 → :132), `Kept` (AudioManager.cs:484), `Stopped` (:441), `Started` (:359), `StartFailed` (:370), `SameTrack` (:376). No outgoing packet, no gump.

**Ignores / partial**
- `index == Constants.MUSIC_STOP_INDEX` (0xFFFF, Constants.cs:101) intercepted at PacketHandlers.cs:2419 and never reaches `AudioManager.PlayMusic`; comment at :2414-2418 records that PlayMusic would reject any index `>= MAX_MUSIC_DATA_INDEX_COUNT` (150, Constants.cs:96) at AudioManager.cs:303 first
- `Settings.GlobalSettings.IgnoreServerStopMusic` (Settings.cs:118) makes the stop a playback no-op; the running track is adopted as the map's — PacketHandlers.cs:2426-2433, AudioManager.cs:477-488
- Stop also ignored when the playing track is what the music map would pick: `MapIsOn() && TryResolve(out wanted) && wanted == playing` — AudioManager.cs:478
- Stop only "kept" when something is still streaming: `playing >= 0 && StillGoing()` — AudioManager.cs:477, 504-507
- `World.OldMusicIndex` written at PacketHandlers.cs:2441 before AudioManager can reject, so an index ≥150 is still remembered as the region track
- `AudioManager.PlayMusic` silently returns on `!_canReproduceAudio` (:298), `music >= 150` (:303), `!EnableCombatMusic && iswarmode` (:329-332), volume outside [-1,1] (:336); a null profile / `!EnableMusic` yields volume 0 rather than a return (:320-322)
- No-op when the same `UOMusic` instance is already `_currentMusic[0]` and not war mode — AudioManager.cs:347, 373-377
- No `World.InGame` / `World.Player` guard — runs on the login scene; `MusicDiagnostics.ServerPacket` skips the position anchor when `World.Player` is null — MusicDiagnostics.cs:52

---

### 0x6E CharacterAnimation

**Purpose** — Play a specific animation group on a mobile.

**Wire** — fixed 0x000E (14) — PacketsTable.cs:151; reader starts at offset 1.
- `[1..4]` uint32BE `serial`, looked up with `World.Mobiles.Get` — PacketHandlers.cs:2632
- `[5..6]` uint16BE `action` — PacketHandlers.cs:2639
- `[7..8]` uint16BE `frame_count` — PacketHandlers.cs:2640, narrowed to byte at :2649
- `[9..10]` uint16BE `repeat_count` — PacketHandlers.cs:2641, narrowed at :2650
- `[11]` bool, **inverted**: `forward = !p.ReadBool()` — PacketHandlers.cs:2642
- `[12]` bool `repeat` — PacketHandlers.cs:2643
- `[13]` uint8 `delay` — PacketHandlers.cs:2644, passed as the animation interval

**Mutates**
- `Mobile._animationGroup` — Mobile.cs:393
- `Mobile.AnimIndex = forward ? 0 : frameCount` — Mobile.cs:394
- `Mobile._animationInterval = delay` — Mobile.cs:395
- `Mobile.AnimationFrameCount = forward ? 0 : frameCount` — Mobile.cs:396 (on a forward animation the server's frame count is discarded and stored as 0)
- `Mobile._animationRepeateMode` / `_animationRepeatModeCount = repeatCount` — Mobile.cs:397-398
- `Mobile._animationRepeat` — Mobile.cs:399; `_isAnimationForwardDirection` — :400
- `Mobile.AnimationFromServer = true` — Mobile.cs:401
- `Mobile.LastAnimationChangeTime = Time.Ticks` — Mobile.cs:402
- Idle timer reseeded via `CalculateRandomIdleTime()` — Mobile.cs:404

**Creates** — nothing. **Destroys** — nothing.

**Triggers** — `Mobile.GetReplacedObjectAnimation(graphic, action)` remaps the group through `AnimationsLoader.Instance.GroupReplaces` and takes it modulo the group's `AnimationCount` — MobileAnimation.cs:1490-1535; the requested group can be silently substituted or wrapped.

**Ignores / partial**
- `World.Mobiles.Get(serial) == null` → no effect — PacketHandlers.cs:2634-2637. No `World.Player` null guard and no `World.InGame` guard.
- `World.Mobiles.Get` (EntityCollection.cs:40) does not filter destroyed entities, unlike `World.Get` (World.cs:551-554) — an animation can land on an `IsDestroyed` Mobile still filed in the dictionary
- `frame_count` cast to byte at PacketHandlers.cs:2649 — values above 255 wrap; `repeat_count` narrowed at :2650
- Forward flag is the logical inverse of the wire byte — PacketHandlers.cs:2642
- `GetReplacedObjectAnimation` returns the index unchanged when the graphic's group is neither Low nor People — MobileAnimation.cs:1507 / falls past :1526

---

### 0x6F SecureTrading

**Purpose** — Secure-trade window control: open, close, accept-state change, gold/platinum for either side.

**Wire** — variable (PacketsTable.cs:152), body offset 3.
- `[3]` uint8 `type` — PacketHandlers.cs:469
- `[4..7]` uint32BE `serial` — PacketHandlers.cs:470
- type 0 (open): uint32BE `id1` — :474; uint32BE `id2` — :475; bool `hasName` — :483; then NUL-terminated ASCII name — :488, read only when `hasName && p.Position < p.Length`
- type 1 (close): nothing further
- type 2 (accept state): uint32BE `id1` — :499; uint32BE `id2` — :500; each used only as a non-zero test
- type 3 (his gold): uint32BE `HisGold` — :525; uint32BE `HisPlatinum` — :526
- type 4 (my gold): uint32BE `Gold` — :520; uint32BE `Platinum` — :521
- types outside 0-4 read nothing beyond type and serial

**Mutates**
- `TradingGump.ImAccepting = id1 != 0` — PacketHandlers.cs:506 → TradingGump.cs:151-162 (setter calls `SetCheckboxes` on change)
- `TradingGump.HeIsAccepting = id2 != 0` — PacketHandlers.cs:507 → TradingGump.cs:164-175
- `TradingGump.Gold` — :520 → TradingGump.cs:83-98; `Platinum` — :521 → TradingGump.cs:100-115
- `TradingGump.HisGold` — :525 → TradingGump.cs:117-132; `HisPlatinum` — :526 → TradingGump.cs:134-149
- No World / Item / Mobile / Chunk / House state.

**Creates** — `TradingGump(serial, name, id1, id2)` added to UIManager for type 0 — PacketHandlers.cs:491 → TradingGump.cs:66-78.

**Destroys** — `TradingGump` disposed for type 1 — PacketHandlers.cs:495; `UIManager.GetTradingGump` matches on ID1, ID2 or LocalSerial.

**Triggers**
- `TradingGump.RequestUpdateContents` for type 2 — PacketHandlers.cs:509; `UpdateContents` reads `World.Get(ID1)` and walks its item list — TradingGump.cs:177-184
- Coin label text rewritten by the Gold/Platinum setters only on `Client.Version >= CV_704565` — TradingGump.cs:92, 109, 126, 143

**Ignores / partial**
- `!World.InGame` → return before reading anything — PacketHandlers.cs:464-467
- type 0 aborts after `id1`/`id2` if `World.Get(id1)` or `World.Get(id2)` is null (invisible trader suppresses the window) — PacketHandlers.cs:478-481; `hasName` and the name are then never read
- Name read only when `hasName && p.Position < p.Length` — PacketHandlers.cs:486
- types 1-4 do nothing when `UIManager.GetTradingGump(serial)` is null — PacketHandlers.cs:495, 504, 516
- Gold/Platinum/HisGold/HisPlatinum setters short-circuit on unchanged values — TradingGump.cs:88, 105, 122, 139 — and skip the label write below `CV_704565`: stored but never displayed
- `ImAccepting` / `HeIsAccepting` re-run `SetCheckboxes` only when the bool actually flips — TradingGump.cs:156, 169

---

### 0x70 GraphicEffect

**Purpose** — Spawn a visual effect; the same body is registered for 0x70, 0xC0 and 0xC7 (PacketHandlers.cs:277, 314, 319) and branches on the packet-id byte.

**Wire** — 0x70 fixed 0x1C (28), body offset 1. `p[0]` is reachable because `StackDataReader`'s indexer returns `_data[0]` for ANY index — StackDataReader.cs:39.
- `@1` uint8 `type` (`GraphicEffectType`) — PacketHandlers.cs:2664
- `@2` uint32BE `source` — :2684; `@6` uint32BE `target` — :2685
- `@10` uint16BE `graphic` — :2686
- `@12` uint16BE `srcX`, `@14` uint16BE `srcY`, `@16` int8 `srcZ` — :2687-2689
- `@17` uint16BE `targetX`, `@19` uint16BE `targetY`, `@21` int8 `targetZ` — :2690-2692
- `@22` uint8 `speed`, `@23` uint8 `duration` — :2693-2694
- `@24` uint16BE `unk` — read and discarded — :2695
- `@26` bool `fixedDirection`, `@27` bool `doesExplode` — :2696-2697
- 0xC0/0xC7 only: uint32BE `hue`, uint32BE `blendmode` — :2704-2705
- 0xC7 only: uint16BE `tileID`, uint16BE `explodeEffect`, uint16BE `explodeSound`, uint32BE `serial`, uint8 `layer`, `Skip(2)` — all into locals and discarded — :2709-2714
- ScreenFade sub-branch (0x70 only): `Skip(8)` then uint16BE `val` — :2670-2671

**Mutates**
- New `GameEffect` pushed onto `World._effectManager`'s linked list — EffectManager.cs:242 via `World.SpawnEffect` (World.cs:743)
- Effect linked into the map chunk list: `GameEffect.SetSource` → `SetInWorldTile` (GameEffect.cs:181/188) → `GameObject.SetInWorldTile` writes X, Y, Z, `IsPositionChanged` and calls `AddToTile` (GameObject.cs:265-272) → `Chunk.AddGameObject` writes `obj.TileChunk`, `TileCellX`, `TileCellY`, `chunk.Tiles[x,y]` (Chunk.cs:172-180)
- Effect hue incremented by 1 when non-zero — EffectManager.cs:88-91
- Duration multiplied by `Constants.ITEM_EFFECT_ANIMATION_DELAY` (50) — EffectManager.cs:93

**Creates** — `MovingEffect` (type 0) — EffectManager.cs:109; `LightningEffect` (type 1) — :169; `FixedEffect` (type 2 FixedXYZ / type 3 FixedFrom) — :188 / :212; `DragEffect` (type 5) — :145, unreachable from this handler.

**Destroys** — nothing here; effects die later in `EffectManager.Update` when `Distance > World.ClientViewRange` — EffectManager.cs:50-53.

**Triggers** — `Log.Warn("Effect not implemented")` for ScreenFade — PacketHandlers.cs:2678; `Log.Warn("Unhandled 'Screen Fade' effect.")` / `"Unhandled effect."` — EffectManager.cs:231/236.

**Ignores / partial**
- `World.Player == null` → return — PacketHandlers.cs:2659
- Any `type > GraphicEffectType.FixedFrom` (0x03) returns without spawning — PacketHandlers.cs:2666-2682; that covers ScreenFade (0x04) and DragEffect (0x05), so 0x70/0xC0/0xC7 can never produce a drag or a fade
- ScreenFade sub-branch runs only for `p[0] == 0x70`; clamps `val` to 4 (PacketHandlers.cs:2673-2676), logs, throws it away
- `hue` / `blendmode` read only when `p[0] != 0x70` — PacketHandlers.cs:2701-2705; a 0x70 always spawns with hue 0 and blendmode Normal
- `blendmode = value % 7` — PacketHandlers.cs:2705; `GraphicEffectBlendMode.ScreenRed` (0x07) is unreachable and folds to Normal
- `hue` read as uint32 and truncated to ushort at the SpawnEffect call — PacketHandlers.cs:2723
- `hasparticles` hardcoded false at the call site — PacketHandlers.cs:2734
- Entire 0xC7 tail parsed and discarded — PacketHandlers.cs:2709-2714
- EffectManager drops the effect when `graphic <= 0` for Moving, Drag, FixedXYZ, FixedFrom — EffectManager.cs:98, 135, 183, 207
- `speed == 0` bumped to 1 for Moving and Drag only — EffectManager.cs:104, 140
- FixedXYZ and FixedFrom pass speed 0 regardless of the wire byte — EffectManager.cs:197, 222
- FixedFrom falls back to source coordinates when the source serial is absent/invalid — FixedEffect.cs:73-79

---

### 0x71 BulletinBoardData

**Purpose** — Bulletin board traffic: open a board, add a summary line, or open one full message.

**Wire** — variable (PacketsTable.cs:154), body offset 3 (PacketHandlers.cs:227-228).
- `[3]` uint8 subcommand — PacketHandlers.cs:2753
- case 0 (open): `[4..7]` uint32BE board serial — :2758; `[8..29]` UTF8 fixed 22 bytes, safe, board name — :2771 (read lazily as the 4th ctor argument, so consumed only when the item exists)
- case 1 (summary): `[4..7]` uint32BE board serial — :2783; `[8..11]` uint32BE message serial — :2790; `[12..15]` uint32BE `parentID` — :2791, read into a local, never used; then uint8 len + UTF8[len] poster — :2794-2795; uint8 len + UTF8[len] subject — :2798-2799; uint8 len + UTF8[len] datetime — :2802-2803; all three concatenated with `" - "`
- case 2 (message): `[4..7]` uint32BE board serial — :2814; `[8..11]` uint32BE message serial — :2821; uint8 len + **ASCII**[len] poster — :2823-2824; uint8 len + UTF8[len] subject — :2826-2827; uint8 len + **ASCII**[len] datetime — :2829-2830; `Skip(4)` — :2832; uint8 `unk` — :2834 then `Skip(unk * 4)` — :2838; uint8 `lines` — :2841; then `lines` × (uint8 lineLen + UTF8[lineLen]) — :2848-2852, each appended with a trailing `\n`

**Mutates**
- `item.Opened = true` — PacketHandlers.cs:2774 (Item.cs:249). The only World-entity write in the handler.
- `BulletinBoardGump._databox` children: existing child with the same LocalSerial disposed — BulletinBoardGump.cs:157; `BulletinBoardObject` added — :163-164; `_databox.WantUpdateSize = true` — :166; `ReArrangeChildren()` — :167
- `UIManager.Gumps` — UIManager.cs:479-494 `AddFirst` + `_needSort`

**Creates**
- case 0: `BulletinBoardGump` (PacketHandlers.cs:2771, ctor BulletinBoardGump.cs:50-…), `LocalSerial` = board serial, positioned at `(window.Width/2 - 245, window.Height/2 - 205)` — :2768-2769, added at :2772
- case 1: a `BulletinBoardObject` child — BulletinBoardGump.cs:163
- case 2: a `BulletinBoardItem` gump at X=40, Y=40 — PacketHandlers.cs:2861-2875 (ctor BulletinBoardGump.cs:183-…), plus a stack-allocated 256-char `ValueStringBuilder` — :2843-2844, disposed :2877

**Destroys** — case 0: any pre-existing `BulletinBoardGump` for the same serial disposed first — PacketHandlers.cs:2763-2766. case 1: same-serial `BulletinBoardObject` disposed — BulletinBoardGump.cs:157.

**Triggers** — gumps only (`BulletinBoardGump`, `BulletinBoardItem`); no outgoing packets. The board gump installs a HitBox MouseUp handler (BulletinBoardGump.cs:80-95) that later disposes any `BulletinBoardItem` and opens a blank one — deferred, not part of this run.

**Ignores / partial**
- `!World.InGame` → whole packet dropped — PacketHandlers.cs:2748-2751
- case 0: `World.Items.Get(serial) == null` → no gump, the board-name bytes are never read — PacketHandlers.cs:2759-2761. Uses `World.Items.Get` (item-only), not `World.Get`.
- case 1: no `BulletinBoardGump` for that serial → summary dropped, poster/subject/date never read — PacketHandlers.cs:2784-2788
- case 2: same guard skips the entire message body — PacketHandlers.cs:2815-2819
- cases 1 and 2: each length-prefixed string collapses to `string.Empty` when its length byte is `<= 0` — :2795, 2799, 2803, 2824, 2827, 2830
- case 2: zero-length body lines skipped, no blank line emitted — PacketHandlers.cs:2850
- case 2: leading whitespace trimmed via `msg.TrimStart()` — PacketHandlers.cs:2868
- case 2: `variant` = 2 when the poster string equals `World.Player.Name`, else 1 — PacketHandlers.cs:2859; a plain string comparison picks the button set
- case 1 decodes poster/subject/date as UTF8; case 2 decodes poster and datetime as ASCII and subject as UTF8 — the two subcommands decode the same fields differently
- `parentID` parsed and discarded — no reply threading
- Any subcommand other than 0/1/2 falls out of the switch with no default — PacketHandlers.cs:2753-2882

---

### 0x72 Warmode

**Purpose** — Tell the client whether the player is in war mode.

**Wire** — registered PacketHandlers.cs:279; fixed 0x0005 (PacketsTable.cs:155), body offset 1.
- `[1]` bool war flag — PacketHandlers.cs:2892
- `[2..4]` the remaining three bytes are never read

**Mutates**
- `PlayerMobile.InWarMode` backing field — PlayerMobile.cs:76 (auto-property), written from PacketHandlers.cs:2892
- `Mobile.Flags` is **not** touched: the base `Mobile.InWarMode` getter derives from `(Flags & Flags.WarMode)` and its setter is an empty body — Mobile.cs:174-178. `PlayerMobile` overrides both with an auto-property, so for the player the stored bool and `Flags.WarMode` are independent values.

**Creates** / **Destroys** — nothing.

**Triggers** — none.

**Ignores / partial**
- `!World.InGame` → return — PacketHandlers.cs:2887; the code dereferences `World.Player` immediately after, relying on InGame implying `Player != null` (World.cs:163)
- The same assignment against any non-player Mobile would be silently discarded by the empty setter — Mobile.cs:177
- Three trailing protocol bytes ignored

---

### 0x73 Ping

**Purpose** — Echo of a client ping sequence byte, for round-trip latency.

**Wire** — fixed 0x0002 (PacketsTable.cs:156), reader `Seek(1)`.
- `[1]` uint8 ping index — PacketHandlers.cs:2897, passed straight to `NetStatistics.PingReceived(byte idx)`

**Mutates**
- `_pings[idx % 5] = Time.Ticks - _startTickValue` — Network/NetStatistics.cs:106
- `LastPingReceived = Time.Ticks` — NetStatistics.cs:107 (used by connection-timeout logic)

**Creates** / **Destroys** — nothing. **Triggers** — nothing; no reply, no gump.

**Ignores / partial**
- No `World.Player` / `World.InGame` guard — runs in the login scene too
- Index not validated, folded with `% _pings.Length` (5) — NetStatistics.cs:106; an out-of-range or wrong index overwrites an arbitrary slot rather than being rejected
- Delta computed against `_startTickValue`, which `NetStatistics.SendPing` (NetStatistics.cs:131) overwrites on every send — the measurement is against the most recent ping sent, not the one carrying this index; with several in flight the sample is wrong rather than discarded
- `NetClient.Socket` aliases `AsyncNetClient.Socket` (NetClient.cs:164), so this writes the same NetStatistics instance the sender uses

---

### 0x74 BuyList

**Purpose** — Prices and display names for items already delivered into a vendor's buy container.

**Wire** — variable (PacketsTable.cs:157), body offset 3.
- `[3..6]` uint32BE container serial — PacketHandlers.cs:2907
- `[7]` uint8 `count` — PacketHandlers.cs:2944-2946, read only if `container.Layer` is `ShopBuyRestock` or `ShopBuy`
- then `count` × { uint32BE price — :2982; uint8 nameLen — :2983; ASCII(nameLen) name — :2984 }
- The vendor serial is **not** on the wire — taken from `container.Container` — PacketHandlers.cs:2914

**Mutates**
- `Item.Price` (Item.cs:251) — PacketHandlers.cs:2982
- `Item.Name` (Entity.cs:85) — PacketHandlers.cs:2988 (OPL), :2992 (cliloc), :3000 (ItemData.Name) or :3004 (literal)
- Container child linked list reordered in place when `container.Graphic == 0x2AF8`: `SortContents` (PacketHandlers.cs:2960 → LinkedObject.cs:223-315) rewrites Next/Previous on every child and reassigns `container.Items` — LinkedObject.cs:308

**Creates** — `ModernShopGump` added to UIManager — PacketHandlers.cs:2927; or `ShopGump` — :2939-2940.

**Destroys** — existing `ModernShopGump` disposed unconditionally when the modern gump is enabled — PacketHandlers.cs:2926; existing `ShopGump` disposed when its LocalSerial differs from the vendor or it is not a buy gump — :2931-2934.

**Triggers** — `UIManager.Add` of the new shop gump — PacketHandlers.cs:2927 / 2940. No outgoing packet.

**Ignores / partial**
- `!World.InGame` → return — PacketHandlers.cs:2902
- `World.Items.Get(container serial) == null` → return before any gump work — :2909
- `World.Mobiles.Get(container.Container) == null` → return — :2916
- `container.Layer` neither `ShopBuyRestock` nor `ShopBuy` → the gump is still created/replaced but the entire price list is never parsed and every price is dropped — :2944
- `container.Items == null` → return with the gump already created — :2950-2953
- The loop breaks the moment the client's local item list runs out even if `count` says more remain; the rest of the wire data is abandoned — :2975-2978
- Name resolution order: OPL wins (:2986), then a purely numeric name is treated as a cliloc id (:2990-2997), then an empty name falls back to `ItemData.Name` (:2998-3001), else the literal (:3004) — the server's string is discarded whenever OPL already has one
- Iteration direction depends on graphic: 0x2AF8 sorts ascending by X and walks forward, everything else walks the list backwards from the tail — :2957-2971, :3007-3014
- `ProfileManager.CurrentProfile` dereferenced without a null check — :2924

---

### 0x77 UpdateCharacter

**Purpose** — Update an already-known mobile's body, position, direction, hue, flags and notoriety.

**Wire** — registered twice: PacketHandlers.cs:282 (0x77) and :326 (0xD2), same body. Fixed lengths differ: 0x77 = 0x0011 (17), 0xD2 = 0x0019 (25) in PacketsTable.cs. Reader starts at absolute offset 1 for both.
- `@1..4` uint32BE `serial` — PacketHandlers.cs:3026
- `@5..6` uint16BE `graphic` — :3034
- `@7..8` uint16BE `x` — :3035; `@9..10` uint16BE `y` — :3036; `@11` int8 `z` — :3037
- `@12` uint8 `direction` (cast to `Direction`, full byte including the Running bit) — :3038
- `@13..14` uint16BE `hue` — :3039
- `@15` uint8 `flags` — :3040; `@16` uint8 `notoriety` — :3041
- Reader ends at 17; for 0xD2 the remaining 8 bytes are never read.

**Mutates**
- `mobile.NotorietyFlag = notoriety` (Mobile.cs:230) — PacketHandlers.cs:3043, for every mobile including the player, before any branch
- Player branch: `mobile.Flags` (Entity.cs:66) — :3047; `mobile.Graphic` (GameObject.cs:107 setter, which also writes `originalGraphic`, runs `GraphicsReplacement.Replace`, rewrites Hue and calls `OnGraphicSet`) — :3048; `CheckGraphicChange()` (Mobile.cs:1054, writes IsFemale and Race) — :3049; `FixHue(hue)` (Entity.cs:115-134) — :3050
- Non-player branch: `UpdateGameObject(serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 1, 1)` — PacketHandlers.cs:3055 → :6545 (type 1 → mobile path). Inside: direct position write X/Y/Z/Direction/IsRunning/ClearSteps when `World.Get(mobile)` is null or the mobile sits at 0xFFFF,0xFFFF — :6708-6716; same direct write when `EnqueueStep` returns false — :6718-6726; `mobile.Graphic = graphic & 0x3FFF` — :6729; `FixHue` — :6730; `mobile.Flags = flagss` — :6731; `mobile.SetInWorldTile(X,Y,Z)` — :6770 → GameObject.cs:267-269 + `UpdateScreenPosition` (:270) + `AddToTile` (:271) → Chunk.AddGameObject (Chunk.cs:172) after `GameObject.RemoveFromTile` (GameObject.cs:220) relinks `Chunk.Tiles[x,y]`
- `EnqueueStep` (Mobile.cs:304) pushes up to three `Step` structs onto `mobile.Steps` — Mobile.cs:340, 348, 358 — may call `SetAnimation(0xFF)` (:322) and write `LastStepTime` (:325)
- `Client.Game.GameCursor.ItemHold.UpdatedInWorld = true` when the held item is this serial — PacketHandlers.cs:6585

**Creates** — only via `UpdateGameObject` and only if the mobile vanished between the Get at :3027 and the call: `World.GetOrCreateMobile` — PacketHandlers.cs:6596 → World.cs:600-601 (`Mobile.Create` + `Mobiles.Add`). The guard at :3029-3032 makes this unreachable for 0x77 in practice.

**Destroys** — nothing directly. `World.GetOrCreateMobile` can return a destroyed entry to the pool — World.cs:589-593 (`Mobiles.Remove` then `mob.ReturnToPool`).

**Triggers**
- `RequestUpdateContents` on ContainerGump / PaperDollGump / ModernPaperdoll when the held item matches — PacketHandlers.cs:6576, 6580, 6581
- OUT `GameActions.SingleClick(serial)` → 0x09 click request (GameActions.cs:660) — PacketHandlers.cs:6740, created path only with `Profile.ShowNewMobileNameIncoming`
- OUT `GameActions.RequestMobileStatus(serial)` — PacketHandlers.cs:6777, created path only
- `HouseDiagnostics.LogRangeProbe` (file write) — PacketHandlers.cs:6758, created path

**Ignores / partial**
- `World.Player == null` → dropped — PacketHandlers.cs:3021-3024 (null check, not `World.InGame`)
- Mobile not already in `World.Mobiles` → dropped entirely, including its notoriety — :3029-3032
- For the player itself x, y, z and direction are deliberately ignored; comment at :3051 "x,y,z, direction cause elastic effect, ignore em for the moment" — only flags, graphic and hue are applied — :3045-3052
- Inside `UpdateGameObject`: position is slammed in directly only when the mobile is unknown to `World.Get` or parked at 0xFFFF/0xFFFF (:6708); otherwise the move is queued as walk steps (:6718) and applied over later frames — the server's coordinates are deferred, not applied
- `EnqueueStep` returns false once `Steps.Count >= Constants.MAX_STEP_COUNT` — Mobile.cs:306-309 — which is what forces the hard write at :6720-6725
- A step identical to the current end position is swallowed — Mobile.cs:313-316
- `graphic` masked with 0x3FFF — PacketHandlers.cs:6729; the top two bits are dropped
- `FixHue` clamps any hue `>= 0x0BB8` down to 1 — Entity.cs:121-124

---

### 0x78 UpdateObject (also 0xD3)

**Purpose** — Full description of a mobile (or the player): position, graphic, hue, flags, notoriety, plus its entire equipment list.

**Wire** — registered for 0x78 (PacketHandlers.cs:283) and 0xD3 (:327); both variable → reader `Seek(3)`; offsets absolute.
- `@3` uint32BE `serial` — :3066; `@7` uint16BE `graphic` — :3067
- `@9` uint16BE `x` — :3068; `@11` uint16BE `y` — :3069; `@13` int8 `z` — :3070
- `@14` uint8 Direction — :3071; `@15` uint16BE `hue` — :3072
- `@17` uint8 Flags — :3073; `@18` uint8 NotorietyFlag — :3074
- if `p[0] != 0x78` (i.e. 0xD3): 6 bytes skipped — :3127-3130
- then uint32BE `itemSerial` — :3132, and while `itemSerial != 0 && Position < Length`: uint16BE `itemGraphic` — :3139; uint8 `layer` — :3140; item hue: uint16BE unconditionally when `Client.Version >= CV_70331` — :3143-3146, ELSE only when `(itemGraphic & 0x8000)`, in which case the top bit is stripped and a uint16BE hue read — :3147-3151; then uint32BE next `itemSerial` — :3170
- For the player branch x, y, z and direction are read and never used — :3078-3085 use only graphic, hue, flags.

**Mutates**
- Player path: `World.Player.Graphic` — :3081; `CheckGraphicChange` — :3082; `FixHue(hue)` — :3083 (Entity.cs:115-133); `World.Player.Flags` — :3084. X/Y/Z/Direction discarded.
- Non-player path: everything `UpdateGameObject` writes — :3088 → :6545. For a mobile: Graphic :6604/:6729, Direction :6606/:6713/:6723, Hue :6607/:6730, X/Y/Z :6608-6610/:6710-6712/:6720-6722, Flags :6611/:6731, IsRunning :6714/:6724, ClearSteps :6715/:6725 (Mobile.cs:298-302), EnqueueStep :6718, SetInWorldTile :6770 (GameObject.cs:265-272 → AddToTile → Chunk.AddGameObject)
- `Mobile.NotorietyFlag` — PacketHandlers.cs:3118
- Per equipment item: `Item.Graphic` — :3154; hue via `FixHue` — :3155; `Item.Amount` forced to 1 regardless of prior value — :3156; `World.RemoveItemFromContainer(item)` — :3157 → World.cs:617-653 (Container=0xFFFFFFFF at :647, Next/Previous nulled :650-651, RemoveFromTile :652); `Item.Container = serial` — :3158; `Item.Layer = (Layer)layer` — :3159; `Mobile.Mount = item` when layer == Layer.Mount — :3163; `Item.CheckGraphicChange()` — :3166 (Item.cs:607-646); `obj.PushToBack(item)` — :3168 (LinkedObject.cs:57-78)
- `World.Season` and every GameObject's seasonal graphic in every used chunk — `World.ChangeSeason`, PacketHandlers.cs:3183/3187 → Game/World.cs:206-234 (Season at :208; :210-222 walks `Map.GetUsedChunks()` × 8 × 8 × every object calling `UpdateGraphicBySeason`)
- `World.Player.Abilities[0]` and `[1]` via `UpdateAbilities()` — PacketHandlers.cs:3195 → PlayerMobile.cs:301-322+

**Creates**
- Mobile via `World.GetOrCreateMobile` inside UpdateGameObject — :6596 → World.cs:583-605 → `Mobile._pool.GetOne()` (Mobile.cs:240-245)
- Item via `World.GetOrCreateItem` inside UpdateGameObject — :6615 → World.cs:559-581 → `Item._pool.GetOne()`
- Item per equipment entry via `World.GetOrCreateItem` — :3153
- House + Multi components indirectly when the item path hits `Item.CheckGraphicChange` with IsMulti — Item.cs:633-645 → `LoadMulti` (Item.cs:335-600), one `Multi.Create` per block added to `House.Components` — Item.cs:470, 546

**Destroys**
- Every child item of the object that is not `Opened` and not `Layer.Backpack`: `World.RemoveItem(it.Serial, true)` — PacketHandlers.cs:3109. `forceRemove=true`, so each is `Destroy()`d, its OPL entry removed (World.cs:676), removed from `World.Items` and returned to the pool (World.cs:681-683), children recursively removed (World.cs:664-674). The walk saves a `next` pointer (:3100-3113) to survive the mutation.
- `Item.Destroy` disposes ContainerGump / GridContainer / SpellbookGump / MapGump / GridLootGump / BulletinBoardGump / SplitMenuGump for any Opened item — Item.cs:263-296
- In `LoadMulti`, an existing House has `ClearComponents()` called, destroying every Multi — Item.cs:359, House.cs:188-199

**Triggers**
- `GameActions.RequestEquippedOPL()` — :3124 (player) and :3193; walks every Layer and calls `World.OPL.Contains(item)`, queuing an OPL request per item
- `PaperDollGump.RequestUpdateContents` / `ModernPaperdoll.RequestUpdateContents` — :3120-3121, :3191-3192
- `World.ChangeSeason(Season.Desolation, 42)` on death, or `ChangeSeason(World.OldSeason, World.OldMusicIndex)` on resurrect — :3183 / :3187; music index 42 causes `Client.Game.Audio.PlayMusic` — World.cs:230-233
- OUT `GameActions.SingleClick(serial)` — :6740 (new mobile + ShowNewMobileNameIncoming) or :6747 (new corpse + ShowNewCorpseNameIncoming)
- OUT `GameActions.RequestMobileStatus(serial)` for every newly created mobile — :6777
- `EventSink.InvokeOnItemCreated` / `InvokeOnItemUpdated` — :6695 / :6697; `InvokeOnCorpseCreated` — :6807
- `HouseDiagnostics.LogRangeProbe` — :6758; `LogHouseItemArrived` — :6802
- `World.Player.TryOpenCorpses()` → `GameActions.DoubleClickQueued` per nearby corpse — :6810 → PlayerMobile.cs:1435-1458
- `GameScene.UpdateMaxDrawZ(true)` and `MiniMapGump.RequestUpdateContents` if LoadMulti runs — Item.cs:591-593, 586
- `Send_CloseStatus` via `Entity.Destroy` on any removed mobile — Entity.cs:198-206

**Ignores / partial**
- `World.Player == null` → whole packet ignored — :3061-3064
- For `serial == World.Player` position and direction are read then discarded; only Graphic, hue and Flags applied — :3078-3085. The player is never moved by this packet.
- Bails after the header if `World.Get(serial)` is still null (e.g. UpdateGameObject returned early) — :3091-3096; the equipment list is then never parsed
- Child items spared from the pre-wipe only if `it.Opened` OR `it.Layer == Layer.Backpack` — :3107
- The 6-byte skip at :3127-3130 applies to every id except 0x78 (i.e. 0xD3)
- Equipment hue read unconditionally only on CV_70331+; on older clients only when the graphic's 0x8000 bit is set, otherwise `item_hue` stays 0 and `FixHue(0)` overwrites any previous hue — :3143-3151
- Equipment `Amount` always overwritten with 1 — :3156
- Equipment loop stops on `itemSerial == 0` OR `Position >= Length` — :3134; a truncated packet silently ends the list
- The commented-out `SerialHelper.IsItem(itemSerial)` guard (:3136-3137) means non-item serials are accepted as equipment
- Season change only when `oldDead != World.Player.IsDead` — :3175; `Send_DeathScreen` is commented out at :3182 with a note about sphere servers
- Inside UpdateGameObject: a mobile is created only when `SerialHelper.IsMobile(serial) && type != 3` — :6594; otherwise an Item is created even for a mobile serial
- Inside UpdateGameObject: EnqueueStep failure teleports the mobile and clears its step queue — :6718-6726
- Inside UpdateGameObject: mobile graphic masked to 0x3FFF (:6729), multi graphics to 0x3FFF (:6663)

---

### 0x7C OpenMenu

**Purpose** — Open an old-style selection menu: icon strip, or gray radio-button list.

**Wire** — variable (PacketsTable.cs:165), body offset 3.
- `[3..6]` uint32BE `serial` — PacketHandlers.cs:3206
- `[7..8]` uint16BE `id` — :3207
- uint8 nameLen + ASCII[nameLen] `name` — :3208
- uint8 `count` — :3209
- uint16BE `menuid` then `p.Seek(p.Position - 2)` rewinds — :3211-3212; the first entry's graphic field is re-read as the style discriminator
- MenuGump branch, per entry: uint16BE graphic — :3222; uint16BE hue — :3223; uint8 nameLen + ASCII name — :3224
- GrayMenuGump branch, per entry: `p.Skip(4)` discarding graphic+hue — :3262; uint8 nameLen + ASCII name — :3263
- No check that `count` entries fit in the remaining bytes.

**Mutates** — no World state; only `UIManager.Gumps` via `UIManager.Add` — PacketHandlers.cs:3247 or :3298 → UIManager.cs:485 (`Gumps.AddFirst`), `_needSort` at :492.

**Creates**
- `MenuGump(serial, id, name)` at X=100 Y=100 when `menuid != 0` — :3216; `LocalSerial` = serial, `ServerSerial` = id, `IsFromServer` = true — MenuGump.cs:52-57; one `ItemView` child per drawable entry — MenuGump.cs:140
- `GrayMenuGump(serial, id, name)` — :3251, positioned from `Client.Game.Window.ClientBounds` — :3253-3254; one `RadioButton` per entry — MenuGump.cs:316; plus two Buttons id 0 and 1 — :3278-3294

**Destroys** — nothing; any menu gump already open for the same serial is left in place.

**Triggers**
- `Client.Game.Arts.GetArt(graphic)` per entry — :3226
- Deferred: MenuGump ItemView double-click sends `Send_MenuResponse(LocalSerial, ServerSerial, index, graphic, hue)` and disposes itself — MenuGump.cs:148-156; right-click sends `Send_MenuResponse(..., 0, 0, 0)` — MenuGump.cs:171; GrayMenuGump button 0 sends `Send_GrayMenuResponse(..., 0)` and button 1 the checked index — MenuGump.cs:332, 346

**Ignores / partial**
- `!World.InGame` → return — :3201
- MenuGump branch: an entry whose art has `artInfo.UV.Width == 0` or `UV.Height == 0` is skipped entirely — not added, `posX` not advanced — :3228-3244. Its bytes are still consumed and its server-side index `i+1` is still burned, so remaining entries keep correct indices but the skipped one is unpickable.
- MenuGump branch: `posY` is 0 when the art is ≥47 tall, else `(47 - height) >> 1` — :3232-3239
- GrayMenuGump branch throws away graphic and hue of every entry — :3262
- GrayMenuGump: per-entry height clamped up to a minimum of 21 — :3267-3270; each subsequent offset reduced by 1 — :3272
- GrayMenuGump created with `CanCloseWithRightClick = false` (MenuGump.cs:294) and initial ResizePic Height 111111 (MenuGump.cs:297) until `SetHeight` at :3296
- The style discriminator is the peeked `menuid` at :3211, not a documented field — a first entry with graphic 0 selects the gray list

---

### 0x82 ReceiveLoginRejection

**Purpose** — Reject a login / character-list request with a reason code.

**Wire** — registered three times to the same body: PacketHandlers.cs:354 (0x82), :355 (0x85), :356 (0x53). All fixed length 0x0002, so reader starts at offset 1.
- `[1]` uint8 `code` — read inside `LoginScene.HandleErrorCode`, Game/Scenes/LoginScene.cs:724
- Packet id re-read as `p[0]` — LoginScene.cs:726. `StackDataReader`'s indexer is `public byte this[int index] => _data[0];` (ClassicUO.IO/StackDataReader.cs:39) — it ignores the index and returns the packet id, which is what `GetError` switches on.

**Mutates**
- `LoginScene.PopupMessage = ServerErrorMessages.GetError(packetId, code)` (property LoginScene.cs:86) — LoginScene.cs:726
- `LoginScene.CurrentLoginStep = LoginSteps.PopUpMessage` (property LoginScene.cs:82) — LoginScene.cs:727
- No World state exists on this path.

**Creates** — nothing; the popup label is built later from `PopupMessage` at LoginScene.cs:293-297, which also clears it (:297).

**Destroys** — nothing.

**Triggers** — none outgoing; the state change makes the login scene render an error popup on its next update — LoginScene.cs:190-199, 255-268, 293-301.

**Ignores / partial**
- `World.InGame` → dropped outright — PacketHandlers.cs:6376-6379
- Current scene is not a `LoginScene` → dropped — PacketHandlers.cs:6383
- `ServerErrorMessages.GetError` clamps the code: 0x82 any code ≥9 → 8 (ServerErrorMessages.cs:124-127); 0x85 ≥6 → 5 (:104-107); 0x53 ≥10 → 9 (:94-97). An unknown code is reported as the last known error, not as unknown.
- Any packet id other than 0x53/0x85/0x27/0x82 returns `string.Empty` (ServerErrorMessages.cs:133), so `PopupMessage` is empty while `CurrentLoginStep` is still forced to PopUpMessage

---

### 0x85 ReceiveLoginRejection (shared body with 0x82 and 0x53)

**Purpose** — Reject a login / character-delete / connection attempt with an error code.

**Wire** — fixed 2 (`0x0002, // 0x85`), reader `Seek(1)`.
- `[1]` uint8 error code — LoginScene.cs:724
- `p[0]` selects the error table via `ServerErrorMessages.GetError(p[0], code)` — LoginScene.cs:726; because the `StackDataReader` indexer always returns `_data[0]` (StackDataReader.cs:39) this resolves to the packet id, distinguishing the 0x82 / 0x85 / 0x53 tables
- No other bytes consumed.

**Mutates**
- `LoginScene.PopupMessage` — Game/Scenes/LoginScene.cs:726
- `LoginScene.CurrentLoginStep = LoginSteps.PopUpMessage` — LoginScene.cs:727
- No world state at all: no entity, item, mobile, house, chunk or gump.

**Creates** — nothing here; the popup is built later when LoginScene renders `LoginSteps.PopUpMessage`.

**Destroys** — nothing. **Triggers** — no outgoing packets, no events.

**Ignores / partial**
- `World.InGame` → dropped silently — PacketHandlers.cs:6376-6379
- `Client.Game.GetScene<LoginScene>() == null` → discarded without logging — PacketHandlers.cs:6383-6386
- Only `World.InGame` (`Player != null && Map != null`) is tested, so a rejection arriving while Player exists but Map does not still reaches the LoginScene lookup

---

### 0x86 UpdateCharacterList

**Purpose** — Refreshed character list at the login screen (e.g. after a character delete).

**Wire** — PacketsTable.cs:175 gives -1, so variable: bytes 1..2 are the 16-bit BE total length and `packetOffset = 3` (PacketHandlers.cs:224-228); reader `Seek(3)`. The handler reads nothing itself and forwards the reader by ref to `LoginScene.UpdateCharacterList` — PacketHandlers.cs:6355.
- `[3]` uint8 character slot count — `int count = p.ReadUInt8()` — LoginScene.cs:759
- `[4…]` `count` records of 60 bytes: 30-byte ASCII name, `p.ReadASCII(30).TrimEnd('\0')` — LoginScene.cs:764 — then `p.Skip(30)` for the password field — LoginScene.cs:766

**Mutates**
- `Characters = new string[count]` (previous array discarded wholesale) — Game/Scenes/LoginScene.cs:760
- `Characters[i] = name` — LoginScene.cs:764
- `PopupMessage = null` — LoginScene.cs:660
- `CurrentLoginStep = LoginSteps.CharacterSelection` — LoginScene.cs:662
- `_currentGump` = the new CharacterSelectionGump — LoginScene.cs:667
- `PopupMessage = null` again after the popup is shown — LoginScene.cs:673

**Creates** — `new CharacterSelectionGump()` added to UIManager — LoginScene.cs:667; a modal `LoadingGump` carrying `PopupMessage` with an OK button — LoginScene.cs:671-672; a fresh `string[]` — LoginScene.cs:760.

**Destroys** — `UIManager.GetGump<CharacterSelectionGump>()?.Dispose()` — LoginScene.cs:663; `_currentGump?.Dispose()` — LoginScene.cs:665. No world entity is involved; World is not touched.

**Triggers** — gumps only (CharacterSelectionGump, optional modal LoadingGump). No outgoing packet.

**Ignores / partial**
- `World.InGame` → whole packet discarded without parsing — PacketHandlers.cs:6346-6349
- `GetScene<LoginScene>() == null` → discarded without parsing — PacketHandlers.cs:6351-6356
- `PopupMessage` cleared only when `CurrentLoginStep != LoginSteps.PopUpMessage`; otherwise the pending popup text survives and is re-shown — LoginScene.cs:658-661, 668-673
- The LoadingGump is created only when `PopupMessage` is non-blank, which after the clear at :660 can only happen on the PopUpMessage step — LoginScene.cs:668
- `count` is taken raw from the wire with no bound check against the declared length; the loop reads 60 bytes per record regardless — LoginScene.cs:759-767
- The 30-byte password field of each record is skipped, never stored — LoginScene.cs:766
- Unlike `ReceiveCharacterList` (PacketHandlers.cs:6359 → LoginScene.cs:677) this path does **not** parse cities and does **not** read the CharacterListFlags, so `World.ClientFeatures` (LoginScene.cs:682) is left at whatever 0xA9 set

---

### 0x88 OpenPaperdoll

**Purpose** — Show a mobile's paperdoll, with its title text and a can-lift flag.

**Wire** — fixed 0x42 (66) — PacketsTable.cs:177; body offset 1.
- `@1` uint32BE serial, looked up with `World.Mobiles.Get`, not `World.Get` — PacketHandlers.cs:3304
- `@5` ASCII fixed 60 bytes, title text — :3311
- `@65` uint8 flags; only bit 0x02 used, as CanLift — :3312, 3341, 3346

**Mutates**
- `mobile.Title = text` — PacketHandlers.cs:3314 → Mobile.cs:238
- `PaperDollGump.CanLift = (flags & 0x02) != 0` — PacketHandlers.cs:3348 → PaperdollGump.cs:137
- PaperDollGump title label text — :3349 → PaperdollGump.cs:450; or ModernPaperdoll title label — :3320 → ModernPaperdoll.cs:194
- Gump `Location` set to the cached position or (100,100) on creation — :3336-3341
- No entity position, hue, graphic or container state.

**Creates** — `ModernPaperdoll` added to UIManager when the profile uses it, the mobile is the player and no such gump exists — :3326; `PaperDollGump` added when no paperdoll gump exists for that mobile — :3341.

**Destroys** — nothing.

**Triggers**
- `GameActions.RequestEquippedOPL` on the modern path — :3328; walks every Layer of `World.Player` and calls `World.OPL.Contains(item)`, issuing property requests for anything uncached
- `PaperDollGump.RequestUpdateContents`, only when CanLift actually changed — :3351-3354
- `SetInScreen` and `BringOnTop` on both paths — :3321-3322, :3356-3357

**Ignores / partial**
- `World.Mobiles.Get(serial) == null` → return before reading text or flags — :3306-3309; an item serial, or a mobile filed under `World.Items`, is never found because `World.Get` is not used
- Modern branch requires `ProfileManager.CurrentProfile.UseModernPaperdoll && mobile.Serial == World.Player.Serial` — :3315; `World.Player` is dereferenced there without a null check and `World.InGame` is never tested
- On the modern path the flags byte is read but never applied — CanLift is not represented — :3315-3329
- On the classic path an existing gump has its title updated in place and rebuilds only when the lift flag flipped — :3345-3354
- Cached gump position used when `UIManager.GetGumpCachePosition` succeeds, else (100,100) — :3336-3339

---

### 0x89 CorpseEquipment

**Purpose** — List the equipment layers of a corpse container so items hang on the right slots.

**Wire** — variable (PacketsTable.cs:178), body offset 3.
- `[3..6]` uint32BE corpse serial — PacketHandlers.cs:3369
- `[7]` uint8 → `Layer`, the loop sentinel — :3383
- loop body: uint32BE item serial — :3387; then uint8 next Layer — :3399
- The loop condition (:3385) tests `layer != Layer.Invalid && p.Position < p.Length` **before** reading the 4-byte serial and the following layer byte, so a record starting on the last byte reads past the declared payload.
- The layer written to the item is `layer - 1` — :3395, one less than the wire value.

**Mutates**
- `World.Items[item_serial] = new Item` when the serial was unknown — World.cs:577 via `GetOrCreateItem`
- `item.Container = corpse serial` — PacketHandlers.cs:3394 (Item.cs:242)
- `item.Layer = layer - 1` — PacketHandlers.cs:3395 (Item.cs:245)
- `corpse.Items` linked list re-linked with the item at the tail — LinkedObject.cs:57-79 via `corpse.PushToBack` (PacketHandlers.cs:3396)
- Prior container unlinked and `item.Container` reset to 0xFFFFFFFF before relinking — World.cs:644, 647 via `RemoveItemFromContainer` (PacketHandlers.cs:3393)
- `item.Next` / `item.Previous` nulled — World.cs:650-651
- Item unlinked from its map tile: `GameObject.TileChunk` cleared, `Chunk.Tiles[cell]` handed to a neighbour, `TNext`/`TPrevious` nulled — GameObject.cs:220-246 via World.cs:652

**Creates** — Item instances from `Item._pool` — Item.cs:257 (`Item.Create`) via `World.GetOrCreateItem` (World.cs:576).

**Destroys** — a previously-destroyed Item still filed under the same serial is dropped from `World.Items` and returned to the pool before a fresh one is taken — World.cs:565, 569.

**Triggers**
- `PaperDollGump` / `ModernPaperdoll` `RequestUpdateContents` for a mobile previous container — World.cs:627-628
- `ContainerGump` / `GridContainer` / `NearbyLootGump` `RequestUpdateContents` for an item previous container — World.cs:632, 634, 637
- No packet is sent back; no gump is opened by this handler.

**Ignores / partial**
- `!World.InGame` → whole packet ignored — :3364-3367
- `World.Get(serial) == null`, which includes a destroyed entity still in the dictionary (World.cs:551-554) → ignored — :3372-3375
- Ignored unless `corpse.Graphic == 0x2006` — :3378-3381; the equipment list for anything the client does not currently believe is a corpse graphic is discarded entirely
- Items whose `(layer - 1) == Layer.Backpack` are skipped: the 4-byte serial is consumed but no item is created, no container set, nothing linked — :3389-3397
- The loop stops at the first `Layer.Invalid` byte, leaving remaining payload unparsed — :3385

---

### 0x8C ReceiveServerRelay

**Purpose** — Login server hands the client to a game shard: new address, port and encryption seed.

**Wire** — registered PacketHandlers.cs:351; fixed 0x000B (PacketsTable.cs:181), body offset 1; all reads happen inside `LoginScene.HandleRelayServerPacket`.
- `[1..4]` uint32 **little-endian** ip — LoginScene.cs:732 (`ReadUInt32LE`, unlike every other multi-byte field here)
- `[5..6]` uint16BE port — LoginScene.cs:733
- `[7..10]` uint32BE seed — LoginScene.cs:734

**Mutates**
- Existing socket disconnected synchronously — LoginScene.cs:736
- `AsyncNetClient.Socket` replaced with a brand new `AsyncNetClient` — LoginScene.cs:737
- `PacketHandlers.Handler.Reset()` clears both the main and plugin CircularBuffers — LoginScene.cs:738 → PacketHandlers.cs:167-177
- `EncryptionHelper` global state re-initialised with the new seed — LoginScene.cs:739
- New TCP connection established — LoginScene.cs:741; compression enabled — LoginScene.cs:745
- No World / Item / Mobile / Chunk / gump state.

**Creates** — a new `AsyncNetClient` and its socket connection — LoginScene.cs:737, 741.

**Destroys** — the previous socket — LoginScene.cs:736; all bytes still queued in the receive buffers discarded by `Handler.Reset` — PacketHandlers.cs:169, 174.

**Triggers** — raw 4-byte seed write on the new connection — LoginScene.cs:748-749; `Send_SecondLogin(Account, Password, seed)` — LoginScene.cs:752.

**Ignores / partial**
- `World.InGame` → return immediately — PacketHandlers.cs:6331
- Nothing happens when the active scene is not a LoginScene — PacketHandlers.cs:6336-6341
- Seed write, compression and `Send_SecondLogin` all skipped when `NetClient.Socket.IsConnected` is false after Connect — LoginScene.cs:743; no error is reported, the handler just returns
- `Disconnect().Wait()` and `Connect().Wait()` block the calling thread — LoginScene.cs:736, 741; packets are dispatched from `GameController.Update` (GameController.cs:478), so this stalls the frame loop
- Re-entrancy: `Handler.Reset()` (LoginScene.cs:738) clears the very CircularBuffer `ParsePackets` is iterating under `lock (stream)` one frame up (PacketHandlers.cs:101-153). The lock is reentrant on the same thread, so the buffer empties mid-loop and the `while (stream.Length > 0)` walk terminates, dropping any packets that arrived in the same batch after 0x8C.

---

### 0x90 DisplayMap

**Purpose** — Open a treasure/city map gump for an item; same body registered for 0x90 and 0xF5 (PacketHandlers.cs:287, 344).

**Wire** — body offset 1 (0x90 fixed 0x13 = 19; 0xF5 is 0x15 = 21).
- `@1` uint32BE serial — PacketHandlers.cs:3405
- `@5` uint16BE gumpid — :3406, passed to the MapGump ctor and then ignored by it (MapGump.cs:57)
- `@7` uint16BE startX — :3407; `@9` uint16BE startY — :3408
- `@11` uint16BE endX — :3409; `@13` uint16BE endY — :3410
- `@15` uint16BE width — :3411; `@17` uint16BE height — :3412
- `@19` uint16BE facet — read ONLY when `p[0] == 0xF5` — :3423-3425

**Mutates**
- `MapGump.mapX/mapY/mapEndX/mapEndY/mapFacet` via `MapInfos` — MapGump.cs:186-190, called from PacketHandlers.cs:3430 or :3436
- `MapGump._mapTexture`, `Width`, `Height`, `WantUpdateSize` via `SetMapTexture` — MapGump.cs:172-178
- UIManager gump list gains the gump — PacketHandlers.cs:3448
- `Item.Opened = true` on the map item — PacketHandlers.cs:3454

**Creates** — `MapGump` — :3414; a `Texture2D` for the multimap image (GPU resource) — MultiMap.cs:29; `ResizePic`, 3 Buttons, HitBox, MenuButton, ContextMenuControl inside the ctor — MapGump.cs:67-136.

**Destroys** — `MapGump._mapTexture` disposed before being replaced — MapGump.cs:172 (no prior texture on a freshly built gump).

**Triggers** — `Log.Error` + `Console.WriteLine` on texture failure — PacketHandlers.cs:3444-3445.

**Ignores / partial**
- No `World.InGame` / `World.Player` guard at all — the handler runs and adds a gump whenever the packet arrives
- No existing-gump check: nothing calls `UIManager.GetGump<MapGump>(serial)?.Dispose()` first, so a repeated 0x90 for the same serial stacks another MapGump
- Facet path taken when `p[0] == 0xF5` OR `Client.Version >= CV_308Z` — :3419, but facet is only read off the wire for 0xF5 — :3423. For a 0x90 on a ≥308Z client `GetMap` is called with facet = 0 (not null) and `MapInfos` records facet 0 — :3428/:3430.
- Older clients take the null-facet path and `MapInfos` defaults facet to -1 — :3434-3436
- `MultiMap.GetMap` returns default (Texture == null) when the loaded pixels are empty or dimensions are ≤0 or >8192 — MultiMap.cs:26-27; `SetMapTexture` is then skipped (:3439) and the gump keeps the wire width/height
- Any exception in texture creation is swallowed and logged — :3442-3446 — and the gump is still added at :3448 with no map image
- `it.Opened` set only if the item is known to `World.Items` — :3452
- `startX`/`startY` are later overwritten in place by `AddPin` (MapGump.cs:217-218) using a hardcoded `Width/300f` multiplier, so the recorded origin is destroyed by the first pin

---

### 0x93 OpenBook

**Purpose** — Open (or refresh) a writable/readable book gump — the pre-AOS fixed-length form.

**Wire** — fixed 0x63 = 99 (PacketsTable.cs:188), reader seeked to offset 1.
- `@1` uint32BE serial — PacketHandlers.cs:3460
- discriminator `p[0] == 0x93` — :3461. `StackDataReader`'s indexer ignores its argument and always returns `_data[0]` (StackDataReader.cs:38: `public byte this[int index] => _data[0];`), so this reads the packet ID byte.
- `@5` uint8 → `editable` (ReadBool) — :3462
- `@6` 1 byte skipped (`p.Skip(1)`) — :3470; the second flag byte is discarded on this form
- new-gump path: `@7` uint16BE `page_count` — :3477; `@9` UTF8 fixed 60 bytes safe title — :3479; `@69` UTF8 fixed 30 bytes safe author — :3483. 1+4+1+1+2+60+30 = 99, an exact fit.
- existing-gump path: `@7` `Skip(2)` — :3497, page_count read past and NOT applied; `@9` UTF8[60] title — :3500; `@69` UTF8[30] author — :3504

**Mutates**
- New-gump path: no World state. The `ModernBookGump` ctor writes `BookPageCount` — ModernBookGump.cs:74; `IsEditable` — :75; `UseNewHeader = !old_packet` — :76 (false for 0x93); then `BuildGump`.
- Existing-gump path: `bgump.IsEditable = editable` — PacketHandlers.cs:3498 (Control.cs:123); `_titleTextBox` text + IsEditable — ModernBookGump.cs:291-292 via `SetTile`; `_authorTextBox` text + IsEditable — ModernBookGump.cs:297-298 via `SetAuthor`; `bgump.UseNewHeader = !oldpacket` → false — PacketHandlers.cs:3507 (ModernBookGump.cs:88)
- `UIManager.Gumps` — UIManager.cs:485 AddFirst, `_needSort = true` (:492); `BringOnTop` / `SetInScreen` reorder and reposition — PacketHandlers.cs:3508-3509

**Creates** — a `ModernBookGump` at X=100, Y=100 — PacketHandlers.cs:3485-3491, with `BookPageCount` straight from the wire. `BuildGump` loops `k = 1..BookPageCount` adding a page-number Label per page — ModernBookGump.cs:237-257 — so the server's page count directly sizes the control tree.

**Destroys** — nothing.

**Triggers** — OUT 0x66 book-page-data request for page 1: `Send_BookPageDataRequest(serial, 1)` — PacketHandlers.cs:3493 → OutgoingPackets.cs:3402-3427 (writes serial, 0x0001, page, 0xFFFF). Gump opened: `ModernBookGump`.

**Ignores / partial**
- No `World.InGame` / `World.Player` guard at all
- Branch chosen on `bgump == null || bgump.IsDisposed` — :3475. An existing, live gump takes the refresh path and never gets a new page count: the 2 bytes are skipped at :3497 and `BookPageCount` keeps its old value, so a server-side page-count change is ignored for an already-open book.
- The page-data request at :3493 is sent only on the create path; refreshing requests nothing
- The second flag byte is skipped (:3470), so only the first boolean decides editability
- Title and author are fixed-width 60/30 and are truncated/zero-terminated by `ReadRawString`/`ReadString` (`GetIndexOfZero`)
- `UseNewHeader` forced false on this path — :3507 / ModernBookGump.cs:76
- Both fixed-width string reads use `safe = true`, so unsafe characters are stripped

---

### 0x95 DyeData

**Purpose** — Open the hue picker for a dye tub / target.

**Wire** — registered PacketHandlers.cs:289; fixed 0x0009 (PacketsTable.cs:190), body offset 1.
- `[1..4]` uint32BE serial — :3515
- `[5..6]` skipped — :3516
- `[7..8]` uint16BE graphic — :3517
- Gump art 0x0906 is measured and the window read for centering — :3519-3522

**Mutates**
- `UIManager.Gumps` gains the gump — UIManager.cs:485 via PacketHandlers.cs:3532; `UIManager._needSort` — UIManager.cs:492
- No World / Item / Mobile / Chunk state.

**Creates** — `ColorPickerGump(serial, graphic, x, y, null)` — :3530; `LocalSerial = serial`, `ServerSerial = 0` (ColorPickerGump.cs `base(serial, 0)`), plus its GumpPic, Button, HSliderBar, ColorPickerBox and StaticPic children.

**Destroys** — an existing ColorPickerGump for that serial is disposed when already disposed or its Graphic differs — :3528.

**Triggers** — nothing immediately; the gump's OK button later sends `Send_DyeDataResponse(LocalSerial, graphic, selectedHue)` — `ColorPickerGump.OnButtonClick`, and only when `LocalSerial != 0`.

**Ignores / partial**
- No `World.InGame` and no `World.Player` guard — runs in any scene
- If a live, non-disposed ColorPickerGump already exists for the serial with the same graphic the packet is fully ignored: no reposition, no refresh — :3526
- x/y recomputed from the current window on each creation, so a user-moved gump that gets recreated snaps back to centre — :3521-3522
- `CanCloseWithRightClick` is only true for serial 0 — ColorPickerGump.cs (`serial == 0` test)

---

### 0x97 MovePlayer

**Purpose** — Force the player one step in a given direction (teleport pads, forced movement).

**Wire** — fixed 0x0002 (PacketsTable.cs:192), reader `Seek(1)`.
- `[1]` uint8 direction — PacketHandlers.cs:3543, split at :3544 into `(direction & Direction.Mask)` = low 3 bits and `(direction & Direction.Running) != 0` = bit 0x80 (Direction.cs:49-50)

**Mutates** — all state comes from `World.Player.Walk` (PlayerMobile.cs:1674) called at PacketHandlers.cs:3544:
- `Walker.StepInfos[StepsCount]` fully populated — Game/GameObjects/PlayerMobile.cs:1811-1821 / 1988-1998
- `Walker.StepsCount++` — PlayerMobile.cs:1823 / 2000
- `Mobile.Steps.AddToBack` — PlayerMobile.cs:1825 / 2002
- `Walker.WalkSequence` — PlayerMobile.cs:1841/1845 / 2020/2024
- `Walker.UnacceptedPacketsCount++` — PlayerMobile.cs:1848 / 2027
- `Walker.LastStepRequestTime` — PlayerMobile.cs:1854 / 2050
- `Mobile.LastStepTime` — PlayerMobile.cs:1801 / 1985
- `AddToTile` — PlayerMobile.cs:1850 / 2029 → GameObject.cs:224/231/236/241/244-245, Chunk.cs:178-180/285/289-291/338-352
- `bank.Items = null` / `bank.Opened = false` via CloseBank — PlayerMobile.cs:1512/1520 (from :1792 / :1976)
- Avoid path only: `Steps.Clear()` and `Offset = Vector3.Zero` — PlayerMobile.cs:1726 → Mobile.cs:300-301
- Avoid path only: X/Y/Z overwritten and object re-tiled by `SetInWorldTile` — GameObject.cs:267-271 (PlayerMobile.cs:1728)

**Creates** — a Step struct on `Mobile.Steps` — PlayerMobile.cs:1825/2002. No entity or gump.

**Destroys** — every bank item `World.RemoveItem`'d when CloseBank fires — PlayerMobile.cs:1507; bank `ContainerGump` / `GridContainer` disposed — PlayerMobile.cs:1515/1517.

**Triggers** — OUT `NetClient.Socket.Send_WalkRequest(...)` — PlayerMobile.cs:1837 / 2015: a server-driven move produces a client walk request, consuming a WalkSequence slot.

**Ignores / partial**
- `!World.InGame` → return, the direction byte is never read — PacketHandlers.cs:3538-3541
- Behaviour forks on `ProfileManager.CurrentProfile.AutoAvoidObstacules` and `Pathfinder.AutoWalking` — PlayerMobile.cs:1676; the avoid path can silently change the requested direction (PlayerMobile.cs:1721-1725) or refuse the move (:1732)
- Move dropped with no feedback if `Walker.WalkingFailed`, `Walker.LastStepRequestTime > Time.Ticks` (still inside the previous step's cooldown), `Walker.StepsCount >= Constants.MAX_STEP_COUNT`, or CV_60142+ and `IsParalyzed` — PlayerMobile.cs:1685 / 1892
- `run` OR'd with `ProfileManager.CurrentProfile.AlwaysRun` (PlayerMobile.cs:1690), so the server's Running bit can be overridden upward; :1692-1695 can then force it back to false
- `Pathfinder.CanWalk` failing on a same-direction step aborts the move outright — PlayerMobile.cs:1746-1749 / 1930-1933
- If `CanWalk` returns a different direction than requested, only a turn is performed; x/y/z are not advanced — the server's step is only partially applied — PlayerMobile.cs:1751-1754 / 1935-1938
- Mid-frame note: relinks the player in `Chunk.Tiles` and can remove items from `World.Items`, both walked by `World.Update` (World.cs:350, :424)

---

### 0x98 UpdateName

**Purpose** — Display name for an entity (and for a world-map tracked entity).

**Wire** — variable (PacketsTable.cs:193), body offset 3.
- `[3..6]` uint32BE serial — PacketHandlers.cs:3554
- `[7…]` NUL-terminated ASCII via unbounded `ReadASCII()` — :3555 (StackDataReader.cs:353)

**Mutates**
- `WMapEntity.Name` — PacketHandlers.cs:3561
- `Entity.Name` (Entity.cs:85) — PacketHandlers.cs:3568
- `NameOverheadGump._text.Text` plus Width/Height/`_background.Width`/`_background.Height`/`_textDrawOffset`/`WantUpdateSize` — NameOverheadGump.cs:160-166 (items) / :173-179 (mobiles) via `SetName()`

**Creates** / **Destroys** — nothing.

**Triggers**
- `Client.Game.SetWindowTitle(name)` — PacketHandlers.cs:3576
- `TitleBarStatsManager.ForceUpdate()` — :3579
- `UIManager.GetGump<NameOverheadGump>(serial)?.SetName()` — :3583 (NameOverheadGump.cs:114)

**Ignores / partial**
- `!World.InGame` → return — :3549
- World-map entity name written only when the WMapEntity exists AND the name is non-empty — :3559
- The Entity name is written with **no** empty check (:3568) — an empty name from the server blanks the entity's name even though the same empty name was rejected for the map entity three lines earlier
- `World.Get(serial) == null`, including an entity flagged destroyed (World.cs:551) → entity name and overhead gump untouched — :3566
- Window title refreshed only when `serial == World.Player.Serial` AND name non-empty AND name differs from the current player name — :3570-3574; the comparison is against `World.Player.Name`, which :3568 has already overwritten when the player is the target
- `TitleBarStatsManager.ForceUpdate` only when `ProfileManager.CurrentProfile?.EnableTitleBarStats == true` — :3577
- `NameOverheadGump.SetName` returns false without touching anything if `World.Get` is null, or for an item with no resolvable text — NameOverheadGump.cs:118-121, 155-158

---

### 0x99 MultiPlacement

**Purpose** — Put the client into multi (house/boat deed) placement targeting mode with a preview model.

**Wire** — registered PacketHandlers.cs:292. Version-dependent length: base `0x001A, // 0x99` (26) in PacketsTable.cs, raised to 0x1E (30) for ≥ CV_7090 (PacketsTable.cs:393) and set to 0x1A below it (PacketsTable.cs:402). Fixed → body offset 1.
- `@1` bool/uint8 `allowGround` — :3594, read into a local and **never used**
- `@2..5` uint32BE `targID` — :3595
- `@6` uint8 `flags` — :3596, read into a local and **never used**
- `@7..17` (11 bytes) skipped — :3597 does an absolute `p.Seek(18)`, not a relative Skip
- `@18..19` uint16BE `multiID` — :3598
- `@20..21` uint16BE `xOff` — :3599; `@22..23` uint16BE `yOff` — :3600; `@24..25` uint16BE `zOff` — :3601
- `@26..27` uint16BE `hue` — :3602. On a pre-CV_7090 client the packet is only 26 bytes, so `Position+2 > Length` and `ReadUInt16BE` returns 0 without advancing — hue is always 0 below 7.0.90.
- `@28..29` on 7090+ are not read.

**Mutates**
- `TargetManager.IsTargeting` — TargetManager.cs:268 = true; `cursorType` is hardcoded `TargetType.Neutral` at PacketHandlers.cs:3604 → TargetManager.cs:329, and Neutral < Cancel, so this is unconditional
- `TargetManager.TargetingState = CursorTarget.MultiPlacement` — TargetManager.cs:269
- `TargetManager.TargetingType = TargetType.Neutral` — TargetManager.cs:270
- `TargetManager._targetCursorId = targID` — TargetManager.cs:285
- `TargetManager.MultiTargetInfo` replaced with `new MultiTargetInfo(model, x, y, z, hue)` — TargetManager.cs:332-339; the previous instance is dropped

**Creates** — a `MultiTargetInfo` — TargetManager.cs:332. Indirectly on the next frame: a preview Item via `Item.Create(0)` (serial 0, from the Item pool) — GameScene.cs:999 — with `Graphic = MultiTargetInfo.Model` (:1000), `Hue = MultiTargetInfo.Hue` (:1001), `IsMulti = true` (:1002); GameScene.cs:1043 obtains a House via `World.HouseManager.TryGetHouse` and sets every component `IsHousePreview = true`, repositioning them into world tiles — GameScene.cs:1047-1052.

**Destroys** — nothing here. The preview Item and its house components are torn down by `TargetManager.CancelTarget` (TargetManager.cs:293-297, `World.HouseManager.Remove(0)`), which this handler does not call on the Neutral path.

**Triggers** — no outgoing packet, no gump. The visible effect comes from `GameScene.Update` reading this state — GameScene.cs:990-1053.

**Ignores / partial**
- `World.Player == null` → dropped — :3589-3592
- `allowGround` (@1) and `flags` (@6) parsed then discarded — the client does not restrict placement to ground or honour the server's flags byte
- `TargetManager.SetTargeting` returns doing nothing if `targeting == CursorTarget.Invalid` (TargetManager.cs:262-265); MultiPlacement is never Invalid, so that branch is dead here
- Because `IsTargeting` always becomes true on this path, the CancelTarget cleanup branch (TargetManager.cs:276-279) is never taken — an in-progress multi placement is replaced without `World.HouseManager.Remove(0)` first
- Preview suppressed entirely while `World.CustomHouseManager != null` — GameScene.cs:993: the targeting state is accepted but nothing is shown
- Below CV_7090 the hue field falls off the end of the packet and is silently read as 0

---

### 0x9A ASCIIPrompt

**Purpose** — Open a text prompt, handing the client an 8-byte token to echo back with the reply.

**Wire** — variable (PacketsTable 0x9A = -1) → reader `Seek(3)`.
- `@3` uint64BE — the prompt id / serial pair, stored whole as `PromptData.Data` — PacketHandlers.cs:3617. Read as one 64-bit big-endian value rather than two 32-bit fields.
- Nothing else is read; the ASCII text body that follows is never touched.

**Mutates** — `MessageManager.PromptData = new PromptData { Prompt = ConsolePrompt.ASCII, Data = <uint64> }` — PacketHandlers.cs:3614-3618, writing the static property at Game/Managers/MessageManager.cs:70. Any previously pending prompt is overwritten without being cancelled.

**Creates** — a `PromptData` value only. No gump; the prompt is consumed later by the chat input path.

**Destroys** — nothing; the previous `PromptData` is replaced silently.

**Triggers** — no outgoing packets, no gumps, no events.

**Ignores / partial**
- `!World.InGame` → whole packet ignored — :3609-3612; the prompt token is lost and the server is never answered
- The prompt text is never parsed or displayed by this handler

---

### 0x9E SellList

**Purpose** — List of the player's items a vendor will buy, with per-item prices.

**Wire** — variable (PacketsTable.cs:199), body offset 3.
- `[3..6]` uint32BE vendor serial — PacketHandlers.cs:3628
- `[7..8]` uint16BE `countItems` — :3635
- per item: uint32BE serial — :3654; uint16BE graphic — :3655; uint16BE hue — :3656; uint16BE amount — :3657; uint16BE price — :3658; uint16BE nameLen + ASCII[nameLen] — :3659
- `price` is read as uint16 and widened to the `uint` parameter of `AddItem` / `HandleSellPacket` — anything above 65535 cannot be represented
- No check that `countItems` entries fit in the remaining bytes.

**Mutates**
- No World entity state; `World.OPL` is only read — :3669
- `BuySellAgent.sellPackets[vendorSerial]` gets a `VendorSellInfo` created on demand — BuySellAgent.cs:188-189 — and one `VendorSellItemData` appended per item — BuySellAgent.cs:191, 298 — one call per item from PacketHandlers.cs:3679
- The `sellPackets` entry is removed again in `HandleSellPacketFinished` — BuySellAgent.cs:200 or 269
- `UIManager.Gumps`: two disposes then one add — :3643, :3645, :3706/:3708

**Creates** — `ModernShopGump(vendor, false)` — :3648 when `UseModernShopGump`, else `ShopGump(vendor, false, 100, 0)` — :3650 (both sell gumps); one `ShopItem` child per entry — ShopGump.cs:377 / ModernShopGump.AddItem :150; a `VendorSellItemData` per item — BuySellAgent.cs:298.

**Destroys** — any existing `ShopGump` for the vendor disposed at :3643 and any existing `ModernShopGump` at :3645 — both, regardless of which style is about to be built. If the sell agent fires, `UIManager.GetGump(vendorSerial)?.Dispose()` at BuySellAgent.cs:275 disposes the gump this handler just added.

**Triggers**
- `ClilocLoader.Instance.GetString` when the name parses as an integer — :3664
- `World.OPL.TryGetNameAndData` when the name is empty — :3669, falling back to `TileDataLoader.Instance.StaticData[graphic].Name` — :3673
- `BuySellAgent.HandleSellPacket` per item — :3679; `HandleSellPacketFinished(vendor)` after the loop — :3710
- OUT `Send_SellRequest(vendorSerial, sellList)` — BuySellAgent.cs:273, plus `GameActions.Print` of the total — :274: an outgoing sell packet generated directly from receiving this one

**Ignores / partial**
- `!World.InGame` → return — :3623
- Vendor serial not in `World.Mobiles` → return — :3630; no fallback to `World.Items` or `World.Get`
- `countItems <= 0` → return — :3637. This test precedes the disposes at :3643/:3645, so an empty list leaves previously open shop gumps untouched.
- Name resolution is three-way: numeric string → cliloc with `fromcliloc = true` (:3662-3666); empty string → OPL name (:3669); OPL miss → static tile name (:3673); otherwise the wire name as-is
- BuySellAgent does nothing unless `ProfileManager.CurrentProfile.SellAgentEnabled` — BuySellAgent.cs:186, 196
- `HandleSellPacketFinished` returns after only removing the pending entry when `sellItems == null` — BuySellAgent.cs:198-202
- Per-config sale amounts clamped: skipped entirely when `backpackTotal <= RestockUpTo` (BuySellAgent.cs:224-227); capped by `sellConfig.MaxAmount` (:234, :240); capped by `SellAgentMaxUniques` (:236) and `SellAgentMaxItems` (:238, :243); further reduced so the backpack keeps `RestockUpTo` (:247-254)
- `sellPackets.Remove(vendorSerial)` runs at BuySellAgent.cs:269 whether or not anything is sold, and the method returns without sending when `sellList` is empty (:271)
- When `UseModernShopGump` is true the local `gump` variable still holds the disposed old `ShopGump` reference (:3642-3643); the same profile flag guards every later use (:3681, :3705), so the disposed reference is never touched

---

### 0xA1 UpdateHitpoints

**Purpose** — Current and maximum hit points for one entity.

**Wire** — fixed 9 (PacketsTable.cs:202), body offset 1.
- `@1` uint32BE serial, resolved with `World.Get` so items (damageable objects) resolve too — PacketHandlers.cs:3715
- `@5` uint16BE `HitsMax` — the maximum is read first — :3722
- `@7` uint16BE `Hits` — :3723

**Mutates**
- `entity.HitsMax` — PacketHandlers.cs:3722 → Entity.cs:81 (plain field)
- `entity.Hits` — PacketHandlers.cs:3723 → Entity.cs:67-80; for a PlayerMobile the setter builds `PlayerStatChangedArgs` from the old value, assigns, then fires `EventSink.InvokeOnPlayerStatChange` — Entity.cs:73
- `entity.HitsRequest` promoted Pending → Received — PacketHandlers.cs:3727
- `SpellVisualRangeManager.ClearCasting` for the player: `isCasting = false`, `currentSpell = null`, `LastSpellTime = DateTime.MinValue`, `World.Player.Flags &= ~Flags.Frozen` — PacketHandlers.cs:3733 → SpellVisualRangeManager.cs:105-111
- Window title text via `TitleBarStatsManager.UpdateTitleBar` → `Client.Game.SetWindowTitle` — PacketHandlers.cs:3734
- `Entity.HitsPercentage` and the cached RenderedText are recomputed later in `Entity.Update`, not here — Entity.cs:189-195

**Creates** / **Destroys** — nothing.

**Triggers**
- `EventSink.InvokeOnPlayerStatChange` when the entity is the PlayerMobile — Entity.cs:73
- `UoAssist.SignalHits` → `PostMessage(STR_STATUS, HitsMax, Hits)` to the external UOAssist window — PacketHandlers.cs:3732 → UoAssist.cs:73-76, 434-440
- `TitleBarStatsManager.UpdateTitleBar` — :3734

**Ignores / partial**
- Returns after reading only the serial when `World.Get` is null, which includes an entity that exists but has `IsDestroyed` set — :3717-3720, World.cs:551-554
- `HitsRequest` advanced only when it is exactly Pending; None and Received are left alone — :3725-3728
- The UoAssist / ClearCasting / title-bar block runs only for `World.Player` — :3730
- `UpdateTitleBar` returns doing nothing if `CurrentProfile` is null, `EnableTitleBarStats` is off, or `World.Player` is null — TitleBarStatsManager.cs
- No `World.InGame` or `World.Player` null-check guards the handler itself; the guard is the `World.Get` result

---

### 0xA2 UpdateMana

**Purpose** — A mobile's current and maximum mana.

**Wire** — fixed 0x0009 (PacketsTable.cs:203), body offset 1.
- `[1..4]` uint32BE serial, looked up with `World.Mobiles.Get` — PacketHandlers.cs:3740
- `[5..6]` uint16BE → `mobile.ManaMax` — :3747, max written FIRST
- `[7..8]` uint16BE → `mobile.Mana` — :3748

**Mutates**
- `Mobile.ManaMax` — Mobile.cs:236 field, written at PacketHandlers.cs:3747
- `Mobile.Mana` — Mobile.cs:235 field, written at PacketHandlers.cs:3748

**Creates** / **Destroys** — nothing.

**Triggers** — `UoAssist.SignalMana()` → `_customWindow?.SignalManaUpdate()` — UoAssist.cs:83-85, called at PacketHandlers.cs:3752; `TitleBarStatsManager.UpdateTitleBar()` — :3753.

**Ignores / partial**
- `World.Mobiles.Get(serial) == null` → no effect — :3742-3745. No `World.Player` / `World.InGame` guard.
- `World.Mobiles.Get` (EntityCollection.cs:40) does not screen out `IsDestroyed` mobiles, so mana can be written onto a destroyed Mobile still in the dictionary
- No clamping: Mana is stored exactly as sent, including values above ManaMax
- UoAssist and title-bar notifications fire only for `World.Player` — :3750

---

### 0xA3 UpdateStamina

**Purpose** — A mobile's stamina and stamina maximum.

**Wire** — fixed 9 (PacketsTable 0xA3 = 0x0009), body offset 1.
- `@1` uint32BE serial — PacketHandlers.cs:3759
- `@5` uint16BE `StaminaMax` — :3766, max read first
- `@7` uint16BE `Stamina` — :3767

**Mutates**
- `mobile.StaminaMax` — PacketHandlers.cs:3766 (public field, Mobile.cs:234)
- `mobile.Stamina` — PacketHandlers.cs:3767 (public field, Mobile.cs:233)
- Window title string via `Client.Game.SetWindowTitle` — TitleBarStatsManager.cs:26

**Creates** / **Destroys** — nothing.

**Triggers** — `UoAssist.SignalStamina()` — PacketHandlers.cs:3771; `TitleBarStatsManager.UpdateTitleBar()` — :3772.

**Ignores / partial**
- Lookup is `World.Mobiles.Get`, not `World.Get` — :3759; an entity filed under `World.Items` (World.Get falls back to Items at World.cs:538) is never found and the packet is dropped
- Returns silently when the mobile is unknown — :3761-3764; no entity is created for an unseen serial
- No clamping: Stamina written verbatim and may exceed StaminaMax; no health-bar gump is refreshed from here
- UoAssist / title-bar work only when `mobile == World.Player` — :3769
- `UpdateTitleBar` returns early when `ProfileManager.CurrentProfile` is null, `EnableTitleBarStats` is false, or `World.Player` is null — TitleBarStatsManager.cs:11-19

---

### 0xA5 OpenUrl

**Purpose** — Open a URL in the system browser.

**Wire** — variable (PacketsTable.cs:206), body offset 3.
- `@3` NUL-terminated ASCII to end of packet → `url` — PacketHandlers.cs:3778 (`p.ReadASCII()`, length -1, `safe = false`, StackDataReader `ReadRawString` path)

**Mutates** — no client or world state.

**Creates** — an OS process: `ProcessStartInfo{ FileName = url, UseShellExecute = true }` on Windows — PlatformHelper.cs:54-60; `open <url>` on macOS — PlatformHelper.cs:64; `xdg-open <url>` elsewhere — PlatformHelper.cs:68.

**Destroys** — nothing.

**Triggers** — external browser/shell launch. No packets, no gumps.

**Ignores / partial**
- Nothing happens when the string is null or empty — PacketHandlers.cs:3780
- Read with `safe = false`, so no character filtering is applied to the value handed to the shell
- No scheme validation, no confirmation prompt, no `World.InGame` guard — reachable at any point in the session
- `PlatformHelper.cs:71-73` swallows any `Process.Start` exception into `Log.Error`, so a failed launch is silent

---

### 0xA6 TipWindow

**Purpose** — Push a tip-of-the-day or notice scroll to display.

**Wire** — registered PacketHandlers.cs:299; variable (PacketsTable.cs:207), body offset 3.
- `[3]` uint8 flag — :3788
- `[4..7]` uint32BE tip id — :3795
- `[8..9]` uint16BE text length — :3796, evaluated before the ASCII read in the same expression
- `[10…]` ASCII text of that length, `'\r'` replaced by `'\n'` — :3796

**Mutates** — `UIManager.Gumps` gains the gump — UIManager.cs:485 via PacketHandlers.cs:3807. No World / Item / Mobile / Chunk state.

**Creates** — `TipNoticeGump(tip, flag, str)` with X/Y set inline — :3807; `LocalSerial` = tip id, `ServerSerial` = 0 (`base(serial, 0)`); children ExpandableScroll, ScrollArea, StbTextBox and, for flag 0, two Buttons.

**Destroys** — nothing.

**Triggers** — the gump's prev/next buttons later send `Send_TipRequest((ushort)LocalSerial, 0|1)` — `TipNoticeGump.OnButtonClick`.

**Ignores / partial**
- `flag == 1` returns before the tip id or any text is read — :3790-3793; that entire class of notice is dropped
- `flag == 0` positions at 200,100; every other value (≥2) at the default 20,20 — :3798-3805
- Only `flag == 0` gets the prev/next buttons and title gump 0x9CA; anything else gets title 0x9D2 and no navigation — TipNoticeGump.cs `type == 0` branch
- No `UIManager.GetGump` lookup, so repeated 0xA6 packets stack unlimited gumps
- No `World.InGame` guard
- The tip id is stored in a uint `LocalSerial` but truncated to ushort when the response is sent — `TipNoticeGump.OnButtonClick`

---

### 0xA8 ServerListReceived

**Purpose** — Shard list; the client rebuilds its server array and, with autologin on, immediately picks one.

**Wire** — variable (PacketsTable.cs:209), body offset 3. The handler reads nothing and forwards the reader by ref to `LoginScene.ServerListReceived` — PacketHandlers.cs:6325.
- `@3` uint8 flags — LoginScene.cs:631, read into a local and **never used**
- `@4` uint16BE count — LoginScene.cs:632
- then per entry, `ServerListEntry.Create` — LoginScene.cs:989-994: uint16BE Index; ASCII fixed 32 bytes safe Name; uint8 PercentFull; uint8 Timezone; uint32BE Address. 40 bytes per entry.

**Mutates**
- `LoginScene.Servers = new ServerListEntry[count]` — LoginScene.cs:634 (property at :83); slots filled at :638
- `LoginScene.CurrentLoginStep = LoginSteps.ServerSelection` — LoginScene.cs:641
- Autologin path, `SelectServer` (LoginScene.cs:433-457): `ServerIndex` — :441; `Settings.GlobalSettings.LastServerNum = (ushort)(1 + ServerIndex)` — :447; `LastServerName` — :448; `Settings.GlobalSettings.Save()` writes to disk — :449; `CurrentLoginStep = LoginSteps.LoginInToServer` — :451; `World.ServerName` — :453
- Per entry: `_ipAddress` and `_ipAddressLittleEndian` (big-endian and byte-reversed forms of the same Address) — LoginScene.cs:999-1022; `_pinger.PingCompleted += PingerOnPingCompleted` — :1028

**Creates** — a fresh `ServerListEntry[]` of the server-declared count (LoginScene.cs:634); one `ServerListEntry` per shard (LoginScene.cs:987-1031), each allocating a `System.Net.NetworkInformation.Ping` (:978), two IPAddress objects and a PingCompleted subscription.

**Destroys** — every previous entry: `DisposeAllServerEntries` (LoginScene.cs:956-971) calls `Dispose()`, nulls the slot and sets `Servers = null` — done at LoginScene.cs:633 before the new array is allocated; the old Ping objects and event subscriptions go with them.

**Triggers**
- OUT 0x5D-family select-server: `NetClient.Socket.Send_SelectServer(index)` — LoginScene.cs:455, reached only via the autologin path
- Settings file write — LoginScene.cs:449
- No gumps here; the login UI reacts to `CurrentLoginStep` on its next update

**Ignores / partial**
- `World.InGame` → dropped outright — PacketHandlers.cs:6316-6319
- `GetScene<LoginScene>() == null` → dropped — PacketHandlers.cs:6323
- Leading flags byte parsed and discarded — LoginScene.cs:631
- Automatic `SelectServer` only when `CanAutologin` (LoginScene.cs:90: `_autoLogin || Reconnect`) — LoginScene.cs:643; otherwise the client stops at ServerSelection
- Skipped when `count == 0` — LoginScene.cs:645
- `GetServerIndexFromSettings` (LoginScene.cs:415-431) prefers `LastServerName`, falls back to `LastServerNum`, and **clamps** an out-of-range index to 0 (:425-428) — a stale saved server silently becomes the first in the list
- `SelectServer` is a no-op unless `CurrentLoginStep == LoginSteps.ServerSelection` at call time (LoginScene.cs:435), and silently keeps `ServerIndex` if no entry matches (:437-445)
- `ServerListEntry.Create` swallows IPAddress construction failures into `Log.Error` (LoginScene.cs:1024-1027), leaving `_ipAddress` null — the entry survives unpingable and `DoPing` does nothing (:1046)
- Name is fixed 32 bytes with `safe = true`, so unsafe characters are stripped and the string is cut at the first zero byte
- No validation that the wire carries `count * 40` bytes; StackDataReader bounds checks return 0/empty past the end, so a short packet yields zero-filled entries rather than throwing

---

### 0xA9 ReceiveCharacterList

**Purpose** — Account's character slots, starting cities, and the server's client-feature flags.

**Wire** — variable (PacketsTable.cs:210), body offset 3. The handler reads nothing and forwards to `LoginScene.ReceiveCharacterList` — PacketHandlers.cs:6370 → LoginScene.cs:677.
- `[3]` uint8 character count — LoginScene.cs:759
- per character: ASCII(30) name with trailing `'\0'` trimmed, then `Skip(30)` for the unused password field — LoginScene.cs:764-766
- uint8 city count — LoginScene.cs:772
- per city when `Client.Version >= CV_70130`: uint8 index; ASCII(32) name; ASCII(32) building; uint32BE x cast to ushort; uint32BE y cast to ushort; uint32BE z cast to sbyte; uint32BE map index; uint32BE description cliloc; `Skip(4)` — LoginScene.cs:798-806
- per city otherwise: uint8 index; ASCII(31) name; ASCII(31) building; description taken from a local text file rather than the wire — LoginScene.cs:823-825, 780
- uint32BE feature flags — LoginScene.cs:682

**Mutates**
- `LoginScene.Characters` replaced with `new string[count]` — LoginScene.cs:760, filled :764
- `LoginScene.Cities` replaced with `new CityInfo[count]` — LoginScene.cs:773
- `World.ClientFeatures.Flags` — ClientFeatures.cs:70; `MaxChars` — :74/:78/:82; `PopupEnabled` — :85; `TooltipsEnabled` — :87; `PaperdollBooks` — :89
- `LoginScene.CurrentLoginStep = LoginSteps.CharacterSelection` — LoginScene.cs:683
- `LoginScene._autoLogin` cleared — LoginScene.cs:692

**Creates** — `CityInfo` instances — LoginScene.cs:808, 827. No world entities; this runs before the world exists.

**Destroys** — the previous Characters and Cities arrays are dropped by reassignment — LoginScene.cs:760, 773.

**Triggers** — `SelectCharacter(charToSelect)` — LoginScene.cs:714, which drives the login flow onward; `StartCharCreation()` — LoginScene.cs:718.

**Ignores / partial**
- `World.InGame` → whole packet discarded — PacketHandlers.cs:6361
- `GetScene<LoginScene>() == null` → nothing happens — PacketHandlers.cs:6368
- City x, y and z arrive as 32-bit and are truncated to ushort/ushort/sbyte — LoginScene.cs:801-803
- The pre-70130 branch ignores the wire description entirely and reads descriptions from a local city text file — LoginScene.cs:778-781
- `TooltipsEnabled` additionally requires `Client.Version >= CV_308Z`, so `CLF_PALADIN_NECROMANCER_TOOLTIPS` can be honoured for PaperdollBooks and simultaneously ignored for tooltips — ClientFeatures.cs:87-89
- `MaxChars` assigned only when one of `CLF_ONE_CHARACTER_SLOT` / `CLF_7_CHARACTER_SLOT` / `CLF_6_CHARACTER_SLOT` is set; otherwise the previous value stands — ClientFeatures.cs:72-83
- Auto-select only when `CanAutologin` AND at least one non-empty character slot exists — LoginScene.cs:712
- If the remembered last-character name matches nothing, `charToSelect` stays 0 and slot 0 is selected — LoginScene.cs:685, 697-710
- No characters at all → `StartCharCreation` instead of the selection screen — LoginScene.cs:716-719

---

### 0xAA AttackCharacter

**Purpose** — Which mobile the client is now attacking (or 0 to clear).

**Wire** — fixed 0x0005 (PacketsTable.cs:211), reader `Seek(1)`.
- `@1..4` uint32BE target serial — PacketHandlers.cs:3812; the only field read
- PacketHandlers.cs:3814-3819 hold a commented-out `if (TargetManager.LastAttack != serial && World.InGame)` guard — the current code is unguarded

**Mutates**
- `_lastAttack = value` (backing field of `TargetManager.LastAttack`) — Game/Managers/TargetManager.cs:193, written from PacketHandlers.cs:3822
- `ent.HitsRequest = HitsRequestStatus.None` on the PREVIOUS LastAttack entity — Game/GameActions.cs:1010 (`SendCloseStatus`, PacketHandlers.cs:3821)
- `ent.HitsRequest = HitsRequestStatus.Pending` on the NEW target — GameActions.cs:988 (`RequestMobileStatus`, PacketHandlers.cs:3823)
- `BaseHealthBarGump.LastAttackBar.SetNewMobile(value)` repoints the shared last-target bar — TargetManager.cs:202
- `BaseHealthBarGump.LastAttackBar` assigned to a newly constructed bar — TargetManager.cs:208/210

**Creates**
- `new HealthBarGumpCustom(value)` via UIManager.Add when `CustomBarsToggled` — TargetManager.cs:208; else `new HealthBarGump(value)` — :210
- A per-serial `HealthBarGumpCustom` / `HealthBarGump` when `UseOneHPBarForLastAttack` is off and no bar exists for that serial — TargetManager.cs:218/220
- All positioned at `ProfileManager.CurrentProfile.LastTargetHealthBarPos` with `IsLastTarget = true`

**Destroys** — nothing is destroyed or pooled; the old LastAttackBar is re-pointed (TargetManager.cs:202), not disposed.

**Triggers** — OUT `Socket.Send_CloseStatusBarGump(previous LastAttack)` — GameActions.cs:1018; OUT `Socket.Send_StatusRequest(new serial)` — GameActions.cs:997; gumps as above.

**Ignores / partial**
- No `World.InGame` / `World.Player` guard in the handler; the guards live one level down
- `SendCloseStatus` does nothing unless `Client.Version >= CV_200 && World.InGame` — GameActions.cs:1004
- The close is only forced if the old entity exists and its `HitsRequest >= Pending` (GameActions.cs:1008); :1014 additionally requires `SerialHelper.IsValid(serial)`, so closing status for serial 0 sends nothing
- `RequestMobileStatus` does nothing unless `World.InGame` — GameActions.cs:972
- The status request is only forced when the entity's `HitsRequest < Received` (GameActions.cs:986-990); with an already-received status `force` stays false (the caller passes `force:false`) and :993 suppresses the outgoing `Send_StatusRequest`
- An unknown serial with `force == false` sends nothing at all — GameActions.cs:993 — so LastAttack updates but no status is requested
- No health-bar work at all unless `ProfileManager.CurrentProfile != null && OpenHealthBarForLastAttack` — TargetManager.cs:194
- `UseOneHPBarForLastAttack` picks the shared-bar path (TargetManager.cs:196); :198 requires the existing bar non-null and not disposed; :200 skips `SetNewMobile` when the bar already points at that serial
- Per-serial path creates a bar only when `UIManager.GetGump<BaseHealthBarGump>(value)` is null — TargetManager.cs:215
- Serial 0 (server clearing the attack) still runs the whole body: LastAttack becomes 0 and, with `OpenHealthBarForLastAttack` on, a health bar for serial 0 can be constructed

---

### 0xAB TextEntryDialog

**Purpose** — Open a modal single-line text-entry dialog on the server's behalf.

**Wire** — variable (PacketsTable.cs:212), body offset 3.
- `[3..6]` uint32BE serial — PacketHandlers.cs:3833
- `[7]` uint8 parentID — :3834; `[8]` uint8 buttonID — :3835
- `[9..10]` uint16BE textLen — :3837; then ASCII(textLen) text — :3838
- `+0` bool haveCancel — :3840; `+1` uint8 variant — :3841
- `+2..+5` uint32BE maxLength — :3842
- `+6..+7` uint16BE descLen — :3844; then ASCII(descLen) desc — :3845

**Mutates** — `UIManager.KeyboardFocusControl` reassigned to the dialog's text box — TextEntryDialogGump.cs:118-119. No World / Entity state.

**Creates** — `TextEntryDialogGump` at fixed screen coordinates 143,172 — PacketHandlers.cs:3847-3860; the packet carries no position. Child controls: GumpPic background 0x0474, two Labels, GumpPic 0x0477, StbTextBox, Ok and Cancel Buttons — TextEntryDialogGump.cs:66-113.

**Destroys** — nothing; no existing dialog for the same serial is looked up or disposed, so repeated packets stack modal gumps.

**Triggers** — `UIManager.Add(gump)` — PacketHandlers.cs:3862; the gump is `IsModal` (TextEntryDialogGump.cs:61) and steals keyboard focus (:118-119); on Ok/Cancel it later sends `Send_TextEntryDialogResponse` (TextEntryDialogGump.cs:130).

**Ignores / partial**
- `!World.InGame` → return, dialog never opens — :3828
- `maxLength` arrives as uint32 and is cast to int with no range check before becoming `max_char_count` — :3852 → TextEntryDialogGump.cs:86
- `haveCancel` only controls `CanCloseWithRightClick` — :3859; the Cancel button is added regardless
- `variant` used solely as `variant == 2 → NumbersOnly` — TextEntryDialogGump.cs:90; every other variant value is ignored
- X/Y hardcoded 143,172 — :3849-3850

---

### 0xAE UnicodeTalk

**Purpose** — Unicode speech/system message attributed to an entity.

**Wire** — variable → reader `Seek(3)`; offsets absolute.
- `@3` uint32BE serial — PacketHandlers.cs:3894, resolved with `World.Get` at :3895
- `@7` uint16BE graphic — :3896, read then never used
- `@9` uint8 MessageType — :3897
- `@10` uint16BE hue — :3898; `@12` uint16BE font — :3899
- `@14` ASCII(4) language code — :3900
- `@18` `ReadASCII()` NUL-terminated speaker name — :3901. The protocol field is a fixed 30 bytes; this code reads to the first NUL and does not skip to a fixed boundary — the position is corrected by the absolute Seek below.
- `p.Seek(48)` then `ReadUnicodeBE()` = text — :3963-3967, only when `p.Length > 48`. Position 48 is hardcoded, not derived from the fields above.
- The wire `font` at :3899 is not used for display — `ProfileManager.CurrentProfile.ChatFont` is passed to HandleMessage instead — :4000.

**Mutates**
- `entity.Name = name` (or `text` if name is empty) — PacketHandlers.cs:3990, only when the entity exists and its Name is currently null/empty
- Inside `MessageManager.HandleMessage`: hue and text may be rewritten for `MessageType.Spell` — MessageManager.cs:176-206; font/unicode may be overridden by `Profile.OverrideAllFonts` — MessageManager.cs:111-115; a TextObject is created and attached via `parent.AddMessage` — MessageManager.cs:294

**Creates** — TextObject overhead message via `MessageManager.CreateMessage` → `parent.AddMessage` — Game/Managers/MessageManager.cs:242-251, 294; journal / UI entries downstream of `EventSink.InvokeMessageReceived` — MessageManager.cs:302-314.

**Destroys** — nothing.

**Triggers**
- A hardcoded 40-byte OUTGOING buffer via `NetClient.Socket.Send(buffer)` — PacketHandlers.cs:3912-3956 — after which the handler returns without displaying anything. Fires on the exact signature `serial==0 && graphic==0 && type==Regular && font==0xFFFF && hue==0xFFFF && name.ToLower()=="system"`.
- `EventSink.InvokeRawMessageReceived` — MessageManager.cs:98-109; `EventSink.InvokeMessageReceived` — :302-314
- `GridContainer.HandleObjectMessage` and `ModernPaperdoll.HandleObjectMessage` for every non-disposed such gump when `textType == OBJECT` — MessageManager.cs:218-228
- `Log.Warn` twice during LoginScene — PacketHandlers.cs:3880, 3886

**Ignores / partial**
- `!World.InGame`: if a LoginScene exists it only logs and (if `Length > 48`) Seeks 48 and logs again — the message is never displayed — :3867-3892; it returns in all `!InGame` cases
- The "system" handshake branch (:3903-3959) consumes the packet, sends the fixed blob, and returns — no message reaches the journal
- Text read only when `p.Length > 48` — :3963; shorter packets yield `text = string.Empty`, and `HandleMessage` returns immediately on the empty-text check — MessageManager.cs:85-88
- `text_type` stays `TextType.SYSTEM` (not attributed to the entity) when type is System, serial is 0xFFFFFFFF, serial is 0, or the name is "system" and the entity is unknown — :3975-3983
- `entity.Name` filled only when currently empty — :3988; an existing name is never overwritten
- Wire `font` ignored in favour of the profile ChatFont — :4000; wire `graphic` read and discarded
- Inside HandleMessage: early return for Guild when `Profile.IgnoreGuildMessages` (MessageManager.cs:166), Alliance when `IgnoreAllianceMessages` (:170), `TextType.OBJECT` when `ForcedTooltipManager` claims it (:94-95); breaks without overhead text when the speaker is on `IgnoreManager.IgnoredCharsList` (:149-150, 239-240) or `parent == null` (:233-235)
- Party messages dropped entirely unless `Profile.DisplayPartyChatOverhead` — MessageManager.cs:127-128

---

### 0xAF DisplayDeath

**Purpose** — A mobile (never the player) has died; names its corpse and whether it died running.

**Wire** — registered PacketHandlers.cs:302; fixed 0x000D (13) — reader starts at offset 1.
- `@1..4` uint32BE `serial` (the dying mobile) — PacketHandlers.cs:4014
- `@5..8` uint32BE `corpseSerial` — :4015
- `@9..12` uint32BE `running` — :4016, read as a full 32-bit value and used only as `running != 0`
- Reader ends at 13 = packet length.

**Mutates**
- Local serial rewritten as `serial |= 0x80000000` — PacketHandlers.cs:4025. Per Game/SerialHelper.cs:41-56 that value is not `IsValid` (≥0x80000000), not `IsMobile` and not `IsItem`.
- `World.Mobiles.Remove(owner.Serial)` — PacketHandlers.cs:4027; this is the dictionary the world sweep enumerates at World.cs:350
- Every child item's `it.Container` repointed at the new out-of-range serial — PacketHandlers.cs:4032 (Item.cs:242)
- `World.Mobiles[serial] = owner` — PacketHandlers.cs:4035: the same mobile re-filed under the 0x8000_0000-ORed key
- `owner.Serial = serial` (Entity.cs:86 public field) — PacketHandlers.cs:4036: the live object's identity changed in place while still linked into its Chunk tile and still referenced by every gump holding its old serial
- `World.CorpseManager`: PacketHandlers.cs:4041 → CorpseManager.cs:55 `_corpses.AddToBack(new CorpseInfo(corpse, obj, dir, run))` with `obj` = the ORed serial
- `owner.SetAnimation(group, 0, 5, 1)` — PacketHandlers.cs:4055 → Mobile.cs:393-404 writes `_animationGroup`, `AnimIndex` (= 5, since forward defaults false and frameCount is 5), `_animationInterval = 0`, `AnimationFrameCount = 5`, `_animationRepeateMode = 1`, `_animationRepeatModeCount = 1`, `_animationRepeat = false`, `_isAnimationForwardDirection = false`, `AnimationFromServer = false`, `LastAnimationChangeTime = Time.Ticks`, then `CalculateRandomIdleTime()`
- `owner.AnimIndex = 0` — PacketHandlers.cs:4056, overwriting the 5 SetAnimation just wrote
- `World.Player.AutoOpenedCorpses` gains entries via `TryOpenCorpses` — PlayerMobile.cs:1453

**Creates** — a `CorpseInfo` in `CorpseManager._corpses` — CorpseManager.cs:55. No Entity is created; the corpse Item arrives separately via 0x1A/0xF3.

**Destroys** — nothing is destroyed or pooled; the dying mobile object is kept alive, only re-keyed.

**Triggers**
- `Client.Game.Animations.ConvertBodyIfNeeded(ref gfx)` — :4045 (Animation.cs:343), called with `isCorpse` defaulting to FALSE, so it runs `ReplaceBody` (Animation.cs:357), not `ReplaceCorpse`
- `AnimationsLoader.Instance.GetDeathAction(gfx, animFlags, animGroup, running != 0, true)` — :4048-4054 (AnimationsLoader.cs:1220); the packet's running value is passed as the `second` parameter and the `isRunning` parameter is the hardcoded literal `true`
- `World.Player.TryOpenCorpses()` — :4060 → PlayerMobile.cs:1435, which iterates `World.Items.Values` (:1449) and calls `GameActions.DoubleClickQueued` (:1454) for each in-range unopened corpse — each becomes an outgoing double-click and, later, an inbound 0x24

**Ignores / partial**
- `!World.InGame` → dropped — :4009-4012
- Dropped when the mobile is unknown OR `serial == World.Player` — :4020-4023; the player's own death is not handled here at all and no corpse is registered for it
- The re-key block (items' Container, dictionary insert, Serial write) runs only if `World.Mobiles.Remove` succeeded — :4027. If it did not, :4029-4036 are skipped but :4041 still registers the corpse against an ORed serial that was never assigned to anything.
- The corpse is registered only when `SerialHelper.IsValid(corpseSerial)` — :4039 (i.e. > 0 and < 0x80000000)
- A second 0xAF naming the same corpse serial is ignored outright — CorpseManager.cs:45-53
- `TryOpenCorpses` gated on `Profile.AutoOpenCorpses` at :4058, re-tested at PlayerMobile.cs:1437; :1439-1447 further refuses while targeting (CorpseOpenOptions 1 or 3) or while hidden (CorpseOpenOptions 2 or 3)
- :4058 dereferences `ProfileManager.CurrentProfile` without a null check
- Concurrency note: :4027 and :4035 remove from and insert into `World.Mobiles`, the exact Dictionary `World.Update` walks with `foreach` at World.cs:350 and whose removals it defers into `_toRemove` to avoid mutating during enumeration (World.cs:396-412). In this build `ProcessNetworkPackets` (GameController.cs:478) runs before `Scene.Update` → `World.Update` (GameScene.cs:913) on the same thread, so the two are ordered within a frame; the re-key still leaves a live object filed under a key that is neither a valid item nor mobile serial, which the sweep at World.cs:356/402 then treats as an ordinary entry.
