using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElvantoSync;

internal class ElvantoSync(IEnumerable<ISync> syncs) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(syncs.Where(i => i.IsActive).Select(i => i.Apply()));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}