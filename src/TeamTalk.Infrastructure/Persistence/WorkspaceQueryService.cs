using Microsoft.EntityFrameworkCore;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;

namespace TeamTalk.Infrastructure.Persistence;

public class WorkspaceQueryService : IWorkspaceQueryService
{
    private readonly TeamTalkDbContext _dbContext;

    public WorkspaceQueryService(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkspaceSummary?> GetSummaryAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await _dbContext.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => new { w.CreatedByUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (workspace is null)
        {
            return null;
        }

        var creator = await _dbContext.Users
            .Where(u => u.Id == workspace.CreatedByUserId)
            .Select(u => new { u.DisplayName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var memberCount = await _dbContext.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId, cancellationToken);

        return new WorkspaceSummary(
            creator?.DisplayName ?? string.Empty,
            creator?.Email ?? string.Empty,
            memberCount);
    }
}
