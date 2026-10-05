using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IChannelRepository
{
    Task<Channel?> GetByIdAsync(Guid id);

    Task<bool> NameExistsInWorkspaceAsync(Guid workspaceId, string name);

    Task<IReadOnlyList<Channel>> GetVisibleToUserAsync(Guid workspaceId, Guid userId);

    Task AddAsync(Channel channel);

    /// <summary>
    /// Deletes the channel together with its messages, members and per-user state.
    /// </summary>
    Task DeleteAsync(Guid id);
}
