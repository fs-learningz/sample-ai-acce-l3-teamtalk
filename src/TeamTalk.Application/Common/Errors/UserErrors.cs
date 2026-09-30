using ErrorOr;

namespace TeamTalk.Application.Common.Errors;

public static class UserErrors
{
    public static Error NotFound
    {
        get { return Error.NotFound("User.NotFound", "The user was not found."); }
    }

    public static Error InvalidTheme
    {
        get { return Error.Validation("User.InvalidTheme", "The selected theme is not available."); }
    }
}
