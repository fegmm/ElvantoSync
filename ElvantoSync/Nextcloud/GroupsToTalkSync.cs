using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Infrastructure.Nextcloud;
using Nextcloud.Interfaces;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Collectives;
using Nextcloud.Models.Talk;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToTalkSync(
    Client elvanto,
    INextcloudTalkClient talkRepo,
    Settings settings
) : Sync<string, string, Conversation>(settings)
{
    public override async Task<Dictionary<string, string>> GetFromAsync()
    {
        return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
           .Groups.Group.ToDictionary(i => i.Name, i => i.Name); ;
    }

    public override async Task<Dictionary<string, Conversation>> GetToAsync()
    {
        var response = await talkRepo.GetConversations();
        return response.ToDictionary(i => i.Name);
    }

    public override async Task AddMissingAsync(Dictionary<string, string> missing)
    {
        var createCollectivesWithMembersTasks = missing.Keys.Select(group => talkRepo.CreateConversation(2,group,"groups",group));
        await Task.WhenAll(createCollectivesWithMembersTasks);
    }
    public override bool IsActive()
    {
        return settings.SyncNextCloudTalk;
    }
}