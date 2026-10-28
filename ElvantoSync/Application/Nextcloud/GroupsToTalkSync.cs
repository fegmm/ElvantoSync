using ElvantoSync.ChurchTools;
using ElvantoSync.Infrastructure.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using ElvantoSync.ChurchTools.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Models.Talk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToTalkSync(
    IChurchToolsSource churchTools,
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
        (await churchTools.GetGroupsAsync())
           .Where(i => i.Members.Any());

    public override async Task<IEnumerable<Conversation>> GetToAsync() =>
        await talkClient.GetConversations();

    protected override async Task<string> AddMissing(Group group)
    {
        string nextcloudGroupId = dbContext.ChurchToolsToNextcloudGroupId(group.Id);

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

        var participants = await talkClient.GetListOfParticipants(to.Token);
        
        var leaderIds = from.Members.Where(GroupsToNextcloudSync.IsLeader)
            .Select(i => dbContext.ChurchToolsToNextcloudPeopleId(i.Id))
            .ToHashSet();
        var leaderAttendeeIds = participants.Where(i => leaderIds.Contains(i.ActorId))
            .Select(i => i.AttendeeId);

        var moderatorsAttendeeIds = participants.Where(IsModerator)
            .Select(i => i.AttendeeId)
            .ToHashSet();
        
        foreach (var attendeeId in leaderAttendeeIds.Except(moderatorsAttendeeIds))
        {
            await talkClient.PromoteToModerator(to.Token, attendeeId);
        }
    }

    protected override async Task RemoveAdditional(Conversation conversation)
        => await talkClient.DeleteConversation(conversation.Token);

    private static bool IsModerator(Participant participant) => participant.ParticipantType == 2;
}