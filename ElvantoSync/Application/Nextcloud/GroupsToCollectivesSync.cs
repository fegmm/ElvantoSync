using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Infrastructure.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Nextcloud.Interfaces;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Collectives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToCollectivesSync(
    IElvantoClient elvanto,
    INextcloudCollectivesClient collectivesRepo,
    INextcloudCircleClient circleRepo,
    DbContext dbContext,
    GroupsToCollectiveSyncSettings settings,
    GroupsToNextcloudSyncSettings groupSettings,
    ILogger<GroupsToCollectivesSync> logger
) : Sync<Group, Collective>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(Collective i) => i.Id.ToString();
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(Collective i) => i.Name;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<Collective>> GetToAsync()
        => await collectivesRepo.GetCollectives();

    protected override async Task<string> AddMissing(Group group)
    {
        var createdCollective = await collectivesRepo.CreateCollective(group.Name);
        await circleRepo.AddMemberToCircle(createdCollective.CircleId, group.Name, MemberTypes.Group);
        string leaderGroupName = group.Name + groupSettings.GroupLeaderSuffix;
        var leaderMemberId = await circleRepo.AddMemberToCircle(createdCollective.CircleId, leaderGroupName, MemberTypes.Group);
        await circleRepo.SetMemberLevel(createdCollective.CircleId, leaderMemberId, MemberLevels.Admin);
        return ToKeySelector(createdCollective);
    }

    protected override async Task RemoveAdditional(Collective collective)
        => await collectivesRepo.DeleteCollective(collective.Id);

    protected override async Task UpdateMatch(Group group, Collective collective)
    {
        if (group.Name != collective.Name)
        {
            await collectivesRepo.SetDisplayName(collective.CircleId, group.Name);
        }
    }
}