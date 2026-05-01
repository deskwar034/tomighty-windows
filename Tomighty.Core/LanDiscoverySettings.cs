namespace Tomighty
{
    public class LanDiscoverySettings
    {
        public const int DefaultApiPort = 5055;
        public const int DefaultDiscoveryPort = 5056;
        public const int DefaultDiscoveryTimeoutMs = 4000;

        public int ApiPort { get; set; } = DefaultApiPort;
        public int DiscoveryPort { get; set; } = DefaultDiscoveryPort;
        public int DiscoveryTimeoutMs { get; set; } = DefaultDiscoveryTimeoutMs;
        public string HostDisplayName { get; set; }
        public bool RequiresSharedKey { get; set; }
    }
}
