using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Infrastructure.Nextcloud;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    DbContext dbContext,
    IOptions<GroupsToCollectiveSyncSettings> settings,
    IOptions<GroupsToNextcloudSyncSettings> groupSettings,
    ILogger<GroupsToCollectivesSync> logger
) : Sync<Group, Collective>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(Collective i) => i.Id.ToString();
    public override string FallbackFromKeySelector(Group i) => SanitizeName(i.Name);
    public override string FallbackToKeySelector(Collective i) => i.Name;

    private string SanitizeName(string name) => name.Replace('/', '-');

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] })).Groups.Group
            .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<Collective>> GetToAsync()
        => await collectivesRepo.GetCollectives();

    protected override async Task<string> AddMissing(Group group)
    {
        string nextcloudGroupId = dbContext.ElvantoToNextcloudGroupId(group.Id);

        var createdCollective = await collectivesRepo.CreateCollective(SanitizeName(group.Name));
        try
        {
            await circleRepo.AddMemberToCircle(createdCollective.CircleId, nextcloudGroupId, MemberTypes.Group);
            string leaderGroupId = nextcloudGroupId + groupSettings.Value.GroupLeaderSuffix;
            var leaderMemberId = await circleRepo.AddMemberToCircle(createdCollective.CircleId, leaderGroupId, MemberTypes.Group);
            await circleRepo.SetMemberLevel(createdCollective.CircleId, leaderMemberId, MemberLevels.Admin);
        }
        catch
        {
            await collectivesRepo.DeleteCollective(createdCollective.Id);
            throw;
        }
        return ToKeySelector(createdCollective);
    }

    protected override async Task RemoveAdditional(Collective collective)
        => await collectivesRepo.DeleteCollective(collective.Id);

    protected override async Task UpdateMatch(Group group, Collective collective)
    {
        if (SanitizeName(group.Name) != collective.Name)
        {
            await collectivesRepo.SetDisplayName(collective.CircleId, group.Name);
        }
    }
}