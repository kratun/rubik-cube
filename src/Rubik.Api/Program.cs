using Rubik.Api.Errors;
using Rubik.Application.Cubes;
using Rubik.Infrastructure.Cubes;

const string HealthPath = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = context => context.HttpContext.RequestServices
            .GetRequiredService<ResultMapper>().ToBadRequest(context.HttpContext));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Singleton: a shorter lifetime would lose every session (AD-5).
builder.Services.AddSingleton<ICubeRepository, InMemoryCubeRepository>();
builder.Services.AddScoped<CreateCubeUseCase>();
builder.Services.AddScoped<GetCubeUseCase>();
builder.Services.AddSingleton<ErrorReporter>();
builder.Services.AddSingleton<ResultMapper>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// UseExceptionHandler requires an IProblemDetailsService as its fallback when no handler takes the exception.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

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
