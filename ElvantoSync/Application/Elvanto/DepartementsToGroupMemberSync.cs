using ElvantoSync.ElvantoApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Elvanto;

class DepartementsToGroupMemberSync(ElvantoApi.Client elvanto, Settings settings) : Sync<(Person person, string departement), (GroupMember member, string group)>(settings)
{
    public override bool IsActive() => settings.SyncElvantoDepartementsToGroups;
    public override string FromKeySelector((Person person, string departement) i) => (i.person.Id, i.departement).ToString();
    public override string ToKeySelector((GroupMember member, string group) i) => (i.member.Id, i.group).ToString();

    public override async Task<IEnumerable<(Person, string)>> GetFromAsync()
    {
        var response = await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest() { Fields = ["departments"] });
        var groups = await elvanto.GroupsGetAllAsync(new GetAllRequest());

        return response.People.Person
            .Where(i => i.Departments != null)
            .SelectMany(person => person.Departments.Department
                .SelectMany(department => department.Sub_departments.Sub_department
                    .SelectMany(sub => sub.Positions.Position.Select(pos => (person, pos.Name)))
                    .Concat(department.Sub_departments.Sub_department.Select(sub => (person, sub.Name)))
                )
                .Concat(person.Departments.Department.Select(department => (person, department.Name)))
            )
            .Distinct()
            .Where(i => groups.Groups.Group.Any(j => j.Name == i.Name));
    }

    public override async Task<IEnumerable<(GroupMember member, string group)>> GetToAsync()
    {
        var departments = new HashSet<string>((await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest() { Fields = new[] { "departments" } })).People.Person
            .Where(i => i.Departments != null)
            .SelectMany(person => person.Departments.Department
                .SelectMany(department => department.Sub_departments.Sub_department
                    .SelectMany(sub => sub.Positions.Position.Select(pos => pos.Name))
                    .Concat(department.Sub_departments.Sub_department.Select(sub => sub.Name))
                )
                .Concat(person.Departments.Department.Select(department => department.Name))
            )
            .Distinct());

        var response = await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } });
        return response.Groups.Group
            .Where(i => i.People != null && i.People.Person != null)
            .Where(i => departments.Contains(i.Name))
            .SelectMany(group => group.People.Person
                .Select(member => (member, group.Name))
            );
    }

    public override async Task AddMissingAsync(IEnumerable<(Person person, string departement)> missing)
    {
        var response = await elvanto.GroupsGetAllAsync(new GetAllRequest());
        var nameToIdDict = response.Groups.Group.ToDictionary(i => i.Name, i => i.Id);
        var count = missing.Where(i => nameToIdDict.ContainsKey(i.departement));

        await Task.WhenAll(missing.Where(i => nameToIdDict.ContainsKey(i.departement))
                                  .Select(i => elvanto.GroupsAddPersonAsync(nameToIdDict[i.departement], i.person.Id)));
    }

    public override async Task RemoveAdditionalAsync(IEnumerable<(GroupMember member, string group)> additionals)
    {
        var response = await elvanto.GroupsGetAllAsync(new GetAllRequest());
        var nameToIdDict = response.Groups.Group.ToDictionary(i => i.Name, i => i.Id);
        await Task.WhenAll(additionals.Select(i => elvanto.GroupsRemovePersonAsync(nameToIdDict[i.group], i.member.Id)));
    }
}
