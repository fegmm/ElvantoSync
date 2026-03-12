using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fegmm.Elvanto;
using Fegmm.Elvanto.Groups.AddPersonJson;
using Fegmm.Elvanto.Models;

namespace ElvantoSync.ElvantoService;

public class ExternalClientWrapper(ElvantoClient client) : IElvantoClient
{
    async Task<IEnumerable<Group>> IElvantoClient.GroupsGetAllAsync(Fegmm.Elvanto.Groups.GetAllJson.GetAllPostRequestBody request)
    {
        var response = await client.Groups.GetAllJson.PostAsync(request);
        return response.GroupQueryResponse?.Groups?.Group ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }

    async Task<IEnumerable<Person>> IElvantoClient.PeopleGetAllAsync(Fegmm.Elvanto.People.GetAllJson.GetAllPostRequestBody request)
    {
        var response = await client.People.GetAllJson.PostAsync(request);
        return response.PeopleQueryResponse?.People?.Person ??
            throw new Exception($"Request failed: {response.ErrorResponse.Error.Code} - {response.ErrorResponse.Error.Message}");
    }

    async Task IElvantoClient.GroupsAddPersonAsync(string groupId, string personId, GroupMemberPositions? position)
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

    async Task IElvantoClient.GroupsRemovePersonAsync(string groupId, string personId)
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
}