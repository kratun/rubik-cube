namespace Rubik.Domain.Cubes;

/// <summary>
/// A cube laid out as a net. <see cref="Faces"/> holds one 3x3 colour grid per face, row by row;
/// <see cref="Lines"/> is the same net as text. Both come from one render, so they always agree.
/// </summary>
public sealed record ExplodedView(
    IReadOnlyDictionary<Face, IReadOnlyList<IReadOnlyList<Color>>> Faces,
    IReadOnlyList<string> Lines
);
