using Microsoft.AspNetCore.Mvc;
using Rubik.Api.Errors;
using Rubik.Application.Cubes;

namespace Rubik.Api.Cubes;

[ApiController]
[Route(CubesRoute)]
public class CubesController(CreateCubeUseCase createCube, GetCubeUseCase getCube, ApplyMovesUseCase applyMoves, ResultMapper resultMapper) : ControllerBase
{
    public const string CubesRoute = "api/cubes";

    [HttpPost]
    [ProducesResponseType<CubeState>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> Create(CancellationToken cancellationToken)
    {
        var state = await createCube.ExecuteAsync(cancellationToken);
        return Created($"/{CubesRoute}/{state.CubeId}", state);
    }

    // No route constraint on id: a malformed id must reach the model-state mapping and get an ErrorDetails body.
    [HttpGet("{id}")]
    [ProducesResponseType<CubeState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getCube.ExecuteAsync(id, cancellationToken);
        return resultMapper.ToActionResult(result, HttpContext);
    }

    [HttpPost("{id}/moves")]
    [ProducesResponseType<CubeState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErrorDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyMoves(Guid id, ApplyMovesRequest request, CancellationToken cancellationToken)
    {
        var result = await applyMoves.ExecuteAsync(id, request.ExpectedRevision, request.Moves, cancellationToken);
        return resultMapper.ToActionResult(result, HttpContext);
    }
}
