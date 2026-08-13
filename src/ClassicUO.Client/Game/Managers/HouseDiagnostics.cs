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

            // No filter. Every cull is recorded, wherever the player is standing. The
            // old gate only logged culls while indoors, which hid the exact event under
            // investigation: a house's contents being dropped while the player was on
            // their way home. Disk is cheaper than another round of guessing.
            TryGetHouseContainingPlayer(out uint houseSerial);

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

        /// <summary>
        /// A house was thrown away, and why. The client only asks the server for a
        /// house it does not have, so a request implies a removal - but the removal
        /// itself was invisible, which left "something drops the house" unanswerable.
        ///
        /// The reasons are separate code paths and worth telling apart: out_of_range is
        /// the distance cull, no_multi_item is a revision packet arriving for a house
        /// whose multi is not in the world, and placement_preview is the serial-zero
        /// house used while positioning a deed - the one that claims every object in
        /// the world when it is left behind.
        /// </summary>
        /// <summary>
        /// How many items are actually standing inside a house's footprint right now.
        /// Components are the walls and floor, which are never the thing that goes
        /// missing; this counts the contents, so "the house is here and the stuff in it
        /// is not" becomes a number that can be compared before and after.
        /// </summary>
        public static int CountContents(uint houseSerial)
        {
            int n = 0;

            try
            {
                foreach (Item item in World.Items.Values)
                {
                    if (item != null && !item.IsDestroyed && item.OnGround && item.Serial != houseSerial
                        && World.HouseManager.EntityIntoHouse(houseSerial, item))
                    {
                        n++;
                    }
                }
            }
            catch
            {
                return -1;
            }

            return n;
        }

        /// <summary>The contents count for every house the client currently holds.</summary>
        public static void LogContentsCensus(string why)
        {
            if (!IsEnabled)
            {
                return;
            }

            try
            {
                foreach (House house in World.HouseManager.Houses)
                {
                    Write($"census\thouse=0x{house.Serial:X8}\treason={why}"
                          + $"\tcomponents={house.Components.Count}\tcontents={CountContents(house.Serial)}");
                }
            }
            catch
            {
            }
        }

        public static void LogHouseRemoved(uint serial, string reason, int components)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"removed\thouse=0x{serial:X8}\treason={reason}\tcomponents={components}");
        }

        /// <summary>
        /// An item left the world by any route, not only the distance cull. If a
        /// house's contents vanish without a single cull line, they were removed by
        /// something else, and this is what says so.
        /// </summary>
        public static void LogItemRemoved(Item item, string reason)
        {
            if (!IsEnabled || item == null)
            {
                return;
            }

            TryGetHouseContaining(item, out uint itemHouse);

            if (itemHouse == 0)
            {
                return;
            }

            Write($"item_gone\tserial=0x{item.Serial:X8}\tgraphic=0x{item.Graphic:X4}"
                  + $"\titem=({item.X},{item.Y},{item.Z})\treason={reason}"
                  + $"\titemhouse=0x{itemHouse:X8}");
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

            Write($"response\thouse=0x{serial:X8}\tcontents={CountContents(serial)}");
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

            // The player's position too, so a playerinside=False can be told apart from
            // a player who was genuinely outside at that instant.
            string at = World.Player == null
                ? "-"
                : $"({World.Player.X},{World.Player.Y},{World.Player.Z})";

            Write($"generate\thouse=0x{serial:X8}\tplayerinside={playerInside}\tplayer={at}"
                  + $"\tcomponents={components}\tcontents={CountContents(serial)}");
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
                        + "#   client   " + CUOEnviroment.Version + "  (" + CUOEnviroment.BuildTag + ")" + Environment.NewLine
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
