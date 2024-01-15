

using System.Collections.Generic;
using System.Threading.Tasks;

interface IResourceIndexManager{
    public Task CreateResourceIndex(ResourceIndexEntity entity);
    public Task<IEnumerable<int>> FindMissingResources(List<int> groupId, string type);    
    public Task<IEnumerable<int>> FindAdditionalResources(List<int> groupId, string type);   
}