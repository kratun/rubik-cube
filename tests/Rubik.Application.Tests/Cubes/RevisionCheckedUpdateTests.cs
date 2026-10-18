using Rubik.Application.Cubes;
using Rubik.Application.Tests.TestHelpers;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class RevisionCheckedUpdateTests
{
    private const string StaleRevision = "stale";
    private const UpdateOutcome UnknownOutcome = (UpdateOutcome)(-1);

    // DRY bent on purpose: see UndoUseCaseTests; the fixtures are tiny and the classes share a shape only by
    // coincidence.
    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);
    private static readonly Move RightPrime = new(Face.Right, Direction.CounterClockwise);

    private readonly CubeRepositoryTestHelper repository = new();

    private RevisionCheckedUpdate Update => new(repository);

    [Fact]
    public async Task ExecuteAsync_CurrentRevision_BuildsFromTheSnapshotAndPassesTheCallersRevisionUnchanged()
    {
        var session = await CreateSessionAsync(FrontTurn);
        IReadOnlyList<Move>? seen = null;

        var result = await Update.ExecuteAsync(
            session.CubeId,
            session.Revision,
            current =>
            {
                seen = current;
                return current.Append(RightPrime);
            },
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal([FrontTurn], seen);
        var update = Assert.Single(repository.Updates);
        Assert.Equal(session.Revision, update.ExpectedRevision);
        Assert.Equal([FrontTurn, RightPrime], update.Replacement.Moves);
        Assert.Equal(update.Replacement.Revision, result.Value.Revision);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentRevision_ReturnsRevisionConflictWithoutRunningTheBuilder()
    {
        var session = await CreateSessionAsync(FrontTurn);
        var builderRan = false;

        var result = await Update.ExecuteAsync(
            session.CubeId,
            StaleRevision,
            current =>
            {
                builderRan = true;
                return current;
            },
            CancellationToken.None
        );

        Assert.Equal(new RevisionConflict(session.CubeId), result.Error);
        Assert.False(builderRan);
        Assert.Empty(repository.Updates);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCube_ReturnsCubeNotFound()
    {
        var unknownId = Guid.NewGuid();

        var result = await Update.ExecuteAsync(unknownId, StaleRevision, current => current, CancellationToken.None);

        Assert.Equal(new CubeNotFound(unknownId), result.Error);
        Assert.Empty(repository.Updates);
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryReportsALostRace_ReturnsRevisionConflict()
    {
        var session = await CreateSessionAsync();
        repository.ForcedUpdateOutcome = UpdateOutcome.RevisionConflict;

        var result = await Update.ExecuteAsync(
            session.CubeId,
            session.Revision,
            current => current,
            CancellationToken.None
        );

        Assert.Equal(new RevisionConflict(session.CubeId), result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SessionVanishesBeforeTheUpdate_ReturnsCubeNotFound()
    {
        var session = await CreateSessionAsync();
        repository.ForcedUpdateOutcome = UpdateOutcome.NotFound;

        var result = await Update.ExecuteAsync(
            session.CubeId,
            session.Revision,
            current => current,
            CancellationToken.None
        );

        Assert.Equal(new CubeNotFound(session.CubeId), result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownOutcome_Throws()
    {
        var session = await CreateSessionAsync();
        repository.ForcedUpdateOutcome = UnknownOutcome;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Update.ExecuteAsync(session.CubeId, session.Revision, current => current, CancellationToken.None)
        );
    }

    private async Task<CubeSession> CreateSessionAsync(params Move[] moves)
    {
        var session = new CubeSession(Guid.NewGuid(), moves);
        await repository.CreateAsync(session, CancellationToken.None);
        return session;
    }
}
