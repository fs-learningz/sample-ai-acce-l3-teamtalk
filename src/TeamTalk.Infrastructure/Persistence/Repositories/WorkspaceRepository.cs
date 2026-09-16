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
}
