using System;
using System.Globalization;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Temporary diagnostic for house contents failing to load. Silent and file-only -
    /// no chat, no journal, no on-screen text - and never allowed to throw.
    ///
    /// Answers two questions that cannot be settled by reading code:
    ///   1. What view range is the server actually sending, and are items inside a
    ///      house being culled for distance while the player is standing in it?
    ///   2. Is every house design request being answered?
    ///
    /// Intended to be removed once the cause is known.
    /// </summary>
    internal static class HouseDiagnostics
    {
        private static bool IsEnabled => Settings.GlobalSettings.LogHouseDiagnostics;

        private static int _lastLoggedViewRange = -1;
        private static bool _bannerWritten;

        /// <summary>
        /// The server sets the client's view range, unclamped. If it is smaller than the
        /// house, items at the far end are legitimately out of range while the player is
        /// inside, which would explain contents loading only partially.
        /// </summary>
        public static void LogViewRange(int viewRange)
        {
            if (!IsEnabled || viewRange == _lastLoggedViewRange)
            {
                return;
            }

            _lastLoggedViewRange = viewRange;

            Write($"viewrange\tvalue={viewRange}");
        }

        /// <summary>
        /// Called when an on-ground item is about to be dropped for distance. Only logs
        /// while the player is inside a house, otherwise this fires for every item left
        /// behind while walking and drowns the file.
        /// </summary>
        public static void LogItemCulled(Item item)
        {
            if (!IsEnabled || item == null || World.Player == null)
            {
                return;
            }

            if (!TryGetHouseContainingPlayer(out uint houseSerial))
            {
                return;
            }

            // Which house the item itself belongs to, which is not necessarily the
            // one the player is standing in - the old line could not tell those apart.
            TryGetHouseContaining(item, out uint itemHouse);

            Write(
                $"cull\tserial=0x{item.Serial:X8}\tgraphic=0x{item.Graphic:X4}"
                + $"\titem=({item.X},{item.Y},{item.Z})"
                + $"\tcentre=({World.RangeSize.X},{World.RangeSize.Y})"
                + $"\tdistance={item.Distance}\tviewrange={World.ClientViewRange}"
                + $"\tinhouse=0x{houseSerial:X8}"
                + $"\titeminhouse={World.HouseManager.EntityIntoHouse(houseSerial, item)}"
                + $"\titemhouse=0x{itemHouse:X8}"
                + $"\tismulti={item.IsMulti}\tbonus={item.MultiDistanceBonus}"
            );
        }

        public static void LogHouseRequest(uint serial)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"request\thouse=0x{serial:X8}");
        }

        public static void LogHouseResponse(uint serial)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"response\thouse=0x{serial:X8}");
        }

        private static bool TryGetHouseContainingPlayer(out uint houseSerial)
        {
            return TryGetHouseContaining(World.Player, out houseSerial);
        }

        /// <summary>
        /// The first house whose bounds contain the object. Beware: HouseManager answers
        /// "yes" for a house whose multi item has gone, so a house left behind claims
        /// everything in the world - which is how a serial-zero house came to own
        /// ninety-seven items in one second. Those are reported as phantom.
        /// </summary>
        private static bool TryGetHouseContaining(GameObject obj, out uint houseSerial)
        {
            houseSerial = 0;

            if (obj == null)
            {
                return false;
            }

            foreach (House house in World.HouseManager.Houses)
            {
                if (!World.HouseManager.EntityIntoHouse(house.Serial, obj))
                {
                    continue;
                }

                houseSerial = house.Serial;

                if (World.Items.Get(house.Serial) == null)
                {
                    LogPhantom(house.Serial);
                }

                return true;
            }

            return false;
        }

        private static uint _lastPhantom = uint.MaxValue;

        private static void LogPhantom(uint serial)
        {
            if (serial == _lastPhantom)
            {
                return;
            }

            _lastPhantom = serial;

            Write($"phantom\thouse=0x{serial:X8}\tnote=house has no multi item, so it claims every object");
        }

        /// <summary>
        /// The draw ceiling, which is what decides whether the inside of a house is
        /// visible. It is cached on the player's position, so a house that finishes
        /// building while the player stands still can leave it stale - which is what
        /// stepping outside and back in resets.
        /// </summary>
        public static void LogDrawZ(bool forced, bool chunkMissing, int maxZ, int maxGroundZ)
        {
            if (!IsEnabled || World.Player == null)
            {
                return;
            }

            if (!forced && maxZ == _lastMaxZ && maxGroundZ == _lastMaxGroundZ)
            {
                return;
            }

            _lastMaxZ = maxZ;
            _lastMaxGroundZ = maxGroundZ;

            Write(
                $"drawz\tmaxz={maxZ}\tmaxgroundz={maxGroundZ}\tforced={forced}"
                + $"\tchunkmissing={chunkMissing}\tplayerz={World.Player.Z}"
            );
        }

        private static int _lastMaxZ = int.MinValue;
        private static int _lastMaxGroundZ = int.MinValue;

        /// <summary>
        /// A house finished building, and whether the client thinks the player is inside
        /// it - which is the condition for recomputing the draw ceiling.
        /// </summary>
        public static void LogHouseGenerated(uint serial, bool playerInside, int components)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"generate\thouse=0x{serial:X8}\tplayerinside={playerInside}\tcomponents={components}");
        }

        private static void Write(string line)
        {
            try
            {
                string directory = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "houselog.txt");

                // Who and where. Without these the log could not say which character it
                // belonged to, and the facet had to be inferred from coordinates.
                string who = World.Player == null
                    ? "char=-\tmap=-"
                    : $"char={World.Player.Name}\tmap={World.MapIndex}";

                if (!File.Exists(path))
                {
                    File.AppendAllText(
                        path,
                        "# timestamp\tevent\tchar\tmap\tdetails" + Environment.NewLine
                    );
                }

                if (!_bannerWritten)
                {
                    _bannerWritten = true;

                    File.AppendAllText(
                        path,
                        "#" + Environment.NewLine
                        + "# ==== session " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " ====" + Environment.NewLine
                        + "#   client   " + CUOEnviroment.Version + Environment.NewLine
                        + "#" + Environment.NewLine
                    );
                }

                int tab = line.IndexOf('\t');
                string ev = tab < 0 ? line : line.Substring(0, tab);
                string rest = tab < 0 ? "" : line.Substring(tab);

                File.AppendAllText(
                    path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                        + "\t" + ev + "\t" + who + rest + Environment.NewLine
                );
            }
            catch
            {
                // A diagnostic must never interrupt the game or crash the client.
            }
        }
    }
}
