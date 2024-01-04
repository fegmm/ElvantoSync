using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using NextcloudApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class PeopleToNextcloudSync(Client elvanto, Api nextcloud, Settings settings) : Sync<string, Person, string>(settings)
{
    readonly Random random = new Random();

    public override async Task<Dictionary<string, Person>> GetFromAsync()
    {
        return (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest()))
            .People.Person.ToDictionary(i => $"Elvanto-{i.Id}");
    }

    public override async Task<Dictionary<string, string>> GetToAsync()
    {

        return (await NextcloudApi.User.List(nextcloud))
            .All(nextcloud)
            .Where(i => i.StartsWith("Elvanto-"))
            .ToDictionary(i => i);
    }

    public override async Task AddMissingAsync(Dictionary<string, Person> missing)
    {
        await Task.WhenAll(
            missing.Select(item => User.Create(nextcloud, new UserInfo()
            {
                userid = item.Key,
                displayName = $"{item.Value.Lastname}, {item.Value.Firstname}",
                email = item.Value.Email,
                password = Guid.NewGuid().ToString(),
                quota = "0 MB"
            }))
        );
    }

    public async override Task RemoveAdditionalAsync(Dictionary<string, string> additionals)
    {
        var user_infos = await Task.WhenAll(additionals.Select(i => GetUserOrReturnNull(i.Key)));
        await Task.WhenAll(user_infos
            .Where(i => i != null && i.quota.used == 0)
            .Select(i => NextcloudApi.User.Delete(nextcloud, i.id))
        );
    }

    private async Task<User> GetUserOrReturnNull(string userid)
    {
        try
        {
            return await NextcloudApi.User.Get(nextcloud, userid);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
