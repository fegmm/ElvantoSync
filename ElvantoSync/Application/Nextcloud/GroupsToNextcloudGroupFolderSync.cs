using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nextcloud.Interfaces;
using Nextcloud.Models.GroupFolders;
using ElvantoSync;
using ElvantoSync.Settings.Nextcloud;


class GroupsToNextcloudGroupFolderSync(
    Client elvanto,
    INextcloudGroupFolderClient groupFolderClient,
    GroupsToNextcloudGroupFolderSyncSettings settings,
    GroupsToNextcloudSyncSettings groupSettings
) : Sync<Group, GroupFolder>(settings)
{
    public override string FromKeySelector(Group i) => i.Name;
    public override string ToKeySelector(GroupFolder i) => i.MountPoint;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<GroupFolder>> GetToAsync()
        => await groupFolderClient.GetGroupFolders();

    public override async Task AddMissingAsync(IEnumerable<Group> missing)
    {
        var requests = missing.Select(async i =>
        {
            var groupFolder = await groupFolderClient.CreateGroupFolder(i.Name);
            await groupFolderClient.AddGroup(groupFolder, i.Name);
            await groupFolderClient.SetPermission(groupFolder, i.Name, Permissions.All);
            await groupFolderClient.SetAcl(groupFolder, true);
            await groupFolderClient.AddAclManager(groupFolder, i.Name + groupSettings.GroupLeaderSuffix);
        });

        await Task.WhenAll(requests);
    }
}