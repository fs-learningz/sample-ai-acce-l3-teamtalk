using TeamTalk.Domain.Enums;

namespace TeamTalk.Domain.Entities;

public class WorkspaceMember
{
    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public WorkspaceRole Role { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public WorkspaceMember(Guid id, Guid workspaceId, Guid userId, WorkspaceRole role, DateTime joinedAtUtc)
    {
        Id = id;
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    private WorkspaceMember()
    {
    }

    public bool IsAdmin()
    {
        return Role == WorkspaceRole.Admin;
    }
}
