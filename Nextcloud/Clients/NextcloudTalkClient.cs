using Nextcloud.Models;
using Nextcloud.Models.Talk;
using System.Net.Http.Json;

namespace ElvantoSync.Infrastructure.Nextcloud;

public class NextcloudTalkClient(HttpClient client) : INextcloudTalkClient
{

    private readonly string basePath = $"/ocs/v2.php/apps/spreed/api/v4/room";


    public async Task<Conversation> CreateConversation(int roomType, string invite, string source, string roomName)
    {
        var reqBody = new { roomType = roomType, invite = invite, source = source, roomName = roomName };
        var response = await client.PostAsJsonAsync(basePath, reqBody);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OCSResponse<Conversation>>();
        return result!.Ocs.Data;
    }

    public async Task DeleteConversation(string token)
    {
        var response = await client.DeleteAsync($"{basePath}/{token}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Conversation>> GetConversations()
    {
        var response = await client.GetAsync(basePath);
        var result = await response.EnsureSuccessStatusCode()
           .Content.ReadFromJsonAsync<OCSResponse<IEnumerable<Conversation>>>();
        return result!.Ocs.Data;

    }

    public async Task SetDescription(string token, string description)
    {
        var reqBody = new { description = description };
        var response = await client.PutAsJsonAsync($"{basePath}/{token}/description", reqBody);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetRoomName(string token, string name)
    {
        var reqBody = new { roomName = name };
        var response = await client.PutAsJsonAsync($"{basePath}/{token}", reqBody);
        response.EnsureSuccessStatusCode();
    }

    public async Task PromoteToModerator(string token, int attendeeId)
    {
        var reqBody = new { attendeeId = attendeeId };
        var response = await client.PostAsJsonAsync($"{basePath}/{token}/moderators", reqBody);
        response.EnsureSuccessStatusCode();
    }

    public async Task DemoteFromModerator(string token, int attendeeId)
    {
        var response = await client.DeleteAsync($"{basePath}/{token}/moderators?attendeeId={attendeeId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Participant>> GetListOfParticipants(string token)
    {
        var response = await client.GetAsync($"{basePath}/{token}/participants");
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<IEnumerable<Participant>>>();
        return result!.Ocs.Data;
    }

    public async Task AddGroupToRoom(string token, string groupId)
    {
        var reqBody = new { newParticipant = groupId, source = "groups" };
        var response = await client.PostAsJsonAsync($"{basePath}/{token}/participants", reqBody);
        response.EnsureSuccessStatusCode();
    }
}