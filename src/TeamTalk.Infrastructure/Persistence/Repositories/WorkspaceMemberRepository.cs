using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public WorkspaceMemberRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkspaceMember?> GetAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(WorkspaceMember member, CancellationToken cancellationToken)
    {
        await _dbContext.WorkspaceMembers.AddAsync(member, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var channelIds = _dbContext.Channels.Where(c => c.WorkspaceId == workspaceId).Select(c => c.Id);

            await _dbContext.ChannelMembers
                .Where(cm => cm.UserId == userId && channelIds.Contains(cm.ChannelId))
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.WorkspaceMembers
                .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
