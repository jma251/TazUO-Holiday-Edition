using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

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
        /// Every house footprint seen this session, kept after the house is let go of.
        ///
        /// Asking HouseManager is not enough. A house is dropped the moment it goes out
        /// of range, and that is exactly when its contents are most likely to be taken -
        /// so a filter that needs a loaded house is blind at the one moment worth
        /// watching. A captured session lost two hundred and twenty-two items between
        /// two visits with no cull, no server delete and no destroy recorded, because
        /// all of it happened while the house was not loaded.
        ///
        /// Bounds do not move, so remembering them costs a few numbers per house and
        /// nothing else.
        /// </summary>
        private static readonly Dictionary<uint, Rectangle> _footprints = new Dictionary<uint, Rectangle>();

        private static void RememberFootprints()
        {
            foreach (House house in World.HouseManager.Houses)
            {
                if (house.Serial == 0 || _footprints.ContainsKey(house.Serial))
                {
                    continue;
                }

                Item multi = World.Items.Get(house.Serial);

                if (multi == null || multi.IsDestroyed || !multi.MultiInfo.HasValue)
                {
                    continue;
                }

                _footprints[house.Serial] = new Rectangle(
                    multi.X + multi.MultiInfo.Value.X,
                    multi.Y + multi.MultiInfo.Value.Y,
                    multi.MultiInfo.Value.Width - multi.MultiInfo.Value.X,
                    multi.MultiInfo.Value.Height - multi.MultiInfo.Value.Y
                );

                // Named once, the first time the house is seen. The tooltip is where the
                // owner is - the client has no notion of who owns a house otherwise, so
                // this is the only way a log line can say "this one is yours" instead of
                // leaving it to be guessed from coordinates.
                World.OPL.TryGetNameAndData(house.Serial, out string name, out string data);

                Write(
                    $"known\thouse=0x{house.Serial:X8}\tat=({multi.X},{multi.Y},{multi.Z})"
                    + $"\tgraphic=0x{multi.Graphic:X4}"
                    + $"\tbounds=({_footprints[house.Serial].X},{_footprints[house.Serial].Y})"
                    + $"-({_footprints[house.Serial].X + _footprints[house.Serial].Width},"
                    + $"{_footprints[house.Serial].Y + _footprints[house.Serial].Height})"
                    + $"\tname={Flatten(name)}\ttip={Flatten(data)}"
                );
            }
        }

        /// <summary>
        /// Is this position inside a house this session has ever known about? Loaded or
        /// not - see the note on the footprints above.
        /// </summary>
        public static bool InAnyKnownHouse(int x, int y, out uint houseSerial)
        {
            houseSerial = 0;

            foreach (KeyValuePair<uint, Rectangle> pair in _footprints)
            {
                Rectangle r = pair.Value;

                if (x >= r.X && x <= r.X + r.Width && y >= r.Y && y <= r.Y + r.Height)
                {
                    houseSerial = pair.Key;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A house has been let go of. This is the moment its contents stop being spared
        /// by the distance cull, so anything that happens to them just after is worth
        /// lining up against it.
        /// </summary>
        public static void LogHouseLetGo(uint serial, string why, int components)
        {
            if (!IsEnabled)
            {
                return;
            }

            int held = 0;

            if (_footprints.TryGetValue(serial, out Rectangle r))
            {
                foreach (KeyValuePair<uint, Item> pair in World.Items)
                {
                    Item item = pair.Value;

                    if (!item.IsMulti && !item.IsDestroyed
                        && item.X >= r.X && item.X <= r.X + r.Width
                        && item.Y >= r.Y && item.Y <= r.Y + r.Height)
                    {
                        held++;
                    }
                }
            }

            Dump(serial, "letgo");

            Write(
                $"letgo\thouse=0x{serial:X8}\twhy={why}\tcomponents={components}"
                + $"\theld={held}"
                + $"\tplayer={(World.Player == null ? "-" : $"({World.Player.X},{World.Player.Y},{World.Player.Z})")}"
            );
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

            if (!InAnyKnownHouse(item.X, item.Y, out uint houseSerial))
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

            if (!InAnyKnownHouse(item.X, item.Y, out uint houseSerial))
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

            if (!InAnyKnownHouse(item.X, item.Y, out uint houseSerial))
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
        private static int _lastInsideCount = -1;

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

            RememberFootprints();

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

            bool entered = houseSerial != _lastContentsHouse;

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

            // On the way in, and on any change in what is being held. Those are the
            // only moments a full dump says anything the previous one did not.
            if (entered || inside != _lastInsideCount)
            {
                Dump(houseSerial, entered ? "entry" : "changed");
            }

            _lastInsideCount = inside;

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
                // Never the placement preview. It is kept under serial zero with no multi
                // item, and EntityIntoHouse answers yes for every object once the multi is
                // gone - so it claims the player is standing in it wherever they are, and
                // every line in this file ends up labelled with a house that is not there.
                if (house.Serial == 0)
                {
                    continue;
                }

                if (World.HouseManager.EntityIntoHouse(house.Serial, World.Player))
                {
                    houseSerial = house.Serial;

                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Everything the client knows about a house, and about every item standing in
        /// it. No filtering and no judgement about what is worth recording - the point
        /// is that a question about a house should be answerable from a capture rather
        /// than needing another build to add the one field that was left out.
        ///
        /// Written on stepping into a house, on the count changing while inside, on the
        /// house being let go of, and on demand with the -housedump command.
        /// </summary>
        public static void Dump(uint serial, string why)
        {
            if (!IsEnabled)
            {
                return;
            }

            // A house being dumped may never have been walked into, so its bounds may
            // not have been picked up yet.
            RememberFootprints();

            Item multi = World.Items.Get(serial);
            World.HouseManager.TryGetHouse(serial, out House house);

            World.OPL.TryGetNameAndData(serial, out string hname, out string htip);

            string bounds = "-";

            if (_footprints.TryGetValue(serial, out Rectangle r))
            {
                bounds = $"({r.X},{r.Y})-({r.X + r.Width},{r.Y + r.Height})";
            }

            Write(
                $"dump\thouse=0x{serial:X8}\twhy={why}"
                + $"\tmulti={(multi == null ? "missing" : $"0x{multi.Graphic:X4}")}"
                + $"\tat={(multi == null ? "-" : $"({multi.X},{multi.Y},{multi.Z})")}"
                + $"\thue={(multi == null ? 0 : multi.Hue)}"
                + $"\tflags={(multi == null ? "-" : multi.Flags.ToString())}"
                + $"\tbounds={bounds}"
                + $"\tcomponents={(house == null ? -1 : house.Components.Count)}"
                + $"\trevision={(house == null ? 0 : house.Revision)}"
                + $"\tcustom={(house == null ? false : house.IsCustom)}"
                + $"\tname={Flatten(hname)}\ttip={Flatten(htip)}"
                + $"\tplayerinside={(World.Player != null && World.HouseManager.EntityIntoHouse(serial, World.Player))}"
                + $"\tplayer={(World.Player == null ? "-" : $"({World.Player.X},{World.Player.Y},{World.Player.Z})")}"
            );

            if (bounds == "-")
            {
                return;
            }

            int count = 0;

            foreach (KeyValuePair<uint, Item> pair in World.Items)
            {
                Item item = pair.Value;

                if (item.X < r.X || item.X > r.X + r.Width || item.Y < r.Y || item.Y > r.Y + r.Height)
                {
                    continue;
                }

                if (item.IsMulti)
                {
                    continue;
                }

                count++;

                World.OPL.TryGetNameAndData(item.Serial, out string iname, out string itip);

                Write(
                    $"dumpitem\thouse=0x{serial:X8}\tkey=0x{pair.Key:X8}\tserial=0x{item.Serial:X8}"
                    + $"\tgraphic=0x{item.Graphic:X4}\tat=({item.X},{item.Y},{item.Z})"
                    + $"\tamount={item.Amount}\thue={item.Hue}\tlayer={item.Layer}"
                    + $"\tcontainer=0x{item.Container:X8}\tonground={item.OnGround}"
                    + $"\tflags={item.Flags}\tcorpse={item.IsCorpse}"
                    + $"\tintile={item.TileChunk != null}"
                    + $"\tdrawnms={(item.LastDrawnTime == 0 ? -1 : Time.Ticks - item.LastDrawnTime)}"
                    + $"\talpha={item.AlphaHue}\tallowdraw={item.AllowedToDraw}"
                    + $"\tdestroyed={item.IsDestroyed}\tdistance={item.Distance}"
                    + $"\tname={Flatten(iname)}\ttip={Flatten(itip)}"
                );
            }

            Write($"dumpend\thouse=0x{serial:X8}\titems={count}");
        }

        /// <summary>Every house the client is holding, in full.</summary>
        public static void DumpAll(string why)
        {
            if (!IsEnabled)
            {
                return;
            }

            RememberFootprints();

            List<uint> serials = new List<uint>();

            foreach (House house in World.HouseManager.Houses)
            {
                serials.Add(house.Serial);
            }

            Write($"dumpall\tcount={serials.Count}\twhy={why}");

            for (int i = 0; i < serials.Count; i++)
            {
                Dump(serials[i], why);
            }
        }


        private static readonly Dictionary<byte, string> _packetNames = new Dictionary<byte, string>
        {
            { 0x11, "MobileStatus" },   { 0x1A, "WorldItem" },      { 0x1C, "ASCIIText" },
            { 0x1D, "DeleteObject" },   { 0x20, "MobileUpdate" },   { 0x21, "DenyWalk" },
            { 0x22, "Resync/Walk" },    { 0x24, "OpenContainer" },  { 0x25, "ContentsUpdate" },
            { 0x2E, "EquipItem" },      { 0x3C, "ContainerContents" }, { 0x4F, "LightLevel" },
            { 0x54, "PlaySound" },      { 0x6D, "PlayMusic" },      { 0x72, "WarMode" },
            { 0x77, "MobileMoving" },   { 0x78, "MobileIncoming" }, { 0xA1, "UpdateHits" },
            { 0xBF, "GeneralInfo" },    { 0xC8, "ClientViewRange" },{ 0xD6, "MegaCliloc" },
            { 0xD8, "CustomHouse" },    { 0xDC, "OPLInfo" },        { 0xF3, "WorldItemNew" },
        };

        private static long _pktWindow;
        private static readonly Dictionary<byte, int> _pktCounts = new Dictionary<byte, int>();

        /// <summary>
        /// Every packet, in and out - what it is and how big, and for the ones that
        /// carry an object, which object and where.
        ///
        /// No hex. The previous version of this wrote whole packets and reached five
        /// hundred megabytes; an id, a length and the decoded fields answer the question
        /// this exists for - whether the server said anything at all - at a fraction of
        /// the size.
        ///
        /// Also totalled once a second per packet type, because "nothing arrived, then
        /// three thousand items arrived" is a shape that is easier to see in counts than
        /// in three thousand lines.
        /// </summary>
        public static void LogPacket(ReadOnlySpan<byte> data, bool toServer)
        {
            if (!IsEnabled || data.Length == 0)
            {
                return;
            }

            byte id = data[0];

            // Never these two. They carry account name and password in clear.
            if (id == 0x80 || id == 0x91)
            {
                return;
            }

            try
            {
                _packetNames.TryGetValue(id, out string name);

                string extra = string.Empty;

                if (!toServer && (id == 0x1A || id == 0xF3))
                {
                    extra = DescribeWorldItem(data, id);
                }
                else if (!toServer && id == 0x1D && data.Length >= 5)
                {
                    extra = $"\tserial=0x{ReadU32(data, 1):X8}";
                }
                else if (!toServer && id == 0xD8 && data.Length >= 11)
                {
                    extra = $"\tserial=0x{ReadU32(data, 4):X8}";
                }

                Write(
                    $"pkt\tdir={(toServer ? "out" : "in")}\tid=0x{id:X2}"
                    + $"\tname={(name ?? "-")}\tlen={data.Length}{extra}"
                );

                if (!toServer)
                {
                    _pktCounts.TryGetValue(id, out int n);
                    _pktCounts[id] = n + 1;

                    if (Time.Ticks >= _pktWindow)
                    {
                        _pktWindow = Time.Ticks + 1000;

                        if (_pktCounts.Count != 0)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder("pktrate");

                            foreach (KeyValuePair<byte, int> pair in _pktCounts)
                            {
                                _packetNames.TryGetValue(pair.Key, out string pn);

                                sb.Append('\t').Append(pn ?? $"0x{pair.Key:X2}").Append('=').Append(pair.Value);
                            }

                            Write(sb.ToString());
                            _pktCounts.Clear();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static string DescribeWorldItem(ReadOnlySpan<byte> data, byte id)
        {
            // 0x1A: serial, graphic, [amount], x, y, [dir], [hue], [flags]
            // 0xF3: 0x0001, type, serial, graphic, inc, amount, amount, x, y, z, dir, hue, flags
            if (id == 0xF3 && data.Length >= 24)
            {
                uint serial = ReadU32(data, 8);
                ushort graphic = (ushort)((data[12] << 8) | data[13]);
                ushort x = (ushort)(((data[18] << 8) | data[19]) & 0x7FFF);
                ushort y = (ushort)(((data[20] << 8) | data[21]) & 0x3FFF);
                sbyte z = (sbyte)data[22];

                string house = InAnyKnownHouse(x, y, out uint hs) ? $"0x{hs:X8}" : "-";

                return $"\tserial=0x{serial:X8}\tgraphic=0x{graphic:X4}\tat=({x},{y},{z})\tinhouse={house}";
            }

            if (id == 0x1A && data.Length >= 12)
            {
                uint serial = ReadU32(data, 3);

                return $"\tserial=0x{serial:X8}";
            }

            return string.Empty;
        }

        private static uint ReadU32(ReadOnlySpan<byte> d, int i)
        {
            if (i + 3 >= d.Length)
            {
                return 0;
            }

            return (uint)((d[i] << 24) | (d[i + 1] << 16) | (d[i + 2] << 8) | d[i + 3]);
        }

        /// <summary>Tooltips arrive with newlines in them, and this file is one record a line.</summary>
        private static string Flatten(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "-";
            }

            return text.Replace("\r", " ").Replace("\n", " | ").Replace("\t", " ");
        }

        private static readonly object _writeLock = new object();
        private static readonly System.Text.StringBuilder _buffer = new System.Text.StringBuilder(1 << 16);
        private static long _nextFlush;

        /// <summary>
        /// Buffered on purpose. Packets arrive in the hundreds a second and the previous
        /// version opened the file for every line, which is a stutter the client would
        /// wear for as long as the log was on. Sending is done off the game loop, so the
        /// buffer is locked.
        /// </summary>
        private static void Write(string line)
        {
            try
            {
                lock (_writeLock)
                {
                    _buffer
                        .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                        .Append('\t')
                        .Append(line)
                        .Append(Environment.NewLine);

                    if (_buffer.Length >= 32768 || Time.Ticks >= _nextFlush)
                    {
                        FlushLocked();
                    }
                }
            }
            catch
            {
                // A diagnostic must never interrupt the game or crash the client.
            }
        }

        /// <summary>Push whatever is held out to disk. Safe to call at any time.</summary>
        public static void Flush()
        {
            try
            {
                lock (_writeLock)
                {
                    FlushLocked();
                }
            }
            catch
            {
            }
        }

        private static void FlushLocked()
        {
            _nextFlush = Time.Ticks + 1000;

            if (_buffer.Length == 0)
            {
                return;
            }

            string directory = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, "houselog.txt");

            if (!File.Exists(path))
            {
                File.AppendAllText(path, "# timestamp\tevent\tdetails" + Environment.NewLine);
            }

            File.AppendAllText(path, _buffer.ToString());

            _buffer.Clear();
        }

    }
}
