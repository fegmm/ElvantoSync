using Microsoft.Extensions.Configuration;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ElvantoSync
{
    class Program
    {
        public static Settings settings;
        static async Task Main(string[] args)
        {
            LoadConfiguration();

            var elvanto = new ElvantoApi.Client(settings.ElvantoKey);
            var nextcloud = new NextcloudApi.Api(new NextcloudApi.Settings()
            {
                ServerUri = new Uri(settings.NextcloudServer),
                Username = settings.NextcloudUser,
                Password = settings.NextcloudPassword,
                ApplicationName = nameof(ElvantoSync),
                RedirectUri = new Uri(settings.NextcloudServer)
            });
            var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader()
            {
                kas_login = settings.KASLogin,
                kas_auth_data = settings.KASAuthData,
                kas_auth_type = "plain"
            });

            if (settings.SyncElvantoDepartementsToGroups)
                await new Elvanto.DepartementsToGroupMemberSync(elvanto).ApplyAsync();

            if (settings.SyncNextcloudPeople)
                await new Nextcloud.PeopleToNextcloudSync(elvanto, nextcloud).ApplyAsync();

            if (settings.SyncNextcloudGroups)
                await new Nextcloud.GroupsToNextcloudSync(elvanto, nextcloud).ApplyAsync();

            if (settings.SyncNextcloudGroupmembers)
                await new Nextcloud.GroupMembersToNextcloudSync(elvanto, nextcloud).ApplyAsync();

            if (settings.SyncNextcloudGroupfolders)
                await new Nextcloud.GroupsToNextcloudGroupFolderSync(elvanto, nextcloud).ApplyAsync();

            if (settings.SyncNextcloudDeck)
                await new Nextcloud.GroupsToDeckSync(elvanto, nextcloud).ApplyAsync();

            if (settings.SyncElvantoGroupsToKASMail)
            {
                await new AllInkl.GroupsToEmailSync(elvanto, kas, settings.KASDomain, nextcloud).ApplyAsync();
                await new AllInkl.GroupMembersToMailForwardMemberSync(elvanto, kas, settings.KASDomain).ApplyAsync();
            }

        }

        private static void LoadConfiguration()
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            settings = new Settings(
                OutputFolder: config["OUTPUT_FOLDER"],
                ElvantoKey: config["ELVANTO_API_KEY"],
                NextcloudServer: config["NEXTCLOUD_SERVER"],
                NextcloudUser: config["NEXTCLOUD_USER"],
                NextcloudPassword: config["NEXTCLOUD_PASSWORD"],
                KASLogin: config["KAS_LOGIN"],
                KASAuthData: config["KAS_AUTH_DATA"],
                KASDomain: config["KAS_DOMAIN"],
                LogOnly: bool.TryParse(config["LOG_ONLY"], out bool sync) && sync,
                SyncElvantoDepartementsToGroups: !bool.TryParse(config["SYNC_ELVANTO_DEPARTEMENTS_TO_GROUPS"], out sync) || sync,
                SyncNextcloudPeople: !bool.TryParse(config["SYNC_NEXTCLOUD_PEOPLE"], out sync) || sync,
                SyncNextcloudGroups: !bool.TryParse(config["SYNC_NEXTCLOUD_GROUPS"], out sync) || sync,
                SyncNextcloudDeck: !bool.TryParse(config["SYNC_NEXTCLOUD_DECK"], out sync) || sync,
                SyncNextcloudGroupmembers: !bool.TryParse(config["SYNC_NEXTCLOUD_GROUPMEMBERS"], out sync) || sync,
                SyncNextcloudGroupfolders: !bool.TryParse(config["SYNC_NEXTCLOUD_GROUPFOLDERS"], out sync) || sync,
                SyncElvantoGroupsToKASMail: !bool.TryParse(config["SYNC_ELVANTO_GROUPS_TO_KAS_MAIL"], out sync) || sync,
                UploadGroupMailAddressesToNextcloudPath: config["UPLOAD_GROUP_MAIL_ADDRESSES_TO_NEXTCLOUD_PATH"]
            );
        }
    }
}
