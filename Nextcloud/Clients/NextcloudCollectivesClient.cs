using Nextcloud.Interfaces;
using Nextcloud.Models;
using Nextcloud.Models.Collectives;
using System.Net.Http.Json;

namespace ElvantoSync.Infrastructure.Nextcloud;

public class NextcloudCollectivesClient(HttpClient client) : INextcloudCollectivesClient
{
    public async Task<Collective[]> GetCollectives(CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync("/index.php/apps/collectives/_api", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<OCS<Collective[]>>(cancellationToken);
        return result!.Data;
    }

    public async Task<Collective> CreateCollective(string name, CancellationToken cancellationToken = default)
    {
        var reqBody = new { name = name };
        var response = await client.PostAsJsonAsync("/index.php/apps/collectives/_api", reqBody, cancellationToken);
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCS<Collective>>(cancellationToken);
        return result!.Data;
    }

    internal static async Task<string> GetCsrfToken(HttpClient httpClient)
    {
        var response = await httpClient.GetAsync("/index.php/csrftoken");
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<CSRFToken>();
        return result!.Token;
    }

    public async Task DeleteCollective(int collectiveId, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"/index.php/apps/collectives/_api/{collectiveId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var deletePermanentResponse = await client.DeleteAsync($"/index.php/apps/collectives/_api/trash/{collectiveId}?circle=1", cancellationToken);
        deletePermanentResponse.EnsureSuccessStatusCode();
    }

    public async Task SetDisplayName(string circleId, string name, CancellationToken cancellationToken = default)
    {
        var query = new System.Collections.Generic.Dictionary<string, string> { { "value", name } };
        var queryString = await new FormUrlEncodedContent(query).ReadAsStringAsync();
        var response = await client.PutAsJsonAsync($"/ocs/v2.php/apps/circles/circles/{circleId}/name?{queryString}", "", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
