using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IWorkspaceInvitationRepository
{
    Task<WorkspaceInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceInvitation>> GetPendingForEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceInvitation>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<WorkspaceInvitation?> GetPendingByWorkspaceAndEmailAsync(Guid workspaceId, string email, CancellationToken cancellationToken);

    Task AddAsync(WorkspaceInvitation invitation, CancellationToken cancellationToken);

    Task UpdateAsync(WorkspaceInvitation invitation, CancellationToken cancellationToken);
}
