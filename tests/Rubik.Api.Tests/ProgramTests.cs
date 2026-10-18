using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rubik.Api.Errors;
using Rubik.Application.Cubes;
using Rubik.Infrastructure.Cubes;
using static Rubik.Api.Tests.TestHelpers.CubesApiTestHelper;
using static Rubik.Api.Tests.TestHelpers.FailureLogTestHelper;

namespace Rubik.Api.Tests;

public class ProgramTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string HealthPath = "/health";
    private const string OpenApiPath = "/swagger/v1/swagger.json";
    private const string MovesPath = "/api/cubes/00000000-0000-0000-0000-000000000001/moves";
    private const string UnknownPath = "/api/nope";
    private const string GetMethod = "GET";
    private const string PutMethod = "PUT";
    private const string PostMethod = "POST";
    private const string JsonContentType = "application/json";
    private const string TextContentType = "text/plain";
    private const string EmptyJsonObject = "{}";
    private const string StatusProperty = "status";
    private const string TraceIdProperty = "traceId";

    [Fact]
    public async Task GetHealth_Always_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync(HealthPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApiDocument_InDevelopment_ReturnsOkAndDescribesTheCubesEndpoint()
    {
        var response = await factory
            .WithWebHostBuilder(host => host.UseEnvironment(Environments.Development))
            .CreateClient()
            .GetAsync(OpenApiPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(CubesPath, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetOpenApiDocument_InProduction_ReturnsNotFound()
    {
        var response = await factory
            .WithWebHostBuilder(host => host.UseEnvironment(Environments.Production))
            .CreateClient()
            .GetAsync(OpenApiPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Repository_Resolved_IsTheInMemoryAdapterAsOneSharedInstance()
    {
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<ICubeRepository>();
        var second = secondScope.ServiceProvider.GetRequiredService<ICubeRepository>();

        Assert.IsType<InMemoryCubeRepository>(first);
        Assert.Same(first, second);
    }

    [Theory]
    [InlineData(StatusCodes.Status404NotFound, UnknownPath, GetMethod, JsonContentType)]
    [InlineData(StatusCodes.Status405MethodNotAllowed, CubesPath, PutMethod, JsonContentType)]
    [InlineData(StatusCodes.Status415UnsupportedMediaType, MovesPath, PostMethod, TextContentType)]
    public async Task Request_MatchingNoEndpoint_ReturnsErrorDetailsWithTheTraceIdOfTheSingleWarning(
        int status,
        string path,
        string method,
        string contentType
    )
    {
        var (client, logs) = factory.CreateLoggingClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(EmptyJsonObject, Encoding.UTF8, contentType),
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(status, (int)response.StatusCode);
        var body = (await ReadBodyAsync(response)).RootElement;
        Assert.Equal(status, body.GetProperty(StatusProperty).GetInt32());
        var record = SingleFailure(logs);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal(body.GetProperty(TraceIdProperty).GetString(), StateOf(record)[ErrorReporter.TraceIdKey]);
    }
}
