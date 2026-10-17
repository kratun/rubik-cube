using System.Reflection;

namespace Rubik.Api.Tests.Architecture;

public class ArchitectureTests
{
    [Fact]
    public void RealAssemblies_ReferenceOnlyAllowedRubikAssemblies()
    {
        var actual = ArchitectureRules.Assemblies.ToDictionary(
            name => name,
            name => (IReadOnlyCollection<string>)Assembly.Load(name)
                .GetReferencedAssemblies()
                .Select(reference => reference.Name!)
                .Where(reference => reference.StartsWith("Rubik.", StringComparison.Ordinal))
                .ToArray());

        Assert.Empty(ArchitectureRules.Violations(actual));
    }

    [Fact]
    public void ProjectReferences_CoverEveryLayerAndReferenceOnlyAllowedProjects()
    {
        var declared = ProjectGraph.Load();

        Assert.Equal(ArchitectureRules.Assemblies.Order(), declared.Keys.Order());
        Assert.Empty(ArchitectureRules.Violations(declared));
    }

    [Fact]
    public void Violations_AllowedEdgesOnly_ReturnsNothing()
    {
        var actual = Map(
            (ArchitectureRules.Api, [ArchitectureRules.Application, ArchitectureRules.Infrastructure]),
            (ArchitectureRules.Infrastructure, [ArchitectureRules.Application]),
            (ArchitectureRules.Application, [ArchitectureRules.Domain]),
            (ArchitectureRules.Domain, []));

        Assert.Empty(ArchitectureRules.Violations(actual));
    }

    [Theory]
    [InlineData(ArchitectureRules.Domain, ArchitectureRules.Application)]
    [InlineData(ArchitectureRules.Domain, ArchitectureRules.Infrastructure)]
    [InlineData(ArchitectureRules.Domain, ArchitectureRules.Api)]
    [InlineData(ArchitectureRules.Application, ArchitectureRules.Infrastructure)]
    [InlineData(ArchitectureRules.Application, ArchitectureRules.Api)]
    [InlineData(ArchitectureRules.Infrastructure, ArchitectureRules.Api)]
    [InlineData(ArchitectureRules.Infrastructure, ArchitectureRules.Domain)]
    [InlineData(ArchitectureRules.Api, ArchitectureRules.Domain)]
    public void Violations_ForbiddenEdge_NamesTheEdge(string from, string to)
    {
        var actual = Map((from, [to]));

        Assert.Equal([$"{from} -> {to}"], ArchitectureRules.Violations(actual));
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Map(
        params (string From, string[] To)[] edges) =>
        edges.ToDictionary(edge => edge.From, edge => (IReadOnlyCollection<string>)edge.To);
}
