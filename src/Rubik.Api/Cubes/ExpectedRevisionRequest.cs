using System.ComponentModel.DataAnnotations;

namespace Rubik.Api.Cubes;

/// <summary>Body of <c>POST /api/cubes/{id}/undo</c> and <c>/reset</c> (AD-7). The revision is required (AD-6).</summary>
public sealed record ExpectedRevisionRequest([Required] string ExpectedRevision);
