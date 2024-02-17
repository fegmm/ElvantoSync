using ElvantoSync.ElvantoApi.Models;
using System.Threading.Tasks;

namespace ElvantoSync.ElvantoService;
public interface IElvantoClient
{
    Task<GroupsGetAllResponse> GroupsGetAllAsync(GetAllRequest request);
    Task<PeopleGetAllResponse> PeopleGetAllAsync(GetAllPeopleRequest request);        
    Task<GroupsChangePersonResponse> GroupsAddPersonAsync(string groupId, string personId, string position = "");
    Task<GroupsChangePersonResponse> GroupsRemovePersonAsync(string groupId, string personId);

}