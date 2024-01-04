using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Nextcloud.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToCollectivesSync(Client elvanto, ICollectiveRepository collectivesRepo, ICircleRepository circleRepo, Settings settings) : Sync<string, string, CollectiveModel>(settings)
{
    public override async Task<Dictionary<string, string>> GetFromAsync()
    {
        return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
           .Groups.Group.ToDictionary(i => i.Name, i => i.Name); ;
    }

    public override async Task<Dictionary<string, CollectiveModel>> GetToAsync()
    {
        var response = await collectivesRepo.GetCollectives();
        return response.data.ToDictionary(i => i.name);
    }

    public override async Task AddMissingAsync(Dictionary<string, string> missing)
    {
        var createCollectivesWithMembersTasks = missing.Keys.Select(async groupName =>
        {
            var createdCollective = await collectivesRepo.CreateCollective(groupName);
            await circleRepo.AddMembersToCircle(createdCollective.data.circleId, groupName);
            string leaderGroupName = groupName + Settings.GroupLeaderSuffix;
            var leaderMemberId = await circleRepo.AddMembersToCircle(createdCollective.data.circleId, leaderGroupName);
            await circleRepo.PromoteMemberToAdmin(createdCollective.data.circleId, leaderMemberId);
        });
        await Task.WhenAll(createCollectivesWithMembersTasks);
    }
}