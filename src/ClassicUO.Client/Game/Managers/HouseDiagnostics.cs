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

            Write(
                $"cull\tserial=0x{item.Serial:X8}\tgraphic=0x{item.Graphic:X4}"
                + $"\titem=({item.X},{item.Y},{item.Z})"
                + $"\tcentre=({World.RangeSize.X},{World.RangeSize.Y})"
                + $"\tdistance={item.Distance}\tviewrange={World.ClientViewRange}"
                + $"\tinhouse=0x{houseSerial:X8}"
                + $"\titeminhouse={World.HouseManager.EntityIntoHouse(houseSerial, item)}"
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
            houseSerial = 0;

            foreach (House house in World.HouseManager.Houses)
            {
                if (World.HouseManager.EntityIntoHouse(house.Serial, World.Player))
                {
                    houseSerial = house.Serial;

                    return true;
                }
            }

            return false;
        }

        private static void Write(string line)
        {
            try
            {
                string directory = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "houselog.txt");

                if (!File.Exists(path))
                {
                    File.AppendAllText(
                        path,
                        "# timestamp\tevent\tdetails" + Environment.NewLine
                    );
                }

                File.AppendAllText(
                    path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                        + "\t" + line + Environment.NewLine
                );
            }
            catch
            {
                // A diagnostic must never interrupt the game or crash the client.
            }
        }
    }
}
