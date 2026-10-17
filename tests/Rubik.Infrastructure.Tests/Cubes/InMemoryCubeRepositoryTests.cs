using Rubik.Application.Cubes;
using Rubik.Domain.Cubes;
using Rubik.Infrastructure.Cubes;

namespace Rubik.Infrastructure.Tests.Cubes;

public class InMemoryCubeRepositoryTests
{
    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);

    private static readonly Move RightTurn = new(Face.Right, Direction.Clockwise);

    private const string StaleRevision = "stale";
    private const int ParallelWriters = 32;
    private const int SingleWinner = 1;

    private readonly InMemoryCubeRepository repository = new();

    [Fact]
    public async Task GetAsync_AfterCreate_ReturnsTheStoredSession()
    {
        var session = new CubeSession(Guid.NewGuid(), [FrontTurn]);
        await repository.CreateAsync(session, CancellationToken.None);

        var found = await repository.GetAsync(session.CubeId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(session.CubeId, found.CubeId);
        Assert.Equal(session.Moves, found.Moves);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        var found = await repository.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task CreateAsync_TwoSessions_AreStoredIndependently()
    {
        var first = new CubeSession(Guid.NewGuid(), []);
        var second = new CubeSession(Guid.NewGuid(), [FrontTurn]);
        await repository.CreateAsync(first, CancellationToken.None);
        await repository.CreateAsync(second, CancellationToken.None);

        Assert.Empty((await repository.GetAsync(first.CubeId, CancellationToken.None))!.Moves);
        Assert.Single((await repository.GetAsync(second.CubeId, CancellationToken.None))!.Moves);
    }

    [Fact]
    public async Task TryUpdateAsync_ExpectedRevisionMatches_ReplacesTheMovesAndReturnsUpdated()
    {
        var session = new CubeSession(Guid.NewGuid(), [FrontTurn]);
        await repository.CreateAsync(session, CancellationToken.None);

        var outcome = await repository.TryUpdateAsync(session.Revision, new CubeSession(session.CubeId, [FrontTurn, RightTurn]), CancellationToken.None);

        Assert.Equal(UpdateOutcome.Updated, outcome);
        var stored = await repository.GetAsync(session.CubeId, CancellationToken.None);
        Assert.Equal([FrontTurn, RightTurn], stored!.Moves);
    }

    [Fact]
    public async Task TryUpdateAsync_ExpectedRevisionDiffers_ReturnsRevisionConflictAndKeepsTheSession()
    {
        var session = new CubeSession(Guid.NewGuid(), [FrontTurn]);
        await repository.CreateAsync(session, CancellationToken.None);

        var outcome = await repository.TryUpdateAsync(StaleRevision, new CubeSession(session.CubeId, [RightTurn]), CancellationToken.None);

        Assert.Equal(UpdateOutcome.RevisionConflict, outcome);
        Assert.Same(session, await repository.GetAsync(session.CubeId, CancellationToken.None));
    }

    [Fact]
    public async Task TryUpdateAsync_UnknownId_ReturnsNotFound()
    {
        var outcome = await repository.TryUpdateAsync(StaleRevision, new CubeSession(Guid.NewGuid(), [RightTurn]), CancellationToken.None);

        Assert.Equal(UpdateOutcome.NotFound, outcome);
    }

    [Fact]
    public async Task TryUpdateAsync_ManyParallelWritersWithTheSameRevision_ExactlyOneWins()
    {
        var session = new CubeSession(Guid.NewGuid(), []);
        await repository.CreateAsync(session, CancellationToken.None);
        using var gate = new ManualResetEventSlim();

        var writers = Enumerable.Range(0, ParallelWriters)
            .Select(_ => Task.Run(() =>
            {
                gate.Wait();
                return repository.TryUpdateAsync(session.Revision, new CubeSession(session.CubeId, [RightTurn]), CancellationToken.None);
            }))
            .ToList();
        gate.Set();
        var outcomes = await Task.WhenAll(writers);

        Assert.Equal(SingleWinner, outcomes.Count(outcome => outcome == UpdateOutcome.Updated));
        Assert.Equal(ParallelWriters - SingleWinner, outcomes.Count(outcome => outcome == UpdateOutcome.RevisionConflict));
    }

    [Fact]
    public async Task CreateAsync_ExistingId_Throws()
    {
        var session = new CubeSession(Guid.NewGuid(), []);
        await repository.CreateAsync(session, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.CreateAsync(session, CancellationToken.None));
    }
}
