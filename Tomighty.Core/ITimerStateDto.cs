using System;

namespace Tomighty
{
    public interface ITimerStateDto
    {
        IntervalType IntervalType { get; }
        TimerStatus Status { get; }
        DateTime? StartedAtUtc { get; }
        DateTime? PausedAtUtc { get; }
        int DurationSeconds { get; }
        int RemainingSeconds { get; }
        DateTime LastUpdatedAtUtc { get; }
        DateTime ServerNowUtc { get; }
    }
}
