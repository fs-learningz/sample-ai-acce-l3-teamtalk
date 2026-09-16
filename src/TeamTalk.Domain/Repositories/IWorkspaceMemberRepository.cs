using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IWorkspaceMemberRepository
{
    Task<WorkspaceMember?> GetAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(WorkspaceMember member, CancellationToken cancellationToken);
}
