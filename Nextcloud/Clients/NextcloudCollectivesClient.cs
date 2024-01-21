using Nextcloud.Interfaces;
using Nextcloud.Models;
using Nextcloud.Models.Collectives;
using System.Net.Http.Json;

namespace ElvantoSync.Infrastructure.Nextcloud;

public class NextcloudCollectivesClient(HttpClient client) : INextcloudCollectivesClient
{
    private string? csrfToken;
    public async Task<Collective[]> GetCollectives(CancellationToken cancellationToken = default)
    {
        var token = await GetCsrfToken(cancellationToken);
        client.DefaultRequestHeaders.Add("requesttoken", token);
        var response = await client.GetAsync("/index.php/apps/collectives/_api", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<OCS<Collective[]>>(cancellationToken);
        return result!.Data;
    }

    public async Task<Collective> CreateCollective(string name, CancellationToken cancellationToken = default)
    {
        var reqBody = new { name = name };
        var token = await GetCsrfToken(cancellationToken);
        client.DefaultRequestHeaders.Add("requesttoken", token);
        var response = await client.PostAsJsonAsync("/index.php/apps/collectives/_api", reqBody, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<OCS<Collective>>(cancellationToken);
        return result!.Data;
    }

    private async Task<string> GetCsrfToken(CancellationToken cancellationToken = default)
    {
        if (csrfToken != null)
            return csrfToken;

        var response = await client.GetAsync("/index.php/csrftoken", cancellationToken);
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<CSRFToken>(cancellationToken);
        csrfToken = result!.Token;
        return csrfToken;
    }

    public async Task SetDisplayName(string circleId, string name, CancellationToken cancellationToken = default)
    {
        var reqBody = new { name = name };
        var response = await client.PutAsJsonAsync($"/ocs/v2.php/apps/circles/circles/{circleId}/name", reqBody, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
