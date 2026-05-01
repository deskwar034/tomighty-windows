namespace Tomighty
{
    public interface IRemoteTimerSyncService
    {
        TimerSyncStatus Status { get; }
        Duration ResolveRemainingTime(ITimerStateDto state, System.DateTime clientUtcNow);
    }
}
