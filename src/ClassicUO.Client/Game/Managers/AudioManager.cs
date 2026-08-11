#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ClassicUO.Utility;
using ClassicUO.Configuration;
using ClassicUO.IO.Audio;
using ClassicUO.Assets;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.Game.Managers
{
    internal class AudioManager
    {
        const float SOUND_DELTA = 250;

        private bool _canReproduceAudio = true;
        private readonly LinkedList<UOSound> _currentSounds = new LinkedList<UOSound>();
        private readonly UOMusic[] _currentMusic = { null, null };
        private readonly int[] _currentMusicIndices = { 0, 0 };
        public int LoginMusicIndex { get; private set; }
        public int DeathMusicIndex { get; } = 42;

        public void Initialize()
        {
            try
            {
                new DynamicSoundEffectInstance(0, AudioChannels.Stereo).Dispose();
            }
            catch (NoAudioHardwareException ex)
            {
                Log.Warn(ex.ToString());
                _canReproduceAudio = false;
            }

            LoginMusicIndex = Client.Version >= ClientVersion.CV_7000 ? 78 : Client.Version > ClientVersion.CV_308Z ? 0 : 8;

            Client.Game.Activated += OnWindowActivated;
            Client.Game.Deactivated += OnWindowDeactivated;

            // UOMusic lives in an assembly that cannot see the settings or the world,
            // so the diagnostic is attached from here.
            UOMusic.Looped = MusicDiagnostics.Looped;
            UOMusic.Ended = MusicDiagnostics.Ended;
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            SoundEffect.MasterVolume = 0;
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            SoundEffect.MasterVolume = 1;
        }

        public void PlaySound(int index)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (!_canReproduceAudio || currentProfile == null)
            {
                return;
            }

            float volume = currentProfile.SoundVolume / SOUND_DELTA;

            if (Client.Game.IsActive)
            {
                if (!currentProfile.ReproduceSoundsInBackground)
                {
                    volume = currentProfile.SoundVolume / SOUND_DELTA;
                }
            }
            else if (!currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (!currentProfile.EnableSound || !Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            UOSound sound = (UOSound) Client.Game.Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume))
            {
                sound.X = -1;
                sound.Y = -1;
                sound.CalculateByDistance = false;

                _currentSounds.AddLast(sound);
            }
        }

        public void PlaySoundWithDistance(int index, int x, int y)
        {
            if (!_canReproduceAudio || !World.InGame)
            {
                return;
            }

            int distX = Math.Abs(x - World.Player.X);
            int distY = Math.Abs(y - World.Player.Y);
            int distance = Math.Max(distX, distY);

            Profile currentProfile = ProfileManager.CurrentProfile;
            float volume = currentProfile.SoundVolume / SOUND_DELTA;
            float distanceFactor = 0.0f;

            if (distance >= 1)
            {
                float volumeByDist = volume / (World.ClientViewRange + 1);
                distanceFactor = volumeByDist * distance;
            }

            if (distance > World.ClientViewRange)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (currentProfile == null || !currentProfile.EnableSound || !Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            UOSound sound = (UOSound)Client.Game.Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume, distanceFactor))
            {
                sound.X = x;
                sound.Y = y;
                sound.CalculateByDistance = true;

                _currentSounds.AddLast(sound);
            }
        }

        // The era that the music config and the file cache were last built for. null
        // means "never applied", which is not the same as "" (the stock install).
        private static string _appliedMusicEra;

        /// <summary>
        /// Lists the subfolders of Music/Digital. Each one is an era the player can
        /// pick, so adding an era is creating a folder rather than a code change.
        /// </summary>
        public static string[] GetAvailableMusicEras()
        {
            try
            {
                string dir = Path.Combine(UOFileManager.BasePath, "Music", "Digital");

                if (Directory.Exists(dir))
                {
                    string[] dirs = Directory.GetDirectories(dir);

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        dirs[i] = Path.GetFileName(dirs[i]);
                    }

                    Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);

                    return dirs;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not list the music era folders: {ex}");
            }

            return new string[0];
        }

        /// <summary>
        /// Rebuilds the music config and drops the cached tracks when the selected era
        /// has moved. Cheap when nothing changed, so it can sit on the play path and
        /// cover startup and profile switches without hooking into profile loading.
        /// The era is global, so this also applies on the login screen, which plays
        /// before any profile exists.
        /// </summary>
        private static void EnsureMusicEraApplied()
        {
            string era = Settings.GlobalSettings.MusicEra ?? string.Empty;

            if (string.Equals(era, _appliedMusicEra, StringComparison.Ordinal))
            {
                return;
            }

            _appliedMusicEra = era;

            SoundsLoader.LoadMusicConfig(string.IsNullOrEmpty(era) ? null : era);
            Client.Game.Sounds.SetMusicEra(era);
        }

        /// <summary>
        /// Applies an era change straight away: stop, rebuild, and restart whatever was
        /// playing. Without this the change would not be heard until the next track,
        /// because both the config and the resolved files are cached.
        /// </summary>
        public void ReloadMusicEra()
        {
            int index = _currentMusicIndices[0];
            bool warMode = _currentMusic[1] != null;

            StopMusic();

            EnsureMusicEraApplied();

            PlayMusic(index, warMode);
        }

        public void PlayMusic(int music, bool iswarmode = false, bool is_login = false)
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
            {
                return;
            }

            EnsureMusicEraApplied();

            float volume;

            if (is_login)
            {
                volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
            }
            else
            {
                Profile currentProfile = ProfileManager.CurrentProfile;

                if (currentProfile == null || !currentProfile.EnableMusic)
                {
                    volume = 0;
                }
                else
                {
                    volume = currentProfile.MusicVolume / SOUND_DELTA;
                }

                if (currentProfile != null && !currentProfile.EnableCombatMusic && iswarmode)
                {
                    return;
                }
            }


            if (volume < -1 || volume > 1f)
            {
                return;
            }

            Sound m = Client.Game.Sounds.GetMusic(music);

            if (m == null && _currentMusic[0] != null)
            {
                StopMusic();
            }
            else if (m != null && (m != _currentMusic[0] || iswarmode))
            {
                StopMusic();

                int idx = iswarmode ? 1 : 0;
                _currentMusicIndices[idx] = music;
                _currentMusic[idx] = (UOMusic) m;

                _currentMusic[idx].Play(Time.Ticks, volume);

                MusicDiagnostics.Started(music, _currentMusic[idx].IsLooping, is_login ? "login" : iswarmode ? "warmode" : "");
            }
            else if (m != null)
            {
                // Already playing this track, so the request changes nothing.
                MusicDiagnostics.SameTrack(music);
            }
        }

        public void UpdateCurrentMusicVolume(bool isLogin = false)
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    float volume;

                    if (isLogin)
                    {
                        volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
                    }
                    else
                    {
                        Profile currentProfile = ProfileManager.CurrentProfile;

                        volume = currentProfile == null || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                    }


                    if (volume < -1 || volume > 1f)
                    {
                        return;
                    }

                    _currentMusic[i].Volume = i == 0 && _currentMusic[1] != null ? 0 : volume;
                }
            }
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            float volume = currentProfile == null || !currentProfile.EnableSound ? 0 : currentProfile.SoundVolume / SOUND_DELTA;

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            for (LinkedListNode<UOSound> soundNode = _currentSounds.First; soundNode != null; soundNode = soundNode.Next)
            {
                soundNode.Value.Volume = volume;
            }
        }

        public void StopMusic()
        {
            if (_currentMusic[0] != null || _currentMusic[1] != null)
            {
                MusicDiagnostics.Stopped(_currentMusic[0]?.Index ?? _currentMusic[1]?.Index ?? -1);
            }

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    _currentMusic[i].Stop();
                    _currentMusic[i].Dispose();
                    _currentMusic[i] = null;
                }
            }
        }

        /// <summary>
        /// The server has told us the current region has no music. Stops playback and
        /// forgets which track was playing, so that toggling war mode afterwards -
        /// StopWarMusic resumes _currentMusicIndices[0] - cannot bring the old town
        /// music back while the player is stood out in the wilderness.
        /// </summary>
        public void StopMusicFromServer()
        {
            StopMusic();

            _currentMusicIndices[0] = -1;
            _currentMusicIndices[1] = -1;

            // The server has nothing for this area, so the music map gets its turn.
            // Until the server names a real track again, position drives the music.
            _musicMapDriving = true;
            _lastMusicBlock = int.MinValue;

            ApplyMusicMap();
        }

        /// <summary>
        /// The server named a real track, so it takes the music back. Called from the
        /// packet handler rather than from PlayMusic, because the music map plays
        /// through PlayMusic too and must not switch itself off doing so.
        /// </summary>
        public void NotifyServerTrack()
        {
            _musicMapDriving = false;
        }

        // True while the server has told us the current area has no music of its own.
        // Cleared the moment it names a real track again - the server always wins.
        private bool _musicMapDriving;
        private int _lastMusicBlock = int.MinValue;

        /// <summary>
        /// Looks up where the player is and plays whatever the music map says belongs
        /// there. Nothing covering the spot means silence, which is the same answer
        /// the 1998 server gave for an unpainted block.
        /// </summary>
        private void ApplyMusicMap()
        {
            if (Settings.GlobalSettings.MusicMapMode <= 0 || World.Player == null)
            {
                return;
            }

            MusicMapManager.Load();

            _lastMusicBlock = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);

            if (MusicMapManager.TryGetTrack(World.MapIndex, World.Player.X, World.Player.Y, World.Player.Z, out int track, out string areaName))
            {
                MusicDiagnostics.MapHit(track, areaName);

                PlayMusic(track);
            }
            else
            {
                MusicDiagnostics.MapMiss();
            }
        }

        /// <summary>
        /// Re-checks the map when the player crosses into a different 8x8 block, and
        /// only acts when the answer has actually changed - the rule the 1998 server
        /// used. A non-looping track that has run out is left alone: silence until the
        /// area changes is the intended behaviour, not a gap to fill.
        /// </summary>
        private void UpdateMusicMap()
        {
            if (!_musicMapDriving || Settings.GlobalSettings.MusicMapMode <= 0 || World.Player == null)
            {
                return;
            }

            int block = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);

            if (block == _lastMusicBlock)
            {
                return;
            }

            _lastMusicBlock = block;

            MusicMapManager.Load();

            if (MusicMapManager.TryGetTrack(World.MapIndex, World.Player.X, World.Player.Y, World.Player.Z, out int track, out string areaName))
            {
                if (track != _currentMusicIndices[0])
                {
                    MusicDiagnostics.MapHit(track, areaName);

                    PlayMusic(track);
                }
            }
            else if (_currentMusicIndices[0] >= 0)
            {
                // Walked out of a mapped area into one with nothing.
                MusicDiagnostics.MapMiss();

                StopMusic();

                _currentMusicIndices[0] = -1;
            }
        }

        /// <summary>
        /// Leaving war mode. Stopping the combat track was only ever a side effect of
        /// restarting the region track - PlayMusic calls StopMusic on its way to
        /// starting it - so where the server has said this region has no music there
        /// was nothing to restart, nothing stopped it, and the combat loop ran on to
        /// its end. The war slot is stopped directly instead.
        /// </summary>
        public void StopWarMusic()
        {
            if (_currentMusic[1] != null)
            {
                MusicDiagnostics.Stopped(_currentMusic[1].Index);

                _currentMusic[1].Stop();
                _currentMusic[1].Dispose();
                _currentMusic[1] = null;
            }

            // Only bring the region track back if there is one. -1 means the server
            // told us this region has none, and restarting the last town track out in
            // the open was the other half of the same bug.
            if (_currentMusicIndices[0] >= 0)
            {
                PlayMusic(_currentMusicIndices[0]);
            }
        }

        public void StopSounds()
        {
            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                first.Value.Stop();

                _currentSounds.Remove(first);

                first = next;
            }
        }

        public void Update()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            MusicDiagnostics.CheckMapChange();
            MusicDiagnostics.CheckZone();

            UpdateMusicMap();

            bool runninWarMusic = _currentMusic[1] != null;
            Profile currentProfile = ProfileManager.CurrentProfile;

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && currentProfile != null)
                {
                    if (Client.Game.IsActive)
                    {
                        if (!currentProfile.ReproduceSoundsInBackground)
                        {
                            _currentMusic[i].Volume = i == 0 && runninWarMusic || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                        }
                    }
                    else if (!currentProfile.ReproduceSoundsInBackground && _currentMusic[i].Volume != 0.0f)
                    {
                        _currentMusic[i].Volume = 0;
                    }
                }

                _currentMusic[i]?.Update();
            }


            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                if (!first.Value.IsPlaying(Time.Ticks))
                {
                    first.Value.Stop();
                    _currentSounds.Remove(first);
                }

                first = next;
            }
        }

        public UOMusic GetCurrentMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && _currentMusic[i].IsPlaying(Time.Ticks))
                {
                    return _currentMusic[i];
                }
            }
            return null;
        }
    }
}