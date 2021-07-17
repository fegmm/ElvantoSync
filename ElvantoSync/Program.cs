using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ElvantoSync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            
            var elvanto = new ElvantoApi.Client(config["ELVANTO_API_KEY"]);
            var nextcloud = new NextcloudApi.Api(new NextcloudApi.Settings()
            {
                ServerUri = new Uri(config["NEXTCLOUD_SERVER"]),
                User = config["NEXTCLOUD_USER"],
                Password = config["NEXTCLOUD_PASSWORD"]
            });

            if (!bool.TryParse(config["SYNC_ELVANTO_DEPARTEMENTS_TO_GROUPS"], out bool sync) || sync)
                await new Elvanto.DepartementsToGroupMemberSync(elvanto).ApplyAsync();

            if (!bool.TryParse(config["SYNC_NEXTCLOUD_PEOPLE"], out sync) || sync)
                await new Nextcloud.PeopleToNextcloudSync(elvanto, nextcloud).ApplyAsync();

            if (!bool.TryParse(config["SYNC_NEXTCLOUD_GROUPS"], out sync) || sync)
                await new Nextcloud.PeopleToNextcloudSync(elvanto, nextcloud).ApplyAsync();

            if (!bool.TryParse(config["SYNC_NEXTCLOUD_GROUPMEMBERS"], out sync) || sync)
                await new Nextcloud.PeopleToNextcloudSync(elvanto, nextcloud).ApplyAsync();

            if (!bool.TryParse(config["SYNC_NEXTCLOUD_GROUPFOLDERS"], out sync) || sync)
                await new Nextcloud.PeopleToNextcloudSync(elvanto, nextcloud).ApplyAsync();
        }
    }
}
