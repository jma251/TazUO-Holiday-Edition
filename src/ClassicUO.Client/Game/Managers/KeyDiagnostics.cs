using System;
using System.Globalization;
using System.IO;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Input;
using SDL2;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Temporary diagnostic for keys that refuse to work as hotkeys. Silent and
    /// file-only - no chat, no journal, no on-screen text - and never allowed to throw.
    ///
    /// Reading the code settles nothing here: no key is special-cased anywhere in the
    /// client, so a key that does nothing is being taken before the macro lookup runs.
    /// This records the three points where that can happen:
    ///   1. Did the key arrive from SDL at all?
    ///   2. Did a plugin claim it? A plugin returning "handled" makes the client drop
    ///      the press entirely - no focused control, no scene handler, no macro.
    ///   3. Did the scene reach the macro lookup, and did the lookup find anything?
    ///
    /// Intended to be removed once the cause is known.
    /// </summary>
    internal static class KeyDiagnostics
    {
        private static bool IsEnabled => Settings.GlobalSettings.LogKeyPresses;

        /// <summary>
        /// The first thing that happens to a key press, before anything else can eat it.
        /// A key that produces no line here never reached the client - which points at
        /// the keyboard, an overlay, or the OS, not at anything in this codebase.
        /// </summary>
        public static void LogKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, bool isRepeat, bool pluginAllowedThrough)
        {
            // Held keys repeat many times a second; only the first press is interesting.
            if (!IsEnabled || isRepeat)
            {
                return;
            }

            Write(
                $"keydown\t{Describe(key, mod)}"
                + $"\tpluginclaimed={!pluginAllowedThrough}"
                + $"\tfocus={DescribeFocus()}"
            );
        }

        /// <summary>
        /// The scene's key handler gives up before the macro lookup in several places.
        /// Each has its own reason string so the log says which one it was.
        /// </summary>
        public static void LogSceneStop(SDL.SDL_Keycode key, string reason)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write($"scenestop\tkey={KeysTranslator.TryGetKey(key)}\treason={reason}");
        }

        /// <summary>
        /// The macro lookup itself. "found=False" here with a binding saved in the
        /// options means the binding is not being stored the way it is being searched.
        /// </summary>
        public static void LogMacroLookup(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift, string macroName)
        {
            if (!IsEnabled)
            {
                return;
            }

            Write(
                $"macro\tkey={KeysTranslator.TryGetKey(key)}\tcode={(int)key}"
                + $"\talt={alt}\tctrl={ctrl}\tshift={shift}"
                + $"\tfound={macroName != null}"
                + $"\tname={macroName ?? "-"}"
            );
        }

        private static string Describe(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            SDL.SDL_Keymod filtered = mod & ~Keyboard.IgnoreKeyMod;

            StringBuilder sb = new StringBuilder();

            if ((filtered & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE)
            {
                sb.Append("shift+");
            }

            if ((filtered & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE)
            {
                sb.Append("ctrl+");
            }

            if ((filtered & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE)
            {
                sb.Append("alt+");
            }

            sb.Append(KeysTranslator.TryGetKey(key));

            return $"key={sb}\tcode={(int)key}\trawmod={(int)mod}";
        }

        /// <summary>
        /// Plugins are only consulted while the chat line holds keyboard focus, so
        /// knowing where focus was explains why a key behaved differently in the
        /// options window than it does in play.
        /// </summary>
        private static string DescribeFocus()
        {
            var focus = UIManager.KeyboardFocusControl;

            if (focus == null)
            {
                return "none";
            }

            if (UIManager.SystemChat != null && focus == UIManager.SystemChat.TextBoxControl)
            {
                return "systemchat";
            }

            return focus.GetType().Name;
        }

        private static void Write(string line)
        {
            try
            {
                string directory = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "keylog.txt");

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
