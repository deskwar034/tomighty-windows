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
                var safeDuration = Math.Max(0, state.DurationSeconds);
                var offset = state.ServerNowUtc - clientUtcNow;
                var adjustedClientUtcNow = clientUtcNow + offset;
                var elapsedTotalSeconds = (adjustedClientUtcNow - state.StartedAtUtc.Value).TotalSeconds;
                var elapsedSeconds = Math.Max(0d, Math.Floor(elapsedTotalSeconds));
                var remaining = safeDuration - elapsedSeconds;
                if (remaining < 0d) remaining = 0d;
                if (remaining > int.MaxValue) remaining = int.MaxValue;
                return new Duration((int)remaining);
            }

            if (state.Status == TimerStatus.Paused)
            {
                var remaining = Math.Max(0, state.RemainingSeconds);
                return new Duration(remaining);
            }

            return Duration.Zero;
        }
    }
}
