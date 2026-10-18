using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Rubik.Api.Tests.TestHelpers;

/// <summary>Requests and response readers shared by the tests of the cubes endpoints.</summary>
internal static class CubesApiTestHelper
{
    public const string CubesPath = "/api/cubes";
    public const string CubeIdProperty = "cubeId";
    public const string RevisionProperty = "revision";

    private const string MoveSeparator = " ";

    public static async Task<HttpResponseMessage> PostMovesAsync(
        HttpClient testClient,
        Guid cubeId,
        string expectedRevision,
        IReadOnlyList<string> moves
    ) =>
        await testClient.PostAsJsonAsync(
            $"{CubesPath}/{cubeId}/moves",
            new { expectedRevision, moves },
            TestContext.Current.CancellationToken
        );

    public static async Task<HttpResponseMessage> PostExpectedRevisionAsync(
        HttpClient testClient,
        Guid cubeId,
        string action,
        string expectedRevision
    ) =>
        await testClient.PostAsJsonAsync(
            $"{CubesPath}/{cubeId}/{action}",
            new { expectedRevision },
            TestContext.Current.CancellationToken
        );

    public static async Task<(Guid CubeId, string Revision)> ReadStateAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = (await ReadBodyAsync(response)).RootElement;
        return (root.GetProperty(CubeIdProperty).GetGuid(), root.GetProperty(RevisionProperty).GetString()!);
    }

    public static string RevisionOf(IEnumerable<string> tokens) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(MoveSeparator, tokens))));

    public static async Task AssertRevisionAsync(HttpClient testClient, Guid cubeId, string expectedRevision)
    {
        var read = await testClient.GetAsync($"{CubesPath}/{cubeId}", TestContext.Current.CancellationToken);
        var root = (await ReadBodyAsync(read)).RootElement;
        Assert.Equal(expectedRevision, root.GetProperty(RevisionProperty).GetString());
    }

    public static async Task<(Guid CubeId, string Revision)> CreateStateAsync(HttpClient testClient)
    {
        var response = await testClient.PostAsync(CubesPath, content: null, TestContext.Current.CancellationToken);
        var root = (await ReadBodyAsync(response)).RootElement;
        return (root.GetProperty(CubeIdProperty).GetGuid(), root.GetProperty(RevisionProperty).GetString()!);
    }

    public static async Task<Guid> CreateAsync(HttpClient testClient) => (await CreateStateAsync(testClient)).CubeId;

    public static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
