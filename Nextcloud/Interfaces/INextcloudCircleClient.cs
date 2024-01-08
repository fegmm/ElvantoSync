using Nextcloud.Models.Circles;

namespace ElvantoSync.Infrastructure.Nextcloud;

public interface INextcloudCircleClient
{
    Task<string> AddMemberToCircle(string circleId, string memberId, MemberTypes memberType, CancellationToken cancellationToken = default);
    Task SetMemberLevel(string circleId, string memberId, MemberLevels level, CancellationToken cancellationToken = default);
}