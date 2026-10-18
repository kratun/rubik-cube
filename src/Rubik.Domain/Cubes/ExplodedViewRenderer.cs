using System.Collections.ObjectModel;

namespace Rubik.Domain.Cubes;

/// <summary>
/// Projects a cube onto the exploded view: Up on top; Left, Front, Right and Back in one row; Down below.
/// The layout lives here, not on <see cref="Cube"/> (AD-3).
/// </summary>
public static class ExplodedViewRenderer
{
    private const int GridSize = 3;

    private const int CentreIndex = 1;

    private const string TileSeparator = " ";

    private const char IndentCharacter = ' ';

    private const int TileWidth = 1;

    private static readonly string FaceIndent = new(IndentCharacter, GridSize * (TileWidth + TileSeparator.Length));

    private static readonly IReadOnlyList<Face> MiddleBand = [Face.Left, Face.Front, Face.Right, Face.Back];

    // For each face: the direction of increasing column and increasing row when the face is viewed from outside,
    // with Up on top and Front below it.
    private static readonly IReadOnlyDictionary<Face, (Vector3 Column, Vector3 Row)> Orientations = new Dictionary<
        Face,
        (Vector3, Vector3)
    >
    {
        [Face.Front] = (Vector3.UnitX, -Vector3.UnitY),
        [Face.Right] = (-Vector3.UnitZ, -Vector3.UnitY),
        [Face.Back] = (-Vector3.UnitX, -Vector3.UnitY),
        [Face.Left] = (Vector3.UnitZ, -Vector3.UnitY),
        [Face.Up] = (Vector3.UnitX, Vector3.UnitZ),
        [Face.Down] = (Vector3.UnitX, -Vector3.UnitZ),
    };

    public static ExplodedView Render(Cube cube)
    {
        var faces = Enum.GetValues<Face>().ToDictionary(face => face, _ => new Color[GridSize, GridSize]);

        foreach (var cubie in cube.Cubies)
        {
            foreach (var sticker in cubie.Stickers)
            {
                var face = FaceOf(sticker.Normal);
                var (column, row) = Orientations[face];
                faces[face][
                    Vector3.Dot(cubie.Position, row) + CentreIndex,
                    Vector3.Dot(cubie.Position, column) + CentreIndex
                ] = sticker.Color;
            }
        }

        var grids = new ReadOnlyDictionary<Face, IReadOnlyList<IReadOnlyList<Color>>>(
            faces.ToDictionary(pair => pair.Key, pair => ToGrid(pair.Value))
        );
        return new ExplodedView(grids, ToLines(grids));
    }

    private static Face FaceOf(Vector3 normal) => Enum.GetValues<Face>().Single(face => face.Normal() == normal);

    private static IReadOnlyList<IReadOnlyList<Color>> ToGrid(Color[,] tiles) =>
        Enumerable
            .Range(0, GridSize)
            .Select(row =>
                (IReadOnlyList<Color>)
                    Enumerable.Range(0, GridSize).Select(column => tiles[row, column]).ToList().AsReadOnly()
            )
            .ToList()
            .AsReadOnly();

    private static IReadOnlyList<string> ToLines(IReadOnlyDictionary<Face, IReadOnlyList<IReadOnlyList<Color>>> grids)
    {
        var lines = new List<string>();
        lines.AddRange(Enumerable.Range(0, GridSize).Select(row => FaceIndent + RowText(grids[Face.Up], row)));
        lines.AddRange(
            Enumerable
                .Range(0, GridSize)
                .Select(row => string.Join(TileSeparator, MiddleBand.Select(face => RowText(grids[face], row))))
        );
        lines.AddRange(Enumerable.Range(0, GridSize).Select(row => FaceIndent + RowText(grids[Face.Down], row)));
        return lines.AsReadOnly();
    }

    private static string RowText(IReadOnlyList<IReadOnlyList<Color>> grid, int row) =>
        string.Join(TileSeparator, grid[row].Select(color => color.Letter()));
}
