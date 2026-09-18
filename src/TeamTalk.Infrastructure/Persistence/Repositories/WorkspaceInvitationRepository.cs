using Microsoft.EntityFrameworkCore;
using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Domain.Repositories;

namespace TeamTalk.Infrastructure.Persistence.Repositories;

public class WorkspaceInvitationRepository : IWorkspaceInvitationRepository
{
    private readonly TeamTalkDbContext _dbContext;

    public WorkspaceInvitationRepository(TeamTalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkspaceInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceInvitations.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceInvitation>> GetPendingForEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceInvitations
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceInvitation>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceInvitation?> GetPendingByWorkspaceAndEmailAsync(Guid workspaceId, string email, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.WorkspaceId == workspaceId && i.Email == email && i.Status == InvitationStatus.Pending,
                cancellationToken);
    }

    public async Task AddAsync(WorkspaceInvitation invitation, CancellationToken cancellationToken)
    {
        await _dbContext.WorkspaceInvitations.AddAsync(invitation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkspaceInvitation invitation, CancellationToken cancellationToken)
    {
        _dbContext.WorkspaceInvitations.Update(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
