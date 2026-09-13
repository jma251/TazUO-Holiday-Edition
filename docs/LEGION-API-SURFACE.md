# Legion API — Modern vs your Legacy, full surface

Every public member of the top-level scripting API on both sides, with what it reads.
Use the **Reads** column to judge whether Razor Enhanced can derive the same thing.

- Modern (`LegionAPI.cs`): **207** members
- Legacy (`API.cs`): **149** members

## A. In Modern, missing from Legacy — 62

| Member | What it does | Reads |
|---|---|---|
| `ActiveSpellNames` | Get a list of names for spells that are currently toggled on/active. | SpellDefinition.EmptySpell, SpellDefinition.FullIndexGetSpell, World.ActiveSpellIcons |
| `ActiveSpells` | Get a list of spell ids for spells that are currently toggled on/active. | World.ActiveSpellIcons |
| `AddFriend` | Add a mobile to the friends list by serial number. | World.Mobiles |
| `CheckSoundLog` | Check if the sound log contains a given sound and retrieves it. | — |
| `ClearSoundLog` | Clear your sound log (This is specific for each script). | — |
| `ClientCommand` | Executes a client command as if typed in the game console | GameActions.Print, World.Instance |
| `CloseContextMenus` | Close all menu and context menus open. | UIManager.ContextMenu, UIManager.ForEach, World.Player |
| `Config` | — | — |
| `CooldownExists` | Checks whether a cooldown bar with the given name exists. | — |
| `CreateDropDown` | Use API.Gumps.CreateDropDown instead. | — |
| `CreateModernGump` | Use API.Gumps.CreateModernGump instead. | — |
| `CurrentAbilityNames` | Gets your currently available ability names. | World.Player |
| `DeleteCooldown` | Deletes an existing cooldown bar. | — |
| `Dispose` | — | — |
| `Dress` | Dress from a saved dress configuration. | — |
| `DressItems` | Dress items by serial | World.Items |
| `DropFromCursor` | Drops an item currently held by the mouse cursor into a container or on the ground at a specifi | Client.Game.UO, GameActions.DropItem, World.Map |
| `Events` | — | — |
| `GetAllFriends` | Get all friends as an array of serials. | — |
| `GetAllGumps` | Gets all currently open server-side gumps. | UIManager.Gumps, World.Player |
| `GetAvailableDressOutfits` | Get all available dress configurations. | — |
| `GetClientBounds` | Get the bounds of the client game window. | Client.Game.GetScene, Client.Game.Window |
| `GetClilocString` | Get the string for a cliloc number. | Client.Game.UO |
| `GetGumpContents` | This will return a string of all the text in a server-side gump. | UIManager.GetGumpServer, World.Player |
| `GetHeldItem` | Retrieves serial of the currently held item on the game cursor. | Client.Game.UO, World.Player |
| `GetPartyLeader` | Gets the party leader's serial, or 0 if not in a party. | World.Party |
| `GetPartyMemberSerials` | Gets a list of serials for all current party members, excluding yourself. | World.Party |
| `GetSoundLog` | Get all the sound logs of the last X seconds. | — |
| `GetViewportBounds` | Get the bounds of the game world viewport. | Client.Game.GetScene |
| `GlobalMsg` | Send a chat message via the global chat msg system ( ,message here ). | Socket.Send_ASCIIPromptResponse, Socket.Send_ChatMessageCommand, World.MessageManager |
| `GrayMenuResponseCurrent` | Send a response to the currently open gray menu (text list menu). | Socket.Send_GrayMenuResponse, UIManager.Gumps |
| `IsFriend` | Check if a mobile is in the friends list. | World.Mobiles |
| `IsProcessingMoveQueue` | Check if the move item queue is being processed. You can use this to prevent actions if the que | — |
| `IsScriptRunning` | Check if a legion script is currently running. | — |
| `IsSpellActive` | Check if a toggle spell/move is currently active. | SpellDefinition.TryGetSpellFromName, World.ActiveSpellIcons |
| `KnownAbilityNames` | Gets an array of all known ability names | — |
| `LastSpellIndex` | The index of the last spell cast by the player. | GameActions.LastSpellIndex, SpellDefinition.FullIndexGetSpell |
| `LastSpellName` | The name of the last spell cast by the player. | GameActions.LastSpellIndex, SpellDefinition.FullIndexGetSpell |
| `LegionAPI` | — | — |
| `ListRunningScripts` | Get the paths of all currently running legion scripts. | — |
| `MenuItemsCurrent` | Retrieve the current open menu's (uses the latest MenuGump) menu item descriptions. | UIManager.Gumps |
| `MenuResponseCurrent` | Send a response to the currently open menu (uses the latest MenuGump). | Socket.Send_MenuResponse, UIManager.Gumps |
| `OnHotKey` | Register or unregister a Python callback for a hotkey. | — |
| `OnStop` | Register an optional callback to run when this script is being stopped. | ProfileManager.CurrentProfile |
| `Organizer` | Runs an organizer agent to move items between containers. | GameActions.Print |
| `PickUpToCursor` | Picks up an item from the game world and places it onto the mouse cursor. | Client.Game.UO, GameActions.PickUp |
| `PlaySound` | Play a sound effect locally (only audible to you). | Client.Game.Audio |
| `QueueMoveItem` | Move an item to another container. | — |
| `QueueMoveItemOffset` | Move an item to the ground near you. | World.Map, World.Player |
| `RemoveFriend` | Remove a mobile from the friends list by serial number. | — |
| `RemoveTimedCallback` | Removes a previously scheduled timed callback | — |
| `RequestOPLData` | Requests Object Property List (OPL) data for the specified serials. | World.OPL |
| `RestartCooldown` | Restarts the countdown of an existing cooldown bar to its full duration. | — |
| `ScheduleTimedCallback` | Schedules a callback to be invoked after a specified delay. | — |
| `ScriptName` | Get this scripts full filename | World.Player |
| `ScriptPath` | Get the full path to the file, no filename included. Use API.ScriptName to get the script. | World.Player |
| `SetWarMode` | Sets the player's war mode state (peace/war toggle). | GameActions.RequestWarMode, World.Player |
| `SoundEntries` | — | — |
| `UnIgnoreObject` | Removes an item or mobile from your ignore list. | — |
| `Undress` | Undress from a saved dress configuration. | — |
| `UndressAll` | Undress all your equipment | — |
| `UpdateCooldown` | Updates an existing cooldown bar. Only the provided values are applied. | — |

