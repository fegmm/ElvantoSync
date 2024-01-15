

using System;
using System.Collections.Generic;
namespace Nextcloud.Models.Talk;
public record Conversation
{
    public int Id { get; set; }
    public string Token { get; set; }
    public int Type { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string ObjectType { get; set; }
    public string ObjectId { get; set; }
    public int ParticipantType { get; set; }
    public int ParticipantFlags { get; set; }
    public int ReadOnly { get; set; }
    public bool HasPassword { get; set; }
    public bool HasCall { get; set; }
    public int CallStartTime { get; set; }
    public int CallRecording { get; set; }
    public bool CanStartCall { get; set; }
    public long LastActivity { get; set; }
    public int LastReadMessage { get; set; }
    public int UnreadMessages { get; set; }
    public bool UnreadMention { get; set; }
    public bool UnreadMentionDirect { get; set; }
    public bool IsFavorite { get; set; }
    public bool CanLeaveConversation { get; set; }
    public bool CanDeleteConversation { get; set; }
    public int NotificationLevel { get; set; }
    public int NotificationCalls { get; set; }
    public int LobbyState { get; set; }
    public int LobbyTimer { get; set; }
    public int LastPing { get; set; }
    public string SessionId { get; set; }
    public LastMessage LastMessage { get; set; }
    public int SipEnabled { get; set; }
    public string ActorType { get; set; }
    public string ActorId { get; set; }
    public int AttendeeId { get; set; }
    public int Permissions { get; set; }
    public int AttendeePermissions { get; set; }
    public int CallPermissions { get; set; }
    public int DefaultPermissions { get; set; }
    public bool CanEnableSIP { get; set; }
    public string AttendeePin { get; set; }
    public string Description { get; set; }
    public int LastCommonReadMessage { get; set; }
    public int Listable { get; set; }
    public int CallFlag { get; set; }
    public int MessageExpiration { get; set; }
    public string AvatarVersion { get; set; }
    public bool IsCustomAvatar { get; set; }
    public int BreakoutRoomMode { get; set; }
    public int BreakoutRoomStatus { get; set; }
    public int RecordingConsent { get; set; }
}

public record LastMessage
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string ActorType { get; set; }
    public string ActorId { get; set; }
    public string ActorDisplayName { get; set; }
    public long Timestamp { get; set; }
    public string Message { get; set; }
    public string SystemMessage { get; set; }
    public string MessageType { get; set; }
    public bool IsReplyable { get; set; }
    public string ReferenceId { get; set; }
    public Dictionary<string, object> Reactions { get; set; }
    public int ExpirationTimestamp { get; set; }
    public bool Markdown { get; set; }
}

public record Actor
{
    public string Type { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
}

public record User
{
    public string Type { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
}
