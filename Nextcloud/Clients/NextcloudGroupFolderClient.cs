using Nextcloud.Interfaces;
using Nextcloud.Models;
using Nextcloud.Models.GroupFolders;
using System.Net.Http.Json;

namespace Nextcloud.Clients;

public class NextcloudGroupFolderClient(HttpClient client) : INextcloudGroupFolderClient
{
    public async Task<IEnumerable<GroupFolder>> GetGroupFolders(CancellationToken cancellationToken = default)
    {
        var request = await client.GetAsync("index.php/apps/groupfolders/folders", cancellationToken);
        var response = await request.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<GetGroupFolderResponse>(cancellationToken);
        return response!.Ocs.Data.Values;
    }

    public async Task<int> CreateGroupFolder(string name, CancellationToken cancellationToken = default)
    {
        var request = await client.PostAsJsonAsync("index.php/apps/groupfolders/folders", new { mountpoint = name }, cancellationToken);
        var response = await request.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<IdResponse<int>>>(cancellationToken);
        return response!.Ocs.Data.Id;
    }

    public async Task DeleteGroupFolder(int id, CancellationToken cancellationToken = default)
    {
        var request = await client.DeleteAsync($"index.php/apps/groupfolders/folders/{id}", cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task SetMountpoint(int id, string mountpoint, CancellationToken cancellationToken = default)
    {
        var reqBody = new { mountpoint = mountpoint };
        var request = await client.PostAsJsonAsync($"index.php/apps/groupfolders/folders/{id}/mountpoint", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task AddGroup(int id, string groupId, CancellationToken cancellationToken = default)
    {
        var reqBody = new { group = groupId };
        var request = await client.PostAsJsonAsync($"index.php/apps/groupfolders/folders/{id}/groups", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task SetAcl(int id, bool enable, CancellationToken cancellationToken = default)
    {
        var reqBody = new { acl = enable ? 1 : 0 };
        var request = await client.PostAsJsonAsync($"index.php/apps/groupfolders/folders/{id}/acl", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task AddAclManager(int id, string groupId, CancellationToken cancellationToken = default)
    {
        var reqBody = new { mappingId = groupId, mappingType = "group", manageAcl = true };
        var request = await client.PostAsJsonAsync($"index.php/apps/groupfolders/folders/{id}/manageACL", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task RemoveAclManager(int id, string groupId, CancellationToken cancellationToken = default)
    {
        var reqBody = new { mappingId = groupId, mappingType = "group", manageAcl = false };
        var request = await client.PostAsJsonAsync($"index.php/apps/groupfolders/folders/{id}/manageACL", reqBody, cancellationToken);
        request.EnsureSuccessStatusCode();
    }

    public async Task SetPermission(int id, string groupId, Permissions permission)
    {
        var reqBody = new { permissions = (int)permission };
        var request = await client.PostAsJsonAsync($"index.php/apps/groupfolders/folders/{id}/groups/{groupId}", reqBody);
        request.EnsureSuccessStatusCode();
    }
}
