using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class PeopleToNextcloudSync(
    IElvantoClient elvanto,
    INextcloudProvisioningClient provisioningClient,
    DbContext dbContext,
    PeopleToNextcloudSyncSettings settings,
    ILogger<PeopleToNextcloudSync> logger
) : Sync<Person, User>(dbContext, settings, logger)
{
    public override string FromKeySelector(Person i) => i.Id;
    public override string ToKeySelector(User i) => i.Id;
    public override string FallbackFromKeySelector(Person i) => (GetDisplayName(i), i.Email).ToString();
    public override string FallbackToKeySelector(User i) => (i.DisplayName, i.Email).ToString();

    public override async Task<IEnumerable<Person>> GetFromAsync() =>
        (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest())).People.Person;

    public override async Task<IEnumerable<User>> GetToAsync()
    {
        var users = await provisioningClient.GetUsers();
        return users.Where(i => i.Id.StartsWith(settings.IdPrefix));
    }

    protected override async Task<string> AddMissing(Person person)
        => await provisioningClient.CreateUser(new CreateUserRequest()
        {
            UserId = settings.IdPrefix + person.Id,
            DisplayName = GetDisplayName(person),
            Email = person.Email,
            Password = Guid.NewGuid().ToString(),
            Quota = settings.Quoata
        });


    protected async override Task RemoveAdditional(User user)
    {
        if (user.Quota.Used != 0)
        {
            logger.LogWarning("User {0} cannot be removed as contains {1} bytes of data.", user.Id, user.Quota.Used);
            // TODO: Stop mapping deltion
        }
        await provisioningClient.DeleteUser(user.Id);
    }

    protected override async Task UpdateMatch(Person person, User user)
    {
        var request = new EditUserRequest()
        {
            DisplayName = GetDisplayName(person) == user.DisplayName ? null : GetDisplayName(person),
            Email = person.Email == user.Email ? null : user.Email,
            Phone = person.Mobile == user.Phone ? null : person.Mobile
        };
        await provisioningClient.EditUser(user.Id, request);
    }

    private static string GetDisplayName(Person i) => $"{i.Lastname}, {i.Firstname}";
}
