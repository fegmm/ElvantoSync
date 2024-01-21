using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Settings.Nextcloud;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class PeopleToNextcloudSync(
    Client elvanto,
    INextcloudProvisioningClient provisioningClient,
    PeopleToNextcloudSyncSettings settings
) : Sync<Person, User>(settings)
{
    public override string FromKeySelector(Person i) => settings.IdPrefix + i.Id;
    public override string ToKeySelector(User i) => i.Id;

    public override async Task<IEnumerable<Person>> GetFromAsync() =>
        (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest())).People.Person;

    public override async Task<IEnumerable<User>> GetToAsync()
    {
        var users = await provisioningClient.GetUsers();
        return users.Where(i => i.Id.StartsWith(settings.IdPrefix));
    }

    public override async Task AddMissingAsync(IEnumerable<Person> missing)
    {
        var requests = missing.Select(i => provisioningClient.CreateUser(new CreateUserRequest()
        {
            UserId = settings.IdPrefix + i.Id,
            DisplayName = $"{i.Lastname}, {i.Firstname}",
            Email = i.Email,
            Password = Guid.NewGuid().ToString(),
            Quota = settings.Quoata
        }));

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
