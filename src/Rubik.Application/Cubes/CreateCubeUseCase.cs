namespace Rubik.Application.Cubes;

/// <summary>Creates a cube session with a new id and no moves, so its cube is solved.</summary>
public sealed class CreateCubeUseCase(ICubeRepository repository)
{
    public async Task<CubeState> ExecuteAsync(CancellationToken cancellationToken)
    {
        var session = new CubeSession(Guid.NewGuid(), []);
        await repository.CreateAsync(session, cancellationToken);
        return CubeState.From(session);
    }
}
