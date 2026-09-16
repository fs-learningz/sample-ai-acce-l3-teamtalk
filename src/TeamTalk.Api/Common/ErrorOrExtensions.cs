using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace TeamTalk.Api.Common;

public static class ErrorOrExtensions
{
    public static ActionResult ToActionResult<TValue>(this ErrorOr<TValue> result, ControllerBase controller)
    {
        if (!result.IsError)
        {
            return controller.Ok(result.Value);
        }

        return ToProblem(result.Errors, controller);
    }

    public static ActionResult ToActionResult(this ErrorOr<Success> result, ControllerBase controller)
    {
        if (!result.IsError)
        {
            return controller.NoContent();
        }

        return ToProblem(result.Errors, controller);
    }

    private static ActionResult ToProblem(List<Error> errors, ControllerBase controller)
    {
        var firstError = errors[0];

        var statusCode = firstError.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(
            statusCode: statusCode,
            title: firstError.Code,
            detail: firstError.Description);
    }
}
