using ElvantoSync.ElvantoService;
using Fegmm.Elvanto.Models;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Elvanto;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Fegmm.Elvanto.Groups.GetAllJson;

namespace ElvantoSync.Application.Elvanto;

class DepartementsToGroupMemberSync(
    IElvantoClient elvanto,
    DbContext dbContext,
    IOptions<DepartementsToGroupMemberSyncSettings> settings,
    ILogger<DepartementsToGroupMemberSync> logger
) : Sync<(Person person, Group group), (GroupMember member, Group group)>(dbContext, settings, logger)
{
    public override string FromKeySelector((Person person, Group group) i) => (i.person.Id, i.group.Id).ToString();
    public override string ToKeySelector((GroupMember member, Group group) i) => (i.member.Id, i.group.Id).ToString();
    public override string FallbackFromKeySelector((Person person, Group group) i) => (i.person.Id, i.group.Name).ToString();
    public override string FallbackToKeySelector((GroupMember member, Group group) i) => (i.member.Id, i.group.Name).ToString();

    public override async Task<IEnumerable<(Person, Group)>> GetFromAsync()
    {
        var people = await elvanto.PeopleGetAllAsync(new() { Fields = [PersonAdditionalFields.Departments] });
        var groups = await elvanto.GroupsGetAllAsync(new());
        var groupNameToGroup = groups.ToDictionary(i => i.Name);

        return people
            .Where(i => i.Departments != null)
            .SelectMany(person => person.Departments.Department
                .SelectMany(department => department.SubDepartments.SubDepartment
                    .SelectMany(sub => sub.Positions.Position.Select(pos => (person, pos.Name)))
                    .Concat(department.SubDepartments.SubDepartment.Select(sub => (person, sub.Name)))
                )
                .Concat(person.Departments.Department.Select(department => (person, department.Name)))
            )
            .Distinct()
            .Where(i => groups.Any(j => j.Name == i.Name))
            .Select(i => (i.person, groupNameToGroup[i.Name]));
    }

    public override async Task<IEnumerable<(GroupMember member, Group group)>> GetToAsync()
    {
        var departments = new HashSet<string>((await elvanto.PeopleGetAllAsync(new() { Fields = [PersonAdditionalFields.Departments] }))
            .Where(i => i.Departments != null)
            .SelectMany(person => person.Departments.Department
                .SelectMany(department => department.SubDepartments.SubDepartment
                    .SelectMany(sub => sub.Positions.Position.Select(pos => pos.Name))
                    .Concat(department.SubDepartments.SubDepartment.Select(sub => sub.Name))
                )
                .Concat(person.Departments.Department.Select(department => department.Name))
            )
            .Distinct());

        var response = await elvanto.GroupsGetAllAsync(new() { Fields = [GroupAdditionalFields.People] });
        return response
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
        => await elvanto.GroupsRemovePersonAsync(additional.group.Id, additional.member.Id);
}
