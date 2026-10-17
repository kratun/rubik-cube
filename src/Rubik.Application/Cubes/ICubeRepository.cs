namespace Rubik.Application.Cubes;

/// <summary>Port for storing cube sessions. Owned by Application; adapters live in Infrastructure.</summary>
public interface ICubeRepository
{
    /// <summary>Stores a new session. The id must not exist yet.</summary>
    Task CreateAsync(CubeSession session, CancellationToken cancellationToken);

    /// <summary>Returns the session with <paramref name="cubeId"/>, or null when there is none.</summary>
    Task<CubeSession?> GetAsync(Guid cubeId, CancellationToken cancellationToken);
}
