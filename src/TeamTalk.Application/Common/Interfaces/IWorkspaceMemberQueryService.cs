using TeamTalk.Application.Members.Contracts;

namespace TeamTalk.Application.Common.Interfaces;

public interface IWorkspaceMemberQueryService
{
    Task<IReadOnlyList<WorkspaceMemberResponse>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);
}
