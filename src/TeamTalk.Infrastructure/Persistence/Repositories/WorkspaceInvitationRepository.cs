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

    public async Task<WorkspaceInvitation?> GetByIdAsync(Guid id)
    {
        return await _dbContext.WorkspaceInvitations.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IReadOnlyList<WorkspaceInvitation>> GetPendingForEmailAsync(string email)
    {
        return await _dbContext.WorkspaceInvitations
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkspaceInvitation>> GetForWorkspaceAsync(Guid workspaceId)
    {
        return await _dbContext.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<WorkspaceInvitation?> GetPendingByWorkspaceAndEmailAsync(Guid workspaceId, string email)
    {
        return await _dbContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.WorkspaceId == workspaceId && i.Email == email && i.Status == InvitationStatus.Pending);
    }

    public async Task AddAsync(WorkspaceInvitation invitation)
    {
        await _dbContext.WorkspaceInvitations.AddAsync(invitation);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkspaceInvitation invitation)
    {
        _dbContext.WorkspaceInvitations.Update(invitation);
        await _dbContext.SaveChangesAsync();
    }
}
