using System;
using System.Globalization;
using System.IO;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Diagnostic for house contents failing to load. Silent and file-only - no chat,
    /// no journal, no on-screen text - and never allowed to throw.
    ///
    /// It records everything, deliberately. Every packet in both directions with its
    /// full bytes, every object the server puts in the world, where the player is and
    /// what they are doing, and a snapshot of every house four times a second. The game
    /// runs on a couple of kilobytes a second, so there is nothing here worth saving
    /// disk over, and every filter added so far has hidden the one line that mattered.
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

            // No filter on "was it in a house". An item leaving the world matters even
            // when no house claims it, because a house whose multi has gone claims
            // nothing - so the old gate hid exactly the removals worth seeing.
            TryGetHouseContaining(item, out uint itemHouse);

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

        // ------------------------------------------------------------------
        // The wire. Every packet, both directions, whole.
        // ------------------------------------------------------------------

        /// <summary>
        /// One line per packet: which way it went, its ID and name, its length, a
        /// short decode of the fields that matter, and then all of its bytes.
        ///
        /// The traffic is not encrypted, so this is the ground truth for the one
        /// question that reading client code cannot answer: when the contents of a
        /// house are missing, did the server never send them, or did it send them and
        /// the client throw them away? Nothing short of the actual bytes settles it.
        /// </summary>
        public static void LogPacket(Span<byte> data, bool toServer)
        {
            if (!IsEnabled || data.Length == 0)
            {
                return;
            }

            try
            {
                byte id = data[0];

                // The two login packets carry the account password in clear. Everything
                // else goes down whole; these are the only bytes withheld.
                bool secret = id == 0x80 || id == 0x91;

                Write($"pkt\tdir={(toServer ? "out" : "in")}\tid=0x{id:X2}\tname={PacketName(id, toServer)}"
                      + $"\tlen={data.Length}\t{Describe(data, toServer)}"
                      + $"\tbytes={(secret ? "[credentials withheld]" : Hex(data))}");
            }
            catch
            {
            }
        }

        private static string Hex(Span<byte> data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 2);

            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static ushort U16(Span<byte> d, int i)
        {
            return i + 1 < d.Length ? (ushort)((d[i] << 8) | d[i + 1]) : (ushort)0;
        }

        private static uint U32(Span<byte> d, int i)
        {
            return i + 3 < d.Length
                ? (uint)((d[i] << 24) | (d[i + 1] << 16) | (d[i + 2] << 8) | d[i + 3])
                : 0u;
        }

        /// <summary>
        /// A field-level decode for the packets that bear on house loading, so the log
        /// can be read without a hex editor. Everything else just says "-".
        ///
        /// Offsets are counted from the first byte of the packet, so a variable-length
        /// packet's fields start at 3 (ID plus the two length bytes) and a fixed-length
        /// one's at 1. They were taken from the handlers in this file rather than from
        /// a packet reference, so they match what this client actually reads.
        /// </summary>
        private static string Describe(Span<byte> d, bool toServer)
        {
            switch (d[0])
            {
                case 0x1A: // world item, old form - variable length
                    return $"info=serial=0x{U32(d, 3) & 0x7FFFFFFFu:X8} graphic=0x{U16(d, 7):X4}";

                case 0x1D: // delete object
                    return $"info=deleted=0x{U32(d, 1):X8}";

                case 0x20: // update player
                    return $"info=serial=0x{U32(d, 1):X8} graphic=0x{U16(d, 5):X4} at=({U16(d, 11)},{U16(d, 13)})";

                case 0x22: // resync request out / move ack in
                    return toServer ? "info=resync_request" : $"info=move_ack seq={(d.Length > 1 ? d[1] : (byte)0)}";

                case 0x25: // item into container - the container moves to 14 pre-6.0.1.7
                    return $"info=item=0x{U32(d, 1):X8} at=({U16(d, 10)},{U16(d, 12)}) container=0x{U32(d, 15):X8}";

                case 0x3C: // container contents
                    return $"info=count={U16(d, 3)}";

                case 0x6D:
                    return $"info=music={U16(d, 1)}";

                case 0x73:
                    return "info=ping";

                case 0x76: // new subserver - the server moving the client's world window
                    return $"info=at=({U16(d, 1)},{U16(d, 3)})";

                case 0xBC:
                    return $"info=season={(d.Length > 1 ? d[1] : (byte)0)} playsound={(d.Length > 2 ? d[2] : (byte)0)}";

                case 0xBF:
                    return $"info=subcommand=0x{U16(d, 3):X4}";

                case 0xD8: // custom house design - variable length
                    return $"info=house=0x{U32(d, 5):X8} revision={U32(d, 9)}";

                case 0xF3: // world item, new form - fixed length, two bytes skipped
                    return $"info=serial=0x{U32(d, 4):X8} graphic=0x{U16(d, 8):X4} at=({U16(d, 15)},{U16(d, 17)},{(d.Length > 19 ? (sbyte)d[19] : (sbyte)0)})";

                default:
                    return "info=-";
            }
        }

        private static string PacketName(byte id, bool toServer)
        {
            switch (id)
            {
                case 0x02: return "MoveRequest";
                case 0x06: return "DoubleClick";
                case 0x07: return "PickUpItem";
                case 0x08: return "DropItem";
                case 0x09: return "SingleClick";
                case 0x11: return "MobileStatus";
                case 0x13: return "EquipRequest";
                case 0x1A: return "WorldItem";
                case 0x1B: return "LoginConfirm";
                case 0x1C: return "AsciiMessage";
                case 0x1D: return "DeleteObject";
                case 0x20: return "UpdatePlayer";
                case 0x21: return "DenyWalk";
                case 0x22: return toServer ? "Resync" : "MoveAck";
                case 0x23: return "DragEffect";
                case 0x24: return "OpenContainer";
                case 0x25: return "ItemIntoContainer";
                case 0x27: return "DenyMoveItem";
                case 0x29: return "DropOk";
                case 0x2E: return "EquipItem";
                case 0x3A: return "Skills";
                case 0x3C: return "ContainerContents";
                case 0x4F: return "GlobalLight";
                case 0x54: return "PlaySoundEffect";
                case 0x55: return "LoginComplete";
                case 0x65: return "Weather";
                case 0x6C: return "Target";
                case 0x6D: return "PlayMusic";
                case 0x6E: return "CharacterAnimation";
                case 0x72: return "WarMode";
                case 0x73: return "Ping";
                case 0x76: return "NewSubserver";
                case 0x77: return "UpdateMobile";
                case 0x78: return "MobileIncoming";
                case 0x88: return "OpenPaperdoll";
                case 0x89: return "CorpseEquipment";
                case 0x98: return "AllNames3D";
                case 0xA1: return "UpdateHitpoints";
                case 0xAE: return "UnicodeMessage";
                case 0xB0: return "OpenGump";
                case 0xB9: return "EnableFeatures";
                case 0xBC: return "Season";
                case 0xBF: return "GeneralInfo";
                case 0xC1: return "ClilocMessage";
                case 0xC8: return "ViewRange";
                case 0xD6: return "MegaCliloc";
                case 0xD7: return toServer ? "CustomHouseRequest" : "GenericAoS";
                case 0xD8: return "CustomHouse";
                case 0xDC: return "OplInfo";
                case 0xF3: return "WorldItemSA";
                case 0xF7: return "PacketList";
                default: return "?";
            }
        }

        // ------------------------------------------------------------------
        // The world. What arrives, what leaves, and where the player is.
        // ------------------------------------------------------------------

        /// <summary>
        /// The server put an object in the world, or moved one that was already there.
        /// This is the decoded form of 0x1A and 0xF3 after the client has read them, so
        /// a missing chest can be traced from "never arrived" through "arrived and was
        /// dropped" without reading hex.
        /// </summary>
        public static void LogWorldObject(
            uint serial,
            ushort graphic,
            ushort x,
            ushort y,
            sbyte z,
            ushort hue,
            ushort amount,
            byte type,
            bool created
        )
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"object\tserial=0x{serial:X8}\tgraphic=0x{graphic:X4}\tat=({x},{y},{z})"
                  + $"\thue={hue}\tamount={amount}\ttype={type}\tnew={created}");
        }

        /// <summary>
        /// Where the player is, four times a second, whenever it changes. "Where I was
        /// when it happened" was previously only inferable from whatever else happened
        /// to be logged at the time.
        /// </summary>
        public static void LogPlayerPosition()
        {
            if (!IsEnabled || World.Player == null)
            {
                return;
            }

            int x = World.Player.X;
            int y = World.Player.Y;
            int z = World.Player.Z;

            if (x == _lastX && y == _lastY && z == _lastZ && World.MapIndex == _lastMap)
            {
                return;
            }

            _lastX = x;
            _lastY = y;
            _lastZ = z;
            _lastMap = World.MapIndex;

            TryGetHouseContaining(World.Player, out uint inHouse);

            Write($"player\tat=({x},{y},{z})\tdir={World.Player.Direction}"
                  + $"\tcentre=({World.RangeSize.X},{World.RangeSize.Y})"
                  + $"\tinhouse=0x{inHouse:X8}");
        }

        private static int _lastX = int.MinValue;
        private static int _lastY = int.MinValue;
        private static int _lastZ = int.MinValue;
        private static int _lastMap = int.MinValue;

        /// <summary>
        /// The whole picture on a timer: counts, view range, and every house with its
        /// structure and contents side by side. Written unconditionally rather than on
        /// change, so a log always says what the world looked like at any given second
        /// even if nothing was moving.
        /// </summary>
        public static void LogSnapshot()
        {
            if (!IsEnabled || World.Player == null)
            {
                return;
            }

            try
            {
                Write($"world\tat=({World.Player.X},{World.Player.Y},{World.Player.Z})"
                      + $"\tcentre=({World.RangeSize.X},{World.RangeSize.Y})"
                      + $"\tviewrange={World.ClientViewRange}"
                      + $"\titems={World.Items.Count}\tmobiles={World.Mobiles.Count}"
                      + $"\thouses={CountHouses()}");

                foreach (House house in World.HouseManager.Houses)
                {
                    Item multi = World.Items.Get(house.Serial);

                    Write($"house\thouse=0x{house.Serial:X8}"
                          + $"\tat={(multi == null ? "gone" : $"({multi.X},{multi.Y},{multi.Z})")}"
                          + $"\tcomponents={house.Components.Count}\tcontents={CountContents(house.Serial)}"
                          + $"\tplayerinside={World.HouseManager.EntityIntoHouse(house.Serial, World.Player)}"
                          + $"\tdistance={(multi == null ? -1 : multi.Distance)}"
                          + $"\tbonus={(multi == null ? 0 : multi.MultiDistanceBonus)}");
                }
            }
            catch
            {
            }
        }

        private static int CountHouses()
        {
            int n = 0;

            foreach (House _ in World.HouseManager.Houses)
            {
                n++;
            }

            return n;
        }

        /// <summary>A free-form line, for anything that does not deserve its own event.</summary>
        public static void Note(string what)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"note\t{what}");
        }

        // A file opened and closed for every line was affordable when this logged a
        // handful of house events. It is not affordable now that it logs every packet:
        // that is hundreds of open/close pairs a second, on the network thread. The
        // handle is kept open instead, buffered, and flushed once a second - so at most
        // one second of tail is at risk if the client is killed outright.
        private static StreamWriter _writer;
        private static uint _lastFlush;
        private static bool _writerFailed;

        // Packets are read on the network path and everything else on the game loop, so
        // two threads can reach this at once. One lock, held only for the write itself.
        private static readonly object _sync = new object();

        /// <summary>Size at which the log is rolled, so a long session cannot grow past uploading.</summary>
        private const long MaxBytes = 128L * 1024L * 1024L;

        private static void Write(string line)
        {
            try
            {
                // Who and where. Without these the log could not say which character it
                // belonged to, and the facet had to be inferred from coordinates.
                string who = World.Player == null
                    ? "char=-\tmap=-"
                    : $"char={World.Player.Name}\tmap={World.MapIndex}";

                int tab = line.IndexOf('\t');
                string ev = tab < 0 ? line : line.Substring(0, tab);
                string rest = tab < 0 ? "" : line.Substring(tab);

                string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    + "\t" + ev + "\t" + who + rest + Environment.NewLine;

                lock (_sync)
                {
                    StreamWriter writer = Writer();

                    if (writer == null)
                    {
                        return;
                    }

                    writer.Write(text);

                    if (Time.Ticks - _lastFlush >= 1000)
                    {
                        _lastFlush = Time.Ticks;
                        writer.Flush();
                    }
                }
            }
            catch
            {
                // A diagnostic must never interrupt the game or crash the client.
            }
        }

        private static StreamWriter Writer()
        {
            if (_writer != null || _writerFailed)
            {
                return _writer;
            }

            try
            {
                string directory = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "houselog.txt");

                // Roll rather than truncate, so the run before the one being examined
                // is still on disk. Two files, bounded, newest always houselog.txt.
                FileInfo info = new FileInfo(path);

                if (info.Exists && info.Length > MaxBytes)
                {
                    string previous = Path.Combine(directory, "houselog-previous.txt");

                    File.Delete(previous);
                    File.Move(path, previous);
                }

                bool fresh = !File.Exists(path);

                _writer = new StreamWriter(
                    new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 1 << 16),
                    Encoding.UTF8,
                    1 << 16
                );

                if (fresh)
                {
                    _writer.Write("# timestamp\tevent\tchar\tmap\tdetails" + Environment.NewLine);
                }

                if (!_bannerWritten)
                {
                    _bannerWritten = true;

                    _writer.Write(
                        "#" + Environment.NewLine
                        + "# ==== session " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " ====" + Environment.NewLine
                        + "#   client   " + CUOEnviroment.Version + "  (" + CUOEnviroment.BuildTag + ")" + Environment.NewLine
                        + "#" + Environment.NewLine
                    );
                }

                _writer.Flush();
            }
            catch
            {
                // Somewhere unwritable, or the file is held open elsewhere. Give up
                // quietly and permanently rather than retrying on every packet.
                _writerFailed = true;
                _writer = null;
            }

            return _writer;
        }

        /// <summary>Push whatever is buffered to disk. Called when the client shuts down cleanly.</summary>
        public static void Flush()
        {
            try
            {
                lock (_sync)
                {
                    _writer?.Flush();
                }
            }
            catch
            {
            }
        }
    }
}
