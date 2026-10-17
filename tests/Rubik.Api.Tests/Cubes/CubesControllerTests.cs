using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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
using Rubik.Domain.Cubes;

namespace Rubik.Api.Tests.Cubes;

public class CubesControllerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CubesPath = "/api/cubes";
    private const string StatusProperty = "status";
    private const string RevisionProperty = "revision";
    private const string MovesProperty = "moves";
    private const string ExplodedViewProperty = "explodedView";
    private const string DetailProperty = "detail";
    private const string CubeIdProperty = "cubeId";
    private const string TraceIdProperty = "traceId";
    private const string NewCubeRevision = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const int NoMoves = 0;
    private const int GridSize = 3;
    private const string MalformedId = "abc";
    private const string GetOperation = "Cubes.Get";
    private const string CreateOperation = "Cubes.Create";
    private const string ApplyMovesOperation = "Cubes.ApplyMoves";
    private const string FrontToken = "F";
    private const string RightPrimeToken = "R'";
    private const string StaleRevision = "stale";
    private const string MoveSeparator = " ";
    private const string JsonContentType = "application/json";

    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);
    private static readonly Move RightTurn = new(Face.Right, Direction.Clockwise);
    private static readonly Move UpTurn = new(Face.Up, Direction.Clockwise);
    private static readonly Move BackTurn = new(Face.Back, Direction.Clockwise);
    private static readonly Move LeftTurn = new(Face.Left, Direction.Clockwise);
    private static readonly Move DownTurn = new(Face.Down, Direction.Clockwise);

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
        var cubeId = body.RootElement.GetProperty(CubeIdProperty).GetGuid();
        Assert.Equal($"{CubesPath}/{cubeId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_Always_ReturnsEmptyListRevisionNoMovesAndTheSolvedState()
    {
        var response = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);

        var root = (await ReadBodyAsync(response)).RootElement;
        Assert.NotEqual(Guid.Empty, root.GetProperty(CubeIdProperty).GetGuid());
        Assert.Equal(NewCubeRevision, root.GetProperty(RevisionProperty).GetString());
        Assert.Equal(NoMoves, root.GetProperty(MovesProperty).GetArrayLength());
        Assert.Equal(SolvedLines, root.GetProperty(ExplodedViewProperty).EnumerateArray().Select(line => line.GetString()));

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

    [Fact]
    public async Task PostMoves_TtcSequenceOnNewCube_Returns200WithTheFixtureViewAndTheSequenceRevision()
    {
        var created = await CreateStateAsync();
        var tokens = TtcFixtureTestHelper.Tokens;

        var response = await PostMovesAsync(client, created.CubeId, created.Revision, tokens);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = (await ReadBodyAsync(response)).RootElement;
        Assert.Equal(RevisionOf(tokens), root.GetProperty(RevisionProperty).GetString());
        Assert.Equal(tokens, root.GetProperty(MovesProperty).EnumerateArray().Select(move => move.GetString()));
        Assert.Equal(TtcFixtureTestHelper.ExpectedLines, root.GetProperty(ExplodedViewProperty).EnumerateArray().Select(line => line.GetString()));
    }

    [Fact]
    public async Task PostMoves_AfterOk_TheStoredSessionHoldsTheMoves()
    {
        var created = await CreateStateAsync();
        var posted = await PostMovesAsync(client, created.CubeId, created.Revision, [FrontToken]);
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);

        var read = await client.GetAsync($"{CubesPath}/{created.CubeId}", TestContext.Current.CancellationToken);

        var root = (await ReadBodyAsync(read)).RootElement;
        Assert.Equal(RevisionOf([FrontToken]), root.GetProperty(RevisionProperty).GetString());
    }

    [Fact]
    public async Task PostMoves_StaleRevision_Returns409WithOneWarningAndLeavesTheSessionUnchanged()
    {
        var (testClient, logs) = CreateLoggingClient();
        var created = await CreateStateAsync(testClient);

        var response = await PostMovesAsync(testClient, created.CubeId, StaleRevision, [FrontToken]);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var root = (await ReadBodyAsync(response)).RootElement;
        Assert.Equal((int)HttpStatusCode.Conflict, root.GetProperty(StatusProperty).GetInt32());
        var record = SingleFailure(logs);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Null(record.Exception);
        var state = StateOf(record);
        Assert.Equal(root.GetProperty(TraceIdProperty).GetString(), state[ErrorReporter.TraceIdKey]);
        Assert.Equal(created.CubeId.ToString(), state[ErrorReporter.CubeIdKey]);
        Assert.Equal(ApplyMovesOperation, state[ErrorReporter.OperationKey]);
        await AssertRevisionAsync(testClient, created.CubeId, created.Revision);
    }

    [Fact]
    public async Task PostMoves_SameMoveCountButOtherHistory_Returns409AndLeavesTheSessionUnchanged()
    {
        var created = await CreateStateAsync();
        var repository = factory.Services.GetRequiredService<ICubeRepository>();
        var seenByClient = new[] { FrontTurn, RightTurn, UpTurn, BackTurn };
        var currentSession = new[] { FrontTurn, RightTurn, LeftTurn, DownTurn };
        await repository.TryUpdateAsync(created.Revision, new CubeSession(created.CubeId, currentSession), CancellationToken.None);
        var currentRevision = CubeRevision.Of(currentSession);

        var response = await PostMovesAsync(client, created.CubeId, CubeRevision.Of(seenByClient), [FrontToken]);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertRevisionAsync(client, created.CubeId, currentRevision);
    }

    [Fact]
    public async Task PostMoves_TwoParallelRequestsWithTheSameRevision_ExactlyOneSucceedsAndTheOtherConflicts()
    {
        var (testClient, logs) = CreateLoggingClient();
        var created = await CreateStateAsync(testClient);
        var gate = new TaskCompletionSource();

        var requests = new[] { FrontToken, RightPrimeToken }
            .Select(token => Task.Run(async () =>
            {
                await gate.Task;
                return await PostMovesAsync(testClient, created.CubeId, created.Revision, [token]);
            }))
            .ToList();
        gate.SetResult();
        var responses = await Task.WhenAll(requests);

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(response => response.StatusCode).Order());
        var winner = responses.Single(response => response.StatusCode == HttpStatusCode.OK);
        var winnerRoot = (await ReadBodyAsync(winner)).RootElement;
        await AssertRevisionAsync(testClient, created.CubeId, winnerRoot.GetProperty(RevisionProperty).GetString()!);
        Assert.Equal(LogLevel.Warning, SingleFailure(logs).Level);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("f")]
    [InlineData("F2")]
    public async Task PostMoves_InvalidToken_Returns400NamingItWithOneWarningAndLeavesTheSessionUnchanged(string token)
    {
        var (testClient, logs) = CreateLoggingClient();
        var created = await CreateStateAsync(testClient);

        var response = await PostMovesAsync(testClient, created.CubeId, created.Revision, [FrontToken, token]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var detail = (await ReadBodyAsync(response)).RootElement.GetProperty(DetailProperty).GetString();
        Assert.Contains($"\"{token}\"", detail);
        Assert.Equal(LogLevel.Warning, SingleFailure(logs).Level);
        await AssertRevisionAsync(testClient, created.CubeId, created.Revision);
    }

    [Theory]
    [InlineData("{\"expectedRevision\":\"r\",\"moves\":[]}")]
    [InlineData("{\"expectedRevision\":\"r\"}")]
    [InlineData("{\"expectedRevision\":\"r\",\"moves\":null}")]
    [InlineData("{\"moves\":[\"F\"]}")]
    [InlineData("{\"expectedRevision\":\"\",\"moves\":[\"F\"]}")]
    [InlineData("{\"expectedRevision\":null,\"moves\":[\"F\"]}")]
    [InlineData("{}")]
    [InlineData("{\"expectedRevision\":\"r\",\"moves\":[\"F\",null]}")]
    public async Task PostMoves_MissingOrEmptyInput_Returns400WithOneWarningAndLeavesTheSessionUnchanged(string body)
    {
        var (testClient, logs) = CreateLoggingClient();
        var created = await CreateStateAsync(testClient);

        var response = await testClient.PostAsync(
            $"{CubesPath}/{created.CubeId}/moves",
            new StringContent(body, Encoding.UTF8, JsonContentType),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadRequest, (await ReadBodyAsync(response)).RootElement.GetProperty(StatusProperty).GetInt32());
        Assert.Equal(LogLevel.Warning, SingleFailure(logs).Level);
        await AssertRevisionAsync(testClient, created.CubeId, created.Revision);
    }

    [Fact]
    public async Task PostMoves_MalformedId_Returns400WithOneWarning()
    {
        var (testClient, logs) = CreateLoggingClient();

        var response = await testClient.PostAsJsonAsync(
            $"{CubesPath}/{MalformedId}/moves",
            new { expectedRevision = StaleRevision, moves = new[] { FrontToken } },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(LogLevel.Warning, SingleFailure(logs).Level);
    }

    [Fact]
    public async Task PostMoves_UnknownCube_Returns404()
    {
        var response = await PostMovesAsync(client, Guid.NewGuid(), StaleRevision, [FrontToken]);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMoves_RepositoryThrows_Returns500WithoutStackTraceAndOneErrorWithTheException()
    {
        var (testClient, logs) = CreateLoggingClient(services => services.AddSingleton<ICubeRepository, ThrowingCubeRepositoryTestHelper>());

        var cubeId = Guid.NewGuid();
        var response = await PostMovesAsync(testClient, cubeId, StaleRevision, [FrontToken]);

        await AssertInternalErrorAsync(response, logs, ApplyMovesOperation, cubeId.ToString());
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

    private static async Task<HttpResponseMessage> PostMovesAsync(
        HttpClient testClient, Guid cubeId, string expectedRevision, IReadOnlyList<string> moves) =>
        await testClient.PostAsJsonAsync(
            $"{CubesPath}/{cubeId}/moves",
            new { expectedRevision, moves },
            TestContext.Current.CancellationToken);

    private static string RevisionOf(IEnumerable<string> tokens) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(MoveSeparator, tokens))));

    private static async Task AssertRevisionAsync(HttpClient testClient, Guid cubeId, string expectedRevision)
    {
        var read = await testClient.GetAsync($"{CubesPath}/{cubeId}", TestContext.Current.CancellationToken);
        var root = (await ReadBodyAsync(read)).RootElement;
        Assert.Equal(expectedRevision, root.GetProperty(RevisionProperty).GetString());
    }

    private async Task<(Guid CubeId, string Revision)> CreateStateAsync(HttpClient? testClient = null)
    {
        var response = await (testClient ?? client).PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);
        var root = (await ReadBodyAsync(response)).RootElement;
        return (root.GetProperty(CubeIdProperty).GetGuid(), root.GetProperty(RevisionProperty).GetString()!);
    }

    private async Task<Guid> CreateAsync()
    {
        var response = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);
        return (await ReadBodyAsync(response)).RootElement.GetProperty(CubeIdProperty).GetGuid();
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
