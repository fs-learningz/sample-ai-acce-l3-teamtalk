using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Repositories;

public interface IWorkspaceMemberRepository
{
    Task<WorkspaceMember?> GetAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task<int> CountAdminsAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(WorkspaceMember member, CancellationToken cancellationToken);

    Task RemoveAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically removes the member unless doing so would leave the workspace with zero Admins.
    /// Returns false when blocked by the last-admin rule.
    /// </summary>
    Task<bool> TryLeaveAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically changes the member's role unless doing so would leave the workspace with zero
    /// Admins. Returns false when blocked by the last-admin rule.
    /// </summary>
    Task<bool> TrySetRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken cancellationToken);
}
