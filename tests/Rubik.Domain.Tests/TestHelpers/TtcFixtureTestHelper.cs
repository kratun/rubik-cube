using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.TestHelpers;

/// <summary>
/// Test helper: the TTC Test Sequence and its expected exploded view, read from the shared fixture files in
/// <c>tests/Fixtures</c> (one source for the domain, API and UI tests), and a way to apply the sequence to a cube.
/// </summary>
internal static class TtcFixtureTestHelper
{
    private const string FixtureFolder = "Fixtures";
    private const string SequenceFile = "ttc-sequence.txt";
    private const string ExplodedViewFile = "ttc-exploded-view.txt";
    private const char TokenSeparator = ' ';

    public static string Sequence => File.ReadAllText(FixturePath(SequenceFile)).Trim();

    public static IReadOnlyList<string> ExpectedLines => File.ReadAllLines(FixturePath(ExplodedViewFile));

    public static Cube Apply(Cube cube, string sequence) =>
        sequence
            .Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(
                cube,
                (current, token) =>
                    Move.TryParse(token, out var move)
                        ? current.Apply(move)
                        : throw new ArgumentException($"Not a move: {token}", nameof(sequence))
            );

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, FixtureFolder, fileName);
}
