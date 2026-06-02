using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Middleware;
using Toptanci.Application.Common;

namespace Toptanci.Api.Extensions;

/// <summary>Application katmanının Result tipini HTTP yanıtına çevirir.</summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
        => result.IsSuccess
            ? new OkResult()
            : ToError(result.Error);

    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ToError(result.Error);

    private static IActionResult ToError(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        var body = new ErrorResponse(error.Code, error.Message, null, string.Empty);
        return new ObjectResult(body) { StatusCode = status };
    }
}
