using Nextcloud.Models.Collectives;

namespace Nextcloud.Interfaces;

public interface INextcloudCollectivesClient
{
    Task<Collective> CreateCollective(string name, CancellationToken cancellationToken = default);
    Task<Collective[]> GetCollectives(CancellationToken cancellationToken = default);
    Task SetDisplayName(string circleId, string name, CancellationToken cancellationToken = default);
}