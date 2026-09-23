using ErrorOr;
using TeamTalk.Application.Invitations.Contracts;
using TeamTalk.Application.Workspaces.Contracts;

namespace TeamTalk.Application.Invitations;

public interface IWorkspaceInvitationService
{
    Task<ErrorOr<WorkspaceInvitationResponse>> InviteAsync(
        Guid inviterUserId,
        Guid workspaceId,
        InviteMemberRequest request,
        CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<WorkspaceInvitationResponse>>> GetForWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<WorkspaceInvitationResponse>>> GetPendingForCurrentUserAsync(
        Guid userId,
        string userEmail,
        CancellationToken cancellationToken);

    Task<ErrorOr<Success>> AcceptAsync(Guid userId, string userEmail, Guid invitationId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> DeclineAsync(Guid userId, string userEmail, Guid invitationId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> RevokeAsync(Guid userId, Guid workspaceId, Guid invitationId, CancellationToken cancellationToken);

    Task<ErrorOr<WorkspaceIconResponse>> GetWorkspaceIconAsync(string userEmail, Guid invitationId, CancellationToken cancellationToken);
}
