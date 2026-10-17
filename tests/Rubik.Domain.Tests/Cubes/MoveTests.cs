using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.Cubes;

public class MoveTests
{
    public static TheoryData<string, Face, Direction> ValidTokens => new()
    {
        { "F", Face.Front, Direction.Clockwise },
        { "R", Face.Right, Direction.Clockwise },
        { "U", Face.Up, Direction.Clockwise },
        { "B", Face.Back, Direction.Clockwise },
        { "L", Face.Left, Direction.Clockwise },
        { "D", Face.Down, Direction.Clockwise },
        { "F'", Face.Front, Direction.CounterClockwise },
        { "R'", Face.Right, Direction.CounterClockwise },
        { "U'", Face.Up, Direction.CounterClockwise },
        { "B'", Face.Back, Direction.CounterClockwise },
        { "L'", Face.Left, Direction.CounterClockwise },
        { "D'", Face.Down, Direction.CounterClockwise },
    };

    public static TheoryData<string> InvalidTokens => new()
    {
        "f",
        "r'",
        "u",
        "d'",
        "R'x",
        "F\n",
        "F'\n",
        "F\t",
        " ",
        "F2",
        "X",
        "F''",
        "F'R",
        "FF",
        "'",
        "'F",
        " F",
        "F ",
        "F\u2019",
        string.Empty,
    };

    [Theory]
    [MemberData(nameof(ValidTokens))]
    public void TryParse_ValidToken_ReturnsMatchingFaceAndDirection(string token, Face face, Direction direction)
    {
        var parsed = Move.TryParse(token, out var move);

        Assert.True(parsed);
        Assert.Equal(new Move(face, direction), move);
    }

    [Fact]
    public void TryParse_ValidTokens_CoverEveryFaceInBothDirections()
    {
        var parsedMoves = ValidTokens
            .Select(row => Move.TryParse(row.Data.Item1, out var move) ? move : default)
            .ToHashSet();
        var expectedMoves = from face in Enum.GetValues<Face>()
                            from direction in Enum.GetValues<Direction>()
                            select new Move(face, direction);

        Assert.True(parsedMoves.SetEquals(expectedMoves));
    }

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public void TryParse_InvalidToken_ReturnsFalseWithoutThrowing(string token)
    {
        var parsed = Move.TryParse(token, out var move);

        Assert.False(parsed);
        Assert.Equal(default, move);
    }

    [Fact]
    public void TryParse_NullToken_ReturnsFalseWithoutThrowing()
    {
        var parsed = Move.TryParse(null, out var move);

        Assert.False(parsed);
        Assert.Equal(default, move);
    }
}
