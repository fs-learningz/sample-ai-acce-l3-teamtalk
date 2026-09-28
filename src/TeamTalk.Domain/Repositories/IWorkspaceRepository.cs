using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Repositories;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<Workspace>> GetForUserAsync(Guid userId);

    Task AddAsync(Workspace workspace);

    Task UpdateAsync(Workspace workspace);

    Task DeleteAsync(Guid id);
}
