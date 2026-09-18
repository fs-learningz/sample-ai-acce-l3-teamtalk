using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Workspaces.Contracts;

public class WorkspaceResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required Guid CreatedByUserId { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public WorkspaceRole? CurrentUserRole { get; init; }
}
