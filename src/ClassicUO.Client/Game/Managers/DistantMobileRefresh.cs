using ClassicUO.Configuration;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Nudges the view range by a single tile on a timer, to make the server describe
    /// again what it has stopped talking about.
    ///
    /// A RunUO-derived server sends a mobile out to whatever range the client asks for,
    /// but announces its movement only within a fixed range of its own. Past that the
    /// client holds a picture of where the mobile used to be, and there is nothing in
    /// the protocol to ask about it - no packet the client can send requests a single
    /// object's position. What does bring the answer back is the server re-evaluating
    /// what is in range for this client, and a change of view range does exactly that.
    /// Observed on a live shard: one click of the slider, the player standing still, and
    /// a mobile frozen at thirty-five tiles snapped to where it really was.
    ///
    /// One tile is enough, and one packet is enough - the range is left alternating
    /// between the setting and one below it, rather than being sent down and back up,
    /// which would ask for two rebuilds where one will do.
    ///
    /// Anchored on the player's setting rather than on what the server last granted, so
    /// a shard that answers with something else cannot walk the range downwards a tile
    /// per tick.
    ///
    /// This is expensive and is meant to be measured rather than left running. The
    /// server answers a range change by sending everything in range again, not merely
    /// the ring that changed, so each tick costs a full resend - the same traffic as a
    /// resync. Off by default on purpose.
    /// </summary>
    internal static class DistantMobileRefresh
    {
        /// <summary>The intervals the setting offers, in milliseconds. Index 0 is off.</summary>
        public static readonly int[] Intervals = { 0, 500, 1000, 2000, 5000 };

        private static long _next;
        private static bool _low;

        public static void Reset()
        {
            _next = 0;
            _low = false;
        }

        public static void Update()
        {
            int interval = IntervalMs();

            if (interval <= 0 || !World.InGame)
            {
                return;
            }

            if (Time.Ticks < _next)
            {
                return;
            }

            _next = Time.Ticks + interval;

            int anchor = Settings.GlobalSettings.ClientViewRange;

            if (anchor < Constants.MIN_VIEW_RANGE)
            {
                anchor = Constants.MIN_VIEW_RANGE;
            }
            else if (anchor > Constants.MAX_VIEW_RANGE)
            {
                anchor = Constants.MAX_VIEW_RANGE;
            }

            // Below the anchor rather than above it: asking for more than the player set
            // would be asking the server for something they did not.
            if (anchor <= Constants.MIN_VIEW_RANGE)
            {
                return;
            }

            _low = !_low;

            NetClient.Socket.Send_ClientViewRange((byte)(_low ? anchor - 1 : anchor));
        }

        private static int IntervalMs()
        {
            int index = Settings.GlobalSettings.DistantMobileRefresh;

            if (index <= 0 || index >= Intervals.Length)
            {
                return 0;
            }

            return Intervals[index];
        }
    }
}
