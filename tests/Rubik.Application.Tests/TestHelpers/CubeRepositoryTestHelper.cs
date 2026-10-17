using Rubik.Application.Cubes;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.TestHelpers;

/// <summary>
/// Test helper: a repository that keeps sessions in a list and records what a use case stored and asked for. With
/// <see cref="ForcedUpdateOutcome"/> set, <see cref="TryUpdateAsync"/> answers that outcome without writing, to
/// simulate a writer that got in between the read and the write.
/// </summary>
internal sealed class CubeRepositoryTestHelper : ICubeRepository
{
    private readonly List<CubeSession> created = [];
    private readonly List<UpdateRequest> updates = [];

    public sealed record UpdateRequest(string ExpectedRevision, CubeSession Replacement);

    public IReadOnlyList<CubeSession> Created => created;

    public IReadOnlyList<UpdateRequest> Updates => updates;

    public UpdateOutcome? ForcedUpdateOutcome { get; set; }

    public Task CreateAsync(CubeSession session, CancellationToken cancellationToken)
    {
        created.Add(session);
        return Task.CompletedTask;
    }

    public Task<CubeSession?> GetAsync(Guid cubeId, CancellationToken cancellationToken) =>
        Task.FromResult(created.SingleOrDefault(session => session.CubeId == cubeId));

    public Task<UpdateOutcome> TryUpdateAsync(string expectedRevision, CubeSession replacement, CancellationToken cancellationToken)
    {
        updates.Add(new UpdateRequest(expectedRevision, replacement));

        if (ForcedUpdateOutcome is { } forced)
        {
            return Task.FromResult(forced);
        }

        var current = created.SingleOrDefault(session => session.CubeId == replacement.CubeId);
        if (current is null)
        {
            return Task.FromResult(UpdateOutcome.NotFound);
        }

        if (current.Revision != expectedRevision)
        {
            return Task.FromResult(UpdateOutcome.RevisionConflict);
        }

        created[created.IndexOf(current)] = replacement;
        return Task.FromResult(UpdateOutcome.Updated);
    }
}
