using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Nextcloud.Interfaces;
using Nextcloud.Models.Deck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToDeckSync(Client elvanto, INextcloudDeckClient deckClient, Settings settings) : Sync<string, string, Board>(settings)
{
    private readonly Random random = new();

    public override async Task<Dictionary<string, string>> GetFromAsync()
    {
        return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
            .Groups.Group.ToDictionary(i => i.Name, i => i.Name);
    }

    public override async Task<Dictionary<string, Board>> GetToAsync()
    {
        var boards_response = await deckClient.GetBoards();
        return boards_response.Where(i => i.DeletedAt == 0).ToDictionary(i => i.Title);
    }

    public override async Task AddMissingAsync(Dictionary<string, string> missing)
    {
        var requests = missing.Select(async i =>
        {
            var createdBoard = await deckClient.CreateBoard(i.Value, string.Format("{0:X6}", random.Next(0x1000000)));
            await deckClient.AddMember(createdBoard.Id, i.Value, MemberTypes.Group, true, false, false);
            await deckClient.AddMember(createdBoard.Id, i.Value + Settings.GroupLeaderSuffix, MemberTypes.Group, true, true, false);
        });
        await Task.WhenAll(requests);
    }

    public override bool IsActive()
    {
        return settings.SyncNextcloudDeck;
    }
}
