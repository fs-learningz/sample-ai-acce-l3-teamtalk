using TeamTalk.Domain.Enums;

namespace TeamTalk.Application.Members.Contracts;

public class ChangeMemberRoleRequest
{
    public required WorkspaceRole Role { get; init; }
}
