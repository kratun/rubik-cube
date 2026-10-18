using Rubik.Application.Results;

namespace Rubik.Application.Cubes;

/// <summary>
/// A token in the request is not a move in standard notation. <paramref name="Token"/> is the first one that failed
/// (null when the request held a null).
/// </summary>
public sealed record InvalidMove(string? Token) : ApplicationError;
