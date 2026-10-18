using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rubik.Api.OpenApi;

/// <summary>
/// Marks every non-nullable property as required (AD-7). Swashbuckle leaves <c>required</c> out for positional records,
/// so generated TypeScript types would make every field optional.
/// </summary>
public sealed class RequiredPropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is null)
        {
            return;
        }

        var nonNullable = concrete.Properties
            .Where(property => !(property.Value.Type?.HasFlag(JsonSchemaType.Null) ?? false))
            .Select(property => property.Key)
            .ToList();

        // An empty required array is invalid in OpenAPI 3.0, so the set is only created when there is a name to add.
        if (nonNullable.Count == 0)
        {
            return;
        }

        concrete.Required ??= new HashSet<string>();
        foreach (var name in nonNullable)
        {
            concrete.Required.Add(name);
        }
    }
}
