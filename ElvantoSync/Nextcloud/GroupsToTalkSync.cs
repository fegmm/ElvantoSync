using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Infrastructure.Nextcloud;
using Nextcloud.Interfaces;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Collectives;
using Nextcloud.Models.Talk;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Nextcloud;

class GroupsToTalkSync(
   IElvantoClient elvanto,
    INextcloudTalkClient talkRepo,
    Settings settings
) : Sync<string, string, Conversation>(settings)
{

   

    public override async Task<Dictionary<string, string>> GetFromAsync()
    {
        return (await elvanto.GroupsGetAllAsync(new GetAllRequest()))
           .Groups.Group.ToDictionary(i => i.Name, i => i.Name); 
    }

    public override async Task<Dictionary<string, Conversation>> GetToAsync()
    {
        var response = await talkRepo.GetConversations();
       // await TestCreation();
        return response.ToDictionary(i => i.Name);
    }

    public async Task TestCreation(){
        var fakeMissing = new Dictionary<string, string>
        {
            { "Admin", "Admin" }
        };
        await AddMissingAsync(fakeMissing);
    }

    public override async Task AddMissingAsync(Dictionary<string, string> missing)
    {

         string description = @"Euer Gruppenchat für's Team! 

Anmerkungen: Neue Mitarbeiter, die ihr in Elvanto hinzufügt, haben am nächsten Tag automatisch Zugriff. Um wie bei WhatsApp über jede neue Nachricht ein Push zu erhalten, stellt die Benachrichtigungseinstellungen auf Alle Nachrichten.";
        var createCollectivesWithMembersTasks = missing.Keys.Select(async group => 
        { 
            var createdConvo = await talkRepo.CreateConversation(2,group,"groups",group);
            await talkRepo.SetDescription(createdConvo.Token, description);
        });
        await Task.WhenAll(createCollectivesWithMembersTasks);
    }
    public override bool IsActive()
    {
        return settings.SyncNextCloudTalk;
    }
}