using Rubik.Application.Cubes;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class CubeStateTests
{
    private const string FrontNotation = "F";
    private const string UpPrimeNotation = "U'";
    private const string FrontKey = "front";
    private const string GreenName = "Green";
    private const int GridSize = 3;

    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);
    private static readonly Move UpPrimeTurn = new(Face.Up, Direction.CounterClockwise);

    private static readonly string[] FaceKeys = ["up", "down", "left", "right", "front", "back"];

    [Fact]
    public void From_SessionWithoutMoves_RendersTheSolvedCube()
    {
        var session = new CubeSession(Guid.NewGuid(), []);

        var state = CubeState.From(session);

        Assert.Equal(session.CubeId, state.CubeId);
        Assert.Equal(CubeRevision.Of(session.Moves), state.Revision);
        Assert.Empty(state.Moves);
        Assert.Equal(ExplodedViewRenderer.Render(new Cube()).Lines, state.ExplodedView);
    }

    [Fact]
    public void From_SessionWithMoves_ReplaysThemInOrderOnASolvedCube()
    {
        var session = new CubeSession(Guid.NewGuid(), [FrontTurn, UpPrimeTurn]);

        var state = CubeState.From(session);

        var expected = ExplodedViewRenderer.Render(new Cube().Apply(FrontTurn).Apply(UpPrimeTurn));
        Assert.Equal(expected.Lines, state.ExplodedView);
        Assert.Equal(CubeRevision.Of(session.Moves), state.Revision);
    }

    [Fact]
    public void From_SessionWithMoves_WritesMovesAsNotationInOrder()
    {
        var session = new CubeSession(Guid.NewGuid(), [FrontTurn, UpPrimeTurn]);

        Assert.Equal([FrontNotation, UpPrimeNotation], CubeState.From(session).Moves);
    }

    [Fact]
    public void From_Solved_WritesLowerCaseFaceKeysWithColourNameGrids()
    {
        var state = CubeState.From(new CubeSession(Guid.NewGuid(), []));

        Assert.Equal(FaceKeys.Order(), state.Faces.Keys.Order());
        var front = state.Faces[FrontKey];
        Assert.Equal(GridSize, front.Count);
        Assert.All(front, row => Assert.Equal(Enumerable.Repeat(GreenName, GridSize), row));
    }
}
