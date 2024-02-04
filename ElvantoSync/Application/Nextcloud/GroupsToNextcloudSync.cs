using ElvantoSync.ElvantoService;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Application.Nextcloud;

public class GroupsToNextcloudSync(IElvantoClient elvanto, INextcloudProvisioningClient provisioningClient, Settings settings)
    : Sync<string, ElvantoApi.Models.Group, Group>(settings)
{
    public override async Task<Dictionary<string, ElvantoApi.Models.Group>> GetFromAsync()
    {
        var groups = (await elvanto.GroupsGetAllAsync(new ElvantoApi.Models.GetAllRequest())).Groups.Group;
        var from_groups = groups.ToDictionary(i => i.Name, i => i);
        if (!Settings.SyncNextcloudGroupLeaders)
            return from_groups;

        var from_leader_groups = groups.ToDictionary(i => $"{i.Name}{Settings.GroupLeaderSuffix}", i => i);
        return from_groups.Concat(from_leader_groups).ToDictionary(i => i.Key, i => i.Value);
    }

    public override async Task<Dictionary<string, Group>> GetToAsync()
    {
        return (await provisioningClient.GetGroups()).ToDictionary(i => i.Id);
    }

    public override async Task AddMissingAsync(Dictionary<string, ElvantoApi.Models.Group> missing)
    {
        await Task.WhenAll(missing.Select(i => provisioningClient.CreateGroup(i.Key, i.Key)));
    }
    public override bool IsActive()
    {
        return settings.SyncNextcloudGroups;
    }
}
