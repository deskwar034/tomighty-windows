using System;
using NUnit.Framework;

namespace Tomighty
{
    [TestFixture]
    public class RemoteTimerSyncServiceTest
    {
        [Test]
        public void ShouldCalculateRemainingTimeUsingServerNowWhenRunning()
        {
            var service = new RemoteTimerSyncService();
            var clientNow = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
            var state = new RemoteTimerStateDto
            {
                Status = TimerStatus.Running,
                DurationSeconds = 1500,
                StartedAtUtc = new DateTime(2026, 5, 1, 11, 55, 0, DateTimeKind.Utc),
                ServerNowUtc = clientNow,
                RemainingSeconds = 999
            };

            var remaining = service.ResolveRemainingTime(state, clientNow);

            Assert.AreEqual(1200, remaining.Seconds);
        }

        [Test]
        public void ShouldApplyClockOffsetWhenRunning()
        {
            var service = new RemoteTimerSyncService();
            var clientNow = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
            var state = new RemoteTimerStateDto
            {
                Status = TimerStatus.Running,
                DurationSeconds = 1500,
                StartedAtUtc = new DateTime(2026, 5, 1, 11, 58, 0, DateTimeKind.Utc),
                ServerNowUtc = clientNow.AddSeconds(30)
            };

            var remaining = service.ResolveRemainingTime(state, clientNow);

            Assert.AreEqual(1350, remaining.Seconds);
        }

        [Test]
        public void ShouldUseSavedRemainingWhenPaused()
        {
            var service = new RemoteTimerSyncService();
            var state = new RemoteTimerStateDto
            {
                Status = TimerStatus.Paused,
                RemainingSeconds = 321,
                ServerNowUtc = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            var remaining = service.ResolveRemainingTime(state, DateTime.UtcNow);

            Assert.AreEqual(321, remaining.Seconds);
        }
    }
}
