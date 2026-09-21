using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Members.Contracts;

public class WorkspaceMemberResponse
{
    public required Guid Id { get; init; }

    public required Guid WorkspaceId { get; init; }

    public required Guid UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public required WorkspaceRole Role { get; init; }

    public required DateTime JoinedAtUtc { get; init; }
}
