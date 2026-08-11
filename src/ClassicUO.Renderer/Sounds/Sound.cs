using ClassicUO.Assets;
using ClassicUO.IO.Audio;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer.Sounds
{
    public sealed class Sound
    {
        const int MAX_SOUND_DATA_INDEX_COUNT = 0xFFFF;

        private readonly IO.Audio.Sound[] _musics = new IO.Audio.Sound[MAX_SOUND_DATA_INDEX_COUNT];
        private readonly IO.Audio.Sound[] _sounds = new IO.Audio.Sound[MAX_SOUND_DATA_INDEX_COUNT];
        private readonly bool _useDigitalMusicFolder;

        // The era folder and its contents, resolved once when the era changes rather
        // than per lookup. Names are kept as they are on disk so a case difference
        // between Config.txt ("oldult01") and the file ("OLDULT01.MID") does not matter.
        private string _era = string.Empty;
        private string _eraDirectory;
        private string[] _eraFiles = new string[0];

        public Sound()
        {
            _useDigitalMusicFolder = Directory.Exists(Path.Combine(UOFileManager.BasePath, "Music", "Digital"));
        }

        /// <summary>
        /// Name of the subfolder of Music/Digital to prefer, or empty for stock
        /// behaviour. Pushed in from the client: this assembly cannot see the profile.
        /// </summary>
        public string MusicEra => _era;

        /// <summary>Number of files found in the era folder. 0 means the folder was
        /// missing or empty, which is why nothing would resolve out of it.</summary>
        public int MusicEraFileCount => _eraFiles.Length;

        public void SetMusicEra(string era)
        {
            era = era ?? string.Empty;

            if (string.Equals(era, _era, StringComparison.Ordinal))
            {
                return;
            }

            _era = era;
            _eraDirectory = null;
            _eraFiles = new string[0];

            if (era.Length != 0)
            {
                try
                {
                    string dir = Path.Combine(UOFileManager.BasePath, "Music", "Digital", era);

                    if (Directory.Exists(dir))
                    {
                        _eraDirectory = dir;
                        _eraFiles = Directory.GetFiles(dir);
                    }
                    else
                    {
                        Log.Warn($"Music era folder not found, falling back to the stock music: {dir}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not read the music era folder '{era}': {ex}");
                }
            }

            ClearMusicCache();
        }

        /// <summary>
        /// Drops the cached music objects so the next lookup resolves against the
        /// current era. Callers are expected to have stopped playback first.
        /// </summary>
        public void ClearMusicCache()
        {
            for (int i = 0; i < _musics.Length; i++)
            {
                _musics[i] = null;
            }
        }

        public IO.Audio.Sound GetSound(int index)
        {
            if (index >= 0 && index < MAX_SOUND_DATA_INDEX_COUNT)
            {
                ref IO.Audio.Sound sound = ref _sounds[index];

                if (sound == null && SoundsLoader.Instance.TryGetSound(index, out byte[] data, out string name))
                {
                    sound = new UOSound(name, index, data);
                }

                return sound;
            }

            return null;
        }

        public IO.Audio.Sound GetMusic(int index)
        {
            if (index >= 0 && index < MAX_SOUND_DATA_INDEX_COUNT)
            {
                ref IO.Audio.Sound music = ref _musics[index];

                if (music == null && SoundsLoader.Instance.TryGetMusicData(index, out string name, out bool loop))
                {
                    string path = ResolveMusicPath(name);

                    if (path.EndsWith(".mid", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Part B replaces this with a UOMidi. Until then there is no
                        // synthesizer, and handing a MIDI to the MP3 decoder would just
                        // produce silence, so play the stock track instead.
                        Log.Warn($"MIDI music is not supported yet, using the stock track for '{name}'.");

                        path = StockMusicPath(name);
                    }

                    music = new UOMusic(index, name, loop, path);
                }

                return music;
            }

            return null;
        }

        /// <summary>
        /// Era folder first (.mp3, then .mid), stock install second. With no era set
        /// this returns exactly what the original code returned.
        /// </summary>
        private string ResolveMusicPath(string name)
        {
            if (_eraDirectory != null)
            {
                // Config.txt names sometimes carry an extension and sometimes do not,
                // so strip whatever is there before probing.
                string bare = Path.GetFileNameWithoutExtension(name);

                string found = FindInEra(bare + ".mp3") ?? FindInEra(bare + ".mid");

                if (found != null)
                {
                    return found;
                }
            }

            return StockMusicPath(name);
        }

        private string StockMusicPath(string name)
        {
            string path = _useDigitalMusicFolder ? $"Music/Digital/{name}" : $"Music/{name}";

            if (!path.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase))
            {
                path += ".mp3";
            }

            return UOFileManager.GetUOFilePath(path);
        }

        private string FindInEra(string fileName)
        {
            for (int i = 0; i < _eraFiles.Length; i++)
            {
                if (string.Equals(Path.GetFileName(_eraFiles[i]), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return _eraFiles[i];
                }
            }

            return null;
        }
    }
}
