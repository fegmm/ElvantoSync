using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.Elvanto
{
    class DepartementsToGroupMemberSync : Sync<(string personId, string groupName), Person, GroupMember>
    {
        private readonly Client elvanto;

        public DepartementsToGroupMemberSync(ElvantoApi.Client elvantoApi)
        {
            elvanto = elvantoApi;
        }

        public override async Task<Dictionary<(string personId, string groupName), Person>> GetFromAsync()
        {
            var response = await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest() { Fields = new[] { "departments" } });
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
                .Where(i => groups.Groups.Group.Any(j => j.Name == i.Name))
                .ToDictionary(i => (i.person.Id, i.Name), i => i.person);
        }

        public override async Task<Dictionary<(string personId, string groupName), GroupMember>> GetToAsync()
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
                )
                .ToDictionary(i => (i.member.Id, i.Name), i => i.member);
        }

        public override async Task AddMissingAsync(Dictionary<(string personId, string groupName), Person> missing)
        {
            var response = await elvanto.GroupsGetAllAsync(new GetAllRequest());
            var nameToIdDict = response.Groups.Group.ToDictionary(i => i.Name, i => i.Id);
            var count = missing.Where(i => nameToIdDict.ContainsKey(i.Key.groupName));

            await Task.WhenAll(missing.Where(i => nameToIdDict.ContainsKey(i.Key.groupName))
                                      .Select(i => elvanto.GroupsAddPersonAsync(nameToIdDict[i.Key.groupName], i.Key.personId)));
        }

        public override async Task RemoveAdditionalAsync(Dictionary<(string personId, string groupName), GroupMember> additionals)
        {
            var response = await elvanto.GroupsGetAllAsync(new GetAllRequest());
            var nameToIdDict = response.Groups.Group.ToDictionary(i => i.Name, i => i.Id);
            await Task.WhenAll(additionals.Select(i => elvanto.GroupsRemovePersonAsync(nameToIdDict[i.Key.groupName], i.Key.personId)));
        }
    }
}
