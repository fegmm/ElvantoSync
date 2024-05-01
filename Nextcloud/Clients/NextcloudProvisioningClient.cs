using Nextcloud.Interfaces;
using Nextcloud.Models;
using Nextcloud.Models.Provisioning;
using System.Net.Http.Json;

namespace Nextcloud.Clients;

public class NextcloudProvisioningClient(HttpClient client) : INextcloudProvisioningClient
{
    public async Task<IEnumerable<Group>> GetGroups(CancellationToken cancellationToken = default)
    {
        var request = await client.GetAsync("/ocs/v2.php/cloud/groups/details", cancellationToken);
        var result = await request.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<GetGroupResponse>>(cancellationToken);
        return result!.Ocs.Data.Groups;
    }

    public async Task<IEnumerable<User>> GetUsers(CancellationToken cancellationToken = default)
    {
        var request = await client.GetAsync("/ocs/v2.php/cloud/users/details", cancellationToken);
        var result = await request.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<GetUserResponse>>(cancellationToken);
        return result!.Ocs.Data.Users.Values;
    }

    public async Task AddUserToGroup(string userId, string groupId, CancellationToken cancellationToken = default)
    {
        var reqBody = new { groupid = groupId };
        var request = await client.PostAsJsonAsync($"/ocs/v2.php/cloud/users/{userId}/groups", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task CreateGroup(string groupId, string groupName, CancellationToken cancellationToken = default)
    {
        var reqBody = new { groupid = groupId, displayname = groupName };
        var request = await client.PostAsJsonAsync("/ocs/v2.php/cloud/groups", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateUser(CreateUserRequest user, CancellationToken cancellationToken = default)
    {
        var request = await client.PostAsJsonAsync("/ocs/v2.php/cloud/users", user, cancellationToken);
        var result = await request.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<IdResponse>>(cancellationToken);
        return result!.Ocs.Data.Id;
    }

    public async Task DeleteGroup(string groupId, CancellationToken cancellationToken = default)
    {
        var request = await client.DeleteAsync($"/ocs/v2.php/cloud/groups/{groupId}", cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task DeleteUser(string userId, CancellationToken cancellationToken = default)
    {
        var request = await client.DeleteAsync($"/ocs/v2.php/cloud/users/{userId}", cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<string>> GetMembers(string groupId, CancellationToken cancellationToken = default)
    {
        var request = await client.GetAsync($"/ocs/v2.php/cloud/groups/{groupId}/users", cancellationToken);
        var response = await request.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<GetMembersResponse>>(cancellationToken);
        return response!.Ocs.Data.Users;
    }

    public async Task RemoveUserFromGroup(string userId, string groupId, CancellationToken cancellationToken = default)
    {
        var reqBody = new { groupid = groupId };
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/ocs/v2.php/cloud/users/{userId}/groups")
        {
            Content = JsonContent.Create(reqBody)
        };
        var request = await client.SendAsync(requestMessage, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task EditUser(string userId, EditUserRequest user, CancellationToken cancellationToken = default)
    {
        foreach (var (key, value) in user.ToDictionary())
        {
            var reqBody = new { key = key, value = value };
            var request = await client.PutAsJsonAsync($"/ocs/v2.php/cloud/users/{userId}", reqBody, cancellationToken);
            request.EnsureSuccessStatusCode();
        }
    }

    public async Task EditGroup(string groupId, string newDisplayName, CancellationToken cancellationToken = default)
    {
        var reqBody = new { key = "displayname", value = newDisplayName };
        var request = await client.PutAsJsonAsync($"/ocs/v2.php/cloud/groups/{groupId}", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }
}
