using ElvantoSync.ElvantoService;
using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Fegmm.Elvanto.Groups.GetAllJson;
using Fegmm.Elvanto.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElvantoGroup = Fegmm.Elvanto.Models.Group;
using NextcloudGroup = Nextcloud.Models.Provisioning.Group;

namespace ElvantoSync.Nextcloud;

public class GroupsToNextcloudSync(
    IElvantoClient elvanto,
    INextcloudProvisioningClient provisioningClient,
    DbContext dbContext,
    IOptions<PeopleToNextcloudSyncSettings> peopleSettings,
    IOptions<GroupsToNextcloudSyncSettings> settings,
    ILogger<GroupsToNextcloudSync> logger
) : Sync<ElvantoGroup, NextcloudGroup>(dbContext, settings, logger)
{
    public override string FromKeySelector(ElvantoGroup i) => i.Id;
    public override string ToKeySelector(NextcloudGroup i) => i.Id;
    public override string FallbackFromKeySelector(ElvantoGroup i) => i.Name;
    public override string FallbackToKeySelector(NextcloudGroup i) => i.Id;

    public override async Task<IEnumerable<ElvantoGroup>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new() { Fields = [GroupAdditionalFields.People] }))
            .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<NextcloudGroup>> GetToAsync()
        => (await provisioningClient.GetGroups())
        .Where(i => !i.Id.EndsWith(settings.Value.GroupLeaderSuffix))
        .Where(i => i.Id != "admin");

    protected override async Task<string> AddMissing(ElvantoGroup group)
    {
        await provisioningClient.CreateGroup(group.Id, group.Name);

        try
        {
            await provisioningClient.CreateGroup(group.Id + settings.Value.GroupLeaderSuffix, group.Name + settings.Value.GroupLeaderSuffix);
            await UpdateMembersOfGroup(group.People.Person, group.Id);
            await UpdateMembersOfGroup(group.People.Person.Where(IsLeader), group.Id + settings.Value.GroupLeaderSuffix);
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

    protected override async Task UpdateMatch(ElvantoGroup elvantoGroup, NextcloudGroup nextcloudGroup)
    {
        if (elvantoGroup.Name != nextcloudGroup.DisplayName)
        {
            await provisioningClient.EditGroup(nextcloudGroup.Id, elvantoGroup.Name);
            await provisioningClient.EditGroup(nextcloudGroup.Id + settings.Value.GroupLeaderSuffix, elvantoGroup.Name + settings.Value.GroupLeaderSuffix);
        }

        await UpdateMembersOfGroup(elvantoGroup.People.Person, nextcloudGroup.Id);
        try
        {
            await UpdateMembersOfGroup(elvantoGroup.People.Person.Where(IsLeader), nextcloudGroup.Id + settings.Value.GroupLeaderSuffix);
        }
        catch (System.Net.Http.HttpRequestException e)
        {
            if (e.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw;
            }
            await provisioningClient.CreateGroup(nextcloudGroup.Id + settings.Value.GroupLeaderSuffix, elvantoGroup.Name + settings.Value.GroupLeaderSuffix);
            await UpdateMembersOfGroup(elvantoGroup.People.Person.Where(IsLeader), nextcloudGroup.Id + settings.Value.GroupLeaderSuffix);
        }
    }

    private async Task UpdateMembersOfGroup(IEnumerable<GroupMember> members, string nextcloudGroupId)
    {
        var nextcloudMembers = await provisioningClient.GetMembers(nextcloudGroupId);
        var compare = members.CompareTo(nextcloudMembers, i => peopleSettings.Value.IdPrefix + i.Id, id => id);

        foreach (var member in compare.additional)
        {
            try
            {
                await provisioningClient.AddUserToGroup(peopleSettings.Value.IdPrefix + member.Id, nextcloudGroupId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not assign user {userId} to group {groupid}", member.Id, nextcloudGroupId);
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
        => member.Position == GroupMemberPositions.Leader || member.Position == GroupMemberPositions.AssistantLeader;
}
