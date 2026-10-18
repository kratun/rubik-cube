using Rubik.Application.Results;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>
/// Clears the move list if the caller still sees the session's current revision (FR7). On an empty session the list
/// stays empty, so the call succeeds as a no-op.
/// </summary>
public sealed class ResetUseCase(RevisionCheckedUpdate update)
{
    public Task<Result<CubeState>> ExecuteAsync(
        Guid cubeId,
        string expectedRevision,
        CancellationToken cancellationToken
    ) => update.ExecuteAsync(cubeId, expectedRevision, _ => Enumerable.Empty<Move>(), cancellationToken);
}
