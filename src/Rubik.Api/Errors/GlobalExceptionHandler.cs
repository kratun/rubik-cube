using Microsoft.AspNetCore.Diagnostics;

namespace Rubik.Api.Errors;

/// <summary>
/// Turns any unhandled exception into a 500 <see cref="ErrorDetails"/> without exposing the exception to the client.
/// </summary>
public sealed class GlobalExceptionHandler(ErrorReporter reporter) : IExceptionHandler
{
    /// <summary>
    /// The "client closed request" status; nobody reads the response, so it only keeps the status honest.
    /// </summary>
    public const int ClientClosedRequestStatus = 499;

    private const string InternalErrorTitle = "Unexpected error";
    private const string InternalErrorDetail = "An unexpected error occurred. Quote the trace id when reporting it.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        // The caller went away: not a server fault, so no 500 and no Error log.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequestStatus;
            return true;
        }

        var details = reporter.Report(
            httpContext,
            StatusCodes.Status500InternalServerError,
            InternalErrorTitle,
            InternalErrorDetail,
            exception
        );
        httpContext.Response.StatusCode = details.Status;
        await httpContext.Response.WriteAsJsonAsync(details, cancellationToken);
        return true;
    }
}
