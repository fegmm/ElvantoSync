using System.Threading.Tasks;
using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Nextcloud.Interfaces;
using Nextcloud.Models.Deck;
namespace ElvantoSync.ElvantoService;
public class ExternalClientWrapper(ElvantoApi.Client client) : IElvantoClient
{
    

    public Task<GroupsGetAllResponse> GroupsGetAllAsync(GetAllRequest request)
    {
        return client.GroupsGetAllAsync(request);
    }

    public Task<PeopleGetAllResponse> PeopleGetAllAsync(GetAllPeopleRequest request)
    {
        return client.PeopleGetAllAsync(request);
    }

    public Task<GroupsChangePersonResponse> GroupsAddPersonAsync(string groupId, string personId, string position = "")
    {
        return client.GroupsAddPersonAsync(groupId, personId, position);
    }

    public async Task<GroupsChangePersonResponse> GroupsRemovePersonAsync(string groupId, string personId)
    {
        return await client.GroupsRemovePersonAsync(groupId, personId);    
    }
}