namespace Tomighty
{
    public interface ILanDiscoveryService
    {
        ILanDiscoveryHost Host { get; }
        ILanDiscoveryClient Client { get; }
    }
}
