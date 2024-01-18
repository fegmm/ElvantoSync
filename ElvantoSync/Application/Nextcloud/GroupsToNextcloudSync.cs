using ElvantoSync.Extensions;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToNextcloudSync(ElvantoApi.Client elvanto, INextcloudProvisioningClient provisioningClient, Settings settings)
    : Sync<ElvantoApi.Models.Group, Group>(settings)
{
    public override bool IsActive() => settings.SyncNextcloudGroups;
    public override string FromKeySelector(ElvantoApi.Models.Group i) => i.Name;
    public override string ToKeySelector(Group i) => i.Id;

    public override async Task<IEnumerable<ElvantoApi.Models.Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new ElvantoApi.Models.GetAllRequest() { Fields = ["people"] })).Groups.Group;

    public override async Task<IEnumerable<Group>> GetToAsync()
        => (await provisioningClient.GetGroups());

    public override async Task AddMissingAsync(IEnumerable<ElvantoApi.Models.Group> missing)
    {
        await Task.WhenAll(missing.Select(async i =>
        {
            await provisioningClient.CreateGroup(i.Name, i.Name);
            await provisioningClient.CreateGroup(i.Name + Settings.GroupLeaderSuffix, i.Name + Settings.GroupLeaderSuffix);
        }));
    }

    public override async Task ApplyUpdate(IEnumerable<(ElvantoApi.Models.Group, Group)> matches)
    {
        await Task.WhenAll(matches.Select(async match =>
        {
            (ElvantoApi.Models.Group elvantoGroup, Group nextcloudGroup) = match;
            var members = await provisioningClient.GetMembers(nextcloudGroup.Id);
            var compare = elvantoGroup.People.Person.CompareTo(members, i => "Elvanto-" + i.Id, id => id);
            var addMemberRequests = compare.additional.Select(async i =>
            {
                await provisioningClient.AddUserToGroup("Elvanto-" + i.Id, nextcloudGroup.Id);
                if (i.Position == "Leader" || i.Position == "Assistant Leader")
                    await provisioningClient.AddUserToGroup("Elvanto-" + i.Id, nextcloudGroup.Id + Settings.GroupLeaderSuffix);
            });
            var removeMemberRequests = compare.missing.Select(id => provisioningClient.RemoveUserFromGroup(id, nextcloudGroup.Id));
            await Task.WhenAll([.. addMemberRequests, .. removeMemberRequests]);
        }));
    }
}
