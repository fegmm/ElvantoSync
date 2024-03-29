using ElvantoSync;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Exceptions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Interfaces;
using Nextcloud.Models.GroupFolders;
using System.Collections.Generic;
using System.Threading.Tasks;

class GroupsToNextcloudGroupFolderSync(
    IElvantoClient elvanto,
    INextcloudGroupFolderClient groupFolderClient,
    DbContext dbContext,
    IOptions<GroupsToNextcloudGroupFolderSyncSettings> settings,
    IOptions<GroupsToNextcloudSyncSettings> groupSettings,
    ILogger<GroupsToNextcloudGroupFolderSync> logger
) : Sync<Group, GroupFolder>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(GroupFolder i) => i.Id.ToString();
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(GroupFolder i) => i.MountPoint;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<GroupFolder>> GetToAsync()
        => await groupFolderClient.GetGroupFolders();

    protected override async Task<string> AddMissing(Group group)
    {
        var groupFolderId = await groupFolderClient.CreateGroupFolder(group.Name);
        await groupFolderClient.AddGroup(groupFolderId, group.Name);
        await groupFolderClient.SetPermission(groupFolderId, group.Name, Permissions.All);
        await groupFolderClient.SetAcl(groupFolderId, true);
        await groupFolderClient.AddAclManager(groupFolderId, group.Name + groupSettings.Value.GroupLeaderSuffix);
        return groupFolderId.ToString();
    }

    protected override async Task RemoveAdditional(GroupFolder groupFolder)
    {
        if (groupFolder.Size > 0)
        {
            throw new ContainsDataException($"Group folder {groupFolder.Id} is not empty and will not be deleted");
        }

        await groupFolderClient.DeleteGroupFolder(groupFolder.Id);
    }

    protected override async Task UpdateMatch(Group group, GroupFolder groupFolder)
    {
        if (group.Name != groupFolder.MountPoint)
        {
            await groupFolderClient.SetMountpoint(groupFolder.Id, group.Name);
        }
    }
}