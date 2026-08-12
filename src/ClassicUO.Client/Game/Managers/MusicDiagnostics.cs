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
        private static int _lastZoneBand;
        private static int _lastMapIndex = int.MinValue;
        private static bool _bannerWritten;

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
            _lastZoneBand = 0;
        }

        /// <summary>The requested track is the one already playing, so nothing happens.</summary>
        public static void SameTrack(int index) => Write("SAME", index);

        /// <summary>
        /// A track started. path is what the index actually resolved to, which is the
        /// only way to tell from the log which era supplied it.
        /// </summary>
        public static void Started(int index, bool loop, string flags, string path = null)
        {
            string where = string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileName(path);
            string dir = string.IsNullOrEmpty(path) ? "" : Directory.GetParent(path)?.Name ?? "";

            Write("START", index, loop, Join(flags, string.IsNullOrEmpty(where) ? "" : dir + "/" + where));
        }

        /// <summary>
        /// The map is about to decide what belongs where the player is standing.
        /// Logged before the answer so an unexpected silence has a visible cause.
        /// </summary>
        public static void MapLook(int mode, bool afterEnd) =>
            Write("MAP_LOOK", -1, null, (afterEnd ? "after_track_ended" : "silence") + " mode=" + mode.ToString(CultureInfo.InvariantCulture));

        private static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b ?? "";
            }

            return string.IsNullOrEmpty(b) ? a : a + " " + b;
        }

        public static void Stopped(int index) => Write("STOP", index);

        /// <summary>A non-looping track ran to its end on its own.</summary>
        public static void Ended(UOMusic music) => Write("END", music?.Index ?? -1, false);

        /// <summary>A looping track hit the end and restarted from the beginning.</summary>
        public static void Looped(UOMusic music) => Write("LOOP", music?.Index ?? -1, true);

        public static void WarMode(bool on) => Write(on ? "WAR_ON" : "WAR_OFF");

        /// <summary>The music map covered this spot and supplied a track.</summary>
        public static void MapHit(int track, string areaName) => Write("MAP_HIT", track, null, areaName ?? "");

        /// <summary>Nothing in the music map covers this spot, so the answer is silence.</summary>
        public static void MapMiss() => Write("MAP_MISS");

        /// <summary>The raw bytes of a music packet, so the wire can be read directly.</summary>
        public static void RawMusicPacket(ushort index) =>
            Write("RAW6D", -1, null, $"6D {(index >> 8) & 0xFF:X2} {index & 0xFF:X2}");

        /// <summary>
        /// The season packet, whose second byte is a play-sound flag and not a music
        /// index. Logged raw so the wire can be read directly, since it was being
        /// mistaken for a track number.
        /// </summary>
        public static void SeasonPacket(int season, int playSound) =>
            Write("SEASON", -1, null, $"BC {season:X2} {playSound:X2}");

        /// <summary>The server's stop packet arrived and was thrown away on request.</summary>
        public static void StopIgnored() => Write("STOP_IGNORED");

        /// <summary>
        /// The map had an answer and did not use it, because the mode says to let the
        /// current track finish first. Without this the log showed a block change and
        /// then nothing, which reads the same as the map being broken.
        /// </summary>
        public static void MapWait(int track, string why) => Write("MAP_WAIT", track, null, why);

        /// <summary>
        /// The map deliberately chose silence, and why. Distinct from MAP_MISS, which
        /// only says nothing covers the spot.
        /// </summary>
        public static void MapSilent(string why) => Write("MAP_SILENT", -1, null, why);

        /// <summary>The music era changed, with the folder it now resolves against.</summary>
        public static void EraChanged(string era) =>
            Write("ERA", -1, null, string.IsNullOrEmpty(era) ? "(default - stock Music/Digital)" : era);

        /// <summary>
        /// Written once when the log is first opened in a session. Without it, an
        /// appended file makes an old run look like the current one - which is exactly
        /// how tracks fixed several builds ago kept appearing to still be playing.
        /// </summary>
        private static void WriteSessionBanner(string path)
        {
            Settings s = Settings.GlobalSettings;

            string era = string.IsNullOrEmpty(s.MusicEra) ? "(default)" : s.MusicEra;
            string mode = s.MusicMapMode == 0 ? "off"
                        : s.MusicMapMode == 1 ? "authentic"
                        : s.MusicMapMode == 2 ? "seamless"
                        : s.MusicMapMode == 3 ? "continuous"
                        : s.MusicMapMode.ToString(CultureInfo.InvariantCulture);

            File.AppendAllText(
                path,
                "#" + Environment.NewLine +
                "# ==== session " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " ====" + Environment.NewLine +
                "#   client            " + CUOEnviroment.Version + Environment.NewLine +
                "#   music era         " + era + Environment.NewLine +
                "#   region music      " + mode + Environment.NewLine +
                "#   ignore stop       " + (s.IgnoreServerStopMusic ? "yes" : "no") + Environment.NewLine +
                "#   music map areas   " + MusicMapManager.AreaCount.ToString(CultureInfo.InvariantCulture) + Environment.NewLine +
                "#" + Environment.NewLine
            );
        }

        /// <summary>
        /// A stop packet arrived and the track was left playing anyway - either because
        /// the option says the server may not cut it, or because the map wanted that
        /// track regardless. Either way the map owns it now.
        /// </summary>
        public static void Kept(int track, string why) => Write("MAP_KEEP", track, null, why);

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
        /// Logged every ZONE_DISTANCE tiles walked away from where the last server
        /// packet arrived - 20, 40, 60 and so on - so the log shows how far the player
        /// actually gets before anything else fires. The count resets when the next
        /// SERVER event sets a new anchor.
        ///
        /// Only a new furthest band is logged, so pacing back and forth over a
        /// boundary does not fill the file.
        /// </summary>
        public static void CheckZone()
        {
            if (!IsEnabled || World.Player == null || _anchorX < 0)
            {
                return;
            }

            if (World.MapIndex != _anchorMap)
            {
                return;
            }

            int band = Distance() / ZONE_DISTANCE;

            if (band <= _lastZoneBand)
            {
                return;
            }

            _lastZoneBand = band;

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
                            "# timestamp\tevent\tidx\ttrack\tloop\tmap\tx\ty\tz\tdist\tsecs\tvol\tflags" + Environment.NewLine
                        );
                    }

                    if (!_bannerWritten)
                    {
                        _bannerWritten = true;

                        WriteSessionBanner(path);
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

                    // So a silent log line can be told apart from a silent client.
                    Profile profile = ProfileManager.CurrentProfile;

                    string vol = profile == null ? "-"
                               : !profile.EnableMusic ? "off"
                               : profile.MusicVolume.ToString(CultureInfo.InvariantCulture);

                    File.AppendAllText(
                        path,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10:0.0}\t{11}\t{12}{13}",
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
                            vol,
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
