namespace Nextcloud.Models.Deck;

public record Board(
    string Title,
    object Owner,
    string Color,
    bool Archived,
    object Labels,
    object Acl,
    object Permissions,
    object Users,
    bool Shared,
    long DeletedAt,
    int Id,
    long LastModified,
    object Settings
);