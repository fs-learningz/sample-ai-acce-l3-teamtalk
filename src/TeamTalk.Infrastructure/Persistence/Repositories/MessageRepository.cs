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

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<Message>> GetChannelHistoryAsync(
        Guid channelId,
        DateTime? beforeUtc,
        int pageSize)
    {
        var query = _dbContext.Messages.Where(m => m.ChannelId == channelId);

        if (beforeUtc.HasValue)
        {
            query = query.Where(m => m.SentAtUtc < beforeUtc.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAtUtc)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(Message message)
    {
        await _dbContext.Messages.AddAsync(message);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Message message)
    {
        _dbContext.Messages.Update(message);
        await _dbContext.SaveChangesAsync();
    }
}
