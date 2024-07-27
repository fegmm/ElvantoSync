using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using ElvantoSync.GroupFinder.Model;

namespace ElvantoSync.GroupFinder.Service;
public interface IGroupFinderService{
    public Task createGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    public Task<string[]> GetGroupAsync( CancellationToken cancellationToken = default);
}