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

    public async Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Channels.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsInWorkspaceAsync(Guid workspaceId, string name, CancellationToken cancellationToken)
    {
        return await _dbContext.Channels
            .AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Channel>> GetVisibleToUserAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var memberChannelIds = _dbContext.ChannelMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ChannelId);

        return await _dbContext.Channels
            .Where(c => c.WorkspaceId == workspaceId
                && (c.Type == ChannelType.Public || memberChannelIds.Contains(c.Id)))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Channel channel, CancellationToken cancellationToken)
    {
        await _dbContext.Channels.AddAsync(channel, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
