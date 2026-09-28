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

    public async Task<Workspace?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<IReadOnlyList<Workspace>> GetForUserAsync(Guid userId)
    {
        var workspaceIds = _dbContext.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.WorkspaceId);

        return await _dbContext.Workspaces
            .Where(w => workspaceIds.Contains(w.Id))
            .OrderBy(w => w.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task AddAsync(Workspace workspace)
    {
        await _dbContext.Workspaces.AddAsync(workspace);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Workspace workspace)
    {
        _dbContext.Workspaces.Update(workspace);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var channelIds = _dbContext.Channels.Where(c => c.WorkspaceId == id).Select(c => c.Id);

            await _dbContext.Messages.Where(m => channelIds.Contains(m.ChannelId)).ExecuteDeleteAsync();
            await _dbContext.ChannelMembers.Where(cm => channelIds.Contains(cm.ChannelId)).ExecuteDeleteAsync();
            await _dbContext.Channels.Where(c => c.WorkspaceId == id).ExecuteDeleteAsync();
            await _dbContext.WorkspaceInvitations.Where(i => i.WorkspaceId == id).ExecuteDeleteAsync();
            await _dbContext.WorkspaceMembers.Where(m => m.WorkspaceId == id).ExecuteDeleteAsync();
            await _dbContext.Workspaces.Where(w => w.Id == id).ExecuteDeleteAsync();

            await transaction.CommitAsync();
        });
    }
}
