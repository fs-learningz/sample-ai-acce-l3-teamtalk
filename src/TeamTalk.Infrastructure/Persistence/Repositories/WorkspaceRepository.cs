using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public WorkspaceRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Workspace>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var workspaceIds = _dbContext.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.WorkspaceId);

        return await _dbContext.Workspaces
            .Where(w => workspaceIds.Contains(w.Id))
            .OrderBy(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        await _dbContext.Workspaces.AddAsync(workspace, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        _dbContext.Workspaces.Update(workspace);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var channelIds = _dbContext.Channels.Where(c => c.WorkspaceId == id).Select(c => c.Id);

            await _dbContext.Messages.Where(m => channelIds.Contains(m.ChannelId)).ExecuteDeleteAsync(cancellationToken);
            await _dbContext.ChannelMembers.Where(cm => channelIds.Contains(cm.ChannelId)).ExecuteDeleteAsync(cancellationToken);
            await _dbContext.Channels.Where(c => c.WorkspaceId == id).ExecuteDeleteAsync(cancellationToken);
            await _dbContext.WorkspaceInvitations.Where(i => i.WorkspaceId == id).ExecuteDeleteAsync(cancellationToken);
            await _dbContext.WorkspaceMembers.Where(m => m.WorkspaceId == id).ExecuteDeleteAsync(cancellationToken);
            await _dbContext.Workspaces.Where(w => w.Id == id).ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
