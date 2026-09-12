using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// A shared snapshot of world entities for the overlays and helpers that would
    /// otherwise scan every mobile on every frame. Walking a List is cheaper than
    /// the dictionary's value enumerator, and one snapshot serves all of them.
    ///
    /// Each list is built only while something is asking for it, so a snapshot
    /// costs nothing for features that are switched off.
    ///
    /// MW Edition also caches a list of every mobile. Nothing here reads it - the
    /// overlays that do are his ambience, blood and notoriety-dot effects, which
    /// are not in this fork - so it is left out rather than built for no one.
    /// </summary>
    public static class MobileCache
    {
        // Rebuilt on each pass. Consumers must still check IsDestroyed - an entity
        // can be destroyed between snapshots.
        public static readonly List<Mobile> Hostiles = new List<Mobile>(32);
        public static readonly List<Mobile> Pets = new List<Mobile>(16);
        public static readonly List<Item> GroundItems = new List<Item>(128);

        public static bool IsNeeded => NeedsHostiles || NeedsPets || NeedsGroundItems;

        private static bool NeedsHostiles =>
            HostileEdgeHighlight.Enabled
            || OffscreenEnemyArrow.Enabled
            || NearestHostileLine.Enabled
            || CombatMobHpBars.Range > 0;

        private static bool NeedsPets =>
            AutoBandageManager.Enabled
            || PetBandageManager.Enabled
            || ExternalBandageManager.Enabled
            || PetHpBarsOverlay.Enabled;

        private static bool NeedsGroundItems =>
            GroundLootFinder.Range > 0
            || CorpseFadeOverlay.Enabled;

        public static void Rebuild()
        {
            bool needHostiles = NeedsHostiles;
            bool needPets = NeedsPets;
            bool needGroundItems = NeedsGroundItems;

            Hostiles.Clear();
            Pets.Clear();
            GroundItems.Clear();

            if (needHostiles || needPets)
            {
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m == null || m.IsDestroyed || m == World.Player)
                    {
                        continue;
                    }

                    NotorietyFlag notoriety = m.NotorietyFlag;

                    if (needHostiles && !m.IsDead
                        && notoriety != NotorietyFlag.Innocent
                        && notoriety != NotorietyFlag.Invulnerable
                        && notoriety != NotorietyFlag.Ally)
                    {
                        Hostiles.Add(m);
                    }

                    if (needPets && ShouldCachePet(false, m.IsDead, m.IsRenamable, notoriety))
                    {
                        Pets.Add(m);
                    }
                }
            }

            if (needGroundItems)
            {
                foreach (Item item in World.Items.Values)
                {
                    if (item != null && !item.IsDestroyed && item.OnGround)
                    {
                        GroundItems.Add(item);
                    }
                }
            }
        }

        internal static bool ShouldCachePet(bool isPlayer, bool isDead, bool isRenamable, NotorietyFlag notoriety)
        {
            // Dead bonded pets stay eligible so Veterinary can resurrect them.
            // PetBandageManager.BlockOnDead remains the user-facing opt-out.
            return !isPlayer && isRenamable &&
                   notoriety != NotorietyFlag.Enemy &&
                   notoriety != NotorietyFlag.Invulnerable;
        }

        public static void Clear()
        {
            Hostiles.Clear();
            Pets.Clear();
            GroundItems.Clear();
        }
    }
}
