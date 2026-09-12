using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// A shared snapshot of world entities, rebuilt at 20Hz, for the helpers that
    /// would otherwise scan every mobile on every frame. Walking a List is cheaper
    /// than the dictionary's value enumerator, and one snapshot serves all of them.
    ///
    /// Only the pet list is here. MW Edition's version also caches all mobiles,
    /// hostiles and ground items, but every switch that turns those on is one of
    /// his overlays - hostile edge highlight, the offscreen enemy arrow, the
    /// ambience and corpse-fade overlays - and none of those are in this fork.
    /// Caching lists nothing reads would be cost without a reader. The remaining
    /// collections belong with the features that want them.
    /// </summary>
    public static class MobileCache
    {
        // Rebuilt at 20Hz. Consumers must still check IsDestroyed - an entity can
        // be destroyed between snapshots.
        public static readonly List<Mobile> Pets = new List<Mobile>(16);

        public static bool IsNeeded => NeedsPets;

        private static bool NeedsPets =>
            AutoBandageManager.Enabled
            || PetBandageManager.Enabled
            || ExternalBandageManager.Enabled;

        public static void Rebuild()
        {
            Pets.Clear();

            if (!NeedsPets)
            {
                return;
            }

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.IsDestroyed || m == World.Player)
                {
                    continue;
                }

                if (ShouldCachePet(false, m.IsDead, m.IsRenamable, m.NotorietyFlag))
                {
                    Pets.Add(m);
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
            Pets.Clear();
        }
    }
}
