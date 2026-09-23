using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Invitations.Contracts;

public class WorkspaceInvitationResponse
{
    public required Guid Id { get; init; }

    public required Guid WorkspaceId { get; init; }

    public required string WorkspaceName { get; init; }

    public bool WorkspaceHasIcon { get; init; }

    public required string Email { get; init; }

    public required WorkspaceRole Role { get; init; }

    public required InvitationStatus Status { get; init; }

    public required Guid InvitedByUserId { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
