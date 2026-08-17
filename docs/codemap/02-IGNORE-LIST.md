# 02 — IGNORE LIST

Every point in this client where **what the server said is not what the client does**, or where
the client keeps / invents / re-derives state the server is authoritative over.

Root: `/home/user/TazUO-Holiday-Edition`. All paths below are relative to
`src/ClassicUO.Client/`. Line numbers are as of the working tree read for this document.

Sections:

- [A. Packets ignored outright](#a-packets-ignored-outright-empty-handler-bodies)
- [B. Whole-packet guard drops](#b-whole-packet-guard-drops)
- [C. Partial apply — fields parsed then discarded](#c-partial-apply--fields-parsed-then-discarded)
- [D. Clamps, floors, caps, masks and substitutions](#d-clamps-floors-caps-masks-and-substitutions)
- [E. Client-side distance culling](#e-client-side-distance-culling)
- [F. Retention past the server's range](#f-retention-past-the-servers-range)
- [G. Deferral — server position queued, not applied](#g-deferral--server-position-queued-not-applied)
- [H. Settings whose effect is to ignore or override the server](#h-settings-whose-effect-is-to-ignore-or-override-the-server)
- [I. Client-initiated re-request, resync and self-issued queries](#i-client-initiated-re-request-resync-and-self-issued-queries)
- [J. Client-invented / client-derived state](#j-client-invented--client-derived-state)
- [K. Playback and interpolation at rates the server did not specify](#k-playback-and-interpolation-at-rates-the-server-did-not-specify)
- [Load-bearing](#load-bearing)

---

## A. Packets ignored outright (empty handler bodies)

Each is still registered, so the bytes are consumed and framing is preserved; nothing else happens.

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| A1 `0x03` | `Network/PacketHandlers.cs:532` (cases 536-546) | client talk echo, 4 sub-cases | all four switch cases are empty bodies | always | — |
| A2 `0x32` | `Network/PacketHandlers.cs:2097` | (unknown) | body empty | always | — |
| A3 `0x5B` | `Network/PacketHandlers.cs:2542` | server clock time | discarded; the client keeps its own clock | always; registered at `:270` | — |
| A4 `0xB7` | `Network/PacketHandlers.cs:4274` | help request response | body empty; registered at `:306` | always | — |
| A5 `0xBB` | `Network/PacketHandlers.cs:4367` | Ultima Messenger | body empty; registered `:310`, drains 9 bytes | always | — |
| A6 `0xBE` | `Network/PacketHandlers.cs:4412` | assist version | body empty | always | — |
| A7 `0xC4` | `Network/PacketHandlers.cs:5167` | semivisible | body empty; registered `:317`, drains 6 bytes | always | — |
| A8 `0xC6` | `Network/PacketHandlers.cs:5169` | invalid-map enable | body empty; registered `:318` | always | — |
| A9 `0xCA` | `Network/PacketHandlers.cs:5173` | god-client server ping | body empty | always | — |
| A10 `0xCB` | `Network/PacketHandlers.cs:5175` | global queue count | body empty | always | — |
| A11 `0xD0` | `Network/PacketHandlers.cs:5177` | configuration file | body empty | always | — |
| A12 `0xD7` | `Network/PacketHandlers.cs:5377` | **every** AOS generic-command response, all sub-commands | body empty; registered `:330` | always | — |
| A13 `0xDB` | `Network/PacketHandlers.cs:5668` | character transfer log | body empty | always | — |
| A14 `0xDE` | `Network/PacketHandlers.cs:5815` | mobile status / attacker serial | parsed at `:5820-5823` then discarded; no guard of any kind | always | — |
| A15 `0xE3` | `Network/PacketHandlers.cs:5958` | KR encryption response | body empty; registered `:338` | always | — |
| A16 `0xF1` | `Network/PacketHandlers.cs:6074` | freeshard list | body empty | always | — |

### A′. Sub-commands ignored inside handlers that otherwise act

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| A17 `0x56` Insert/Move/Remove/Edit | `Network/PacketHandlers.cs:2519-2532` | map pin insert, move, remove, edit | four empty `case` bodies; only Add acts | always | — |
| A18 `0xBF` cmd 0 / cmd 0x11 | `Network/PacketHandlers.cs:4429-4430`, `:4637-4638` | — | explicit no-ops | always | — |
| A19 `0xBF` cmd 0x20 types 1,2,3 | `Network/PacketHandlers.cs:4896-4903` | house "update", "remove", "update multi pos" | empty branches | always | — |
| A20 `0xBF` cmd 0xBEEF | `Network/PacketHandlers.cs:5028-5032` | uint16 payload | read and discarded | always | — |
| A21 `0xE5` 11 of 13 waypoint types | `Network/PacketHandlers.cs:5977-6001` | PartyMember, RallyPoint, QuestGiver, QuestDestination, PointOfInterest, Landmark, Town, Dungeon, Moongate, Shop, Player waypoints | empty case bodies; only Corpse (0x01) and Resurrection (0x06) act | always | — |
| A22 `0xF0` subtypes 0x03, 0x04, 0xF0 | `Network/PacketHandlers.cs:6057-6063` | runebook contents, guardline data | registered cases that do nothing | always | — |
| A23 `0xF0` subtype 0x02 with `locations == false` | `Network/PacketHandlers.cs:6033` | party/guild serial list | list walked to its terminator, nothing stored | `locations == false` | — |
| A24 `0xB2` 0x03EE / 0x03EF / 0x03F0 | `Network/PacketHandlers.cs:4124` handler | chat user lists | payloads parsed, nothing applied — no user list is kept | always | — |
| A25 `0x16/0x17` type 3 | `Network/PacketHandlers.cs:862-865` | healthbar status type 3 | 3 bytes consumed, empty branch commented `???` | always | — |
| A26 `0x2E` layers 0x1A-0x1C | `Network/PacketHandlers.cs:2018-2021` | equip on ShopBuyRestock / ShopBuy / ShopSell | deliberately empty branch; the `item.Clear()` is commented out | layer in 0x1A..0x1C | — |
| A27 `0xBF` sub-4 gump close | `Network/PacketHandlers.cs:4461` | close gump `ser` | only closed when `ServerSerial == ser` **and** `IsFromServer` | else ignored | — |
| A28 `0x2C` action==1 | `Network/PacketHandlers.cs:1908` | death screen, action 1 | everything skipped; only `action != 1` is treated as death | `action == 1` | — |
| A29 `0xA6` flag==1 | `Network/PacketHandlers.cs:3790-3793` | tip/notice, flag 1 | returns before the tip id or text is read | `flag == 1` | — |
| A30 `0x21` sequence byte | `Network/PacketHandlers.cs:1296` → `Game/Managers/WalkerManager.cs:108` | deny-walk sequence number | read and passed to `DenyWalk`, which never uses it | always | — |
| A31 `0x1D` party branch | `Network/PacketHandlers.cs:1216` | delete a party member | branch is a no-op — both `m.RemoveFromTile()` and its alternative are commented out | always | — |
| A32 `0x78`/`0xD3` death screen send | `Network/PacketHandlers.cs:3182` | player died | `Send_DeathScreen` is commented out with a note about sphere servers | always | — |
| A33 `0x20` target reset | `Network/PacketHandlers.cs:6854-6860` | body change | target-reset commented out: "std client keeps the target open!" | always | — |
| A34 `0x2E` ItemHold reconciliation | `Network/PacketHandlers.cs:2036-2041` | equip confirms a held item | upstream's ItemHold reconciliation is commented out | always | — |
| A35 `0x24` `ClearContainerAndRemoveItems` | `Network/PacketHandlers.cs:1991` | open container | commented out on the spellbook path | always | — |
| A36 `0x70` ScreenFade | `Network/PacketHandlers.cs:2673-2681` | screen fade value | 8 bytes skipped, value read, clamped to 4, logged, **discarded** | `p[0]==0x70 && type==ScreenFade` | — |
| A37 `0xC7` trailer | `Network/PacketHandlers.cs:2709-2714` | tile id, explode effect, explode sound, serial, layer | all five parsed and thrown away | always for 0xC7 | — |
| A38 `0xCE` header | `Game/Managers/EnhancedPacketHandler.cs:54-55`, `:63-64` | enhanced-packet id + version | id and ver consumed, then dropped if `EnhancedPacketsEnabled` is false | setting off | on unless `Data/DISABLE_ENHANCED_PACKETS` exists (`Configuration/Settings.cs:163-174`) |
| A39 `0xCE` late enable | `Game/Managers/EnhancedPacketHandler.cs:17-20` | — | sub-handler registered only if the setting was true at **type-initialisation** time; turning it on later never registers it | setting flipped after init | as above |

---

## B. Whole-packet guard drops

The packet arrives, the guard fails, nothing is applied and the server is never told.

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| B1 `0x0B` Damage | `Network/PacketHandlers.cs:559` | damage to `serial` | dropped; the damage ushort is never read | `World.Get(serial) == null` — includes an entity already culled for distance by E1/E2 | — |
| B2 `0x0B` Damage | `Network/PacketHandlers.cs:563` | damage of 0 | no overhead, no journal, no event | `damage == 0` | — |
| B3 `0x11` CharacterStatus | `Network/PacketHandlers.cs:581` | full status block | dropped | `World.Get(serial) == null`, which `Game/World.cs:551-554` also returns for a found-but-`IsDestroyed` entity | — |
| B4 `0x11` | `Network/PacketHandlers.cs:602` | status for a mobile serial | returns **after** Name/Hits/HitsMax/HitsRequest have already been written | serial is mobile-range but the entity resolved to an `Item` | — |
| B5 `0x11` non-player | `Network/PacketHandlers.cs:612` | str/dex/int and every extended stat | read-skipped entirely for any mobile that is not the player | `mobile != World.Player` | — |
| B6 `0x16/0x17` | `Network/PacketHandlers.cs:814-817` | poison / yellow-bar status | dropped; no fallback to `World.Items` / `World.Get`, no request for the unknown mobile | `World.Mobiles.Get(serial) == null` | — |
| B7 `0x16` | `Network/PacketHandlers.cs:807-810` | status | dropped | `p[0]==0x16 && Client.Version < CV_500A` | — |
| B8 `0x1D` DeleteObject | `Network/PacketHandlers.cs:1114` | delete the player | ignored outright | `serial == World.Player` | — |
| B9 `0x1D` DeleteObject | `Network/PacketHandlers.cs:1209-1212` | delete `serial` | **hard deferral**: returns having done only gump refreshes. Not removed from `World.Items`/`World.Mobiles`, not destroyed, not pooled | `World.CorpseManager.Exists(0, serial)` — anything playing a death animation (`Game/Managers/CorpseManager.cs:91`) | — |
| B10 `0x21` DenyWalk | `Network/PacketHandlers.cs:1291` | walk denied | dropped without reading | `World.Player == null` | — |
| B11 `0x24` OpenContainer | `Network/PacketHandlers.cs:1430-1433`, `:1453-1456` | open spellbook / vendor | aborts; the whole vendor buy list is discarded | container item / vendor mobile unknown | — |
| B12 `0x24` OpenContainer | `Network/PacketHandlers.cs:1540` | open this corpse | **every** gump-creation path is skipped — server said open, client opens nothing | `NearbyLootGump.IsCorpseRequested(serial)` (`Game/UI/Gumps/NearbyLootGump.cs:264`, which also *removes* the entry as a side effect of the test) | — |
| B13 `0x24` OpenContainer | `Network/PacketHandlers.cs:1554-1557` | open container | returns from the handler entirely: no `OnOpenContainer` event, no position cache removal, `Opened` never set | `GridLootType == 1` | `GridLootType` default 0 (`Configuration/Profile.cs:313`) |
| B14 `0x24` vendor shop layer | `Network/PacketHandlers.cs:1477-1481` | vendor stock on a shop layer | layer skipped silently | that container item has no children | — |
| B15 `0x25` UpdateContainedItem | `Network/PacketHandlers.cs:6417-6423` | item is in container X | logged "No container found", item not created, no request sent | `World.Get(containerSerial) == null` | — |
| B16 `0x27` DenyMoveItem | `Network/PacketHandlers.cs:1753-1757`, `:1855` | move denied, restore the item | only logs "There was a problem with ItemHold object"; the denial is otherwise not acted on | `ItemHold.Enabled` false and not the dropped-first-item case | — |
| B17 `0x27` | `Network/PacketHandlers.cs:1769` | restore | skipped — client assumes the world copy is already right | `ItemHold.UpdatedInWorld == true` | — |
| B18 `0x27` | `Network/PacketHandlers.cs:1865` | error code >= 5 | error text suppressed entirely (the `ServerErrorMessages.cs:112-118` clamp is unreachable from here) | `code >= 5` | — |
| B19 `0x2D` MobileAttributes | `Network/PacketHandlers.cs:1941-1944`, `:1958-1961` | hits/mana/stam | dropped, or hits applied and mana/stam dropped | entity null / destroyed, or mobile serial resolving to an Item | — |
| B20 `0x2D` | `Network/PacketHandlers.cs:1954` | mana and stamina | last 8 wire bytes ignored entirely for an item serial | `!SerialHelper.IsMobile(serial)` | — |
| B21 `0x2F` Swing | `Network/PacketHandlers.cs:2055-2058` | a swing happened | dropped; the defender serial is not even read | `attackers != World.Player` — the player as defender or observer is discarded | — |
| B22 `0x2F` Swing | `Network/PacketHandlers.cs:2064-2069` | face the defender | no effect unless **all** of: `LastAttack == defenders`, `InWarMode`, `LastStepRequestTime + 2000 < Ticks`, `Steps.Count == 0` | else | — |
| B23 `0x38` Pathfinding | `Game/Pathfinder.cs:1006-1009`, `:1025-1033` | walk to x,y,z | refused outright, or `AutoWalking=false` and the client never moves; server is not told | player null / paralyzed, or no path within `PATHFINDER_MAX_NODES` | — |
| B24 `0x3A` UpdateSkills | `Network/PacketHandlers.cs:2197`, `:2201` | a skill entry | fully parsed then silently discarded — no growth, no log | `id >= Skills.Length` or `Skills[id] == null` | — |
| B25 `0x3A` | `Network/PacketHandlers.cs:2261-2264` | multiple entries in a single-update packet | only the **first** entry is applied | `isSingleUpdate` (type 0xFF/0xDF) | — |
| B26 `0x3A` | `Network/PacketHandlers.cs:2246-2254` | skill Base/Value/Cap changed | change events fire **only** on `isSingleUpdate`; a full list rewrite at `:2241-2244` fires nothing | full-list packet | — |
| B27 `0x3B` CloseVendorInterface | `Network/PacketHandlers.cs:2322-2331` | close the shop | only `ShopGump` is disposed; `ModernShopGump` (created at `:3648`) is never looked up and stays open. Only the topmost match is disposed | `UseModernShopGump` on | default **off** (`Configuration/Profile.cs:531`) |
| B28 `0x3C` UpdateContainedItems | `Network/PacketHandlers.cs:2308-2316` | contents of several containers | container is cleared **only for record 0**; later containers in the same packet keep their previous contents | count > 1 | — |
| B29 `0x55` LoginComplete | `Network/PacketHandlers.cs:2449` | you are in the world | entire handler is a no-op | `World.Player == null` or the scene is not `LoginScene` | — |
| B30 `0x56` MapData | `Network/PacketHandlers.cs:2503-2505` | map pin data | message-type byte never read, whole packet dropped silently | no `MapGump` with that `LocalSerial` | — |
| B31 `0x65` SetWeather | `Network/PacketHandlers.cs:2556` | weather type/count/temp | count and temp bytes never read, `Generate` never called | incoming type equals `weather.CurrentWeather` | — |
| B32 `0x66` BookData | `Network/PacketHandlers.cs:2576-2581` | book page text | discarded without being parsed at all | no `ModernBookGump` for the serial, or it is disposed | — |
| B33 `0x66` | `Network/PacketHandlers.cs:2588`, `:2619-2624`, `:2596` | page N of the book | logged and skipped **without reading `lineCnt` or the lines** — the reader is left inside that page's payload, so every subsequent page in the packet is parsed from the wrong offset | `pageNum >= BookPageCount` or `< 0`; or line index >= `BookLines.Length` | — |
| B34 `0x6C` TargetCursor | `Game/Managers/TargetManager.cs:262-265` | open a target cursor | returns changing nothing | `cursorTarget == CursorTarget.Invalid` | — |
| B35 `0x6C` | `Network/PacketHandlers.cs:450-458` | cursor opened | the queued auto-target is **not** sent on a mismatch, but `NextAutoTarget` is cleared anyway ("no queuing") | `ExpectedCursorTarget != cursorTarget` or `ExpectedTargetType != targetType` | — |
| B36 `0x6D` PlayMusic | `Game/Managers/AudioManager.cs:298`, `:303`, `:329-332`, `:336`, `:347` | play track N | silently returns | audio unavailable, `music >= 150`, combat music off in war mode, volume out of `[-1,1]`, or the same instance already current | `EnableCombatMusic` default **on** |
| B37 `0x6E` CharacterAnimation | `Network/PacketHandlers.cs:2634-2637` | play animation on `serial` | returns with no effect; no `World.InGame` / `World.Player` guard, and `World.Mobiles.Get` does **not** screen `IsDestroyed` (`Game/Managers/EntityCollection.cs:40` vs `Game/World.cs:551-554`) | mobile not in `World.Mobiles` | — |
| B38 `0x6F` SecureTrading | `Network/PacketHandlers.cs:478-481` | open a trade window | aborts after reading the two serials; the name is never read | either trader invisible (`World.Get` null) | — |
| B39 `0x6F` | `Network/PacketHandlers.cs:495`, `:504`, `:516` | trade accept / gold / platinum | nothing happens | `UIManager.GetTradingGump(serial) == null` | — |
| B40 `0x70/0xC0/0xC7` GraphicEffect | `Network/PacketHandlers.cs:2666-2682` | effect of type > `FixedFrom` (3) | returns without spawning — `ScreenFade` (4) and `DragEffect` (5) can **never** be produced from these packets, even though `EffectManager` has cases for them | `type > 3` | — |
| B41 effects | `Game/Managers/EffectManager.cs:98`, `:135`, `:183`, `:207` | effect graphic 0 | Moving, Drag, FixedXYZ and FixedFrom all abort without creating anything | `graphic <= 0` | — |
| B42 `0x71` BulletinBoardData | `Network/PacketHandlers.cs:2759-2761`, `:2784-2788`, `:2815-2819` | board name / message summary / message body | nothing at all happens; the strings are never even read | board item unknown (`World.Items.Get` only, not `World.Get`), or no `BulletinBoardGump` for that serial | — |
| B43 `0x71` | `Network/PacketHandlers.cs:2753-2882` | subcommand other than 0/1/2 | falls out of the switch, no default branch | always | — |
| B44 `0x74` BuyList | `Network/PacketHandlers.cs:2944` | prices for the vendor's stock | gump still created but **the entire price list is never parsed** and every price is dropped | container Layer is neither ShopBuyRestock nor ShopBuy | — |
| B45 `0x74` | `Network/PacketHandlers.cs:2975-2978` | N priced entries | loop breaks the moment the client's local item list runs out; the rest of the wire data is abandoned | server sent more entries than the client holds | — |
| B46 `0x77`/`0xD2` UpdateCharacter | `Network/PacketHandlers.cs:3029-3032` | a mobile's position/graphic/notoriety | creates nothing, discards everything including notoriety | mobile not already in `World.Mobiles` | — |
| B47 `0x7C` OpenMenu | `Network/PacketHandlers.cs:3228-3244` | a menu entry | entry skipped entirely — not added to the gump, `posX` not advanced; the user cannot pick it | `artInfo.UV.Width == 0` or `UV.Height == 0` | — |
| B48 `0x88` OpenPaperdoll | `Network/PacketHandlers.cs:3306-3309` | show paperdoll | dropped; `World.Get` is not used, so an item serial or a mobile filed under Items is never found | `World.Mobiles.Get(serial) == null` | — |
| B49 `0x89` CorpseEquipment | `Network/PacketHandlers.cs:3378-3381` | the corpse's equipment list | the entire list is discarded | `corpse.Graphic != 0x2006` | — |
| B50 `0x89` | `Network/PacketHandlers.cs:3389-3397` | an item on the Backpack layer | 4-byte serial consumed, no item created, no container set, nothing linked | `(layer-1) == Layer.Backpack` | — |
| B51 `0x93`/`0xD4` OpenBook | `Network/PacketHandlers.cs:3475`, `:3497` | new page count for an open book | 2 bytes skipped, `BookPageCount` keeps its old value; no page-data request is sent on the refresh path | an existing non-disposed `ModernBookGump` for the serial | — |
| B52 `0x95` DyeData | `Network/PacketHandlers.cs:3526` | open the dye picker | packet fully ignored: no reposition, no refresh | a live `ColorPickerGump` already exists for that serial with the same graphic | — |
| B53 `0x98` UpdateName | `Network/PacketHandlers.cs:3566` | new name | entity name and overhead gump left alone | `World.Get(serial) == null`, including destroyed | — |
| B54 `0x9E` SellList | `Network/PacketHandlers.cs:3630`, `:3637` | sell list | returns; the empty-list case leaves previously open shop gumps untouched because the test precedes the disposes at `:3643`/`:3645` | vendor not in `World.Mobiles`; or `countItems <= 0` | — |
| B55 `0xA1` UpdateHitpoints | `Network/PacketHandlers.cs:3717-3720` | hits/hitsmax | dropped after reading only the serial | `World.Get` null, including `IsDestroyed` | — |
| B56 `0xA2` UpdateMana | `Network/PacketHandlers.cs:3742-3745` | mana | dropped; `World.Mobiles.Get` does not screen `IsDestroyed`, so mana can also land on a destroyed mobile | mobile unknown | — |
| B57 `0xA3` UpdateStamina | `Network/PacketHandlers.cs:3759-3764` | stamina | dropped; `World.Items` is never consulted | mobile unknown | — |
| B58 `0xA5` OpenUrl | `Network/PacketHandlers.cs:3780` | open URL | nothing happens on an empty string; otherwise no scheme validation, no prompt, no `InGame` guard, and `Utility/Platforms/PlatformHelper.cs:71-73` swallows launch failures | string null/empty | — |
| B59 `0xAA` AttackCharacter | `Game/GameActions.cs:993` | you are attacking `serial` | `LastAttack` is updated but **no status request is sent at all** | entity unknown and `force == false` | — |
| B60 `0xAB` TextEntryDialog | `Network/PacketHandlers.cs:3828` | prompt dialog | dialog never opens, server never answered | `!World.InGame` | — |
| B61 `0xAE` UnicodeTalk | `Network/PacketHandlers.cs:3867-3892` | speech | logged, `Seek(48)`, never displayed | `!World.InGame` with a `LoginScene` | — |
| B62 `0xAE` | `Network/PacketHandlers.cs:3903-3959` | "system" handshake | consumes the packet, sends a fixed byte blob, returns; no message reaches the journal | name is the system handshake | — |
| B63 `0xAF` DisplayDeath | `Network/PacketHandlers.cs:4020-4023` | a mobile died | dropped; **the player's own death is not handled here at all** and no corpse is registered for it | mobile unknown, or `serial == World.Player` | — |
| B64 `0xAF` | `Game/Managers/CorpseManager.cs:45-53` | second death notice for the same corpse | ignored outright | duplicate corpse serial | — |
| B65 `0xB0`/`0xDD` OpenGump | `Network/PacketHandlers.cs:6931-6934`, `:7526` | gump layout | nothing created; unrecognised layout commands logged "Invalid Gump Command" and dropped | tokenized layout empty / unknown command | — |
| B66 `0xB0` layout tokens | `Network/PacketHandlers.cs:7311`, `:7298`, `:7510-7519`, `:7501`, `:6992-6995` | `text`, `page`, `maparea`, `gumppichued` | dropped when the parameter count is short or an int fails to parse; zero-parameter tokens skipped | short token | — |
| B67 `0xB0` | `Network/PacketHandlers.cs:7504-7507` | `togglelimitgumpscale` | recognised and deliberately does nothing | always | — |
| B68 `0xB9` EnableLockedFeatures | `Assets/AnimationsLoader.cs:534-537`, `:541-542` | body-conversion flags | flags stored but the animation table is untouched | `UOFileManager.Version < CV_300` or `Bodyconv.def` missing | — |
| B69 `0xB9` | `Assets/AnimationsLoader.cs:576-590` | Anim3 / Anim4 gating | columns 3 and 4 accepted **regardless** of the flags — those checks are commented out | always | — |
| B70 `0xBA` DisplayQuestArrow | `Network/PacketHandlers.cs:4353-4356`, `:4360` | show/hide arrow | with an existing gump only `_mx`/`_my` update — no reposition, no re-add; hide with no gump does nothing | always | — |
| B71 `0xBF` cmd 0x10 | `Network/PacketHandlers.cs:4515` | item property block | `return` from the **whole** `ExtendedCommand`, so nothing after is parsed | item unknown | — |
| B72 `0xBF` cmd 0x10 | `Network/PacketHandlers.cs:4571` | attribute list | loop terminates at `Position < Length - 4`, deliberately leaving 4 bytes unread | always | — |
| B73 `0xBF` cmd 0x18 | `Network/PacketHandlers.cs:4694` | map patches | map is only reloaded when `MapLoader.ApplyPatches` returns true | else | — |
| B74 `0xBF` cmd 0x19 v2 | `Network/PacketHandlers.cs:4752` | stat locks for `serial` | ignored for anyone but the player | `serial != World.Player` | — |
| B75 `0xBF` cmd 0x20 type 4 | `Network/PacketHandlers.cs:4906-4911` | open house customization | nothing happens; note the lookup is **not scoped to `serial`** | any `HouseCustomizationGump` exists | — |
| B76 `0xBF` cmd 0x25 | `Network/PacketHandlers.cs:4963`, `:4978` | spell icon on/off | `World.ActiveSpellIcons` is only updated if a visible `UseSpellButtonGump` exists; stops at the first match | else | — |
| B77 `0xBF` cmd 0x2B | `Network/PacketHandlers.cs:5014-5024` | animation for a 16-bit serial | matches on `(m.Serial & 0xFFFF) == serial`, first match wins | collision | — |
| B78 `0xC1`/`0xCC` DisplayClilocString | `Network/PacketHandlers.cs:5090-5093` | localized message | **returns before any display** — an unknown cliloc number is silently dropped (after the PartyInvite gumps have already been disposed at `:5063-5072`) | `ClilocLoader.Translate` returns null | — |
| B79 `0xC2` UnicodePrompt | `Network/PacketHandlers.cs:5160` | prompt token | any pending `PromptData` is overwritten without being answered | always | — |
| B80 `0xD6` MegaCliloc | `Network/PacketHandlers.cs:5213-5216` | property list | returns before the serial is even read | leading uint16 > 1 | — |
| B81 `0xD6` | `Network/PacketHandlers.cs:5259-5262` | a cliloc entry | skipped with `continue`; argument bytes already consumed so framing holds, but the entry is silently absent | `ClilocLoader` cannot translate it | — |
| B82 `0xD6` | `Network/PacketHandlers.cs:5344` | the object's name | `Entity.Name` written **only** for non-mobile serials; mobiles keep whatever name they had | `SerialHelper.IsMobile(serial)` | — |
| B83 `0xD8` CustomHouse | `Network/PacketHandlers.cs:5563`, `:5570`, `:5593-5600` | a custom house design | returns — and the `:5593` bail happens **after** the house was created/cleared and its `Revision`/`IsCustom` overwritten at `:5577-5587`, i.e. after `ClearComponents` destroyed the old design | item unknown, not a multi, or multi bounds all zero | — |
| B84 `0xD8` | `Network/PacketHandlers.cs:5614-5617`, `:5418-5537` | a design plane | plane skipped (`p` not advanced by `clen`), or decompressed and then entirely discarded | `clen <= 0`; `planeMode` outside {0,1,2} | — |
| B85 `0xD8` | `Network/PacketHandlers.cs:5430`, `:5465`, `:5522` | a tile with id 0 | skipped, no component added | `tile id == 0` | — |
| B86 `0xD8` | `Game/GameObjects/House.cs:105` | clear custom components | does nothing at all | `World.Items.Get(house.Serial) == null` | — |
| B87 `0xDC` OPLInfo | `Network/PacketHandlers.cs:5672` | this object's property revision | everything is skipped, including the field reads | `!World.ClientFeatures.TooltipsEnabled` | — |
| B88 `0xDC` | `Network/PacketHandlers.cs:5677` → `Game/Managers/ObjectPropertiesListManager.cs:90-99` | revision changed | no request is queued | `World.OPL.IsRevisionEquals(serial, revision)` (masks off `0x40000000`, then exact compare) | — |
| B89 `0xDF` BuffDebuff | `Network/PacketHandlers.cs:5844` | buff icon | packet ignored **including the count field**; nothing read past the icon type | `iconID >= BuffTable.Table.Length` | — |
| B90 `0xDF` | `Network/PacketHandlers.cs:5918`, `:5921-5924` | buff refreshed | `BuffGump.RequestUpdateContents` skipped — refreshing an existing buff does not repaint the gump | icon type already existed | — |
| B91 `0xE2` NewCharacterAnimation | `Game/GameObjects/MobileAnimation.cs:1548-1551`, `:1590` | animation type N | returns 0 and **group 0 is played instead of nothing** | graphic >= `MaxAnimationCount`, or type outside {0..11,14} | — |
| B92 `0xE5` DisplayWaypoint / `0xF0` | `Game/Managers/WorldMapEntityManager.cs:134-141`, `:85-93` | a waypoint / party position | `AddOrUpdate` returns immediately | `from_packet` false and no 0xF0 within 10s; or `!Enabled` (needs encryption off, movement-system flag clear/acked, `WorldMapShowParty` on, and a `WorldMapGump` open) | `WorldMapShowParty` default **on** (`Configuration/Profile.cs:357`) |
| B93 `0xF3` UpdateItemSA | `Network/PacketHandlers.cs:6121` | update addressed to the player | **nothing at all** — no else, no log — when the packet arrives as a standalone `0xF3` rather than inside `0xF7` | `serial == World.Player && p[0] != 0xF7` | — |
| B94 `0xF6` BoatMoving | `Network/PacketHandlers.cs:6144-6147`, `:6218-6221` | boat + rider positions | returns before the rider count; unknown riders skipped after their 10 bytes are consumed | multi unknown / rider unknown | — |
| B95 `0xF7` PacketList | `Network/PacketHandlers.cs:6305-6310` | a batch of sub-packets | any sub-id other than `0xF3` aborts the loop with a warning; the rest of the payload is left unparsed and the reader is **not** resynchronised | non-0xF3 sub-id | — |
| B96 `0xA8`/`0x86`/`0xA9`/`0x82`/`0x85`/`0x53`/`0x8C` | `Network/PacketHandlers.cs:6316-6319`, `:6346-6349`, `:6361`, `:6376-6379`, `:6331` | server list, char list, login rejection, relay | dropped outright, without parsing | `World.InGame` is true, or the scene is not `LoginScene` | — |
| B97 `0xD1` Logout | `Network/PacketHandlers.cs:5183-5189`, `:5197-5200` | log out | no-op; when the bool is false the client stays connected and only logs a warning | not `DisconnectionRequested` **and** `CLF_OWERWRITE_CONFIGURATION_BUTTON` not set | — |
| B98 `0x1C` Talk | `Network/PacketHandlers.cs:1067-1079` | a SYSTEM-prefixed message | full early-out with only an ACK; the text is discarded | `serial==0 && graphic==0 && type==Regular && font==0xFFFF && hue==0xFFFF && name.StartsWith("SYSTEM")` | — |
| B99 all messages | `Game/Managers/MessageManager.cs:85-88`, `:119-124`, `:127`, `:145`, `:149`, `:233-237` | text | returns/breaks with no overhead text | empty text; System/Command/Encoded/ChatSystem type; party overhead off or no matching member; ignored speaker; `parent == null` | see H |
| B100 `0x29` DropItemAccepted | `Network/PacketHandlers.cs:1892-1895` | the drop was accepted | drop confirmation dropped; `ItemHold` is left as-is | `!World.InGame` | — |
| B101 `0xB2` ChatMessage default | `Network/PacketHandlers.cs:4239-4242` | chat message cmd | returns early; the trailing 4-byte skip and the text are not read | `ChatManager.GetMessage(cmd-1)` empty — any index >= 41 (`Game/Managers/ChatManager.cs:91-94`) | — |

Additional whole-packet `!World.InGame` / `World.Player == null` drops with no other effect (the packet
is consumed and lost): `0x0B`:552, `0x1C` via MessageManager, `0x1D`:1107, `0x20`:1270, `0x21`:1291,
`0x22`:1311, `0x24`:1418, `0x25`:1722, `0x27`:1746, `0x28`:1881, `0x29`:1892, `0x2E`:1980, `0x2F`:2046,
`0x38`:2271, `0x3A`:2101, `0x3B`:2324, `0x3C`:2285, `0x4E`:2336, `0x4F`:2361, `0x54`:2389, `0x56`:2496,
`0x65`:2546, `0x66`:2568, `0x6F`:464, `0x70/0xC0/0xC7`:2659, `0x71`:2748, `0x72`:2887, `0x74`:2902,
`0x77/0xD2`:3021, `0x78/0xD3`:3061, `0x7C`:3201, `0x89`:3364, `0x97`:3538, `0x98`:3549, `0x99`:3589,
`0x9A`:3609, `0x9E`:3623, `0xAB`:3828, `0xAF`:4009, `0xB0`:4066, `0xC1/0xCC`:5043, `0xC2`:5155,
`0xD6`:5206, `0xDF`:5828, `0xE2`:5932, `0xF3`:6078, `0xF6`:6129, `0xF7`:6290.

---

## C. Partial apply — fields parsed then discarded

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| C1 `0x77`/`0xD2` self | `Network/PacketHandlers.cs:3045-3052` | player x, y, z, direction | **deliberately discarded** — comment at `:3051`: "x,y,z, direction cause elastic effect, ignore em for the moment". Only NotorietyFlag, Flags, Graphic, Hue are applied | `serial == World.Player` | — |
| C2 `0x78`/`0xD3` self | `Network/PacketHandlers.cs:3078-3085` | player x, y, z, direction, notoriety | read and thrown away; only Graphic, hue, Flags applied. Position for self comes from `0x77`/`0x20` instead | `serial == World.Player` | — |
| C3 `0x20` UpdatePlayer | `Network/PacketHandlers.cs:6829` | update for `serial` | **does nothing at all** unless `serial == World.Player`, after being fully parsed | any other serial | — |
| C4 `0x20` | `Network/PacketHandlers.cs:6816` args | `graphic_inc` (wire off 7) and `serverID` (wire off 15) | both threaded through and never applied — no graphic increment, no map/server switch | always | — |
| C5 `0x70` hue/blend | `Network/PacketHandlers.cs:2701-2705` | effect hue and blend mode | for `p[0]==0x70` neither is read; the effect spawns with hue 0 and blend Normal regardless of the wire | packet 0x70 | — |
| C6 `0x1B` EnterWorld | `Network/PacketHandlers.cs:986` | starting map | `World.MapIndex` forced to 0 **only** when `World.Map == null`; otherwise the packet's position is applied to whatever map is already loaded and no map switch happens | a map already exists | — |
| C7 `0x54` PlaySoundEffect | `Game/Managers/AudioManager.cs:158-160` | sound at x,y,z | z is parsed and never fed to the audio system; distance is 2D only. The second uint16 (`audio`) is parsed and thrown away | always | — |
| C8 `0x71` case 1 | `Network/PacketHandlers.cs:2748` handler, off 12 | `parentID` for reply threading | parsed and discarded — no threading applied | always | — |
| C9 `0x90`/`0xF5` DisplayMap | `Network/PacketHandlers.cs:3403` | `gumpid` | decoded and unused | always | — |
| C10 `0x99` MultiPlacement | `Network/PacketHandlers.cs:3587` off 1 and 6 | `allowGround` and `flags` | parsed and discarded — placement is not restricted to ground and the flags byte is not honoured | always | — |
| C11 `0xAB` TextEntryDialog | `Network/PacketHandlers.cs:3826` | `variant` | only `variant == 2` is used (NumbersOnly); every other value ignored. X/Y hardcoded 143,172 at `:3849-3850` | always | — |
| C12 `0xAE` UnicodeTalk | `Network/PacketHandlers.cs:4000` | `font` and `graphic` | font ignored in favour of the profile `ChatFont`; graphic read and discarded | always | — |
| C13 `0xB2` 0x03ED | `Network/PacketHandlers.cs:4165-4167` | accepted username and channel | username thrown away, then **unconditionally joins "General"** rather than the channel the server named | always | — |
| C14 `0xBC` Season | `Network/PacketHandlers.cs:4378-4381`, `:4403` | second wire byte (music/track flag) | never used as a track number in this fork; the stock behaviour of playing a track on season change is gone | always | — |
| C15 `0xC1`/`0xCC` | `Network/PacketHandlers.cs:5130` | the graphic field | parsed and discarded; the line that would apply it is commented out | always | — |
| C16 `0xD8` CustomHouse | `Network/PacketHandlers.cs:5553-5554` | compression flag and `enableResponse` | parsed and never consulted | always | — |
| C17 `0xE5` DisplayWaypoint | `Network/PacketHandlers.cs:5975`, `:5986` | waypoint name, hp, z | name discarded and replaced with the literal "Corpse"/"Resurrection"; hp hardcoded 0; `isguild` hardcoded true; z read and thrown away | always | — |
| C18 `0xF3`/`0x1A` | `Network/PacketHandlers.cs:6076` | `unk` (duplicate amount) and `unk2` | read, passed to `UpdateGameObject`, never used | always | — |
| C19 `0x86` UpdateCharacterList | `Game/Scenes/LoginScene.cs:766` | the 30-byte password field per record | skipped, never stored. This path also does not parse cities and does not read `CharacterListFlags`, so `World.ClientFeatures` keeps whatever `0xA9` last set | always | — |
| C20 `0x11` CharacterStatus | `Network/PacketHandlers.cs:717-726` | `WeightMax` | **the server's value is ignored** and a local one is synthesized: `7*(Strength>>1)+40` for >= CV_500A, else `Strength*4+25` | `type < 5` | — |
| C21 `0x23` DragAnimation | `Network/PacketHandlers.cs:1366-1368`, `:1379-1381` | drag source and dest coordinates | **replaced** with the mobiles' current positions whenever the mobiles are found | source/dest resolvable | — |
| C22 `0x23` | `Network/PacketHandlers.cs:1398-1403` | (drag parameters) | speed 5, duration 5000, `fixedDir` true, `doesExplode` false, `hasparticles` false and the blend mode are all **hardcoded**, not from the wire | always | — |
| C23 `0x6E` CharacterAnimation | `Network/PacketHandlers.cs:2642`, `Game/GameObjects/Mobile.cs:396` | forward flag, frame count | the forward flag is the logical **inverse** of the wire byte; on a forward animation the frame count the server sent is **discarded and stored as 0** | always / forward | — |
| C24 `0xDF` BuffDebuff | `Network/PacketHandlers.cs:5851`, `:5860`, `:5919` | target serial, per-entry icon, icon graphic | the addressed serial is parsed then ignored (the buff always lands on `World.Player`); the per-entry `icon` at `:5860` is discarded so a multi-entry packet collapses onto one slot; the stored graphic is `BuffTable.Table[iconID]`, not what the packet carried | always | — |
| C24b `0xE2` | `Network/PacketHandlers.cs:5930` | (no interval/frame fields) | `interval` and `frameCount` left at 0, so effective frame count is 0 and timing falls back to animation defaults | always | — |
| C25 `0xF6` BoatMoving non-smooth | `Network/PacketHandlers.cs:6179-6191`, `:6272`, `:6276` | boat facing direction; rider graphic/hue/flags | the non-smooth path never applies a direction to the multi (the `UpdateGameObject` call is commented out); rider `Amount`/`Direction` are echoed from the entity itself rather than taken from the wire | `UseSmoothBoatMovement` off | default **off** (`Configuration/Profile.cs:342`) |
| C26 `0xBD` ClientVersion | `Network/PacketHandlers.cs:4407-4409` | version request payload | ignored entirely; the client answers with `Settings.GlobalSettings.ClientVersion` | always | — |
| C27 `0x1C`/`0xAE`/`0xC1`/`0xCC` names | `:1096`, `:3988`, `:5133` | this entity's name | `Entity.Name` is written **only when it is currently null or empty**; a server-sent name for an already-named entity is ignored | always | — |
| C28 `0x98` UpdateName | `Network/PacketHandlers.cs:3559` vs `:3568` | new name (empty) | the empty name is rejected for the world-map entity three lines earlier, but written unguarded to `Entity.Name` — an empty name from the server blanks the entity's name | name empty | — |

---

## D. Clamps, floors, caps, masks and substitutions

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| D1 `0x4F` LightLevel | `Network/PacketHandlers.cs:2368-2371` | overall light level | `> 0x1E` forced to `0x1E` **before anything is written**, so `RealOverall` never records what the server actually sent above 30 | level > 30 | — |
| D2 `0x4F` | `Network/PacketHandlers.cs:2375-2383` | overall light level | `World.Light.Overall` is not written at all; only `RealOverall` records it. With `LightLevelType == 1` the value becomes `min(level, Profile.LightLevel)` — the server can only ever make it darker than the user's ceiling | `UseCustomLightLevel` on and `LightLevelType != 1` / `== 1` | `UseCustomLightLevel` default **off**, `LightLevel` 0, `LightLevelType` 0 (`Configuration/Profile.cs:215-217`) |
| D3 `0x4E` PersonalLightLevel | `Network/PacketHandlers.cs:2345-2354` | personal light level | clamped to `0x1E`; `Personal` left alone when `UseCustomLightLevel` is set (only `RealPersonal` records it); the whole packet is ignored for anyone but the player at `:2341` | as above | as above |
| D4 `0x55` LoginComplete | `Network/PacketHandlers.cs:2468-2471` | — (client is announcing its range) | requested view range clamped to `[MIN_VIEW_RANGE=5, MAX_VIEW_RANGE=40]` (`Game/Constants.cs:116-117`) before being sent | `Client.Version >= CV_305D` | `client_view_range` default **40** (`Configuration/Settings.cs:144`) |
| D5 `0xC8` ClientViewRange | `Network/PacketHandlers.cs:2739-2741` | the granted view range | **no clamp at all** — the raw byte 0..255 is written straight into `World.ClientViewRange` (`Game/World.cs:87`, a plain auto-property). Contrast D4 and `Game/Managers/MacroManager.cs:1684-1698` which both clamp | always | — |
| D6 `0x22` ConfirmWalk | `Network/PacketHandlers.cs:1317-1322` | notoriety | `0x40` bit masked off and never used; a masked value of 0 or `>= 8` is **forced to 0x01 (Innocent)** | noto 0 or >= 8 | — |
| D7 `0x11` CharacterStatus | `Network/PacketHandlers.cs:708-711` | race byte | a race byte of 0 is clamped **up** to 1 before the cast to `RaceType` | race == 0 | — |
| D8 `0x11` | `Network/PacketHandlers.cs:750-779` | extended caps (type >= 6) | every field is clamped to 0 when fewer than 2 bytes remain — a truncated packet silently zeroes the remaining caps rather than aborting | truncated | — |
| D9 `0x3A` UpdateSkills | `Network/PacketHandlers.cs:2190`, `:2243` | (no cap field on this list type) | `cap` is **fabricated as 1000** and written into `Skill.CapFixed` | `!haveCap` | — |
| D10 `0xBC` Season | `Network/PacketHandlers.cs:4386-4389` | season index | `> 4` clamped to 0 rather than rejected | season > 4 | — |
| D11 `0xBC` | `Network/PacketHandlers.cs:4391-4394` | season 4 (Desolation) | whole packet dropped — `OldSeason` not written, `ChangeSeason` not called | player is dead | — |
| D12 `0x53`/`0x82`/`0x85` | `Game/Data/ServerErrorMessages.cs:94-97`, `:104-107`, `:123-126`, `:132` | error code | code clamped per packet (0x53: >=10 → 9; 0x85: >=6 → 5; 0x82: >=9 → 8). An unknown code is reported as the last known error, not as unknown. An unrecognised packet id returns `string.Empty` while `CurrentLoginStep` is still forced to `PopUpMessage` | always | — |
| D13 `0x27` | `Game/Data/ServerErrorMessages.cs:112-118` | move-denied code | would clamp `>= 5` to 4, but is unreachable because `Network/PacketHandlers.cs:1865` suppresses the text first | — | — |
| D14 hue, everywhere | `Game/GameObjects/Entity.cs:115-133` (`FixHue`) | an item/mobile hue | masked `& 0x3FFF`; **any masked value >= 0x0BB8 is replaced with 1**; the `0xC000` bits are re-ORed, or only `0x8000` kept when the low 14 bits are zero | every `FixHue` call site | — |
| D15 amount, everywhere | `Network/PacketHandlers.cs:6684-6687` | `count`/amount of 0 | rewritten to 1. Because `0x77`, `0xD2` and `0xD3` all pass `count = 0` at their call sites (`:3055`, `:3088`), **every item described by those packets gets `Amount = 1` regardless of what it really is** | count == 0 | — |
| D16 equipment amount | `Network/PacketHandlers.cs:3156` | equipped item amount | always overwritten with 1 | `0x78`/`0xD3` equipment loop | — |
| D17 mobile graphic | `Network/PacketHandlers.cs:6729` | mobile body graphic | masked `& 0x3FFF` — the top two bits the server sent are dropped | every mobile update | — |
| D18 multi graphic | `Network/PacketHandlers.cs:6663` | multi graphic | masked `& 0x3FFF` | `type == 2` | — |
| D19 direction | `Network/PacketHandlers.cs:992` (`& 0x7`), `:1300` (low 3 bits), `:6606` (`& Direction.Up`), `:6840` (`& Direction.Mask`), `:6705-6706` | direction byte including the Running/Up bits | masked; the running bit is dropped or split out. `0xD2`'s player branch never masks at all | always | — |
| D20 Z | `Network/PacketHandlers.cs:984` | Z as two bytes | truncated by an `(sbyte)` cast | `0x1B` | — |
| D21 Z | `Network/PacketHandlers.cs:6446` | (no Z field) | `item.Z` **forced to 0**, discarding the item's previous Z | `AddItemToContainer` (`0x25`, `0x3C`, `0x27` branch A) | — |
| D22 city coords | `Game/Scenes/LoginScene.cs:801-803` | city x, y, z as 32-bit | truncated to ushort/ushort/sbyte | `0xA9` | — |
| D23 gump position | `Network/PacketHandlers.cs:4073-4074` | gump x, y as uint32 | cast to `int` — a value above `int.MaxValue` becomes negative | `0xB0` | — |
| D24 blend mode | `Network/PacketHandlers.cs:2705` | effect blend mode | `value % 7`, so `GraphicEffectBlendMode.ScreenRed (0x07)` is unreachable and folds to Normal | effects | — |
| D25 effect speed | `Game/Managers/EffectManager.cs:104`, `:140` / `:197`, `:222` | effect speed | 0 bumped to 1 for Moving and Drag; **FixedXYZ and FixedFrom pass speed 0 regardless of the wire speed byte** | always | — |
| D26 effect duration | `Game/Managers/EffectManager.cs:93` | duration (one byte) | multiplied by `Constants.ITEM_EFFECT_ANIMATION_DELAY` (50) | always | — |
| D27 effect hue | `Game/Managers/EffectManager.cs:88-91` | effect hue | a non-zero hue is **incremented by 1** | hue != 0 | — |
| D28 lightning | `Game/GameObjects/LightningEffect.cs:40` | graphic and duration | **ignored entirely**; hardcoded 0x4E20 and 400 | Lightning effects | — |
| D29 `0x6E` | `Network/PacketHandlers.cs:2649-2650` | frame_count, repeat_count as 16-bit | `frame_count` cast to byte — values above 255 wrap | always | — |
| D30 `0x65` weather | `Game/Weather.cs:102` | weather count | clamped down by `Math.Min(count, MAX_WEATHER_EFFECT)` | always | — |
| D31 `0x7C` gray menu | `Network/PacketHandlers.cs:3267-3272` | per-entry height | clamped **up** to a minimum of 21, and each subsequent offset reduced by 1 | gray menu style | — |
| D32 `0x90` map origin | `Game/UI/Gumps/MapGump.cs:217-218` | map start x/y | `startX`/`startY` overwritten **in place** by `AddPin` using a hardcoded `Width/300f` multiplier — the recorded origin is destroyed by the first pin | first pin added | — |
| D33 `0x73` Ping | `Game/Managers/NetStatistics.cs:106` | ping index | not validated: folded with `% _pings.Length` (5), so an out-of-range or wrong index overwrites an arbitrary slot. The delta is measured against `_startTickValue`, which the sender overwrites on every send (`:131`) — with more than one ping in flight the sample is wrong rather than discarded | always | — |
| D34 `0xA8` server index | `Game/Scenes/LoginScene.cs:425-428` | (saved server) | an out-of-range saved index is silently clamped to 0 — a stale saved server becomes the first in the list | index out of range | — |
| D35 `0xBF` cmd 0x26 | `Network/PacketHandlers.cs:4990-4993` | character speed type | any value above `FastUnmountAndCantRun` is clamped to 0 | always | — |
| D36 `World.MapIndex` | `Game/World.cs:136-139`, `:111` | map index | `>= MapLoader.MAPS_COUNT` silently clamped to 0; a value equal to the current index does nothing at all | always | — |
| D37 `0x1B` season music | `Network/PacketHandlers.cs:1003` | (light) | `LightLevelType == 1` takes `Math.Min(existing Overall, profile level)` rather than the profile value | `UseCustomLightLevel` on | default **off** |
| D38 `0xB9` body-conv flags | `Network/PacketHandlers.cs:4317-4327` | locked-feature flags | mapping is **not one-to-one**: UOR sets Anim1\|Anim2, LBR Anim1, AOS Anim2, SE Anim3, ML Anim4, OR-accumulated — UOR alone already grants what LBR and AOS would | always | — |
| D39 `0xB9` chat | `Network/PacketHandlers.cs:4311-4315` | feature flags | chat enabled solely on the T2A bit; every other combination sets `ChatIsEnabled = 0`, assigned unconditionally — **this packet can turn chat back off** | always | — |
| D40 `0x74` BuyList name | `Network/PacketHandlers.cs:2986-3004` | the item's name string | the server's string is **discarded whenever OPL already has one**; then numeric → cliloc, then empty → `ItemData.Name`, then the literal | OPL hit | — |
| D41 `0x2C` corpse marker | `Network/PacketHandlers.cs:1920-1929` | (you died) | a `WMapEntity` "Your Corpse" is written with `LastUpdate = Time.Ticks + 300000`, so `Game/Managers/WorldMapEntityManager.cs:196` cannot expire it for five minutes | `EnableDeathScreen` | default **on** (`Configuration/Profile.cs:179`) |

---

## E. Client-side distance culling

The server delivered these objects; the client throws them away on its own initiative and the
server is never told, so it has no reason to send them again.

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| E1 mobile cull | `Game/World.cs:356-359` | this mobile exists | `RemoveMobile(mob)` — destroyed and swept out | `do_delete && mob.Distance > ClientViewRange`; `do_delete` throttled to every 50 ms at `Game/World.cs:334-339` | `client_view_range` default **40** |
| E2 item cull | `Game/World.cs:436-453` | this item exists on the ground | `RemoveItem(item)`; for a multi only if `HouseManager.TryToRemove(item, ClientViewRange)` agrees | `do_delete && item.OnGround && item.Distance > keepWithin && !KeptByItsHouse(item)`, where `keepWithin = ClientViewRange + (IsMulti ? MultiDistanceBonus : 0)` | as above; `keep_house_contents_loaded` default **on** |
| E3 house let-go | `Game/Managers/HouseManager.cs:147-165` | this house exists | `ClearComponents()` and the house is dropped from `_houses` | `!IsHouseInRange(serial, distance)` where distance is widened by `MultiDistanceBonus` (`:194`) | — |
| E4 effect cull | `Game/Managers/EffectManager.cs:50` | this effect is playing | effect removed | `!f.IsDestroyed && f.Distance > World.ClientViewRange` | — |
| E5 world-map entity expiry | `Game/Managers/WorldMapEntityManager.cs:194-225` | party/guild member at x,y | entry removed from `Entities` | `entity.LastUpdate < Time.Ticks - 1000`; the sweep itself self-throttles to once per second at `:200-205` | — |
| E6 world-map corpse expiry | `Game/Managers/WorldMapEntityManager.cs:196` | your corpse is here | `_corpse = null` | `_corpse.LastUpdate < Time.Ticks - 1000` (but see D41) | — |
| E7 follow stop | `Game/Scenes/GameScene.cs:959` | (following target's position) | `StopFollowing()` | `follow.Distance > World.ClientViewRange` | `FollowingMode` |
| E8 map-change purge | `Game/World.cs:965-1019` (`InternalMapChangeClear`) | everything on the old map | every item and mobile except the player and the player's own contents is removed with `forceRemove`, and every ground multi is dropped from `HouseManager` | any `World.MapIndex` change (`Game/World.cs:113`) | — |
| E9 boat step trim | `Game/Managers/BoatMovingManager.cs:104-107` | a queued boat position | the **oldest** queued step is dropped | `deque.Count > 5` | — |
| E10 out-of-range multi rebuild refusal | `Game/GameObjects/Item.cs:637-644` | this multi's graphic changed | `LoadMulti()` is not called and the multi is left un-rebuilt | `MultiDistanceBonus != 0 && !HouseManager.IsHouseInRange(Serial, ClientViewRange)` | — |
| E11 sound silencing | `Game/Managers/AudioManager.cs:172-175` | play this sound at x,y | volume forced to 0 — the sound is still fetched and queued, just silent | `distance > World.ClientViewRange` (2D only, see C7) | — |
| E12 draw suppression | `Views/ItemView.cs:158`, `:412`, `Views/LandView.cs:62`, `Views/StaticView.cs:81`, `Views/MultiView.cs:104`, `Views/MobileView.cs:114`, `Views/GameEffectView.cs:109`, `Views/LightningEffectView.cs:48`, `Game/GameObjects/DragEffect.cs:114` | (object exists) | drawn in the out-of-range hue instead of its own | `NoColorObjectsOutOfRange && Distance > World.ClientViewRange` | default **off** (`Configuration/Profile.cs:158`) |
| E13 corpse auto-open range | `Game/GameObjects/PlayerMobile.cs:1451` | a corpse appeared | not auto-opened | `AutoOpenCorpseRange` exceeded, or already in `AutoOpenedCorpses` | `AutoOpenCorpseRange` default **2** |

---

## F. Retention past the server's range

State the client keeps after the server's own range no longer covers it, or after it has told the
client to let go.

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| F1 house-contents sparing | `Game/World.cs:275-288` (`KeptByItsHouse`) + `:438` | (nothing — no packet) | anything standing inside a **remembered** house footprint is **never** culled for distance, at any distance | `!item.IsMulti && KeepHouseContentsLoaded && HouseManager.IsInsideKnownHouse(item)` | `keep_house_contents_loaded` default **ON** (`Configuration/Settings.cs:138`) |
| F2 house footprints | `Game/Managers/HouseManager.cs:68`, `:82-103`, `:122-140` | (nothing) | `_footprints[serial]` records where every house seen **this session** stood, and is kept after the house itself has been let go of; `IsInsideKnownHouse` walks that table with **no distance limit** | written from `Game/GameObjects/Item.cs:591` at the end of `LoadMulti`; rewritten on re-load, never removed except by `Clear()` | as F1 |
| F3 multi distance bonus | `Game/World.cs:436`, `Game/Managers/HouseManager.cs:194`, `Game/GameObjects/Item.cs:581-584` | (view range) | a multi is kept out to `ClientViewRange + MultiDistanceBonus`, i.e. beyond the range the client asked the server for | always | — |
| F4 view range raised above protocol | `Configuration/Settings.cs:140-144`, `Game/Constants.cs:117` | (the client asks) | `MAX_VIEW_RANGE` is **40** in this fork, not the protocol's 24, and the default `client_view_range` is 40 | always | default **40** |
| F5 corpse-animation deferral | `Network/PacketHandlers.cs:1209-1212` | delete this object | not removed, not destroyed, not pooled — only gump refreshes run | `World.CorpseManager.Exists(0, serial)` | — |
| F6 stale key retention | `Game/World.cs:341-349`, `:414-423`, `:396-412`, `:461-477` | (nothing) | the sweep removes by **key**, and only when the entry is still destroyed and still the one condemned; an entity whose serial has drifted from its key stays filed forever | pooled object handed out under a new serial | — |
| F7 pooling deferral | `Game/GameObjects/Item.cs:296-331`, `Game/GameObjects/Mobile.cs:1142-1158` | (nothing) | `Destroy()` deliberately does **not** return the object to the pool; only `ReturnToPool`, called after the dictionary entry is out, does | always | — |
| F8 corpse re-key | `Network/PacketHandlers.cs:4025-4036` | this mobile died | the mobile is re-filed under `serial \| 0x80000000` — a value that is **not** `IsValid`, not `IsMobile`, not `IsItem` (`Game/SerialHelper.cs:41-56`) — and `owner.Serial` is rewritten in place while it is still linked into its chunk and still referenced by gumps holding the old serial | `0xAF` for a non-player mobile | — |
| F9 corpse self-removal | `Game/GameObjects/Mobile.cs:698-718` | (nothing) | a re-keyed corpse removes **itself** from the world when its death animation runs out of frames, on a per-frame path | `(Serial & 0x80000000) != 0` and the frame index overruns | — |
| F10 gump contents kept | `Network/PacketHandlers.cs:1712` | open this container | contents are **not** cleared — the client keeps whatever it already had | corpse, or `graphic == 0xFFFF` (spellbook) | — |
| F11 corpse equipment kept | `Network/PacketHandlers.cs:6434` | this item is in container X | the old item is only torn down when `item.Container != containerSerial` **and** (`container.Graphic != 0x2006` or `item.Layer == Layer.Invalid`) — same-container and corpse-equipment re-sends leave the existing linkage in place ("prevent closing containers when changing facets") | as stated | — |
| F12 child purge exemption | `Network/PacketHandlers.cs:3107` | this object's full contents | children with `Opened == true` or `Layer == Backpack` are spared the pre-wipe; **everything else the client believed the object carried is destroyed** before the wire list is applied | `0x78`/`0xD3` | — |
| F13 unequip survivors | `Network/PacketHandlers.cs:6899-6905`, `:6914` | clear this corpse | every child whose `Layer != 0` is kept and only the first survivor becomes the new head; the survivors' `Previous` pointers are left as they were | `remove_unequipped` (`container.Graphic == 0x2006`) | — |
| F14 profile / stored gumps | `Network/PacketHandlers.cs:2484-2488`, `Configuration/Profile.cs:908` | (nothing) | every gump read from `gumps.xml` is re-added on login and `UIManager.SavePosition(serverSerial, …)` re-anchors it | `ReadGumps` non-null | — |
| F15 gump position cache | `Network/PacketHandlers.cs:6941-6942`, `:6964` | this gump goes at x,y | the server's x/y are **discarded** whenever `UIManager` has a cached position for that `gumpID`; the cache is written only the first time the gumpID is seen | cached hit | — |
| F16 gump re-centre | `Network/PacketHandlers.cs:7628-7632` | (position) | a finished gump at 0,0 is re-centred in the viewport, overriding both the server position and the cached one | position is 0,0 | — |
| F17 `_customHouseRequests` dedup | `Network/PacketHandlers.cs:4867` | house revision changed | a duplicate serial is not added; duplicate requests inside one flush collapse to one | already queued | — |
| F18 `_clilocRequests` dedup | `Network/PacketHandlers.cs:421-429` | property revision changed | a serial already queued is silently dropped | already queued | — |
| F19 cliloc batch never cleared | `Network/PacketHandlers.cs:363-369` vs `:377` | (queued requests) | on the CV_5090+ path `SendMegaClilocRequests` does not clear `_clilocRequests`; clearing is left to `Send_MegaClilocRequest`. Entries queued while tooltips were on are never sent if the feature is turned off before the next frame (re-gated at `:361`) | `Client.Version >= CV_5090` | — |

---

## G. Deferral — server position queued, not applied

| id | file:line | what the server said | what the client does | condition | setting |
| --- | --- | --- | --- | --- | --- |
| G1 mobile move deferral | `Network/PacketHandlers.cs:6703-6726` | this mobile is at x,y,z facing d | the coordinates are **not applied**; they are pushed into the walk-step queue and played back over subsequent frames | position only snapped when `World.Get(mobile) == null`, or the mobile sits at `0xFFFF,0xFFFF`, or `EnqueueStep` refuses | — |
| G2 step queue refusal | `Game/GameObjects/Mobile.cs:304-309` | the position | `EnqueueStep` returns false; the handler then teleports the mobile and clears its queue (`:6720-6726`) | `Steps.Count >= Constants.MAX_STEP_COUNT` (5, `Game/Constants.cs:48`) | — |
| G3 duplicate step swallowed | `Game/GameObjects/Mobile.cs:313-316` | the position | returns **true** having queued nothing — the server's position is silently dropped | queued end position already equals the target | — |
| G4 boat move deferral | `Network/PacketHandlers.cs:6161-6171` | the boat is at x,y,z facing d | on the smooth path the multi is **not moved at all** here; only a step is queued | `UseSmoothBoatMovement` on | default **off** |
| G5 self position deferral | `Network/PacketHandlers.cs:6829`, C1/C2 | player position | the player is never moved by `0x1A`, `0x77`/`0xD2` self, `0x78`/`0xD3` self, or a standalone `0xF3`; position for self arrives only via `0x20`/`0x22`/`0xF7` | always | — |
| G6 walk request gating | `Game/GameObjects/PlayerMobile.cs:1685`, `:1892` | (forced move `0x97`, or a swing turn `0x2F`) | `Walk` returns false with no state change and **no feedback to the server** | `Walker.WalkingFailed`, `LastStepRequestTime > Ticks`, `StepsCount >= MAX_STEP_COUNT`, or CV_60142+ and `IsParalyzed` | — |
| G7 obstacle avoidance rewrite | `Game/GameObjects/PlayerMobile.cs:1676`, `:1721-1732` | move in direction d | the avoid path may substitute a **different direction** or refuse the move | `AutoAvoidObstacules` on and `Pathfinder.AutoWalking` off | default **on** (`Configuration/Profile.cs:150`) |
| G8 partial step | `Game/GameObjects/PlayerMobile.cs:1751-1754`, `:1935-1938` | move to the next tile | if `CanWalk` returns a different direction, only a **turn** is performed; x/y/z are not advanced | `CanWalk` disagrees | — |

---

## H. Settings whose effect is to ignore or override the server

### H.1 Global (`Configuration/Settings.cs`)

| id | setting | file:line | what it ignores or overrides | default |
| --- | --- | --- | --- | --- |
| H1 | `ignore_server_stop_music` | `Configuration/Settings.cs:118`; used `Network/PacketHandlers.cs:2426-2433` → `Game/Managers/AudioManager.cs:477-488`, `:815-820` | Throws away the server's stop-music packet; the running track is **adopted as the map's** instead of being stopped. Also stops the music map from going quiet when it has no data for the block | **off** |
| H2 | `keep_house_contents_loaded` | `Configuration/Settings.cs:138`; used `Game/World.cs:277` | Turns off the distance cull entirely for anything inside a remembered house footprint (F1/F2) | **ON** |
| H3 | `client_view_range` | `Configuration/Settings.cs:144`; used `Network/PacketHandlers.cs:2468-2473`, `Game/UI/Gumps/ModernOptionsGump.cs:2372-2381` | What the client asks the server to send. **40**, above the protocol's 24, chosen because "a client that lets go of things the server still believes it has can never be sent them again" | **40** |
| H4 | `music_map_mode` | `Configuration/Settings.cs:112`; used `Game/Managers/AudioManager.cs:671-800` | Invents music the server never named, from `Data/MusicMap.txt`, whenever the server's track is not sounding. 0 off / 1 authentic / 2 seamless / 3 continuous | **0 (off)** |
| H5 | `music_era` | `Configuration/Settings.cs:106`; `Game/Managers/AudioManager.cs` `EnsureMusicEraApplied` | Substitutes a different subfolder of `Music/Digital` for the track index the server named | **""** |
| H6 | `log_music_indices`, `log_house_diagnostics`, `music_overlay` | `Configuration/Settings.cs:122-131` | Diagnostics only; `LogViewRange` (`Game/Managers/HouseDiagnostics.cs:36-43`) additionally suppresses repeats of the same value | **off** |
| H7 | `EnhancedPacketsEnabled` | `Configuration/Settings.cs:163-174`; used `Game/Managers/EnhancedPacketHandler.cs:17-20`, `:63-64` | Drops every `0xCE` after its header has been read; when false at type-init the sub-handler is never registered at all (A38/A39) | **on** unless `Data/DISABLE_ENHANCED_PACKETS` exists |
| H8 | `shard_type` | `Configuration/Settings.cs:146` | 0 = no house customization | **0** |
| H9 | `encryption` | `Configuration/Settings.cs:159`; read `Game/Managers/WorldMapEntityManager.cs:89`, `:105-114` | Non-zero disables the whole world-map party/guild query path | **0** |

### H.2 Per-profile (`Configuration/Profile.cs`)

| id | setting | file:line | what it ignores or overrides | default |
| --- | --- | --- | --- | --- |
| H10 | `IgnoreGuildMessages` | `Profile.cs:108`; `Game/Managers/MessageManager.cs:166` | Guild speech **returns** before `InvokeMessageReceived` at `:303` — dropped entirely, not merely hidden | off |
| H11 | `IgnoreAllianceMessages` | `Profile.cs:107`; `Game/Managers/MessageManager.cs:170` | as above for alliance | off |
| H12 | `OverrideAllFonts` / `OverrideAllFontsIsUnicode` | `Profile.cs:317-318`; `Game/Managers/MessageManager.cs:111-115` | Replaces the server's font and unicode flag on **every** message | off / true |
| H13 | `ForceUnicodeJournal` | `Profile.cs:106`; `Game/Managers/JournalManager.cs:76` | Overrides the ASCII/unicode choice the server made | off |
| H14 | `ForceTooltipsOnOldClients` | `Profile.cs:644`; `Game/Managers/MessageManager.cs:94-95`, `Game/Managers/ObjectPropertiesListManager.cs:72-79`, `Game/GameCursor.cs:579` | **Swallows the server's OBJECT speech entirely** and re-manufactures it as an OPL entry (see J3) | **ON** |
| H15 | `SpellDisplayFormat` + Benefic/Harmful/Neutral hues | `Profile.cs:131`; `Game/Managers/MessageManager.cs:180-205` | Rewrites the text and **replaces the server's hue** on spell messages | `"{power} [{spell}]"` |
| H16 | `DisplayPartyChatOverhead` | `Profile.cs:506`; `Game/Managers/MessageManager.cs:127`, `:145` | Party overhead text dropped; also dropped if no party member matches the name | on |
| H17 | `IgnoreManager.IgnoredCharsList` | `Game/Managers/MessageManager.cs:149`, `:240` | Speech from listed senders dropped unless the type is Spell | (list) |
| H18 | `UseCustomLightLevel`, `LightLevel`, `LightLevelType` | `Profile.cs:215-217` | D2/D3/D37 — the server's light level reaches only `RealOverall`/`RealPersonal` | off / 0 / 0 |
| H19 | `AlwaysRun`, `AlwaysRunUnlessHidden` | `Profile.cs:197-198`; `Game/GameObjects/PlayerMobile.cs:1690`, `:1692-1695` | `run` is OR'd with `AlwaysRun`, so the server's Running bit can be overridden **upward**; then forced back down for low stamina / speed mode | on / on |
| H20 | `AutoAvoidObstacules` | `Profile.cs:150` | G7 — rewrites or refuses a server-forced move | on |
| H21 | `IgnoreStaminaCheck` | `Profile.cs:344`; `Game/Pathfinder.cs:161` | Ignores the stamina condition when deciding whether characters block a path | off |
| H22 | `GridLootType` | `Profile.cs:313`; `Network/PacketHandlers.cs:1542-1557`, `:6507-6522` | 1 makes `0x24` return before the container is ever marked `Opened` (B13) | 0 |
| H23 | `UseLargeContainerGumps` | `Profile.cs:332`; `Network/PacketHandlers.cs:1561-1643` | **Substitutes a different container graphic** for the one the server sent, when the replacement texture loads | off |
| H24 | `AutoOpenCorpses`, `AutoOpenCorpseRange`, `CorpseOpenOptions` | `Profile.cs:240-242`; `Game/GameObjects/PlayerMobile.cs:1437-1453`, `Game/Scenes/GameScene.cs:918-926` | Client opens corpses on its own initiative; also refuses while targeting/hidden and clears the queue | on / 2 / 3 |
| H25 | `UseSmoothBoatMovement` | `Profile.cs:342` | G4 / C25 | off |
| H26 | `UseModernShopGump` | `Profile.cs:531`; `Network/PacketHandlers.cs:3648` vs `:2322-2331` | The modern shop gump is never closed by `0x3B` (B27) | off |
| H27 | `UseModernPaperdoll` | `Profile.cs:231`; `Network/PacketHandlers.cs:3315-3329` | On the modern path the `CanLift` flag byte the server sent is read but never applied | off |
| H28 | `UseImprovedBuffBar` | `Profile.cs:435`; `Game/GameObjects/PlayerMobile.cs:270`, `:293` | Gates which buff UI receives the server's buff data | on |
| H29 | `ShowStatsChangedMessage`, `ShowSkillsChangedMessage`, `ShowSkillsChangedDeltaValue` | `Profile.cs:290-292`; `Network/PacketHandlers.cs:636-640`, `:2208-2217` | Suppresses the notification of a server-reported change (the value is still applied) | on / on / 1 |
| H30 | `EnableDeathScreen` | `Profile.cs:179`; `Network/PacketHandlers.cs:1914-1916` | Whether `DeathScreenTimer` is written at all | on |
| H31 | `EnableSound`, `EnableMusic`, `EnableCombatMusic`, `ReproduceSoundsInBackground` | `Profile.cs:90-96`; `Game/Managers/AudioManager.cs:182-185`, `:320-332` | Forces volume to 0 or refuses the server's sound/music outright | on / on / on / off |
| H32 | `EnableTitleBarStats` | `Profile.cs:284`; `Game/Managers/TitleBarStatsManager.cs:11-19` | Suppresses the title-bar reflection of server stats | off |
| H33 | `ShowDPS` | `Profile.cs:446`; `Game/GameObjects/EntityTextContainer.cs:132` | — | on |
| H34 | `ShowNewMobileNameIncoming`, `ShowNewCorpseNameIncoming` | `Profile.cs:308-309`; `Network/PacketHandlers.cs:6734-6749` | Whether the client fires an unsolicited `SingleClick` at newly seen objects | on / on |
| H35 | `OpenHealthBarForLastAttack`, `UseOneHPBarForLastAttack` | `Profile.cs:540`, `:621`; `Game/Managers/TargetManager.cs:194-215` | Whether `0xAA` produces a health bar; with serial 0 a bar for serial 0 can be constructed | on / on |
| H36 | `DisableGrayEnemies` | `Profile.cs:666`; `Views/MobileView.cs:168` | Overrides the notoriety hue for the last-attack target | off |
| H37 | `OverridePartyAndGuildHue` | `Profile.cs:137`; `Views/MobileView.cs:170` | Replaces the server's notoriety hue for party members | off |
| H38 | `NoColorObjectsOutOfRange` | `Profile.cs:158` | E12 | off |
| H39 | `ForceHouseTransparency`, `ForcedHouseTransparency`, `ForcedTransparencyHouseTileHue` | `Profile.cs:661-664`; `Game/GameObjects/House.cs:93-94` | **Overwrites `World.Map.GetTile(x,y).Hue`** and the static `Multi.ForcedTransparency` when building a house | off / 40 / 0 |
| H40 | `ForceResyncOnHang` | `Profile.cs:619`; `Game/Scenes/GameScene.cs:905-909` | Sends `Send_Resync` on the client's own initiative — see I2 | off |
| H41 | `SellAgentEnabled` + `SellAgentMaxUniques` / `SellAgentMaxItems` / per-config `MaxAmount` / `RestockUpTo` | `Profile.cs:649`; `Game/Managers/BuySellAgent.cs:186-269` | Client answers a vendor's sell list by itself, with client-side caps on the amounts | off |
| H42 | `DisableSystemChat` | `Profile.cs:393` | Suppresses system chat display | off |
| H43 | `StandardSkillsGump` | `Profile.cs:306`; `Network/PacketHandlers.cs:2138-2165` | Which gump a server skill list opens, and whether one opens at all | on |
| H44 | `AutoLootManager` configs | `Game/Managers/AutoLootManager.cs:256-304` | The client picks up items on its own, rate-limited by `MoveMultiObjectDelay` (`Profile.cs:485`, default 1000 ms) | (list) |
| H45 | `FollowingMode` / `AutoFollowDistance` | `Game/Scenes/GameScene.cs:951-975` | Client walks on its own initiative and stops following past `ClientViewRange` | off |
| H46 | `MacroManager` view-range macros | `Game/Managers/MacroManager.cs:1665-1719` | The user can overwrite `World.ClientViewRange` locally at any time (clamped to 5..40), changing what E1/E2 cull, **without telling the server** | (macro) |

---

## I. Client-initiated re-request, resync and self-issued queries

| id | file:line | trigger | what the client sends on its own initiative | throttle |
| --- | --- | --- | --- | --- |
| I1 | `Game/Managers/WalkerManager.cs:175-178` | a walk ack the client cannot match to a queued step (`isBadStep`) | `Send_Resync()`, then `ResendPacketResync = true` | suppressed if `ResendPacketResync` already true; cleared at `Network/PacketHandlers.cs:6874` |
| I2 | `Game/Scenes/GameScene.cs:905-909` | no ping reply for > 5000 ms | `Send_Resync()` + a printed "Possible connection hang, resync attempted" | `Time.Ticks - _lastResync > 5000`; only when `ForceResyncOnHang` |
| I3 | `Game/Managers/MacroManager.cs:2161` | user macro | `Send_Resync()` | — |
| I4 | `Network/PacketHandlers.cs:359-379` | queued OPL requests, flushed per frame | `Send_MegaClilocRequest` (batched, CV_5090+) or `Send_MegaClilocRequest_Old` per serial | re-gated on `TooltipsEnabled` (F19) |
| I5 | `Game/Managers/ObjectPropertiesListManager.cs:78-85` | a tooltip is wanted for a serial the client has no OPL for | `PacketHandlers.AddMegaClilocRequest(serial)` | dedup at `Network/PacketHandlers.cs:421-427` |
| I6 | `Network/PacketHandlers.cs:4867-4869`, `:381-390` | `0xBF` cmd 0x1D revision differs, or the house is unknown | `Send_CustomHouseDataRequest` on the next flush | dedup per flush (F17) |
| I7 | `Network/PacketHandlers.cs:4631` | `0xBF` cmd 0x10 path | `Send_MegaClilocRequest_Old(item)` | — |
| I8 | `Game/GameActions.cs:970-997` (`RequestMobileStatus`) | new mobile created (`:6777`), attack target (`:3823`), login (`:2454`), target (`Game/Managers/TargetManager.cs:415`, `:469`), health bar open (`Game/UI/Gumps/HealthBarGump.cs:68`, `:705`, `:1976`) | `Send_StatusRequest(serial)` | only when `HitsRequest < Received` unless `force` |
| I9 | `Game/GameActions.cs:1004-1014` (`SendCloseStatus`) | attack target changes | closes the previous target's status window | needs `CV_200`, `InGame`, valid serial, `HitsRequest >= Pending` |
| I10 | `Game/Managers/MobileStatusRequestQueue.cs:25-39` | queued serials | `GameActions.RequestMobileStatus` on a **thread-pool task**, one per second (`Task.Delay(1000).Wait()`), each also printing `"Processing {serial}"` to the journal | 1 s per serial |
| I11 | `Game/Managers/WorldMapEntityManager.cs:242-266` | world map open | `Send_QueryGuildPosition()`, and `Send_QueryPartyPosition()` if any party member is unknown or beyond `ClientViewRange` | `_lastPacketSend` 250 ms; requires `World.InGame` |
| I12 | `Game/Managers/ForcedTooltipManager.cs:14-25` | a tooltip is wanted on a pre-tooltip shard | `GameActions.SingleClick(serial)` — an unsolicited single-click sent to make the server speak | `DELAY` 500 ms per serial; skipped if a client-invented revision is still in the future |
| I13 | `Network/PacketHandlers.cs:1024`, `:6734-6749` | `0x1B` enter world; a new mobile/corpse appears | `GameActions.SingleClick` | `ShowNewMobileNameIncoming` / `ShowNewCorpseNameIncoming`, and `!obj.IsClicked` |
| I14 | `Network/PacketHandlers.cs:1009-1035` | `0x1B` | `Send_GameWindowSize`, `Send_Language`, `Send_ShowPublicHouseContent`, `Send_ClientVersion` | version-gated (CV_200 / CV_70796) and profile-gated |
| I15 | `Network/PacketHandlers.cs:2473` | `0x55` login complete | `Send_ClientViewRange(World.ClientViewRange)` — the client tells the server what range it wants | `Client.Version >= CV_305D` |
| I16 | `Game/UI/Gumps/ModernOptionsGump.cs:2377-2381` | user moves the view-range slider | `Send_ClientViewRange((byte)i)` | — |
| I17 | `Game/Managers/EnhancedPacketHandler.cs:28-45` | `0xCE` sub 1 | `SendEnhancedPacket(EnableEnhancedPacket)` — note the guard at `Network/EnhancedOutgoingPackets.cs:14` is always satisfied by the line above it | — |
| I18 | `Game/Managers/AutoLootManager.cs:256-304`, `Game/Scenes/GameScene.cs:917-932` | items appear in a corpse | double-clicks / move requests issued by the client | `nextLootTime = Ticks + MoveMultiObjectDelay` (default 1000) |
| I19 | `Game/Managers/BuySellAgent.cs:186-269` | `0x9E` sell list arrives | the client composes and sends a sell packet by itself | `SellAgentEnabled` |
| I20 | `Game/Managers/BandageManager.cs:87-131` | the `Hits` setter fires mid-handler (`Game/GameObjects/Entity.cs:73`) | a heal is issued and `TargetManager.SetAutoTarget(...)` is set — **from inside the `0x2D` handler, before mana/stamina have been applied** | `nextBandageTime` |
| I21 | `Game/Scenes/GameScene.cs:898-901` | every 1000 ms | `NetClient.Socket.Statistics.SendPing()` | 1 s |

---

## J. Client-invented / client-derived state

| id | file:line | what the client makes up | notes |
| --- | --- | --- | --- |
| J1 | `Game/World.cs:87` | `ClientViewRange` starts at `Constants.MAX_VIEW_RANGE` (40) before the server has said anything | drives every entry in section E |
| J2 | `Game/Managers/HouseManager.cs:68`, `:82-103` | `_footprints` — a table of where houses stood, with no server counterpart and no expiry | F1/F2 |
| J3 | `Game/Managers/ForcedTooltipManager.cs:39-58` | **A fabricated OPL revision of `Time.Ticks + 1500`**, and an OPL name/data block synthesized from overhead speech the client swallowed. `World.OPL.Add(serial, Time.Ticks + UPDATE_DELAY, …)` puts a timestamp where the server's revision number belongs, which `Game/Managers/ObjectPropertiesListManager.cs:90-99` then compares against real revisions | `ForceTooltipsOnOldClients` default **on** |
| J4 | `Game/Managers/SpellVisualRangeManager.cs:98-101`, `:105-111` | `World.Player.Flags \|= Flags.Frozen` and `&= ~Flags.Frozen` — the client sets and clears a **server flag** on the player. `OnClilocReceived` (`:82-91`) does this from a `Task.Factory` thread-pool task, i.e. potentially mid-frame during `World.Update` | `FreezeCharacterWhileCasting` per spell |
| J5 | `Game/GameObjects/PlayerMobile.cs:76` vs `Game/GameObjects/Mobile.cs:174-178` | `PlayerMobile.InWarMode` is an auto-property that **shadows** the base `Flags.WarMode` derivation; the base setter is an empty body. For the player the stored bool and `Flags.WarMode` are independent values | `0x72` writes only the bool |
| J6 | `Network/PacketHandlers.cs:717-726` | `WeightMax` computed from Strength when `type < 5` | C20 |
| J7 | `Network/PacketHandlers.cs:2190` | skill `cap` of 1000 | D9 |
| J8 | `Game/Managers/AudioManager.cs:617`, `:650-668`, `:686-800` | `_mapPlayedTrack` / `_mapChoseSilence` / `_silentArea` / `_lastMusicBlock` / `_lastMusicMap` — an entire parallel music authority that plays tracks the server never named | `music_map_mode` default 0 |
| J9 | `Network/PacketHandlers.cs:4025-4036` | a synthetic corpse serial `serial \| 0x80000000` that is not a valid serial of any kind | F8 |
| J10 | `Game/UI/Gumps/MapGump.cs:198`, `:201`, `:222` | pin numbers taken from the local container count, not the wire; the estimated world location is computed only for the first pin ever added | `0x56` |
| J11 | `Network/PacketHandlers.cs:2859` | bulletin-board `variant` decided by a plain string comparison of the poster name against `World.Player.Name` | `0x71` case 2 |
| J12 | `Network/PacketHandlers.cs:4292` | `canEdit` on a character profile derived purely from `serial == World.Player.Serial`; no server-supplied editability flag is read | `0xB8` |
| J13 | `Network/PacketHandlers.cs:3211` | `0x7C` menu style chosen by peeking the first entry's graphic, not a documented packet field | — |
| J14 | `Network/PacketHandlers.cs:5895-5911` | buff description / third string suppressed when the translation is shorter than 2 chars or whitespace; args backfilled from the first entry | `0xDF` |
| J15 | `Game/GameObjects/Mobile.cs:159-169` | `IsDead` derived from `Graphic`, OR'd with a separately settable `_isDead` — so the dead/alive transition that drives `ChangeSeason` at `Network/PacketHandlers.cs:6862` is decided by the graphic the server just sent, not by a death message | — |
| J16 | `Game/GameObjects/PlayerMobile.cs:1526-1535` | `CloseRangedGumps` — the loop body is unreachable (`if (UIManager.Gumps.Count > i) continue;`), so **no ranged gump is ever closed**, despite being called from `0x20`, `0xF3` and `0xF7` | dead code on a live path |
| J17 | `Game/Managers/EnhancedPacketHandler.cs:30` | `if (version >= 0)` on a widened `ushort` can never be false | — |
| J18 | `Game/Managers/WalkerManager.cs:114` | `if (x != -1)` on an int widened from a ushort can never be false | — |

---

## K. Playback and interpolation at rates the server did not specify

| id | file:line | rate | notes |
| --- | --- | --- | --- |
| K1 | `Game/GameObjects/Mobile.cs:783-793` | `stepTime = MovementSpeed.TimeToCompleteMovement(run, mounted)`, then **divided by `Steps.Count`** when more than one step is queued; `maxDelay = Math.Max(1, stepTime - FrameDelay[1])` | **This fork's change.** A full queue plays back five times as fast. The comment at `:769-782` states the alternative is a mobile moving at the wrong speed and then appearing somewhere else |
| K2 | `Game/Data/MovementSpeed.cs:39-51` | compiled-in table: mount-run 100, mount-walk 200, run 200, walk 400 ms | the server never specifies these |
| K3 | `Game/GameObjects/Mobile.cs:758-767` | a mobile is treated as mounted if `delay <= STEP_DELAY_MOUNT_RUN/WALK` | client-side inference to "remove the teleport effect" |
| K4 | `Game/Constants.cs:43` | `CHARACTER_ANIMATION_DELAY = 80` ms | used by `Game/GameObjects/Mobile.cs:615`, `:720`, `:826-827`, `Game/GameObjects/Item.cs:895` |
| K5 | `Game/GameObjects/Mobile.cs:637` | `currentDelay += currentDelay * (_animationInterval + 1)` | the only place the server's animation interval reaches playback |
| K6 | `Game/Constants.cs:44` | `ITEM_EFFECT_ANIMATION_DELAY = 50` ms multiplier on every effect duration | D26 |
| K7 | `Game/Managers/EffectManager.cs:197`, `:222` | FixedXYZ and FixedFrom pass **speed 0** deliberately, to get the 50 ms default | D25 |
| K8 | `Game/GameObjects/MovingEffect.cs:57`, `:62-64` | `MovingEffect` passes duration 0 to its base and rebuilds `IntervalInMs` from speed itself | the wire duration effectively becomes -1 |
| K9 | `Game/GameObjects/Item.cs:855`, `:895` | corpse animation advances one frame per `CHARACTER_ANIMATION_DELAY` and then freezes at the last frame | `frameIndex >= frames.Length` → clamped |
| K10 | `Game/GameObjects/Mobile.cs:275-279`, `:290-292` | idle animation fires at `Time.Ticks + 30000 + rand(0,30000)` and is reseeded on every `SetAnimation` | entirely client-side |
| K11 | `Game/Managers/BoatMovingManager.cs:56`, `:134`, `:255` | `TimeDiff = GetVelocity(speed)` on the first step or an empty queue, otherwise the **wall-clock gap since the last packet** | `_timePacket` at `:74`, `:144` |
| K12 | `Game/Weather.cs:104`, `:262-269` | `_timer = Ticks + Constants.WEATHER_TIMER`; wind re-rolled every `rand(13,19)` seconds | the server sends type/count/temperature only |
| K13 | `Game/World.cs:334-339` | the whole distance cull runs at most every 50 ms | `_timeToDelete` |
| K14 | `Game/Managers/WorldMapEntityManager.cs:200-205`, `:242-246` | world-map expiry once per second; party/guild query at most every 250 ms | E5, I11 |
| K15 | `Game/Scenes/GameScene.cs:898-901` | ping every 1000 ms | I21 |
| K16 | `Game/Managers/MobileStatusRequestQueue.cs:35` | one status request per second, on a background task | I10 |
| K17 | `Game/GameActions.cs` via `Game/Managers/GlobalActionCooldown.cs:15` | `nextActionTime = Ticks + Profile.MoveMultiObjectDelay` (default 1000 ms) | set at login (`Network/PacketHandlers.cs` `0x55` path) |
| K18 | `Game/Managers/ForcedTooltipManager.cs:11-12` | `DELAY = 500`, `UPDATE_DELAY = 1500` | I12, J3 |
| K19 | `Network/PacketHandlers.cs:1916`, `Game/Constants.cs` `DEATH_SCREEN_TIMER = 1500` | death screen holds for 1500 ms | read at `Game/Scenes/GameScene.cs:1667` |
| K20 | `Game/GameObjects/EntityTextContainer.cs:139` | damage overhead text lives `Time.Ticks + 1500` | — |
| K21 | `Game/GameObjects/GameObject.cs:141` | DPS averaged over a 15-second window (`AverageOverTime`) | — |
| K22 | `Game/Managers/HouseDiagnostics.cs` `LogHouseContents` per `Game/Scenes/GameScene.cs:914` | diagnostics sampled every frame | `log_house_diagnostics` off by default |

---

## Load-bearing

### Per-frame — these run inside `World.Update` / `Scene.Update`, every frame or every 50 ms

Packet dispatch happens at `Game/GameController.cs:478`, ahead of `Scene.Update` at `:486`, capped at
`MAX_PACKETS_PER_FRAME = 25` (`Game/GameController.cs:136-146`). Everything below runs in the sweep
that follows, or in the draw pass after it.

| entry | why it is load-bearing |
| --- | --- |
| **E1** `Game/World.cs:356` mobile cull | walks the entire `World.Mobiles` dictionary; fires every 50 ms |
| **E2** `Game/World.cs:436-453` item cull | walks the entire `World.Items` dictionary; the `keepWithin` and `KeptByItsHouse` tests are evaluated per item per sweep |
| **F1/F2** `Game/World.cs:275-288` → `Game/Managers/HouseManager.cs:122-140` | `IsInsideKnownHouse` is a **linear scan of `_footprints` for every ground item, every sweep**, reached from inside E2 |
| **F3/E3** `Game/Managers/HouseManager.cs:167-200` | `IsHouseInRange` does a `World.Items.Get` per call, reached from E2 for every multi and from `Game/GameObjects/Item.cs:639` on every `CheckGraphicChange` |
| **F6** `Game/World.cs:341-349`, `:414-423` | the by-key sweep and the `TryGetValue && IsDestroyed` re-check are the per-frame guard against the stale-key failure documented there |
| **K1/K13** `Game/GameObjects/Mobile.cs:783-793` | `ProcessSteps` runs for every mobile, every frame, from `Mobile.Update` → `ProcessAnimation` |
| **K4/K5/K9/K10** animation timers | `Game/GameObjects/Mobile.cs:603-722` and `Game/GameObjects/Item.cs:849-898` run per entity per frame |
| **F9** `Game/GameObjects/Mobile.cs:698-718` | a corpse removes itself from `World.Mobiles` from inside `ProcessAnimation`, i.e. from inside the `World.Mobiles` enumeration at `Game/World.cs:350` |
| **E4** `Game/Managers/EffectManager.cs:50` | every effect, every frame, from `Game/World.cs:479` |
| **E12** the nine `NoColorObjectsOutOfRange` view sites | evaluated per drawn object per frame |
| **J4** `Game/Managers/SpellVisualRangeManager.cs:82-91` | the `Task.Factory` task writes `World.Player.Flags` **off the main thread**, so it can land inside the `World.Update` sweeps rather than in the packet phase |
| **I2** `Game/Scenes/GameScene.cs:905-909` | the resync condition is tested every frame before `World.Update` |
| **H4/J8** `Game/Managers/AudioManager.cs:686` `UpdateMusicMap` | runs on the audio update path and calls `MusicMapManager.BlockOf(player x,y)` each time |
| **K22** `Game/Scenes/GameScene.cs:914` | `HouseDiagnostics.LogHouseContents()` sits directly after `World.Update()` |

### Per-packet — on the dispatch path, potentially 25× per frame

| entry | why it is load-bearing |
| --- | --- |
| **D14** `Game/GameObjects/Entity.cs:115-133` `FixHue` | called from essentially every object-update handler: `:990`, `:1155`, `:3050`, `:3083`, `:3155`, `:6443`, `:6607`, `:6682`, `:6730`, `:6841` |
| **D15/D16** amount forced to 1 | `Network/PacketHandlers.cs:6684-6687` sits inside `UpdateGameObject`, the common tail of `0x1A`, `0x77`, `0xD2`, `0xD3`, `0x78`, `0xF3`, `0xF7`, `0xF6` |
| **D17/D18/D19** graphic and direction masks | same path, same frequency |
| **G1/G2/G3** step deferral | `Network/PacketHandlers.cs:6703-6726` + `Game/GameObjects/Mobile.cs:304-316`, on every mobile movement packet |
| **C1/C2/C3/G5** self-position discards | `:3045-3052`, `:3078-3085`, `:6829` — every player-addressed update packet |
| **B1** `Network/PacketHandlers.cs:559` | damage silently lost for anything E1/E2 has just culled |
| **B37/B56/B57** `World.Mobiles.Get` without an `IsDestroyed` screen | animation, mana and stamina can be written onto destroyed mobiles awaiting the sweep |
| **F11/F12/F13** container teardown rules | `:6434`, `:3107`, `:6899-6914` decide, per packet, what survives a container update |
| **B95** `0xF7` sub-id abort | one bad sub-id abandons the rest of a batched frame's payload without resynchronising the reader |
| **B33** `0x66` page desync | one out-of-range page misaligns every later page in the same packet |
| **H14/J3** `ForceTooltipsOnOldClients` | `Game/Managers/MessageManager.cs:94-95` is on the message path for **every** OBJECT-type message |
| **I20** `Game/Managers/BandageManager.cs:87` | reached from the `Hits` setter (`Game/GameObjects/Entity.cs:73`), so it re-enters arbitrary code and can send packets **mid-handler**, before `0x2D` has applied mana and stamina |
| **B78/J4** `0xC1`/`0xCC` | disposes gumps and starts an off-thread task before the translation check that may drop the message entirely |
| **A38** `0xCE` | id and version are consumed before the enabled check, on every enhanced packet |

### Rare — login, scene change, map change, house load, user action

`0x55` (B29, D4, I15), `0x8C` / `0x82` / `0x85` / `0x53` / `0x86` / `0xA8` / `0xA9` (B96, D12, D34, C19),
`0xD1` Logout (B97), `0xBF` cmd 0x08 map change → **E8** `InternalMapChangeClear` (`Game/World.cs:965-1019`),
`0xD8` CustomHouse (B83–B86), `0xB9` (B68, B69, D38, D39 — rewrites the shared body-conversion table),
`0xBC` Season and every `ChangeSeason` call (`Game/World.cs:206-234`, which walks every used chunk ×8×8
×every object), `0x1B` EnterWorld (C6, D20), `0x90`/`0xF5` DisplayMap (C9, D32), `0xB0`/`0xDD` gump
creation (F15, F16, B65–B67), `0xD6` MegaCliloc (B80–B82), `H46` macro view-range changes,
`I16` the options-gump slider, `I3` the macro resync, `E9` boat step trim, `A21` waypoint types.
