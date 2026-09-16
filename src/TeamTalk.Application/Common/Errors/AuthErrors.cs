using ErrorOr;

namespace TeamTalk.Application.Common.Errors;

public static class AuthErrors
{
    public static Error EmailAlreadyExists
    {
        get { return Error.Conflict("Auth.EmailAlreadyExists", "An account with this email address already exists."); }
    }

    public static Error InvalidCredentials
    {
        get { return Error.Unauthorized("Auth.InvalidCredentials", "The email or password is incorrect."); }
    }

    public static Error RegistrationFailed(string description)
    {
        return Error.Validation("Auth.RegistrationFailed", description);
    }
}
