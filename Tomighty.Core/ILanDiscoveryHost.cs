using System.Threading;
using System.Threading.Tasks;

namespace Tomighty
{
    public interface ILanDiscoveryHost
    {
        Task StartAsync(LanDiscoverySettings settings, CancellationToken cancellationToken);
    }
}
