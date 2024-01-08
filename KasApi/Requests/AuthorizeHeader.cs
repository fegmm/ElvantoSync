namespace KasApi.Requests;

public record AuthorizeHeader(
    string? kas_login,
    string? kas_auth_type,
    string? kas_auth_data
);
