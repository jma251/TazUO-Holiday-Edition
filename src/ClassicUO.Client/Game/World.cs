#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.IO.Audio;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Utility.Logging;
using ClassicUO.Assets;
using ClassicUO.Game.UI;

namespace ClassicUO.Game
{
    public static class World
    {
        private static readonly EffectManager _effectManager = new EffectManager();
        private static readonly List<uint> _toRemove = new List<uint>();
        private static uint _timeToDelete;

        public static Point RangeSize;

        public static PlayerMobile Player { get; private set; }

        public static HouseCustomizationManager CustomHouseManager;

        public static WorldMapEntityManager WMapManager = new WorldMapEntityManager();

        public static ActiveSpellIconsManager ActiveSpellIcons = new ActiveSpellIconsManager();

        public static uint LastObject, ObjectToRemove;

        public static ObjectPropertiesListManager OPL { get; } = new ObjectPropertiesListManager();
        public static DurabilityManager DurabilityManager { get; } = new DurabilityManager();

        public static CorpseManager CorpseManager { get; } = new CorpseManager();

        public static PartyManager Party { get; } = new PartyManager();

        public static HouseManager HouseManager { get; } = new HouseManager();

        public static Dictionary<uint, Item> Items { get; } = new Dictionary<uint, Item>();

        public static Dictionary<uint, Mobile> Mobiles { get; } = new Dictionary<uint, Mobile>();

        public static Map.Map Map { get; private set; }

        // What the server has granted, overwritten by its answer to what was asked for.
        // Starts at the stock range rather than the slider's ceiling: on a client old
        // enough to skip the 0x55 handshake the setting is never applied, and starting
        // at the ceiling meant culling at 40 all session with nothing to justify it.
        public static byte ClientViewRange { get; set; } = Constants.DEFAULT_VIEW_RANGE;

        public static bool SkillsRequested { get; set; }

        public static Season Season { get; private set; } = Season.Summer;
        public static Season OldSeason { get; set; } = Season.Summer;

        // The last track the server actually named, kept so coming back from the death
        // screen restores it. -1 until the server has named one, which means the death
        // screen leaves the music alone rather than starting track 0 out of nowhere.
        public static int OldMusicIndex { get; set; } = -1;

        public static WorldTextManager WorldTextManager { get; } = new WorldTextManager();

        public static JournalManager Journal { get; } = new JournalManager();

        public static CoolDownBarManager CoolDownBarManager { get; } = new CoolDownBarManager();


        public static int MapIndex
        {
            get => Map?.Index ?? -1;
            set
            {
                if (MapIndex != value)
                {
                    InternalMapChangeClear(true);

                    if (value < 0 && Map != null)
                    {
                        Map.Destroy();
                        Map = null;

                        return;
                    }

                    if (Map != null)
                    {
                        if (MapIndex >= 0)
                        {
                            Map.Destroy();
                        }

                        ushort x = Player.X;
                        ushort y = Player.Y;
                        sbyte z = Player.Z;

                        Map = null;

                        if (value >= MapLoader.MAPS_COUNT)
                        {
                            value = 0;
                        }
                        MapLoader.Instance.LoadMap(value, ClientFeatures.Flags.HasFlag(CharacterListFlags.CLF_UNLOCK_FELUCCA_AREAS));
                        Map = new Map.Map(value);

                        Player.SetInWorldTile(x, y, z);
                        Player.ClearSteps();
                    }
                    else
                    {
                        MapLoader.Instance.LoadMap(value, ClientFeatures.Flags.HasFlag(CharacterListFlags.CLF_UNLOCK_FELUCCA_AREAS));
                        Map = new Map.Map(value);
                    }

                    // force cursor update when switching map
                    if (Client.Game.GameCursor != null)
                    {
                        Client.Game.GameCursor.Graphic = 0xFFFF;
                    }

                    UoAssist.SignalMapChanged(value);
                }
            }
        }

        public static bool InGame => Player != null && Map != null;

        public static IsometricLight Light { get; } = new IsometricLight
        {
            Overall = 0,
            Personal = 0,
            RealOverall = 0,
            RealPersonal = 0
        };

