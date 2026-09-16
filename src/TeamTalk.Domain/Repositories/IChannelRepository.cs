using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IChannelRepository
{
    Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsInWorkspaceAsync(Guid workspaceId, string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Channel>> GetVisibleToUserAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Channel channel, CancellationToken cancellationToken);
}
