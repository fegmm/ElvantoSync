using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using Nextcloud.Interfaces;
using Nextcloud.Models.Deck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToDeckSync(Client elvanto, INextcloudDeckClient deckClient, Settings settings) : Sync<Group, Board>(settings)
{
    public override bool IsActive() => settings.SyncNextcloudDeck;
    public override string FromKeySelector(Group i) => i.Name;
    public override string ToKeySelector(Board i) => i.Title;

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<Board>> GetToAsync()
        => await deckClient.GetBoards();

    public override async Task AddMissingAsync(IEnumerable<Group> missing)
    {
        var requests = missing.Select(async i =>
        {
            var createdBoard = await deckClient.CreateBoard(i.Name, string.Format("{0:X6}", Random.Shared.Next(0x1000000)));
            await deckClient.AddMember(createdBoard.Id, i.Name, MemberTypes.Group, true, false, false);
            await deckClient.AddMember(createdBoard.Id, i.Name + Settings.GroupLeaderSuffix, MemberTypes.Group, true, true, false);
        });
        await Task.WhenAll(requests);
    }

}
