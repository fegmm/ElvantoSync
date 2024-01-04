using ElvantoSync.ElvantoApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToNextcloudSync(ElvantoApi.Client elvanto, NextcloudApi.Api nextcloud, Settings settings) : Sync<string, Group, string>(settings)
{
    public override async Task<Dictionary<string, Group>> GetFromAsync()
    {
        var groups = (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;
        var from_groups = groups.ToDictionary(i => i.Name, i => i);
        if (!Settings.SyncNextcloudGroupLeaders)
            return from_groups;

        var from_leader_groups = groups.ToDictionary(i => $"{i.Name}{Settings.GroupLeaderSuffix}", i => i);
        return from_groups.Concat(from_leader_groups).ToDictionary(i => i.Key, i => i.Value);
    }

    public override async Task<Dictionary<string, string>> GetToAsync()
    {
        return (await NextcloudApi.Group.List(nextcloud)).All(nextcloud).ToDictionary(i => i);
    }

    public override async Task AddMissingAsync(Dictionary<string, Group> missing)
    {
        await Task.WhenAll(missing.Select(i => NextcloudApi.Group.Create(nextcloud, i.Key)));
    }
}
