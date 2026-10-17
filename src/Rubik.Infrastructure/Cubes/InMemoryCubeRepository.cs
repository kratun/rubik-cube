using System.Collections.Concurrent;
using Rubik.Application.Cubes;

namespace Rubik.Infrastructure.Cubes;

/// <summary>Keeps sessions in memory for the life of the process. Register it as a singleton, or sessions are lost.</summary>
public sealed class InMemoryCubeRepository : ICubeRepository
{
    private readonly ConcurrentDictionary<Guid, CubeSession> sessions = new();

    public Task CreateAsync(CubeSession session, CancellationToken cancellationToken)
    {
        if (!sessions.TryAdd(session.CubeId, session))
        {
            throw new InvalidOperationException($"A cube with id {session.CubeId} already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<CubeSession?> GetAsync(Guid cubeId, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.GetValueOrDefault(cubeId));
}
