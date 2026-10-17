using Rubik.Application.Cubes;
using Rubik.Application.Tests.TestHelpers;
using Rubik.Domain.Cubes;

namespace Rubik.Application.Tests.Cubes;

public class CreateCubeUseCaseTests
{
    private const string NewCubeRevision = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const int CreatedSessionCount = 2;

    private readonly CubeRepositoryTestHelper repository = new();

    [Fact]
    public async Task ExecuteAsync_Always_ReturnsSolvedStateWithNoMovesAndNewId()
    {
        var state = await new CreateCubeUseCase(repository).ExecuteAsync(CancellationToken.None);

        Assert.NotEqual(Guid.Empty, state.CubeId);
        Assert.Equal(NewCubeRevision, state.Revision);
        Assert.Empty(state.Moves);
        Assert.Equal(ExplodedViewRenderer.Render(new Cube()).Lines, state.ExplodedView);
    }

    [Fact]
    public async Task ExecuteAsync_Always_StoresOnlyTheIdAndAnEmptyMoveList()
    {
        var state = await new CreateCubeUseCase(repository).ExecuteAsync(CancellationToken.None);

        var stored = Assert.Single(repository.Created);
        Assert.Equal(state.CubeId, stored.CubeId);
        Assert.Empty(stored.Moves);
    }

    [Fact]
    public async Task ExecuteAsync_CalledTwice_CreatesTwoSessionsWithDifferentIds()
    {
        var useCase = new CreateCubeUseCase(repository);

        var first = await useCase.ExecuteAsync(CancellationToken.None);
        var second = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.NotEqual(first.CubeId, second.CubeId);
        Assert.Equal(CreatedSessionCount, repository.Created.Count);
    }
}
