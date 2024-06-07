using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ElvantoSync.GroupFinder.Model;
using ElvantoSync.GroupFinder.Service;

namespace ElvantoSync.GroupFinder.service;

public class GroupFinderService(HttpClient client) : IGroupFinderService
{
    public async Task createGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {   
        
        var response = await client.PostAsJsonAsync($"http://nextcloud.local/index.php/apps/app_api/proxy/simpleapi/group", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}