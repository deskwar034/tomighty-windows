using System.Timers;
using Tomighty.Events;
using SystemTimer = System.Timers.Timer;

namespace Tomighty
{
    public class Timer : ITimer
    {
        private const int IntervalInSeconds = 1;
        private readonly object sync = new object();
        private readonly SystemTimer systemTimer = new SystemTimer();
        private readonly IEventHub eventHub;
        private Duration remainingTime = Duration.Zero;
        private IntervalType intervalType;
        private Duration duration;
        private TimerStatus status = TimerStatus.Idle;

        public Timer(IEventHub eventHub)
        {
            this.eventHub = eventHub;
            systemTimer.Interval = IntervalInSeconds * 1000;
            systemTimer.AutoReset = true;
            systemTimer.Elapsed += SystemTimerOnElapsed;
        }

        public Duration RemainingTime { get { lock(sync) return remainingTime; } }

        public void Start(Duration duration, IntervalType intervalType)
        {
            TimerStarted started = null;
            lock (sync)
            {
                if (status != TimerStatus.Idle) return;
                this.duration = duration;
                this.intervalType = intervalType;
                status = TimerStatus.Running;
                remainingTime = duration;
                systemTimer.Start();
                started = new TimerStarted(intervalType, duration, remainingTime);
            }
            eventHub.Publish(started);
        }

        public void Stop()
        {
            TimerStopped stopped = null;
            lock (sync)
            {
                if (status == TimerStatus.Idle) return;
                systemTimer.Stop();
                status = TimerStatus.Idle;
                stopped = new TimerStopped(intervalType, duration, remainingTime);
                remainingTime = Duration.Zero;
            }
            eventHub.Publish(stopped);
        }

        public void Pause(){ TimerPaused ev=null; lock(sync){ if(status!=TimerStatus.Running)return; status=TimerStatus.Paused; systemTimer.Stop(); ev=new TimerPaused(intervalType,duration,remainingTime);} eventHub.Publish(ev);} 
        public void Resume(){ TimerResumed ev=null; lock(sync){ if(status!=TimerStatus.Paused)return; status=TimerStatus.Running; systemTimer.Start(); ev=new TimerResumed(intervalType,duration,remainingTime);} eventHub.Publish(ev);} 

        private void DecreaseRemainingTime(int seconds)
        {
            TimeElapsed elapsed = null; TimerStopped stopped = null;
            lock (sync)
            {
                if (status != TimerStatus.Running) return;
                var next = remainingTime.Seconds - seconds;
                if (next < 0) next = 0;
                remainingTime = new Duration(next);
                elapsed = new TimeElapsed(intervalType, duration, remainingTime);
                if (remainingTime.Seconds == 0)
                {
                    systemTimer.Stop();
                    status = TimerStatus.Idle;
                    stopped = new TimerStopped(intervalType, duration, remainingTime);
                    remainingTime = Duration.Zero;
                }
            }
            eventHub.Publish(elapsed);
            if (stopped != null) eventHub.Publish(stopped);
        }

        private void SystemTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            DecreaseRemainingTime(IntervalInSeconds);
        }
    }
}
