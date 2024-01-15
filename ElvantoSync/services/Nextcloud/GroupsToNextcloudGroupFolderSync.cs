using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nextcloud.Interfaces;
using Nextcloud.Models.GroupFolders;


class GroupsToNextcloudGroupFolderSync(Client elvanto, INextcloudGroupFolderClient groupFolderClient, Settings settings)
    : Sync<string, Group, GroupFolder>(settings)
{
    public override async Task<Dictionary<string, Group>> GetFromAsync()
    {
        return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
            .Groups.Group.ToDictionary(i => i.Name, i => i);
    }

    public override async Task<Dictionary<string, GroupFolder>> GetToAsync()
    {
        var groupFolders = await groupFolderClient.GetGroupFolders();
        return groupFolders.ToDictionary(i => i.MountPoint, i => i);
    }

    public override async Task AddMissingAsync(Dictionary<string, Group> missing)
    {
        var requests = missing.Select(async i =>
        {
            var groupFolder = await groupFolderClient.CreateGroupFolder(i.Key);
            await groupFolderClient.AddGroup(groupFolder, i.Key);
            await groupFolderClient.SetPermission(groupFolder, i.Key, Permissions.All);
            await groupFolderClient.SetAcl(groupFolder, true);
            await groupFolderClient.AddAclManager(groupFolder, i.Key + Settings.GroupLeaderSuffix);
        });

        await Task.WhenAll(requests);
    }

    public override bool IsActive()
    {
        return settings.SyncNextcloudGroupfolders;
    }
}