using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using NextcloudApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud
{
    class GroupsToNextcloudGroupFolderSync : Sync<string, string, NextcloudApi.GroupFolder>
    {
        private readonly Client elvanto;
        private readonly NextcloudApi.Api nextcloud;

        public GroupsToNextcloudGroupFolderSync(Client elvantoApi, NextcloudApi.Api nextcloud)
        {
            this.elvanto = elvantoApi;
            this.nextcloud = nextcloud;
        }

        public override async Task<Dictionary<string, string>> GetFromAsync()
        {
            return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
                .Groups.Group.ToDictionary(i => i.Name, i => i.Name);
        }

        public override async Task<Dictionary<string, NextcloudApi.GroupFolder>> GetToAsync()
        {
            return (await NextcloudApi.GroupFolder.List(nextcloud)).List.ToDictionary(i => i.mount_point);
        }

        public override async Task AddMissingAsync(Dictionary<string, string> missing)
        {
            await Task.WhenAll(missing.Select(i => NextcloudApi.GroupFolder.Create(nextcloud, i.Key)));

            var foldersToId = (await GroupFolder.List(nextcloud)).List.ToDictionary(i => i.mount_point, i => i.id);
            await Task.WhenAll(missing.Select(i => GroupFolder.AddGroup(nextcloud, foldersToId[i.Key], i.Key)));
            await Task.WhenAll(missing.Select(i => GroupFolder.SetPermissions(nextcloud, foldersToId[i.Key], i.Key, GroupFolder.Permissions.All)));
        }
    }
}
