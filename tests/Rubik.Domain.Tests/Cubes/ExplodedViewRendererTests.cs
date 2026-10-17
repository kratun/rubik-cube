using Rubik.Domain.Cubes;
using Rubik.Domain.Tests.TestHelpers;

namespace Rubik.Domain.Tests.Cubes;

public class ExplodedViewRendererTests
{
    private const int GridSize = 3;
    private const int BandCount = 3;
    private const int LineCount = GridSize * BandCount;
    private const int TileStride = 2;

    // A face is three letters with a space between them, and one space separates faces in a line.
    private const int FaceStride = GridSize * TileStride;

    private const int TopBandLine = 0;
    private const int MiddleBandLine = GridSize;
    private const int BottomBandLine = 2 * GridSize;

    private const int LeftmostSlot = 0;
    private const int SecondSlot = 1;
    private const int ThirdSlot = 2;
    private const int FourthSlot = 3;

    // Where each face's first tile sits in the text lines of the net: its first line and its character offset.
    private static readonly IReadOnlyDictionary<Face, (int FirstLine, int Offset)> NetPositions =
        new Dictionary<Face, (int, int)>
        {
            [Face.Up] = (TopBandLine, SecondSlot * FaceStride),
            [Face.Left] = (MiddleBandLine, LeftmostSlot * FaceStride),
            [Face.Front] = (MiddleBandLine, SecondSlot * FaceStride),
            [Face.Right] = (MiddleBandLine, ThirdSlot * FaceStride),
            [Face.Back] = (MiddleBandLine, FourthSlot * FaceStride),
            [Face.Down] = (BottomBandLine, SecondSlot * FaceStride),
        };

    public static TheoryData<string> SequencesAgainstIndependentNetReader => new()
    {
        string.Empty,
        "U",
        "R'",
        "B L'",
        "F R' U B' L D'",
    };

    // Hard-coded on purpose: independent of the production Face-to-normal and colour mappings.
    public static TheoryData<Face, Color> SolvedFaces => new()
    {
        { Face.Front, Color.Green },
        { Face.Right, Color.Red },
        { Face.Up, Color.White },
        { Face.Back, Color.Blue },
        { Face.Left, Color.Orange },
        { Face.Down, Color.Yellow },
    };

    private static readonly IReadOnlyList<string> SolvedLines =
    [
        "      W W W",
        "      W W W",
        "      W W W",
        "O O O G G G R R R B B B",
        "O O O G G G R R R B B B",
        "O O O G G G R R R B B B",
        "      Y Y Y",
        "      Y Y Y",
        "      Y Y Y",
    ];

    [Theory]
    [MemberData(nameof(SolvedFaces))]
    public void Render_SolvedCube_EachFaceIsOneColour(Face face, Color expected)
    {
        var view = ExplodedViewRenderer.Render(new Cube());

        Assert.Equal(GridSize, view.Faces[face].Count);
        Assert.All(view.Faces[face], row =>
        {
            Assert.Equal(GridSize, row.Count);
            Assert.All(row, color => Assert.Equal(expected, color));
        });
    }

    [Fact]
    public void Render_SolvedCube_HasSixFaces()
    {
        var view = ExplodedViewRenderer.Render(new Cube());

        Assert.Equal(Enum.GetValues<Face>().Order(), view.Faces.Keys.Order());
    }

    [Fact]
    public void Render_SolvedCube_TextIsNetLayout()
    {
        var view = ExplodedViewRenderer.Render(new Cube());

        Assert.Equal(SolvedLines, view.Lines);
    }

    [Fact]
    public void Render_TtcSequence_TextEqualsExpectedOutput()
    {
        var view = RenderTtc();

        Assert.Equal(TtcFixtureTestHelper.ExpectedLines, view.Lines);
        Assert.Equal(LineCount, view.Lines.Count);
    }

    [Fact]
    public void Render_TtcSequence_FacesMatchTextTileForTile()
    {
        var view = RenderTtc();

        foreach (var (face, (firstLine, offset)) in NetPositions)
        {
            for (var row = 0; row < GridSize; row++)
            for (var column = 0; column < GridSize; column++)
            {
                Assert.Equal(
                    view.Lines[firstLine + row][offset + column * TileStride],
                    view.Faces[face][row][column].Letter());
            }
        }
    }

    [Theory]
    [MemberData(nameof(SequencesAgainstIndependentNetReader))]
    public void Render_AfterSequence_FacesMatchIndependentNetReader(string sequence)
    {
        var cube = TtcFixtureTestHelper.Apply(new Cube(), sequence);
        var view = ExplodedViewRenderer.Render(cube);

        foreach (var face in Enum.GetValues<Face>())
        {
            var expectedRows = CubeNetTestHelper.Rows(cube, face);
            var actualRows = view.Faces[face].Select(row => new string(row.Select(color => color.Letter()).ToArray()));
            Assert.Equal(expectedRows, actualRows);
        }
    }

    private static ExplodedView RenderTtc() =>
        ExplodedViewRenderer.Render(TtcFixtureTestHelper.Apply(new Cube(), TtcFixtureTestHelper.Sequence));
}
