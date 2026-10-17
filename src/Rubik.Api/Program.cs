using Rubik.Application.Cubes;
using Rubik.Infrastructure.Cubes;

const string HealthPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Singleton: a shorter lifetime would lose every session (AD-5).
builder.Services.AddSingleton<ICubeRepository, InMemoryCubeRepository>();
builder.Services.AddScoped<CreateCubeUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks(HealthPath);

app.Run();

/// <summary>Makes the entry point visible to the integration tests.</summary>
public partial class Program;
