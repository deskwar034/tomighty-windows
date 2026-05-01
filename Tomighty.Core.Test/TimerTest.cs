using System.Reflection;
using System.Linq;
using NUnit.Framework;
using Tomighty.Events;

namespace Tomighty
{
    [TestFixture]
    public class TimerTest
    {
        private FakeEventHub eventHub;
        private Timer timer;

        [SetUp]
        public void SetUp()
        {
            eventHub = new FakeEventHub();
            timer = new Timer(eventHub);
        }

        [Test]
        public void PauseShouldNotPublishTimerStopped()
        {
            timer.Start(Duration.InMinutes(25), IntervalType.Pomodoro);

            timer.Pause();

            Assert.AreEqual(0, eventHub.PublishedEvents<TimerStopped>().Count());
        }

        [Test]
        public void ResumeAfterStopShouldDoNothing()
        {
            timer.Start(Duration.InMinutes(25), IntervalType.Pomodoro);
            timer.Stop();

            timer.Resume();

            Assert.AreEqual(0, eventHub.PublishedEvents<TimerResumed>().Count());
        }

        [Test]
        public void LateTickAfterPauseShouldNotChangeRemainingTime()
        {
            timer.Start(Duration.InMinutes(25), IntervalType.Pomodoro);
            timer.Pause();
            var remainingAtPause = timer.RemainingTime;

            InvokeDecreaseRemainingTime(timer, 1);

            Assert.AreEqual(remainingAtPause, timer.RemainingTime);
            Assert.AreEqual(0, eventHub.PublishedEvents<TimeElapsed>().Count());
        }

        [Test]
        public void TickFromZeroShouldNotThrowAndShouldStopAtZero()
        {
            timer.Start(Duration.Zero, IntervalType.Pomodoro);

            Assert.DoesNotThrow(() => InvokeDecreaseRemainingTime(timer, 1));
            Assert.AreEqual(Duration.Zero, timer.RemainingTime);
            Assert.AreEqual(1, eventHub.PublishedEvents<TimeElapsed>().Count());
            Assert.AreEqual(1, eventHub.PublishedEvents<TimerStopped>().Count());
        }

        private static void InvokeDecreaseRemainingTime(Timer timer, int seconds)
        {
            var method = typeof(Timer).GetMethod("DecreaseRemainingTime", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(timer, new object[] { seconds });
        }
    }
}
