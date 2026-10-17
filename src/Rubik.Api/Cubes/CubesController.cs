using Microsoft.AspNetCore.Mvc;
using Rubik.Application.Cubes;

namespace Rubik.Api.Cubes;

[ApiController]
[Route(CubesRoute)]
public class CubesController(CreateCubeUseCase createCube) : ControllerBase
{
    public const string CubesRoute = "api/cubes";

    [HttpPost]
    [ProducesResponseType<CubeState>(StatusCodes.Status201Created)]
    public async Task<CreatedResult> Create(CancellationToken cancellationToken)
    {
        var state = await createCube.ExecuteAsync(cancellationToken);
        return Created($"/{CubesRoute}/{state.CubeId}", state);
    }
}
