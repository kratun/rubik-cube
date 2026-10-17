using Rubik.Application.Results;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>
/// Appends moves to a session if the caller still sees its current revision (AD-5, AD-6). The new list is built only
/// from a snapshot whose revision equals <c>expectedRevision</c>, and the write is a compare-and-set that gets the
/// caller's <c>expectedRevision</c> unchanged, so a lost race comes back as <see cref="RevisionConflict"/>.
/// </summary>
public sealed class ApplyMovesUseCase(ICubeRepository repository)
{
    private const string UnknownOutcomeFormat = "Unknown update outcome {0}.";

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

        var snapshot = await repository.GetAsync(cubeId, cancellationToken);
        if (snapshot is null)
        {
            return Result<CubeState>.Failure(new CubeNotFound(cubeId));
        }

        if (snapshot.Revision != expectedRevision)
        {
            return Result<CubeState>.Failure(new RevisionConflict(cubeId));
        }

        var replacement = new CubeSession(cubeId, snapshot.Moves.Concat(parsed));
        var outcome = await repository.TryUpdateAsync(expectedRevision, replacement, cancellationToken);

        return outcome switch
        {
            UpdateOutcome.Updated => Result<CubeState>.Success(CubeState.From(replacement)),
            UpdateOutcome.RevisionConflict => Result<CubeState>.Failure(new RevisionConflict(cubeId)),
            UpdateOutcome.NotFound => Result<CubeState>.Failure(new CubeNotFound(cubeId)),
            _ => throw new InvalidOperationException(string.Format(UnknownOutcomeFormat, outcome)),
        };
    }
}
