using ElvantoSync.GroupFinder.Model;
using System.Threading;
using System.Threading.Tasks;

namespace ElvantoSync.GroupFinder.Service;
public interface IGroupFinderService
{
    public Task CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    public Task<string[]> GetGroupAsync(CancellationToken cancellationToken = default);
}