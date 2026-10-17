using Rubik.Application.Cubes;

namespace Rubik.Api.Tests.TestHelpers;

/// <summary>Test helper: a repository whose every call fails, to drive the unexpected-error path.</summary>
internal sealed class ThrowingCubeRepositoryTestHelper : ICubeRepository
{
    public const string FailureMessage = "storage is down";

    public Task CreateAsync(CubeSession session, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(FailureMessage);

    public Task<CubeSession?> GetAsync(Guid cubeId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(FailureMessage);

    public Task<UpdateOutcome> TryUpdateAsync(string expectedRevision, CubeSession replacement, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(FailureMessage);
}
