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

    // TryUpdate swaps the entry only if it is still the very object that was checked (CubeSession is a class, so
    // that is a reference comparison). A false result does not mean "conflict": it only means another writer replaced
    // the entry after we read it. The loop therefore reads again and decides on the fresh session: a different
    // revision gives RevisionConflict, an equal one (same content) is still a valid write. It repeats only when
    // another writer succeeded in between, so it ends and never blocks.
    public Task<UpdateOutcome> TryUpdateAsync(string expectedRevision, CubeSession replacement, CancellationToken cancellationToken)
    {
        // Retry loop, not a spin on a conflict: we only come round again when TryUpdate lost to another writer.
        while (true)
        {
            if (!sessions.TryGetValue(replacement.CubeId, out var current))
            {
                return Task.FromResult(UpdateOutcome.NotFound);
            }

            if (current.Revision != expectedRevision)
            {
                return Task.FromResult(UpdateOutcome.RevisionConflict);
            }

            if (sessions.TryUpdate(replacement.CubeId, replacement, current))
            {
                return Task.FromResult(UpdateOutcome.Updated);
            }
        }
    }
}
