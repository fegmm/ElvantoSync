using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToNextcloudSync(
    ElvantoApi.Client elvanto,
    INextcloudProvisioningClient provisioningClient,
    DbContext dbContext,
    PeopleToNextcloudSyncSettings peopleSettings,
    GroupsToNextcloudSyncSettings settings,
    ILogger<GroupsToNextcloudSync> logger
) : Sync<ElvantoApi.Models.Group, Group>(dbContext, settings, logger)
{
    public override string FromKeySelector(ElvantoApi.Models.Group i) => i.Id;
    public override string ToKeySelector(Group i) => i.Id;
    public override string FallbackFromKeySelector(ElvantoApi.Models.Group i) => i.Name;
    public override string FallbackToKeySelector(Group i) => i.Id;

    public override async Task<IEnumerable<ElvantoApi.Models.Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new ElvantoApi.Models.GetAllRequest() { Fields = ["people"] })).Groups.Group;

    public override async Task<IEnumerable<Group>> GetToAsync()
        => (await provisioningClient.GetGroups());

    protected override async Task<string> AddMissing(ElvantoApi.Models.Group group)
    {
        await Task.WhenAll(
            provisioningClient.CreateGroup(group.Id, group.Name),
            provisioningClient.CreateGroup(group.Id + settings.GroupLeaderSuffix, group.Name + settings.GroupLeaderSuffix)
        );
        return group.Id;
    }

    protected override async Task RemoveAdditional(Group group)
        => await Task.WhenAll([
            provisioningClient.DeleteGroup(group.Id),
            provisioningClient.DeleteGroup(group.Id + settings.GroupLeaderSuffix)
        ]);

    protected override async Task UpdateMatch(ElvantoApi.Models.Group elvantoGroup, Group nextcloudGroup)
    {
        if (elvantoGroup.Name != nextcloudGroup.Id)
        {
            await provisioningClient.EditGroup(nextcloudGroup.Id, elvantoGroup.Name);
            await provisioningClient.EditGroup(nextcloudGroup.Id + settings.GroupLeaderSuffix, elvantoGroup.Name + settings.GroupLeaderSuffix);
        }

        var members = await provisioningClient.GetMembers(nextcloudGroup.Id);
        var compare = elvantoGroup.People.Person.CompareTo(members, i => peopleSettings.IdPrefix + i.Id, id => id);
        var addMemberRequests = compare.additional.Select(async i =>
        {
            await provisioningClient.AddUserToGroup(peopleSettings.IdPrefix + i.Id, nextcloudGroup.Id);
            if (i.Position == "Leader" || i.Position == "Assistant Leader")
                await provisioningClient.AddUserToGroup(
                    peopleSettings.IdPrefix + i.Id,
                    nextcloudGroup.Id + settings.GroupLeaderSuffix
                );
        });
        var removeMemberRequests = compare.missing.Select(id => provisioningClient.RemoveUserFromGroup(id, nextcloudGroup.Id));
        await Task.WhenAll([.. addMemberRequests, .. removeMemberRequests]);
    }
}
