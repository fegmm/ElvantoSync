using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.GroupFinder.Model;
using ElvantoSync.GroupFinder.Service;
using ElvantoSync.Persistence;
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
    IOptions<GroupFinderToNextcloudSyncSettings> settings
) : Sync<Group, string>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(string i) => i;
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(string i) => i;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"], Category_id = "1d8d2db3-8367-43d2-b263-11a0ae810a80" })).Groups.Group
            .Where(i => i.People?.Person.Any() ?? false).Where(i => i.Meeting_postcode != "");

    public override async Task<IEnumerable<string>> GetToAsync()
        => await groupFinderService.GetGroupAsync();

    protected override async Task UpdateMatch(Group group, string _){
       await insertGroup(group);
    }

    protected override async Task<string> AddMissing(Group group)
    {
       return await insertGroup(group);
    }

    private async Task<string> insertGroup(Group group){
          var leader = group.People.Person.FirstOrDefault(p => p.Position == "Leader");
        if (group.Meeting_postcode == null)
        {
            logger.LogWarning("Group {group} has no postcode, skipping", group.Name);
            return null;
        }
        var request = new CreateGroupRequest
        {
            Id = group.Id,
            ModifiedAt = group.Date_modified,
            Name = group.Name,
            Address = new Address
            {
                Street = group.Meeting_address,
                PostalCode = int.Parse(group.Meeting_postcode),
                City = group.Meeting_city
            },
            Leader = new Leader
            {
                ElvantoId = leader.Id,
                Name = leader.Firstname + " " + leader.Lastname,
                Email = leader.Email
            },
            MeetingDay = group.Meeting_day,
            MeetingTime = group.Meeting_time,
            MeetingFrequency = group.Meeting_frequency,
            ///TODO: remove param from API
            MaxCapacity = 999
        };
        logger.LogInformation("Creating group {request}", request);
        await groupFinderService.CreateGroupAsync(request);

        //TODO: adjust structure
        return ToKeySelector(group.Id);
    }
}