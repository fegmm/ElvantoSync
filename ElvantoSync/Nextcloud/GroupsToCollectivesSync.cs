using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Microsoft.IdentityModel.Protocols.WsTrust;
using NextcloudApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud
{
    class GroupsToCollectivesSync : Sync<string, string, Board>
    {
        private readonly Client elvanto;
        private readonly NextcloudApi.Api nextcloud;
        private readonly Random random;

        public GroupsToCollectivesSync(Client elvantoApi, NextcloudApi.Api nextcloud)
        {
            this.elvanto = elvantoApi;
            this.nextcloud = nextcloud;
        }

        public override async Task<Dictionary<string, string>> GetFromAsync()
        {
            return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
                .Groups.Group.ToDictionary(i => i.Name, i => i.Name);
        }

        public override async Task<Dictionary<string, Board>> GetToAsync()
        {
            var boards_response = await nextcloud.GetPlainListAsync<object>("index.php/apps/collectives/_api", "List");
            throw new NotImplementedException();
        }

        public override async Task AddMissingAsync(Dictionary<string, string> missing)
        {
            var requests = missing.Select(i => nextcloud.PostAsync<Board>("index.php/apps/deck/api/v1.1/boards", postParameters: new Dictionary<string, string>
                {
                    {"title", i.Value },
                    {"color", string.Format("{0:X6}", random.Next(0x1000000)) }
                })
            );
            var created_boards = await Task.WhenAll(requests);

            var add_group_requests = created_boards.Select(i =>
                nextcloud.PostAsync($"index.php/apps/deck/api/v1.1/boards/{i.id}/acl", postParameters: new Dictionary<string, object>
                {
                    {"type", 1},
                    {"participant", i.title},
                    {"permissionEdit", true},
                    {"permissionShare", true},
                    {"permissionManage", true},
                })
            );
            await Task.WhenAll(add_group_requests);
        }
    }
}
