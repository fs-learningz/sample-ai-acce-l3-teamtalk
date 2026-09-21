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

    public static Error NotOwner
    {
        get { return Error.Forbidden("Workspace.NotOwner", "Only the workspace owner can delete this workspace."); }
    }

    public static Error MemberNotFound
    {
        get { return Error.NotFound("Workspace.MemberNotFound", "The specified member was not found in this workspace."); }
    }

    public static Error CannotRemoveSelf
    {
        get { return Error.Validation("Workspace.CannotRemoveSelf", "Use the leave workspace action to remove yourself."); }
    }

    public static Error CannotRemoveOwner
    {
        get { return Error.Validation("Workspace.CannotRemoveOwner", "The workspace owner cannot be removed."); }
    }

    public static Error OwnerCannotLeave
    {
        get { return Error.Validation("Workspace.OwnerCannotLeave", "The workspace owner cannot leave. Delete the workspace instead."); }
    }
}
