using Rubik.Application.Cubes;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class CubeRevisionTests
{
    // SHA-256 of the empty text: the revision of a session without moves.
    private const string EmptyMoveListRevision = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private static readonly Move Front = new(Face.Front, Direction.Clockwise);
    private static readonly Move Right = new(Face.Right, Direction.Clockwise);
    private static readonly Move Up = new(Face.Up, Direction.Clockwise);
    private static readonly Move Down = new(Face.Down, Direction.Clockwise);
    private static readonly Move FrontPrime = new(Face.Front, Direction.CounterClockwise);

    [Fact]
    public void Of_NoMoves_ReturnsTheRevisionOfEmptyText()
    {
        Assert.Equal(EmptyMoveListRevision, CubeRevision.Of([]));
    }

    [Fact]
    public void Of_SameMoves_ReturnsSameRevision()
    {
        Assert.Equal(CubeRevision.Of([Front, Right, Up]), CubeRevision.Of([Front, Right, Up]));
    }

    [Fact]
    public void Of_SameMovesInOtherOrder_ReturnsDifferentRevision()
    {
        Assert.NotEqual(CubeRevision.Of([Front, Right]), CubeRevision.Of([Right, Front]));
    }

    [Fact]
    public void Of_ClockwiseAndCounterClockwiseMove_ReturnDifferentRevisions()
    {
        Assert.NotEqual(CubeRevision.Of([Front]), CubeRevision.Of([FrontPrime]));
    }

    [Fact]
    public void Of_ListsOfEqualLengthWithDifferentMoves_ReturnDifferentRevisions()
    {
        // The two-tab case: both tabs hold four moves, but one tab undid two moves and applied two others.
        var seenByStaleClient = new[] { Front, Right, Up, Down };
        var onServer = new[] { Front, Right, Down, Up };

        Assert.Equal(seenByStaleClient.Length, onServer.Length);
        Assert.NotEqual(CubeRevision.Of(seenByStaleClient), CubeRevision.Of(onServer));
    }

    [Fact]
    public void Of_ListBackToAnEarlierContent_ReturnsTheEarlierRevision()
    {
        var before = CubeRevision.Of([Front, Right]);

        var moves = new List<Move> { Front, Right, Up };
        moves.RemoveAt(moves.Count - 1);
        var afterUndo = CubeRevision.Of(moves);

        Assert.Equal(before, afterUndo);
    }
}
