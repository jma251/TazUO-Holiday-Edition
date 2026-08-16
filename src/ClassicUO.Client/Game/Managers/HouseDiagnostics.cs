using System;
using System.Collections.Generic;
using System.Diagnostics;
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


        /// <summary>
        /// Is this position standing inside a house the client holds, and which one?
        ///
        /// The whole lifecycle log is filtered through this. An empty house is a
        /// question about the things that belong to a house, and everything else on the
        /// map is noise that has drowned this file before.
        /// </summary>
        public static bool InAnyLoadedHouse(int x, int y, out uint houseSerial)
        {
            houseSerial = 0;

            foreach (House house in World.HouseManager.Houses)
            {
                if (house.Serial == 0)
                {
                    continue;
                }

                Item multi = World.Items.Get(house.Serial);

                if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
                {
                    continue;
                }

                if (x >= multi.X + multi.MultiInfo.Value.X
                    && x <= multi.X + multi.MultiInfo.Value.Width
                    && y >= multi.Y + multi.MultiInfo.Value.Y
                    && y <= multi.Y + multi.MultiInfo.Value.Height)
                {
                    houseSerial = house.Serial;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The server has described an on-ground item standing inside a house. This is
        /// the top of the chain: if an empty house never produces these, nothing was
        /// ever sent and no amount of looking at the client will explain it.
        /// </summary>
        public static void LogHouseItemArrived(Item item, bool created)
        {
            if (!IsEnabled || item == null || !World.InGame)
            {
                return;
            }

            if (!InAnyLoadedHouse(item.X, item.Y, out uint houseSerial))
            {
                return;
            }

            Write(
                $"arrive\tserial=0x{item.Serial:X8}\tgraphic=0x{item.Graphic:X4}"
                + $"\tat=({item.X},{item.Y},{item.Z})\thouse=0x{houseSerial:X8}"
                + $"\tcreated={created}\tcontainer=0x{item.Container:X8}"
            );
        }

        /// <summary>
        /// The server has ordered an item removed. Logged separately from the destroy
        /// below so "the server took it away" can be told apart from "the client threw
        /// it away", which is the difference between a shard behaviour and a bug here.
        /// </summary>
        public static void LogHouseItemDeleteOrdered(Item item)
        {
            if (!IsEnabled || item == null || !World.InGame)
            {
                return;
            }

            if (!InAnyLoadedHouse(item.X, item.Y, out uint houseSerial))
            {
                return;
            }

            Write(
                $"srvdelete\tserial=0x{item.Serial:X8}\tgraphic=0x{item.Graphic:X4}"
                + $"\tat=({item.X},{item.Y},{item.Z})\thouse=0x{houseSerial:X8}"
            );
        }

        /// <summary>
        /// An item standing in a house is being destroyed, and the call stack that is
        /// doing it.
        ///
        /// Taken from inside Destroy rather than from the call sites, because the call
        /// sites are the thing in question - instrumenting the ones already suspected
        /// would only ever confirm a suspicion and would say nothing about the path
        /// nobody has thought of. Everything that destroys an item passes through here.
        ///
        /// The stack costs real time to walk, which is why it is taken only for an item
        /// inside a house, with the log switched on.
        /// </summary>
        public static void LogHouseItemDestroyed(Item item)
        {
            if (!IsEnabled || item == null || !World.InGame)
            {
                return;
            }

            if (!InAnyLoadedHouse(item.X, item.Y, out uint houseSerial))
            {
                return;
            }

            string via = "?";

            try
            {
                StackTrace trace = new StackTrace(2, false);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                for (int i = 0; i < trace.FrameCount && i < 7; i++)
                {
                    System.Reflection.MethodBase m = trace.GetFrame(i)?.GetMethod();

                    if (m == null)
                    {
                        continue;
                    }

                    if (sb.Length != 0)
                    {
                        sb.Append('<');
                    }

                    sb.Append(m.DeclaringType?.Name).Append('.').Append(m.Name);
                }

                via = sb.ToString();
            }
            catch
            {
            }

            Write(
                $"destroy\tserial=0x{item.Serial:X8}\tgraphic=0x{item.Graphic:X4}"
                + $"\tat=({item.X},{item.Y},{item.Z})\thouse=0x{houseSerial:X8}"
                + $"\tvia={via}"
            );
        }

        private static long _nextContentsLog;
        private static uint _lastContentsHouse;

        /// <summary>
        /// Once a second while the player is standing in a house, how much of that house
        /// the client is actually holding.
        ///
        /// This is the one number that splits an empty-looking house in half. The client
        /// draws what it holds, so if the count is above zero while the room looks bare,
        /// the things are in memory and something in the drawing is dropping them; if it
        /// is zero, they never arrived or something took them away. Every other question
        /// about an empty house is guesswork until this one is answered.
        ///
        /// Counted the same way the house itself is bounded - the multi's own footprint -
        /// so it means "things standing in this house", not "things near the player".
        /// </summary>
        public static void LogHouseContents()
        {
            if (!IsEnabled || !World.InGame)
            {
                return;
            }

            if (!TryGetHouseContainingPlayer(out uint houseSerial))
            {
                _lastContentsHouse = 0;

                return;
            }

            // Every second, and immediately on stepping into a different house, so
            // walking in on a bare room is recorded at the moment it is seen rather than
            // up to a second later.
            if (houseSerial == _lastContentsHouse && Time.Ticks < _nextContentsLog)
            {
                return;
            }

            _lastContentsHouse = houseSerial;
            _nextContentsLog = Time.Ticks + 1000;

            Item multi = World.Items.Get(houseSerial);

            if (multi == null || !multi.MultiInfo.HasValue)
            {
                Write($"contents\thouse=0x{houseSerial:X8}\tmulti=missing");

                return;
            }

            int minX = multi.X + multi.MultiInfo.Value.X;
            int maxX = multi.X + multi.MultiInfo.Value.Width;
            int minY = multi.Y + multi.MultiInfo.Value.Y;
            int maxY = multi.Y + multi.MultiInfo.Value.Height;

            int inside = 0;
            int onGround = 0;
            int drawable = 0;
            int drawn = 0;

            foreach (KeyValuePair<uint, Item> pair in World.Items)
            {
                Item item = pair.Value;

                if (item.IsMulti || item.IsDestroyed)
                {
                    continue;
                }

                if (item.X < minX || item.X > maxX || item.Y < minY || item.Y > maxY)
                {
                    continue;
                }

                inside++;

                if (item.OnGround)
                {
                    onGround++;

                    // Linked into the map cell it stands on. An item that is not is held
                    // by the world and reachable by nothing that draws.
                    if (item.TileChunk != null)
                    {
                        drawable++;
                    }

                    // Stamped by the draw loop when the item is actually queued for
                    // drawing. Two seconds of slack so a frame that skipped it for an
                    // ordinary reason does not read as never drawn.
                    if (item.LastDrawnTime != 0 && Time.Ticks - item.LastDrawnTime < 2000)
                    {
                        drawn++;
                    }
                }
            }

            House house = null;
            World.HouseManager.TryGetHouse(houseSerial, out house);

            Write(
                $"contents\thouse=0x{houseSerial:X8}"
                + $"\tat=({multi.X},{multi.Y},{multi.Z})"
                + $"\tbounds=({minX},{minY})-({maxX},{maxY})"
                + $"\tcomponents={(house == null ? -1 : house.Components.Count)}"
                + $"\tinside={inside}\tonground={onGround}\tintile={drawable}\tdrawn={drawn}"
                + $"\tplayer=({World.Player.X},{World.Player.Y},{World.Player.Z})"
            );
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
