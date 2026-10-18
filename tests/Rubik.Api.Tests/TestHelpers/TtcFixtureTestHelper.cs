namespace Rubik.Api.Tests.TestHelpers;

/// <summary>
/// Test helper: the TTC Test Sequence and its expected exploded view, read from the shared fixture files in
/// <c>tests/Fixtures</c>. Same files and folder contract as the helper in Rubik.Domain.Tests; this one only reads them,
/// because the API tests only send tokens and compare text, while the Domain helper also applies them to a Cube (DRY
/// bent: the file-reading part is repeated here).
/// </summary>
internal static class TtcFixtureTestHelper
{
    private const string FixtureFolder = "Fixtures";
    private const string SequenceFile = "ttc-sequence.txt";
    private const string ExplodedViewFile = "ttc-exploded-view.txt";

    public static IReadOnlyList<string> Tokens =>
        File.ReadAllText(FixturePath(SequenceFile))
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static IReadOnlyList<string> ExpectedLines => File.ReadAllLines(FixturePath(ExplodedViewFile));

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, FixtureFolder, fileName);
}
