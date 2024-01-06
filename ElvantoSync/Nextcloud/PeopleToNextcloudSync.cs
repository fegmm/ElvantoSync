using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class PeopleToNextcloudSync(Client elvanto, INextcloudProvisioningClient provisioningClient, Settings settings) : Sync<string, Person, User>(settings)
{
    readonly Random random = new Random();

    public override async Task<Dictionary<string, Person>> GetFromAsync()
    {
       return (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest()))
            .People.Person.ToDictionary(i => $"Elvanto-{i.Id}"); 
    }

    public override async Task<Dictionary<string, User>> GetToAsync()
    {
        var users = await provisioningClient.GetUsers();
        return users.Where(i => i.Id.StartsWith("Elvanto-")).ToDictionary(i => i.Id);
    }

    public override async Task AddMissingAsync(Dictionary<string, Person> missing)
    {
        var requests = missing.Select(i => provisioningClient.CreateUser(new CreateUserRequest(
            i.Key,
            $"{i.Value.Lastname}, {i.Value.Firstname}",
            i.Value.Email,
            null,
            null,
            null,
            null,
            Guid.NewGuid().ToString(),
            "1 GB"
        )));

        await Task.WhenAll(requests);
    }

    public async override Task RemoveAdditionalAsync(Dictionary<string, User> additionals)
    {
        var deleteEmptyUsers = additionals.Where(i => i.Value.Quota.Used == 0)
            .Select(i => provisioningClient.DeleteUser(i.Key));

        await Task.WhenAll(deleteEmptyUsers);
    }

    public override bool IsActive()
    {
        return settings.SyncNextcloudPeople;
    }
}
