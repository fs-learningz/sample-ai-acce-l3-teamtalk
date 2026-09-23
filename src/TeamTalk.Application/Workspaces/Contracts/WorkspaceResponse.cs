using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Workspaces.Contracts;

public class WorkspaceResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool HasIcon { get; init; }

    public required Guid CreatedByUserId { get; init; }

    public string? CreatedByDisplayName { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public int? MemberCount { get; init; }

    public WorkspaceRole? CurrentUserRole { get; init; }
}
