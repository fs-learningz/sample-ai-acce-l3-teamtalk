using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public ChannelRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Channel?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Channels.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> NameExistsInWorkspaceAsync(Guid workspaceId, string name)
    {
        return await _dbContext.Channels
            .AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == name);
    }

    public async Task<IReadOnlyList<Channel>> GetVisibleToUserAsync(Guid workspaceId, Guid userId)
    {
        var memberChannelIds = _dbContext.ChannelMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ChannelId);

        return await _dbContext.Channels
            .Where(c => c.WorkspaceId == workspaceId
                && (c.Type == ChannelType.Public || memberChannelIds.Contains(c.Id)))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Channel channel)
    {
        await _dbContext.Channels.AddAsync(channel);
        await _dbContext.SaveChangesAsync();
    }
}
