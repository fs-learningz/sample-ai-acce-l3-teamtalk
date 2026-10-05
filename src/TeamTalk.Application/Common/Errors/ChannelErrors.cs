using ErrorOr;

namespace TeamTalk.Application.Common.Errors;

public static class ChannelErrors
{
    public static Error NotFound
    {
        get { return Error.NotFound("Channel.NotFound", "The channel was not found."); }
    }

    public static Error NameAlreadyExists
    {
        get { return Error.Conflict("Channel.NameAlreadyExists", "A channel with this name already exists in the workspace."); }
    }

    public static Error NotVisible
    {
        get { return Error.Forbidden("Channel.NotVisible", "You do not have access to this channel."); }
    }

    public static Error NotAMember
    {
        get { return Error.Forbidden("Channel.NotAMember", "You are not a member of this channel."); }
    }

    public static Error CannotJoinPrivateChannel
    {
        get { return Error.Forbidden("Channel.CannotJoinPrivateChannel", "You cannot join a private channel without an invitation."); }
    }
}
