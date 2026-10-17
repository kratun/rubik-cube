using Rubik.Application.Results;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>
/// The one revision-checked write that apply, undo and reset share (AD-5, AD-6). The caller's <c>expectedRevision</c>
/// is compared with a snapshot first; only then is the new move list built, from that snapshot alone, and written with
/// the caller's <c>expectedRevision</c> unchanged, so a lost race comes back as <see cref="RevisionConflict"/>.
/// </summary>
public sealed class RevisionCheckedUpdate(ICubeRepository repository)
{
    private const string UnknownOutcomeFormat = "Unknown update outcome {0}.";

    public async Task<Result<CubeState>> ExecuteAsync(
        Guid cubeId,
        string expectedRevision,
        Func<IReadOnlyList<Move>, IEnumerable<Move>> buildMoves,
        CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetAsync(cubeId, cancellationToken);
        if (snapshot is null)
        {
            return Result<CubeState>.Failure(new CubeNotFound(cubeId));
        }

        if (snapshot.Revision != expectedRevision)
        {
            return Result<CubeState>.Failure(new RevisionConflict(cubeId));
        }

        var replacement = new CubeSession(cubeId, buildMoves(snapshot.Moves));
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
