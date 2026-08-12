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

            UOMusic.Ended = m =>
            {
                MusicDiagnostics.Ended(m);

                OnMusicTrackEnded(m);
            };
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

            // Only what is actually loaded gets restarted. The index outlives the
            // track - nothing clears it when playback stops - so on a change of era in
            // silence this used to resurrect whatever played last, which after walking
            // in from the login screen is the login music.
            bool wasPlaying = _currentMusic[0] != null || _currentMusic[1] != null;

            StopMusic();

            EnsureMusicEraApplied();

            if (wasPlaying && index >= 0)
            {
                PlayMusic(index, warMode);
            }
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
        /// The server has told us the current region has no music, so the map gets its
        /// turn. Whatever was playing stops - unless the map was going to ask for that
        /// very track anyway, in which case stopping it only to start it again from the
        /// beginning is a cut for no reason. Stepping in and out of the blessed area at
        /// the Britain bank produces a stop packet every few seconds, and that restart
        /// was the whole of the problem.
        ///
        /// keepPlaying is the "ignore the stop packet" option, and means exactly that:
        /// the server does not get to cut the track short. It does not mean the map
        /// stops working - the map still takes over, and still changes the track at the
        /// next area boundary or when it runs out, whichever the mode calls for.
        /// </summary>
        public void StopMusicFromServer(bool keepPlaying = false)
        {
            int playing = _currentMusicIndices[0];

            // A track that repeats is always still going. One that does not is only
            // still going if it is ours and we have not been told it ran out - the
            // slot keeps hold of a finished track, so its presence proves nothing.
            bool stillGoing = _currentMusic[0] != null
                              && (_currentMusic[0].IsLooping || (playing == _mapPlayedTrack && _mapTrackRunning));

            _currentMusicIndices[1] = -1;

            // The server has nothing for this area, so the music map gets its turn.
            // Until the server names a real track again, position drives the music.
            _musicMapDriving = true;
            _lastMusicBlock = int.MinValue;

            if (Settings.GlobalSettings.MusicMapMode > MAP_OFF && World.Player != null && MapCoversHere())
            {
                _lastMusicBlock = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);

                // Two reasons to let the track carry on: the option says the server
                // may not cut it, or the map was about to ask for it anyway.
                bool keep = stillGoing;

                if (keep && !keepPlaying)
                {
                    keep = TryResolve(out int wanted, out _) && wanted == playing;
                }

                if (keep)
                {
                    // The map takes ownership of it from here, so it still changes at
                    // the next area boundary - it just is not cut short to get there.
                    MusicDiagnostics.Kept(playing, keepPlaying ? "stop_ignored" : "same_track");

                    _mapPlayedTrack = playing;
                    _mapTrackEnded = false;
                    _mapTrackRunning = true;
                    _mapTrackLoops = _currentMusic[0].IsLooping;

                    return;
                }
            }
            else if (keepPlaying && stillGoing)
            {
                // No map to hand over to - with it off, or on a facet it has no data
                // for, ignoring the stop just means the track carries on.
                MusicDiagnostics.Kept(playing, "stop_ignored");

                return;
            }

            StopMusic();

            _currentMusicIndices[0] = -1;
            _mapPlayedTrack = -1;
            _mapTrackRunning = false;

            ApplyMusicMap();
        }

        /// <summary>
        /// Whether the season packet - the only thing that plays music when the facet
        /// changes - is allowed to. It used to hold off whenever anything at all was
        /// playing, and the music map starting a track milliseconds before the facet
        /// change was enough to silence Tokuno for the whole visit. Music the map put
        /// there is a stand-in for exactly this, so it does not count.
        /// </summary>
        public bool CanSeasonMusicTakeOver()
        {
            UOMusic current = GetCurrentMusic();

            return current == null || current.Index == LoginMusicIndex || current.Index == _mapPlayedTrack;
        }

        /// <summary>
        /// The server named a real track, so it takes the music back. Called from the
        /// packet handler rather than from PlayMusic, because the music map plays
        /// through PlayMusic too and must not switch itself off doing so.
        /// </summary>
        public void NotifyServerTrack()
        {
            _musicMapDriving = false;
            _mapPlayedTrack = -1;
            _mapTrackEnded = false;
            _mapTrackRunning = false;
        }

        // What to do at the seams, matching Settings.MusicMapMode.
        private const int MAP_OFF = 0;
        private const int MAP_AUTHENTIC = 1;  // cut on area change, silence at the end
        private const int MAP_SEAMLESS = 2;   // let the track finish, then silence
        private const int MAP_CONTINUOUS = 3; // let the track finish, then pick again

        // True while the server has told us the current area has no music of its own.
        // Cleared the moment it names a real track again - the server always wins.
        private bool _musicMapDriving;
        private int _lastMusicBlock = int.MinValue;

        // The track the map started. Anything else in slot 0 came from the server -
        // the season packet plays music without going through the music packet, so it
        // never reaches NotifyServerTrack - and the map stands down rather than
        // talking over it. This was Zento, Umbra and Lakeshire going silent.
        private int _mapPlayedTrack = -1;

        // Set from the decoder, which is not always the main thread, so it is only
        // ever a flag; what to do about it is decided in Update.
        private volatile bool _mapTrackEnded;
        private int _endedTrack = -1;

        // Whether one of ours is still going, and whether it is the kind that ever
        // stops. Town tracks repeat for ever, so waiting for one to finish before
        // changing area would mean never changing area - those cut over regardless.
        private bool _mapTrackRunning;
        private bool _mapTrackLoops;

        /// <summary>
        /// A track has run out. Only of interest when it was one the map started -
        /// the server's tracks are the server's business.
        /// </summary>
        private void OnMusicTrackEnded(UOMusic music)
        {
            if (music != null && music.Index == _mapPlayedTrack)
            {
                _endedTrack = music.Index;
                _mapTrackEnded = true;
            }
        }

        /// <summary>
        /// Looks up where the player is and plays whatever the music map says belongs
        /// there. Nothing covering the spot means silence, which is the same answer
        /// the 1998 server gave for an unpainted block.
        /// </summary>
        private void ApplyMusicMap()
        {
            if (Settings.GlobalSettings.MusicMapMode <= MAP_OFF || World.Player == null || !MapCoversHere())
            {
                return;
            }

            _lastMusicBlock = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);
            _mapTrackEnded = false;

            if (TryResolve(out int track, out string areaName))
            {
                PlayFromMap(track, areaName);
            }
            else
            {
                MusicDiagnostics.MapMiss();
            }
        }

        /// <summary>
        /// Re-checks the map when the player crosses into a different 8x8 block, and
        /// only acts when the answer has actually changed - the rule the 1998 server
        /// used. What happens at that moment, and when a track runs out where the
        /// player is stood, is what the transition mode decides.
        /// </summary>
        private void UpdateMusicMap()
        {
            int mode = Settings.GlobalSettings.MusicMapMode;

            if (mode <= MAP_OFF || World.Player == null || !MapCoversHere())
            {
                return;
            }

            // Something in slot 0 that the map did not put there is the server's, and
            // the server always wins.
            if (_currentMusicIndices[0] >= 0 && _currentMusicIndices[0] != _mapPlayedTrack)
            {
                _musicMapDriving = false;
                _mapTrackEnded = false;
                _mapTrackRunning = false;
            }

            if (!_musicMapDriving)
            {
                return;
            }

            bool ended = _mapTrackEnded;

            if (ended)
            {
                _mapTrackEnded = false;
                _mapTrackRunning = false;
            }

            int block = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);
            bool moved = block != _lastMusicBlock;

            if (moved)
            {
                _lastMusicBlock = block;
            }

            if (!moved && !ended)
            {
                return;
            }

            if (moved)
            {
                // Crossed into a new block. Authentic cuts over there and then; the
                // other two let the current track finish and pick the new area up when
                // it does. Nothing of ours playing means there is nothing to wait for.
                if (mode != MAP_AUTHENTIC && _mapTrackRunning && !_mapTrackLoops)
                {
                    return;
                }

                if (TryResolve(out int track, out string areaName))
                {
                    // Comparing against the last answer, not against what is audible,
                    // so re-entering a block inside the same area after the track has
                    // run out does not start it over.
                    if (track != _currentMusicIndices[0])
                    {
                        PlayFromMap(track, areaName);
                    }
                }
                else
                {
                    NothingHere(mode, false);
                }

                return;
            }

            // A track of ours has run out and the player has not gone anywhere.
            if (mode == MAP_AUTHENTIC)
            {
                // Silence until the area changes. This is what 1998 did.
                return;
            }

            if (!TryResolve(out int nextTrack, out string nextArea))
            {
                NothingHere(mode, true);

                return;
            }

            if (mode == MAP_SEAMLESS && nextTrack == _endedTrack)
            {
                // Seamless is about not being cut off mid-track, not about music
                // without end. Same area, same track: leave it quiet.
                return;
            }

            PlayFromMap(nextTrack, nextArea);
        }

        /// <summary>
        /// Nothing in the map covers where the player is standing. Whether that means
        /// silence is the mode's decision, not the data's: the 1998 data leaves about
        /// one block in six unpainted, and treating every one of those as an order to
        /// stop punched a hole in the music every time the player clipped a corner -
        /// audible around the west edge of Britain, which the city rectangle misses by
        /// a few tiles.
        ///
        /// Authentic goes quiet, because that is what 1998 did. The other two carry on
        /// until the track finishes by itself, and the option to ignore stop packets
        /// means nothing at all is allowed to cut the music.
        /// </summary>
        private void NothingHere(int mode, bool ended)
        {
            MusicDiagnostics.MapMiss();

            if (Settings.GlobalSettings.IgnoreServerStopMusic)
            {
                return;
            }

            if (mode == MAP_AUTHENTIC || ended || !_mapTrackRunning)
            {
                StopFromMap();
            }
        }

        /// <summary>
        /// Whether the map is entitled to an opinion about where the player is. On a
        /// facet it has no data for at all it is not, and says nothing rather than
        /// answering "silence" - that answer arriving on the way into Tokuno stopped
        /// the music the server had just started there.
        /// </summary>
        private static bool MapCoversHere()
        {
            MusicMapManager.Load();

            return MusicMapManager.CoversMap(World.MapIndex);
        }

        private bool TryResolve(out int track, out string areaName)
        {
            MusicMapManager.Load();

            track = -1;

            if (!MusicMapManager.TryGetTrack(World.MapIndex, World.Player.X, World.Player.Y, World.Player.Z, out int[] tracks, out areaName))
            {
                return false;
            }

            // The area's tracks are in order of preference. An era's Config.txt need
            // not go all the way up - the 1997 one stops around 48 - so a modern index
            // like Zento's 49 may have no file under it, and asking for it anyway
            // would produce silence. Take the first the era can actually play.
            for (int i = 0; i < tracks.Length; i++)
            {
                if (HasTrack(tracks[i]))
                {
                    track = tracks[i];

                    return true;
                }
            }

            return false;
        }

        private static bool HasTrack(int track)
        {
            EnsureMusicEraApplied();

            return Client.Game.Sounds.GetMusic(track) != null;
        }

        private void PlayFromMap(int track, string areaName)
        {
            MusicDiagnostics.MapHit(track, areaName);

            // Playing the same index again is a no-op inside PlayMusic - the cached
            // track object is still the current one even after it has run out - so it
            // is cleared out of the way first.
            if (_currentMusicIndices[0] == track)
            {
                StopMusic();
            }

            _mapPlayedTrack = track;

            PlayMusic(track);

            // Music turned off, or the file is missing: nothing started, so nothing is
            // going to report that it finished either.
            _mapTrackRunning = _currentMusic[0] != null;
            _mapTrackLoops = _mapTrackRunning && _currentMusic[0].IsLooping;
        }

        private void StopFromMap()
        {
            if (_currentMusicIndices[0] < 0 && _mapPlayedTrack < 0)
            {
                return;
            }

            StopMusic();

            _currentMusicIndices[0] = -1;
            _mapPlayedTrack = -1;
            _mapTrackRunning = false;
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