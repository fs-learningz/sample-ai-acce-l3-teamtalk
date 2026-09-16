using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Message>> GetChannelHistoryAsync(
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddAsync(Message message, CancellationToken cancellationToken);

    Task UpdateAsync(Message message, CancellationToken cancellationToken);
}
