using Rubik.Application.Cubes;
using Rubik.Application.Tests.TestHelpers;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class ResetUseCaseTests
{
    private const string StaleRevision = "stale";

    // DRY bent on purpose: the use case test classes share a shape only by coincidence, so each keeps its own small
    // fixtures.
    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);
    private static readonly Move RightPrime = new(Face.Right, Direction.CounterClockwise);

    private readonly CubeRepositoryTestHelper repository = new();

    private ResetUseCase UseCase => new(new RevisionCheckedUpdate(repository));

    [Fact]
    public async Task ExecuteAsync_SessionWithMoves_ClearsTheMovesAndReturnsTheEmptyListRevision()
    {
        var session = await CreateSessionAsync(FrontTurn, RightPrime);

        var result = await UseCase.ExecuteAsync(session.CubeId, session.Revision, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(Assert.Single(repository.Created).Moves);
        Assert.Equal(CubeRevision.Of([]), result.Value.Revision);
        Assert.Empty(result.Value.Moves);
        Assert.Equal(CubeState.From(new CubeSession(session.CubeId, [])).ExplodedView, result.Value.ExplodedView);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySession_SucceedsWithTheSameRevision()
    {
        var session = await CreateSessionAsync();

        var result = await UseCase.ExecuteAsync(session.CubeId, session.Revision, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(session.Revision, result.Value.Revision);
    }

    [Fact]
    public async Task ExecuteAsync_StaleRevision_ReturnsRevisionConflictAndChangesNothing()
    {
        var session = await CreateSessionAsync(FrontTurn);

        var result = await UseCase.ExecuteAsync(session.CubeId, StaleRevision, CancellationToken.None);

        Assert.Equal(new RevisionConflict(session.CubeId), result.Error);
        Assert.Empty(repository.Updates);
        Assert.Same(session, Assert.Single(repository.Created));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCube_ReturnsCubeNotFound()
    {
        var unknownId = Guid.NewGuid();

        var result = await UseCase.ExecuteAsync(unknownId, StaleRevision, CancellationToken.None);

        Assert.Equal(new CubeNotFound(unknownId), result.Error);
    }

    private async Task<CubeSession> CreateSessionAsync(params Move[] moves)
    {
        var session = new CubeSession(Guid.NewGuid(), moves);
        await repository.CreateAsync(session, CancellationToken.None);
        return session;
    }
}
