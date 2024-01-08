using Nextcloud.Models.Deck;

namespace Nextcloud.Interfaces;

public interface INextcloudDeckClient
{
    Task<Board> CreateBoard(string boardName, string boardColor, CancellationToken cancellationToken = default);
    Task<IEnumerable<Board>> GetBoards(CancellationToken cancellationToken = default);
    Task AddMember(int boardId, string memberId, MemberTypes memberType, bool canEdit, bool canShare, bool canManage, CancellationToken cancellationToken = default);
}