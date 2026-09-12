using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Automation
{
    public class AutomationPolicy
    {
        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, true, false)]
        public void AutomaticTargets_Should_Never_Replace_A_Pending_Target(bool manualTargetActive, bool automaticTargetQueued, bool expected)
        {
            // The rule the whole coordinator exists for: an automatic action may
            // not take the cursor while a manual target is up, and may not
            // overwrite another automatic target that is already queued.
            AutomationCoordinator.TargetIsAvailable(manualTargetActive, automaticTargetQueued)
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(999, 1000, false)]
        [InlineData(1000, 1000, true)]
        [InlineData(1001, 1000, true)]
        [InlineData(1001, 0, false)]
        public void Queued_Automatic_Targets_Should_Expire(long now, long expiresAt, bool expected)
        {
            // expiresAt of 0 means nothing is queued, so it never counts as
            // expired - otherwise an empty slot would report itself stale.
            AutoTargetInfo.IsExpired(now, expiresAt)
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void Death_Pause_Should_Only_Resume_Automation_That_Was_Running(bool pausedForDeath, bool profileAllowsAutomation, bool expected)
        {
            // Dying pauses the helpers; resurrecting must not switch them back on
            // for a profile that had them off to begin with, nor resume a pause
            // that was never taken.
            AutoStopOnDeathManager.ShouldResumeAutomation(pausedForDeath, profileAllowsAutomation)
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(false, false, true, NotorietyFlag.Ally, true)]
        [InlineData(false, true, true, NotorietyFlag.Ally, true)]
        [InlineData(true, false, true, NotorietyFlag.Ally, false)]
        [InlineData(false, false, false, NotorietyFlag.Ally, false)]
        [InlineData(false, false, true, NotorietyFlag.Enemy, false)]
        [InlineData(false, false, true, NotorietyFlag.Invulnerable, false)]
        public void Pet_Cache_Should_Keep_Dead_Bonded_Pets(bool isPlayer, bool isDead, bool isRenamable, NotorietyFlag notoriety, bool expected)
        {
            // Dead pets stay in the snapshot on purpose so Veterinary can
            // resurrect them - being dead is not what disqualifies a pet.
            MobileCache.ShouldCachePet(isPlayer, isDead, isRenamable, notoriety)
                .Should()
                .Be(expected);
        }
    }
}
