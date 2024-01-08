using Nextcloud.Models.GroupFolders;

namespace Nextcloud.Interfaces
{
    public interface INextcloudGroupFolderClient
    {
        Task AddAclManager(int id, string groupId, CancellationToken cancellationToken = default);
        Task AddGroup(int id, string groupId, CancellationToken cancellationToken = default);
        Task<int> CreateGroupFolder(string name, CancellationToken cancellationToken = default);
        Task DeleteGroupFolder(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<GroupFolder>> GetGroupFolders(CancellationToken cancellationToken = default);
        Task RemoveAclManager(int id, string groupId, CancellationToken cancellationToken = default);
        Task SetAcl(int id, bool enable, CancellationToken cancellationToken = default);
        Task SetMountpoint(int id, string mountpoint, CancellationToken cancellationToken = default);
        Task SetPermission(int id, string groupId, Permissions permission);
    }
}