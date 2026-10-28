using System;
using System.Collections.Generic;

namespace ElvantoSync.ChurchTools.Models;

public record Person
{
    public string Id { get; init; }
    public IReadOnlyCollection<int> AreaIds { get; init; } = [];
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }
    public string PhonePrivate { get; init; }
    public string Mobile { get; init; }
    public string ImageUrl { get; init; }
    public DateTime? Birthday { get; init; }
    public DateTimeOffset? LastEditedDate { get; init; }
}
