using System.Diagnostics;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Talk;

namespace ElvantoSync.Infrastructure.Nextcloud;

public interface INextcloudTalkClient
{
    Task CreateConversation(int roomType, string invite, string source, string roomName );
    Task<IEnumerable<Conversation>> GetConversations();

}