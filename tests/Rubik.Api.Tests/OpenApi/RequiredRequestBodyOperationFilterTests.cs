using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Rubik.Api.Tests.TestHelpers;

namespace Rubik.Api.Tests.OpenApi;

public class RequiredRequestBodyOperationFilterTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string PathsProperty = "paths";
    private const string PostOperation = "post";
    private const string RequestBodyProperty = "requestBody";
    private const string RequiredProperty = "required";

    public static TheoryData<string> BodyPaths => new()
    {
        "/api/cubes/{id}/moves",
        "/api/cubes/{id}/undo",
        "/api/cubes/{id}/reset",
    };

    [Theory]
    [MemberData(nameof(BodyPaths))]
    public async Task Apply_OperationWithBody_MarksTheBodyRequired(string path)
    {
        using var document = await OpenApiDocumentTestHelper.FetchAsync(factory);

        var body = document.RootElement.GetProperty(PathsProperty).GetProperty(path).GetProperty(PostOperation)
            .GetProperty(RequestBodyProperty);

        Assert.Equal(JsonValueKind.True, body.GetProperty(RequiredProperty).ValueKind);
    }
}
