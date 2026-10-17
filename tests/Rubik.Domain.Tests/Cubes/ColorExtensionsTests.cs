using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.Cubes;

public class ColorExtensionsTests
{
    private const int UndefinedColor = 99;

    public static TheoryData<Color, char> ExpectedLetters => new()
    {
        { Color.White, 'W' },
        { Color.Yellow, 'Y' },
        { Color.Green, 'G' },
        { Color.Blue, 'B' },
        { Color.Red, 'R' },
        { Color.Orange, 'O' },
    };

    [Theory]
    [MemberData(nameof(ExpectedLetters))]
    public void Letter_EachColor_IsExpectedLetter(Color color, char expected)
    {
        Assert.Equal(expected, color.Letter());
    }

    [Fact]
    public void Letter_UndefinedColor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Color)UndefinedColor).Letter());
    }
}
