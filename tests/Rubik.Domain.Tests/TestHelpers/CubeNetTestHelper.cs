using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.TestHelpers;

/// <summary>
/// Test helper: reads a cube as one 3x3 net per face, written as three row strings of colour letters.
/// It lives in the tests on purpose: an independent check on <c>ExplodedViewRenderer</c>, which the production cube does not share (AD-3).
/// </summary>
internal static class CubeNetTestHelper
{
    private const int GridSize = 3;
    private const int CentreIndex = 1;

    // For each face: the direction of increasing column and increasing row when the face is viewed from outside,
    // with Up on top and Front below it. Written out by hand, independent of the production mappings.
    private static readonly IReadOnlyDictionary<Face, (Vector3 Column, Vector3 Row)> Orientations =
        new Dictionary<Face, (Vector3, Vector3)>
        {
            [Face.Front] = (Vector3.UnitX, -Vector3.UnitY),
            [Face.Right] = (-Vector3.UnitZ, -Vector3.UnitY),
            [Face.Back] = (-Vector3.UnitX, -Vector3.UnitY),
            [Face.Left] = (Vector3.UnitZ, -Vector3.UnitY),
            [Face.Up] = (Vector3.UnitX, Vector3.UnitZ),
            [Face.Down] = (Vector3.UnitX, -Vector3.UnitZ),
        };

    /// <summary>The three rows of one face, for example <c>["WWG", "WWG", "WWG"]</c>.</summary>
    public static IReadOnlyList<string> Rows(Cube cube, Face face)
    {
        var (column, row) = Orientations[face];
        var normal = face.Normal();
        var stickers = cube.Cubies
            .SelectMany(cubie => cubie.Stickers.Where(s => s.Normal == normal).Select(s => (cubie.Position, s.Color)))
            .ToList();

        return Enumerable.Range(0, GridSize)
            .Select(r => new string(Enumerable.Range(0, GridSize)
                .Select(c => ColorAt(stickers, normal, column, row, r, c).Letter())
                .ToArray()))
            .ToList();
    }

    /// <summary>All 54 cells of the net, keyed by face, row and column.</summary>
    public static IReadOnlyDictionary<(Face Face, int Row, int Column), char> Cells(Cube cube) =>
        Enum.GetValues<Face>()
            .SelectMany(face => Rows(cube, face)
                .SelectMany((text, r) => text.Select((letter, c) => (Key: (face, r, c), Letter: letter))))
            .ToDictionary(cell => cell.Key, cell => cell.Letter);

    public static bool IsCentre(int row, int column) => row == CentreIndex && column == CentreIndex;

    private static Color ColorAt(
        List<(Vector3 Position, Color Color)> stickers, Vector3 normal, Vector3 column, Vector3 row, int r, int c)
    {
        // The cell's cubie position: the face layer, offset along the row and column directions from the face centre.
        var position = normal + row * (r - CentreIndex) + column * (c - CentreIndex);
        return stickers.Single(s => s.Position == position).Color;
    }
}
