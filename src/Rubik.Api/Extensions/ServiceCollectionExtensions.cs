using Rubik.Api.Errors;
using Rubik.Api.OpenApi;
using Rubik.Application.Cubes;
using Rubik.Infrastructure.Cubes;

namespace Rubik.Api.Extensions;

/// <summary>
/// Service registrations of the Api, grouped so that <c>Program.cs</c> stays a short composition root.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>MVC controllers, error reporting and mapping (AD-6, AD-12) and the health check.</summary>
    public static IServiceCollection AddCubeApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // The status code pages write the ErrorDetails body for empty 4xx, so MVC must not add its own.
                options.SuppressMapClientErrors = true;
                options.InvalidModelStateResponseFactory = context =>
                    context
                        .HttpContext.RequestServices.GetRequiredService<ResultMapper>()
                        .ToBadRequest(context.HttpContext);
            });
        services.AddHealthChecks();
        services.AddSingleton<ErrorReporter>();
        services.AddSingleton<ResultMapper>();
        services.AddSingleton<StatusCodeErrorWriter>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // UseExceptionHandler requires an IProblemDetailsService as its fallback when no handler takes the exception.
        services.AddProblemDetails();
        return services;
    }

    /// <summary>
    /// The Swagger generator with the filters that make the OpenAPI document match the real contract.
    /// </summary>
    public static IServiceCollection AddCubeOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
            options.SchemaFilter<RequiredPropertiesSchemaFilter>();
            options.OperationFilter<RequiredRequestBodyOperationFilter>();
        });
        return services;
    }

    /// <summary>The repository and the use cases of Rubik.Application.</summary>
    public static IServiceCollection AddCubeServices(this IServiceCollection services)
    {
        // Singleton: a shorter lifetime would lose every session (AD-5).
        services.AddSingleton<ICubeRepository, InMemoryCubeRepository>();
        services.AddScoped<CreateCubeUseCase>();
        services.AddScoped<GetCubeUseCase>();
        services.AddScoped<RevisionCheckedUpdate>();
        services.AddScoped<ApplyMovesUseCase>();
        services.AddScoped<UndoUseCase>();
        services.AddScoped<ResetUseCase>();
        return services;
    }
}
