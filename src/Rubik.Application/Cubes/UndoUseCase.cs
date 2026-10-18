using Rubik.Application.Results;

namespace Rubik.Application.Cubes;

/// <summary>
/// Removes the last move if the caller still sees the session's current revision (FR7). On an empty session the list
/// stays empty, so the call succeeds as a no-op.
/// </summary>
public sealed class UndoUseCase(RevisionCheckedUpdate update)
{
    public Task<Result<CubeState>> ExecuteAsync(
        Guid cubeId,
        string expectedRevision,
        CancellationToken cancellationToken
    ) => update.ExecuteAsync(cubeId, expectedRevision, current => current.SkipLast(1), cancellationToken);
}
