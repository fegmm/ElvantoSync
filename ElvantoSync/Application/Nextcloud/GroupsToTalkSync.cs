using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Infrastructure.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Models.Talk;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToTalkSync(
    IElvantoClient elvanto,
    INextcloudTalkClient talkClient,
    DbContext dbContext,
    IOptions<GroupsToTalkSyncSettings> settings,
    ILogger<GroupsToTalkSync> logger
) : Sync<Group, Conversation>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(Conversation i) => i.Token;
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(Conversation i) => i.Name;

    public override async Task<IEnumerable<Group>> GetFromAsync() =>
        (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] })).Groups.Group
           .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<Conversation>> GetToAsync() =>
        await talkClient.GetConversations();

    protected override async Task<string> AddMissing(Group group)
    {
        string nextcloudGroupId = dbContext.ElvantoToNextcloudGroupId(group.Id);

        var createdConvo = await talkClient.CreateConversation(2, nextcloudGroupId, "groups", group.Name);
        try
        {
            await talkClient.SetDescription(createdConvo.Token, settings.Value.GroupChatDescription);
        }
        catch
        {
            await talkClient.DeleteConversation(createdConvo.Token);
            throw;
        }
        return ToKeySelector(createdConvo);
    }

    protected override async Task UpdateMatch(Group from, Conversation to)
    {
        if (from.Name != to.Name)
        {
            await talkClient.SetRoomName(to.Token, from.Name);
        }
    }

    protected override async Task RemoveAdditional(Conversation conversation)
        => await talkClient.DeleteConversation(conversation.Token);
}