using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fegmm.Elvanto;
using Fegmm.Elvanto.Groups.AddPersonJson;
using Fegmm.Elvanto.Models;
using Fegmm.Elvanto.Songs.GetAllJson;

namespace ElvantoSync.ElvantoService;

public class ExternalClientWrapper(ElvantoClient client) : IElvantoClient
{
    public async Task<IEnumerable<Group>> GroupsGetAllAsync(Fegmm.Elvanto.Groups.GetAllJson.GetAllPostRequestBody request)
    {
        var response = await client.Groups.GetAllJson.PostAsync(request);
        return response.GroupQueryResponse?.Groups?.Group ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }

    public async Task<IEnumerable<Person>> PeopleGetAllAsync(Fegmm.Elvanto.People.GetAllJson.GetAllPostRequestBody request)
    {
        var response = await client.People.GetAllJson.PostAsync(request);
        return response.PeopleQueryResponse?.People?.Person ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }

    public async Task GroupsAddPersonAsync(string groupId, string personId, GroupMemberPositions? position)
    {
        var response = await client.Groups.AddPersonJson.PostAsync(new()
        {
            Id = groupId,
            PersonId = personId,
            Position = position
        });
        if (response.ErrorResponse != null)
        {
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
        }
    }

    public async Task GroupsRemovePersonAsync(string groupId, string personId)
    {
        var response = await client.Groups.RemovePersonJson.PostAsync(new()
        {
            Id = groupId,
            PersonId = personId
        });
        if (response.ErrorResponse != null)
        {
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
        }
    }

    public async Task<IEnumerable<Song>> GetSongsAsync(GetAllPostRequestBody request)
    {
        var response = await client.Songs.GetAllJson.PostAsync(request);
        return response.SongsQueryResponse?.Songs?.Song ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }

    public async Task<IEnumerable<Arrangement>> GetArrangementsAsync(Fegmm.Elvanto.Songs.Arrangements.GetAllJson.GetAllPostRequestBody request)
    {
        var response = await client.Songs.Arrangements.GetAllJson.PostAsync(request);
        return response.ArrangementsQueryResponse?.Arrangements?.Arrangement ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }

    public async Task<List<ArrangementKey>> GetArrangementKeysAsync(Fegmm.Elvanto.Songs.Keys.GetAllJson.GetAllPostRequestBody request)
    {
        var response = await client.Songs.Keys.GetAllJson.PostAsync(request);

        if (response.ErrorResponse?.Error?.Code == 404)
        {
            // Some songs do not have a key attached. Elvanto returns a 404 in those cases.
            return [];
        }
        
        return response.KeysQueryResponse?.Keys?.Key ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }
}
