using Fegmm.Elvanto.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ElvantoSync.ElvantoService;

public interface IElvantoClient
{
    Task<IEnumerable<Group>> GroupsGetAllAsync(Fegmm.Elvanto.Groups.GetAllJson.GetAllPostRequestBody request);
    Task<IEnumerable<Person>> PeopleGetAllAsync(Fegmm.Elvanto.People.GetAllJson.GetAllPostRequestBody request);
    Task<IEnumerable<Song>> GetSongsAsync(Fegmm.Elvanto.Songs.GetAllJson.GetAllPostRequestBody request);
    Task<IEnumerable<Arrangement>> GetArrangementsAsync(Fegmm.Elvanto.Songs.Arrangements.GetAllJson.GetAllPostRequestBody request);
    Task GroupsAddPersonAsync(string groupId, string personId, GroupMemberPositions? position = null);
    Task GroupsRemovePersonAsync(string groupId, string personId);
    Task<List<ArrangementKey>> GetArrangementKeysAsync(Fegmm.Elvanto.Songs.Keys.GetAllJson.GetAllPostRequestBody request);
}