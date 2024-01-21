using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Infrastructure.Nextcloud;
using Nextcloud.Interfaces;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Collectives;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToCollectivesSync(
   IElvantoClient elvanto,
    INextcloudCollectivesClient collectivesRepo,
    INextcloudCircleClient circleRepo,
    Settings settings
) : Sync<string, string, Collective>(settings)
{
    public override async Task<Dictionary<string, string>> GetFromAsync()
    {
        return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
           .Groups.Group.ToDictionary(i => i.Name, i => i.Name); ;
    }

    public override async Task<Dictionary<string, Collective>> GetToAsync()
    {
        var response = await collectivesRepo.GetCollectives();
        return response.ToDictionary(i => i.Name);
    }

    public override async Task AddMissingAsync(Dictionary<string, string> missing)
    {
        var createCollectivesWithMembersTasks = missing.Keys.Select(async groupName =>
        {
            var createdCollective = await collectivesRepo.CreateCollective(groupName);
            await circleRepo.AddMemberToCircle(createdCollective.CircleId, groupName, MemberTypes.Group);
            string leaderGroupName = groupName + Settings.GroupLeaderSuffix;
            var leaderMemberId = await circleRepo.AddMemberToCircle(createdCollective.CircleId, leaderGroupName, MemberTypes.Group);
            await circleRepo.SetMemberLevel(createdCollective.CircleId, leaderMemberId, MemberLevels.Admin);
        });
        await Task.WhenAll(createCollectivesWithMembersTasks);
    }
    public override bool IsActive()
    {
        return settings.SyncNextcloudCollectives;
    }
}