## B. Present in both — 145

Already in your client. If the matching RE call is broken, the logic to copy is here.

| Member | What it does | Reads (Legacy) |
|---|---|---|
| `ActiveBuffs` | Get a list of all buffs that are active. | World.Player |
| `AddControlOnClick` | Use API.Gumps.AddControlOnClick instead. | — |
| `AddControlOnDisposed` | Use API.Gumps.AddControlOnDisposed instead. | — |
| `AddGump` | Use API.Gumps.AddGump instead | UIManager.Add |
| `AddMapMarker` | Add a marker to the current World Map (If one is open) | UIManager.GetGump, World.MapIndex, World.Player |
| `AllyMsg` | Send a message to your alliance. | GameActions.Say, ProfileManager.CurrentProfile |
| `Attack` | Attack a mobile | GameActions.Attack |
| `AutoFollow` | Automatically follow a mobile. This is different from pathfinding. This will continue to follow | ProfileManager.CurrentProfile |
| `AutoLootContainer` | Use autoloot on a specific container. | NetClient.Socket |
| `Backpack` | Get the player's backpack serial | World.Player |
| `BandageSelf` | Attempt to bandage yourself. Older clients this will not work, you will need to find a bandage, | GameActions.BandageSelf, World.Player |
| `Bank` | Return the player's bank container serial if open, otherwise 0 | World.Player |
| `BuffExists` | Check if a buff is active. | World.Player |
| `CancelAutoFollow` | Cancel auto follow mode. | ProfileManager.CurrentProfile, World.Player |
| `CancelPathfinding` | Cancel pathfinding. | Pathfinder.StopAutoWalk |
| `CancelPreTarget` | Cancels any active pre-target. | TargetManager.NextAutoTarget |
| `CancelTarget` | Cancel targeting. | TargetManager.CancelTarget |
| `CastSpell` | Attempt to cast a spell by its name. | GameActions.CastSpellByName, World.Player |
| `ClearIgnoreList` | Clears the ignore list. Allowing functions to see those items again. | — |
| `ClearJournal` | Clear your journal(This is specific for each script). | — |
| `ClearLeftHand` | If you have an item in your left hand, move it to your backpack | World.Player |
| `ClearMoveQueue` | Clear the move item que of all items. | Client.Game.GetScene |
| `ClearRightHand` | If you have an item in your right hand, move it to your backpack | World.Player |
| `ClearSharedVars` | Clear all shared vars. | — |
| `ClickObject` | Single click an object | GameActions.Double, GameActions.DoubleClick, GameActions.SingleClick |
| `CloseGump` | Close the last gump open, or a specific gump. | UIManager.GetGumpServer, World.Player |
| `CloseGumps` | Close all gumps created by the API unless marked to remain open. | — |
| `Contents` | Get an item count for the contents of a container | World.Items |
| `ContextMenu` | Send a context menu(right click menu) response by matching the entry text. | NetClient.Socket |
| `CreateCooldownBar` | Create a cooldown bar. | — |
| `CreateGump` | Use API.Gumps.CreateGump instead | — |
| `CreateGumpButton` | Use API.Gumps.CreateGumpButton instead. | — |
| `CreateGumpCheckbox` | Use API.Gumps.CreateGumpCheckbox instead. | — |
| `CreateGumpColorBox` | Use API.Gumps.CreateGumpColorBox instead. | — |
| `CreateGumpItemPic` | Use API.Gumps.CreateGumpItemPic instead. | — |
| `CreateGumpLabel` | Use API.Gumps.CreateGumpLabel instead. | — |
| `CreateGumpPic` | Use API.Gumps.CreateGumpPic instead. | — |
| `CreateGumpRadioButton` | Use API.Gumps.CreateGumpRadioButton instead. | — |
| `CreateGumpScrollArea` | Use API.Gumps.CreateGumpScrollArea instead. | — |
| `CreateGumpSimpleProgressBar` | Use API.Gumps.CreateGumpSimpleProgressBar instead. | — |
| `CreateGumpTTFLabel` | Use API.Gumps.CreateGumpTTFLabel instead. | — |
| `CreateGumpTextBox` | Use API.Gumps.CreateGumpTextBox instead. | — |
| `CreateSimpleButton` | Use API.Gumps.CreateSimpleButton instead. | — |
| `Dismount` | Attempt to dismount if mounted. | GameActions.DoubleClick, GameActions.DoubleClickQueued, World.Player |
| `DisplayRange` | Show a radius around the player. | ProfileManager.CurrentProfile |
| `EmoteMsg` | Emote a message. | GameActions.Say, NetClient.Socket, ProfileManager.CurrentProfile |
| `EquipItem` | Attempt to equip an item. Layer is automatically detected. | Client.Game.GetScene, GameActions.Equip, GameActions.PickUp |
| `FindItem` | Try to get an item by its serial. | World.Items |
| `FindLayer` | Attempt to find an item on a layer. | World.Mobiles, World.Player |
| `FindMobile` | Get a mobile from its serial. | World.Mobiles |
| `FindType` | Attempt to find an item by type(graphic). | — |
| `FindTypeAll` | Return a list of items matching the parameters set. | — |
| `Found` | The serial of the last item or mobile from the various findtype/mobile methods | — |
| `GetAllMobiles` | Return a list of all mobiles the client is aware of, optionally filtered by graphic, distance,  | World.Map, World.Mobiles |
| `GetGump` | Get a gump by ID. | UIManager.GetGumpServer, World.Player |
| `GetItemsOnGround` | Get all items on the ground within specified range. | World.Items |
| `GetJournalEntries` | Get all the journal entires in the last X seconds. | — |
| `GetMap` | Get the current map index. | World.MapIndex |
| `GetMultisAt` | Gets all multi objects at a specific position (x, y coordinates). | World.HouseManager, World.Map |
| `GetMultisInArea` | Gets all multi objects within a rectangular area defined by coordinates. | World.HouseManager, World.Map |
| `GetPath` | Attempt to build a path to a location.  This will fail with large distances. | Pathfinder.GetPathTo, World.Map, p.X |
| `GetPersistentVar` | Get a persistent variable. | GameActions.Print |
| `GetSharedVar` | Get the value of a shared variable. | — |
| `GetSkill` | Get a skill from the player. See the Skill class for what properties are available: https://git | World.Player |
| `GetStaticsAt` | Gets all static objects at a specific position (x, y coordinates). | World.Map |
| `GetStaticsInArea` | Gets all static objects within a rectangular area defined by coordinates. | World.Map |
| `GetTile` | Get the tile at a location. | World.Map |
| `GuildMsg` | Send your guild a message. | GameActions.Say, ProfileManager.CurrentProfile |
| `GumpContains` | Check if a gump contains a specific text. | UIManager.GetGumpServer, World.Player |
| `HasGump` | Check if a player has a server gump. Leave blank to check if they have any server gump. | World.Player |
| `HasTarget` | Check if the player has a target cursor. | TargetManager.IsTargeting, TargetManager.TargetingType |
| `HeadMsg` | Show a message above a mobile or item, this is only visible to you. | ProfileManager.CurrentProfile, World.Get |
| `IgnoreObject` | Adds an item or mobile to your ignore list. | — |
| `InJournal` | Check if your journal contains a message. | — |
| `InJournalAny` | Check if the journal contains *any* of the strings in this list. | — |
| `IsGlobalCooldownActive` | Check if the global cooldown is currently active. This applies to actions like moving or using  | GameActions.Print |
| `IsProcessingUseItemQueue` | Check if the use item queue is being processed. You can use this to prevent actions if the queu | — |
| `ItemNameAndProps` | Gets item name and properties. | World.OPL |
| `ItemsInContainer` | Get all items in a container. | — |
| `JournalEntries` | — | World.Player |
| `LastTargetGraphic` | The graphic of the last targeting object | TargetManager.LastTargetInfo |
| `LastTargetPos` | The last target's position | TargetManager.LastTargetInfo |
| `LastTargetSerial` | The serial of the last target, if it has a serial. | TargetManager.LastTargetInfo |
| `Logout` | Logout of the game. | GameActions.Logout |
| `MarkTile` | Mark a tile with a specific hue. | World.Map |
| `Mount` | Attempt to mount(double click) | GameActions.DoubleClick, GameActions.DoubleClickQueued, ProfileManager.CurrentProfile |
| `MoveItem` | Move an item to another container. | GameActions.DropItem, GameActions.PickUp |
| `MoveItemOffset` | Move an item to the ground near you. | GameActions.PickUp, World.Map, World.Player |
| `Msg` | Say a message outloud. | GameActions.Say, ProfileManager.CurrentProfile |
| `NearestCorpse` | Get the nearest corpse within a distance. | — |
| `NearestEntity` | Find the nearest item/mobile based on scan type. | World.Get |
| `NearestMobile` | Get the nearest mobile by Notoriety. | World.Mobiles, World.Player |
| `NearestMobiles` | Get all mobiles matching Notoriety and distance. | World.Mobiles, World.Player |
| `OnIgnoreList` | Check if a serial is on the ignore list. | — |
| `PartyMsg` | Send a message to your party. | GameActions.Say, GameActions.SayParty, ProfileManager.CurrentProfile |
| `Pathfind` | Attempt to pathfind to a location.  This will fail with large distances. | Pathfinder.AutoWalking, Pathfinder.WalkTo, World.Map |
| `PathfindEntity` | Attempt to pathfind to a mobile or item. | Pathfinder.WalkTo, World.Get |
| `Pathfinding` | Check if you are already pathfinding. | Pathfinder.AutoWalking, Pathfinder.StopAutoWalk |
| `Pause` | Pause the script. | — |
| `PlayScript` | Play a legion script. | GameActions.Print |
| `Player` | Returns the player character object | World.Player |
| `PreTarget` | Sets a pre-target that will be automatically applied when the next targeting request comes from | — |
| `PrimaryAbilityActive` | Check if your primary ability is active. | World.Player |
| `ProcessCallbacks` | Use this when you need to wait for players to click buttons. | — |
| `PromptResponse` | Send a response to a server prompt(Like renaming a rune for example). | NetClient.Socket |
| `Random` | Can be used for random numbers. | TargetManager.LastTargetInfo |
| `RemoveMapMarker` | Remove a marker from the world map. | UIManager.GetGump |
| `RemoveMarkedTile` | Remove a marked tile. See MarkTile for more info. | World.Map |
| `RemovePersistentVar` | Delete/remove a persistent variable. | GameActions.Print |
| `RemoveSharedVar` | Try to remove a shared variable. | — |
| `Rename` | Attempt to rename something like a pet. | GameActions.DoubleClick, GameActions.Rename, World.Player |
| `ReplyGump` | Reply to a gump. | GameActions.ReplyGump, UIManager.GetGumpServer, World.Player |
| `RequestAnyTarget` | Prompts the player to target any object in the game world, including an Item, Mobile, Land tile | TargetManager.IsTargeting, TargetManager.LastTargetInfo, TargetManager.SetTargeting |
| `RequestTarget` | Request the player to target something. | TargetManager.IsTargeting, TargetManager.LastTargetInfo, TargetManager.SetTargeting |
| `Run` | Run in a direction. | World.Player |
| `SavePersistentVar` | Save a variable that persists between sessions and scripts. | GameActions.Print |
| `SecondaryAbilityActive` | Check if your secondary ability is active. | World.Player |
| `SetMount` | This will set your saved mount for this character. | ProfileManager.CurrentProfile |
| `SetSharedVar` | Set a variable that is shared between scripts. | — |
| `SetSkillLock` | Set a skills lock status. | World.Player |
| `SetStatLock` | Set a skills lock status. | GameActions.ChangeStatLock |
| `Stop` | Stops the current script. | ProfileManager.CurrentProfile |
| `StopScript` | Stop a legion script. | GameActions.Print |
| `SysMsg` | Show a system message(Left side of screen). | GameActions.Print, GameActions.Say, ProfileManager.CurrentProfile |
| `Target` | Target a location. Include graphic if targeting a static. | TargetManager.Target |
| `TargetLandRel` | Target a land tile relative to your position. | TargetManager.IsTargeting, World.Map, World.Player |
| `TargetResource` | This will attempt to use an item and target a resource, some servers may not support this. | Socket.Send_TargetByResource, TargetManager.CancelTarget |
| `TargetSelf` | Target yourself. | TargetManager.IsTargeting, TargetManager.Target, World.Player |
| `TargetTileRel` | Target a tile relative to your location. | TargetManager.IsTargeting, World.Map, World.Player |
| `ToggleAbility` | Toggle an ability. | GameActions.UsePrimaryAbility, GameActions.UseSecondaryAbility, NetClient.Socket |
| `ToggleAutoLoot` | Toggle autolooting on or off. | ProfileManager.CurrentProfile |
| `ToggleFly` | Toggle flying if you are a gargoyle. | NetClient.Socket, World.Player |
| `ToggleScript` | Toggle another script on or off. | — |
| `TrackingArrow` | Create a tracking arrow pointing towards a location. | UIManager.Add, UIManager.GetGump |
| `Turn` | Turn your character a specific direction. | World.Player |
| `UseObject` | Attempt to use(double click) an object. | GameActions.DoubleClick, GameActions.DoubleClickQueued |
| `UseSkill` | Use a skill. | GameActions.UseSkill, World.Player |
| `UseType` | Attempt to use the first item found by graphic(type). | GameActions.DoubleClick, GameActions.DoubleClickQueued |
| `Virtue` | Use a virtue. | NetClient.Socket |
| `WaitForGump` | Wait for a server-side gump. | UIManager.GetGumpServer, World.Player |
| `WaitForTarget` | Wait for a target cursor. | TargetManager.IsTargeting, TargetManager.TargetingType |
| `Walk` | Walk in a direction. | World.Player |
| `WhisperMsg` | Whisper a message. | GameActions.Say, ProfileManager.CurrentProfile |
| `YellMsg` | Yell a message. | GameActions.Say, ProfileManager.CurrentProfile |
| `new` | Access useful player settings. | — |

