using System.Diagnostics;
using Nextcloud.Models.Circles;
using Nextcloud.Models.Talk;

namespace ElvantoSync.Infrastructure.Nextcloud;

public interface INextcloudTalkClient
{
    Task<Conversation> CreateConversation(int roomType, string invite, string source, string roomName);
    Task DeleteConversation(string token);
    Task<IEnumerable<Conversation>> GetConversations();
    Task SetDescription(string token, string description);
    Task SetRoomName(string token, string name);
    Task PromoteToModerator(string token, int attendeeId);
    Task DemoteFromModerator(string token, int attendeeId);
    Task<IEnumerable<Participant>> GetListOfParticipants(string token);
    Task AddGroupToRoom(string token, string groupId);
}