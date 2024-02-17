using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElvantoSync;

internal class ElvantoSync(IEnumerable<ISync> syncs) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        foreach (var activeSync in syncs.Where(i => i.IsActive))
        {
            await activeSync.Apply();
        };
    }
}

internal class HostedElvantoSync(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            var syncs = scope.ServiceProvider.GetService<IEnumerable<ISync>>();
            await new ElvantoSync(syncs).Execute(null);
        }
    }
}