using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Workspace>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Workspace workspace, CancellationToken cancellationToken);
}