        public static LockedFeatures ClientLockedFeatures { get; } = new LockedFeatures();

        public static ClientFeatures ClientFeatures { get; } = new ClientFeatures();

        public static string ServerName { get; set; } = "_";



        public static void CreatePlayer(uint serial)
        {
            if (ProfileManager.CurrentProfile == null)
            {
                string lastChar = LastCharacterManager.GetLastCharacter(LoginScene.Account, World.ServerName);
                ProfileManager.Load(World.ServerName, LoginScene.Account, lastChar);
            }

            if (Player != null)
            {
                Clear();
            }

            Player = new PlayerMobile(serial);
            Mobiles.Add(Player);
            EventSink.InvokeOnPlayerCreated();
            Log.Trace($"Player [0x{serial:X8}] created");
        }

        /// <summary>
        /// Changes the season, and optionally the music with it. A negative index
        /// means the caller has no track to offer and the music is left alone - which
        /// is the case for the season packet, whose second byte is a flag and not an
        /// index at all.
        /// </summary>
        public static void ChangeSeason(Season season, int music = -1)
        {
            Season = season;

            foreach (Chunk chunk in Map.GetUsedChunks())
            {
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        for (GameObject obj = chunk?.GetHeadObject(x, y); obj != null; obj = obj.TNext)
                        {
                            obj.UpdateGraphicBySeason();
                        }
                    }
                }
            }

            if (music < 0)
            {
                return;
            }

