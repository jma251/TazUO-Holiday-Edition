using System;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// What the music system is doing, on screen, while you play. Reading it out of
    /// the log afterwards means guessing at which build and which era produced which
    /// line; this answers the two questions that actually come up - what am I hearing,
    /// and what did the server ask for - at the moment they come up.
    ///
    /// Double-click to fold it down to one line.
    /// </summary>
    internal class MusicInfoGump : Gump
    {
        private static Point _lastPosition = new Point(-1, -1);

        private readonly AlphaBlendControl _background;
        private string _text = string.Empty;
        private uint _nextUpdate;

        public MusicInfoGump(int x, int y) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            // Not written into the profile's gump list. Whether it is shown is a global
            // setting, and GameScene puts it back on login from that - saving it as
            // well would give two sources of truth for one checkbox.
            CanBeSaved = false;

            Width = 260;
            Height = 90;

            X = _lastPosition.X <= 0 ? x : _lastPosition.X;
            Y = _lastPosition.Y <= 0 ? y : _lastPosition.Y;

            Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height });

            LayerOrder = UILayer.Over;
            WantUpdateSize = true;
        }

        public bool IsMinimized { get; set; }

        public override GumpType GumpType => GumpType.MusicInfo;

        /// <summary>
        /// Shows or hides the panel. Called from the option, and again on entering the
        /// world so the choice survives a relog.
        /// </summary>
        public static void Toggle(bool show)
        {
            MusicInfoGump existing = UIManager.GetGump<MusicInfoGump>();

            if (!show)
            {
                existing?.Dispose();

                return;
            }

            if (existing == null)
            {
                UIManager.Add(new MusicInfoGump(100, 100));
            }
            else
            {
                existing.SetInScreen();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return false;
            }

            IsMinimized = !IsMinimized;

            return true;
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed || Time.Ticks < _nextUpdate)
            {
                return;
            }

            _nextUpdate = Time.Ticks + 250;

            _lastPosition.X = X;
            _lastPosition.Y = Y;

            AudioManager.MusicStatus st = Client.Game.Audio.GetMusicStatus();

            _text = IsMinimized ? OneLine(st) : Full(st);

            Vector2 size = Fonts.Bold.MeasureString(_text);

            _background.Width = Width = (int)size.X + 20;
            _background.Height = Height = (int)size.Y + 16;

            WantUpdateSize = true;
        }

        private static string OneLine(AudioManager.MusicStatus st)
        {
            if (st.WarIndex >= 0)
            {
                return $"combat {st.WarIndex}  {Name(st.WarIndex)}";
            }

            return st.Playing
                ? $"{st.Index}  {File(st.File)}  {(st.FromMap ? "map" : "server")}"
                : "silent";
        }

        private static string Full(AudioManager.MusicStatus st)
        {
            StringBuilder sb = new StringBuilder();

            // What you are hearing, and who chose it.
            if (st.WarIndex >= 0)
            {
                sb.Append($"combat  {st.WarIndex}  {Name(st.WarIndex)}\n");
            }

            if (!st.Playing)
            {
                sb.Append("playing  nothing\n");
            }
            else
            {
                sb.Append($"playing  {st.Index}  {File(st.File)}  {(st.Loops ? "loops" : "once")}\n");
                sb.Append($"  from   {(st.FromMap ? "music map" : "server")}\n");
            }

            // What the server asked for, whatever is actually playing.
            if (st.ServerIndex < 0)
            {
                sb.Append("server   has not said anything yet\n");
            }
            else if (st.ServerIndex == Constants.MUSIC_STOP_INDEX)
            {
                sb.Append($"server   stop - no music here  ({st.ServerSecs}s ago)\n");
            }
            else
            {
                sb.Append($"server   {st.ServerIndex}  {Name(st.ServerIndex)}  ({st.ServerSecs}s ago)\n");
            }

            // What the map would play here, whether or not it is playing it.
            sb.Append(
                st.MapHasAnswer
                    ? $"map      {st.MapTrack}  {Name(st.MapTrack)}  {st.MapArea}\n"
                    : "map      nothing covers this spot\n"
            );

            sb.Append(
                $"mode     {ModeName(st.Mode)}{(st.IgnoreStop ? ", ignoring stop" : "")}\n" +
                $"era      {(string.IsNullOrEmpty(st.Era) ? "Default (stock Music/Digital)" : st.Era)}"
            );

            return sb.ToString();
        }

        private static string ModeName(int mode)
        {
            switch (mode)
            {
                case 0: return "off - server only";
                case 1: return "authentic";
                case 2: return "seamless";
                case 3: return "continuous";
                default: return mode.ToString();
            }
        }

        // The filename the index resolves to under the era in force. Falls back to the
        // name in the config when nothing is loaded to ask.
        private static string Name(int index)
        {
            return index >= 0 && SoundsLoader.Instance.TryGetMusicData(index, out string name, out bool _)
                ? name
                : "?";
        }

        private static string File(string path)
        {
            return string.IsNullOrEmpty(path) ? "?" : System.IO.Path.GetFileName(path);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            batcher.DrawString(Fonts.Bold, _text, x + 10, y + 8, ShaderHueTranslator.GetHueVector(0));

            return true;
        }
    }
}
