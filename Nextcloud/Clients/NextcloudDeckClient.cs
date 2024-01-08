using Nextcloud.Interfaces;
using Nextcloud.Models.Deck;
using System.Net.Http.Json;

namespace Nextcloud.Clients;

internal class NextcloudDeckClient(HttpClient client) : INextcloudDeckClient
{
    public async Task<IEnumerable<Board>> GetBoards(CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync("/index.php/apps/deck/api/v1.1/boards", cancellationToken);
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<IEnumerable<Board>>(cancellationToken);
        return result ?? new List<Board>();
    }

    public async Task<Board> CreateBoard(string boardName, string boardColor, CancellationToken cancellationToken = default)
    {
        var reqBody = new
        {
            title = boardName,
            color = boardColor
        };
        var response = await client.PostAsJsonAsync("index.php/apps/deck/api/v1.1/boards", reqBody, cancellationToken);
        var result = await response.EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<Board>(cancellationToken);
        return result!;
    }

    public async Task AddMember(int boardId, string memberId, MemberTypes memberType, bool canEdit, bool canShare, bool canManage, CancellationToken cancellationToken = default)
    {
        var reqBody = new
        {
            type = (int)memberType,
            participant = memberId,
            permissionEdit = canEdit,
            permissionShare = canShare,
            permissionManage = canManage
        };
        var response = await client.PostAsJsonAsync($"index.php/apps/deck/api/v1.1/boards/{boardId}/acl", reqBody, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
