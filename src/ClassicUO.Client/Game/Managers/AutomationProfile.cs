using ClassicUO.Configuration;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// The one place a helper's switch is read from and the one place it is
    /// written to.
    ///
    /// These used to be bare statics with no owner. Half were cleared on the way
    /// out of a world and half were not, so a helper armed on one character could
    /// still be armed on the next; none of them survived a restart; and two of
    /// them switched themselves on from a skill value, which meant a helper that
    /// consumes bandages could arm itself on a character whose owner had never
    /// asked for it. The switch now lives on the profile, which is per character,
    /// and nothing but the checkbox moves it.
    ///
    /// AutoFollow is absent on purpose: it is armed by having a target, not by a
    /// flag, and it is cleared on the way into a world like any other session
    /// state.
    /// </summary>
    public static class AutomationProfile
    {
        /// <summary>
        /// Profile to helpers. Called on the way into a world, after every
        /// ResetForProfile has put the tuning back to its defaults and the
        /// tuning files have been read over them.
        /// </summary>
        public static void Apply(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            AutomationCoordinator.ResetForProfile(profile.AutomationEnabled);

            EmergencyHealManager.Enabled = profile.AutoEmergencyHeal;
            EmergencyHealManager.ThresholdPct = Clamp(profile.AutoEmergencyHealPct, 1, 99);

            if (!string.IsNullOrWhiteSpace(profile.AutoEmergencyHealSpell))
            {
                EmergencyHealManager.SpellName = profile.AutoEmergencyHealSpell;
            }

            AutoCurePotionManager.Enabled = profile.AutoCurePotion;

            AutoHealPotionManager.Enabled = profile.AutoHealPotion;
            AutoHealPotionManager.ThresholdPct = Clamp(profile.AutoHealPotionPct, 1, 99);

            AutoRefreshPotionManager.Enabled = profile.AutoRefreshPotion;
            AutoRefreshPotionManager.ThresholdPct = Clamp(profile.AutoRefreshPotionPct, 1, 99);

            PoisonCureManager.Enabled = profile.AutoPoisonCure;

            if (!string.IsNullOrWhiteSpace(profile.AutoPoisonCureSpell))
            {
                PoisonCureManager.SpellName = profile.AutoPoisonCureSpell;
            }

            AutoBandageManager.SetEnabledQuiet(profile.AutoBandageSelf);
            PetBandageManager.SetEnabledQuiet(profile.AutoBandagePet);
            ExternalBandageManager.Enabled = profile.AutoBandageOthers;
            BandageStockWarner.Enabled = profile.AutoBandageStockWarn;

            // Armed rather than merely switched on: with nothing to watch, Tick
            // returns at its first guard, so the checkbox would do nothing. An
            // unparseable buff name leaves it off rather than half-armed.
            ApplyBuff(profile);

            AutoStealthManager.Enabled = profile.AutoStealth;
            AutoRearmManager.Enabled = profile.AutoRearm;
            AutoMountManager.Enabled = profile.AutoMount;

            AutoHitListManager.Enabled = profile.AutoHitList;
            AutoHitListManager.AlsoAttack = profile.AutoHitListAttack;

            // The name list loads lazily, from Tick. The options page shows the
            // list for editing and would have shown it empty on a character whose
            // autohit.tsv had not been touched yet this session.
            AutoHitListManager.EnsureLoaded();

            AutoRespawnTargetManager.Enabled = profile.AutoRespawnTarget;
            AutoRespawnTargetManager.AlsoAttack = profile.AutoRespawnTargetAttack;

            AutoStopOnDeathManager.Enabled = profile.AutoStopOnDeath;

            AutoCloseEmptyCorpse.Enabled = profile.AutoCloseEmptyCorpse;
            AutoOpenBackpackManager.Enabled = profile.AutoOpenBackpack;
            AutoOpenPaperdollManager.Enabled = profile.AutoOpenPaperdoll;
            AutoVendorCloseManager.Enabled = profile.AutoVendorClose;

            AfkReplyManager.Enabled = profile.AutoAfkReply;

            if (!string.IsNullOrWhiteSpace(profile.AutoAfkReplyMessage))
            {
                AfkReplyManager.Message = profile.AutoAfkReplyMessage;
            }

            AutoSayThanksManager.Enabled = profile.AutoSayThanks;

            // Subscribed here rather than from each SetEnabled, which nothing
            // calls. Both handlers test Enabled first, so a subscription that
            // outlives a switch costs one comparison; a switch that outlives a
            // missing subscription is a feature that silently does nothing.
            // _hooked makes each of these a no-op after the first world.
            AfkReplyManager.EnsureHooked();
            AutoSayThanksManager.EnsureHooked();
            AutoStealthManager.EnsureHooked();
        }

        private static void ApplyBuff(Profile profile)
        {
            AutoBuffManager.Disarm();

            if (!profile.AutoBuff
                || string.IsNullOrWhiteSpace(profile.AutoBuffWatch)
                || string.IsNullOrWhiteSpace(profile.AutoBuffSpell))
            {
                return;
            }

            AutoBuffManager.ArmQuiet(profile.AutoBuffWatch, profile.AutoBuffSpell);
        }

        /// <summary>True if the name is one the buff watcher can act on.</summary>
        public static bool IsKnownBuff(string name)
        {
            BuffIconType ignored;

            return AutoBuffManager.TryParseBuffPublic(name, out ignored);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
