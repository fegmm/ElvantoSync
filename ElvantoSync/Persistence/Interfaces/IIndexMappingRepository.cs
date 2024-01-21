using ElvantoSync.Persistence.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElvantoSync.Persistence.Interfaces;

interface IIndexMappingRepository
{
    Task AddMapping(IndexMapping entity);
    Task<IEnumerable<string>> MapIndex(IEnumerable<string> fromId, string type);
}