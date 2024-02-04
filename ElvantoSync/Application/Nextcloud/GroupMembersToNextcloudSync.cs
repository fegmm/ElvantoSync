using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using Nextcloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Application.Nextcloud;

class GroupMembersToNextcloudSync(IElvantoClient elvanto, INextcloudProvisioningClient provisioningClient, Settings settings)
    : Sync<(string group, string user), GroupMember, string>(settings)
{
    public override async Task<Dictionary<(string group, string user), GroupMember>> GetFromAsync()
    {
        // People that are Contacts are reported as GroupMembers may be contacts, these must not be synced to nextcloud!
        var valid_persons = (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest()))
            .People.Person.Select(i => i.Id);

        var groups = (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } })).Groups.Group;
        var from_group_members = groups
            .Where(i => i.People != null && i.People.Person != null)
            .SelectMany(i => i.People.Person
                .Where(j => valid_persons.Contains(j.Id))
                .Select(j => (i.Name, j)))
            .ToDictionary(i => (i.Name, "Elvanto-" + i.j.Id), i => i.j);

        if (!Settings.SyncNextcloudGroupLeaders)
            return from_group_members;

        var from_group_member_leaders = groups
            .Where(i => i.People != null && i.People.Person != null)
            .SelectMany(i => i.People.Person
                .Where(j => j.Position == "Leader" || j.Position == "Assistant Leader")
                .Where(j => valid_persons.Contains(j.Id))
                .Select(j => (Name: $"{i.Name}{Settings.GroupLeaderSuffix}", j)))
            .ToDictionary(i => (i.Name, "Elvanto-" + i.j.Id), i => i.j);
        return from_group_members.Concat(from_group_member_leaders).ToDictionary(i => i.Key, i => i.Value);
    }

    public override async Task<Dictionary<(string group, string user), string>> GetToAsync()
    {
        var members = new List<(string group, string user)>();
        var elvantoGroups = (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group.Select(i => i.Name);
        var nextcloudGroups = await provisioningClient.GetGroups();

        foreach (var group in nextcloudGroups.Where(i => elvantoGroups.Contains(i.Id.Replace(Settings.GroupLeaderSuffix, ""))).Select(i => i.Id))
            foreach (var user in await provisioningClient.GetMembers(group))
                members.Add((group, user));
        return members.Where(i => i.user.Contains("Elvanto")).ToDictionary(i => i, i => i.group);
    }

    public override async Task AddMissingAsync(Dictionary<(string group, string user), GroupMember> missing)
        => await Task.WhenAll(missing.Select(item => provisioningClient.AddUserToGroup(item.Key.user, item.Key.group)));

    public override async Task RemoveAdditionalAsync(Dictionary<(string group, string user), string> additionals)
        => await Task.WhenAll(additionals.Select(item => provisioningClient.RemoveUserFromGroup(item.Key.user, item.Key.group)));

    public override bool IsActive()
    {
        return settings.SyncNextcloudGroupmembers;
    }
}