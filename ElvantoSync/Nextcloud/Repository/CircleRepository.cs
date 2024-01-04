using ElvantoSync.Util;
using Flurl.Http;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud.Repository;

interface ICircleRepository
{
    Task<string> AddMembersToCircle(string circleId, string groupName);
    Task<bool> PromoteMemberToAdmin(string circleId, string memberId);
}

class CircleRepository : ICircleRepository
{
    private readonly NextcloudApi.Api nextcloud;
    private readonly FlurlClient client;

    public CircleRepository(NextcloudApi.Api nextCloud, FlurlClientFactory flurlFactory)
    {
        this.nextcloud = nextCloud;
        client = flurlFactory.GetClient();
    }

    public async Task<bool> PromoteMemberToAdmin(string circleId, string memberId)
    {
        var reqBody = new
        {
            level = 8
        };

        var result = await client.Request(nextcloud.Settings.ServerUri?.ToString())
            .AppendPathSegment($"/ocs/v2.php/apps/circles/circles/{circleId}/members/{memberId}/level")
            .WithBasicAuth(nextcloud.Settings.Username, nextcloud.Settings.Password)
            .WithHeader("Accept", "application/json")
            .WithHeader("OCS-ApiRequest", "true")
            .PutJsonAsync(reqBody)
            .ReceiveJson<OCSResponse<object>>();

        if (result.ocs.meta.statuscode != 200)
        {
            return false;
        }

        return true;
    }

    public async Task<string> AddMembersToCircle(string circleId, string groupName)
    {

        var reqBody = new
        {
            userId = groupName,
            type = 2
        };
        var result = await client.Request(nextcloud.Settings.ServerUri?.ToString())
                .AppendPathSegment($"/ocs/v2.php/apps/circles/circles/{circleId}/members")
                .WithBasicAuth(nextcloud.Settings.Username, nextcloud.Settings.Password)
                .WithHeader("Accept", "application/json")
                .WithHeader("OCS-ApiRequest", "true")
                .PostJsonAsync(reqBody)
                .ReceiveJson<OCSResponse<AddMemberData>>();

        if (result.ocs.meta.statuscode != 200)
        {
            throw new System.Exception(result.ocs.meta.message);
        }

        return result.ocs.data.Id;
    }
}

class MockCircleRepository : ICircleRepository
{
    async Task<string> ICircleRepository.AddMembersToCircle(string circleId, string groupName)
    {
        await Task.Delay(3);
        return "";
    }

    async Task<bool> ICircleRepository.PromoteMemberToAdmin(string circleId, string memberId)
    {
        await Task.Delay(3);
        return true;
    }
}

public class OCSResponse<T>
{
    public Ocs<T> ocs { get; set; }
}

public class Ocs<T>
{
    public Meta meta { get; set; }
    public T data { get; set; }
}

public class AddMemberData
{
    public string Id { get; set; }
}

public class Meta
{
    public string status { get; set; }
    public int statuscode { get; set; }
    public string message { get; set; }
}


