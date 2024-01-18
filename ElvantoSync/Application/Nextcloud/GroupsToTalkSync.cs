using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Infrastructure.Nextcloud;
using Nextcloud.Models.Talk;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToTalkSync(
    Client elvanto,
    INextcloudTalkClient talkRepo,
    Settings settings
) : Sync<Group, Conversation>(settings)
{
    public override bool IsActive() => settings.SyncNextCloudTalk;
    public override string FromKeySelector(Group i) => i.Name;
    public override string ToKeySelector(Conversation i) => i.Name;

    public override async Task<IEnumerable<Group>> GetFromAsync() =>
        (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group;

    public override async Task<IEnumerable<Conversation>> GetToAsync() =>
        await talkRepo.GetConversations();

    public override async Task AddMissingAsync(IEnumerable<Group> missing)
    {
        string description = @"Euer Gruppenchat für's Team! 

Anmerkungen: Neue Mitarbeiter, die ihr in Elvanto hinzufügt, haben am nächsten Tag automatisch Zugriff. Um wie bei WhatsApp über jede neue Nachricht ein Push zu erhalten, stellt die Benachrichtigungseinstellungen auf Alle Nachrichten.";

        var createCollectivesWithMembersTasks = missing.Select(async group =>
        {
            var createdConvo = await talkRepo.CreateConversation(2, group.Name, "groups", group.Name);
            await talkRepo.SetDescription(createdConvo.Token, description);
        });
        await Task.WhenAll(createCollectivesWithMembersTasks);
    }
}