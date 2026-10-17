namespace Rubik.Domain.Cubes;

public sealed record Cubie(Vector3 Position, IReadOnlyList<Sticker> Stickers);
