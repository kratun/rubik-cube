using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Rubik.Api.Errors;

namespace Rubik.Api.Tests.TestHelpers;

/// <summary>Creates a client that records the logs, and reads back the one failure entry (AD-12).</summary>
internal static class FailureLogTestHelper
{
    public static (HttpClient Client, FakeLogCollector Logs) CreateLoggingClient(
        this WebApplicationFactory<Program> factory,
        Action<IServiceCollection>? configureServices = null
    )
    {
        var loggingFactory = factory.WithWebHostBuilder(host =>
        {
            host.ConfigureLogging(logging => logging.AddFakeLogging());
            host.ConfigureTestServices(services => configureServices?.Invoke(services));
        });
        return (loggingFactory.CreateClient(), loggingFactory.Services.GetFakeLogCollector());
    }

    // AD-12: a failure is logged in one place. Any Warning or worse from another category (the framework's own
    // middleware, for example) would be a second entry, so the single entry must come from the reporter.
    public static FakeLogRecord SingleFailure(FakeLogCollector logs)
    {
        var record = Assert.Single(logs.GetSnapshot(), entry => entry.Level >= LogLevel.Warning);
        Assert.Equal(typeof(ErrorReporter).FullName, record.Category);
        return record;
    }

    public static Dictionary<string, string?> StateOf(FakeLogRecord record) =>
        record.StructuredState!.ToDictionary(pair => pair.Key, pair => pair.Value);
}
