using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Asks the server for what is in a house the client emptied behind its own back.
    ///
    /// The client lets go of a house once it is out of range, and the moment it does,
    /// everything standing in that house stops being spared by the distance cull and is
    /// deleted - in the same pass, because the cull reaches the house's own item first.
    /// A capture caught it to the millisecond: the house dropped holding two hundred and
    /// thirty-eight things, and two milliseconds later two hundred and thirty-nine
    /// destroys, all of them the client's own cull.
    ///
    /// That would be harmless if the server sent the things again on the way back. It
    /// does not, and the same capture shows why it would not: the house design is asked
    /// for and answered every time, and nothing else follows, because from the server's
    /// side the client was told about those items once and never said otherwise. There
    /// is no message for "I have thrown away what you sent me".
    ///
    /// So the client has to ask, and packet 0x22 is the only thing that asks - the
    /// server's handler for it ends in SendEverything. What stepping off the foundation
    /// and back on does is trip the server's own region enter, which does the same
    /// thing; this simply does not require the player to know that trick.
    ///
    /// Asked for at one moment only: a house is taken back that this client previously
    /// dropped while it was holding something. Not on entering a house, which costs a
    /// full resend every time a door is walked through whether anything is wrong or not.
    /// Not on a timer. A house that was never dropped, or was dropped empty, is never
    /// asked about.
    /// </summary>
    internal static class HouseContentsRecovery
    {
        /// <summary>Long enough for an answer to arrive before another is asked for.</summary>
        private const long AskInterval = 3000;

        /// <summary>Houses dropped while holding something, and how much they held.</summary>
        private static readonly Dictionary<uint, int> _droppedHolding = new Dictionary<uint, int>();

        private static long _nextAsk;

        public static void Reset()
        {
            _droppedHolding.Clear();
            _nextAsk = 0;
        }

        /// <summary>
        /// A house is about to be let go of. Counted now, while its things are still
        /// here - a moment later they are gone and there is nothing left to count.
        /// </summary>
        public static void OnHouseLetGo(uint serial)
        {
            if (!World.InGame)
            {
                return;
            }

            Item multi = World.Items.Get(serial);

            if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
            {
                return;
            }

            int minX = multi.X + multi.MultiInfo.Value.X;
            int maxX = multi.X + multi.MultiInfo.Value.Width;
            int minY = multi.Y + multi.MultiInfo.Value.Y;
            int maxY = multi.Y + multi.MultiInfo.Value.Height;

            int held = 0;

            foreach (KeyValuePair<uint, Item> pair in World.Items)
            {
                Item item = pair.Value;

                if (item.IsMulti || item.IsDestroyed || !item.OnGround)
                {
                    continue;
                }

                if (item.X >= minX && item.X <= maxX && item.Y >= minY && item.Y <= maxY)
                {
                    held++;
                }
            }

            if (held > 0)
            {
                _droppedHolding[serial] = held;
            }
        }

        /// <summary>
        /// A house has been taken back. If this client emptied it on the way out, the
        /// server still believes those things were delivered, so say otherwise the only
        /// way there is.
        /// </summary>
        public static void OnHouseAcquired(uint serial)
        {
            if (!Settings.GlobalSettings.RecoverHouseContents || !World.InGame)
            {
                return;
            }

            if (!_droppedHolding.TryGetValue(serial, out int held))
            {
                return;
            }

            // Taken off whether the ask happens or not. A house that has come back is no
            // longer a house that was dropped, and leaving the record would have it asked
            // about again on the next approach.
            _droppedHolding.Remove(serial);

            if (Time.Ticks < _nextAsk)
            {
                return;
            }

            _nextAsk = Time.Ticks + AskInterval;

            HouseDiagnostics.Note($"recover house=0x{serial:X8} dropped_holding={held}");

            NetClient.Socket.Send_Resync();
        }
    }
}
