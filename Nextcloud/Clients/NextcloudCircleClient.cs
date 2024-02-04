using Nextcloud.Models;
using Nextcloud.Models.Circles;
using System.Net.Http.Json;

namespace ElvantoSync.Infrastructure.Nextcloud;

public class NextcloudCircleClient(HttpClient client) : INextcloudCircleClient
{
    public async Task SetMemberLevel(string circleId, string memberId, MemberLevels level, CancellationToken cancellationToken = default)
    {
        var reqBody = new { level = (int)level };
        var response = await client.PutAsJsonAsync($"/ocs/v2.php/apps/circles/circles/{circleId}/members/{memberId}/level", reqBody, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> AddMemberToCircle(string circleId, string memberId, MemberTypes memberType, CancellationToken cancellationToken = default)
    {
        var reqBody = new { userId = memberId, type = (int)memberType };
        var response = await client.PostAsJsonAsync($"/ocs/v2.php/apps/circles/circles/{circleId}/members", reqBody, cancellationToken);
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<IdResponse>>(cancellationToken);
        return result!.Ocs.Data.Id;
    }
}