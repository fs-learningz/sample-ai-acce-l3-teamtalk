using ErrorOr;

namespace TeamTalk.Application.Common.Errors;

public static class InvitationErrors
{
    public static Error NotFound
    {
        get { return Error.NotFound("Invitation.NotFound", "The invitation was not found."); }
    }

    public static Error AlreadyPending
    {
        get { return Error.Conflict("Invitation.AlreadyPending", "This email already has a pending invitation to this workspace."); }
    }

    public static Error AlreadyAMember
    {
        get { return Error.Conflict("Invitation.AlreadyAMember", "This user is already a member of this workspace."); }
    }

    public static Error NotPending
    {
        get { return Error.Conflict("Invitation.NotPending", "This invitation has already been responded to."); }
    }
}
