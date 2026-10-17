using Rubik.Application.Results;

namespace Rubik.Application.Cubes;

/// <summary>No cube session exists with the given id.</summary>
public sealed record CubeNotFound(Guid CubeId) : ApplicationError;
