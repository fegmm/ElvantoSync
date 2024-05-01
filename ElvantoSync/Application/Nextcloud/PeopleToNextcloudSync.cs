using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Exceptions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

public class PeopleToNextcloudSync(
    IElvantoClient elvanto,
    INextcloudProvisioningClient provisioningClient,
    DbContext dbContext,
    IOptions<PeopleToNextcloudSyncSettings> settings,
    ILogger<PeopleToNextcloudSync> logger
) : Sync<Person, User>(dbContext, settings, logger)
{
    public override string FromKeySelector(Person i) => i.Id;
    public override string ToKeySelector(User i) => i.Id;
    public override string FallbackFromKeySelector(Person i) => settings.Value.IdPrefix + i.Id.ToString();
    public override string FallbackToKeySelector(User i) => i.Id.ToString();

    public override async Task<IEnumerable<Person>> GetFromAsync() =>
        (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest())).People.Person;

    public override async Task<IEnumerable<User>> GetToAsync()
    {
        var users = await provisioningClient.GetUsers();
        return users.Where(i => i.Id.StartsWith(settings.Value.IdPrefix));
    }

    protected override async Task<string> AddMissing(Person person)
        => await provisioningClient.CreateUser(new CreateUserRequest()
        {
            UserId = settings.Value.IdPrefix + person.Id,
            DisplayName = GetDisplayName(person),
            Email = person.Email,
            Password = Guid.NewGuid().ToString(),
            Quota = settings.Value.Quoata
        });


    protected async override Task RemoveAdditional(User user)
    {
        if (user.Quota.Used != 0)
        {
            throw new ContainsDataException($"User {user.Id} cannot be removed as it contains {user.Quota.Used} bytes of data.");
        }
        await provisioningClient.DeleteUser(user.Id);
    }

    protected override async Task UpdateMatch(Person person, User user)
    {
        var request = new EditUserRequest()
        {
            DisplayName = GetDisplayName(person) == user.DisplayName ? null : GetDisplayName(person),
            Email = person.Email?.ToLower() == user.Email?.ToLower() ? null : user.Email,
        };
        await provisioningClient.EditUser(user.Id, request);
    }

    private static string GetDisplayName(Person i) => $"{i.Lastname}, {i.Firstname}";
}
