using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Recovers a house whose contents did not load, without the player having to walk
    /// out and back in.
    ///
    /// What walking out and back in actually does is make the server resend everything
    /// in range, because the client's world window moves. Packet 0x22 asks for the same
    /// thing directly: the server's handler for it ends in SendEverything(), which is
    /// literally every item and mobile in range again. The client already sends 0x22 for
    /// the same reason elsewhere - GameScene sends it when the connection looks like it
    /// hung, on the assumption something was missed. Same assumption, better trigger.
    ///
    /// The trigger is deliberately narrow. It fires only while the player is standing
    /// inside a house whose structure is built and which contains nothing at all, and it
    /// stops after a few tries so a genuinely empty house does not get asked forever.
    /// Everything it sees and everything it decides goes to the log either way, so a
    /// build that fails to recover still says why.
    /// </summary>
    internal static class HouseContentsWatchdog
    {
        /// <summary>How long the player must be inside an empty-looking house before the first ask.</summary>
        private const uint GraceMs = 3000;

        /// <summary>Gap between asks. The server needs time to answer before it is worth asking again.</summary>
        private const uint CooldownMs = 5000;

        /// <summary>Asks per visit. A house that is genuinely empty must stop being asked about.</summary>
        private const int MaxAttempts = 5;

        private static uint _house;
        private static uint _enteredAt;
        private static int _best;
        private static int _attempts;
        private static uint _lastAttempt;

        private static uint _nextPositionCheck;
        private static uint _nextSnapshot;
        private static uint _nextWatch;

        public static void Update()
        {
            if (!World.InGame || World.Player == null)
            {
                return;
            }

            if (Time.Ticks >= _nextPositionCheck)
            {
                _nextPositionCheck = Time.Ticks + 250;
                HouseDiagnostics.LogPlayerPosition();
            }

            if (Time.Ticks >= _nextSnapshot)
            {
                _nextSnapshot = Time.Ticks + 1000;
                HouseDiagnostics.LogSnapshot();
            }

            if (Time.Ticks < _nextWatch)
            {
                return;
            }

            _nextWatch = Time.Ticks + 1000;

            ForgetUnloadedHouses();

            if (Settings.GlobalSettings.AutoRecoverHouseContents)
            {
                int repaired = Sweep();

                if (repaired > 0)
                {
                    HouseDiagnostics.Note($"sweep repaired={repaired}");

                    GameActions.Print(
                        $"Put back {repaired} item{(repaired == 1 ? "" : "s")} the client had lost track of.",
                        68,
                        MessageType.System
                    );
                }
            }

            Watch();
        }

        /// <summary>Forget everything. Called when the world goes away, so a new session starts clean.</summary>
        public static void Reset()
        {
            _house = 0;
            _enteredAt = 0;
            _best = 0;
            _attempts = 0;
            _lastAttempt = 0;

            // What a house was seen holding belongs to the session that saw it.
            _mostSeen.Clear();
        }

        private static void Watch()
        {
            uint serial = HouseAroundPlayer();

            if (serial != _house)
            {
                if (_house != 0)
                {
                    HouseDiagnostics.Note($"left house=0x{_house:X8} best={_best} attempts={_attempts}");
                }

                _house = serial;
                _enteredAt = Time.Ticks;
                _best = 0;
                _attempts = 0;
                _lastAttempt = 0;

                if (serial != 0)
                {
                    HouseDiagnostics.Note($"entered house=0x{serial:X8}");

                    AskOnEntry(serial);
                }
            }

            if (_house == 0 || !World.HouseManager.TryGetHouse(_house, out House house))
            {
                return;
            }

            int components = house.Components.Count;
            int contents = HouseDiagnostics.CountContents(_house);

            if (contents > _best)
            {
                if (_attempts > 0)
                {
                    HouseDiagnostics.Note(
                        $"recovered house=0x{_house:X8} contents={contents} after={_attempts} asks"
                    );

                    GameActions.Print(
                        $"House contents recovered ({contents} items).",
                        68,
                        MessageType.System
                    );
                }

                _best = contents;
                _attempts = 0;

                return;
            }

            // How much this house has ever been seen holding, which is the only thing
            // that can tell a house that failed to load from a house that is empty.
            //
            // Watching for contents falling to nothing inside a single visit was not
            // enough, and the log says exactly why: the player rode home, the server
            // sent sixteen of two hundred and thirty-eight items and stopped, and the
            // player walked in on sixteen. Nothing dropped, so nothing looked wrong -
            // and moving around inside did not help, because the two item packets that
            // arrived over the next ten minutes were all the server was going to send.
            // What it took was stepping off the foundation and back on.
            //
            // Held per house, so an empty house is still known to be empty and is never
            // asked about, and a house that has held two hundred items and is showing
            // sixteen is not mistaken for one.
            if (!_mostSeen.TryGetValue(_house, out int mostSeen) || contents > mostSeen)
            {
                _mostSeen[_house] = contents;

                return;
            }

            if (components == 0 || Time.Ticks - _enteredAt < GraceMs)
            {
                return;
            }

            // Well short of what this house is known to hold. Half is deliberately
            // coarse: a few things moved or taken since last time is normal, most of the
            // room missing is not.
            bool wellShort = mostSeen > 0 && (contents == 0 || contents * 2 < mostSeen);

            if (!wellShort || !Settings.GlobalSettings.AutoRecoverHouseContents)
            {
                return;
            }

            if (_attempts >= MaxAttempts || Time.Ticks - _lastAttempt < CooldownMs)
            {
                return;
            }

            _attempts++;
            _lastAttempt = Time.Ticks;

            // Ask for everything in range again. Walking out of the door and back in is
            // what fixes this by hand; the server answers this packet with the same
            // thing, without needing the player to do it.
            NetClient.Socket.Send_Resync();

            HouseDiagnostics.Note(
                $"retry house=0x{_house:X8} kind=resync attempt={_attempts}"
                + $" components={components} contents={contents} mostseen={mostSeen}"
            );

            if (_attempts == 1)
            {
                GameActions.Print(
                    $"House is showing {contents} of {mostSeen} items, asking the server again...",
                    32,
                    MessageType.System
                );
            }
        }

        /// <summary>The most this house has ever been seen holding, by serial.</summary>
        private static readonly Dictionary<uint, int> _mostSeen = new Dictionary<uint, int>();

        private static readonly List<uint> _forget = new List<uint>();

        /// <summary>
        /// Forget what a house was holding once the client has let go of the house.
        ///
        /// This is what keeps the entry ask honest. Skipping the ask for a house that is
        /// already as full as it has ever been is safe for stepping out of the door and
        /// back in - the count is seconds old and the house never left. It is not safe
        /// across a house unloading and coming back, because that is precisely when it
        /// arrives short: a house that only ever loaded sixteen items would have sixteen
        /// recorded as its truth, and would be skipped ever after.
        ///
        /// Dropping the record with the house means a house that has been away always
        /// gets asked about on the way back in, which is the case the ask exists for,
        /// and only the cheap in-and-out hops are skipped.
        /// </summary>
        private static void ForgetUnloadedHouses()
        {
            if (_mostSeen.Count == 0)
            {
                return;
            }

            _forget.Clear();

            foreach (KeyValuePair<uint, int> pair in _mostSeen)
            {
                if (!World.HouseManager.Exists(pair.Key) || World.Items.Get(pair.Key) == null)
                {
                    _forget.Add(pair.Key);
                }
            }

            for (int i = 0; i < _forget.Count; i++)
            {
                _mostSeen.Remove(_forget[i]);
            }
        }

        /// <summary>
        /// One resync on stepping into a house, whatever it appears to be holding.
        ///
        /// The server already tries to do this. HouseRegion.OnEnter ends in
        /// SendEverything, which is precisely the right idea - but it fires on crossing
        /// into the house *region*, and the region is the house's whole bounding
        /// rectangle. Its CanSee refuses every item in the house unless IsInside is
        /// true, and IsInside is a stricter test needing real floor at your height. Land
        /// on the steps or the porch and OnEnter has fired, sent nothing, and will not
        /// fire again - so walking the rest of the way in brings nothing with it.
        ///
        /// Watching for a shortfall does not cover this on its own. What a house is
        /// holding is only known from having seen it holding more, in this session, so
        /// arriving already short - or logging in inside a house that is already short -
        /// records the wrong number as the truth and nothing ever looks amiss.
        ///
        /// So the client asks once, at the moment the server's own attempt should have
        /// worked and did not: when the player is genuinely inside. One packet per
        /// entry, which is nothing next to what the server does for a footstep, and it
        /// needs no history to be right.
        /// </summary>
        private static void AskOnEntry(uint serial)
        {
            if (!Settings.GlobalSettings.AutoRecoverHouseContents)
            {
                return;
            }

            int contents = HouseDiagnostics.CountContents(serial);

            // Nothing to ask for if the house is already holding as much as it has ever
            // been seen holding. Asking anyway is not free: the answer re-sends every
            // item in the room, and putting an item back on a tile that is already full
            // means walking that tile's list to find where it sorts, which is far more
            // work than dropping it onto a bare one. A whole furnished room re-seated in
            // a frame is the stutter, and half the asks in a captured session were this
            // - a full house being sent a full house.
            //
            // With no record of the house at all this cannot tell a house that failed to
            // load from one that is genuinely empty, so it asks. That is the case the
            // entry ask exists for.
            if (_mostSeen.TryGetValue(serial, out int mostSeen) && mostSeen > 0 && contents >= mostSeen)
            {
                HouseDiagnostics.Note(
                    $"entryskip house=0x{serial:X8} contents={contents} mostseen={mostSeen}"
                );

                return;
            }

            // Counts as an ask, so the shortfall test waits its cooldown rather than
            // sending a second one on top. Without this the entry ask left the cooldown
            // at zero and a second resync went out three seconds later, while the first
            // one's couple of hundred packets had only just arrived - two whole rooms
            // delivered back to back, which is a frame's work in one go and is felt.
            _lastAttempt = Time.Ticks;

            NetClient.Socket.Send_Resync();

            HouseDiagnostics.Note(
                $"entryask house=0x{serial:X8} kind=resync contents={contents} mostseen={mostSeen}"
            );
        }

        /// <summary>
        /// Put back anything that is in the world but not linked into the tile it is
        /// standing on.
        ///
        /// This is the repair for the failure in GameObject.RemoveFromTile: an object
        /// still in World.Items, at real coordinates, which nothing can reach and so
        /// nothing draws. It cannot be found by counting contents - the count includes
        /// it - and no packet brings it back, because as far as the server is concerned
        /// it was delivered. Walking out and back in worked because that tore the chunk
        /// down and relinked everything from scratch.
        ///
        /// Costs one short walk of a tile's list per nearby item, once a second, and is
        /// worth keeping even with the cause fixed: it is cheap, it says in the log
        /// exactly what it put back, and there is more than one way to unlink an object.
        /// </summary>
        private static int Sweep()
        {
            if (World.Map == null)
            {
                return 0;
            }

            int repaired = 0;

            uint held = Client.Game.GameCursor.ItemHold.Enabled
                ? Client.Game.GameCursor.ItemHold.Serial
                : 0;

            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.IsDestroyed || !item.OnGround || item.Serial == held)
                {
                    continue;
                }

                // Beyond the view range it is on its way out anyway, and the tile it
                // claims to stand on may not be loaded.
                if (item.Distance > World.ClientViewRange + (item.IsMulti ? item.MultiDistanceBonus : 0))
                {
                    continue;
                }

                if (LinkedToItsTile(item))
                {
                    continue;
                }

                HouseDiagnostics.LogOrphanRepaired(item);

                item.AddToTile();

                repaired++;
            }

            return repaired;
        }

        private static bool LinkedToItsTile(Item item)
        {
            Chunk chunk = World.Map.GetChunk(item.X, item.Y, false);

            if (chunk == null)
            {
                // No chunk loaded here, so there is nothing to be linked into and
                // nothing to repair. Not the same thing as being orphaned.
                return true;
            }

            for (GameObject o = chunk.GetHeadObject(item.X % 8, item.Y % 8); o != null; o = o.TNext)
            {
                if (ReferenceEquals(o, item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The house the player is standing in, or zero.
        ///
        /// A house whose multi item has gone is skipped: HouseManager answers "inside"
        /// for every object once the multi is missing, so a leftover house - the
        /// serial-zero placement preview above all - would otherwise be reported as the
        /// one being stood in, anywhere on the map.
        /// </summary>
        private static uint HouseAroundPlayer()
        {
            foreach (House house in World.HouseManager.Houses)
            {
                if (house.Serial == 0 || World.Items.Get(house.Serial) == null)
                {
                    continue;
                }

                if (World.HouseManager.EntityIntoHouse(house.Serial, World.Player))
                {
                    return house.Serial;
                }
            }

            return 0;
        }
    }
}
