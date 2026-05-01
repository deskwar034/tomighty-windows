using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tomighty
{
    public interface ILanDiscoveryClient
    {
        Task<IReadOnlyCollection<LanDiscoveredHost>> DiscoverAsync(LanDiscoverySettings settings, CancellationToken cancellationToken);
    }
}
