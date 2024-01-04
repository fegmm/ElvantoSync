using ElvantoSync.Util;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud.Repository
{
    interface ICollectiveRepository
    {
        Task<CollectivesResponse> GetCollectives();
        Task<CreateCollectivesResponse> CreateCollective(string name);
    }


    class CollectivesRepository : ICollectiveRepository
    {

        private CookieContainer cookies = new CookieContainer();
        private readonly NextcloudApi.Api nextcloud;
        private readonly FlurlClient client;
        private CSRFToken token;
        public CollectivesRepository(NextcloudApi.Api nextCloud, FlurlClientFactory factory)
        {
            this.nextcloud = nextCloud;
            this.client = factory.GetClient();

        }
        public async Task<CollectivesResponse> GetCollectives()
        {

            await refreshToken();
            return await client.Request(nextcloud.Settings.ServerUri?.ToString())
                    .AppendPathSegment("/index.php/apps/collectives/_api")
                    .WithBasicAuth(nextcloud.Settings.Username, nextcloud.Settings.Password)
                    .WithHeader("OCS-ApiRequest", "true")
                    .WithHeader("requesttoken", token.token)
                    .GetJsonAsync<CollectivesResponse>();
        }

        public async Task<CreateCollectivesResponse> CreateCollective(string name)
        {
            var reqBody = new
            {
                name = name
            };
            await refreshToken();
            return await client.Request(nextcloud.Settings.ServerUri?.ToString())
                    .AppendPathSegment("/index.php/apps/collectives/_api")
                    .WithBasicAuth(nextcloud.Settings.Username, nextcloud.Settings.Password)
                    .WithHeader("OCS-ApiRequest", "true")
                    .WithHeader("requesttoken", token.token)
                    .PostJsonAsync(reqBody)
                    .ReceiveJson<CreateCollectivesResponse>();
        }

        private async Task<CSRFToken> refreshToken()
        {
            //  await nextcloud.LoginOrRefreshIfRequiredAsync();
            if (token != null)
                return token;


            var result = await client.Request(nextcloud.Settings.ServerUri?.ToString())
                    .AppendPathSegment("/index.php/csrftoken")
                    .WithBasicAuth(nextcloud.Settings.Username, nextcloud.Settings.Password)
                    .WithHeader("OCS-ApiRequest", "true")
                    .GetJsonAsync<CSRFToken>();
            token = result;
            return token;
        }



    }


    class MockCollectiveRepository : ICollectiveRepository
    {
        private Random random = new Random();

        async Task<CreateCollectivesResponse> ICollectiveRepository.CreateCollective(string name)
        {
            var newCollective = new CollectiveModel
            {
                id = random.Next(1, 1000),
                circleId = Guid.NewGuid().ToString(),
                emoji = GetRandomEmoji(),
                trashTimestamp = null,
                pageMode = 0,
                name = name, // Use the provided name
                level = random.Next(1, 10),
                editPermissionLevel = 1,
                sharePermissionLevel = 1,
                canEdit = true,
                canShare = true,
                shareToken = null,
                shareEditable = false,
                userPageOrder = 0,
                userShowRecentPages = true
            };

            // Create response object with the new collective
            var response = new CreateCollectivesResponse
            {
                data = newCollective
            };

            // Simulate async work, if necessary
            await Task.Delay(10);  // Dummy delay to mimic async operation

            return response;
        }

        Task<CollectivesResponse> ICollectiveRepository.GetCollectives()
        {

            var collectivesResponse = new CollectivesResponse
            {
                data = new List<CollectiveModel>()
            };

            // HashSet to track used names within this method call
            HashSet<string> usedNames = new HashSet<string>();

            int count = random.Next(5, 21); // Randomly choose a number between 5 and 20

            while (collectivesResponse.data.Count < count)
            {
                string randomName;
                do
                {
                    randomName = "RandomName" + random.Next(1, 10000); // Generate a random name
                } while (usedNames.Contains(randomName)); // Ensure it's unique within this call

                usedNames.Add(randomName); // Mark this name as used within this call

                collectivesResponse.data.Add(new CollectiveModel
                {
                    id = random.Next(1, 1000), // Random ID
                    circleId = Guid.NewGuid().ToString(),
                    emoji = GetRandomEmoji(),
                    trashTimestamp = null,
                    pageMode = 0,
                    name = randomName, // Use the unique random name
                    level = random.Next(1, 10),
                    editPermissionLevel = 1,
                    sharePermissionLevel = 1,
                    canEdit = random.Next(2) == 1,
                    canShare = random.Next(2) == 1,
                    shareToken = null,
                    shareEditable = false,
                    userPageOrder = 0,
                    userShowRecentPages = true
                });
            }

            return Task.FromResult(collectivesResponse);
        }
        private string GetRandomEmoji()
        {
            // Assuming you have a predefined list of emojis you want to randomize from
            string[] emojis = new string[] { "\ud83d\ude00", "\ud83d\ude01", "\ud83d\ude02", "\ud83d\ude03" }; // Sample emojis
            return emojis[random.Next(emojis.Length)];
        }
    }

    class CollectivesResponse
    {
        public List<CollectiveModel> data { get; set; }
    }

    class CreateCollectivesResponse
    {
        public CollectiveModel data { get; set; }
    }

    class CollectiveModel
    {
        public int id { get; set; }
        public string circleId { get; set; }
        public string emoji { get; set; }
        public object trashTimestamp { get; set; }
        public int pageMode { get; set; }
        public string name { get; set; }
        public int level { get; set; }
        public int editPermissionLevel { get; set; }
        public int sharePermissionLevel { get; set; }
        public bool canEdit { get; set; }
        public bool canShare { get; set; }
        public object shareToken { get; set; }
        public bool shareEditable { get; set; }
        public int userPageOrder { get; set; }
        public bool userShowRecentPages { get; set; }
    }

    class CSRFToken
    {
        public string token { get; set; }
    }

}