using ElvantoSync.GroupFinder.Model;
using ElvantoSync.GroupFinder.Service;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ElvantoSync.GroupFinder.service;

public class GroupFinderService(HttpClient client, ILogger<GroupFinderService> logger) : IGroupFinderService
{
    public async Task CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync($"http://nextcloud.local/index.php/apps/app_api/proxy/simpleapi/group", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string[]> GetGroupAsync(CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // Macht die Deserialisierung unempfindlich gegenüber Groß-/Kleinschreibung
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignoriert nicht gemappte Attribute

            WriteIndented = true
        };
        var request = await client.GetAsync($"http://nextcloud.local/index.php/apps/app_api/proxy/simpleapi/groupIds", cancellationToken);

        try
        {
            var response = await request.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<string[]>(options, cancellationToken);

            return response;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error during group receival");
            return null;
        }
    }
}