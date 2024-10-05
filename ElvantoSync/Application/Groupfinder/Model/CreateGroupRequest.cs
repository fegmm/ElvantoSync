namespace ElvantoSync.GroupFinder.Model;

public record Address
{
    public string Street { get; set; }
    public int PostalCode { get; set; }
    public string City { get; set; }
}

public record Leader
{
    public string ElvantoId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public record CreateGroupRequest
{
    public string Id { get; set; }
    public string ModifiedAt { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
    public Leader Leader { get; set; }
    public string MeetingDay { get; set; }
    public string MeetingTime { get; set; }
    public string MeetingFrequency { get; set; }
    public int MaxCapacity { get; set; }
}
