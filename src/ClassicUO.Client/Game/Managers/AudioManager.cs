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

            MusicDiagnostics.EraChanged(era);
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

            // Only something actually sounding gets restarted. The index outlives the
            // track - nothing clears it when playback stops - so on a change of era in
            // silence this used to resurrect whatever played last, which after walking
            // in from the login screen is the login music.
            bool wasPlaying = StillGoing() || (_currentMusic[1] != null && _currentMusic[1].IsStreaming);

            bool wasOurs = index >= 0 && index == _mapPlayedTrack;

            StopMusic();

            EnsureMusicEraApplied();

            if (!wasPlaying || index < 0)
            {
                return;
            }

            PlayMusic(index, warMode);

            // The map keeps hold of it across the rebuild, so it still changes at the
            // next area boundary rather than being left as somebody else's track.
            if (wasOurs && !warMode && StillGoing())
            {
                AdoptAsMapTrack(index);
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

                if (_currentMusic[idx].IsStreaming)
                {
                    MusicDiagnostics.Started(
                        music,
                        _currentMusic[idx].IsLooping,
                        is_login ? "login" : iswarmode ? "warmode" : "",
                        _currentMusic[idx].Path
                    );
                }
                else
                {
                    // The decoder could not open the file. It says nothing about it,
                    // which is heard as a moment of audio and then silence.
                    MusicDiagnostics.StartFailed(music, _currentMusic[idx].Path);
                }
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
        /// The server has told us the current region has no music. Everything stops,
        /// and the map fills the silence on the next frame - unless the track playing
        /// is the one the map would ask for anyway, in which case stopping it only to
        /// start it again from the beginning is a cut for no reason. Stepping in and
        /// out of the blessed area at the Britain bank produces a stop packet every
        /// few seconds, and that restart was the whole of the problem.
        ///
        /// keepPlaying is the "ignore the stop packet" option and means exactly that:
        /// the server does not get to cut a track short. Any track, from anywhere - a
        /// track that does not repeat was exactly the kind still being cut, which is
        /// what made the option look like it did nothing.
        /// </summary>
        public void StopMusicFromServer(bool keepPlaying = false)
        {
            int playing = _currentMusicIndices[0];

            _lastServerIndex = Constants.MUSIC_STOP_INDEX;
            _lastServerAt = Time.Ticks;

            _currentMusicIndices[1] = -1;

            bool keep = playing >= 0 && StillGoing()
                        && (keepPlaying || (MapIsOn() && TryResolve(out int wanted, out _) && wanted == playing));

            if (keep)
            {
                // Adopted, so the map changes it at the next area boundary or when it
                // runs out - it just is not cut short to get there.
                MusicDiagnostics.Kept(playing, keepPlaying ? "stop_ignored" : "same_track");

                AdoptAsMapTrack(playing);

                return;
            }

            StopMusic();

            _currentMusicIndices[0] = -1;

            ForgetMapTrack();
        }

        /// <summary>
        /// Whether the region track is still audible. UOMusic knows this for certain -
        /// its decoder flag is cleared when a track is stopped or runs out - whereas
        /// Sound.IsPlaying only knows about the last buffer submitted, about a second
        /// of audio, and so reports "not playing" over almost any track that is.
        /// </summary>
        private bool StillGoing()
        {
            return _currentMusic[0] != null && _currentMusic[0].IsStreaming;
        }

        /// <summary>
        /// Whether the season packet - which some callers still use to set music, such
        /// as the death screen - is allowed to. Music the map put there does not count
        /// as something else playing; it is a stand-in for exactly this.
        /// </summary>
        public bool CanSeasonMusicTakeOver()
        {
            if (!StillGoing())
            {
                return true;
            }

            int playing = _currentMusicIndices[0];

            return playing == LoginMusicIndex || playing == _mapPlayedTrack;
        }

        /// <summary>
        /// The server named a real track, so it owns the music. The map lets go of
        /// whatever it was holding; from here it only fills silence.
        /// </summary>
        public void NotifyServerTrack(int index)
        {
            _lastServerIndex = index;
            _lastServerAt = Time.Ticks;

            ForgetMapTrack();
        }

        // What the server last asked for, and when. -1 before it has said anything,
        // and Constants.MUSIC_STOP_INDEX when what it said was "this region has none".
        private int _lastServerIndex = -1;
        private uint _lastServerAt;

        /// <summary>
        /// Everything the music system is doing right now, for the overlay. A snapshot
        /// rather than live properties, so what is drawn is self-consistent.
        /// </summary>
        public struct MusicStatus
        {
            public int Index;          // what is loaded in the region slot, -1 for none
            public string File;        // the file it resolved to, era folder included
            public bool Loops;
            public bool Playing;       // actually sounding, not merely loaded
            public bool FromMap;       // the map put it there rather than the server
            public int WarIndex;       // combat track over the top, -1 for none

            public int ServerIndex;    // the last thing the server asked for
            public int ServerSecs;     // how long ago

            public bool MapHasAnswer;  // what the map says belongs where you stand
            public int MapTrack;
            public string MapArea;

            public string Era;
            public int Mode;
            public bool IgnoreStop;
        }

        public MusicStatus GetMusicStatus()
        {
            MusicStatus st = new MusicStatus
            {
                Index = _currentMusic[0] == null ? -1 : _currentMusicIndices[0],
                File = _currentMusic[0]?.Path,
                Loops = _currentMusic[0] != null && _currentMusic[0].IsLooping,
                Playing = StillGoing(),
                FromMap = _mapPlayedTrack >= 0 && _currentMusicIndices[0] == _mapPlayedTrack,
                WarIndex = _currentMusic[1] == null ? -1 : _currentMusicIndices[1],
                ServerIndex = _lastServerIndex,
                ServerSecs = _lastServerIndex < 0 ? -1 : (int)((Time.Ticks - _lastServerAt) / 1000),
                MapTrack = -1,
                Era = Settings.GlobalSettings.MusicEra,
                Mode = Settings.GlobalSettings.MusicMapMode,
                IgnoreStop = Settings.GlobalSettings.IgnoreServerStopMusic
            };

            if (World.Player != null && MapCoversHere() && TryResolve(out int track, out string area))
            {
                st.MapHasAnswer = true;
                st.MapTrack = track;
                st.MapArea = area;
            }

            return st;
        }

        /// <summary>
        /// Everything the music system remembers, dropped. Called when leaving the
        /// world, because none of it means anything in the next session - the track
        /// index outliving the track is what used to bring the login music back.
        /// </summary>
        public void ForgetMusicState()
        {
            _currentMusicIndices[0] = -1;
            _currentMusicIndices[1] = -1;

            ForgetMapTrack();
        }

        // What to do at the seams, matching Settings.MusicMapMode.
        private const int MAP_OFF = 0;
        private const int MAP_AUTHENTIC = 1;  // cut on area change, silence at the end
        private const int MAP_SEAMLESS = 2;   // let the track finish, then silence
        private const int MAP_CONTINUOUS = 3; // let the track finish, then pick again

        // The track the map started, or adopted. Anything else in slot 0 belongs to
        // the server, and the map stays out of its way while it is sounding.
        private int _mapPlayedTrack = -1;
        private bool _mapTrackLoops;

        // The block and facet the map last gave an answer for, and whether that
        // answer was silence. Without the last one, standing still in an area the map
        // has nothing for would re-ask - and re-log - every frame.
        private int _lastMusicBlock = int.MinValue;
        private int _lastMusicMap = int.MinValue;
        private bool _mapChoseSilence;

        // The area the map last went quiet in. Silence is decided per block, but it is
        // only news once per area - walking around Umbra after its track ran out wrote
        // twenty identical pairs of lines saying nothing had changed.
        private string _silentArea;

        // Set from the decoder, which is not always the main thread, so it is only
        // ever a flag; what to do about it is decided in Update.
        private volatile bool _mapTrackEnded;
        private int _endedTrack = -1;

        /// <summary>
        /// A track has run out. Only of interest when it was one the map owns - the
        /// server's tracks are the server's business.
        /// </summary>
        private void OnMusicTrackEnded(UOMusic music)
        {
            if (music != null && music.Index == _mapPlayedTrack)
            {
                _endedTrack = music.Index;
                _mapTrackEnded = true;
            }
        }

        private void AdoptAsMapTrack(int track)
        {
            _mapPlayedTrack = track;
            _mapTrackLoops = _currentMusic[0] != null && _currentMusic[0].IsLooping;
            _mapTrackEnded = false;
            _mapChoseSilence = false;
            _lastMusicBlock = World.Player == null ? int.MinValue : MusicMapManager.BlockOf(World.Player.X, World.Player.Y);
            _lastMusicMap = World.MapIndex;
        }

        private void ForgetMapTrack()
        {
            _mapPlayedTrack = -1;
            _mapTrackLoops = false;
            _mapTrackEnded = false;
            _mapChoseSilence = false;
            _lastMusicBlock = int.MinValue;
            _lastMusicMap = int.MinValue;
            _silentArea = null;
        }

        private static bool MapIsOn()
        {
            return Settings.GlobalSettings.MusicMapMode > MAP_OFF && World.Player != null && MapCoversHere();
        }

        /// <summary>
        /// The whole of the map's behaviour, and it is one rule: the server wins while
        /// its track is actually sounding, and the map fills the silence otherwise.
        ///
        /// This replaces a permission flag that was set only by a stop packet and
        /// cleared by any server track. Every way the music got stuck was that flag in
        /// the wrong position - ignoring stop packets meant it never got set at all, a
        /// server track that ran out left it clear with nothing able to help, and it
        /// carried over stale across facet changes and relogs.
        /// </summary>
        private void UpdateMusicMap()
        {
            int mode = Settings.GlobalSettings.MusicMapMode;

            if (mode <= MAP_OFF || World.Player == null || !MapCoversHere())
            {
                return;
            }

            bool sounding = StillGoing();
            bool ours = _mapPlayedTrack >= 0 && _currentMusicIndices[0] == _mapPlayedTrack;

            // Somebody else is playing. Not our business until it stops.
            if (sounding && !ours)
            {
                ForgetMapTrack();

                return;
            }

            bool ended = _mapTrackEnded;

            if (ended)
            {
                _mapTrackEnded = false;
            }

            if (ours && !sounding)
            {
                // Ran out without the hook firing - a stop from elsewhere, or a file
                // that failed to open. Same thing as far as the next decision goes.
                ended = true;
            }

            int block = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);

            // A facet change is not the same area continuing. Arriving in Umbra with
            // Ilshenar's jungle track still running kept it playing to the end, and
            // Umbra never got its own music - so this counts as a move whatever the
            // block works out to, and overrides letting the track finish.
            bool facetChanged = World.MapIndex != _lastMusicMap;
            bool moved = facetChanged || block != _lastMusicBlock;

            if (moved)
            {
                _lastMusicBlock = block;
                _lastMusicMap = World.MapIndex;
                _mapChoseSilence = false;
            }

            if (sounding)
            {
                // Ours, and still going. Only an area change is worth acting on.
                if (!moved)
                {
                    return;
                }

                // Seamless and continuous let a track finish before changing area.
                // A track that repeats never finishes, so those cut over regardless -
                // waiting for a town track to end would mean never leaving town - and
                // so does a facet change, which is a different world, not a boundary.
                if (mode != MAP_AUTHENTIC && !_mapTrackLoops && !facetChanged)
                {
                    MusicDiagnostics.MapWait(_mapPlayedTrack, "track_unfinished");

                    return;
                }

                if (!TryResolve(out int next, out string nextArea))
                {
                    NothingHere(mode);

                    return;
                }

                if (next != _mapPlayedTrack)
                {
                    PlayFromMap(next, nextArea);
                }

                return;
            }

            // Silence. Fill it, unless the silence is the answer we already gave for
            // this spot, or the mode wants it.
            if (_mapChoseSilence)
            {
                return;
            }

            if (!TryResolve(out int track, out string areaName))
            {
                NothingHere(mode);

                return;
            }

            if (ended && mode != MAP_CONTINUOUS && track == _endedTrack)
            {
                // The track that just ran out here is the same one this area asks for.
                // Authentic and seamless both go quiet rather than loop it round; only
                // continuous plays it again.
                if (!string.Equals(areaName, _silentArea, StringComparison.Ordinal))
                {
                    MusicDiagnostics.MapLook(mode, true);
                    MusicDiagnostics.MapSilent("track_ended_same_area " + (areaName ?? ""));

                    _silentArea = areaName;
                }

                _mapChoseSilence = true;

                return;
            }

            MusicDiagnostics.MapLook(mode, ended);

            _silentArea = null;

            PlayFromMap(track, areaName);
        }

        /// <summary>
        /// Nothing in the map covers where the player is. Whether that means silence is
        /// the mode's decision, not the data's: the 1998 data leaves about one block in
        /// six unpainted, and treating every one of those as an order to stop punched a
        /// hole in the music whenever the player clipped a corner - audible along the
        /// west edge of Britain, which the city rectangle misses by a few tiles.
        /// </summary>
        private void NothingHere(int mode)
        {
            MusicDiagnostics.MapMiss();

            _mapChoseSilence = true;

            // The option says nothing may cut the music, and that includes the map.
            if (Settings.GlobalSettings.IgnoreServerStopMusic)
            {
                return;
            }

            // Authentic goes quiet the moment the area runs out, which is what 1998
            // did. The other two leave a track alone until it finishes by itself.
            if (mode == MAP_AUTHENTIC || !StillGoing())
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
            // may have no file under it, and asking for it anyway would be silence.
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

            PlayMusic(track);

            // Music turned off, or the file missing: nothing started, so nothing will
            // report that it finished either. Do not claim it as ours in that case.
            if (StillGoing())
            {
                AdoptAsMapTrack(track);
            }
            else
            {
                ForgetMapTrack();

                _mapChoseSilence = true;
                _lastMusicBlock = MusicMapManager.BlockOf(World.Player.X, World.Player.Y);
            }
        }

        private void StopFromMap()
        {
            if (_currentMusicIndices[0] < 0 && _mapPlayedTrack < 0)
            {
                return;
            }

            StopMusic();

            _currentMusicIndices[0] = -1;

            ForgetMapTrack();

            _mapChoseSilence = true;
            _lastMusicBlock = World.Player == null ? int.MinValue : MusicMapManager.BlockOf(World.Player.X, World.Player.Y);
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
            // Nothing to leave. OpenStatusBar calls this every time the status bar is
            // opened, and without this guard that restarted the region track from the
            // beginning each time.
            if (_currentMusic[1] == null)
            {
                return;
            }

            MusicDiagnostics.Stopped(_currentMusic[1].Index);

            _currentMusic[1].Stop();
            _currentMusic[1].Dispose();
            _currentMusic[1] = null;

            // Only bring the region track back if there is one, and only if it is not
            // still going underneath - the region slot keeps playing at zero volume
            // while war music is up, so usually there is nothing to restart. -1 means
            // the server said this region has none, and restarting the last town track
            // out in the open was the other half of the same bug.
            if (_currentMusicIndices[0] >= 0 && !StillGoing())
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