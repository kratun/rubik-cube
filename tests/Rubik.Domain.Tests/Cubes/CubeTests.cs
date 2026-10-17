using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.Cubes;

public class CubeTests
{
    private const int CubieCount = 26;
    private const int StickersPerFace = 9;
    private const int StickerCount = 54;
    private const int CornerCount = 8;
    private const int EdgeCount = 12;
    private const int CentreCount = 6;
    private const int CornerStickers = 3;
    private const int EdgeStickers = 2;
    private const int CentreStickers = 1;
    private const int MinCoordinate = -1;
    private const int MaxCoordinate = 1;
    private const int MiddleLayer = 0;

    // Hard-coded on purpose: independent of the production Face-to-normal and colour mappings.
    public static TheoryData<Vector3, Color> SolvedFaces => new()
    {
        { new Vector3(0, 0, 1), Color.Green },
        { new Vector3(1, 0, 0), Color.Red },
        { new Vector3(0, 1, 0), Color.White },
        { new Vector3(0, 0, -1), Color.Blue },
        { new Vector3(-1, 0, 0), Color.Orange },
        { new Vector3(0, -1, 0), Color.Yellow },
    };

    private static IEnumerable<Sticker> StickersFacing(Cube cube, Vector3 normal) =>
        cube.Cubies.SelectMany(c => c.Stickers).Where(s => s.Normal == normal);

    [Theory]
    [MemberData(nameof(SolvedFaces))]
    public void New_SolvedFace_HasNineStickersOfOneColor(Vector3 normal, Color expected)
    {
        var stickers = StickersFacing(new Cube(), normal).ToList();

        Assert.Equal(StickersPerFace, stickers.Count);
        Assert.All(stickers, s => Assert.Equal(expected, s.Color));
    }

    [Fact]
    public void New_CubieSet_Has26DistinctPositionsInRange()
    {
        var positions = new Cube().Cubies.Select(c => c.Position).ToList();

        Assert.Equal(CubieCount, positions.Count);
        Assert.Equal(CubieCount, positions.Distinct().Count());
        Assert.All(positions, p =>
        {
            Assert.InRange(p.X, MinCoordinate, MaxCoordinate);
            Assert.InRange(p.Y, MinCoordinate, MaxCoordinate);
            Assert.InRange(p.Z, MinCoordinate, MaxCoordinate);
        });
        Assert.DoesNotContain(Vector3.Zero, positions);
    }

    [Fact]
    public void New_EveryCubie_HasOneStickerPerNonZeroCoordinateWithMatchingNormal()
    {
        foreach (var cubie in new Cube().Cubies)
        {
            var p = cubie.Position;
            var expected = new List<Vector3>();
            if (p.X != MiddleLayer) expected.Add(new Vector3(p.X, MiddleLayer, MiddleLayer));
            if (p.Y != MiddleLayer) expected.Add(new Vector3(MiddleLayer, p.Y, MiddleLayer));
            if (p.Z != MiddleLayer) expected.Add(new Vector3(MiddleLayer, MiddleLayer, p.Z));

            Assert.Equal(
                expected.OrderBy(v => v.ToString()),
                cubie.Stickers.Select(s => s.Normal).OrderBy(v => v.ToString()));
        }
    }

    [Fact]
    public void New_Totals_Are54StickersAcrossCornersEdgesAndCentres()
    {
        var cubies = new Cube().Cubies;

        Assert.Equal(StickerCount, cubies.Sum(c => c.Stickers.Count));
        Assert.Equal(CornerCount, cubies.Count(c => c.Stickers.Count == CornerStickers));
        Assert.Equal(EdgeCount, cubies.Count(c => c.Stickers.Count == EdgeStickers));
        Assert.Equal(CentreCount, cubies.Count(c => c.Stickers.Count == CentreStickers));
    }
}
