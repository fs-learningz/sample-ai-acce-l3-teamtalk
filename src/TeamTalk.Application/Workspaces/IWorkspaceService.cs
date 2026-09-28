using ErrorOr;
using TeamTalk.Application.Workspaces.Contracts;

namespace TeamTalk.Application.Workspaces;

public interface IWorkspaceService
{
    Task<ErrorOr<WorkspaceResponse>> CreateAsync(Guid userId, CreateWorkspaceRequest request);

    Task<ErrorOr<IReadOnlyList<WorkspaceResponse>>> GetForUserAsync(Guid userId);

    Task<ErrorOr<WorkspaceResponse>> GetByIdAsync(Guid userId, Guid workspaceId);

    Task<ErrorOr<WorkspaceResponse>> UpdateAsync(Guid userId, Guid workspaceId, UpdateWorkspaceRequest request);

    Task<ErrorOr<WorkspaceResponse>> UpdateIconAsync(Guid userId, Guid workspaceId, byte[] iconData, string contentType);

    Task<ErrorOr<WorkspaceResponse>> RemoveIconAsync(Guid userId, Guid workspaceId);

    Task<ErrorOr<WorkspaceIconResponse>> GetIconAsync(Guid userId, Guid workspaceId);

    Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid workspaceId);
}
