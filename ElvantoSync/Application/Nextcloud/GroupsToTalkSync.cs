using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Infrastructure.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Nextcloud.Models.Talk;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToTalkSync(
    IElvantoClient elvanto,
    INextcloudTalkClient talkClient,
    DbContext dbContext,
    GroupsToTalkSyncSettings settings,
    ILogger<GroupsToTalkSync> logger
) : Sync<Group, Conversation>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(Conversation i) => i.Token;
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(Conversation i) => i.Name;

    public override async Task<IEnumerable<Group>> GetFromAsync() =>
        (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<Conversation>> GetToAsync() =>
        await talkClient.GetConversations();

    protected override async Task<string> AddMissing(Group group)
    {
        var createdConvo = await talkClient.CreateConversation(2, group.Id, "groups", group.Name);
        await talkClient.SetDescription(createdConvo.Token, settings.GroupChatDescription);
        return ToKeySelector(createdConvo);
    }

    protected override async Task UpdateMatch(Group from, Conversation to)
    {
        if (from.Name != to.Name)
        {
            await talkClient.SetRoomName(to.Token, from.Name);
        }
    }
}