## C. In Legacy only — 4

| Member | What it does | Reads |
|---|---|---|
| `API` | — | GameActions.Print |
| `IsProcessingMoveQue` | Check if the move item queue is being processed. You can use this to prevent actions if the que | — |
| `QueMoveItem` | Move an item to another container. | Client.Game.GetScene |
| `QueMoveItemOffset` | Move an item to the ground near you. | Client.Game.GetScene, World.Map, World.Player |

---

# `Player.*` — the biggest single gap

Modern exposes **54** members on `ApiPlayer`. Legacy's nearest equivalent (`PyMobile`) exposes **13**.

**The important column is the last one.** Where it names a class, the value is *already in your Legacy client* — 
it simply isn't surfaced through the scripting API. That also means Razor can read it: it comes off the 
status packet (`0x11`) like everything else here.

| `Player.` member | Modern reads | Backing property in YOUR client |
|---|---|---|
| `ColdResistance` | `ReadPlayer(static p => p.ColdResistance)` | **PlayerMobile** ✅ |
| `DamageIncrease` | `ReadPlayer(static p => p.DamageIncrease)` | **PlayerMobile** ✅ |
| `DamageMax` | `ReadPlayer(static p => p.DamageMax)` | **PlayerMobile** ✅ |
| `DamageMin` | `ReadPlayer(static p => p.DamageMin)` | **PlayerMobile** ✅ |
| `DefenseChanceIncrease` | `ReadPlayer(static p => p.DefenseChanceIncrease` | **PlayerMobile** ✅ |
| `DexLock` | `ReadPlayer(static p => p.DexLock, Lock.Up)` | **PlayerMobile** ✅ |
| `Dexterity` | `ReadPlayer(static p => p.Dexterity)` | **PlayerMobile** ✅ |
| `DexterityIncrease` | `ReadPlayer(static p => p.DexterityIncrease)` | **PlayerMobile** ✅ |
| `EnergyResistance` | `ReadPlayer(static p => p.EnergyResistance)` | **PlayerMobile** ✅ |
| `EnhancePotions` | `ReadPlayer(static p => p.EnhancePotions)` | **PlayerMobile** ✅ |
| `FasterCastRecovery` | `ReadPlayer(static p => p.FasterCastRecovery)` | **PlayerMobile** ✅ |
| `FasterCasting` | `ReadPlayer(static p => p.FasterCasting)` | **PlayerMobile** ✅ |
| `FireResistance` | `ReadPlayer(static p => p.FireResistance)` | **PlayerMobile** ✅ |
| `Followers` | `ReadPlayer(static p => p.Followers)` | **PlayerMobile** ✅ |
| `FollowersMax` | `ReadPlayer(static p => p.FollowersMax)` | **PlayerMobile** ✅ |
| `Gold` | `ReadPlayer(static p => p.Gold)` | **PlayerMobile** ✅ |
| `HitChanceIncrease` | `ReadPlayer(static p => p.HitChanceIncrease)` | **PlayerMobile** ✅ |
| `HitPointsIncrease` | `ReadPlayer(static p => p.HitPointsIncrease)` | **PlayerMobile** ✅ |
| `HitPointsRegeneration` | `ReadPlayer(static p => p.HitPointsRegeneration` | **PlayerMobile** ✅ |
| `IntLock` | `ReadPlayer(static p => p.IntLock, Lock.Up)` | **PlayerMobile** ✅ |
| `Intelligence` | `ReadPlayer(static p => p.Intelligence)` | **PlayerMobile** ✅ |
| `IntelligenceIncrease` | `ReadPlayer(static p => p.IntelligenceIncrease)` | **PlayerMobile** ✅ |
| `IsCasting` | `ReadPlayer(static p => p.IsCasting)` | — not present ❌ |
| `IsRecovering` | `ReadPlayer(static p => p.IsRecovering)` | — not present ❌ |
| `IsWalking` | `ReadPlayer(static p => p.IsWalking)` | **PlayerMobile** ✅ |
| `LowerManaCost` | `ReadPlayer(static p => p.LowerManaCost)` | **PlayerMobile** ✅ |
| `LowerReagentCost` | `ReadPlayer(static p => p.LowerReagentCost)` | **PlayerMobile** ✅ |
| `Luck` | `ReadPlayer(static p => p.Luck)` | **PlayerMobile** ✅ |
| `ManaIncrease` | `ReadPlayer(static p => p.ManaIncrease)` | **PlayerMobile** ✅ |
| `ManaRegeneration` | `ReadPlayer(static p => p.ManaRegeneration)` | **PlayerMobile** ✅ |
| `MaxColdResistance` | `ReadPlayer(static p => p.MaxColdResistence)` | **PlayerMobile** ✅ |
| `MaxDefenseChanceIncrease` | `ReadPlayer(static p => p.MaxDefenseChanceIncre` | **PlayerMobile** ✅ |
| `MaxEnergyResistance` | `ReadPlayer(static p => p.MaxEnergyResistence)` | **PlayerMobile** ✅ |
| `MaxFireResistance` | `ReadPlayer(static p => p.MaxFireResistence)` | **PlayerMobile** ✅ |
| `MaxHitPointsIncrease` | `ReadPlayer(static p => p.MaxHitPointsIncrease)` | **PlayerMobile** ✅ |
| `MaxManaIncrease` | `ReadPlayer(static p => p.MaxManaIncrease)` | **PlayerMobile** ✅ |
| `MaxPhysicResistance` | `ReadPlayer(static p => p.MaxPhysicResistence)` | **PlayerMobile** ✅ |
| `MaxPoisonResistance` | `ReadPlayer(static p => p.MaxPoisonResistence)` | **PlayerMobile** ✅ |
| `MaxStaminaIncrease` | `ReadPlayer(static p => p.MaxStaminaIncrease)` | **PlayerMobile** ✅ |
| `PhysicalResistance` | `ReadPlayer(static p => p.PhysicalResistance)` | **PlayerMobile** ✅ |
| `PoisonResistance` | `ReadPlayer(static p => p.PoisonResistance)` | **PlayerMobile** ✅ |
| `Position` | `ReadPlayer(static p => new ApiPoint3D { X = p.` | **Mobile** ✅ |
| `ReflectPhysicalDamage` | `ReadPlayer(static p => p.ReflectPhysicalDamage` | **PlayerMobile** ✅ |
| `SpellDamageIncrease` | `ReadPlayer(static p => p.SpellDamageIncrease)` | **PlayerMobile** ✅ |
| `StaminaIncrease` | `ReadPlayer(static p => p.StaminaIncrease)` | **PlayerMobile** ✅ |
| `StaminaRegeneration` | `ReadPlayer(static p => p.StaminaRegeneration)` | **PlayerMobile** ✅ |
| `StatsCap` | `ReadPlayer(static p => p.StatsCap)` | **PlayerMobile** ✅ |
| `StrLock` | `ReadPlayer(static p => p.StrLock, Lock.Up)` | **PlayerMobile** ✅ |
| `Strength` | `ReadPlayer(static p => p.Strength)` | **PlayerMobile** ✅ |
| `StrengthIncrease` | `ReadPlayer(static p => p.StrengthIncrease)` | **PlayerMobile** ✅ |
| `SwingSpeedIncrease` | `ReadPlayer(static p => p.SwingSpeedIncrease)` | **PlayerMobile** ✅ |
| `TithingPoints` | `ReadPlayer(static p => p.TithingPoints)` | **PlayerMobile** ✅ |
| `Weight` | `ReadPlayer(static p => p.Weight)` | **PlayerMobile** ✅ |
| `WeightMax` | `ReadPlayer(static p => p.WeightMax)` | **PlayerMobile** ✅ |

**52 of 54 already exist in your client.** Only 2 have no backing data at all.

---

## How to use this

Three questions per Razor call you want to fix:

1. **Is it in section B?** Then your client already does it — read the `Reads` column,
   that is the derivation to copy into RE.
2. **Is it in section A?** Modern has it, you don't. The `Reads` column still tells you
   what it is built from, and almost all of it is client state RE can see in packets.
3. **Is it a `Player.` member?** See the last table. 52 of the 54 Modern exposes are
   already sitting in your client unexposed.

Razor sees every packet the client sees. So "the client would have to feed it" is almost
never the blocker — the blocker is knowing *which* packet and *what derivation*. That is
what these tables are for.

### Two things that are not what they look like

- **`IsRecovering`** is not implemented in Modern either. Its source reads
  `public bool IsRecovering => IsCasting; //May incorporate this again later`.
  Reproducing it gets you a duplicate of `IsCasting`.
- **`IsCasting` flaps false on damage.** Modern's own self-heal code documents this:
  *"the client clears its casting flag on any HP change, so a hit mid-cast flaps
  IsCasting off constantly while healing under fire — even though the cast is still
  going."* Anything built on a naive read will misfire in a fight.
