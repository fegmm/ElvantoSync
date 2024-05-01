using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nextcloud.Interfaces;
using Nextcloud.Models.Deck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToDeckSync(
    IElvantoClient elvanto,
    INextcloudDeckClient deckClient,
    DbContext dbContext,
    IOptions<GroupsToDeckSyncSettings> settings,
    IOptions<GroupsToNextcloudSyncSettings> groupSettings,
    ILogger<GroupsToDeckSync> logger
) : Sync<Group, Board>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Id;
    public override string ToKeySelector(Board i) => i.Id.ToString();
    public override string FallbackFromKeySelector(Group i) => i.Name;
    public override string FallbackToKeySelector(Board i) => i.Title;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] })).Groups.Group
        .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<Board>> GetToAsync()
        => await deckClient.GetBoards();

    protected override async Task<string> AddMissing(Group group)
    {
        var createdBoard = await deckClient.CreateBoard(group.Name, string.Format("{0:X6}", Random.Shared.Next(0x1000000)));
        try
        {
            await deckClient.AddMember(createdBoard.Id, group.Name, MemberTypes.Group, true, false, false);
            await deckClient.AddMember(createdBoard.Id, group.Name + groupSettings.Value.GroupLeaderSuffix, MemberTypes.Group, true, true, false);
        }
        catch
        {
            await deckClient.DeleteBoard(createdBoard.Id);
            throw;
        }
        return ToKeySelector(createdBoard);
    }

    protected override async Task RemoveAdditional(Board board)
        => await deckClient.DeleteBoard(board.Id);

    protected override async Task UpdateMatch(Group group, Board board)
    {
        if (group.Name != board.Title)
        {
            await deckClient.SetDisplayName(board.Id, group.Name, board.Color);
        }
    }
}
