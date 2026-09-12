using ClassicUO.Game.Managers;
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
    }
}
