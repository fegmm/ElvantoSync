using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud
{
    class GroupMembersToNextcloudSync : Sync<(string group, string user), GroupMember, string>
    {
        private readonly Client elvanto;
        private readonly NextcloudApi.Api nextcloud;

        public GroupMembersToNextcloudSync(ElvantoApi.Client elvantoApi, NextcloudApi.Api nextcloud)
        {
            this.elvanto = elvantoApi;
            this.nextcloud = nextcloud;
        }

        public override async Task<Dictionary<(string group, string user), GroupMember>> GetFromAsync()
        {
            return (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } })).Groups.Group
                .Where(i => i.People != null && i.People.Person != null)
                .SelectMany(i => i.People.Person.Select(j => (i.Name, j)))
                .ToDictionary(i => (i.Name, "Elvanto-" + i.j.Id), i => i.j);
        }

        public override async Task<Dictionary<(string group, string user), string>> GetToAsync()
        {
            var members = new List<(string group, string user)>();
            var elvantoGroups = (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group.Select(i => i.Name);
            foreach (var group in (await NextcloudApi.Group.List(nextcloud)).All(nextcloud).Where(i => elvantoGroups.Contains(i)))
                foreach (var user in (await NextcloudApi.Group.GetMembers(nextcloud, group)).List)
                    members.Add((group, user));
            return members.Where(i => i.user.Contains("Elvanto")).ToDictionary(i => i, i => i.group);
        }

        public override async Task AddMissingAsync(Dictionary<(string group, string user), GroupMember> missing)
        {
            await Task.WhenAll(missing.Select(item => NextcloudApi.User.AddToGroup(nextcloud, item.Key.user, item.Key.group)));
        }

        public override async Task RemoveAdditionalAsync(Dictionary<(string group, string user), string> additionals)
        {
            await Task.WhenAll(additionals.Select(item => NextcloudApi.User.RemoveFromGroup(nextcloud, item.Key.user, item.Key.group)));
        }
    }
}