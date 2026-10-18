using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rubik.Api.OpenApi;

/// <summary>Marks every request body as required, so generated clients cannot omit it (AD-7).</summary>
public sealed class RequiredRequestBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody is OpenApiRequestBody body)
        {
            body.Required = true;
        }
    }
}
