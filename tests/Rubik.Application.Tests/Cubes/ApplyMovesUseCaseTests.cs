using Rubik.Application.Cubes;
using Rubik.Application.Tests.TestHelpers;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class ApplyMovesUseCaseTests
{
    private const string FrontToken = "F";
    private const string RightPrimeToken = "R'";
    private const string UnknownFaceToken = "X";
    private const string LowerCaseToken = "f";
    private const string StaleRevision = "stale";

    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);
    private static readonly Move RightPrime = new(Face.Right, Direction.CounterClockwise);
    private static readonly Move UpTurn = new(Face.Up, Direction.Clockwise);
    private static readonly Move BackTurn = new(Face.Back, Direction.Clockwise);
    private static readonly Move LeftTurn = new(Face.Left, Direction.Clockwise);
    private static readonly Move DownTurn = new(Face.Down, Direction.Clockwise);

    private readonly CubeRepositoryTestHelper repository = new();

    private ApplyMovesUseCase UseCase => new(repository);

    [Fact]
    public async Task ExecuteAsync_CurrentRevision_AppendsTheMovesAndReturnsTheNewState()
    {
        var session = await CreateSessionAsync(FrontTurn);

        var result = await UseCase.ExecuteAsync(session.CubeId, session.Revision, [RightPrimeToken], CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(repository.Created);
        Assert.Equal([FrontTurn, RightPrime], stored.Moves);
        Assert.Equal(stored.Revision, result.Value.Revision);
        Assert.Equal([FrontToken, RightPrimeToken], result.Value.Moves);
        Assert.Equal(CubeState.From(stored).ExplodedView, result.Value.ExplodedView);
    }

    [Fact]
    public async Task ExecuteAsync_CurrentRevision_PassesTheCallersRevisionUnchangedToTheUpdate()
    {
        var session = await CreateSessionAsync(FrontTurn);

        await UseCase.ExecuteAsync(session.CubeId, session.Revision, [RightPrimeToken], CancellationToken.None);

        var update = Assert.Single(repository.Updates);
        Assert.Equal(session.CubeId, update.Replacement.CubeId);
        Assert.Equal(session.Revision, update.ExpectedRevision);
        Assert.Equal([FrontTurn, RightPrime], update.Replacement.Moves);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentRevision_ReturnsRevisionConflictWithoutBuildingOrWritingAList()
    {
        var session = await CreateSessionAsync(FrontTurn);

        var result = await UseCase.ExecuteAsync(session.CubeId, StaleRevision, [RightPrimeToken], CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(new RevisionConflict(session.CubeId), result.Error);
        Assert.Empty(repository.Updates);
        Assert.Same(session, Assert.Single(repository.Created));
    }

    [Fact]
    public async Task ExecuteAsync_SameMoveCountButOtherHistory_ReturnsRevisionConflict()
    {
        var seen = new CubeSession(Guid.NewGuid(), [FrontTurn, RightPrime, UpTurn, BackTurn]);
        var current = new CubeSession(seen.CubeId, [FrontTurn, RightPrime, LeftTurn, DownTurn]);
        await repository.CreateAsync(current, CancellationToken.None);

        var result = await UseCase.ExecuteAsync(seen.CubeId, seen.Revision, [FrontToken], CancellationToken.None);

        Assert.Equal(new RevisionConflict(seen.CubeId), result.Error);
        Assert.Empty(repository.Updates);
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryReportsALostRace_ReturnsRevisionConflict()
    {
        var session = await CreateSessionAsync();
        repository.ForcedUpdateOutcome = UpdateOutcome.RevisionConflict;

        var result = await UseCase.ExecuteAsync(session.CubeId, session.Revision, [FrontToken], CancellationToken.None);

        Assert.Equal(new RevisionConflict(session.CubeId), result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SessionVanishesBeforeTheUpdate_ReturnsCubeNotFound()
    {
        var session = await CreateSessionAsync();
        repository.ForcedUpdateOutcome = UpdateOutcome.NotFound;

        var result = await UseCase.ExecuteAsync(session.CubeId, session.Revision, [FrontToken], CancellationToken.None);

        Assert.Equal(new CubeNotFound(session.CubeId), result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCube_ReturnsCubeNotFound()
    {
        var unknownId = Guid.NewGuid();

        var result = await UseCase.ExecuteAsync(unknownId, StaleRevision, [FrontToken], CancellationToken.None);

        Assert.Equal(new CubeNotFound(unknownId), result.Error);
        Assert.Empty(repository.Updates);
    }

    [Theory]
    [InlineData(UnknownFaceToken)]
    [InlineData(LowerCaseToken)]
    [InlineData("")]
    [InlineData(null)]
    public async Task ExecuteAsync_InvalidToken_ReturnsInvalidMoveNamingItAndChangesNothing(string? token)
    {
        var session = await CreateSessionAsync(FrontTurn);

        var result = await UseCase.ExecuteAsync(session.CubeId, session.Revision, [FrontToken, token], CancellationToken.None);

        Assert.Equal(new InvalidMove(token), result.Error);
        Assert.Empty(repository.Updates);
        Assert.Same(session, Assert.Single(repository.Created));
    }

    // Pins the order: the request's own tokens are checked before the cube is looked up or its revision compared.
    [Fact]
    public async Task ExecuteAsync_InvalidTokenAndStaleRevision_ReturnsInvalidMove()
    {
        var session = await CreateSessionAsync(FrontTurn);

        var result = await UseCase.ExecuteAsync(session.CubeId, StaleRevision, [UnknownFaceToken], CancellationToken.None);

        Assert.Equal(new InvalidMove(UnknownFaceToken), result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTokenAndUnknownCube_ReturnsInvalidMove()
    {
        var result = await UseCase.ExecuteAsync(Guid.NewGuid(), StaleRevision, [UnknownFaceToken], CancellationToken.None);

        Assert.Equal(new InvalidMove(UnknownFaceToken), result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SeveralInvalidTokens_NamesTheFirstOne()
    {
        var session = await CreateSessionAsync();

        var result = await UseCase.ExecuteAsync(
            session.CubeId, session.Revision, [FrontToken, LowerCaseToken, UnknownFaceToken], CancellationToken.None);

        Assert.Equal(new InvalidMove(LowerCaseToken), result.Error);
    }

    private async Task<CubeSession> CreateSessionAsync(params Move[] moves)
    {
        var session = new CubeSession(Guid.NewGuid(), moves);
        await repository.CreateAsync(session, CancellationToken.None);
        return session;
    }
}