            //TODO(deccer): refactor this out into _audioPlayer.PlayMusic(...)
            if (Client.Game.Audio.CanSeasonMusicTakeOver())
            {
                Client.Game.Audio.PlayMusic(music, false);
            }
        }


        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckToRemove(Entity obj, int distance)
        {
            if (Player == null || obj.Serial == Player.Serial)
                return false;

            return Math.Max(Math.Abs(obj.X - RangeSize.X), Math.Abs(obj.Y - RangeSize.Y)) > distance;
        }
        */

        public static void Update()
        {
            if (Player != null)
            {
                if (SerialHelper.IsValid(ObjectToRemove))
                {
                    Item rem = Items.Get(ObjectToRemove);
                    ObjectToRemove = 0;

                    if (rem != null)
                    {
                        Entity container = Get(rem.Container);

                        RemoveItem(rem, true);

                        if (rem.Layer == Layer.OneHanded || rem.Layer == Layer.TwoHanded)
                        {
                            Player.UpdateAbilities();
                        }

                        if (container != null)
                        {
                            if (SerialHelper.IsMobile(container.Serial))
                            {
                                UIManager.GetGump<PaperDollGump>(container.Serial)?.RequestUpdateContents();
                                UIManager.GetGump<ModernPaperdoll>(container.Serial)?.RequestUpdateContents();
                            }
                            else if (SerialHelper.IsItem(container.Serial))
                            {
                                UIManager.GetGump<ContainerGump>(container.Serial)?.RequestUpdateContents();
                                #region GridContainer
                                UIManager.GetGump<GridContainer>(container.Serial)?.RequestUpdateContents();
                                #endregion

                                if (container.Graphic == 0x2006)
                                {
                                    UIManager.GetGump<GridLootGump>(container)?.RequestUpdateContents();
                                    UIManager.GetGump<NearbyLootGump>(container)?.RequestUpdateContents();
                                }
                            }
                        }
                    }
                }

                bool do_delete = _timeToDelete < Time.Ticks;

                if (do_delete)
                {
                    _timeToDelete = Time.Ticks + 50;
                }

                // Walked by key, and taken out by key below.
                //
                // The key an entity is filed under and the serial the entity itself
                // carries can drift apart. A destroyed entity goes back to a pool, and
                // the pool can hand it out again under a different serial before this
                // sweep runs - so the old key is left pointing at a live object whose
                // serial is now something else. Taking the entry out by the object's
                // serial then removes the wrong one and leaves the stale key behind for
                // the rest of the session, where nothing can ever look it up again.
                foreach (KeyValuePair<uint, Mobile> pair in Mobiles)
                {
                    Mobile mob = pair.Value;

                    mob.Update();

                    if (do_delete && mob.Distance > ClientViewRange)
                    {
                        RemoveMobile(mob);
                    }

                    if (mob.IsDestroyed)
                    {
                        _toRemove.Add(pair.Key);
                    }
                    else
                    {
                        if (mob.NotorietyFlag == NotorietyFlag.Ally)
                        {
                            WMapManager.AddOrUpdate
                            (
                                mob.Serial,
                                mob.X,
                                mob.Y,
                                MathHelper.PercetangeOf(mob.Hits, mob.HitsMax),
                                MapIndex,
                                true,
                                mob.Name
                            );
                        }
                        else if (Party.Leader != 0 && Party.Contains(mob))
                        {
                            WMapManager.AddOrUpdate
                            (
                                mob.Serial,
                                mob.X,
                                mob.Y,
                                MathHelper.PercetangeOf(mob.Hits, mob.HitsMax),
                                MapIndex,
                                false,
                                mob.Name
                            );
                        }
                    }
                }

                if (_toRemove.Count != 0)
                {
                    for (int i = 0; i < _toRemove.Count; i++)
                    {
                        // Still destroyed, and still the one that was condemned - never
                        // take an entry out on the strength of a serial alone.
                        if (Mobiles.TryGetValue(_toRemove[i], out Mobile gone) && gone.IsDestroyed)
                        {
                            Mobiles.Remove(_toRemove[i]);

                            // Only now, with the entry out. See Item.ReturnToPool.
                            gone.ReturnToPool();
                        }
                    }

                    _toRemove.Clear();
                }

                // By key, for the reason given over the mobile sweep above. This is the
                // one that was caught doing it: an item left under a stale key cannot be
                // found by RemoveItem, so it is never destroyed and never swept out, and
                // the distance cull condemns it twenty times a second for the rest of
                // the session. One log had a single item condemned 8,963 times and
                // actually removed 3, and 458 MB of the 563 MB file was that.
                //
                // It is also how a house comes to have no multi item while still being
                // held, which makes it claim every object on the map and makes the
                // client rebuild it as a plain shell it then never asks to replace.
                foreach (KeyValuePair<uint, Item> pair in Items)
                {
                    Item item = pair.Value;

                    item.Update();

                    // A multi is kept while any part of it is in range, not only its
                    // centre - the same rule HouseManager uses when deciding whether to
                    // let go of it. Testing the centre alone condemned a large house
                    // every frame and pardoned it every frame, because TryToRemove then
                    // refused: twenty passes a second, for as long as the player stood
                    // there. In one log 97.6% of all cull work was that loop.
                    // A multi answers to the house range, everything else to the view
                    // range. They are separate numbers because they are separate
                    // questions: a house is a design fetched by revision and safe to
                    // hold early, a loose object is only as good as the last update the
                    // server sent for it.
                    int houseRange = Settings.GlobalSettings.HouseLoadRange;

                    int keepWithin = item.IsMulti
                        ? houseRange + item.MultiDistanceBonus
                        : ClientViewRange;

                    if (do_delete && item.OnGround && item.Distance > keepWithin)
                    {
                        HouseDiagnostics.LogItemCulled(item);

                        if (item.IsMulti)
                        {
                            if (HouseManager.TryToRemove(item, houseRange))
                            {
                                RemoveItem(item);
                            }
                        }
                        else
                        {
                            RemoveItem(item);
                        }
                    }

                    if (item.IsDestroyed)
                    {
                        _toRemove.Add(pair.Key);
                    }
                }

                if (_toRemove.Count != 0)
                {
                    for (int i = 0; i < _toRemove.Count; i++)
                    {
                        // Still destroyed, and still the one that was condemned. See the
                        // matching note in the mobile sweep.
                        if (Items.TryGetValue(_toRemove[i], out Item gone) && gone.IsDestroyed)
                        {
                            Items.Remove(_toRemove[i]);

                            // Only now, with the entry out. See Item.ReturnToPool.
                            gone.ReturnToPool();
                        }
                    }

                    _toRemove.Clear();
                }

                _effectManager.Update();
                WorldTextManager.Update();
                WMapManager.RemoveUnupdatedWEntity();
            }
        }

        public static bool Contains(uint serial)
        {
            if (SerialHelper.IsItem(serial))
            {
                return Items.Contains(serial);
            }

            return SerialHelper.IsMobile(serial) && Mobiles.Contains(serial);
        }

        public static GameObject GetStaticOrMulti(ushort graphic, ushort x, ushort y, sbyte z)
        {
            if (Map is null)
            {
                Log.Error("World.GetStaticOrMulti called without a valid map.");
                return null;
            }

            if (Map.GetTile(x, y) is not Land land)
            {
                return null;
            }

            GameObject i = land.TNext;
            while (i is not null)
            {
                if (i is Static s)
                {
                    if (s.Graphic == graphic && s.X == x && s.Y == y && s.Z == z)
                        return s;
                }
                else if (i is Multi m)
                {
                    if (m.Graphic == graphic && m.X == x && m.Y == y && m.Z == z)
                        return m;
                }

                i = i.TNext;
            }

            return null;
        }

        public static Entity Get(uint serial)
        {
            Entity ent;

            if (SerialHelper.IsMobile(serial))
            {
                ent = Mobiles.Get(serial);

                if (ent == null)
                {
                    ent = Items.Get(serial);
                }
            }
            else
            {
                ent = Items.Get(serial);

                if (ent == null)
                {
                    ent = Mobiles.Get(serial);
                }
            }

            if (ent != null && ent.IsDestroyed)
            {
                ent = null;
            }

            return ent;
        }

        public static Item GetOrCreateItem(uint serial)
        {
            Item item = Items.Get(serial);

            if (item != null && item.IsDestroyed)
            {
                Items.Remove(serial);

                // The entry is out, so this one may be reused now rather than waiting
                // for the sweep to come round. See Item.ReturnToPool.
                item.ReturnToPool();

                item = null;
            }

            if (item == null /*|| item.IsDestroyed*/)
            {
                item = Item.Create(serial);
                Items.Add(item);
            }

            return item;
        }

        public static Mobile GetOrCreateMobile(uint serial)
        {
            Mobile mob = Mobiles.Get(serial);

            if (mob != null && mob.IsDestroyed)
            {
                Mobiles.Remove(serial);

                // The entry is out, so this one may be reused now rather than waiting
                // for the sweep to come round. See Item.ReturnToPool.
                mob.ReturnToPool();

                mob = null;
            }

            if (mob == null /*|| mob.IsDestroyed*/)
            {
                mob = Mobile.Create(serial);
                Mobiles.Add(mob);
            }

            return mob;
        }

        public static void RemoveItemFromContainer(uint serial)
        {
            Item it = Items.Get(serial);

            if (it != null)
            {
                RemoveItemFromContainer(it);
            }
        }

        public static void RemoveItemFromContainer(Item obj)
        {
            uint containerSerial = obj.Container;

            // if entity is running the "dying" animation we have to reset container too.
            // SerialHelper.IsValid(containerSerial) is not ideal in this case
            if (containerSerial != 0xFFFF_FFFF)
            {
                if (SerialHelper.IsMobile(containerSerial))
                {
                    UIManager.GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
                    UIManager.GetGump<ModernPaperdoll>(containerSerial)?.RequestUpdateContents();
                }
                else if (SerialHelper.IsItem(containerSerial))
                {
                    UIManager.GetGump<ContainerGump>(containerSerial)?.RequestUpdateContents();
                    #region GridContainer
                    UIManager.GetGump<GridContainer>(containerSerial)?.RequestUpdateContents();
                    #endregion

                    UIManager.GetGump<NearbyLootGump>(containerSerial)?.RequestUpdateContents();
                }

                Entity container = Get(containerSerial);

                if (container != null)
                {
                    container.Remove(obj);
                }

                obj.Container = 0xFFFF_FFFF;
            }

            obj.Next = null;
            obj.Previous = null;
            obj.RemoveFromTile();
        }

        public static bool RemoveItem(uint serial, bool forceRemove = false)
        {
            Item item = Items.Get(serial);

            if (item == null || item.IsDestroyed)
            {
                return false;
            }

            LinkedObject first = item.Items;
            RemoveItemFromContainer(item);

            while (first != null)
            {
                LinkedObject next = first.Next;

                RemoveItem(first as Item, forceRemove);

                first = next;
            }

            OPL.Remove(serial);
            item.Destroy();

            if (forceRemove)
            {
                Items.Remove(serial);

                item.ReturnToPool();
            }

            return true;
        }

        public static bool RemoveMobile(uint serial, bool forceRemove = false)
        {
            Mobile mobile = Mobiles.Get(serial);

            if (mobile == null || mobile.IsDestroyed)
            {
                return false;
            }

            LinkedObject first = mobile.Items;

            while (first != null)
            {
                LinkedObject next = first.Next;

                RemoveItem(first as Item, forceRemove);

                first = next;
            }

            OPL.Remove(serial);
            mobile.Destroy();

            if (forceRemove)
            {
                Mobiles.Remove(serial);

                mobile.ReturnToPool();
            }

            return true;
        }

        public static void SpawnEffect
        (
            GraphicEffectType type,
            uint source,
            uint target,
            ushort graphic,
            ushort hue,
            ushort srcX,
            ushort srcY,
            sbyte srcZ,
            ushort targetX,
            ushort targetY,
            sbyte targetZ,
            byte speed,
            int duration,
            bool fixedDir,
            bool doesExplode,
            bool hasparticles,
            GraphicEffectBlendMode blendmode
        )
        {
            _effectManager.CreateEffect
            (
                type,
                source,
                target,
                graphic,
                hue,
                srcX,
                srcY,
                srcZ,
                targetX,
                targetY,
                targetZ,
                speed,
                duration,
                fixedDir,
                doesExplode,
                hasparticles,
                blendmode
            );
        }

        public static uint FindNearest(ScanTypeObject scanType)
        {
            int distance = int.MaxValue;
            uint serial = 0;

            if (scanType == ScanTypeObject.Objects)
            {
                foreach (Item item in Items.Values)
                {
                    if (item.IsMulti || item.IsDestroyed || !item.OnGround)
                    {
                        continue;
                    }

                    if (item.Distance < distance)
                    {
                        distance = item.Distance;
                        serial = item.Serial;
                    }
                }
            }
            else
            {
                foreach (Mobile mobile in Mobiles.Values)
                {
                    if (mobile.IsDestroyed || mobile == Player)
                    {
                        continue;
                    }

                    switch (scanType)
                    {
                        case ScanTypeObject.Party:
                            if (!Party.Contains(mobile))
                            {
                                continue;
                            }
                            break;
                        case ScanTypeObject.Followers:
                            if (!(mobile.IsRenamable && mobile.NotorietyFlag != NotorietyFlag.Invulnerable && mobile.NotorietyFlag != NotorietyFlag.Enemy))
                            {
                                continue;
                            }
                            break;
                        case ScanTypeObject.Hostile:
                            if (mobile.NotorietyFlag == NotorietyFlag.Ally || mobile.NotorietyFlag == NotorietyFlag.Innocent || mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                            {
                                continue;
                            }
                            break;
                        case ScanTypeObject.Objects:
                            /* This was handled separately above */
                            continue;
                    }

                    if (mobile.Distance < distance)
                    {
                        distance = mobile.Distance;
                        serial = mobile.Serial;
                    }
                }
            }

            return serial;
        }

        public static uint FindNext(ScanTypeObject scanType, uint lastSerial, bool reverse)
        {
            bool found = false;

            if (scanType == ScanTypeObject.Objects)
            {
                var items = reverse ? Items.Values.Reverse() : Items.Values;
                foreach (Item item in items)
                {
                    if (item.IsMulti || item.IsDestroyed || !item.OnGround)
                    {
                        continue;
                    }

                    if (lastSerial == 0)
                    {
                        return item.Serial;
                    }
                    else if (item.Serial == lastSerial)
                    {
                        found = true;
                    }
                    else if (found)
                    {
                        return item.Serial;
                    }
                }
            }
            else
            {
                IEnumerable<Mobile> mobiles = reverse ? Mobiles.Values.Reverse() : Mobiles.Values;
                foreach (Mobile mobile in mobiles)
                {
                    if (mobile.IsDestroyed || mobile == Player)
                    {
                        continue;
                    }

                    switch (scanType)
                    {
                        case ScanTypeObject.Party:
                            if (!Party.Contains(mobile))
                            {
                                continue;
                            }
                            break;
                        case ScanTypeObject.Followers:
                            if (!(mobile.IsRenamable && mobile.NotorietyFlag != NotorietyFlag.Invulnerable && mobile.NotorietyFlag != NotorietyFlag.Enemy))
                            {
                                continue;
                            }
                            break;
                        case ScanTypeObject.Hostile:
                            if (mobile.NotorietyFlag == NotorietyFlag.Ally || mobile.NotorietyFlag == NotorietyFlag.Innocent || mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                            {
                                continue;
                            }
                            break;
                        case ScanTypeObject.Objects:
                            /* This was handled separately above */
                            continue;
                    }

                    if (lastSerial == 0)
                    {
                        return mobile.Serial;
                    }
                    else if (mobile.Serial == lastSerial)
                    {
                        found = true;
                    }
                    else if (found)
                    {
                        return mobile.Serial;
                    }
                }
            }

            if (lastSerial != 0)
            {
                /* If we get here, it means we didn't find anything but we started with a serial number. That means
                 * if we restart the search from the beginning it may find something again. */
                return FindNext(scanType, 0, reverse);
            }

            return 0;
        }


        public static void Clear()
        {
            foreach (Mobile mobile in Mobiles.Values)
            {
                RemoveMobile(mobile);
            }

            foreach (Item item in Items.Values)
            {
                RemoveItem(item);
            }

            UIManager.GetGump<BaseHealthBarGump>(Player.Serial)?.Dispose();

            ObjectToRemove = 0;
            LastObject = 0;
            Items.Clear();
            Mobiles.Clear();
            Player?.Destroy();
            Player = null;
            Map?.Destroy();
            Map = null;
            Light.Overall = Light.RealOverall = 0;
            Light.Personal = Light.RealPersonal = 0;
            ClientLockedFeatures.SetFlags(0);
            Party?.Clear();
            TargetManager.LastAttack = 0;
            MessageManager.PromptData = default;
            _effectManager.Clear();
            _toRemove.Clear();
            CorpseManager.Clear();
            OPL.Clear();
            WMapManager.Clear();
            HouseManager?.Clear();
            HouseContentsRecovery.Reset();

            Season = Season.Summer;
            OldSeason = Season.Summer;

            Journal.Clear();
            WorldTextManager.Clear();
            ActiveSpellIcons.Clear();

            SkillsRequested = false;
        }

        private static void InternalMapChangeClear(bool noplayer)
        {
            if (!noplayer)
            {
                Map.Destroy();
                Map = null;
                Player.Destroy();
                Player = null;
            }

            foreach (Item item in Items.Values)
            {
                if (noplayer && Player != null && !Player.IsDestroyed)
                {
                    if (item.RootContainer == Player)
                    {
                        continue;
                    }
                }

                if (item.OnGround && item.IsMulti)
                {
                    HouseManager.Remove(item.Serial);
                }

                _toRemove.Add(item);
            }

            foreach (uint serial in _toRemove)
            {
                RemoveItem(serial, true);
            }

            _toRemove.Clear();

            foreach (Mobile mob in Mobiles.Values)
            {
                if (noplayer && Player != null && !Player.IsDestroyed)
                {
                    if (mob == Player)
                    {
                        continue;
                    }
                }

                _toRemove.Add(mob);
            }

            foreach (uint serial in _toRemove)
            {
                RemoveMobile(serial, true);
            }

            _toRemove.Clear();
        }
    }
}
