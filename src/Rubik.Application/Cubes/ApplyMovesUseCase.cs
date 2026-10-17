using Rubik.Application.Results;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>
/// Appends moves to a session if the caller still sees its current revision (AD-5, AD-6). The tokens are parsed first;
/// the revision check and the write are <see cref="RevisionCheckedUpdate"/>.
/// </summary>
public sealed class ApplyMovesUseCase(RevisionCheckedUpdate update)
{
    public async Task<Result<CubeState>> ExecuteAsync(
        Guid cubeId,
        string expectedRevision,
        IReadOnlyList<string?> tokens,
        CancellationToken cancellationToken)
    {
        var parsed = new List<Move>(tokens.Count);
        foreach (var token in tokens)
        {
            if (!Move.TryParse(token, out var move))
            {
                return Result<CubeState>.Failure(new InvalidMove(token));
            }

            parsed.Add(move);
        }

        return await update.ExecuteAsync(cubeId, expectedRevision, current => current.Concat(parsed), cancellationToken);
    }
}
