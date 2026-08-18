using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Asks the server for what is standing in a house the client discarded behind its
    /// own back.
    ///
    /// The client drops what it cannot see, which is correct - holding it would mean
    /// drawing furniture that may have been carried off hours ago. But there is no
    /// message in the protocol for "I have thrown away what you sent me", so the server
    /// goes on believing those items were delivered and never mentions them again.
    ///
    /// Measured on a capture: standing inside the house holding 16 of 238 things, fifty
    /// seconds of walking around produced 25 arrivals and not one of them a new object.
    /// One 0x22 produced 242 arrivals, 222 of them new, and the room was whole again
    /// before the next log sample. Stepping off the foundation and back on works because
    /// it trips the server's own region enter, which resends unconditionally; 0x22 is the
    /// same instruction without needing to know the trick.
    ///
    /// Asked for only when this client is holding less than it handed back, and only
    /// while standing in the house it is short on. Not on entering any house, which would
    /// cost a full resend every time a door is walked through.
    /// </summary>
    internal static class HouseContentsRecovery
    {
        /// <summary>Long enough for an answer to arrive before another is asked for.</summary>
        private const long AskInterval = 3000;

        /// <summary>How often the standing-inside check runs at all.</summary>
        private const long PollInterval = 1000;

        /// <summary>
        /// Houses this client has discarded things from, and how many. Counts only -
        /// nothing here keeps an object alive or remembers where one stood.
        /// </summary>
        private static readonly Dictionary<uint, int> _owed = new Dictionary<uint, int>();

        private static long _nextAsk;
        private static long _nextPoll;

        public static void Reset()
        {
            _owed.Clear();
            _nextAsk = 0;
            _nextPoll = 0;
        }

        /// <summary>
        /// One item standing in a loaded house has just been dropped for distance. The
        /// house is still held, so it will not be re-acquired and nothing else would ever
        /// notice the shortfall.
        /// </summary>
        public static void OnContentsCulled(uint house)
        {
            if (house == 0)
            {
                return;
            }

            int held;
            _owed[house] = _owed.TryGetValue(house, out held) ? held + 1 : 1;
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
                int already;
                _owed[serial] = _owed.TryGetValue(serial, out already) ? already + held : held;
            }
        }

        /// <summary>
        /// A house has been taken back after being let go. If this client emptied it on
        /// the way out, say so the only way there is.
        /// </summary>
        public static void OnHouseAcquired(uint serial)
        {
            if (!World.InGame || !_owed.ContainsKey(serial))
            {
                return;
            }

            Ask(serial);
        }

        /// <summary>
        /// Standing inside a house this client is short on. Covers the case the acquire
        /// hook cannot see: the house never left range, so it was never re-acquired, but
        /// its contents were dropped at the view range all the same.
        /// </summary>
        public static void Update()
        {
            if (_owed.Count == 0 || !World.InGame || Time.Ticks < _nextPoll)
            {
                return;
            }

            _nextPoll = Time.Ticks + PollInterval;

            uint house;

            if (World.HouseManager.TryGetLoadedHouseAt(World.Player, out house) && _owed.ContainsKey(house))
            {
                Ask(house);
            }
        }

        private static void Ask(uint serial)
        {
            int owed;

            if (!_owed.TryGetValue(serial, out owed))
            {
                return;
            }

            if (!Settings.GlobalSettings.RecoverHouseContents)
            {
                _owed.Remove(serial);

                return;
            }

            if (Time.Ticks < _nextAsk)
            {
                return;
            }

            // Taken off whether the answer helps or not. Leaving it would have the same
            // house asked about on every poll for as long as the shortfall persisted.
            _owed.Remove(serial);
            _nextAsk = Time.Ticks + AskInterval;

            HouseDiagnostics.Note($"recover house=0x{serial:X8} discarded={owed}");

            NetClient.Socket.Send_Resync();
        }
    }
}
