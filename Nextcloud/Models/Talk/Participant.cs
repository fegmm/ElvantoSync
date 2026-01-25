using System.Text.Json.Serialization;

namespace Nextcloud.Models.Talk;

public class Participant
{
    /// <summary>
    /// Unique attendee id
    /// </summary>
    [JsonPropertyName("attendeeId")]
    public int AttendeeId { get; set; }

    /// <summary>
    /// Currently known users |guests|emails|groups|circles
    /// </summary>
    [JsonPropertyName("actorType")]
    public string ActorType { get; set; }

    /// <summary>
    /// The unique identifier for the given actor type
    /// </summary>
    [JsonPropertyName("actorId")]
    public string ActorId { get; set; }

    /// <summary>
    /// Can be empty for guests
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    /// <summary>
    /// Permissions level of the participant (see constants list)
    /// </summary>
    [JsonPropertyName("participantType")]
    public int ParticipantType { get; set; }

    /// <summary>
    /// Timestamp of the last ping of the user (should be used for sorting)
    /// </summary>
    [JsonPropertyName("lastPing")]
    public int LastPing { get; set; }

    /// <summary>
    /// Call flags the user joined with (see constants list)
    /// </summary>
    [JsonPropertyName("inCall")]
    public int InCall { get; set; }

    /// <summary>
    /// Combined final permissions for the participant, permissions are picked in order of attendee then call then default and the first which is Custom will apply (see constants list)
    /// </summary>
    [JsonPropertyName("permissions")]
    public int Permissions { get; set; }

    /// <summary>
    /// Dedicated permissions for the current participant, if not Custom this are not the resulting permissions (see constants list)
    /// </summary>
    [JsonPropertyName("attendeePermissions")]
    public int AttendeePermissions { get; set; }

    /// <summary>
    /// '0' if not connected, otherwise a 512 character long string
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; }

    /// <summary>
    /// array of session ids, each are 512 character long strings, or empty if no session
    /// </summary>
    [JsonPropertyName("sessionIds")]
    public string[] SessionIds { get; set; }

    /// <summary>
    /// Optional: Only available with includeStatus=true, for users with a set status and when there are less than 100 participants in the conversation
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Optional: Only available with includeStatus=true, for users with a set status and when there are less than 100 participants in the conversation
    /// </summary>
    [JsonPropertyName("statusIcon")]
    public string? StatusIcon { get; set; }

    /// <summary>
    /// Optional: Only available with includeStatus=true, for users with a set status and when there are less than 100 participants in the conversation
    /// </summary>
    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Optional: Only available with breakout-rooms-v1 capability
    /// </summary>
    [JsonPropertyName("roomToken")]
    public string? RoomToken { get; set; }

    /// <summary>
    /// Optional: Only available with sip-support-dialout capability and only filled for moderators that are allowed to configure SIP for conversations
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Optional: Only available with sip-support-dialout capability and only filled for moderators that are allowed to configure SIP for conversations
    /// </summary>
    [JsonPropertyName("callId")]
    public string? CallId { get; set; }
}

