using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Infrastructure.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Nextcloud.Interfaces;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Collectives;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToCollectivesSync(
    Client elvanto,
    INextcloudCollectivesClient collectivesRepo,
    INextcloudCircleClient circleRepo,
    DbContext dbContext,
    GroupsToCollectiveSyncSettings settings,
    GroupsToNextcloudSyncSettings groupSettings
) : MappedSync<Group, Collective>(dbContext, settings)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(Collective i) => i.Id.ToString();

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<Collective>> GetToAsync()
        => await collectivesRepo.GetCollectives();

    public override async Task AddMissingAsync(IEnumerable<Group> missing)
    {
        var createCollectivesWithMembersTasks = missing.Select(async group =>
        {
            var createdCollective = await collectivesRepo.CreateCollective(group.Name);
            await circleRepo.AddMemberToCircle(createdCollective.CircleId, group.Name, MemberTypes.Group);
            string leaderGroupName = group.Name + groupSettings.GroupLeaderSuffix;
            var leaderMemberId = await circleRepo.AddMemberToCircle(createdCollective.CircleId, leaderGroupName, MemberTypes.Group);
            await circleRepo.SetMemberLevel(createdCollective.CircleId, leaderMemberId, MemberLevels.Admin);
        });
        await Task.WhenAll(createCollectivesWithMembersTasks);
    }

    public override async Task ApplyUpdate(IEnumerable<(Group, Collective)> matches)
    {
        await Task.WhenAll(matches.Select(async match =>
        {
            var (group, collective) = match;
            if (group.Name != collective.Name)
            {
                await collectivesRepo.SetDisplayName(collective.CircleId, group.Name);
            }
        }));
    }
}