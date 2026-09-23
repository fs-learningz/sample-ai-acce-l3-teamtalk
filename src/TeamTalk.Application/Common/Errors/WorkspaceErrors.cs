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

    public static Error LastAdmin
    {
        get { return Error.Conflict("Workspace.LastAdmin", "A workspace needs at least one admin."); }
    }

    public static Error IconTooLarge
    {
        get { return Error.Validation("Workspace.IconTooLarge", "The workspace icon cannot exceed 2 MB."); }
    }

    public static Error UnsupportedIconFormat
    {
        get { return Error.Validation("Workspace.UnsupportedIconFormat", "The workspace icon must be a PNG, JPG, or SVG file."); }
    }

    public static Error IconNotFound
    {
        get { return Error.NotFound("Workspace.IconNotFound", "This workspace does not have an icon."); }
    }

    public static Error MemberNotFound
    {
        get { return Error.NotFound("Workspace.MemberNotFound", "The specified member was not found in this workspace."); }
    }

    public static Error CannotRemoveSelf
    {
        get { return Error.Validation("Workspace.CannotRemoveSelf", "Use the leave workspace action to remove yourself."); }
    }

}
