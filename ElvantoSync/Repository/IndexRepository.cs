

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

class IndexRepository(MyDbContext context) : IResourceIndexManager
{
    public async Task CreateResourceIndex(ResourceIndexEntity entity)
    {
        await context.IndexEntities.AddAsync(entity);
        await context.SaveChangesAsync();              
    }

    public async Task<IEnumerable<string>> FindAdditionalResources(List<string> groupId, string type)
    {
       return await 
        context.IndexEntities.Where(i => i.Type.Equals(type))
        .Where(i => !groupId.Contains(i.TFromIdentifier))
        .Select(index => index.TFromIdentifier).ToListAsync();
    }
   
    public async Task<IEnumerable<string>> FindMissingResources(List<string> groupId, string type)
    {
       return groupId.Except(
        await context.IndexEntities
        .Where(i => i.Type.Equals(type))
        .Select(i => i.TFromIdentifier).ToListAsync()
         );
    }



}