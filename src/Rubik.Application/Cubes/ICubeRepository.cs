namespace Rubik.Application.Cubes;

/// <summary>Port for storing cube sessions. Owned by Application; adapters live in Infrastructure.</summary>
public interface ICubeRepository
{
    /// <summary>Stores a new session. The id must not exist yet.</summary>
    Task CreateAsync(CubeSession session, CancellationToken cancellationToken);

    /// <summary>Returns the session with <paramref name="cubeId"/>, or null when there is none.</summary>
    Task<CubeSession?> GetAsync(Guid cubeId, CancellationToken cancellationToken);

    /// <summary>
    /// Compare-and-set (AD-5): stores <paramref name="replacement"/> (the session with its new move list) only if the
    /// stored session with the same id has the revision <paramref name="expectedRevision"/>, as one atomic step per
    /// cube. <paramref name="expectedRevision"/> is the caller's value, not one read back from storage. The new moves
    /// travel inside a <see cref="CubeSession"/> because Infrastructure may not reference Domain, so no signature of
    /// the port can name <c>Move</c>.
    /// </summary>
    Task<UpdateOutcome> TryUpdateAsync(string expectedRevision, CubeSession replacement, CancellationToken cancellationToken);
}
