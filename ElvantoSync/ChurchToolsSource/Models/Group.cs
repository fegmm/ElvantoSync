using System.Collections.Generic;

namespace ElvantoSync.ChurchTools.Models;

public record Group
{
    public string Id { get; init; }
    public string Name { get; init; }
    public IReadOnlyList<GroupMember> Members { get; init; } = [];
}

public record GroupMember
{
    public string Id { get; init; }
    public string Email { get; init; }
    public bool IsLeader { get; init; }
}
