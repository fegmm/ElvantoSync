using ElvantoSync.ChurchTools;
using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using ElvantoSync.ChurchTools.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SourceGroup = ElvantoSync.ChurchTools.Models.Group;
using NextcloudGroup = Nextcloud.Models.Provisioning.Group;

namespace ElvantoSync.Nextcloud;

public class GroupsToNextcloudSync(
    IChurchToolsSource churchTools,
    INextcloudProvisioningClient provisioningClient,
    DbContext dbContext,
    IOptions<GroupsToNextcloudSyncSettings> settings,
    ILogger<GroupsToNextcloudSync> logger
) : Sync<SourceGroup, NextcloudGroup>(dbContext, settings, logger)
{
    public override string FromKeySelector(SourceGroup i) => i.Id;
    public override string ToKeySelector(NextcloudGroup i) => i.Id;
    public override string FallbackFromKeySelector(SourceGroup i) => i.Name;
    public override string FallbackToKeySelector(NextcloudGroup i) => i.DisplayName;

    public override async Task<IEnumerable<SourceGroup>> GetFromAsync()
        => (await churchTools.GetGroupsAsync())
            .Where(i => i.Members.Any());

    public override async Task<IEnumerable<NextcloudGroup>> GetToAsync()
        => (await provisioningClient.GetGroups())
        .Where(i => !i.Id.EndsWith(settings.Value.GroupLeaderSuffix))
        .Where(i => i.Id != "admin");

    protected override async Task<string> AddMissing(SourceGroup group)
    {
        await provisioningClient.CreateGroup(group.Id, group.Name);

        try
        {
        await provisioningClient.CreateGroup(group.Id + settings.Value.GroupLeaderSuffix, group.Name + settings.Value.GroupLeaderSuffix);
        await UpdateMembersOfGroup(group.Members, group.Id);
        await UpdateMembersOfGroup(group.Members.Where(IsLeader), group.Id + settings.Value.GroupLeaderSuffix);
        }
        catch { } // Ignore errors, as the group will be updated in the next sync and no rollback is needed

        return group.Id;
    }

    protected override async Task RemoveAdditional(NextcloudGroup group)
    {
        try
        {
            await provisioningClient.DeleteGroup(group.Id);
        }
        finally
        {
            await provisioningClient.DeleteGroup(group.Id + settings.Value.GroupLeaderSuffix);
        }
    }

    protected override async Task UpdateMatch(SourceGroup sourceGroup, NextcloudGroup nextcloudGroup)
    {
        if (sourceGroup.Name != nextcloudGroup.DisplayName)
        {
            await provisioningClient.EditGroup(nextcloudGroup.Id, sourceGroup.Name);
            await provisioningClient.EditGroup(nextcloudGroup.Id + settings.Value.GroupLeaderSuffix, sourceGroup.Name + settings.Value.GroupLeaderSuffix);
        }

        await UpdateMembersOfGroup(sourceGroup.Members, nextcloudGroup.Id);
        try
        {
            await UpdateMembersOfGroup(sourceGroup.Members.Where(IsLeader), nextcloudGroup.Id + settings.Value.GroupLeaderSuffix);
        }
        catch (System.Net.Http.HttpRequestException e)
        {
            if (e.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw;
            }
            await provisioningClient.CreateGroup(nextcloudGroup.Id + settings.Value.GroupLeaderSuffix, sourceGroup.Name + settings.Value.GroupLeaderSuffix);
            await UpdateMembersOfGroup(sourceGroup.Members.Where(IsLeader), nextcloudGroup.Id + settings.Value.GroupLeaderSuffix);
        }
    }

    private async Task UpdateMembersOfGroup(IEnumerable<GroupMember> members, string nextcloudGroupId)
    {
        var nextcloudMembers = await provisioningClient.GetMembers(nextcloudGroupId);
        var mappedMembers = members.Select(m => dbContext.ChurchToolsToNextcloudPeopleId(m.Id)).ToList();
        if (mappedMembers.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"Cannot synchronize membership of '{nextcloudGroupId}' because a Nextcloud user mapping is missing.");
        }
        var compare = mappedMembers.CompareTo(nextcloudMembers, id => id, id => id);

        foreach (var member in compare.additional)
        {
            try
            {
                await provisioningClient.AddUserToGroup(member, nextcloudGroupId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not assign user {userId} to group {groupid}", member, nextcloudGroupId);
            }
        }

        foreach (var id in compare.missing)
        {
            try
            {
                await provisioningClient.RemoveUserFromGroup(id, nextcloudGroupId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not remove user {userId} from group {groupid}", id, nextcloudGroupId);
            }
        }
    }

    public static bool IsLeader(GroupMember member)
        => member.IsLeader;
}
