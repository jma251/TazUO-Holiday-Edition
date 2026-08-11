using System;
using System.Globalization;
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.IO.Audio;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Records what the music system is actually doing, to answer a question that
    /// cannot be answered by reading code: does the server ever tell the client the
    /// player has left a region?
    ///
    /// The previous logger only wrote a line when the playing track changed, which
    /// hid exactly that - walking out of a city produces no change, so it produced
    /// no evidence either way. This writes a line for every event, including the
    /// ones that look uninteresting, and deliberately does not deduplicate.
    ///
    /// Silent and file-only, and never allowed to throw.
    /// </summary>
    internal static class MusicDiagnostics
    {
        private const int ZONE_DISTANCE = 20;

        // The end-of-track hooks are raised from FNA's buffer-needed callback, which
        // is not guaranteed to be the main thread, so writes are serialised.
        private static readonly object _writeLock = new object();

        private static bool IsEnabled => Settings.GlobalSettings.LogMusicIndices;

        // Where and when the last server music packet arrived. dist and secs on every
        // line are measured from here, so a line says how far the player has walked
        // and how long it has been since the server last said anything.
        private static int _anchorX = -1;
        private static int _anchorY = -1;
        private static int _anchorMap = -1;
        private static DateTime _anchorTime = DateTime.Now;
        private static bool _zoneLoggedForAnchor;
        private static int _lastMapIndex = int.MinValue;

        /// <summary>
        /// A music packet arrived from the server. Logged every time, even when it
        /// names the track already playing - a repeated packet is itself data.
        /// Resets the distance and time anchor.
        /// </summary>
        public static void ServerPacket(int index)
        {
            Write("SERVER", index);

            if (World.Player != null)
            {
                _anchorX = World.Player.X;
                _anchorY = World.Player.Y;
                _anchorMap = World.MapIndex;
            }

            _anchorTime = DateTime.Now;
            _zoneLoggedForAnchor = false;
        }

        /// <summary>The requested track is the one already playing, so nothing happens.</summary>
        public static void SameTrack(int index) => Write("SAME", index);

        public static void Started(int index, bool loop, string flags) => Write("START", index, loop, flags);

        public static void Stopped(int index) => Write("STOP", index);

        /// <summary>A non-looping track ran to its end on its own.</summary>
        public static void Ended(UOMusic music) => Write("END", music?.Index ?? -1, false);

        /// <summary>A looping track hit the end and restarted from the beginning.</summary>
        public static void Looped(UOMusic music) => Write("LOOP", music?.Index ?? -1, true);

        public static void WarMode(bool on) => Write(on ? "WAR_ON" : "WAR_OFF");

        /// <summary>
        /// Called from the play/update path rather than hooked into World, so a map
        /// change is noticed wherever it happens.
        /// </summary>
        public static void CheckMapChange()
        {
            if (!IsEnabled || World.Player == null)
            {
                return;
            }

            int map = World.MapIndex;

            if (map == _lastMapIndex)
            {
                return;
            }

            _lastMapIndex = map;

            Write("MAP");
        }

        /// <summary>
        /// Logged once when the player first gets more than ZONE_DISTANCE tiles from
        /// where the last server packet arrived. One line per anchor, so the gap
        /// between a ZONE line and the next SERVER line is the answer to "how far do
        /// I get before anything else fires".
        /// </summary>
        public static void CheckZone()
        {
            if (!IsEnabled || _zoneLoggedForAnchor || World.Player == null || _anchorX < 0)
            {
                return;
            }

            if (World.MapIndex != _anchorMap || Distance() <= ZONE_DISTANCE)
            {
                return;
            }

            _zoneLoggedForAnchor = true;

            Write("ZONE");
        }

        // UO measures distance as the larger of the two axes, not as the diagonal.
        private static int Distance()
        {
            if (World.Player == null || _anchorX < 0)
            {
                return 0;
            }

            int dx = Math.Abs(World.Player.X - _anchorX);
            int dy = Math.Abs(World.Player.Y - _anchorY);

            return dx > dy ? dx : dy;
        }

        private static void Write(string ev, int index = -1, bool? loop = null, string flags = "")
        {
            if (!IsEnabled)
            {
                return;
            }

            try
            {
                // The end-of-track hooks are raised from FNA's buffer-needed callback,
                // which is not guaranteed to be the main thread.
                lock (_writeLock)
                {
                    string directory = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
                    Directory.CreateDirectory(directory);

                    string path = Path.Combine(directory, "musiclog.txt");

                    if (!File.Exists(path))
                    {
                        File.AppendAllText(
                            path,
                            "# timestamp\tevent\tidx\ttrack\tloop\tmap\tx\ty\tz\tdist\tsecs\tflags" + Environment.NewLine
                        );
                    }

                    string idx = index < 0 ? "" : index.ToString(CultureInfo.InvariantCulture);
                    string track = "";

                    if (index >= 0 && SoundsLoader.Instance.TryGetMusicData(index, out string name, out bool doesLoop))
                    {
                        track = name;

                        loop = loop ?? doesLoop;
                    }

                    string x = "-", y = "-", z = "-", map = "-";

                    if (World.Player != null)
                    {
                        x = World.Player.X.ToString(CultureInfo.InvariantCulture);
                        y = World.Player.Y.ToString(CultureInfo.InvariantCulture);
                        z = World.Player.Z.ToString(CultureInfo.InvariantCulture);
                        map = World.MapIndex.ToString(CultureInfo.InvariantCulture);
                    }

                    double secs = (DateTime.Now - _anchorTime).TotalSeconds;

                    File.AppendAllText(
                        path,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10:0.0}\t{11}{12}",
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                            ev,
                            idx,
                            track,
                            loop.HasValue ? (loop.Value ? "loop" : "once") : "",
                            map,
                            x,
                            y,
                            z,
                            Distance(),
                            secs,
                            flags ?? "",
                            Environment.NewLine
                        )
                    );
                }
            }
            catch
            {
                // A diagnostic must never interrupt the game or crash the client.
            }
        }
    }
}
