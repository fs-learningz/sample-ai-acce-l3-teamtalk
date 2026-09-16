using ErrorOr;
using TeamTalk.Application.Workspaces.Contracts;

namespace TeamTalk.Application.Workspaces;

public interface IWorkspaceService
{
    Task<ErrorOr<WorkspaceResponse>> CreateAsync(Guid userId, CreateWorkspaceRequest request, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<WorkspaceResponse>>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceResponse>> GetByIdAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);
}
