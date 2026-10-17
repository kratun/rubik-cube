using Rubik.Application.Results;

namespace Rubik.Application.Cubes;

/// <summary>The caller's expected revision is not the session's current one, so nothing was written.</summary>
public sealed record RevisionConflict(Guid CubeId) : ApplicationError;
