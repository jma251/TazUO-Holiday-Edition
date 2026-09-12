using System;
using ClassicUO.Configuration;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Polls the automatic helpers at 5 Hz behind a shared fault boundary.
    ///
    /// One timer for all of them rather than one each: a helper that is switched
    /// off costs a delegate call and nothing more, and the order of the list is
    /// the order competing actions get their chance at the lease. Emergency care
    /// comes before consumables, consumables before the cure spell, and the
    /// slower upkeep last.
    ///
    /// Every call goes through FeatureDiagnostics.Guard, so a helper that throws
    /// is logged and eventually isolated instead of taking the frame down with it.
    /// </summary>
    public static class AutomationScheduler
    {
        private const long INTERVAL_MS = 200;

        private sealed class Feature
        {
            public readonly string Name;
            public readonly Action Tick;
            public readonly Func<bool> IsActive;

            public Feature(string name, Action tick, Func<bool> isActive = null)
            {
                Name = name;
                Tick = tick;
                IsActive = isActive;
            }
        }

        // Priority order for competing automatic actions: emergency care,
        // cure/heal consumables, the cure spell, refresh, then the upkeep that
        // can wait a frame.
        private static readonly Feature[] _features =
        {
            new Feature("EmergencyHeal", EmergencyHealManager.Tick, () => EmergencyHealManager.Enabled),
            new Feature("AutoCurePotion", AutoCurePotionManager.Tick, () => AutoCurePotionManager.Enabled),
            new Feature("AutoHealPotion", AutoHealPotionManager.Tick, () => AutoHealPotionManager.Enabled),
            new Feature("PoisonCure", PoisonCureManager.Tick, () => PoisonCureManager.Enabled),
            new Feature("AutoRefreshPotion", AutoRefreshPotionManager.Tick, () => AutoRefreshPotionManager.Enabled),
            new Feature("AutoStealth", AutoStealthManager.Tick, () => AutoStealthManager.Enabled),
            new Feature("AutoBuff", AutoBuffManager.Tick, () => AutoBuffManager.Enabled),
            new Feature("AutoMount", AutoMountManager.Tick, () => AutoMountManager.Enabled),
            new Feature("AutoRearm", AutoRearmManager.Tick, () => AutoRearmManager.Enabled),
            new Feature("AutoFollow", AutoFollowManager.Tick, () => AutoFollowManager.Active),
            new Feature("AutoCloseEmptyCorpse", AutoCloseEmptyCorpse.Tick, () => AutoCloseEmptyCorpse.Enabled),
            new Feature("AutoOpenBackpack", AutoOpenBackpackManager.Tick, () => AutoOpenBackpackManager.Enabled),
            new Feature("AutoVendorClose", AutoVendorCloseManager.Tick, () => AutoVendorCloseManager.Enabled),
            new Feature("AutoOpenPaperdoll", AutoOpenPaperdollManager.Tick, () => AutoOpenPaperdollManager.Enabled)
        };

        private static long _nextTick;

        public static void Tick()
        {
            if (Time.Ticks < _nextTick)
            {
                return;
            }

            _nextTick = (long) Time.Ticks + INTERVAL_MS;

            foreach (Feature feature in _features)
            {
                if (feature.IsActive != null && !feature.IsActive())
                {
                    continue;
                }

                FeatureDiagnostics.Guard(feature.Name, feature.Tick);
            }
        }

        /// <summary>
        /// AfkReply and AutoSayThanks are driven by EventSink rather than polled,
        /// so they are not in the list above and only their session state resets
        /// here.
        /// </summary>
        public static void ResetSession()
        {
            _nextTick = 0;

            AutomationCoordinator.ResetForProfile(ProfileManager.CurrentProfile?.AutomationEnabled ?? true);
            FeatureDiagnostics.ResetSession();

            AfkReplyManager.ResetSession();
            AutoCloseEmptyCorpse.ResetSession();
            AutoFollowManager.ResetSession();
            AutoMountManager.ResetSession();
            AutoOpenBackpackManager.ResetSession();
            AutoOpenPaperdollManager.ResetSession();
            AutoRearmManager.ResetSession();
            AutoSayThanksManager.ResetSession();
            AutoStealthManager.ResetSession();
            AutoVendorCloseManager.ResetSession();
        }
    }
}
