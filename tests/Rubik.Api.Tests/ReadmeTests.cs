using Rubik.Api.Tests.TestHelpers;

namespace Rubik.Api.Tests;

/// <summary>Keeps the README's copy of the TTC expected output and sequence in line with the shared fixtures.</summary>
public class ReadmeTests
{
    private const string ReadmeFile = "README.md";
    private const string LineSeparator = "\n";
    private const string TokenSeparator = ",";
    private const string TokenQuote = "\"";
    private const string MovesArrayFormat = "\"moves\":[{0}]";

    private static string Readme =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ReadmeFile)).ReplaceLineEndings(LineSeparator);

    [Fact]
    public void Readme_Always_ContainsTheFixtureExplodedViewAsOneBlock()
    {
        var expectedBlock = string.Join(LineSeparator, TtcFixtureTestHelper.ExpectedLines);

        Assert.Contains(expectedBlock, Readme);
    }

    [Fact]
    public void Readme_Always_HasTheFixtureSequenceAsTheMovesArrayOfTheMovesCommand()
    {
        var quoted = TtcFixtureTestHelper.Tokens.Select(token => TokenQuote + token + TokenQuote);
        var expectedArray = string.Format(MovesArrayFormat, string.Join(TokenSeparator, quoted));

        Assert.Contains(expectedArray, Readme);
    }
}
