using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Rubik.Api.Errors;

/// <summary>
/// The one place that builds <see cref="ErrorDetails"/> and writes the log entry for a failure (AD-12): Warning without
/// exception for expected failures, Error with the exception for unexpected ones. The mapper and the exception handler
/// only choose status and detail.
/// </summary>
public sealed partial class ErrorReporter
{
    public const string OperationKey = "Operation";
    public const string CubeIdKey = "CubeId";
    public const string StatusKey = "Status";
    public const string TraceIdKey = "TraceId";

    private const string CubeIdRouteKey = "id";
    private const string TypeBaseUri = "https://httpstatuses.io/";
    private const string ActionOperationFormat = "{0}.{1}";
    private const string RequestOperationFormat = "{0} {1}";
    private const string LogMessage =
        "Request failed. Operation={Operation} CubeId={CubeId} Status={Status} TraceId={TraceId}";

    private readonly ILogger<ErrorReporter> logger;

    public ErrorReporter(ILogger<ErrorReporter> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Logs the failure and returns the body to send. Pass <paramref name="exception"/> only for unexpected failures.
    /// </summary>
    public ErrorDetails Report(
        HttpContext context,
        int status,
        string title,
        string detail,
        Exception? exception = null
    )
    {
        var traceId = context.TraceIdentifier;
        var operation = OperationOf(context);
        var cubeId = CubeIdOf(context);

        if (exception is null)
        {
            LogExpected(operation, cubeId, status, traceId);
        }
        else
        {
            LogUnexpected(exception, operation, cubeId, status, traceId);
        }

        return new ErrorDetails($"{TypeBaseUri}{status}", title, status, detail, traceId);
    }

    // The exception handler middleware clears the endpoint and route values, so its feature has the original ones.
    private static string OperationOf(HttpContext context)
    {
        var endpoint = context.Features.Get<IExceptionHandlerFeature>()?.Endpoint ?? context.GetEndpoint();
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is not null)
        {
            return string.Format(ActionOperationFormat, action.ControllerName, action.ActionName);
        }

        var path = context.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? context.Request.Path;
        return string.Format(RequestOperationFormat, context.Request.Method, path);
    }

    private static Guid? CubeIdOf(HttpContext context)
    {
        var routeValues = context.Features.Get<IExceptionHandlerFeature>()?.RouteValues ?? context.Request.RouteValues;
        return
            routeValues.TryGetValue(CubeIdRouteKey, out var value) && Guid.TryParse(value?.ToString(), out var cubeId)
            ? cubeId
            : null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = LogMessage)]
    private partial void LogExpected(string operation, Guid? cubeId, int status, string traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = LogMessage)]
    private partial void LogUnexpected(Exception exception, string operation, Guid? cubeId, int status, string traceId);
}
