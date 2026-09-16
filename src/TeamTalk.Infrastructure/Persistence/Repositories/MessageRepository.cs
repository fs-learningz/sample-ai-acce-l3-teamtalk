using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public MessageRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetChannelHistoryAsync(
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Messages.Where(m => m.ChannelId == channelId);

        if (beforeUtc.HasValue)
        {
            query = query.Where(m => m.SentAtUtc < beforeUtc.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAtUtc)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken)
    {
        await _dbContext.Messages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Message message, CancellationToken cancellationToken)
    {
        _dbContext.Messages.Update(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
