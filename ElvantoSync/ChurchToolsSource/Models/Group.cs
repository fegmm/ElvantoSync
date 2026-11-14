using System.Collections.Generic;

namespace ElvantoSync.ChurchTools.Models;

public record Group
{
    public int Id { get; init; }
    public string Name { get; init; }
    public int? GroupTypeId { get; init; }
    public IReadOnlyList<GroupMember> Members { get; init; } = [];
}

public record GroupMember
{
    public string Id { get; init; }
    public string Email { get; init; }
    public bool IsLeader { get; init; }
}
