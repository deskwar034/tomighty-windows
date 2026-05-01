using System;

namespace Tomighty
{
    public class RemoteTimerStateDto : ITimerStateDto
    {
        public IntervalType IntervalType { get; set; }
        public TimerStatus Status { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? PausedAtUtc { get; set; }
        public int DurationSeconds { get; set; }
        public int RemainingSeconds { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }
        public DateTime ServerNowUtc { get; set; }
    }
}
