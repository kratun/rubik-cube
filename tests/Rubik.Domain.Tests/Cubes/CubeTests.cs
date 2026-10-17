using Rubik.Domain.Cubes;
using Rubik.Domain.Tests.TestHelpers;

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
    private const int AdjacentTileCount = 12;
    private const int FullTurnCount = 4;
    private const int SexyMoveOrder = 6;
    private const int UndefinedDirection = 99;

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

    private static readonly IReadOnlyList<Move> Scramble =
    [
        new(Face.Front, Direction.Clockwise),
        new(Face.Right, Direction.CounterClockwise),
        new(Face.Up, Direction.Clockwise),
        new(Face.Back, Direction.CounterClockwise),
        new(Face.Left, Direction.Clockwise),
        new(Face.Down, Direction.CounterClockwise),
    ];

    public static TheoryData<Face, Direction> AllMoves
    {
        get
        {
            var data = new TheoryData<Face, Direction>();
            foreach (var face in Enum.GetValues<Face>())
            foreach (var direction in Enum.GetValues<Direction>())
            {
                data.Add(face, direction);
            }

            return data;
        }
    }

    public static TheoryData<Face> AllFaces => new(Enum.GetValues<Face>());

    // Reference nets from an independent simulation (rows top to bottom), each the full 3x3 net of one face.
    // F, R and U list all six faces (as the acceptance criteria do); the other moves list the faces that change.
    public static TheoryData<Move, Face, string[]> ReferenceNets => new()
    {
        { new Move(Face.Front, Direction.CounterClockwise), Face.Up, ["WWW", "WWW", "RRR"] },
        { new Move(Face.Front, Direction.CounterClockwise), Face.Right, ["YRR", "YRR", "YRR"] },
        { new Move(Face.Front, Direction.CounterClockwise), Face.Down, ["OOO", "YYY", "YYY"] },
        { new Move(Face.Front, Direction.CounterClockwise), Face.Left, ["OOW", "OOW", "OOW"] },
        { new Move(Face.Right, Direction.CounterClockwise), Face.Front, ["GGW", "GGW", "GGW"] },
        { new Move(Face.Right, Direction.CounterClockwise), Face.Up, ["WWB", "WWB", "WWB"] },
        { new Move(Face.Right, Direction.CounterClockwise), Face.Back, ["YBB", "YBB", "YBB"] },
        { new Move(Face.Right, Direction.CounterClockwise), Face.Down, ["YYG", "YYG", "YYG"] },
        { new Move(Face.Left, Direction.Clockwise), Face.Up, ["BWW", "BWW", "BWW"] },
        { new Move(Face.Left, Direction.Clockwise), Face.Front, ["WGG", "WGG", "WGG"] },
        { new Move(Face.Left, Direction.Clockwise), Face.Down, ["GYY", "GYY", "GYY"] },
        { new Move(Face.Left, Direction.Clockwise), Face.Back, ["BBY", "BBY", "BBY"] },
        { new Move(Face.Back, Direction.Clockwise), Face.Up, ["RRR", "WWW", "WWW"] },
        { new Move(Face.Back, Direction.Clockwise), Face.Left, ["WOO", "WOO", "WOO"] },
        { new Move(Face.Back, Direction.Clockwise), Face.Down, ["YYY", "YYY", "OOO"] },
        { new Move(Face.Back, Direction.Clockwise), Face.Right, ["RRY", "RRY", "RRY"] },
        { new Move(Face.Down, Direction.Clockwise), Face.Front, ["GGG", "GGG", "OOO"] },
        { new Move(Face.Down, Direction.Clockwise), Face.Right, ["RRR", "RRR", "GGG"] },
        { new Move(Face.Down, Direction.Clockwise), Face.Back, ["BBB", "BBB", "RRR"] },
        { new Move(Face.Down, Direction.Clockwise), Face.Left, ["OOO", "OOO", "BBB"] },
        { new Move(Face.Front, Direction.Clockwise), Face.Up, ["WWW", "WWW", "OOO"] },
        { new Move(Face.Front, Direction.Clockwise), Face.Right, ["WRR", "WRR", "WRR"] },
        { new Move(Face.Front, Direction.Clockwise), Face.Down, ["RRR", "YYY", "YYY"] },
        { new Move(Face.Front, Direction.Clockwise), Face.Left, ["OOY", "OOY", "OOY"] },
        { new Move(Face.Front, Direction.Clockwise), Face.Front, ["GGG", "GGG", "GGG"] },
        { new Move(Face.Front, Direction.Clockwise), Face.Back, ["BBB", "BBB", "BBB"] },
        { new Move(Face.Right, Direction.Clockwise), Face.Up, ["WWG", "WWG", "WWG"] },
        { new Move(Face.Right, Direction.Clockwise), Face.Front, ["GGY", "GGY", "GGY"] },
        { new Move(Face.Right, Direction.Clockwise), Face.Back, ["WBB", "WBB", "WBB"] },
        { new Move(Face.Right, Direction.Clockwise), Face.Down, ["YYB", "YYB", "YYB"] },
        { new Move(Face.Right, Direction.Clockwise), Face.Right, ["RRR", "RRR", "RRR"] },
        { new Move(Face.Right, Direction.Clockwise), Face.Left, ["OOO", "OOO", "OOO"] },
        { new Move(Face.Up, Direction.Clockwise), Face.Front, ["RRR", "GGG", "GGG"] },
        { new Move(Face.Up, Direction.Clockwise), Face.Right, ["BBB", "RRR", "RRR"] },
        { new Move(Face.Up, Direction.Clockwise), Face.Back, ["OOO", "BBB", "BBB"] },
        { new Move(Face.Up, Direction.Clockwise), Face.Left, ["GGG", "OOO", "OOO"] },
        { new Move(Face.Up, Direction.Clockwise), Face.Up, ["WWW", "WWW", "WWW"] },
        { new Move(Face.Up, Direction.Clockwise), Face.Down, ["YYY", "YYY", "YYY"] },
    };

    private static Cube ApplyAll(Cube cube, IEnumerable<Move> moves) =>
        moves.Aggregate(cube, (current, move) => current.Apply(move));

    private static HashSet<(Vector3 Position, Vector3 Normal, Color Color)> Snapshot(Cube cube) =>
        cube.Cubies
            .SelectMany(cubie => cubie.Stickers.Select(s => (cubie.Position, s.Normal, s.Color)))
            .ToHashSet();

    private static bool IsOppositeOf(Face candidate, Face face) => candidate.Normal() == -face.Normal();

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

    [Theory]
    [MemberData(nameof(ReferenceNets))]
    public void Apply_FromSolved_MatchesReferenceNet(Move move, Face face, string[] expectedRows)
    {
        var cube = new Cube().Apply(move);

        Assert.Equal(expectedRows, CubeNetTestHelper.Rows(cube, face));
    }

    [Theory]
    [MemberData(nameof(AllMoves))]
    public void Apply_FromSolved_KeepsCentresAndChangesOnlyTwelveAdjacentTiles(Face face, Direction direction)
    {
        var before = CubeNetTestHelper.Cells(new Cube());
        var after = CubeNetTestHelper.Cells(new Cube().Apply(new Move(face, direction)));

        var changed = before.Keys.Where(key => before[key] != after[key]).ToList();

        Assert.All(
            before.Keys.Where(key => CubeNetTestHelper.IsCentre(key.Row, key.Column)),
            key => Assert.Equal(before[key], after[key]));
        Assert.Equal(AdjacentTileCount, changed.Count);
        Assert.DoesNotContain(changed, key => key.Face == face || IsOppositeOf(key.Face, face));
    }

    [Theory]
    [MemberData(nameof(AllMoves))]
    public void Apply_AnyMove_KeepsCubieAndStickerCounts(Face face, Direction direction)
    {
        var cube = ApplyAll(new Cube(), Scramble).Apply(new Move(face, direction));

        Assert.Equal(CubieCount, cube.Cubies.Count);
        Assert.Equal(CubieCount, cube.Cubies.Select(c => c.Position).Distinct().Count());
        Assert.Equal(StickerCount, cube.Cubies.Sum(c => c.Stickers.Count));
    }

    [Theory]
    [MemberData(nameof(AllFaces))]
    public void Apply_ClockwiseFourTimes_RestoresPreviousState(Face face)
    {
        var start = ApplyAll(new Cube(), Scramble);
        var turned = ApplyAll(start, Enumerable.Repeat(new Move(face, Direction.Clockwise), FullTurnCount));

        Assert.True(Snapshot(start).SetEquals(Snapshot(turned)));
    }

    [Theory]
    [MemberData(nameof(AllMoves))]
    public void Apply_MoveThenItsInverse_RestoresPreviousState(Face face, Direction direction)
    {
        var inverse = direction == Direction.Clockwise ? Direction.CounterClockwise : Direction.Clockwise;
        var start = ApplyAll(new Cube(), Scramble);

        var restored = start.Apply(new Move(face, direction)).Apply(new Move(face, inverse));

        Assert.True(Snapshot(start).SetEquals(Snapshot(restored)));
    }

    [Fact]
    public void Apply_SexyMoveSixTimes_RestoresSolvedCube()
    {
        // R U R' U' has order six: a known identity that checks the moves compose correctly.
        IReadOnlyList<Move> sexyMove =
        [
            new(Face.Right, Direction.Clockwise),
            new(Face.Up, Direction.Clockwise),
            new(Face.Right, Direction.CounterClockwise),
            new(Face.Up, Direction.CounterClockwise),
        ];

        var cube = ApplyAll(new Cube(), Enumerable.Repeat(sexyMove, SexyMoveOrder).SelectMany(moves => moves));

        Assert.True(Snapshot(new Cube()).SetEquals(Snapshot(cube)));
    }

    [Fact]
    public void Apply_UndefinedDirection_Throws()
    {
        var move = new Move(Face.Front, (Direction)UndefinedDirection);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Cube().Apply(move));
    }

    [Fact]
    public void Apply_Move_ReturnsNewCubeAndLeavesOriginalUnchanged()
    {
        var original = new Cube();
        var solvedSnapshot = Snapshot(original);

        var turned = original.Apply(new Move(Face.Front, Direction.Clockwise));

        Assert.NotSame(original, turned);
        Assert.True(solvedSnapshot.SetEquals(Snapshot(original)));
        Assert.False(solvedSnapshot.SetEquals(Snapshot(turned)));
    }
}
