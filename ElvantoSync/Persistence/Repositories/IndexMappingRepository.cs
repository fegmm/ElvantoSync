using ElvantoSync.Persistence.Entities;
using ElvantoSync.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Persistence.Repositories;

public class IndexMappingRepository(DbContext context) : IIndexMappingRepository
{
    public async Task AddMapping(IndexMapping entity)
    {
        await context.IndexMappings.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task RemoveMapping(IndexMapping entity)
    {
        context.IndexMappings.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> FindAdditionalResources(IEnumerable<string> ids, string type)
    {
        return await context.IndexMappings
            .Where(i => i.Type == type)
            .GroupJoin(ids, i => i.ToId, i => i, (i, _) => i.ToId)
            .Where(i => i == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> FindMissingResources(IEnumerable<string> ids, string type)
    {
        return await context.IndexMappings
            .Where(i => i.Type == type)
            .GroupJoin(ids, i => i.FromId, i => i, (i, _) => i.FromId)
            .Where(i => i == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> MapIndex(IEnumerable<string> fromId, string type)
        => await context.IndexMappings
            .Where(i => i.Type == type)
            .Join(fromId, i => i.FromId, i => i, (i, _) => i.ToId)
            .ToListAsync();
}