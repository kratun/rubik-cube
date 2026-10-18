using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Rubik.Api.Tests.TestHelpers;

/// <summary>Fetches the OpenAPI document of the Development API as parsed JSON.</summary>
public static class OpenApiDocumentTestHelper
{
    private const string OpenApiPath = "/swagger/v1/swagger.json";

    public static async Task<JsonDocument> FetchAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.WithWebHostBuilder(host => host.UseEnvironment(Environments.Development)).CreateClient();
        var json = await client.GetStringAsync(OpenApiPath, TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json);
    }
}
