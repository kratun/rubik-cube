using Rubik.Application.Cubes;
using Rubik.Domain.Cubes;
using Rubik.Infrastructure.Cubes;

namespace Rubik.Infrastructure.Tests.Cubes;

public class InMemoryCubeRepositoryTests
{
    private static readonly Move FrontTurn = new(Face.Front, Direction.Clockwise);

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
    public async Task CreateAsync_ExistingId_Throws()
    {
        var session = new CubeSession(Guid.NewGuid(), []);
        await repository.CreateAsync(session, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.CreateAsync(session, CancellationToken.None));
    }
}
