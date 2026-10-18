using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Rubik.Api.Errors;

namespace Rubik.Api.Tests.Errors;

public class ErrorReporterTests
{
    private const string TraceId = "trace-1";
    private const string Title = "A title";
    private const string Detail = "A detail";
    private const string ControllerName = "Cubes";
    private const string ActionName = "Get";
    private const string Method = "GET";
    private const string RequestPath = "/api/other";
    private const string IdRouteKey = "id";
    private const string NotAGuid = "abc";
    private const string ExpectedTypeFormat = "https://httpstatuses.io/{0}";
    private const int Status = StatusCodes.Status404NotFound;

    private readonly FakeLogger<ErrorReporter> logger = new();
    private readonly DefaultHttpContext context = new() { TraceIdentifier = TraceId };

    private ErrorReporter Reporter => new(logger);

    [Fact]
    public void Report_NoException_ReturnsDetailsWithTraceIdAndLogsOneWarningWithoutException()
    {
        var cubeId = Guid.NewGuid();
        context.Request.RouteValues[IdRouteKey] = cubeId.ToString();
        context.SetEndpoint(ActionEndpoint());

        var details = Reporter.Report(context, Status, Title, Detail);

        Assert.Equal(
            new ErrorDetails(string.Format(ExpectedTypeFormat, Status), Title, Status, Detail, TraceId),
            details
        );
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Null(record.Exception);
        AssertState(record, $"{ControllerName}.{ActionName}", cubeId.ToString(), Status, TraceId);
    }

    [Fact]
    public void Report_WithException_LogsOneErrorCarryingTheException()
    {
        var exception = new InvalidOperationException();

        Reporter.Report(context, StatusCodes.Status500InternalServerError, Title, Detail, exception);

        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(exception, record.Exception);
    }

    [Fact]
    public void Report_NoControllerEndpoint_UsesMethodAndPathAsOperation()
    {
        context.Request.Method = Method;
        context.Request.Path = RequestPath;

        Reporter.Report(context, Status, Title, Detail);

        AssertState(
            Assert.Single(logger.Collector.GetSnapshot()),
            $"{Method} {RequestPath}",
            cubeId: null,
            Status,
            TraceId
        );
    }

    [Fact]
    public void Report_IdIsNotAGuid_LogsNoCubeId()
    {
        context.Request.Method = Method;
        context.Request.Path = RequestPath;
        context.Request.RouteValues[IdRouteKey] = NotAGuid;

        Reporter.Report(context, Status, Title, Detail);

        AssertState(
            Assert.Single(logger.Collector.GetSnapshot()),
            $"{Method} {RequestPath}",
            cubeId: null,
            Status,
            TraceId
        );
    }

    [Fact]
    public void Report_InsideTheExceptionHandler_ReadsEndpointAndRouteValuesFromTheFeature()
    {
        var cubeId = Guid.NewGuid();
        context.Features.Set<IExceptionHandlerFeature>(
            new ExceptionHandlerFeature
            {
                Error = new InvalidOperationException(),
                Endpoint = ActionEndpoint(),
                RouteValues = new RouteValueDictionary { [IdRouteKey] = cubeId },
            }
        );

        Reporter.Report(context, Status, Title, Detail);

        AssertState(
            Assert.Single(logger.Collector.GetSnapshot()),
            $"{ControllerName}.{ActionName}",
            cubeId.ToString(),
            Status,
            TraceId
        );
    }

    private static Endpoint ActionEndpoint() =>
        new(
            requestDelegate: null,
            new EndpointMetadataCollection(
                new ControllerActionDescriptor { ControllerName = ControllerName, ActionName = ActionName }
            ),
            displayName: null
        );

    private static void AssertState(FakeLogRecord record, string operation, string? cubeId, int status, string traceId)
    {
        var state = record.StructuredState!.ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.Equal(operation, state[ErrorReporter.OperationKey]);
        Assert.Equal(cubeId, state[ErrorReporter.CubeIdKey]);
        Assert.Equal(status.ToString(), state[ErrorReporter.StatusKey]);
        Assert.Equal(traceId, state[ErrorReporter.TraceIdKey]);
    }
}
