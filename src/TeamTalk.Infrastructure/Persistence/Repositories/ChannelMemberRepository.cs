using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class ChannelMemberRepository : IChannelMemberRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public ChannelMemberRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsMemberAsync(Guid channelId, Guid userId)
    {
        return await _dbContext.ChannelMembers
            .AnyAsync(m => m.ChannelId == channelId && m.UserId == userId);
    }

    public async Task AddAsync(ChannelMember member)
    {
        await _dbContext.ChannelMembers.AddAsync(member);
        await _dbContext.SaveChangesAsync();
    }
}
