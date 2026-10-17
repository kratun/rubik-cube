using Microsoft.AspNetCore.Mvc;
using Rubik.Application.Cubes;
using Rubik.Application.Results;

namespace Rubik.Api.Errors;

/// <summary>The one place that turns a <see cref="Result{T}"/> or a model-state failure into an HTTP response (AD-6).</summary>
public sealed class ResultMapper(ErrorReporter reporter)
{
    private const string NotFoundTitle = "Cube not found";
    private const string NotFoundDetailFormat = "No cube exists with id {0}.";
    private const string InvalidMoveTitle = "Invalid move";
    private const string InvalidMoveDetailFormat = "\"{0}\" is not a move. Use F, R, U, B, L or D, optionally followed by '.";
    private const string NullMoveDetail = "A move must be a string such as F or R', not null.";
    private const string ConflictTitle = "Revision conflict";
    private const string ConflictDetail = "The cube has changed since the expected revision. Read it again and retry.";
    private const string InvalidRequestTitle = "Invalid request";
    private const string InvalidRequestDetail = "The request is not valid.";
    private const string UnmappedErrorFormat = "No HTTP mapping for error {0}.";

    /// <summary>200 with the value, or the error's status with an <see cref="ErrorDetails"/> body. An unmapped error throws, so it surfaces as 500.</summary>
    public IActionResult ToActionResult<T>(Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var details = result.Error switch
        {
            CubeNotFound notFound => reporter.Report(
                context,
                StatusCodes.Status404NotFound,
                NotFoundTitle,
                string.Format(NotFoundDetailFormat, notFound.CubeId)),
            InvalidMove invalid => reporter.Report(
                context,
                StatusCodes.Status400BadRequest,
                InvalidMoveTitle,
                invalid.Token is null ? NullMoveDetail : string.Format(InvalidMoveDetailFormat, invalid.Token)),
            RevisionConflict => reporter.Report(
                context,
                StatusCodes.Status409Conflict,
                ConflictTitle,
                ConflictDetail),
            _ => throw new InvalidOperationException(string.Format(UnmappedErrorFormat, result.Error.GetType().Name)),
        };

        return ToObjectResult(details);
    }

    /// <summary>400 for a request that failed model binding or validation. The model errors are not echoed: they can contain raw input.</summary>
    public IActionResult ToBadRequest(HttpContext context)
    {
        var details = reporter.Report(context, StatusCodes.Status400BadRequest, InvalidRequestTitle, InvalidRequestDetail);
        return ToObjectResult(details);
    }

    private static ObjectResult ToObjectResult(ErrorDetails details) => new(details) { StatusCode = details.Status };
}
