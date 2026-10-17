using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>What is stored for a cube: its id and the moves applied so far. Everything else is derived (AD-4).</summary>
public sealed class CubeSession
{
    public CubeSession(Guid cubeId, IEnumerable<Move> moves)
    {
        CubeId = cubeId;

        // A private copy, so nobody can change a stored session through the list they passed in.
        Moves = moves.ToArray();
    }

    public Guid CubeId { get; }

    public IReadOnlyList<Move> Moves { get; }
}
