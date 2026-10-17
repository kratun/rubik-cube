using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Rubik.Api.Errors;

namespace Rubik.Api.Tests.Errors;

public class GlobalExceptionHandlerTests
{
    private const string TraceId = "trace-1";
    private const string ExceptionMessage = "secret internal message";
    private const string TraceIdProperty = "traceId";
    private const int EmptyBodyLength = 0;

    private readonly FakeLogger<ErrorReporter> logger = new();
    private readonly MemoryStream body = new();
    private readonly DefaultHttpContext context;
    private readonly GlobalExceptionHandler handler;

    public GlobalExceptionHandlerTests()
    {
        context = new DefaultHttpContext
        {
            TraceIdentifier = TraceId,
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Response.Body = body;
        handler = new GlobalExceptionHandler(new ErrorReporter(logger));
    }

    [Fact]
    public async Task TryHandleAsync_Exception_Writes500WithoutTheExceptionMessageAndLogsError()
    {
        var exception = new InvalidOperationException(ExceptionMessage);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var text = Encoding.UTF8.GetString(body.ToArray());
        Assert.DoesNotContain(ExceptionMessage, text);
        using var json = JsonDocument.Parse(text);
        Assert.Equal(TraceId, json.RootElement.GetProperty(TraceIdProperty).GetString());
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(exception, record.Exception);
    }

    [Fact]
    public async Task TryHandleAsync_RequestAborted_ReportsNothingAndWritesNoBody()
    {
        using var aborted = new CancellationTokenSource();
        context.RequestAborted = aborted.Token;
        await aborted.CancelAsync();

        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(GlobalExceptionHandler.ClientClosedRequestStatus, context.Response.StatusCode);
        Assert.Empty(logger.Collector.GetSnapshot());
        Assert.Equal(EmptyBodyLength, body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_CancellationWhileTheRequestIsStillOpen_StillReports500()
    {
        var cancellation = new OperationCanceledException();

        var handled = await handler.TryHandleAsync(context, cancellation, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(cancellation, record.Exception);
    }
}
