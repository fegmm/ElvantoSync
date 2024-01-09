using Nextcloud.Models;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Talk;
using System.Net.Http.Json;

namespace ElvantoSync.Infrastructure.Nextcloud;

public class NextcloudTalkClient(HttpClient client) : INextcloudTalkClient
{

    private readonly string basePath = $"/ocs/v2.php/apps/spreed/api/v4/room";
   

    public async Task CreateConversation(int roomType, string invite, string source, string roomName)
    {
          var reqBody = new { roomType = roomType, invite = invite, source = source, roomName = roomName};
            var response = await client.PostAsJsonAsync(basePath, reqBody);
         var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<Conversation>>();
         response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Conversation>> GetConversations()
    {   var response = await client.GetAsync(basePath);
         var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<OCSResponse<IEnumerable<Conversation>>>();
         return result.Ocs.Data;
    }
}