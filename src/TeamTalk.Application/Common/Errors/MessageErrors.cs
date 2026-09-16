using ErrorOr;

namespace TeamTalk.Application.Common.Errors;

public static class MessageErrors
{
    public static Error NotFound
    {
        get { return Error.NotFound("Message.NotFound", "The message was not found."); }
    }

    public static Error NotAuthor
    {
        get { return Error.Forbidden("Message.NotAuthor", "Only the author can edit this message."); }
    }

    public static Error NotAuthorOrAdmin
    {
        get { return Error.Forbidden("Message.NotAuthorOrAdmin", "Only the author or a workspace admin can delete this message."); }
    }

    public static Error AlreadyDeleted
    {
        get { return Error.Validation("Message.AlreadyDeleted", "This message has already been deleted."); }
    }
}
