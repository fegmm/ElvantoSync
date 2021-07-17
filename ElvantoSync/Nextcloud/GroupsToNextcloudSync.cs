using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud
{
    class GroupsToNextcloudSync : Sync<string, Group, string>
    {
        private readonly Client elvanto;
        private readonly NextcloudApi.Api nextcloud;

        public GroupsToNextcloudSync(ElvantoApi.Client elvantoApi, NextcloudApi.Api nextcloud)
        {
            this.elvanto = elvantoApi;
            this.nextcloud = nextcloud;
        }

        public override async Task<Dictionary<string, Group>> GetFromAsync()
        {
            return (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group
                .ToDictionary(i => i.Name, i => i);
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
}
