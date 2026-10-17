using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rubik.Application.Cubes;
using Rubik.Infrastructure.Cubes;

namespace Rubik.Api.Tests;

public class ProgramTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string HealthPath = "/health";
    private const string OpenApiPath = "/swagger/v1/swagger.json";
    private const string CubesPath = "/api/cubes";

    [Fact]
    public async Task GetHealth_Always_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync(HealthPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApiDocument_InDevelopment_ReturnsOkAndDescribesTheCubesEndpoint()
    {
        var response = await factory.WithWebHostBuilder(host => host.UseEnvironment(Environments.Development))
            .CreateClient().GetAsync(OpenApiPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(CubesPath, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetOpenApiDocument_InProduction_ReturnsNotFound()
    {
        var response = await factory.WithWebHostBuilder(host => host.UseEnvironment(Environments.Production))
            .CreateClient().GetAsync(OpenApiPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Repository_Resolved_IsTheInMemoryAdapterAsOneSharedInstance()
    {
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<ICubeRepository>();
        var second = secondScope.ServiceProvider.GetRequiredService<ICubeRepository>();

        Assert.IsType<InMemoryCubeRepository>(first);
        Assert.Same(first, second);
    }
}
