using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IWorkspaceInvitationRepository
{
    Task<WorkspaceInvitation?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<WorkspaceInvitation>> GetPendingForEmailAsync(string email);

    Task<IReadOnlyList<WorkspaceInvitation>> GetForWorkspaceAsync(Guid workspaceId);

    Task<WorkspaceInvitation?> GetPendingByWorkspaceAndEmailAsync(Guid workspaceId, string email);

    Task AddAsync(WorkspaceInvitation invitation);

    Task UpdateAsync(WorkspaceInvitation invitation);
}
