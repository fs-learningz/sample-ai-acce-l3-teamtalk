using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class ChannelUserStateRepository : IChannelUserStateRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public ChannelUserStateRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChannelUserState?> GetAsync(Guid channelId, Guid userId)
    {
        return await _dbContext.ChannelUserStates
            .FirstOrDefaultAsync(s => s.ChannelId == channelId && s.UserId == userId);
    }

    public async Task AddAsync(ChannelUserState state)
    {
        await _dbContext.ChannelUserStates.AddAsync(state);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChannelUserState state)
    {
        _dbContext.ChannelUserStates.Update(state);
        await _dbContext.SaveChangesAsync();
    }
}
