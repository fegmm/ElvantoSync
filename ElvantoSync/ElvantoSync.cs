using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElvantoSync;

internal class ElvantoSync(IEnumerable<ISync> syncs, ILogger<ElvantoSync> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        foreach (var activeSync in syncs.Where(i => i.IsActive))
        {
            try
            {
                using (logger.BeginScope(activeSync.GetType().Name))
                {
                    await activeSync.Apply();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, activeSync.GetType().Name);
            }
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
            var logger = scope.ServiceProvider.GetService<ILogger<ElvantoSync>>();
            await new ElvantoSync(syncs, logger).Execute(null);
        }
    }
}