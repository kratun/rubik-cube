using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Rubik.Api.Errors;

namespace Rubik.Api.Tests.Errors;

public class StatusCodeErrorWriterTests
{
    private const string TraceId = "trace-1";
    private const string TraceIdProperty = "traceId";
    private const string StatusProperty = "status";
    private const string TitleProperty = "title";
    private const string MissingTitle = "Not Found";
    private const int EmptyBodyLength = 0;

    private readonly FakeLogger<ErrorReporter> logger = new();
    private readonly MemoryStream body = new();
    private readonly DefaultHttpContext context;
    private readonly StatusCodeErrorWriter writer;

    public StatusCodeErrorWriterTests()
    {
        context = new DefaultHttpContext
        {
            TraceIdentifier = TraceId,
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Response.Body = body;
        writer = new StatusCodeErrorWriter(new ErrorReporter(logger));
    }

    [Fact]
    public async Task WriteAsync_EmptyNotFound_WritesErrorDetailsWithTheStatusAndLogsWarning()
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await writer.WriteAsync(new StatusCodeContext(context, new StatusCodePagesOptions(), next: null!));

        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(body.ToArray()));
        Assert.Equal(StatusCodes.Status404NotFound, json.RootElement.GetProperty(StatusProperty).GetInt32());
        Assert.Equal(MissingTitle, json.RootElement.GetProperty(TitleProperty).GetString());
        Assert.Equal(TraceId, json.RootElement.GetProperty(TraceIdProperty).GetString());
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Null(record.Exception);
    }

    [Fact]
    public async Task WriteAsync_RequestAborted_WritesNoBodyAndLogsNothing()
    {
        using var aborted = new CancellationTokenSource();
        context.RequestAborted = aborted.Token;
        context.Response.StatusCode = GlobalExceptionHandler.ClientClosedRequestStatus;
        await aborted.CancelAsync();

        await writer.WriteAsync(new StatusCodeContext(context, new StatusCodePagesOptions(), next: null!));

        Assert.Empty(logger.Collector.GetSnapshot());
        Assert.Equal(EmptyBodyLength, body.Length);
    }
}
