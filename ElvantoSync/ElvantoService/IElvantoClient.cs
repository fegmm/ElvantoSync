using Fegmm.Elvanto.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fegmm.Elvanto.Groups.AddPersonJson;

namespace ElvantoSync.ElvantoService;
public interface IElvantoClient
{
    Task<IEnumerable<Group>> GroupsGetAllAsync(Fegmm.Elvanto.Groups.GetAllJson.GetAllPostRequestBody request);
    Task<IEnumerable<Person>> PeopleGetAllAsync(Fegmm.Elvanto.People.GetAllJson.GetAllPostRequestBody request);        
    Task GroupsAddPersonAsync(string groupId, string personId, GroupMemberPositions? position = null);
    Task GroupsRemovePersonAsync(string groupId, string personId);
}