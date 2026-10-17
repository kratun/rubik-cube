using Rubik.Application.Cubes;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class CubeSessionTests
{
    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);

    [Fact]
    public void Constructor_CallerChangesItsListLater_SessionIsUnchanged()
    {
        var moves = new List<Move>();
        var session = new CubeSession(Guid.NewGuid(), moves);

        moves.Add(FrontTurn);

        Assert.Empty(session.Moves);
    }
}
