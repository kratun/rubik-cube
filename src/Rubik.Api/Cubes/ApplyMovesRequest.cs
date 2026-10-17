using System.ComponentModel.DataAnnotations;

namespace Rubik.Api.Cubes;

/// <summary>Body of <c>POST /api/cubes/{id}/moves</c> (AD-7). Both parts are required and the list must not be empty (AD-6).</summary>
public sealed record ApplyMovesRequest(
    [Required] string ExpectedRevision,
    [Required, MinLength(1)] IReadOnlyList<string?> Moves);
