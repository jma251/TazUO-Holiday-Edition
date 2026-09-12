using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace ClassicUO.Game.Managers
{
    using System.Text.Json.Serialization;
    using ClassicUO.Utility.Logging;

    [JsonSerializable(typeof(SpellVisualRangeManager.SpellRangeInfo))]
    [JsonSerializable(typeof(SpellVisualRangeManager.SpellRangeInfo[]))]
    public partial class SpellVisualRangeJsonContext : JsonSerializerContext
    {
    }

    public class SpellVisualRangeManager
    {
        public static SpellVisualRangeManager Instance => instance ??= new SpellVisualRangeManager();

        public Vector2 LastCursorTileLoc { get; set; } = Vector2.Zero;
        public DateTime LastSpellTime { get; private set; } = DateTime.Now;
        public Dictionary<int, SpellRangeInfo> SpellRangeCache => spellRangeCache;

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath ?? "", "Data", "Profiles", "SpellVisualRange.json");
        private string overridePath = Path.Combine(ProfileManager.ProfilePath ?? "", "SpellVisualRange.json");

        private Dictionary<int, SpellRangeInfo> spellRangeCache = new Dictionary<int, SpellRangeInfo>();
        private Dictionary<int, SpellRangeInfo> spellRangeOverrideCache = new Dictionary<int, SpellRangeInfo>();
        private Dictionary<string, SpellRangeInfo> spellRangePowerWordCache = new Dictionary<string, SpellRangeInfo>();

        private bool loaded = false;
        private static SpellVisualRangeManager instance;

        private bool isCasting { get; set; } = false;
        private SpellRangeInfo currentSpell { get; set; }

        //Taken from Dust client
        private static readonly int[] stopAtClilocs = new int[]
        {
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            502644,     // You have not yet recovered from casting a spell.
            1072060,    // You cannot cast a spell while calmed.
        };

        private SpellVisualRangeManager()
        {
            Load();
        }

        private void OnRawMessageReceived(object sender, MessageEventArgs e)
        {
            Task.Run(() =>
            {
                if (loaded && e.Parent != null && ReferenceEquals(e.Parent, World.Player))
                {
                    if (spellRangePowerWordCache.TryGetValue(e.Text.Trim(), out SpellRangeInfo spell))
                    {
                        SetCasting(spell);
                    }
                }
            });
        }

        public void OnClilocReceived(int cliloc)
        {
            Task.Factory.StartNew(() =>
            {
                if (isCasting && stopAtClilocs.Contains(cliloc))
                {
                    ClearCasting();
                }
            });
        }

        private void SetCasting(SpellRangeInfo spell)
        {
            LastSpellTime = DateTime.Now;
            currentSpell = spell;
            isCasting = true;

            // Mirrored onto the player so it can be read from outside this manager.
            // Guarded because nothing above this point requires a player to exist.
            if (World.Player != null)
            {
                World.Player.IsCasting = true;
            }

            if (currentSpell != null && currentSpell.FreezeCharacterWhileCasting)
            {
                World.Player.Flags |= Flags.Frozen;
            }
            EventSink.InvokeSpellCastBegin(spell.ID);
        }

        public void ClearCasting()
        {
            // Only a real transition is worth announcing: this is called from several
            // places that do not know whether a cast was live, so announcing
            // unconditionally would fire the event on clears that ended nothing.
            bool wasCasting = isCasting;
            int ended = currentSpell != null ? currentSpell.ID : -1;

            isCasting = false;
            currentSpell = null;
            LastSpellTime = DateTime.MinValue;

            if (World.Player != null)
            {
                World.Player.IsCasting = false;
            }

            World.Player.Flags &= ~Flags.Frozen;

            // The other half of SpellCastBegin. Without it the only way to know a cast is
            // over is to watch the flag or wait out the duration, which is what every
            // consumer was doing separately.
            if (wasCasting)
            {
                EventSink.InvokeSpellCastEnd(ended);
            }
        }

        /// <summary>
        /// A target cursor has arrived. For a spell that was waiting for one, that is the
        /// cast finishing - the server does announce completion, it just does it by asking
        /// where to aim rather than in words.
        ///
        /// Only for spells marked ExpectTargetCursor, which is most of them: eighty-two of
        /// the hundred and thirty-five in the shipped data. A cursor arriving for anything
        /// else - a skill, a context menu, a tool - is not this cast ending and is ignored.
        /// </summary>
        public void OnTargetCursorReceived()
        {
            if (isCasting && currentSpell != null && currentSpell.ExpectTargetCursor)
            {
                ClearCasting();
            }
        }

        /// <summary>
        /// Ends a cast nothing was ever said about.
        ///
        /// The stop clilocs cover a cast the server refused or disturbed, and the target
        /// cursor covers one that finished and wants aiming. What is left is a spell that
        /// needs no target and simply worked - it has no signal at all, so it is let go
        /// after the spell's own MaxDuration.
        ///
        /// That field is what the data uses to say how long to allow, it is per spell and
        /// editable, and no arithmetic is done on it here. Faster Casting is not applied:
        /// this errs late, which is the right way round for a backstop, and precise timing
        /// belongs to whoever is reading the state.
        ///
        /// Called once a tick from World.Update. One comparison unless a cast is live.
        /// </summary>
        public void CheckCastExpiry()
        {
            if (!isCasting || currentSpell == null)
            {
                return;
            }

            if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) <= DateTime.Now)
            {
                ClearCasting();
            }
        }

        public SpellRangeInfo GetCurrentSpell()
        {
            return currentSpell;
        }

        #region Load and unload
        public void OnSceneLoad()
        {
            EventSink.RawMessageReceived += OnRawMessageReceived;
        }

        public void OnSceneUnload()
        {
            EventSink.RawMessageReceived -= OnRawMessageReceived;
            instance = null;
        }
        #endregion

        public bool IsTargetingAfterCasting()
        {
            if (!loaded || currentSpell == null || !isCasting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators)
            {
                return false;
            }

            if (TargetManager.IsTargeting || (currentSpell.ShowCastRangeDuringCasting && IsCastingWithoutTarget()))
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCastingWithoutTarget()
        {
            if (!loaded || currentSpell == null || !isCasting || currentSpell.CastTime <= 0 || TargetManager.IsTargeting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators)
            {
                return false;
            }

            if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.CastTime) > DateTime.Now)
                {
                    return true;
                }
                else if (currentSpell.FreezeCharacterWhileCasting)
                {
                    World.Player.Flags &= ~Flags.Frozen;
                }
            }
            else if (currentSpell.FreezeCharacterWhileCasting)
            {
                World.Player.Flags &= ~Flags.Frozen;
            }

            return false;
        }

        public ushort ProcessHueForTile(ushort hue, GameObject o)
        {
            if (!loaded || currentSpell == null) { return hue; }

            if (currentSpell.CastRange > 0 && o.Distance <= currentSpell.CastRange)
            {
                hue = currentSpell.Hue;
            }

            int cDistance = o.DistanceFrom(LastCursorTileLoc);

            if (currentSpell.CursorSize > 0 && cDistance < currentSpell.CursorSize)
            {
                if (currentSpell.IsLinear)
                {
                    if (GetDirection(new Vector2(World.Player.X, World.Player.Y), LastCursorTileLoc) == SpellDirection.EastWest)
                    { //X
                        if (o.Y == LastCursorTileLoc.Y)
                        {
                            hue = currentSpell.CursorHue;
                        }
                    }
                    else
                    { //Y
                        if (o.X == LastCursorTileLoc.X)
                        {
                            hue = currentSpell.CursorHue;
                        }
                    }
                }
                else
                {
                    hue = currentSpell.CursorHue;
                }
            }

            return hue;
        }

        private static SpellDirection GetDirection(Vector2 from, Vector2 to)
        {
            int dx = (int)(from.X - to.X);
            int dy = (int)(from.Y - to.Y);
            int rx = (dx - dy) * 44;
            int ry = (dx + dy) * 44;

            if (rx >= 0 && ry >= 0)
            {
                return SpellDirection.SouthNorth;
            }
            else if (rx >= 0)
            {
                return SpellDirection.EastWest;
            }
            else if (ry >= 0)
            {
                return SpellDirection.EastWest;
            }
            else
            {
                return SpellDirection.SouthNorth;
            }
        }

        #region Save and load
        private Timer saveTimer;
        private readonly object saveLock = new object();
        private volatile bool hasPendingChanges = false;
        private void Load()
        {
            spellRangeCache.Clear();
            Task.Factory.StartNew(() =>
            {
                if (!File.Exists(savePath))
                {
                    //CreateAndLoadDataFile();
                    var assembly = GetType().Assembly;

                    var resourceName = assembly.GetName().Name + ".Game.Managers.DefaultSpellIndicatorConfig.json";
                    try
                    {
                        using Stream stream = assembly.GetManifestResourceStream(resourceName);

                        using StreamReader reader = new StreamReader(stream);

                        LoadFromString(reader.ReadToEnd());
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                        CreateAndLoadDataFile();
                    }

                    AfterLoad();
                    loaded = true;
                    Save();
                }
                else
                {
                    try
                    {
                        string data = File.ReadAllText(savePath);
                        SpellRangeInfo[] fileData = JsonSerializer.Deserialize(data, SpellVisualRangeJsonContext.Default.SpellRangeInfoArray);

                        foreach (var entry in fileData)
                        {
                            spellRangeCache.Add(entry.ID, entry);
                        }
                        AfterLoad();
                        loaded = true;
                    }
                    catch
                    {
                        CreateAndLoadDataFile();
                        AfterLoad();
                        loaded = true;
                    }

                }
            });
        }

        private void LoadOverrides()
        {
            spellRangeOverrideCache.Clear();

            if (File.Exists(overridePath))
            {
                try
                {
                    string data = File.ReadAllText(overridePath);
                    SpellRangeInfo[] fileData = JsonSerializer.Deserialize<SpellRangeInfo[]>(data);

                    foreach (var entry in fileData)
                    {
                        spellRangeOverrideCache.Add(entry.ID, entry);
                    }

                    foreach (var entry in spellRangeOverrideCache.Values)
                    {
                        if (string.IsNullOrEmpty(entry.PowerWords))
                        {
                            SpellDefinition spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                            if (spellD == SpellDefinition.EmptySpell)
                            {
                                SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);
                            }

                            if (spellD != SpellDefinition.EmptySpell)
                            {
                                entry.PowerWords = spellD.PowerWords;
                            }
                        }
                        if (!string.IsNullOrEmpty(entry.PowerWords))
                        {
                            if (spellRangePowerWordCache.ContainsKey(entry.PowerWords))
                            {
                                spellRangePowerWordCache[entry.PowerWords] = entry;
                            }
                            else
                            {
                                spellRangePowerWordCache.Add(entry.PowerWords, entry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public bool LoadFromString(string json)
        {
            try
            {
                SpellRangeInfo[] fileData = JsonSerializer.Deserialize<SpellRangeInfo[]>(json);

                loaded = false;
                spellRangeCache.Clear();

                foreach (var entry in fileData)
                {
                    spellRangeCache.Add(entry.ID, entry);
                }
                AfterLoad();
                LoadOverrides();
                loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                loaded = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Fills in timings for spells that have none.
        ///
        /// The spell definitions the cache is built from carry no timing at all - no cast
        /// time, no recovery, nothing about Faster Casting - so every entry used to default
        /// to a cast time of zero and a flat ten second ceiling. That is enough for an
        /// indicator and not enough to say when a cast is over.
        ///
        /// Only empty values are filled. A spell whose cast time has been set by hand, in
        /// the saved file or the in-game editor, keeps it: this seeds, it does not overwrite.
        /// That also means an existing profile picks the timings up rather than having to be
        /// deleted.
        /// </summary>
        private void SeedMissingTimings()
        {
            try
            {
                using (Stream stream = typeof(SpellVisualRangeManager).Assembly
                    .GetManifestResourceStream("ClassicUO.Game.Managers.DefaultSpellIndicatorConfig.json"))
                {
                    if (stream == null)
                    {
                        Log.Warn("Spell timing defaults are missing from the assembly; cast times stay unset.");

                        return;
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        SpellRangeInfo[] defaults = JsonSerializer.Deserialize(
                            reader.ReadToEnd(),
                            typeof(SpellRangeInfo[]),
                            SpellVisualRangeJsonContext.Default) as SpellRangeInfo[];

                        if (defaults == null)
                        {
                            return;
                        }

                        foreach (SpellRangeInfo d in defaults)
                        {
                            if (!spellRangeCache.TryGetValue(d.ID, out SpellRangeInfo entry))
                            {
                                continue;
                            }

                            if (entry.CastTime <= 0)
                            {
                                entry.CastTime = d.CastTime;
                            }

                            if (entry.RecoveryTime <= 0)
                            {
                                entry.RecoveryTime = d.RecoveryTime;
                            }

                            if (entry.MaxFasterCasting <= 0)
                            {
                                entry.MaxFasterCasting = d.MaxFasterCasting;
                            }

                            if (entry.MaxFasterCastRecovery <= 0)
                            {
                                entry.MaxFasterCastRecovery = d.MaxFasterCastRecovery;
                            }

                            if (string.IsNullOrEmpty(entry.School))
                            {
                                entry.School = d.School;
                            }

                            if (!entry.CapChivalryFasterCasting.HasValue)
                            {
                                entry.CapChivalryFasterCasting = d.CapChivalryFasterCasting;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // A missing or malformed defaults file must not stop the client. Without it
                // every spell simply falls back to MaxDuration, which is where it was before.
                Log.Warn($"Could not seed spell timings: {e.Message}");
            }
        }

        private void AfterLoad()
        {
            SeedMissingTimings();

            spellRangePowerWordCache.Clear();
            foreach (var entry in spellRangeCache.Values)
            {
                if (string.IsNullOrEmpty(entry.PowerWords))
                {
                    SpellDefinition spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                    if (spellD == SpellDefinition.EmptySpell)
                    {
                        SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);
                    }

                    if (spellD != SpellDefinition.EmptySpell)
                    {
                        entry.PowerWords = spellD.PowerWords;
                    }
                }
                if (!string.IsNullOrEmpty(entry.PowerWords))
                {
                    spellRangePowerWordCache.Add(entry.PowerWords, entry);
                }
            }
            LoadOverrides();
        }

        private void CreateAndLoadDataFile()
        {
            foreach (var entry in SpellsMagery.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsNecromancy.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsChivalry.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsBushido.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsNinjitsu.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsSpellweaving.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsMysticism.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsMastery.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }

            Task.Factory.StartNew(() =>
            {
                Save();
            });
        }

        public void DelayedSave()
        {
            lock (saveLock)
            {
                hasPendingChanges = true;

                // Cancel existing timer if it's running
                saveTimer?.Dispose();

                saveTimer = new Timer();
                saveTimer.Interval = 500;
                saveTimer.Elapsed += (_,_) => { PerformSave(); };
            }
        }

        private void PerformSave()
        {
            lock (saveLock)
            {
                if (!hasPendingChanges)
                    return;

                hasPendingChanges = false;
            }

            string tempPath = null;
            try
            {
                tempPath = Path.GetTempFileName();
                var options = new JsonSerializerOptions() { WriteIndented = true };
                string fileData = JsonSerializer.Serialize(spellRangeCache.Values.ToArray(), options);
                File.WriteAllText(tempPath, fileData);

                if (File.Exists(savePath))
                    File.Delete(savePath);
                File.Move(tempPath, savePath);
            }
            catch (Exception e)
            {
                Log.Error($"Save failed: {e}");
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        public void Save()
        {
            lock (saveLock)
            {
                saveTimer?.Dispose();
                if (hasPendingChanges)
                {
                    PerformSave();
                }
            }
        }
        #endregion

        private enum SpellDirection
        {
            EastWest,
            SouthNorth
        }

        public class SpellRangeInfo
        {
            public int ID { get; set; } = -1;
            public string Name { get; set; } = "";
            public string PowerWords { get; set; } = "";
            public int CursorSize { get; set; } = 0;
            public int CastRange { get; set; } = 1;
            public ushort Hue { get; set; } = 32;
            public ushort CursorHue { get; set; } = 10;
            public int MaxDuration { get; set; } = 10;
            public bool IsLinear { get; set; } = false;
            public double CastTime { get; set; } = 0.0;
            public bool ShowCastRangeDuringCasting { get; set; } = false;
            public bool FreezeCharacterWhileCasting { get; set; } = false;
            public bool ExpectTargetCursor { get; set; } = false;

            // --- Timing. Seeded from DefaultSpellIndicatorConfig.json, overridable per
            // --- spell in the saved file and in the in-game editor.

            /// <summary>Seconds before another spell may be cast, before any Faster Cast Recovery.</summary>
            public double RecoveryTime { get; set; } = 0.0;

            /// <summary>Which spell school, for reference when tuning by hand.</summary>
            public string School { get; set; } = "";

            /// <summary>
            /// Points of Faster Casting this spell's school actually counts. It is not a
            /// single number across the game - magery and necromancy stop at 2, chivalry,
            /// spellweaving and mysticism at 4 - which is why it is carried per spell
            /// rather than assumed.
            /// </summary>
            public int MaxFasterCasting { get; set; } = 0;

            /// <summary>Points of Faster Cast Recovery this spell's school counts.</summary>
            public int MaxFasterCastRecovery { get; set; } = 0;

            /// <summary>Chivalry counts Faster Casting differently; null where it does not apply.</summary>
            public bool? CapChivalryFasterCasting { get; set; }

            public static SpellRangeInfo FromSpellDef(SpellDefinition spell)
            {
                return new SpellRangeInfo() { ID = spell.ID, Name = spell.Name, PowerWords = spell.PowerWords };
            }
        }

        #region Cast Timer Bar


        public class CastTimerProgressBar : Gump
        {
            private Rectangle barBounds, barBoundsF;
            private Texture2D background;
            private Texture2D foreground;
            private Vector3 hue = ShaderHueTranslator.GetHueVector(0);


            public CastTimerProgressBar() : base(0, 0)
            {
                CanMove = false;
                AcceptMouseInput = false;
                CanCloseWithEsc = false;
                CanCloseWithRightClick = false;

                ref readonly var gi = ref Client.Game.Gumps.GetGump(0x0805);
                background = gi.Texture;
                barBounds = gi.UV;

                gi = ref Client.Game.Gumps.GetGump(0x0806);
                foreground = gi.Texture;
                barBoundsF = gi.UV;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
                {
                    SpellRangeInfo i = SpellVisualRangeManager.Instance.GetCurrentSpell();
                    if (i != null)
                    {
                        if (i.CastTime > 0)
                        {
                            if (background != null && foreground != null)
                            {
                                Mobile m = World.Player;
                                Client.Game.Animations.GetAnimationDimensions(
                                    m.AnimIndex,
                                    m.GetGraphicForAnimation(),
                                    0,
                                    0,
                                    m.IsMounted,
                                    0,
                                    out int centerX,
                                    out int centerY,
                                    out int width,
                                    out int height
                                );

                                WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

                                x = vp.Location.X + (int)(m.RealScreenPosition.X - (m.Offset.X + 22 + 5));
                                y = vp.Location.Y + (int)(m.RealScreenPosition.Y - ((m.Offset.Y - m.Offset.Z) - (height + centerY + 15) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0)));

                                batcher.Draw(background, new Rectangle(x, y, barBounds.Width, barBounds.Height), barBounds, hue);

                                double percent = (DateTime.Now - SpellVisualRangeManager.Instance.LastSpellTime).TotalSeconds / i.CastTime;

                                int widthFromPercent = (int)(barBounds.Width * percent);
                                widthFromPercent = widthFromPercent > barBounds.Width ? barBounds.Width : widthFromPercent; //Max width is the bar width

                                if (widthFromPercent > 0)
                                {
                                    batcher.DrawTiled(foreground, new Rectangle(x, y, widthFromPercent, barBoundsF.Height), barBoundsF, hue);
                                }

                                if (percent <= 0 && i.FreezeCharacterWhileCasting)
                                {
                                    World.Player.Flags &= ~Flags.Frozen;
                                }
                            }
                        }
                    }
                }
                return base.Draw(batcher, x, y);
            }
        }
        #endregion
    }
}
