using ElvantoSync.Persistence.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElvantoSync.Persistence.Interfaces;

interface IIndexMappingRepository
{
    public Task AddMapping(IndexMapping entity);
    public Task<IEnumerable<string>> FindMissingResources(IEnumerable<string> ids, string type);
    public Task<IEnumerable<string>> FindAdditionalResources(IEnumerable<string> ids, string type);
}