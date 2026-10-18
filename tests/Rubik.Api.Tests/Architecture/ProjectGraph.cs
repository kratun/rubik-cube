using System.Xml.Linq;

namespace Rubik.Api.Tests.Architecture;

/// <summary>
/// Reads the declared ProjectReference and PackageReference items of every src project, so an unused one is still seen.
/// </summary>
internal static class ProjectGraph
{
    private const string ProjectReferenceElement = "ProjectReference";
    private const string PackageReferenceElement = "PackageReference";
    private const string IncludeAttribute = "Include";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Load() =>
        LoadItems(
            ProjectReferenceElement,
            include => Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar))
        );

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> LoadPackages() =>
        LoadItems(PackageReferenceElement, include => include);

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> LoadItems(
        string elementName,
        Func<string, string> nameOf
    )
    {
        var srcDirectory = Path.Combine(FindRepositoryRoot(), "src");

        return Directory
            .GetFiles(srcDirectory, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(
                project => Path.GetFileNameWithoutExtension(project),
                project =>
                    (IReadOnlyCollection<string>)
                        XDocument
                            .Load(project)
                            .Descendants(elementName)
                            .Select(item => nameOf((string)item.Attribute(IncludeAttribute)!))
                            .ToArray()
            );
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Rubik.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Rubik.slnx not found above " + AppContext.BaseDirectory);
    }
}
