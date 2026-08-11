using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Decides what music belongs where, for the times the server says it has none.
    ///
    /// Modern server data only defines music for cities and dungeons - the outdoor
    /// regions the 1998 server carried were never reimplemented - so walking out of
    /// town produces a stop packet and then nothing at all. This fills that in from
    /// the 1998 region data, which is authored, not guessed.
    ///
    /// The server always wins. This is only ever consulted after the server has said
    /// the current area has no music of its own.
    /// </summary>
    internal static class MusicMapManager
    {
        private const int BLOCK_SIZE = 8;

        private sealed class Area
        {
            public int Map;
            public int X, Y, Width, Height;
            public int ZMin, ZMax;
            public int[] Tracks;
            public string Name;

            public bool Contains(int map, int x, int y, int z)
            {
                return map == Map
                       && x >= X && x < X + Width
                       && y >= Y && y < Y + Height
                       && z >= ZMin && z <= ZMax;
            }
        }

        // Ordered largest first, exactly as the 1998 server painted them, so the last
        // match found while scanning is the smallest area covering the spot.
        private static Area[] _areas = new Area[0];
        private static bool _loaded;

        private static readonly Random _random = new Random();

        public static int AreaCount => _areas.Length;

        public static void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            try
            {
                string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "MusicMap.txt");

                if (!File.Exists(path))
                {
                    Log.Warn($"No music map at '{path}'. Region music will be left to the server.");

                    return;
                }

                List<Area> list = new List<Area>();

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();

                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    string[] p = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (p.Length < 8)
                    {
                        continue;
                    }

                    Area area = new Area();

                    if (!TryInt(p[0], out area.Map) || !TryInt(p[1], out area.X) || !TryInt(p[2], out area.Y)
                        || !TryInt(p[3], out area.Width) || !TryInt(p[4], out area.Height)
                        || !TryInt(p[5], out area.ZMin) || !TryInt(p[6], out area.ZMax))
                    {
                        continue;
                    }

                    area.Tracks = ParseTracks(p[7]);

                    if (area.Tracks.Length == 0)
                    {
                        continue;
                    }

                    area.Name = p.Length > 8 ? p[8] : string.Empty;

                    list.Add(area);
                }

                // Sorted rather than trusted, so a hand-added area still obeys the
                // smallest-wins rule wherever it is placed in the file.
                list.Sort((a, b) => ((long)b.Width * b.Height).CompareTo((long)a.Width * a.Height));

                _areas = list.ToArray();

                Log.Trace($"Music map loaded: {_areas.Length} areas.");
            }
            catch (Exception ex)
            {
                Log.Error($"Could not read the music map: {ex}");

                _areas = new Area[0];
            }
        }

        /// <summary>
        /// The music index for a position, or false when nothing covers it - which
        /// means silence, the same answer the 1998 server gave.
        /// </summary>
        public static bool TryGetTrack(int map, int x, int y, int z, out int track, out string areaName)
        {
            track = -1;
            areaName = null;

            Area found = null;

            for (int i = 0; i < _areas.Length; i++)
            {
                if (_areas[i].Contains(map, x, y, z))
                {
                    found = _areas[i];
                }
            }

            if (found == null)
            {
                return false;
            }

            // A single-entry list is the 1998 data as authored. Longer lists are a
            // local addition, and picking from them at random is how every era of the
            // game did it.
            track = found.Tracks.Length == 1 ? found.Tracks[0] : found.Tracks[_random.Next(found.Tracks.Length)];
            areaName = found.Name;

            return true;
        }

        /// <summary>
        /// The 8x8 block a position falls in. Music was per block, not per tile, so
        /// this is what a "did the area change" test compares.
        /// </summary>
        public static int BlockOf(int x, int y) => (x / BLOCK_SIZE) << 16 | (y / BLOCK_SIZE);

        private static bool TryInt(string s, out int value) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        private static int[] ParseTracks(string field)
        {
            string[] parts = field.Split(',');
            List<int> tracks = new List<int>(parts.Length);

            for (int i = 0; i < parts.Length; i++)
            {
                if (TryInt(parts[i].Trim(), out int t) && t >= 0 && t < Constants.MAX_MUSIC_DATA_INDEX_COUNT)
                {
                    tracks.Add(t);
                }
            }

            return tracks.ToArray();
        }
    }
}
