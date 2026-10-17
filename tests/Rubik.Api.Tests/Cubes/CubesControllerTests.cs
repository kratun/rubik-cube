using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Rubik.Api.Errors;
using Rubik.Api.Tests.TestHelpers;
using Rubik.Application.Cubes;

namespace Rubik.Api.Tests.Cubes;

public class CubesControllerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CubesPath = "/api/cubes";
    private const string StatusProperty = "status";
    private const string TraceIdProperty = "traceId";
    private const string NewCubeRevision = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const int NoMoves = 0;
    private const int GridSize = 3;
    private const string MalformedId = "abc";
    private const string GetOperation = "Cubes.Get";
    private const string CreateOperation = "Cubes.Create";

    private static readonly IReadOnlyList<string> SolvedLines =
    [
        "      W W W",
        "      W W W",
        "      W W W",
        "O O O G G G R R R B B B",
        "O O O G G G R R R B B B",
        "O O O G G G R R R B B B",
        "      Y Y Y",
        "      Y Y Y",
        "      Y Y Y",
    ];

    private static readonly IReadOnlyDictionary<string, string> SolvedColours = new Dictionary<string, string>
    {
        ["up"] = "White",
        ["down"] = "Yellow",
        ["left"] = "Orange",
        ["right"] = "Red",
        ["front"] = "Green",
        ["back"] = "Blue",
    };

    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Post_Always_ReturnsCreatedWithLocationOfTheNewCube()
    {
        var response = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadBodyAsync(response);
        var cubeId = body.RootElement.GetProperty("cubeId").GetGuid();
        Assert.Equal($"{CubesPath}/{cubeId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_Always_ReturnsEmptyListRevisionNoMovesAndTheSolvedState()
    {
        var response = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);

        var root = (await ReadBodyAsync(response)).RootElement;
        Assert.NotEqual(Guid.Empty, root.GetProperty("cubeId").GetGuid());
        Assert.Equal(NewCubeRevision, root.GetProperty("revision").GetString());
        Assert.Equal(NoMoves, root.GetProperty("moves").GetArrayLength());
        Assert.Equal(SolvedLines, root.GetProperty("explodedView").EnumerateArray().Select(line => line.GetString()));

        var faces = root.GetProperty("faces");
        Assert.Equal(SolvedColours.Count, faces.EnumerateObject().Count());
        foreach (var (face, colour) in SolvedColours)
        {
            var grid = faces.GetProperty(face).EnumerateArray().ToList();
            Assert.Equal(GridSize, grid.Count);
            Assert.All(grid, row => Assert.Equal(Enumerable.Repeat(colour, GridSize), row.EnumerateArray().Select(tile => tile.GetString())));
        }
    }

    [Fact]
    public async Task Post_CalledTwice_ReturnsDifferentIdsAndStoresIndependentSessions()
    {
        var first = await CreateAsync();
        var second = await CreateAsync();

        Assert.NotEqual(first, second);
        var repository = factory.Services.GetRequiredService<ICubeRepository>();
        var firstSession = await repository.GetAsync(first, CancellationToken.None);
        var secondSession = await repository.GetAsync(second, CancellationToken.None);
        Assert.Equal(first, firstSession?.CubeId);
        Assert.Equal(second, secondSession?.CubeId);
    }

    [Fact]
    public async Task Get_AfterPost_ReturnsOkWithTheSameBodyAndTheLocationResolves()
    {
        var created = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);
        var createdBody = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var response = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(createdBody, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_UnknownId_Returns404WithErrorDetailsAndOneWarningWithTheSameTraceId()
    {
        var (testClient, logs) = CreateLoggingClient();
        var unknownId = Guid.NewGuid();

        var response = await testClient.GetAsync($"{CubesPath}/{unknownId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var root = (await ReadBodyAsync(response)).RootElement;
        Assert.Equal((int)HttpStatusCode.NotFound, root.GetProperty(StatusProperty).GetInt32());
        var record = SingleFailure(logs);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Null(record.Exception);
        var state = StateOf(record);
        Assert.Equal(root.GetProperty(TraceIdProperty).GetString(), state[ErrorReporter.TraceIdKey]);
        Assert.Equal(unknownId.ToString(), state[ErrorReporter.CubeIdKey]);
        Assert.Equal(GetOperation, state[ErrorReporter.OperationKey]);
        Assert.Equal(((int)HttpStatusCode.NotFound).ToString(), state[ErrorReporter.StatusKey]);
    }

    [Fact]
    public async Task Get_MalformedId_Returns400WithErrorDetailsAndNoCubeIdInTheLog()
    {
        var (testClient, logs) = CreateLoggingClient();

        var response = await testClient.GetAsync($"{CubesPath}/{MalformedId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var root = (await ReadBodyAsync(response)).RootElement;
        Assert.Equal((int)HttpStatusCode.BadRequest, root.GetProperty(StatusProperty).GetInt32());
        var record = SingleFailure(logs);
        Assert.Equal(LogLevel.Warning, record.Level);
        var state = StateOf(record);
        Assert.Equal(root.GetProperty(TraceIdProperty).GetString(), state[ErrorReporter.TraceIdKey]);
        Assert.Null(state[ErrorReporter.CubeIdKey]);
    }

    [Fact]
    public async Task Get_RepositoryThrows_Returns500WithoutStackTraceAndOneErrorWithTheException()
    {
        var (testClient, logs) = CreateLoggingClient(services => services.AddSingleton<ICubeRepository, ThrowingCubeRepositoryTestHelper>());

        var cubeId = Guid.NewGuid();
        var response = await testClient.GetAsync($"{CubesPath}/{cubeId}", TestContext.Current.CancellationToken);

        await AssertInternalErrorAsync(response, logs, GetOperation, cubeId.ToString());
    }

    [Fact]
    public async Task Post_RepositoryThrows_Returns500WithoutStackTraceAndOneErrorWithTheException()
    {
        var (testClient, logs) = CreateLoggingClient(services => services.AddSingleton<ICubeRepository, ThrowingCubeRepositoryTestHelper>());

        var response = await testClient.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);

        await AssertInternalErrorAsync(response, logs, CreateOperation, cubeId: null);
    }

    private static async Task AssertInternalErrorAsync(HttpResponseMessage response, FakeLogCollector logs, string operation, string? cubeId)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(ThrowingCubeRepositoryTestHelper.FailureMessage, text);
        Assert.DoesNotContain(nameof(InvalidOperationException), text);
        var root = JsonDocument.Parse(text).RootElement;
        Assert.Equal((int)HttpStatusCode.InternalServerError, root.GetProperty(StatusProperty).GetInt32());
        var record = SingleFailure(logs);
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.IsType<InvalidOperationException>(record.Exception);
        var state = StateOf(record);
        Assert.Equal(root.GetProperty(TraceIdProperty).GetString(), state[ErrorReporter.TraceIdKey]);
        Assert.Equal(operation, state[ErrorReporter.OperationKey]);
        Assert.Equal(cubeId, state[ErrorReporter.CubeIdKey]);
        Assert.Equal(((int)HttpStatusCode.InternalServerError).ToString(), state[ErrorReporter.StatusKey]);
    }

    private (HttpClient Client, FakeLogCollector Logs) CreateLoggingClient(Action<IServiceCollection>? configureServices = null)
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
    private static FakeLogRecord SingleFailure(FakeLogCollector logs)
    {
        var record = Assert.Single(logs.GetSnapshot(), entry => entry.Level >= LogLevel.Warning);
        Assert.Equal(typeof(ErrorReporter).FullName, record.Category);
        return record;
    }

    private static Dictionary<string, string?> StateOf(FakeLogRecord record) =>
        record.StructuredState!.ToDictionary(pair => pair.Key, pair => pair.Value);

    private async Task<Guid> CreateAsync()
    {
        var response = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);
        return (await ReadBodyAsync(response)).RootElement.GetProperty("cubeId").GetGuid();
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
