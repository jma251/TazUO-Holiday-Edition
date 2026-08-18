using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Asks the server for what stood in a house this client emptied while it was away.
    ///
    /// Measured on this shard: crossing out of the server's twenty-four tile box makes it
    /// send 0x1D for the room. Coming back, it streams only what it counts as newly in
    /// range - seventeen items where two hundred and twenty-two were deleted - because
    /// its delete never cleared its own record of what it had delivered. Stepping one
    /// tile off the foundation and back is a region crossing rather than a range
    /// re-entry, which is why that always works and a long trip does not.
    ///
    /// Packet 0x22 is the only thing that makes the server disregard that record. It is
    /// asked for at one moment: a house is taken back that this client let go of while
    /// it was holding something. Five events in a twenty-four hour capture. Never on
    /// entering a house, never on a timer, never for a house that was not dropped -
    /// which is every house you simply walk into.
    ///
    /// An earlier version also counted individual items culled near any house and polled
    /// once a second. That fired ten times in twenty-five minutes with the house full
    /// every time, because ordinary boundary culls near neighbouring houses armed it.
    /// It is not coming back.
    /// </summary>
    internal static class HouseContentsRecovery
    {
        /// <summary>Long enough for an answer to arrive before another is asked for.</summary>
        private const long AskInterval = 3000;

        /// <summary>How often the standing-inside count is taken. Only while the option is on.</summary>
        private const long PollInterval = 1000;

        /// <summary>
        /// How long a shortfall must persist before it counts as one.
        ///
        /// Standing on the threshold tile drops the count to a handful and it comes back
        /// on the next sample - roughly seventy times in a day's capture, every one of
        /// them recovering within a second. Three seconds is well clear of that and well
        /// inside the thirty-one a real one lasted.
        /// </summary>
        private const long ShortfallGrace = 3000;

        /// <summary>Below this, a difference is churn rather than a missing room.</summary>
        private const int ShortfallFloor = 10;

        /// <summary>The most this house has been seen holding, per house.</summary>
        private static readonly Dictionary<uint, int> _highWater = new Dictionary<uint, int>();

        private static uint _shortHouse;
        private static long _shortSince;
        private static long _nextPoll;

        /// <summary>
        /// Houses let go of while holding something, and how much. Counts only - nothing
        /// here keeps an object alive or remembers where one stood.
        /// </summary>
        private static readonly Dictionary<uint, int> _emptied = new Dictionary<uint, int>();

        private static long _nextAsk;

        public static void Reset()
        {
            _emptied.Clear();
            _highWater.Clear();
            _shortHouse = 0;
            _shortSince = 0;
            _nextPoll = 0;
            _nextAsk = 0;
        }

        /// <summary>
        /// Standing in a house that is holding less than it was.
        ///
        /// Caught on a capture: the house held 238, the player walked out and back, and
        /// it held 189 for thirty-one seconds while they walked around inside it. The
        /// house was never let go of, so nothing else here noticed. Stepping back out to
        /// the threshold and in again fixed it, which is the region crossing the server
        /// answers and a walk around the inside is not.
        ///
        /// Only runs while the option is on, because it counts the room once a second.
        /// </summary>
        public static void Update()
        {
            if (!Settings.GlobalSettings.RecoverHouseContents || !World.InGame || Time.Ticks < _nextPoll)
            {
                return;
            }

            _nextPoll = Time.Ticks + PollInterval;

            uint serial;

            if (!World.HouseManager.TryGetLoadedHouseAt(World.Player, out serial))
            {
                _shortHouse = 0;

                return;
            }

            int held = CountInside(serial);

            if (held < 0)
            {
                _shortHouse = 0;

                return;
            }

            int high;

            if (!_highWater.TryGetValue(serial, out high) || held > high)
            {
                _highWater[serial] = held;

                _shortHouse = 0;

                return;
            }

            if (high - held < ShortfallFloor)
            {
                _shortHouse = 0;

                return;
            }

            if (_shortHouse != serial)
            {
                _shortHouse = serial;
                _shortSince = Time.Ticks;

                return;
            }

            if (Time.Ticks - _shortSince < ShortfallGrace)
            {
                return;
            }

            _shortHouse = 0;

            // Whatever comes back is the new truth. Asked once per shortfall, so a room
            // that is genuinely emptier than it was does not get asked about forever.
            int missing = high - held;
            _highWater[serial] = held;

            Ask(serial, missing, "shortfall");
        }

        /// <summary>How many ground items this house is holding, or -1 if it cannot be counted.</summary>
        private static int CountInside(uint serial)
        {
            Item multi = World.Items.Get(serial);

            if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
            {
                return -1;
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

            return held;
        }

        /// <summary>
        /// A house is about to be let go of. Counted now, while its things are still here
        /// to count - a moment later the same sweep has taken them.
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
                _emptied[serial] = held;
            }
        }

        /// <summary>
        /// A house has been taken back. If this client emptied it on the way out, say so
        /// the only way there is.
        /// </summary>
        public static void OnHouseAcquired(uint serial)
        {
            if (!World.InGame)
            {
                return;
            }

            int emptied;

            if (!_emptied.TryGetValue(serial, out emptied))
            {
                return;
            }

            // Taken off whether the ask happens or not. A house that has come back is no
            // longer a house that was dropped, and leaving the record would have it asked
            // about again on every future approach.
            _emptied.Remove(serial);

            Ask(serial, emptied, "reacquired");
        }

        private static void Ask(uint serial, int missing, string why)
        {
            if (!Settings.GlobalSettings.RecoverHouseContents || Time.Ticks < _nextAsk)
            {
                return;
            }

            _nextAsk = Time.Ticks + AskInterval;

            HouseDiagnostics.Note($"recover house=0x{serial:X8} missing={missing} why={why}");

            NetClient.Socket.Send_Resync();
        }
    }
}
