using Rubik.Api.Errors;

namespace Rubik.Api.Extensions;

/// <summary>Pipeline setup of the Api, grouped so that <c>Program.cs</c> stays a short composition root.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Exception handler plus <see cref="ErrorDetails"/> bodies for empty 4xx and 5xx responses.</summary>
    public static WebApplication UseCubeErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(app.Services.GetRequiredService<StatusCodeErrorWriter>().WriteAsync);
        return app;
    }

    /// <summary>Swagger and its UI, in Development only.</summary>
    public static WebApplication UseDevelopmentTools(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
