using ErrorOr;
using TeamTalk.Application.Workspaces.Contracts;

namespace TeamTalk.Application.Workspaces;

public interface IWorkspaceService
{
    Task<ErrorOr<WorkspaceResponse>> CreateAsync(Guid userId, CreateWorkspaceRequest request, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<WorkspaceResponse>>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceResponse>> GetByIdAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceResponse>> UpdateAsync(Guid userId, Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceResponse>> UpdateIconAsync(Guid userId, Guid workspaceId, byte[] iconData, string contentType, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceResponse>> RemoveIconAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceIconResponse>> GetIconAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);
}
