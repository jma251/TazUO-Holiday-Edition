using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
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

            if (contents > 0)
            {
                return;
            }

            // Nothing in the house. Two flavours: it had contents a moment ago and they
            // went away, which is the bug outright; or it has never had any since the
            // player walked in, which is either the bug at load time or an empty house.
            bool lost = _best > 0;

            if (!lost && Time.Ticks - _enteredAt < GraceMs)
            {
                return;
            }

            // A structure that never arrived is a different failure and is asked for
            // separately - a resync will not rebuild a design the client never got.
            if (components == 0)
            {
                if (Time.Ticks - _lastAttempt >= CooldownMs && _attempts < MaxAttempts)
                {
                    _attempts++;
                    _lastAttempt = Time.Ticks;

                    NetClient.Socket.Send_CustomHouseDataRequest(_house);

                    HouseDiagnostics.Note(
                        $"retry house=0x{_house:X8} kind=design attempt={_attempts} components=0"
                    );
                }

                return;
            }

            if (!Settings.GlobalSettings.AutoRecoverHouseContents)
            {
                return;
            }

            if (_attempts >= MaxAttempts || Time.Ticks - _lastAttempt < CooldownMs)
            {
                return;
            }

            _attempts++;
            _lastAttempt = Time.Ticks;

            NetClient.Socket.Send_Resync();

            HouseDiagnostics.Note(
                $"retry house=0x{_house:X8} kind=resync attempt={_attempts}"
                + $" components={components} best={_best} lost={lost}"
            );

            if (_attempts == 1)
            {
                GameActions.Print(
                    "House contents missing, asking the server again...",
                    32,
                    MessageType.System
                );
            }
            else if (_attempts == MaxAttempts)
            {
                GameActions.Print(
                    "House contents still missing after several tries.",
                    32,
                    MessageType.System
                );
            }
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
