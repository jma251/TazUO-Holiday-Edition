using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Asks the server for what stood in a house this client is no longer holding.
    ///
    /// Packet 0x22 is the only thing that makes the server disregard its record of what
    /// it has already delivered. It is asked for at two moments: a house is taken back
    /// that this client let go of while it was holding something, and a house being
    /// stood in is holding fewer things than it has been seen holding.
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

        /// <summary>Below this, a difference is churn rather than a missing room.</summary>
        private const int ShortfallFloor = 10;

        /// <summary>The most this house has been seen holding, per house.</summary>
        private static readonly Dictionary<uint, int> _highWater = new Dictionary<uint, int>();

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
            _nextPoll = 0;
            _nextAsk = 0;
        }

        /// <summary>
        /// Standing in a house that is holding less than it was.
        ///
        /// Caught on a capture: the house held 238, the player walked out and back, and
        /// it held 189 for thirty-one seconds while they walked around inside it. The
        /// house was never let go of, so nothing else here noticed.
        ///
        /// Judged only from a tile that is not on the house's perimeter. Standing on the
        /// perimeter the count dips to a handful and recovers on the next sample - about
        /// seventy times in a day's capture, none of them real. Replaying both captures,
        /// that test on its own removes every one of them, so a shortfall seen from
        /// inside is acted on at once rather than waited out.
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
                return;
            }

            int minX, minY, maxX, maxY;

            if (!TryGetBounds(serial, out minX, out minY, out maxX, out maxY))
            {
                return;
            }

            if (World.Player.X <= minX || World.Player.X >= maxX || World.Player.Y <= minY || World.Player.Y >= maxY)
            {
                return;
            }

            int held = CountInside(minX, minY, maxX, maxY);

            int high;

            if (!_highWater.TryGetValue(serial, out high) || held > high)
            {
                _highWater[serial] = held;

                return;
            }

            if (high - held < ShortfallFloor)
            {
                return;
            }

            // Whatever comes back is the new truth. Asked once per shortfall, so a room
            // that is genuinely emptier than it was does not get asked about forever.
            int missing = high - held;
            _highWater[serial] = held;

            Ask(serial, missing, "shortfall");
        }

        /// <summary>Where this house's multi says its floor is, or false if it cannot say.</summary>
        private static bool TryGetBounds(uint serial, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = minY = maxX = maxY = 0;

            Item multi = World.Items.Get(serial);

            if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
            {
                return false;
            }

            minX = multi.X + multi.MultiInfo.Value.X;
            maxX = multi.X + multi.MultiInfo.Value.Width;
            minY = multi.Y + multi.MultiInfo.Value.Y;
            maxY = multi.Y + multi.MultiInfo.Value.Height;

            return true;
        }

        /// <summary>How many ground items are standing within these bounds.</summary>
        private static int CountInside(int minX, int minY, int maxX, int maxY)
        {
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

            int minX, minY, maxX, maxY;

            if (!TryGetBounds(serial, out minX, out minY, out maxX, out maxY))
            {
                return;
            }

            int held = CountInside(minX, minY, maxX, maxY);

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
