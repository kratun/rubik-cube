using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.OpenApi;
using Rubik.Api.OpenApi;
using Rubik.Api.Tests.TestHelpers;

namespace Rubik.Api.Tests.OpenApi;

public class RequiredPropertiesSchemaFilterTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SchemasProperty = "components";
    private const string SchemasSection = "schemas";
    private const string RequiredProperty = "required";
    private const string CubeStateSchema = "CubeState";
    private const string ErrorDetailsSchema = "ErrorDetails";
    private const string ExpectedRevisionRequestSchema = "ExpectedRevisionRequest";
    private const string ApplyMovesRequestSchema = "ApplyMovesRequest";
    private const string MandatoryName = "mandatory";
    private const string OptionalName = "optional";

    public static TheoryData<string, string[]> RequiredByDto => new()
    {
        { CubeStateSchema, ["cubeId", "revision", "moves", "faces", "explodedView"] },
        { ErrorDetailsSchema, ["type", "title", "status", "detail", "traceId"] },
        { ExpectedRevisionRequestSchema, ["expectedRevision"] },
        { ApplyMovesRequestSchema, ["expectedRevision", "moves"] },
    };

    [Theory]
    [MemberData(nameof(RequiredByDto))]
    public async Task Apply_NonNullableProperties_AreListedAsRequired(string schemaName, string[] expected)
    {
        using var document = await OpenApiDocumentTestHelper.FetchAsync(factory);

        var schema = document.RootElement.GetProperty(SchemasProperty).GetProperty(SchemasSection)
            .GetProperty(schemaName);
        var required = schema.GetProperty(RequiredProperty).EnumerateArray().Select(item => item.GetString()).ToList();

        Assert.Equal(expected.Order(), required.Order());
    }

    [Fact]
    public void Apply_NullableProperty_IsNotListedAsRequired()
    {
        var schema = SchemaWith(
            (MandatoryName, JsonSchemaType.String),
            (OptionalName, JsonSchemaType.String | JsonSchemaType.Null));

        new RequiredPropertiesSchemaFilter().Apply(schema, null!);

        Assert.Equal([MandatoryName], schema.Required);
    }

    [Fact]
    public void Apply_OnlyNullableProperties_LeavesRequiredUnset()
    {
        var schema = SchemaWith((OptionalName, JsonSchemaType.String | JsonSchemaType.Null));

        new RequiredPropertiesSchemaFilter().Apply(schema, null!);

        Assert.True(schema.Required is null or { Count: 0 });
    }

    private static OpenApiSchema SchemaWith(params (string Name, JsonSchemaType Type)[] properties) =>
        new()
        {
            Type = JsonSchemaType.Object,
            Properties = properties.ToDictionary(
                property => property.Name,
                property => (IOpenApiSchema)new OpenApiSchema { Type = property.Type }),
        };
}
