using ElvantoSync.ElvantoService;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Elvanto;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Application.Elvanto;

class DepartementsToGroupMemberSync(
    IElvantoClient elvanto,
    DbContext dbContext,
    DepartementsToGroupMemberSyncSettings settings,
    ILogger<DepartementsToGroupMemberSync> logger
) : Sync<(Person person, Group group), (GroupMember member, Group group)>(dbContext, settings, logger)
{
    public override string FromKeySelector((Person person, Group group) i) => (i.person.Id, i.group.Id).ToString();
    public override string ToKeySelector((GroupMember member, Group group) i) => (i.member.Id, i.group.Id).ToString();
    public override string FallbackFromKeySelector((Person person, Group group) i) => (i.person.Id, i.group.Name).ToString();
    public override string FallbackToKeySelector((GroupMember member, Group group) i) => (i.member.Id, i.group.Name).ToString();

    public override async Task<IEnumerable<(Person, Group)>> GetFromAsync()
    {
        var response = await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest() { Fields = ["departments"] });
        var groups = await elvanto.GroupsGetAllAsync(new GetAllRequest());
        var groupNameToGroup = groups.Groups.Group.ToDictionary(i => i.Name);

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
            .Where(i => groups.Groups.Group.Any(j => j.Name == i.Name))
            .Select(i => (i.person, groupNameToGroup[i.Name]));
    }

    public override async Task<IEnumerable<(GroupMember member, Group group)>> GetToAsync()
    {
        var departments = new HashSet<string>((await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest() { Fields = ["departments"] })).People.Person
            .Where(i => i.Departments != null)
            .SelectMany(person => person.Departments.Department
                .SelectMany(department => department.Sub_departments.Sub_department
                    .SelectMany(sub => sub.Positions.Position.Select(pos => pos.Name))
                    .Concat(department.Sub_departments.Sub_department.Select(sub => sub.Name))
                )
                .Concat(person.Departments.Department.Select(department => department.Name))
            )
            .Distinct());

        var response = await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] });
        return response.Groups.Group
            .Where(i => i.People != null && i.People.Person != null)
            .Where(i => departments.Contains(i.Name))
            .SelectMany(group => group.People.Person
                .Select(member => (member, group))
            );
    }

    protected override async Task<string> AddMissing((Person person, Group group) missing)
    {
        await elvanto.GroupsAddPersonAsync(missing.group.Id, missing.person.Id);
        return FromKeySelector(missing);
    }

    protected override async Task RemoveAdditional((GroupMember member, Group group) additional)
    {
        await elvanto.GroupsRemovePersonAsync(additional.group.Id, additional.member.Id);
    }
}
