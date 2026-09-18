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
            .SelectMany(person => GetDepartmentNames(person).Select(name => (person, name)))
            .Distinct()
            .Where(i => groups.Any(j => j.Name == i.name))
            .Select(i => (i.person, groupNameToGroup[i.name]));
    }

    public override async Task<IEnumerable<(GroupMember member, Group group)>> GetToAsync()
    {
        var departments = new HashSet<string>(
            (await elvanto.PeopleGetAllAsync(new() { Fields = [PersonAdditionalFields.Departments] }))
                .SelectMany(GetDepartmentNames)
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

    private static IEnumerable<string> GetDepartmentNames(Person person)
    {
        foreach (var department in person.Departments?.Department ?? [])
        {
            if (department == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(department.Name))
            {
                yield return department.Name;
            }

            foreach (var subDepartment in department.SubDepartments?.SubDepartment ?? [])
            {
                if (subDepartment == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(subDepartment.Name))
                {
                    yield return subDepartment.Name;
                }

                foreach (var position in subDepartment.Positions?.Position ?? [])
                {
                    if (position == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(position.Name))
                    {
                        yield return position.Name;
                    }
                }
            }
        }
    }
}
