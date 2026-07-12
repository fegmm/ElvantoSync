using ElvantoSync.ElvantoService;
using ElvantoSync.GroupFinder.Model;
using ElvantoSync.GroupFinder.Service;
using ElvantoSync.Persistence;
using Fegmm.Elvanto.Groups.GetAllJson;
using Fegmm.Elvanto.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ElvantoSync.GroupFinder;

class GroupFinderSync(
    IElvantoClient elvanto,
    DbContext dbContext,
    ILogger<GroupFinderSync> logger,
    IGroupFinderService groupFinderService,
    IOptions<GroupFinderToNextCloudSyncSettings> settings
) : Sync<Group, string>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(string i) => i;
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(string i) => i;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new() { Fields = [GroupAdditionalFields.People], CategoryId = ["1d8d2db3-8367-43d2-b263-11a0ae810a80"] }))
            .Where(i => i.People?.Person?.Any() ?? false).Where(i => i.MeetingPostcode is not null);

    public override async Task<IEnumerable<string>> GetToAsync()
        => await groupFinderService.GetGroupAsync();

    protected override async Task UpdateMatch(Group group, string _){
       await insertGroup(group);
    }

    protected override async Task<string> AddMissing(Group group)
    {
       return await insertGroup(group);
    }

    protected override async Task RemoveAdditional(string toId) {
         await groupFinderService.DeleteGroupAsync(toId);
    }
    

    private async Task<string> insertGroup(Group group)
    {
        var leader = group.People.Person.FirstOrDefault(p => p.Position == GroupMemberPositions.Leader);
        if (leader == null)
        {
            logger.LogWarning("Group {group} has no leader, skipping", group.Name);
            return null;
        }
        string ncLeaderId = dbContext.ElvantoToNextcloudPeopleId(leader.Id) ?? leader.Id;
        string nextcloudGroupId = dbContext.ElvantoToNextcloudGroupId(group.Id) ?? group.Id;
        if (group.MeetingPostcode == null)
        {
            logger.LogWarning("Group {group} has no postcode, skipping", group.Name);
            return null;
        }

        var request = new CreateGroupRequest
        {
            Id = nextcloudGroupId,
            ModifiedAt = group.DateModified?.ToString("O"),
            Name = group.Name,
            Address = new Address
            {
                Street = group.MeetingAddress,
                PostalCode = int.Parse(group.MeetingPostcode),
                City = group.MeetingCity
            },
            Leader = new Leader
            {
                ElvantoId = ncLeaderId,
                Name = leader.Firstname + " " + leader.Lastname,
                Email = leader.Email
            },
            MeetingDay = group.MeetingDay,
            MeetingTime = group.MeetingTime,
            MeetingFrequency = group.MeetingFrequency,
            ///TODO: remove param from API
            MaxCapacity = 999
        };
        logger.LogInformation("Creating group {request}", request);
        await groupFinderService.CreateGroupAsync(request);

        //TODO: adjust structure
        return ToKeySelector(nextcloudGroupId);
    }
}