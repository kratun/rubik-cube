using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Rubik.Application.Cubes;

namespace Rubik.Api.Tests.Cubes;

public class CubesControllerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CubesPath = "/api/cubes";
    private const string NewCubeRevision = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const int NoMoves = 0;
    private const int GridSize = 3;

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

    private async Task<Guid> CreateAsync()
    {
        var response = await client.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);
        return (await ReadBodyAsync(response)).RootElement.GetProperty("cubeId").GetGuid();
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
