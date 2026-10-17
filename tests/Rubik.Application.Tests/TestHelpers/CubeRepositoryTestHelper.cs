using Rubik.Application.Cubes;

namespace Rubik.Application.Tests.TestHelpers;

/// <summary>Test helper: a repository that keeps what it was given in a list, so tests can see what a use case stored.</summary>
internal sealed class CubeRepositoryTestHelper : ICubeRepository
{
    private readonly List<CubeSession> created = [];

    public IReadOnlyList<CubeSession> Created => created;

    public Task CreateAsync(CubeSession session, CancellationToken cancellationToken)
    {
        created.Add(session);
        return Task.CompletedTask;
    }

    public Task<CubeSession?> GetAsync(Guid cubeId, CancellationToken cancellationToken) =>
        Task.FromResult(created.SingleOrDefault(session => session.CubeId == cubeId));
}
