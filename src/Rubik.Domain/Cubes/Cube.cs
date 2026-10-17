namespace Rubik.Domain.Cubes;

/// <summary>A 3x3x3 cube of 26 cubies. A new cube is solved.</summary>
public sealed class Cube
{
    public const int CubieCount = 26;

    /// <summary>Lowest and highest cubie coordinate on each axis.</summary>
    public const int MinCoordinate = -1;

    public const int MaxCoordinate = 1;

    /// <summary>A corner cubie shows a sticker on three faces, the most any cubie has.</summary>
    private const int MaxStickersPerCubie = 3;

    /// <summary>A cubie lies on a face when its position projected on the face normal reaches the outer layer.</summary>
    private const int OuterLayerProjection = 1;

    private static readonly IReadOnlyDictionary<Face, Color> SolvedColors = new Dictionary<Face, Color>
    {
        [Face.Front] = Color.Green,
        [Face.Right] = Color.Red,
        [Face.Up] = Color.White,
        [Face.Back] = Color.Blue,
        [Face.Left] = Color.Orange,
        [Face.Down] = Color.Yellow,
    };

    public Cube()
    {
        var cubies = new List<Cubie>(CubieCount);
        for (var x = MinCoordinate; x <= MaxCoordinate; x++)
        for (var y = MinCoordinate; y <= MaxCoordinate; y++)
        for (var z = MinCoordinate; z <= MaxCoordinate; z++)
        {
            var position = new Vector3(x, y, z);
            if (position == Vector3.Zero)
            {
                continue;
            }

            cubies.Add(new Cubie(position, SolvedStickersAt(position)));
        }

        Cubies = cubies.AsReadOnly();
    }

    public IReadOnlyList<Cubie> Cubies { get; }

    private static IReadOnlyList<Sticker> SolvedStickersAt(Vector3 position)
    {
        var stickers = new List<Sticker>(MaxStickersPerCubie);
        foreach (var (face, color) in SolvedColors)
        {
            var normal = face.Normal();
            if (Vector3.Dot(normal, position) == OuterLayerProjection)
            {
                stickers.Add(new Sticker(normal, color));
            }
        }

        return stickers.AsReadOnly();
    }
}
