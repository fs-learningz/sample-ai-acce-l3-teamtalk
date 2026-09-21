using Microsoft.EntityFrameworkCore;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Members.Contracts;

namespace TeamTalk.Infrastructure.Persistence;

public class WorkspaceMemberQueryService : IWorkspaceMemberQueryService
{
    private readonly TeamTalkDbContext _dbContext;

    public WorkspaceMemberQueryService(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkspaceMemberResponse>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await (
            from m in _dbContext.WorkspaceMembers
            join u in _dbContext.Users on m.UserId equals u.Id
            where m.WorkspaceId == workspaceId
            orderby m.JoinedAtUtc
            select new WorkspaceMemberResponse
            {
                Id = m.Id,
                WorkspaceId = m.WorkspaceId,
                UserId = m.UserId,
                DisplayName = u.DisplayName,
                Email = u.Email!,
                Role = m.Role,
                JoinedAtUtc = m.JoinedAtUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
