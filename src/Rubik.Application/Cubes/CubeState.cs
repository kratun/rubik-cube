using Rubik.Domain.Cubes;

namespace Rubik.Application.Cubes;

/// <summary>
/// A cube session as clients see it (AD-7), rebuilt from the stored moves (AD-4); the revision is derived from them too.
/// It holds no Domain types, so Api can return it as is (AD-1): moves are notation tokens, face keys are lower case,
/// colours are their names.
/// </summary>
public sealed record CubeState(
    Guid CubeId,
    string Revision,
    IReadOnlyList<string> Moves,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> Faces,
    IReadOnlyList<string> ExplodedView)
{
    /// <summary>Replays the session's moves on a solved cube and renders the result.</summary>
    public static CubeState From(CubeSession session)
    {
        var cube = session.Moves.Aggregate(new Cube(), (current, move) => current.Apply(move));
        var view = ExplodedViewRenderer.Render(cube);

        return new CubeState(
            session.CubeId,
            session.Revision,
            session.Moves.Select(move => move.ToNotation()).ToList(),
            view.Faces.ToDictionary(
                face => face.Key.ToString().ToLowerInvariant(),
                face => (IReadOnlyList<IReadOnlyList<string>>)face.Value
                    .Select(row => (IReadOnlyList<string>)row.Select(color => color.ToString()).ToList())
                    .ToList()),
            view.Lines);
    }
}
