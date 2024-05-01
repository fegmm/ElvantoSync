using ElvantoSync;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Exceptions;
using ElvantoSync.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Interfaces;
using Nextcloud.Models.GroupFolders;
using System.Collections.Generic;
using System.Linq;
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
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] })).Groups.Group
        .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<GroupFolder>> GetToAsync()
        => await groupFolderClient.GetGroupFolders();

    protected override async Task<string> AddMissing(Group group)
    {
        string nextcloudGroupId = dbContext.ElvantoToNextcloudGroupId(group.Id);
        var groupFolderId = await groupFolderClient.CreateGroupFolder(group.Name);
        try
        {
            await groupFolderClient.AddGroup(groupFolderId, nextcloudGroupId);
            await groupFolderClient.SetPermission(groupFolderId, nextcloudGroupId, Permissions.All);
            await groupFolderClient.SetAcl(groupFolderId, true);
            await groupFolderClient.AddGroup(groupFolderId, nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix);
            await groupFolderClient.AddAclManager(groupFolderId, nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix);
        }
        catch
        {
            await groupFolderClient.DeleteGroupFolder(groupFolderId);
            throw;
        }
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
        string nextcloudGroupId = dbContext.ElvantoToNextcloudGroupId(group.Id);

        if (group.Name != groupFolder.MountPoint)
        {
            await groupFolderClient.SetMountpoint(groupFolder.Id, group.Name);
        }

        if (!groupFolder.Groups.Any(i => i.Key == nextcloudGroupId))
        {
            await groupFolderClient.AddGroup(groupFolder.Id, nextcloudGroupId);
            await groupFolderClient.SetPermission(groupFolder.Id, nextcloudGroupId, Permissions.All);
        }

        if (!groupFolder.Groups.Any(i => i.Key == nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix))
        {
            await groupFolderClient.AddGroup(groupFolder.Id, nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix);
        }

        if (!groupFolder.Acl)
        {
            await groupFolderClient.SetAcl(groupFolder.Id, true);
        }

        if (!groupFolder.Manage.Any(i => i.Id == nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix))
        {
            await groupFolderClient.AddAclManager(groupFolder.Id, nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix);
        }
    }
}