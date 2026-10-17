using Rubik.Application.Results;

namespace Rubik.Application.Cubes;

/// <summary>Reads a cube session and rebuilds its state by replay, or reports <see cref="CubeNotFound"/>.</summary>
public sealed class GetCubeUseCase(ICubeRepository repository)
{
    public async Task<Result<CubeState>> ExecuteAsync(Guid cubeId, CancellationToken cancellationToken)
    {
        var session = await repository.GetAsync(cubeId, cancellationToken);
        return session is null
            ? Result<CubeState>.Failure(new CubeNotFound(cubeId))
            : Result<CubeState>.Success(CubeState.From(session));
    }
}
