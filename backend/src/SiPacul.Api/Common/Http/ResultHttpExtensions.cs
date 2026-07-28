using SiPacul.Shared.Results;

namespace SiPacul.Api.Common.Http;

internal static class ResultHttpExtensions
{
    public static IResult ToHttpResult<TValue>(
        this Result<TValue> result,
        Func<TValue, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation =>
                StatusCodes.Status400BadRequest,

            ErrorType.NotFound =>
                StatusCodes.Status404NotFound,

            ErrorType.Conflict =>
                StatusCodes.Status409Conflict,

            ErrorType.Failure =>
                StatusCodes.Status500InternalServerError,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        var title = result.Error.Type switch
        {
            ErrorType.Validation =>
                "Validation error",

            ErrorType.NotFound =>
                "Resource not found",

            ErrorType.Conflict =>
                "Resource conflict",

            _ =>
                "Application error"
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: result.Error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.Error.Code
            });
    }
}
