using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Rubik.Api.Errors;

/// <summary>
/// Gives an empty 4xx or 5xx response that no controller produced (unknown route, 405, 415) the same
/// <see cref="ErrorDetails"/> body and log entry as every other failure (AD-12).
/// </summary>
public sealed class StatusCodeErrorWriter(ErrorReporter reporter)
{
    private const string UnhandledDetail =
        "The request could not be completed. Check the method, path and content type.";

    public async Task WriteAsync(StatusCodeContext context)
    {
        var httpContext = context.HttpContext;

        // The caller went away (GlobalExceptionHandler sets 499): nobody reads a body, and it is not a failure to log.
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        var status = httpContext.Response.StatusCode;
        var title = ReasonPhrases.GetReasonPhrase(status);
        var details = reporter.Report(httpContext, status, title, UnhandledDetail);
        await httpContext.Response.WriteAsJsonAsync(details, httpContext.RequestAborted);
    }
}
