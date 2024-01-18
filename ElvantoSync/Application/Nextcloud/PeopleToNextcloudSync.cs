using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class PeopleToNextcloudSync(Client elvanto, INextcloudProvisioningClient provisioningClient, Settings settings) : Sync<Person, User>(settings)
{
    public override bool IsActive() => settings.SyncNextcloudPeople;
    public override string FromKeySelector(Person i) => $"Elvanto-{i.Id}";
    public override string ToKeySelector(User i) => i.Id;

    public override async Task<IEnumerable<Person>> GetFromAsync() =>
        (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest())).People.Person;

    public override async Task<IEnumerable<User>> GetToAsync()
    {
        var users = await provisioningClient.GetUsers();
        return users.Where(i => i.Id.StartsWith("Elvanto-"));
    }

    public override async Task AddMissingAsync(IEnumerable<Person> missing)
    {
        var requests = missing.Select(i => provisioningClient.CreateUser(new CreateUserRequest(
            "Elvanto-" + i.Id,
            $"{i.Lastname}, {i.Firstname}",
            i.Email,
            null,
            null,
            null,
            null,
            Guid.NewGuid().ToString(),
            "1 GB"
        )));

        await Task.WhenAll(requests);
    }

    public async override Task RemoveAdditionalAsync(IEnumerable<User> additionals)
    {
        var deleteEmptyUsers = additionals
            .Where(i => i.Quota.Used == 0)
            .Select(i => provisioningClient.DeleteUser(i.Id));

        await Task.WhenAll(deleteEmptyUsers);
    }

}
