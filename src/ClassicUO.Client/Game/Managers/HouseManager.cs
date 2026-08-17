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
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    public class HouseManager
    {
        private readonly Dictionary<uint, House> _houses = new Dictionary<uint, House>();

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public void Add(uint serial, House revision)
        {
            _houses[serial] = revision;

            RememberFootprint(serial);
        }

        /// <summary>
        /// Where every house seen this session stands, kept after the house itself has
        /// been let go of.
        ///
        /// A house is dropped as soon as it is out of range, and in the same pass of the
        /// cull everything standing in it loses what was sparing it - because the test
        /// asked whether the item was in a *loaded* house, and a moment earlier it
        /// stopped being one. Caught in a capture: the house dropped holding two hundred
        /// and twenty-two things, and one millisecond later every one of them destroyed.
        ///
        /// The server does not send them again on the way back. It has no way of knowing
        /// the client threw them away and there is no message for saying so, which is why
        /// the house comes back built and bare and only stepping off the foundation and
        /// back on - a region event on the server - refills it.
        ///
        /// Bounds do not move, so remembering them costs four numbers per house.
        /// </summary>
        private readonly Dictionary<uint, Rectangle> _footprints = new Dictionary<uint, Rectangle>();

        private void RememberFootprint(uint serial)
        {
            if (serial == 0 || _footprints.ContainsKey(serial))
            {
                return;
            }

            Item multi = World.Items.Get(serial);

            if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
            {
                return;
            }

            // Width and Height on MultiInfo hold maximum offsets, not sizes.
            int minX = multi.X + multi.MultiInfo.Value.X;
            int minY = multi.Y + multi.MultiInfo.Value.Y;
            int maxX = multi.X + multi.MultiInfo.Value.Width;
            int maxY = multi.Y + multi.MultiInfo.Value.Height;

            _footprints[serial] = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Is this standing in a house this session knows of, and near enough that
        /// letting go of it would be letting go of something the server may still count
        /// as delivered?
        ///
        /// Measured from the house's own edge rather than from its centre tile, so a
        /// large house is not treated as though it were a single point, and against the
        /// widest range the server ever gathers with - so by the time the client does let
        /// go, the server has stopped tracking it too and an ordinary approach sends it
        /// again with nothing having to ask.
        /// </summary>
        public bool IsInsideKnownHouse(GameObject obj)
        {
            if (obj == null || World.Player == null)
            {
                return false;
            }

            foreach (KeyValuePair<uint, Rectangle> pair in _footprints)
            {
                Rectangle r = pair.Value;

                if (obj.X < r.X || obj.X > r.X + r.Width || obj.Y < r.Y || obj.Y > r.Y + r.Height)
                {
                    continue;
                }

                int dx = World.Player.X < r.X ? r.X - World.Player.X
                    : World.Player.X > r.X + r.Width ? World.Player.X - (r.X + r.Width)
                    : 0;

                int dy = World.Player.Y < r.Y ? r.Y - World.Player.Y
                    : World.Player.Y > r.Y + r.Height ? World.Player.Y - (r.Y + r.Height)
                    : 0;

                if (Math.Max(dx, dy) <= Constants.MAX_VIEW_RANGE)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetHouse(uint serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public bool TryToRemove(uint serial, int distance)
        {
            if (!IsHouseInRange(serial, distance))
            {
                if (_houses.TryGetValue(serial, out House house))
                {
                    // The moment the contents stop being spared by the distance cull.
                    HouseDiagnostics.LogHouseLetGo(serial, "out_of_range", house.Components.Count);

                    house.ClearComponents();
                    _houses.Remove(serial);
                }


                return true;
            }

            return false;
        }

        public bool IsHouseInRange(uint serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = World.RangeSize.X;
                int currY = World.RangeSize.Y;

                //if (World.Player.IsMoving)
                //{
                //    Mobile.Step step = World.Player.Steps.Back();

                //    currX = step.X;
                //    currY = step.Y;
                //}
                //else
                //{
                //    currX = World.Player.X;
                //    currY = World.Player.Y;
                //}

                Item found = World.Items.Get(serial);

                if (found == null)
                {
                    return true;
                }

                distance += found.MultiDistanceBonus;

                return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
            }

            return false;
        }

        public bool EntityIntoHouse(uint house, GameObject obj)
        {
            if (obj != null && TryGetHouse(house, out _))
            {
                Item found = World.Items.Get(house);

                if (found == null || !found.MultiInfo.HasValue)
                {
                    return true;
                }

                int minX = found.X + found.MultiInfo.Value.X;
                int maxX = found.X + found.MultiInfo.Value.Width;
                int minY = found.Y + found.MultiInfo.Value.Y;
                int maxY = found.Y + found.MultiInfo.Value.Height;

                return obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY;
            }

            return false;
        }

        /// <summary>
        /// Is this object standing inside a house the client currently holds?
        ///
        /// Only a house whose multi item is still in the world counts. EntityIntoHouse
        /// answers "yes" for every object once the multi is gone, so a house left behind -
        /// the serial-zero placement preview above all - would otherwise claim the whole
        /// map and nothing anywhere would ever be let go of.
        /// </summary>
        public bool IsInsideLoadedHouse(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            foreach (KeyValuePair<uint, House> pair in _houses)
            {
                if (pair.Key == 0)
                {
                    continue;
                }

                Item multi = World.Items.Get(pair.Key);

                if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
                {
                    continue;
                }

                int minX = multi.X + multi.MultiInfo.Value.X;
                int maxX = multi.X + multi.MultiInfo.Value.Width;
                int minY = multi.Y + multi.MultiInfo.Value.Y;
                int maxY = multi.Y + multi.MultiInfo.Value.Height;

                if (obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY)
                {
                    return true;
                }
            }

            return false;
        }

        public void Remove(uint serial)
        {
            if (TryGetHouse(serial, out House house))
            {

                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        // The house being positioned from a deed is kept under serial zero. Left behind,
        // it is the phantom that answers "yes, that is inside me" for every object in
        // the world, because EntityIntoHouse says so for a house with no multi item.
        public void RemoveMultiTargetHouse()
        {
            if (_houses.TryGetValue(0, out House house))
            {

                house.ClearComponents();
                _houses.Remove(0);
            }
        }

        public bool Exists(uint serial)
        {
            return _houses.ContainsKey(serial);
        }

        public void Clear()
        {
            foreach (KeyValuePair<uint, House> house in _houses)
            {
                house.Value.ClearComponents();
            }

            _houses.Clear();
        }
    }
}