using System.Security.Cryptography;
using System.Text;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>
/// The revision of a session: a token derived only from its move list. Equal move lists give equal revisions and
/// any other list gives a different one, so a client holding an old revision cannot write over a history it never saw
/// (a move count cannot promise that: undo and new moves can bring the count back to the same number).
/// </summary>
public static class CubeRevision
{
    private const string MoveSeparator = " ";

    public static string Of(IReadOnlyList<Move> moves) =>
        Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Join(MoveSeparator, moves.Select(move => move.ToNotation())))));
}
