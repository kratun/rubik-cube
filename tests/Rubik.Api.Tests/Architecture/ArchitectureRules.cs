namespace Rubik.Api.Tests.Architecture;

/// <summary>The allowed project-reference edges between the Rubik assemblies (AD-1).</summary>
internal static class ArchitectureRules
{
    public const string Domain = "Rubik.Domain";
    public const string Application = "Rubik.Application";
    public const string Infrastructure = "Rubik.Infrastructure";
    public const string Api = "Rubik.Api";

    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>
    {
        [Api] = [Application, Infrastructure],
        [Infrastructure] = [Application],
        [Application] = [Domain],
        [Domain] = [],
    };

    /// <summary>The assemblies that must declare no package at all (the Domain is pure, AD-1).</summary>
    private static readonly string[] PackageFree = [Domain];

    public static IReadOnlyCollection<string> Assemblies => Allowed.Keys.ToArray();

    /// <summary>
    /// Returns every "Project -> Package" pair in <paramref name="actual"/> declared by a package-free project.
    /// </summary>
    public static IReadOnlyList<string> PackageViolations(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> actual
    ) =>
        actual
            .Where(entry => PackageFree.Contains(entry.Key))
            .SelectMany(entry => entry.Value.Select(package => $"{entry.Key} -> {package}"))
            .ToList();

    /// <summary>Returns every "From -> To" edge in <paramref name="actual"/> that AD-1 does not allow.</summary>
    public static IReadOnlyList<string> Violations(IReadOnlyDictionary<string, IReadOnlyCollection<string>> actual) =>
        actual
            .SelectMany(entry => entry.Value.Select(target => (From: entry.Key, To: target)))
            .Where(edge => !Allowed.TryGetValue(edge.From, out var targets) || !targets.Contains(edge.To))
            .Select(edge => $"{edge.From} -> {edge.To}")
            .ToList();
}
