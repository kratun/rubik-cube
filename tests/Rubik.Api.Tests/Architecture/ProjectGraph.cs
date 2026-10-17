using System.Xml.Linq;

namespace Rubik.Api.Tests.Architecture;

/// <summary>Reads the declared ProjectReference edges of every src project, so an unused reference is still seen.</summary>
internal static class ProjectGraph
{
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Load()
    {
        var srcDirectory = Path.Combine(FindRepositoryRoot(), "src");

        return Directory.GetFiles(srcDirectory, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(
                project => Path.GetFileNameWithoutExtension(project),
                project => (IReadOnlyCollection<string>)XDocument.Load(project)
                    .Descendants("ProjectReference")
                    .Select(reference => Path.GetFileNameWithoutExtension(
                        ((string)reference.Attribute("Include")!).Replace('\\', Path.DirectorySeparatorChar)))
                    .ToArray());
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
