using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Invitations.Contracts;

public class InviteMemberRequest
{
    public required string Email { get; init; }

    public WorkspaceRole Role { get; init; } = WorkspaceRole.Member;
}
