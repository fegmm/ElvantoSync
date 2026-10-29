using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ElvantoSync.ChurchTools.Models;
using Fegmm.ChurchTools;
using Microsoft.Kiota.Serialization;

namespace ElvantoSync.ChurchTools;

// One snapshot per job scope keeps all downstream syncs on the same source data.
public sealed class ChurchToolsSource(ChurchToolsClient client) : IChurchToolsSource
{
    private Task<IEnumerable<Person>> people;
    private Task<IEnumerable<Group>> groups;

    public Task<IEnumerable<Person>> GetPeopleAsync() => people ??= LoadPeopleAsync();
    public Task<IEnumerable<Group>> GetGroupsAsync() => groups ??= LoadGroupsAsync();

    private async Task<IEnumerable<Person>> LoadPeopleAsync()
    {
        List<Person> result = [];
        int pages = 1;
        for (int page = 1; page <= pages; page++)
        {
            var response = await client.Persons.GetAsPersonsGetResponseAsync(config =>
            {
                config.QueryParameters.Limit = 200;
                config.QueryParameters.Page = page;
                config.QueryParameters.IsArchived = false;
            });
            pages = response?.Meta?.Pagination?.LastPage
                ?? throw new InvalidOperationException("ChurchTools persons response is missing pagination.");
            var data = response.Data
                ?? throw new InvalidOperationException("ChurchTools persons response is missing data.");
            result.AddRange(data.Where(p => p.IsArchived != true && p.IsDead != true).Select(p => new Person
            {
                Id = Id(p.Id),
                AreaIds = p.DepartmentIds?.Where(id => id.HasValue).Select(id => id.Value).ToArray() ?? [],
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                PhonePrivate = p.PhonePrivate,
                Mobile = p.Mobile,
                ImageUrl = p.ImageUrl,
                Birthday = p.Birthday?.DateTime,
                LastEditedDate = p.LastEditedDate,
            }));
        }
        return result;
    }

    private async Task<IEnumerable<Group>> LoadGroupsAsync()
    {
        var persons = (await GetPeopleAsync()).ToDictionary(p => p.Id);
        var roles = await client.Group.Roles.GetAsRolesGetResponseAsync()
            ?? throw new InvalidOperationException("ChurchTools roles response is missing.");
        // The generated client exposes roles as an untyped node.
        using var roleJson = JsonDocument.Parse(await roles.SerializeAsStringAsync("application/json"));
        var roleData = roleJson.RootElement.GetProperty("data");
        var roleItems = roleData.ValueKind == JsonValueKind.Array
            ? roleData.EnumerateArray().ToArray()
            : roleData.EnumerateObject().Select(p => p.Value).ToArray();
        var leaderRoles = roleItems
            .Where(r => r.GetProperty("isLeader").GetBoolean())
            .Select(r => r.GetProperty("id").GetInt32())
            .ToHashSet();

        var memberResponse = await client.Groups.Members.GetAsMembersGetResponseAsync(config =>
            config.QueryParameters.WithDeleted = false);
        var members = memberResponse?.Data
            ?? throw new InvalidOperationException("ChurchTools members response is missing data.");
        var activeMembers = members
            .Where(m => m.Deleted != true && m.GroupMemberStatus?.String == "active")
            .Where(m => persons.ContainsKey(Id(m.PersonId)))
            .ToLookup(m => m.GroupId);

        List<Group> result = [];
        int pages = 1;
        for (int page = 1; page <= pages; page++)
        {
            var response = await client.Groups.GetAsGroupsGetResponseAsync(config =>
            {
                config.QueryParameters.Limit = 200;
                config.QueryParameters.Page = page;
            });
            pages = response?.Meta?.Pagination?.LastPage
                ?? throw new InvalidOperationException("ChurchTools groups response is missing pagination.");
            var data = response.Data
                ?? throw new InvalidOperationException("ChurchTools groups response is missing data.");
            result.AddRange(data.Select(g => new Group
            {
                Id = Id(g.Id),
                Name = g.Name,
                Members = activeMembers[g.Id].Select(m => new GroupMember
                {
                    Id = Id(m.PersonId),
                    Email = persons[Id(m.PersonId)].Email,
                    IsLeader = m.GroupTypeRoleId.HasValue && leaderRoles.Contains(m.GroupTypeRoleId.Value),
                }).ToList(),
            }));
        }
        return result;
    }

    private static string Id(int? id) => id?.ToString(CultureInfo.InvariantCulture)
        ?? throw new InvalidOperationException("ChurchTools response contains an entity without an ID.");
}
