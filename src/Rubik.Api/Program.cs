using Rubik.Api.Extensions;

const string HealthPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCubeApi();
builder.Services.AddCubeOpenApi();
builder.Services.AddCubeServices();

var app = builder.Build();

app.UseCubeErrorHandling();
app.UseDevelopmentTools();
app.MapControllers();
app.MapHealthChecks(HealthPath);

app.Run();

/// <summary>Makes the entry point visible to the integration tests.</summary>
public partial class Program;
