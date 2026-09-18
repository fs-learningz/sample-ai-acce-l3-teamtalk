using ErrorOr;

namespace TeamTalk.Application.Common.Errors;

public static class WorkspaceErrors
{
    public static Error NotFound
    {
        get { return Error.NotFound("Workspace.NotFound", "The workspace was not found."); }
    }

    public static Error NotAMember
    {
        get { return Error.Forbidden("Workspace.NotAMember", "You are not a member of this workspace."); }
    }

    public static Error NotAnAdmin
    {
        get { return Error.Forbidden("Workspace.NotAnAdmin", "Only workspace admins can perform this action."); }
    }
}
