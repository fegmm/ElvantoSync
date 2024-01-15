using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public record ResourceIndexEntity
{
    // TODO: resourceID is not unique
    [Key]
    public string TToIdentifier { get; set; }
    public string TFromIdentifier { get; set; }
    public string Type { get; set; }
    // Other properties
}
