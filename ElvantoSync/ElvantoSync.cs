using ElvantoSync.Persistence;
using Quartz;
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