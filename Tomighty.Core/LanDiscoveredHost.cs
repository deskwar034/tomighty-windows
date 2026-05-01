using System;

namespace Tomighty
{
    public class LanDiscoveredHost
    {
        public string Protocol { get; set; }
        public string MachineName { get; set; }
        public string DisplayName { get; set; }
        public string Host { get; set; }
        public int ApiPort { get; set; }
        public int DiscoveryPort { get; set; }
        public SyncMode SyncMode { get; set; }
        public bool RequiresSharedKey { get; set; }

        public string DeduplicationKey => string.Format("{0}:{1}", Host ?? string.Empty, ApiPort);

        public string DisplayLabel => string.IsNullOrWhiteSpace(DisplayName) ? MachineName : DisplayName;
    }
}
