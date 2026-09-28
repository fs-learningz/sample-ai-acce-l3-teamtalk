using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<Message>> GetChannelHistoryAsync(
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize);

    Task AddAsync(Message message);

    Task UpdateAsync(Message message);
}
