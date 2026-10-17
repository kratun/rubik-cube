using Rubik.Application.Cubes;
using Rubik.Application.Tests.TestHelpers;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class GetCubeUseCaseTests
{
    private readonly CubeRepositoryTestHelper repository = new();

    [Fact]
    public async Task ExecuteAsync_SessionExists_ReturnsTheReplayedState()
    {
        var moves = new[] { new Move(Face.Front, Direction.Clockwise), new Move(Face.Up, Direction.CounterClockwise) };
        var session = new CubeSession(Guid.NewGuid(), moves);
        await repository.CreateAsync(session, CancellationToken.None);

        var result = await new GetCubeUseCase(repository).ExecuteAsync(session.CubeId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CubeState.From(session), result.Value, new CubeStateComparer());
        Assert.Equal(session.CubeId, result.Value.CubeId);
    }

    [Fact]
    public async Task ExecuteAsync_NoSuchSession_ReturnsCubeNotFoundWithTheId()
    {
        var unknownId = Guid.NewGuid();

        var result = await new GetCubeUseCase(repository).ExecuteAsync(unknownId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(new CubeNotFound(unknownId), result.Error);
    }

    // CubeState holds lists, which a record compares by reference; the revision and rendered lines identify the state.
    private sealed class CubeStateComparer : IEqualityComparer<CubeState>
    {
        public bool Equals(CubeState? x, CubeState? y) =>
            x?.CubeId == y?.CubeId
            && x?.Revision == y?.Revision
            && (x?.Moves.SequenceEqual(y?.Moves ?? []) ?? false)
            && (x?.ExplodedView.SequenceEqual(y?.ExplodedView ?? []) ?? false);

        public int GetHashCode(CubeState obj) => obj.CubeId.GetHashCode();
    }
}
