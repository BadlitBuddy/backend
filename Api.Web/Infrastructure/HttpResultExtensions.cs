using Api.Application.Common.Models;
using Api.Application.Common.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Web.Infrastructure;

public static class HttpResultExtensions
{
    public static Results<ProblemHttpResult, Ok<T>> ToHttpResult<T>(this Result<T> result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(result.Value);
        }

        return CreateProblemResult(result);
    }

    public static Results<ProblemHttpResult, NoContent> ToHttpResult(this Result result)
    {
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return CreateProblemResult(result);
    }


    public static ProblemHttpResult ToProblemResult(this Result result)
    {
        return CreateProblemResult(result);
    }

    private static ProblemHttpResult CreateProblemResult(Result result)
    {
        var errorType = result.ErrorType ?? ErrorTypes.BadRequest;

        var (statusCode, title) = errorType switch
        {
            ErrorTypes.Validation => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            ErrorTypes.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorTypes.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorTypes.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorTypes.BadRequest or _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        var extensions = new Dictionary<string, object?>
        {
            { "errors", result.Errors }
        };

        return TypedResults.Problem(
            detail: result.Errors.Length == 1 ? result.Errors[0] : "One or more errors occurred.",
            statusCode: statusCode,
            title: title,
            extensions: extensions
        );
    }
}