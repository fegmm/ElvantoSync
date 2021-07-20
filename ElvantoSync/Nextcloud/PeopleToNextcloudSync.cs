using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using NextcloudApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud
{
    class PeopleToNextcloudSync : Sync<string, Person, string>
    {
        readonly Random random = new Random();
        private readonly Client elvanto;
        private readonly Api nextcloud;

        public PeopleToNextcloudSync(Client elvantoApi, Api nextcloudApi)
        {
            this.elvanto = elvantoApi;
            this.nextcloud = nextcloudApi;
        }

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
                    password = random.Next(int.MaxValue / 10, int.MaxValue).ToString(),
                    quota = "0 MB",
                }))
            );
        }
    }
}
