using Nextcloud.Models.Provisioning;

namespace Nextcloud.Interfaces;

public interface INextcloudProvisioningClient
{
    Task AddUserToGroup(string userId, string groupId, CancellationToken cancellationToken = default);

    Task CreateGroup(string groupId, string groupName, CancellationToken cancellationToken = default);
    Task<string> CreateUser(CreateUserRequest user, CancellationToken cancellationToken = default);
    Task DeleteGroup(string groupId, CancellationToken cancellationToken = default);
    Task DeleteUser(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Group>> GetGroups(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetMembers(string groupId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsers(CancellationToken cancellationToken = default);
    Task RemoveUserFromGroup(string userId, string groupId, CancellationToken cancellationToken = default);
}