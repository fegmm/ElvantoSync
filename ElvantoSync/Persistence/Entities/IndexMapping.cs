namespace ElvantoSync.Persistence.Entities;

public record IndexMapping
{
    public string ToId { get; set; }
    public string FromId { get; set; }
    public string Type { get; set; }
}
