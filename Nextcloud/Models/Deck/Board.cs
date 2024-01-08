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
    int Shared,
    int DeletedAt,
    int Id,
    int LastModified,
    object Settings
);