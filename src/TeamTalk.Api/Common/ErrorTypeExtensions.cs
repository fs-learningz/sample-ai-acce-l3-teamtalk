using ErrorOr;

namespace TeamTalk.Api.Common;

public static class ErrorTypeExtensions
{
    public static int TypeToCode(this ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
