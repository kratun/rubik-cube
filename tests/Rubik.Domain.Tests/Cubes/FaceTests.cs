using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.Cubes;

public class FaceTests
{
    private const int FaceCount = 6;

    public static TheoryData<Face, Vector3> ExpectedNormals =>
        new()
        {
            { Face.Up, new Vector3(0, 1, 0) },
            { Face.Down, new Vector3(0, -1, 0) },
            { Face.Left, new Vector3(-1, 0, 0) },
            { Face.Right, new Vector3(1, 0, 0) },
            { Face.Front, new Vector3(0, 0, 1) },
            { Face.Back, new Vector3(0, 0, -1) },
        };

    [Theory]
    [MemberData(nameof(ExpectedNormals))]
    public void Normal_EachFace_IsExpectedUnitVector(Face face, Vector3 expected)
    {
        Assert.Equal(expected, face.Normal());
    }

    [Fact]
    public void Normal_AllFaces_AreDistinct()
    {
        var normals = Enum.GetValues<Face>().Select(f => f.Normal()).ToList();

        Assert.Equal(FaceCount, normals.Distinct().Count());
    }
}
