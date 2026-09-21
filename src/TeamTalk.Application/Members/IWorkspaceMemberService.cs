using ErrorOr;
using TeamTalk.Application.Members.Contracts;

namespace TeamTalk.Application.Members;

public interface IWorkspaceMemberService
{
    Task<ErrorOr<IReadOnlyList<WorkspaceMemberResponse>>> GetForWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> RemoveAsync(Guid adminUserId, Guid workspaceId, Guid memberUserId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> LeaveAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);
}
