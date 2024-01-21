using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Nextcloud.Interfaces;
using Nextcloud.Models.Deck;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nextcloud.Interfaces;
using Nextcloud.Models.GroupFolders;

namespace ElvantoSync.ElvantoService;
public interface IElvantoClient
{
    Task<GroupsGetAllResponse> GroupsGetAllAsync(GetAllRequest request);
    Task<PeopleGetAllResponse> PeopleGetAllAsync(GetAllPeopleRequest request);        
    Task<GroupsChangePersonResponse> GroupsAddPersonAsync(string groupId, string personId, string position = "");
    Task<GroupsChangePersonResponse> GroupsRemovePersonAsync(string groupId, string personId);

}