using System;

namespace Tomighty
{
    public class RemoteTimerSyncService : IRemoteTimerSyncService
    {
        public TimerSyncStatus Status { get; private set; }

        public RemoteTimerSyncService()
        {
            Status = TimerSyncStatus.Disconnected;
        }

        public Duration ResolveRemainingTime(ITimerStateDto state, DateTime clientUtcNow)
        {
            if (state == null)
            {
                Status = TimerSyncStatus.Error;
                return Duration.Zero;
            }

            Status = TimerSyncStatus.Connected;

            if (state.Status == TimerStatus.Running && state.StartedAtUtc.HasValue)
            {
                var offset = state.ServerNowUtc - clientUtcNow;
                var adjustedClientUtcNow = clientUtcNow + offset;
                var elapsedSeconds = (int)Math.Floor((adjustedClientUtcNow - state.StartedAtUtc.Value).TotalSeconds);
                var remainingSeconds = Math.Max(0, state.DurationSeconds - Math.Max(0, elapsedSeconds));
                return new Duration(remainingSeconds);
            }

            if (state.Status == TimerStatus.Paused)
            {
                return new Duration(Math.Max(0, state.RemainingSeconds));
            }

            return Duration.Zero;
        }
    }
}
